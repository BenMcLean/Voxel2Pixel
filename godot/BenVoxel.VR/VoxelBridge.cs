using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BenVoxel.Models;
using Godot;

namespace BenVoxel.VR;

/// <summary>
/// Bridges SegmentedBrickModel (CPU) to Godot GPU textures for raymarching.
///
/// The model is split into segments of 128³ voxels. Each loaded segment becomes one 128³ 3D texture in
/// R8 format: one texel is one voxel, and its byte is the voxel's palette index. R8 is unsigned-normalised
/// (not an integer format), so the shader reads it with texelFetch and recovers the index as
/// round(value * 255), which is exact. The CPU model stores 2×2×2 bricks of 8 bytes; they are unpacked
/// into this linear layout on upload. Each texture is 128³ × 1 byte = 2 MB.
///
/// A small directory texture tells the shader which segments exist. It is rebuilt whenever a segment
/// loads or unloads.
///
/// What the shader (voxel_raymarch.gdshader) expects:
/// - uniform int active_segment_count         = ActiveSegmentCount
/// - uniform sampler2D segment_directory      = DirectoryTexture  (MaxSegments×1 RGBAF;
///                                              texel i = segX, segY, segZ, texture slot, as floats)
/// - uniform sampler3D segment_voxels[64]     = the entries of Textures, ordered as Directory is, so a
///                                              directory entry's TextureIndex is its array position
///
/// Voxel coordinates here are Z-up storage coordinates (MagicaVoxel convention). A voxel at (x, y, z)
/// is in segment (x >> 7, y >> 7, z >> 7), at texel (x &amp; 127, y &amp; 127, z &amp; 127) of that segment's texture.
///
/// The bridge also tracks when removals may have left the model's bounds larger than necessary and
/// can recompute tight bounds on the GPU (see <see cref="Poll"/> and <see cref="TrimBounds"/>).
/// </summary>
public sealed class VoxelBridge : IDisposable
{
	public readonly record struct SegmentEntry(ushort X, ushort Y, ushort Z, uint TextureIndex);

	private const int VoxelsPerAxis = 128; // 128³ voxels per segment
	private const int BricksPerAxis = 64;  // 64³ bricks per segment (CPU storage)
	private const int TotalBricks = BricksPerAxis * BricksPerAxis * BricksPerAxis; // 262,144
	private const int BytesPerSlice = VoxelsPerAxis * VoxelsPerAxis; // 16 KB, one Z slice of R8 texels

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

	/// <summary>
	/// Maximum number of segments resident on the GPU at once. Must equal the size of the
	/// <c>segment_voxels</c> sampler3D array in voxel_raymarch.gdshader; if it is larger, the shader
	/// indexes past the end of the array. It also sizes the directory texture.
	///
	/// TEMPORARY LIMIT - NOT PART OF THE FORMAT. The file format addresses 512 segments per axis
	/// (SegmentedBrickModel.md), and each resident segment costs 2 MB of GPU memory here, so 64 is
	/// 128 MB. Supporting big scenes needs a different residency scheme, for example one large 3D atlas
	/// texture whose slots are paged in and out, with the directory mapping segment -> atlas slot. That
	/// would replace this constant, the per-segment ImageTexture3D dictionary, the sampler array in the
	/// shader, and the texture-array binding in Root.cs (SetupShaderUniforms / UpdateShaderSegments).
	/// </summary>
	public const int MaxSegments = 64;

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
				TryUploadSegment(segmentId, bricks);
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

		if (TryUploadSegment(segmentId, bricks))
			RebuildDirectory();
	}

	/// <summary>
	/// Uploads a segment's full texture (2 MB) unless doing so would exceed <see cref="MaxSegments"/>.
	/// Replacing a segment that is already resident is always allowed. A refused segment is NOT drawn:
	/// its voxels are treated as empty space, and an error is logged. Does not rebuild the directory.
	/// </summary>
	/// <returns>True if the segment is now resident on the GPU.</returns>
	private bool TryUploadSegment(uint segmentId, ulong[] bricks)
	{
		if (!_textures.ContainsKey(segmentId) && _textures.Count >= MaxSegments)
		{
			GD.PrintErr($"VoxelBridge: segment {segmentId} not uploaded; GPU limit of {MaxSegments} resident segments reached");
			return false;
		}
		_textures[segmentId] = CreateSegmentTexture(bricks);
		return true;
	}

	private void HandleBrickDirty(uint segmentId, int brickIndex, ulong payload)
	{
		if (!_textures.TryGetValue(segmentId, out ImageTexture3D texture))
			return; // Segment not resident on GPU

		// ImageTexture3D can only be updated whole, so the segment's texture is rewritten from the model.
		// Update keeps the same texture object, so nothing has to be rebound.
		if (_model.TryGetSegment(segmentId, out ulong[] bricks))
			texture.Update(CreateSliceImages(bricks));

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
	/// Creates the 128³ R8 texture for a segment from its 64³ brick array.
	/// </summary>
	private ImageTexture3D CreateSegmentTexture(ulong[] bricks)
	{
		ImageTexture3D texture = new();
		texture.Create(Image.Format.R8, VoxelsPerAxis, VoxelsPerAxis, VoxelsPerAxis, false, CreateSliceImages(bricks));
		return texture;
	}

	/// <summary>
	/// Unpacks bricks into one R8 image per Z slice, the form ImageTexture3D takes. Brick i sits at
	/// (i &amp; 63, (i >> 6) &amp; 63, i >> 12); within a brick, byte k is the voxel at offset
	/// (k &amp; 1, (k >> 1) &amp; 1, k >> 2). Empty bricks are skipped, since the buffer starts zeroed.
	/// </summary>
	private static Godot.Collections.Array<Image> CreateSliceImages(ulong[] bricks)
	{
		byte[] voxels = new byte[VoxelsPerAxis * BytesPerSlice];
		for (int i = 0; i < TotalBricks; i++)
		{
			ulong brick = bricks[i];
			if (brick == 0)
				continue;
			int x0 = (i & 0x3F) << 1, y0 = ((i >> 6) & 0x3F) << 1, z0 = (i >> 12) << 1;
			for (int k = 0; k < 8; k++)
			{
				int x = x0 + (k & 1), y = y0 + ((k >> 1) & 1), z = z0 + (k >> 2);
				voxels[z * BytesPerSlice + y * VoxelsPerAxis + x] = (byte)(brick >> (k * 8));
			}
		}

		Godot.Collections.Array<Image> images = [];
		for (int z = 0; z < VoxelsPerAxis; z++)
		{
			byte[] sliceData = new byte[BytesPerSlice];
			Array.Copy(voxels, z * BytesPerSlice, sliceData, 0, BytesPerSlice);
			images.Add(Image.CreateFromData(VoxelsPerAxis, VoxelsPerAxis, false, Image.Format.R8, sliceData));
		}
		return images;
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
