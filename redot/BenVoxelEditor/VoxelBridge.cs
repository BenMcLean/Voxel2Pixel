using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BenVoxel.Models;
using Godot;

namespace BenVoxelEditor;

/// <summary>
/// Bridges SegmentedBrickModel (CPU) to Godot GPU textures for raymarching.
/// Uploads 64³ brick arrays directly as RG32UI textures (2 MB each) - NO conversion needed!
///
/// Usage in spatial shaders (Godot 4):
/// - Bind DirectoryTexture as uniform sampler2D (RGBA32UI format)
/// - Bind segment textures as usampler3D array (RG32UI brick data)
/// - Use ActiveSegmentCount for loop bounds
///
/// Shader example (Godot Shader Language):
/// uniform int active_segment_count;
/// uniform usampler2D segment_directory; // RGBA32UI: (segX, segY, segZ, texIndex)
/// uniform usampler3D segment_bricks[64]; // RG32UI: each texel = one brick (8 bytes)
///
/// uint fetch_voxel(ivec3 world_pos) {
///     ivec3 seg_coord = world_pos >> 7;
///
///     // Linear search through directory texture
///     for (int i = 0; i < active_segment_count; i++) {
///         uvec4 entry = texelFetch(segment_directory, ivec2(i, 0), 0);
///         if (entry.xyz == uvec3(seg_coord)) {
///             // Extract brick coordinates and voxel offset
///             ivec3 brick_coord = (world_pos >> 1) & 0x3F;  // Bits 1-6
///             ivec3 voxel_offset = world_pos & 1;           // Bit 0
///
///             // Fetch entire brick (8 bytes as uvec2)
///             uvec2 brick = texelFetch(segment_bricks[entry.w], brick_coord, 0).rg;
///
///             // Extract specific voxel byte
///             int byte_idx = (voxel_offset.z << 2) | (voxel_offset.y << 1) | voxel_offset.x;
///             return (byte_idx < 4)
///                 ? (brick.x >> (byte_idx * 8)) & 0xFFu
///                 : (brick.y >> ((byte_idx - 4) * 8)) & 0xFFu;
///         }
///     }
///     return 0u; // Empty space
/// }
/// </summary>
public sealed class VoxelBridge : IDisposable
{
	public readonly record struct SegmentEntry(ushort X, ushort Y, ushort Z, uint TextureIndex);

	private const int BricksPerAxis = 64;  // 64³ bricks per segment
	private const int BytesPerBrick = 8;   // ulong = 8 bytes
	private const int TotalBricks = BricksPerAxis * BricksPerAxis * BricksPerAxis; // 262,144
	private const int TextureSize = TotalBricks * BytesPerBrick; // 2 MB

	private readonly SegmentedBrickModel _model;

	/// <summary>
	/// SegmentId → GPU texture (ImageTexture3D)
	/// </summary>
	private readonly Dictionary<uint, ImageTexture3D> _textures = [];

	/// <summary>
	/// Dense GPU directory (rebuilt on load/unload)
	/// </summary>
	private readonly List<SegmentEntry> _directory = [];

	/// <summary>
	/// GPU directory texture for spatial shader access
	/// Each texel = one segment entry (segX, segY, segZ, texIndex)
	/// </summary>
	private ImageTexture _directoryTexture;

	private const int MaxSegments = 256; // Max directory size

	// GPU bounds computation
	private BoundsCompute _boundsCompute;
	private bool _boundsPotentiallyOversized;

	public VoxelBridge(SegmentedBrickModel model)
	{
		_model = model ?? throw new ArgumentNullException(nameof(model));

		_model.OnSegmentLoaded += HandleSegmentLoaded;
		_model.OnSegmentUnloaded += HandleSegmentUnloaded;
		_model.OnBrickDirty += HandleBrickDirty;

		// Initialize GPU bounds compute
		_boundsCompute = new BoundsCompute();

		// Upload any segments that were already loaded before we subscribed to events
		UploadExistingSegments();
	}

	private void UploadExistingSegments()
	{
		// Enumerate all existing bricks to find which segments exist
		HashSet<uint> seenSegments = new HashSet<uint>();
		foreach (BenVoxel.Structs.VoxelBrick brick in _model)
		{
			// Calculate segment ID from brick coordinates
			uint segmentId = ((uint)(brick.X >> 7) << 18) | ((uint)(brick.Y >> 7) << 9) | (uint)(brick.Z >> 7);

			if (seenSegments.Add(segmentId) && _model.TryGetSegment(segmentId, out ulong[] bricks))
			{
				// Upload this segment
				ImageTexture3D texture = CreateSegmentTexture(bricks);
				_textures[segmentId] = texture;
			}
		}

		if (seenSegments.Count > 0)
		{
			RebuildDirectory();
		}
	}

	public IReadOnlyList<SegmentEntry> Directory => _directory;
	public IReadOnlyDictionary<uint, ImageTexture3D> Textures => _textures;
	public ImageTexture DirectoryTexture => _directoryTexture;
	public int ActiveSegmentCount => _directory.Count;

	private void HandleSegmentLoaded(uint segmentId)
	{
		if (!_model.TryGetSegment(segmentId, out ulong[] bricks))
			return;

		// Upload full segment texture (2 MB)
		ImageTexture3D texture = CreateSegmentTexture(bricks);
		_textures[segmentId] = texture;
		RebuildDirectory();
	}

	private void HandleBrickDirty(uint segmentId, int brickIndex, ulong payload)
	{
		if (!_textures.TryGetValue(segmentId, out ImageTexture3D texture))
			return; // Segment not resident on GPU

		// Incremental update: only update 2×2×2 voxel region (8 bytes)
		int bx = brickIndex & 0x3F;
		int by = (brickIndex >> 6) & 0x3F;
		int bz = (brickIndex >> 12) & 0x3F;

		UpdateBrickInTexture(texture, bx, by, bz, payload);

		// If any voxels were cleared, bounds might need shrinking
		// We detect this by checking if the new payload has any zero bytes where it didn't before
		// For simplicity, mark as potentially oversized if payload is 0 or has any zero bytes
		if (payload == 0 || HasClearedVoxels(payload))
		{
			_boundsPotentiallyOversized = true;
		}
	}

	/// <summary>
	/// Checks if a brick payload might have cleared voxels (contains zero bytes).
	/// </summary>
	private static bool HasClearedVoxels(ulong payload)
	{
		// Check each byte of the payload
		for (int i = 0; i < 8; i++)
		{
			if (((payload >> (i * 8)) & 0xFF) == 0)
				return true;
		}
		return false;
	}

	private void HandleSegmentUnloaded(uint segmentId)
	{
		if (_textures.Remove(segmentId))
		{
			// ImageTexture3D will be garbage collected automatically
		}
		RebuildDirectory();
	}

	private void RebuildDirectory()
	{
		_directory.Clear();
		uint index = 0;
		foreach (uint segmentId in _textures.Keys)
			_directory.Add(new SegmentEntry(
				X: (ushort)((segmentId >> 18) & 0x1FF),
				Y: (ushort)((segmentId >> 9) & 0x1FF),
				Z: (ushort)(segmentId & 0x1FF),
				TextureIndex: index++));

		// Upload directory to GPU storage buffer
		UploadDirectoryToGPU();
	}

	/// <summary>
	/// Uploads segment directory to GPU texture for spatial shader access.
	/// Format: RGBAF (2D texture, MaxSegments×1)
	/// Each texel: (segX, segY, segZ, texIndex) as floats
	/// </summary>
	private void UploadDirectoryToGPU()
	{
		_directoryTexture = null;

		if (_directory.Count == 0)
		{
			return;
		}

		// Pack directory into RGBAF texture data
		// Each pixel = 16 bytes (4 floats)
		byte[] textureData = new byte[MaxSegments * 16];

		for (int i = 0; i < _directory.Count; i++)
		{
			SegmentEntry entry = _directory[i];
			int offset = i * 16;

			// RGBA channels: segmentX, segmentY, segmentZ, textureIndex (as floats)
			BitConverter.GetBytes((float)entry.X).CopyTo(textureData, offset + 0);  // R
			BitConverter.GetBytes((float)entry.Y).CopyTo(textureData, offset + 4);  // G
			BitConverter.GetBytes((float)entry.Z).CopyTo(textureData, offset + 8);  // B
			BitConverter.GetBytes((float)entry.TextureIndex).CopyTo(textureData, offset + 12); // A
		}

		// Fill remaining entries with zeros (empty segments)
		// Already zeroed by default allocation

		// Create directory texture using ImageTexture
		Image image = Image.CreateFromData(MaxSegments, 1, false, Image.Format.Rgbaf, textureData);
		_directoryTexture = ImageTexture.CreateFromImage(image);

		GD.Print($"Directory texture created, {_directory.Count} segments");
	}

	/// <summary>
	/// Uploads 64³ brick array as RGBAF texture (simpler than trying to use uint formats).
	/// Converts ulong bricks to 2xfloat32 representation.
	/// </summary>
	private ImageTexture3D CreateSegmentTexture(ulong[] bricks)
	{
		// Convert brick data to RGBAF format (4 floats per texel)
		// We'll pack the ulong as: R=low32bits, G=high32bits, B=0, A=0
		byte[] textureData = new byte[TotalBricks * 16]; // 4 floats × 4 bytes

		for (int i = 0; i < TotalBricks; i++)
		{
			ulong brick = bricks[i];
			uint low = (uint)(brick & 0xFFFFFFFF);
			uint high = (uint)(brick >> 32);

			int offset = i * 16;

			// Store as floats (reinterpret uint bits as float)
			BitConverter.GetBytes(BitConverter.UInt32BitsToSingle(low)).CopyTo(textureData, offset + 0);   // R
			BitConverter.GetBytes(BitConverter.UInt32BitsToSingle(high)).CopyTo(textureData, offset + 4);  // G
			BitConverter.GetBytes(0f).CopyTo(textureData, offset + 8);   // B (unused)
			BitConverter.GetBytes(0f).CopyTo(textureData, offset + 12);  // A (unused)
		}

		// Create array of Z-slice images for 3D texture
		Godot.Collections.Array<Image> images = new Godot.Collections.Array<Image>();
		int bytesPerSlice = BricksPerAxis * BricksPerAxis * 16;

		for (int z = 0; z < BricksPerAxis; z++)
		{
			byte[] sliceData = new byte[bytesPerSlice];
			Array.Copy(textureData, z * bytesPerSlice, sliceData, 0, bytesPerSlice);
			images.Add(Image.CreateFromData(BricksPerAxis, BricksPerAxis, false, Image.Format.Rgbaf, sliceData));
		}

		ImageTexture3D texture = new ImageTexture3D();
		texture.Create(Image.Format.Rgbaf, BricksPerAxis, BricksPerAxis, BricksPerAxis, false, images);
		GD.Print($"Created 3D brick texture: {BricksPerAxis}³ RGBAF");
		return texture;
	}

	/// <summary>
	/// Updates a single brick (2×2×2 voxels) in an existing GPU texture.
	///
	/// PERFORMANCE NOTE: ImageTexture3D doesn't support partial updates efficiently.
	/// Re-uploading the entire segment is acceptable for editor use with moderate edit rates.
	///
	/// Future optimization: Implement dirty tracking and batch updates per frame.
	/// </summary>
	private void UpdateBrickInTexture(ImageTexture3D texture, int bx, int by, int bz, ulong brick)
	{
		// Find which segment this texture belongs to
		uint? foundSegmentId = null;
		foreach (KeyValuePair<uint, ImageTexture3D> kvp in _textures)
		{
			if (kvp.Value == texture)
			{
				foundSegmentId = kvp.Key;
				break;
			}
		}

		if (!foundSegmentId.HasValue || !_model.TryGetSegment(foundSegmentId.Value, out ulong[] bricks))
			return;

		// Re-create the texture with updated data (same as CreateSegmentTexture)
		// This is inefficient but simple for now
		ImageTexture3D newTexture = CreateSegmentTexture(bricks);
		_textures[foundSegmentId.Value] = newTexture;
	}

	#region Bounds Trimming

	/// <summary>
	/// Whether bounds might be larger than necessary due to voxel removals.
	/// </summary>
	public bool BoundsPotentiallyOversized => _boundsPotentiallyOversized;

	/// <summary>
	/// Call this every frame to check if bounds need trimming.
	/// Uses GPU compute with throttling (max once per 100ms).
	/// </summary>
	public void Poll()
	{
		if (!_boundsPotentiallyOversized)
			return;

		// Try to compute bounds (will be throttled if called too frequently)
		SegmentedBrickModel.Bounds? result = _boundsCompute?.ComputeBounds(_textures, _directory);

		if (result.HasValue)
		{
			_model.ApplyTrimmedBounds(result);
			_boundsPotentiallyOversized = false;
			GD.Print($"VoxelBridge: Bounds trimmed to {result}");
		}
	}

	/// <summary>
	/// Forces an immediate bounds recalculation via GPU compute shader.
	/// Bypasses throttling. Use for save/export operations.
	/// </summary>
	public void TrimBounds()
	{
		if (_boundsCompute == null || !_boundsCompute.IsAvailable)
		{
			// Fallback to CPU recalculation
			_model.RecalculateBounds();
			_boundsPotentiallyOversized = false;
			GD.Print("VoxelBridge: Bounds trimmed via CPU fallback");
			return;
		}

		SegmentedBrickModel.Bounds? result = _boundsCompute.ComputeBounds(_textures, _directory, force: true);
		_model.ApplyTrimmedBounds(result);
		_boundsPotentiallyOversized = false;

		GD.Print($"VoxelBridge: Bounds trimmed to {result}");
	}

	#endregion

	public void Dispose()
	{
		_model.OnSegmentLoaded -= HandleSegmentLoaded;
		_model.OnSegmentUnloaded -= HandleSegmentUnloaded;
		_model.OnBrickDirty -= HandleBrickDirty;

		_boundsCompute?.Dispose();
		_boundsCompute = null;

		// ImageTexture3D and ImageTexture will be garbage collected automatically
		_textures.Clear();
		_directoryTexture = null;
		_directory.Clear();
	}
}
