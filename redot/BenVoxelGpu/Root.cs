using System.IO;
using System.Linq;
using BenProgress;
using BenVoxel.FileToVoxCore;
using BenVoxel.Models;
using BenVoxel.Structs;
using Godot;

namespace BenVoxelGpu;

/// <summary>
/// Interactive demo for the Volumetric Ortho-Impostor Rendering System.
/// Controls camera, handles input, and manages the demo environment.
/// </summary>
public partial class Root : Node3D
{
	public static readonly string SpatialShader = File.ReadAllText("volumetric_ortho_impostor.gdshader");

	private Camera3D _camera;
	private VolumetricOrthoImpostor _impostor;
	private GpuSvoModelTextureBridge _bridge;

	private float _rotationAngle = 0f;
	private float _voxelSize = 0.1f;
	private float _sigma = 4f;
	private float _cameraDistance = 20f;

	public override void _Ready()
	{
		// Load models and create texture bridge
		VoxFileModel[] vox = [
			new(@"..\..\src\Tests\Voxel2Pixel.Test\TestData\Models\Sora.vox"),
			new(@"..\..\src\Tests\Voxel2Pixel.Test\TestData\Models\Tree.vox"),
			new(@"..\..\src\Tests\Voxel2Pixel.Test\TestData\Models\NumberCube.vox")];
		GpuSvoModel[] gpuModels = [.. vox.Parallelize(v => new GpuSvoModel(v))];
		uint[][] palettes = [.. vox.Select(v => v.Palette)];

		_bridge = new GpuSvoModelTextureBridge(gpuModels, palettes);

		// Create impostor and set up camera info callback
		_impostor = new VolumetricOrthoImpostor
		{
			CameraInfoProvider = GetCameraInfo
		};
		AddChild(_impostor);

		// Initialize with first model
		Vector3I modelSize = _bridge.GetModelSize(0);
		Point3D anchorPoint = new(modelSize.X >> 1, modelSize.Y >> 1, 0);
		_impostor.Initialize(_bridge, 0, _voxelSize, _sigma, anchorPoint);

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

		UpdateCameraPosition();
	}

	/// <summary>
	/// Provides camera information to the impostor for shader updates.
	/// </summary>
	private ImpostorCameraInfo GetCameraInfo(Vector3 modelCenterWorld)
	{
		Transform3D camTransform = _camera.GlobalTransform;
		Vector3 camPos = camTransform.Origin;
		Vector3 camForward = -camTransform.Basis.Z.Normalized();
		Vector3 camRight = camTransform.Basis.X.Normalized();
		Vector3 camUp = camTransform.Basis.Y.Normalized();

		// Transform from Godot Y-up to voxel Z-up space
		// Godot: X=right, Y=up, Z=towards viewer
		// Voxel: X=right, Y=forward (into screen), Z=up
		// Transformation: voxel.X = godot.X, voxel.Y = -godot.Z, voxel.Z = godot.Y
		Vector3 forwardVoxel = new Vector3(camForward.X, -camForward.Z, camForward.Y).Normalized();
		Vector3 upVoxel = new Vector3(camUp.X, -camUp.Z, camUp.Y).Normalized();

		// Light direction (from upper left in view space)
		Vector3 lightDirWorld = (-camRight + camUp * 0.5f - camForward).Normalized();
		Vector3 lightDirVoxel = new Vector3(lightDirWorld.X, -lightDirWorld.Z, lightDirWorld.Y).Normalized();

		return new ImpostorCameraInfo
		{
			ForwardVoxel = forwardVoxel,
			UpVoxel = upVoxel,
			LightDirVoxel = lightDirVoxel,
			CameraDistance = (camPos - modelCenterWorld).Length()
		};
	}

	private void UpdateCameraPosition()
	{
		// Position camera to look at the model center
		Vector3 modelCenterWorld = _impostor.ModelCenterWorld;

		float camX = modelCenterWorld.X + Mathf.Sin(_rotationAngle) * _cameraDistance;
		float camZ = modelCenterWorld.Z + Mathf.Cos(_rotationAngle) * _cameraDistance;
		float camY = modelCenterWorld.Y + _cameraDistance * 0.5f;

		_camera.Position = new Vector3(camX, camY, camZ);
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
