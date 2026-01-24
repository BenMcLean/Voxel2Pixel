using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenProgress;
using BenVoxel.FileToVoxCore;
using BenVoxel.Models;
using BenVoxel.Structs;
using Godot;

namespace BenVoxelGpu;

/// <summary>
/// Volumetric Ortho-Impostor Rendering System Demo
/// Renders voxel models as sprite-like entities in a 3D perspective world
/// with orthographic internal projection and 1-pixel silhouette outline.
/// </summary>
public partial class Root : Node3D
{
	public static readonly string SpatialShader = File.ReadAllText("volumetric_ortho_impostor.gdshader");
	private Camera3D _camera;
	private VolumetricOrthoImpostor _impostor;
	private ImageTexture _svoTexture;
	private ShaderMaterial _material;
	private int _textureWidth;
	private float _rotationAngle = 0f;
	// Multi-model state
	private IReadOnlyList<SvoModelDescriptor> _descriptors;
	private ImageTexture[] _paletteTextures;
	private int _currentModelIndex = 0;
	private int _totalNodesCount;
	// Volumetric impostor parameters
	private float _voxelSize = 0.1f;     // World-space size of one voxel (meters)
	private float _sigma = 4f;           // Virtual pixels per voxel
	private float _cameraDistance = 20f; // Distance from camera to anchor point
	public override void _Ready()
	{
		// Load models and create SVO texture
		VoxFileModel[] vox = [
			new(@"..\..\src\Tests\Voxel2Pixel.Test\TestData\Models\Sora.vox"),
			new(@"..\..\src\Tests\Voxel2Pixel.Test\TestData\Models\Tree.vox"),
			new(@"..\..\src\Tests\Voxel2Pixel.Test\TestData\Models\NumberCube.vox")];
		GpuSvoModel[] gpuModel = [.. vox.Parallelize(vox => new GpuSvoModel(vox))];
		uint[][] palettes = [.. vox.Select(vox => vox.Palette)];
		GpuSvoModelTexture modelTexture = new(gpuModel);
		_descriptors = modelTexture.Descriptors;
		_totalNodesCount = modelTexture.TotalNodesCount;
		_textureWidth = modelTexture.Width;
		// Create ImageTexture from the model texture data
		Image image = Image.CreateFromData(_textureWidth, _textureWidth, false, Image.Format.Rgba8, modelTexture.Data);
		_svoTexture = ImageTexture.CreateFromImage(image);
		// Create palette textures for all models
		_paletteTextures = new ImageTexture[palettes.Length];
		for (int m = 0; m < palettes.Length; m++)
		{
			byte[] paletteBytes = new byte[256 << 2];
			for (int i = 0; i < 256; i++)
				BinaryPrimitives.WriteUInt32BigEndian(paletteBytes.AsSpan(i << 2, 4), palettes[m][i]);
			Image paletteImage = Image.CreateFromData(256, 1, false, Image.Format.Rgba8, paletteBytes);
			_paletteTextures[m] = ImageTexture.CreateFromImage(paletteImage);
		}
		// Create shader material
		_material = new ShaderMaterial
		{
			Shader = new Shader { Code = SpatialShader },
		};
		_material.SetShaderParameter("svo_texture", _svoTexture);
		_material.SetShaderParameter("texture_width", _textureWidth);
		_material.SetShaderParameter("svo_nodes_count", (uint)_totalNodesCount);
		// Create volumetric ortho-impostor container
		_impostor = new VolumetricOrthoImpostor();
		AddChild(_impostor);
		// Create perspective camera
		_camera = new Camera3D
		{
			Projection = Camera3D.ProjectionType.Perspective,
			Fov = 45f,
			Current = true,
		};
		AddChild(_camera);
		// Add environment for background
		WorldEnvironment env = new()
		{
			Environment = new Godot.Environment()
			{
				BackgroundMode = Godot.Environment.BGMode.Color,
				BackgroundColor = new Color(0.1f, 0.1f, 0.15f),
			}
		};
		AddChild(env);
		// Initialize first model
		SwitchToModel(0);
		UpdateCamera();
	}
	private void SwitchToModel(int index)
	{
		_currentModelIndex = index;
		SvoModelDescriptor descriptor = _descriptors[index];
		Vector3I modelSize = new Vector3I(descriptor.SizeX, descriptor.SizeY, descriptor.SizeZ);
		// Update SVO uniforms
		_material.SetShaderParameter("palette_texture", _paletteTextures[index]);
		_material.SetShaderParameter("svo_model_size", modelSize);
		_material.SetShaderParameter("svo_max_depth", (uint)descriptor.MaxDepth);
		_material.SetShaderParameter("node_offset", descriptor.NodeOffset);
		_material.SetShaderParameter("payload_offset", descriptor.PayloadOffset);
		// Initialize the impostor with the new model
		// Anchor point at bottom center (integer division)
		Point3D anchorPoint = new(modelSize.X >> 1, modelSize.Y >> 1, 0);
		_impostor.Initialize(modelSize, _voxelSize, _sigma, _material, anchorPoint);
		GD.Print($"Switched to model {index}: {descriptor.SizeX}x{descriptor.SizeY}x{descriptor.SizeZ}");
	}
	private void UpdateCamera()
	{
		// Model center in world space = impostor position + proxy box offset
		// (ProxyBox.Position includes anchor_to_center offset + ground clearance)
		Vector3 modelCenterWorld = _impostor.GlobalPosition + _impostor.ProxyBox.Position;
		// Position camera to look at the model center
		// Rotate around Y axis (Godot up) for turntable effect
		float camX = modelCenterWorld.X + Mathf.Sin(_rotationAngle) * _cameraDistance;
		float camZ = modelCenterWorld.Z + Mathf.Cos(_rotationAngle) * _cameraDistance;
		float camY = modelCenterWorld.Y + _cameraDistance * 0.5f; // Slight elevation
		_camera.Position = new Vector3(camX, camY, camZ);
		// Look at the model center
		_camera.LookAt(modelCenterWorld, Vector3.Up);
		// Compute camera vectors
		Transform3D camTransform = _camera.GlobalTransform;
		Vector3 camPos = camTransform.Origin;
		Vector3 camForward = -camTransform.Basis.Z.Normalized(); // Camera looks down -Z
		Vector3 camRight = camTransform.Basis.X.Normalized();
		Vector3 camUp = camTransform.Basis.Y.Normalized();
		// Transform from Godot Y-up to voxel Z-up space
		// Godot: X=right, Y=up, Z=towards viewer (camera looks at -Z)
		// Voxel: X=right, Y=forward (into screen), Z=up
		// Transformation: voxel.X = godot.X, voxel.Y = -godot.Z, voxel.Z = godot.Y
		Vector3 camForwardVoxel = new Vector3(camForward.X, -camForward.Z, camForward.Y).Normalized();
		Vector3 camUpVoxel = new Vector3(camUp.X, -camUp.Z, camUp.Y).Normalized();
		// Light direction (from upper left in view space)
		Vector3 lightDirWorld = (-camRight + camUp * 0.5f - camForward).Normalized();
		Vector3 lightDirVoxel = new Vector3(lightDirWorld.X, -lightDirWorld.Z, lightDirWorld.Y).Normalized();
		// Compute camera distance to model center (for screen-to-sprite mapping)
		float cameraDistanceToCenter = (camPos - modelCenterWorld).Length();
		// Update shader uniforms (camera_right derived in shader from cross product)
		_material.SetShaderParameter("ray_dir_local", camForwardVoxel);
		_material.SetShaderParameter("camera_up_local", camUpVoxel);
		_material.SetShaderParameter("light_dir", lightDirVoxel);
		_material.SetShaderParameter("camera_distance", cameraDistanceToCenter);
	}
	public override void _Process(double delta)
	{
		// Up/Down: Adjust sigma (virtual pixels per voxel)
		if (Input.IsActionJustPressed("ui_up"))
		{
			_sigma = Mathf.Min(16f, _sigma + 1f);
			_impostor.UpdateSizing(_voxelSize, _sigma);
			GD.Print($"Sigma: {_sigma}, Voxel Size: {_voxelSize}");
		}
		if (Input.IsActionJustPressed("ui_down"))
		{
			_sigma = Mathf.Max(1f, _sigma - 1f);
			_impostor.UpdateSizing(_voxelSize, _sigma);
			GD.Print($"Sigma: {_sigma}, Voxel Size: {_voxelSize}");
		}
		// Left/Right: Adjust voxel size (zoom)
		if (Input.IsActionJustPressed("ui_right"))
		{
			_voxelSize = Mathf.Min(1f, _voxelSize * 1.25f);
			_impostor.UpdateSizing(_voxelSize, _sigma);
			GD.Print($"Sigma: {_sigma}, Voxel Size: {_voxelSize}");
		}
		if (Input.IsActionJustPressed("ui_left"))
		{
			_voxelSize = Mathf.Max(0.01f, _voxelSize / 1.25f);
			_impostor.UpdateSizing(_voxelSize, _sigma);
			GD.Print($"Sigma: {_sigma}, Voxel Size: {_voxelSize}");
		}
		// Space: Cycle to next model
		if (Input.IsActionJustPressed("ui_accept"))
			SwitchToModel((_currentModelIndex + 1) % _descriptors.Count);
		// Rotate camera around model
		_rotationAngle += (float)delta * 0.5f;
		UpdateCamera();
	}
}
