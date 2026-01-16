using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BenVoxel.Models;
using Godot;

namespace BenVoxelEditor;

/// <summary>
/// Manages GPU compute shader for bounds calculation.
/// Processes segment textures to find min/max voxel coordinates.
///
/// Synchronous execution with throttling - GPU parallelism makes this fast.
/// Godot 4 doesn't support true async GPU readback, so we embrace the sync
/// but benefit from GPU parallelism for the actual computation.
/// </summary>
public sealed class BoundsCompute : IDisposable
{
	// Bounds result: 6 uints (minX, maxX, minY, maxY, minZ, maxZ) per segment
	private const int BoundsPerSegment = 6;
	private const int MaxSegments = 256;

	private readonly RenderingDevice _rd;
	private Rid _shader;
	private Rid _pipeline;
	private Rid _boundsBuffer;

	// Throttling to avoid computing every frame
	private double _lastTrimTime;
	private const double ThrottleIntervalSeconds = 0.1; // Max once per 100ms

	/// <summary>
	/// Whether the compute shader is available and initialized.
	/// </summary>
	public bool IsAvailable => _shader.IsValid && _pipeline.IsValid && _boundsBuffer.IsValid;

	public BoundsCompute()
	{
		// Use the main RenderingDevice to access textures from the rendering pipeline
		// via RenderingServer.TextureGetRdTexture()
		_rd = RenderingServer.GetRenderingDevice();
		if (_rd == null)
		{
			GD.PrintErr("BoundsCompute: RenderingDevice not available");
			return;
		}

		InitializeShader();
		InitializeBuffers();
	}

	private void InitializeShader()
	{
		// Load and compile the compute shader
		RDShaderFile shaderFile = GD.Load<RDShaderFile>("res://bounds_reduction.glsl");
		if (shaderFile == null)
		{
			GD.PrintErr("BoundsCompute: Failed to load bounds_reduction.glsl");
			return;
		}

		RDShaderSpirV spirv = shaderFile.GetSpirV();
		if (spirv == null)
		{
			GD.PrintErr("BoundsCompute: Failed to get SPIR-V from shader");
			return;
		}

		_shader = _rd.ShaderCreateFromSpirV(spirv);
		if (!_shader.IsValid)
		{
			GD.PrintErr("BoundsCompute: Failed to create shader");
			return;
		}

		_pipeline = _rd.ComputePipelineCreate(_shader);
		if (!_pipeline.IsValid)
		{
			GD.PrintErr("BoundsCompute: Failed to create compute pipeline");
			return;
		}

		GD.Print("BoundsCompute: Shader initialized successfully");
	}

	private void InitializeBuffers()
	{
		if (!_shader.IsValid) return;

		// Create output buffer for bounds results
		// 6 uints per segment × max segments × 4 bytes per uint
		int bufferSize = BoundsPerSegment * MaxSegments * sizeof(uint);
		byte[] initialData = new byte[bufferSize];

		_boundsBuffer = _rd.StorageBufferCreate((uint)bufferSize, initialData);
		if (!_boundsBuffer.IsValid)
		{
			GD.PrintErr("BoundsCompute: Failed to create bounds buffer");
			return;
		}

		GD.Print($"BoundsCompute: Created bounds buffer ({bufferSize} bytes)");
	}

	/// <summary>
	/// Computes bounds using GPU, with throttling.
	/// Returns null if throttled, empty, or not available.
	/// </summary>
	/// <param name="segmentTextures">Segment textures already on GPU</param>
	/// <param name="directory">Segment directory</param>
	/// <param name="force">If true, bypasses throttling</param>
	/// <returns>Computed bounds, or null</returns>
	public SegmentedBrickModel.Bounds? ComputeBounds(
		IReadOnlyDictionary<uint, ImageTexture3D> segmentTextures,
		IReadOnlyList<VoxelBridge.SegmentEntry> directory,
		bool force = false)
	{
		if (!IsAvailable)
			return null;

		// Throttle unless forced
		if (!force)
		{
			double now = Time.GetTicksMsec() / 1000.0;
			if (now - _lastTrimTime < ThrottleIntervalSeconds)
				return null;
			_lastTrimTime = now;
		}

		if (directory.Count == 0)
			return null;

		// Clear the bounds buffer to initial values
		ClearBoundsBuffer();

		// Dispatch compute for each segment
		DispatchCompute(segmentTextures, directory);

		// Note: On main RenderingDevice, we can't call Submit/Sync.
		// BufferGetData should implicitly wait for compute to complete.
		return ReadResults(directory.Count);
	}

	private void ClearBoundsBuffer()
	{
		// Initialize buffer with max values for min and 0 for max
		// This allows atomic min/max to work correctly
		uint[] initialValues = new uint[BoundsPerSegment * MaxSegments];
		for (int i = 0; i < MaxSegments; i++)
		{
			int baseIdx = i * BoundsPerSegment;
			initialValues[baseIdx + 0] = uint.MaxValue; // minX
			initialValues[baseIdx + 1] = 0;             // maxX
			initialValues[baseIdx + 2] = uint.MaxValue; // minY
			initialValues[baseIdx + 3] = 0;             // maxY
			initialValues[baseIdx + 4] = uint.MaxValue; // minZ
			initialValues[baseIdx + 5] = 0;             // maxZ
		}

		byte[] bytes = MemoryMarshal.AsBytes(initialValues.AsSpan()).ToArray();
		_rd.BufferUpdate(_boundsBuffer, 0, (uint)bytes.Length, bytes);
	}

	private void DispatchCompute(
		IReadOnlyDictionary<uint, ImageTexture3D> segmentTextures,
		IReadOnlyList<VoxelBridge.SegmentEntry> directory)
	{
		long computeList = _rd.ComputeListBegin();

		for (int i = 0; i < directory.Count; i++)
		{
			VoxelBridge.SegmentEntry entry = directory[i];
			uint segmentId = ((uint)entry.X << 18) | ((uint)entry.Y << 9) | (uint)entry.Z;

			if (!segmentTextures.TryGetValue(segmentId, out ImageTexture3D texture))
				continue;

			// Get the texture's RID and convert to RenderingDevice-compatible RID
			Rid highLevelRid = texture.GetRid();
			if (!highLevelRid.IsValid)
				continue;

			// Bridge from high-level texture to RenderingDevice texture
			Rid textureRid = RenderingServer.TextureGetRdTexture(highLevelRid);
			if (!textureRid.IsValid)
				continue;

			// Create sampler for the texture
			RDSamplerState samplerState = new()
			{
				MinFilter = RenderingDevice.SamplerFilter.Nearest,
				MagFilter = RenderingDevice.SamplerFilter.Nearest,
				MipFilter = RenderingDevice.SamplerFilter.Nearest,
				RepeatU = RenderingDevice.SamplerRepeatMode.ClampToEdge,
				RepeatV = RenderingDevice.SamplerRepeatMode.ClampToEdge,
				RepeatW = RenderingDevice.SamplerRepeatMode.ClampToEdge
			};
			Rid sampler = _rd.SamplerCreate(samplerState);

			// Create uniform set for this segment
			Godot.Collections.Array<RDUniform> uniforms = [];

			// Binding 0: Segment texture (sampler3D)
			RDUniform textureUniform = new()
			{
				UniformType = RenderingDevice.UniformType.SamplerWithTexture,
				Binding = 0
			};
			textureUniform.AddId(sampler);
			textureUniform.AddId(textureRid);
			uniforms.Add(textureUniform);

			// Binding 1: Bounds buffer
			RDUniform bufferUniform = new()
			{
				UniformType = RenderingDevice.UniformType.StorageBuffer,
				Binding = 1
			};
			bufferUniform.AddId(_boundsBuffer);
			uniforms.Add(bufferUniform);

			Rid uniformSet = _rd.UniformSetCreate(uniforms, _shader, 0);

			// Prepare push constants
			// segment_index, segment_origin_x, segment_origin_y, segment_origin_z
			uint[] pushConstants =
			[
				(uint)i,
				(uint)(entry.X * 128),
				(uint)(entry.Y * 128),
				(uint)(entry.Z * 128)
			];
			byte[] pushConstantBytes = MemoryMarshal.AsBytes(pushConstants.AsSpan()).ToArray();

			// Bind and dispatch
			_rd.ComputeListBindComputePipeline(computeList, _pipeline);
			_rd.ComputeListBindUniformSet(computeList, uniformSet, 0);
			_rd.ComputeListSetPushConstant(computeList, pushConstantBytes, (uint)pushConstantBytes.Length);

			// Dispatch 1 workgroup (4×4×4 threads handle entire 64³ segment)
			_rd.ComputeListDispatch(computeList, 1, 1, 1);

			// Add barrier between segments to ensure writes complete before next segment
			_rd.ComputeListAddBarrier(computeList);
		}

		// Final barrier to ensure all writes are complete before we read
		_rd.ComputeListAddBarrier(computeList);
		_rd.ComputeListEnd();
	}

	private SegmentedBrickModel.Bounds? ReadResults(int segmentCount)
	{
		byte[] data = _rd.BufferGetData(_boundsBuffer);
		if (data == null || data.Length == 0)
			return null;

		uint[] values = MemoryMarshal.Cast<byte, uint>(data).ToArray();

		// Combine results from all segments
		uint globalMinX = uint.MaxValue, globalMaxX = 0;
		uint globalMinY = uint.MaxValue, globalMaxY = 0;
		uint globalMinZ = uint.MaxValue, globalMaxZ = 0;
		bool hasAnyVoxels = false;

		for (int i = 0; i < segmentCount; i++)
		{
			int baseIdx = i * BoundsPerSegment;
			uint minX = values[baseIdx + 0];
			uint maxX = values[baseIdx + 1];
			uint minY = values[baseIdx + 2];
			uint maxY = values[baseIdx + 3];
			uint minZ = values[baseIdx + 4];
			uint maxZ = values[baseIdx + 5];

			// Check if this segment had any voxels (maxX >= minX means valid)
			if (maxX >= minX && maxY >= minY && maxZ >= minZ)
			{
				hasAnyVoxels = true;
				globalMinX = Math.Min(globalMinX, minX);
				globalMaxX = Math.Max(globalMaxX, maxX);
				globalMinY = Math.Min(globalMinY, minY);
				globalMaxY = Math.Max(globalMaxY, maxY);
				globalMinZ = Math.Min(globalMinZ, minZ);
				globalMaxZ = Math.Max(globalMaxZ, maxZ);
			}
		}

		if (!hasAnyVoxels)
			return null;

		return new SegmentedBrickModel.Bounds(
			(ushort)globalMinX, (ushort)globalMaxX,
			(ushort)globalMinY, (ushort)globalMaxY,
			(ushort)globalMinZ, (ushort)globalMaxZ);
	}

	public void Dispose()
	{
		if (_boundsBuffer.IsValid)
			_rd.FreeRid(_boundsBuffer);
		if (_pipeline.IsValid)
			_rd.FreeRid(_pipeline);
		if (_shader.IsValid)
			_rd.FreeRid(_shader);
	}
}
