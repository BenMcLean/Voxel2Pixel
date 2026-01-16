using BenVoxel.FileToVoxCore;
using BenVoxel.Models;
using BenVoxelEditor;
using Godot;

public partial class Root : Node3D
{
	private VoxelBridge _voxelBridge;
	private ShaderMaterial _raymarchMaterial;
	private FreeLookCamera _camera;

	private SegmentedBrickModel _model;

	public override void _Ready()
	{
		// Load voxel model
		GD.Print("Loading voxel model...");
		VoxFileModel vox = new(@"..\..\src\Tests\Voxel2Pixel.Test\TestData\Models\Sora.vox");
		uint[] palette = vox.Palette; // 256 RGBA8888 colors

		// Create VoxelBridge to upload to GPU
		GD.Print("Creating VoxelBridge...");
		_model = new SegmentedBrickModel(new SvoModel(vox));
		_voxelBridge = new(_model);
		GD.Print($"Active segments: {_voxelBridge.ActiveSegmentCount}");
		GD.Print($"Model bounds: ({_model.SizeX}, {_model.SizeY}, {_model.SizeZ})");

		// Print segment positions to see where voxels are
		foreach (var entry in _voxelBridge.Directory)
		{
			int worldX = entry.X * 128;
			int worldY = entry.Y * 128;
			int worldZ = entry.Z * 128;
			GD.Print($"  Segment at world coords: ({worldX}, {worldY}, {worldZ})");
		}

		// Create full-screen quad for raymarching
		SetupRaymarchQuad();

		// Create camera programmatically AFTER we know the model bounds
		SetupCamera();

		// Set up shader uniforms
		SetupShaderUniforms();

		GD.Print("Demo ready! Use right-click + WASDQE to fly around.");
	}

	private void SetupCamera()
	{
		// Calculate model center
		Vector3 modelCenter = new Vector3(_model.SizeX / 2f, _model.SizeY / 2f, _model.SizeZ / 2f);

		// Position camera at a distance where we can see the whole model
		// Calculate a good viewing distance based on model size
		float maxDimension = Mathf.Max(_model.SizeX, Mathf.Max(_model.SizeY, _model.SizeZ));
		float viewDistance = maxDimension * 2.5f; // 2.5x the largest dimension

		// Position camera in front and slightly above the model
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
		// Use actual model size instead of segment size
		// The model starts at (0,0,0) and extends to (SizeX, SizeY, SizeZ)
		Vector3 min = new Vector3(0, 0, 0);
		Vector3 max = new Vector3(_model.SizeX, _model.SizeY, _model.SizeZ);

		Vector3 center = (min + max) / 2;
		Vector3 size = max - min;

		GD.Print($"Voxel bounds: min={min}, max={max}, center={center}, size={size}");

		// Create a box mesh sized to the actual voxel model
		BoxMesh boxMesh = new BoxMesh();
		boxMesh.Size = size;

		MeshInstance3D boxInstance = new MeshInstance3D();
		boxInstance.Mesh = boxMesh;

		// Load shader
		Shader raymarchShader = GD.Load<Shader>("res://voxel_raymarch.gdshader");
		_raymarchMaterial = new ShaderMaterial();
		_raymarchMaterial.Shader = raymarchShader;

		boxInstance.MaterialOverride = _raymarchMaterial;

		// Position box at voxel volume center
		boxInstance.Position = center;

		// Disable casting shadows (this is a raymarched volume, not geometry)
		boxInstance.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;

		AddChild(boxInstance);
		GD.Print($"Created bounding box at {center} with size {size}");
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
		var textureArray = new Godot.Collections.Array<Texture3D>();
		foreach (var entry in _voxelBridge.Directory)
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
	}

	public override void _Process(double delta)
	{
		// Could update shader parameters here if needed
	}
}
