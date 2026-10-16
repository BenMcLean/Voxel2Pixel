using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using BenVoxel.FileToVoxCore;
using BenVoxel.Models;
using BenVoxelEditor;
using Godot;

/// <summary>
/// Demo scene root: loads a .vox model, uploads it to the GPU through <see cref="VoxelBridge"/> and
/// renders it by raymarching inside a bounding box (see voxel_raymarch.gdshader).
///
/// Coordinate systems: voxel data is Z-up (MagicaVoxel); Godot is Y-up. Everything on the C# side that
/// positions the box or camera converts (x, y, z)_zup to (x, z, y)_yup. The shader converts back when
/// it reads voxels, so the scene's world space is voxel space, one unit per voxel.
/// </summary>
public partial class Root : Node3D
{
	/// <summary>Uploads the model's segments to GPU textures and keeps them in sync with edits.</summary>
	private VoxelBridge _voxelBridge;
	/// <summary>Material running voxel_raymarch.gdshader; carries every shader uniform.</summary>
	private ShaderMaterial _raymarchMaterial;
	private FreeLookCamera _camera;
	/// <summary>256x1 RGBA8 palette lookup texture bound to the shader.</summary>
	private ImageTexture _paletteTexture;

	private SegmentedBrickModel _model;
	/// <summary>256 palette colours, each a uint packed as 0xRRGGBBAA ("rgba8888, big endian", see VoxFileModel.Argb2rgba). Entry 0 is empty.</summary>
	private uint[] _palette;

	/// <summary>
	/// Box mesh instance covering the model's bounds. It is only the rasterisation footprint: the shader
	/// ignores its geometry and traces rays through the voxels inside it.
	/// </summary>
	private MeshInstance3D _boundingBox;
	private BoxMesh _boxMesh;

	// Toggling voxel demo state (adds/removes the voxel at (0,0,0) once a second)
	private double _toggleTimer;
	private bool _voxelPresent;
	/// <summary>Last segment count printed, so the log only reports changes.</summary>
	private int _lastSegmentCount;

	/// <summary>
	/// Loads the model, builds the GPU resources, creates the bounding box and camera, and hooks
	/// model events so the shader follows edits and bounds changes.
	/// </summary>
	public override void _Ready()
	{
		// Load voxel model
		GD.Print("Loading voxel model...");
		VoxFileModel vox = new(@"..\..\src\Tests\Voxel2Pixel.Test\TestData\Models\Sora.vox");
		_palette = vox.Palette; // 256 RGBA8888 colors

		// Create VoxelBridge to upload to GPU
		GD.Print("Creating VoxelBridge...");
		_model = new SegmentedBrickModel(vox);
		_voxelBridge = new(_model);
		GD.Print($"Active segments: {_voxelBridge.ActiveSegmentCount}");
		GD.Print($"Model bounds: ({_model.SizeX}, {_model.SizeY}, {_model.SizeZ})");

		// Print segment positions to see where voxels are (in voxel Z-up space)
		foreach (VoxelBridge.SegmentEntry entry in _voxelBridge.Directory)
		{
			int voxelX = entry.X * 128;
			int voxelY = entry.Y * 128;
			int voxelZ = entry.Z * 128;
			GD.Print($"  Segment at voxel coords (Z-up): ({voxelX}, {voxelY}, {voxelZ})");
		}

		// Create full-screen quad for raymarching
		SetupRaymarchQuad();

		// Create camera programmatically AFTER we know the model bounds
		SetupCamera();

		// Set up shader uniforms
		SetupShaderUniforms();

		// Subscribe to model changes to update shader when data changes
		_model.OnSegmentLoaded += _ => UpdateShaderSegments();
		_model.OnSegmentUnloaded += _ => UpdateShaderSegments();
		_model.OnBrickDirty += (_, _, _) => UpdateShaderSegments();
		_model.OnBoundsChanged += HandleBoundsChanged;

		GD.Print("Demo ready! Use right-click + WASDQE to fly around.");
		GD.Print("Toggling voxel at (0,0,0) every second to demonstrate real-time editing.");
	}

	/// <summary>
	/// Creates the free-look camera, placed in front of and slightly above the model and aimed at its
	/// centre, at 2.5x the model's largest dimension away so the whole model is in view.
	/// Must run after the model is loaded because it uses the model's size.
	/// </summary>
	private void SetupCamera()
	{
		// Model center in voxel space (Z-up)
		float voxelCenterX = _model.SizeX / 2f;
		float voxelCenterY = _model.SizeY / 2f;
		float voxelCenterZ = _model.SizeZ / 2f;

		// Convert to Godot world space (Y-up): (x, y, z)_zup -> (x, z, y)_yup
		Vector3 modelCenter = new Vector3(voxelCenterX, voxelCenterZ, voxelCenterY);

		// Calculate viewing distance based on model size in Godot space
		float maxDimension = Mathf.Max(_model.SizeX, Mathf.Max(_model.SizeY, _model.SizeZ));
		float viewDistance = maxDimension * 2.5f; // 2.5x the largest dimension

		// Position camera in front and slightly above the model (in Godot Y-up space)
		Vector3 cameraPos = modelCenter + new Vector3(0, maxDimension * 0.5f, viewDistance);

		_camera = new FreeLookCamera
		{
			Position = cameraPos
		};
		AddChild(_camera); // Add to scene tree first
		_camera.LookAt(modelCenter);
		_camera.Enabled = true; // Enable after adding to tree

		GD.Print($"Camera positioned at {cameraPos} looking at {modelCenter}");
	}

	/// <summary>
	/// Creates the bounding box mesh sized and positioned to the model's bounds, assigns the raymarch
	/// shader material and passes the box to the shader. Despite the name this is a box, not a quad.
	/// The BoxMesh is centred on its own origin, so the instance's Position is the box centre in voxel
	/// space, which is why the shader adds box_center to mesh-local coordinates.
	/// </summary>
	private void SetupRaymarchQuad()
	{
		// Get bounds and calculate box dimensions
		SegmentedBrickModel.Bounds? bounds = _model.CurrentBounds;
		(Vector3 center, Vector3 size) = CalculateBoundingBox(bounds);

		GD.Print($"Model bounds: {bounds}");
		GD.Print($"Godot bounds (Y-up): center={center}, size={size}");

		// Create a box mesh sized to the actual voxel model (in Godot Y-up space)
		_boxMesh = new BoxMesh
		{
			Size = size
		};

		_boundingBox = new MeshInstance3D
		{
			Mesh = _boxMesh
		};

		// Load shader
		Shader raymarchShader = GD.Load<Shader>("res://voxel_raymarch.gdshader");
		_raymarchMaterial = new ShaderMaterial
		{
			Shader = raymarchShader
		};

		_boundingBox.MaterialOverride = _raymarchMaterial;

		// Position box at voxel volume center
		_boundingBox.Position = center;
		SetBoxUniforms(center, size);

		// Disable casting shadows (this is a raymarched volume, not geometry)
		_boundingBox.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;

		AddChild(_boundingBox);
		GD.Print($"Created bounding box at {center} with size {size}");
	}

	/// <summary>
	/// Calculates bounding box center and size in Godot world space (Y-up) from model bounds (Z-up).
	/// The max is extended by 2 voxels because bounds are stored at brick granularity (2x2x2 voxels), so
	/// the last brick may contain voxels up to max + 1. Corners are therefore always integer coordinates.
	/// Returns a zero-sized box at the origin if the model has no bounds yet.
	/// </summary>
	private static (Vector3 Center, Vector3 Size) CalculateBoundingBox(SegmentedBrickModel.Bounds? bounds)
	{
		if (!bounds.HasValue)
			return (Vector3.Zero, Vector3.Zero);

		// Voxel data is in Z-up right-handed (MagicaVoxel convention)
		// Godot uses Y-up right-handed
		// Conversion: (X, Y, Z)_zup -> (X, Z, Y)_yup

		SegmentedBrickModel.Bounds b = bounds.Value;

		// Min/max in voxel space (Z-up), +2 to include the brick size
		Vector3 min = new Vector3(b.MinX, b.MinZ, b.MinY);
		Vector3 max = new Vector3(b.MaxX + 2, b.MaxZ + 2, b.MaxY + 2);

		Vector3 center = (min + max) / 2;
		Vector3 size = max - min;

		return (center, size);
	}

	/// <summary>
	/// Handles bounds changes (growth from edits, or shrinkage after GPU trimming) by resizing and
	/// repositioning the raymarching bounding box and updating the shader's copy of it.
	/// </summary>
	private void HandleBoundsChanged(SegmentedBrickModel.Bounds? oldBounds, SegmentedBrickModel.Bounds newBounds)
	{
		if (_boundingBox == null || _boxMesh == null)
			return;

		(Vector3 center, Vector3 size) = CalculateBoundingBox(newBounds);

		// Update the box mesh size and position
		_boxMesh.Size = size;
		_boundingBox.Position = center;
		SetBoxUniforms(center, size);

		GD.Print($"Bounds expanded: {oldBounds} -> {newBounds}");
		GD.Print($"  New bounding box: center={center}, size={size}");
	}

	/// <summary>
	/// Tells the shader the bounding box in voxel space; it intersects rays with this box analytically
	/// to find where each ray enters the volume. Must be called whenever the BoxMesh size or the
	/// instance Position changes, or rays will start in the wrong place.
	/// </summary>
	private void SetBoxUniforms(Vector3 center, Vector3 size)
	{
		_raymarchMaterial.SetShaderParameter("box_center", center);
		_raymarchMaterial.SetShaderParameter("box_size", size);
	}

	/// <summary>
	/// Binds the segment count, directory texture, per-segment brick textures and palette to the shader.
	/// The brick textures are bound as an array in directory order, so the directory's texture index
	/// for a segment is its position in that array. The array holds at most
	/// <see cref="VoxelBridge.MaxSegments"/> textures (a temporary limit that big scenes will need to
	/// replace with a paged atlas; see the note on that constant).
	/// </summary>
	private void SetupShaderUniforms()
	{
		if (_raymarchMaterial == null || _voxelBridge == null)
			return;

		// Set active segment count
		_raymarchMaterial.SetShaderParameter("active_segment_count", _voxelBridge.ActiveSegmentCount);
		GD.Print($"Set active_segment_count: {_voxelBridge.ActiveSegmentCount}");

		// Set segment directory texture
		if (_voxelBridge.DirectoryTexture != null)
		{
			_raymarchMaterial.SetShaderParameter("segment_directory", _voxelBridge.DirectoryTexture);
			GD.Print($"Set directory texture");
		}
		else
		{
			GD.PrintErr("ERROR: Directory texture is null!");
		}

		// Set segment brick textures
		// Build texture array in the same order as the directory entries
		Godot.Collections.Array<Texture3D> textureArray = new Godot.Collections.Array<Texture3D>();
		foreach (VoxelBridge.SegmentEntry entry in _voxelBridge.Directory)
		{
			// Find the texture for this segment
			uint segmentId = ((uint)entry.X << 18) | ((uint)entry.Y << 9) | (uint)entry.Z;
			if (_voxelBridge.Textures.TryGetValue(segmentId, out ImageTexture3D texture))
			{
				textureArray.Add(texture);
				GD.Print($"  Segment {entry.TextureIndex}: ({entry.X}, {entry.Y}, {entry.Z}) -> Texture3D");
			}
			else
			{
				GD.PrintErr($"ERROR: Texture not found for segment ({entry.X}, {entry.Y}, {entry.Z})");
			}
		}

		// Bind the entire texture array to the shader
		_raymarchMaterial.SetShaderParameter("segment_bricks", textureArray);

		GD.Print($"Bound {textureArray.Count} segment brick textures to shader array");

		// Create and bind palette texture
		SetupPaletteTexture();
	}

	/// <summary>
	/// Builds the 256x1 RGBA8 palette texture. The model stores each colour as a 0xRRGGBBAA uint.
	/// Reversing the endianness makes it 0xAABBGGRR, whose little-endian bytes in memory are R, G, B, A,
	/// which is the byte order an RGBA8 image expects; the raw byte copy below relies on that, so the
	/// two steps must be changed together. The shader reads this texture as sRGB (source_color).
	/// </summary>
	private void SetupPaletteTexture()
	{
		// Convert palette with endianness reversal
		// Process whole colors at a time (4 bytes per uint)
		uint[] convertedPalette = new uint[256];

		for (int i = 0; i < 256; i++)
		{
			// Reverse endianness: 0xAABBGGRR -> 0xRRGGBBAA
			convertedPalette[i] = BinaryPrimitives.ReverseEndianness(_palette[i]);
		}

		// Direct memory copy: uint[] -> byte[]
		byte[] paletteData = MemoryMarshal.AsBytes(convertedPalette.AsSpan()).ToArray();

		// Create 256x1 RGBA8 texture
		Image image = Image.CreateFromData(256, 1, false, Image.Format.Rgba8, paletteData);
		_paletteTexture = ImageTexture.CreateFromImage(image);

		// Bind to shader
		_raymarchMaterial.SetShaderParameter("palette", _paletteTexture);

		GD.Print("Palette texture created and bound (256 colors)");
	}

	/// <summary>
	/// Per-frame work: lets the bridge finish any pending GPU bounds trim, refreshes the inverse model
	/// matrix the shader needs, and runs the toggling-voxel editing demo.
	/// </summary>
	public override void _Process(double delta)
	{
		// Poll for async GPU bounds computation results
		_voxelBridge?.Poll();

		// The fragment stage has no inverse of MODEL_MATRIX, so supply it every frame
		_raymarchMaterial?.SetShaderParameter("inv_model_matrix", new Projection(_boundingBox.GlobalTransform.AffineInverse()));

		// Toggle voxel at (0,0,0) every second to demonstrate real-time editing
		_toggleTimer += delta;
		if (_toggleTimer >= 1.0)
		{
			_toggleTimer -= 1.0;
			_voxelPresent = !_voxelPresent;

			// Set or clear the voxel at (0,0,0) with color index 1
			_model[0, 0, 0] = _voxelPresent ? (byte)1 : (byte)0;

			GD.Print($"Voxel at (0,0,0): {(_voxelPresent ? "ADDED" : "REMOVED")}");
		}
	}

	/// <summary>
	/// Updates shader uniforms when segments are added or removed.
	/// Called via OnSegmentLoaded/OnSegmentUnloaded events.
	/// </summary>
	private void UpdateShaderSegments()
	{
		if (_raymarchMaterial == null || _voxelBridge == null)
			return;

		// Update active segment count
		_raymarchMaterial.SetShaderParameter("active_segment_count", _voxelBridge.ActiveSegmentCount);

		// Update directory texture
		if (_voxelBridge.DirectoryTexture != null)
			_raymarchMaterial.SetShaderParameter("segment_directory", _voxelBridge.DirectoryTexture);

		// Rebuild texture array in directory order
		Godot.Collections.Array<Texture3D> textureArray = new Godot.Collections.Array<Texture3D>();
		foreach (VoxelBridge.SegmentEntry entry in _voxelBridge.Directory)
		{
			uint segmentId = ((uint)entry.X << 18) | ((uint)entry.Y << 9) | (uint)entry.Z;
			if (_voxelBridge.Textures.TryGetValue(segmentId, out ImageTexture3D texture))
				textureArray.Add(texture);
		}

		_raymarchMaterial.SetShaderParameter("segment_bricks", textureArray);

		// Only log when segment count changes to avoid spam
		if (_voxelBridge.ActiveSegmentCount != _lastSegmentCount)
		{
			_lastSegmentCount = _voxelBridge.ActiveSegmentCount;
			GD.Print($"Shader updated: {_voxelBridge.ActiveSegmentCount} segments");
		}
	}
}
