using System.Collections.Generic;
using System.Linq;
using BenProgress;
using BenVoxel.FileToVoxCore;
using BenVoxel.Structs;
using Godot;

namespace BenVoxelGpu;

/// <summary>
/// Interactive demo for the Volumetric Ortho-Impostor Rendering System.
/// Controls camera, handles input, and manages the demo environment.
/// Now includes procedural terrain with sprites placed on the surface.
/// </summary>
public partial class Root : Node3D
{
	private Camera3D _camera;
	private VolumetricOrthoSprite _impostor;
	private GpuSvoModelTextureBridge _bridge;
	private Label _perfLabel;
	private Terrain _terrain;

	private float _rotationAngle = 0f,
		_voxelSize = 0.1f,
		_sigma = 4f,
		_cameraDistance = 40f;
	private static readonly IReadOnlyCollection<string> sourceArray = [
		@"..\..\src\Tests\Voxel2Pixel.Test\TestData\Models\Sora.vox",
		@"..\..\src\Tests\Voxel2Pixel.Test\TestData\Models\Tree.vox",
		@"..\..\src\Tests\Voxel2Pixel.Test\TestData\Models\NumberCube.vox" ];
	public override void _Ready()
	{
		// Create terrain first
		_terrain = new Terrain
		{
			TerrainSize = 64,
			TileSize = 1.0f,
			HeightStep = 0.5f,
			MaxHeight = 16,
			NoiseFrequency = 0.03f,
			NoiseOctaves = 4,
			Seed = 42,
		};
		AddChild(_terrain);

		// Load models and create texture bridge
		VoxFileModel[] models = [.. sourceArray.Parallelize(path => new VoxFileModel(path))];
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
		_impostor = new VolumetricOrthoSprite
		{
			CameraTransformProvider = () => _camera.GlobalTransform,
		};
		AddChild(_impostor);

		// Initialize with first model
		Vector3I modelSize = _bridge.GetModelSize(0);
		Point3D anchorPoint = new(modelSize.X >> 1, modelSize.Y >> 1, 0);
		_impostor.Initialize(_bridge, 0, _voxelSize, _sigma, anchorPoint);

		// Position the impostor on the terrain
		PositionImpostorOnTerrain();

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

		// Add directional light for the terrain
		DirectionalLight3D light = new()
		{
			Position = new Vector3(0, 50, 0),
			RotationDegrees = new Vector3(-45, 45, 0),
			ShadowEnabled = true,
		};
		AddChild(light);

		// Add performance overlay
		CanvasLayer overlay = new();
		AddChild(overlay);
		_perfLabel = new Label
		{
			Position = new Vector2(10, 10),
			LabelSettings = new LabelSettings
			{
				FontSize = 64,
				FontColor = Colors.White,
				OutlineSize = 2,
				OutlineColor = Colors.Black,
			},
		};
		overlay.AddChild(_perfLabel);

		UpdateCameraPosition();
	}

	/// <summary>
	/// Positions the impostor at the center of the terrain, on the surface.
	/// </summary>
	private void PositionImpostorOnTerrain()
	{
		Vector3 terrainCenter = _terrain.GetCenter();
		float terrainHeight = _terrain.SampleHeight(terrainCenter.X, terrainCenter.Z);
		_impostor.Position = new Vector3(terrainCenter.X, terrainHeight, terrainCenter.Z);
	}

	private void UpdateCameraPosition()
	{
		// Position camera to look at the terrain center
		Vector3 terrainCenter = _terrain.GetCenter();
		_camera.Position = new Vector3(
			x: terrainCenter.X + Mathf.Sin(_rotationAngle) * _cameraDistance,
			y: terrainCenter.Y + _cameraDistance * 0.5f,
			z: terrainCenter.Z + Mathf.Cos(_rotationAngle) * _cameraDistance);
		_camera.LookAt(terrainCenter, Vector3.Up);
	}

	public override void _Process(double delta)
	{
		// Left/Right: Adjust sigma (virtual pixels per voxel)
		if (Input.IsActionJustPressed("ui_left"))
		{
			_sigma = Mathf.Max(1f, _sigma - 1f);
			_impostor.UpdateSizing(_voxelSize, _sigma);
			GD.Print($"Sigma: {_sigma}, Voxel Size: {_voxelSize}");
		}
		if (Input.IsActionJustPressed("ui_right"))
		{
			_sigma = Mathf.Min(16f, _sigma + 1f);
			_impostor.UpdateSizing(_voxelSize, _sigma);
			GD.Print($"Sigma: {_sigma}, Voxel Size: {_voxelSize}");
		}

		// Up/Down: Adjust voxel size (zoom)
		if (Input.IsActionJustPressed("ui_up"))
		{
			_voxelSize = Mathf.Min(1f, _voxelSize * 1.25f);
			_impostor.UpdateSizing(_voxelSize, _sigma);
			GD.Print($"Sigma: {_sigma}, Voxel Size: {_voxelSize}");
		}
		if (Input.IsActionJustPressed("ui_down"))
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
			PositionImpostorOnTerrain();
		}

		// Tab: Regenerate terrain with new seed
		if (Input.IsActionJustPressed("ui_focus_next"))
		{
			_terrain.Randomize();
			PositionImpostorOnTerrain();
			GD.Print($"Terrain regenerated with seed: {_terrain.Seed}");
		}

		// Rotate camera around model
		_rotationAngle += (float)delta * 0.5f;
		UpdateCameraPosition();

		// Update performance overlay
		_perfLabel.Text = $"FPS: {Engine.GetFramesPerSecond()}\n"
			+ $"Frame Time: {Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000:F1} ms\n"
			+ $"Draw Calls: {Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame)}\n"
			+ $"VRAM: {Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed) / (1024 * 1024):F1} MB\n"
			+ $"RAM: {Performance.GetMonitor(Performance.Monitor.MemoryStatic) / (1024 * 1024):F1} MB\n"
			+ $"Objects: {Performance.GetMonitor(Performance.Monitor.ObjectCount)}\n"
			+ $"Sigma: {_sigma}  Voxel Size: {_voxelSize:F3}\n"
			+ $"Terrain: {_terrain.TerrainSize}x{_terrain.TerrainSize} (Tab to regenerate)";
	}
}
