using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using BenVoxel.FileToVoxCore;
using BenVoxel.Models;
using BenVoxelEditor;
using Godot;

public partial class Root : Node3D
{
	private VoxelBridge _voxelBridge;
	private ShaderMaterial _raymarchMaterial;
	private FreeLookCamera _camera;
	private ImageTexture _paletteTexture;

	private SegmentedBrickModel _model;
	private uint[] _palette;

	// Bounding box mesh for raymarching
	private MeshInstance3D _boundingBox;
	private BoxMesh _boxMesh;

	// Toggling voxel demo state
	private double _toggleTimer;
	private bool _voxelPresent;
	private int _lastSegmentCount;

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

		// Disable casting shadows (this is a raymarched volume, not geometry)
		_boundingBox.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;

		AddChild(_boundingBox);
		GD.Print($"Created bounding box at {center} with size {size}");
	}

	/// <summary>
	/// Calculates bounding box center and size in Godot world space (Y-up) from model bounds (Z-up).
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
	/// Handles bounds changes by resizing and repositioning the raymarching bounding box.
	/// </summary>
	private void HandleBoundsChanged(SegmentedBrickModel.Bounds? oldBounds, SegmentedBrickModel.Bounds newBounds)
	{
		if (_boundingBox == null || _boxMesh == null)
			return;

		(Vector3 center, Vector3 size) = CalculateBoundingBox(newBounds);

		// Update the box mesh size and position
		_boxMesh.Size = size;
		_boundingBox.Position = center;

		GD.Print($"Bounds expanded: {oldBounds} -> {newBounds}");
		GD.Print($"  New bounding box: center={center}, size={size}");
	}

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

	public override void _Process(double delta)
	{
		// Poll for async GPU bounds computation results
		_voxelBridge?.Poll();

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
