using System.Linq;
using BenVoxel.FileToVoxCore;
using BenVoxel.Structs;
using Godot;

namespace BenVoxelGpu;

/// <summary>
/// Interactive demo for the Volumetric Ortho-Impostor Rendering System.
/// Controls camera, handles input, and manages the demo environment.
/// </summary>
public partial class Root : Node3D
{
	private Camera3D _camera;
	private VolumetricOrthoImpostor _impostor;
	private GpuSvoModelTextureBridge _bridge;

	private float _rotationAngle = 0f,
		_voxelSize = 0.1f,
		_sigma = 4f,
		_cameraDistance = 20f;

	public override void _Ready()
	{
		// Load models and create texture bridge
		VoxFileModel[] models = [
			new(@"..\..\src\Tests\Voxel2Pixel.Test\TestData\Models\Sora.vox"),
			new(@"..\..\src\Tests\Voxel2Pixel.Test\TestData\Models\Tree.vox"),
			new(@"..\..\src\Tests\Voxel2Pixel.Test\TestData\Models\NumberCube.vox")];
		_bridge = new GpuSvoModelTextureBridge(
			models: models,
			palettes: [.. models.Select(model => model.Palette)]);

		// Create perspective camera
		_camera = new Camera3D
		{
			Projection = Camera3D.ProjectionType.Perspective,
			Fov = 45f,
			Current = true,
		};
		AddChild(_camera);

		// Create impostor with camera transform callback
		_impostor = new VolumetricOrthoImpostor
		{
			CameraTransformProvider = () => _camera.GlobalTransform,
		};
		AddChild(_impostor);

		// Initialize with first model
		Vector3I modelSize = _bridge.GetModelSize(0);
		Point3D anchorPoint = new(modelSize.X >> 1, modelSize.Y >> 1, 0);
		_impostor.Initialize(_bridge, 0, _voxelSize, _sigma, anchorPoint);

		// Add environment for background
		WorldEnvironment env = new()
		{
			Environment = new Godot.Environment()
			{
				BackgroundMode = Godot.Environment.BGMode.Color,
				BackgroundColor = Godot.Colors.DarkSlateGray,
			}
		};
		AddChild(env);

		UpdateCameraPosition();
	}

	private void UpdateCameraPosition()
	{
		// Position camera to look at the model center
		Vector3 modelCenterWorld = _impostor.ModelCenterWorld;
		_camera.Position = new Vector3(
			x: modelCenterWorld.X + Mathf.Sin(_rotationAngle) * _cameraDistance,
			y: modelCenterWorld.Y + _cameraDistance * 0.5f,
			z: modelCenterWorld.Z + Mathf.Cos(_rotationAngle) * _cameraDistance);
		_camera.LookAt(modelCenterWorld, Vector3.Up);
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
		{
			int nextModel = (_impostor.ModelIndex + 1) % _bridge.ModelCount;
			Vector3I modelSize = _bridge.GetModelSize(nextModel);
			Point3D anchorPoint = new(modelSize.X >> 1, modelSize.Y >> 1, 0);
			_impostor.SetModel(nextModel, anchorPoint);
		}

		// Rotate camera around model
		_rotationAngle += (float)delta * 0.5f;
		UpdateCameraPosition();
	}
}
