using System;
using System.IO;
using BenVoxel.Structs;
using Godot;

namespace BenVoxelGpu;

/// <summary>
/// Camera information needed for volumetric ortho-impostor rendering.
/// All vectors should be in voxel space (Z-up).
/// </summary>
public struct ImpostorCameraInfo
{
	/// <summary>Camera forward direction in voxel space (normalized).</summary>
	public Vector3 ForwardVoxel;
	/// <summary>Camera up direction in voxel space (normalized).</summary>
	public Vector3 UpVoxel;
	/// <summary>Light direction in voxel space (normalized).</summary>
	public Vector3 LightDirVoxel;
	/// <summary>Distance from camera to the impostor's model center in world units.</summary>
	public float CameraDistance;
}

/// <summary>
/// Delegate for providing camera information to an impostor.
/// </summary>
/// <param name="modelCenterWorld">The model center position in world space.</param>
/// <returns>Camera information for rendering.</returns>
public delegate ImpostorCameraInfo CameraInfoProvider(Vector3 modelCenterWorld);

/// <summary>
/// Container node for volumetric ortho-impostor entities.
/// Manages proxy box sizing, anchor point offset, and shader parameters.
///
/// Entity Management Pattern:
/// <code>
/// VolumetricOrthoImpostor (Entity root - position/rotation set here)
/// └── MeshInstance3D (Proxy BoxMesh - offset internally for anchor + ground clearance)
/// </code>
/// </summary>
public partial class VolumetricOrthoImpostor : Node3D
{
	private MeshInstance3D _proxyBox;
	private ShaderMaterial _material;
	private GpuSvoModelTextureBridge _bridge;
	private int _modelIndex;
	private Vector3I _modelSize;
	private float _voxelSize;
	private float _sigma;

	/// <summary>
	/// Callback that provides camera information each frame.
	/// Must be set before the impostor can render correctly.
	/// </summary>
	public CameraInfoProvider CameraInfoProvider { get; set; }

	/// <summary>
	/// The anchor point in voxel space (Z-up).
	/// Default is bottom-center: (sizeX/2, sizeY/2, 0).
	/// </summary>
	public Point3D AnchorPoint { get; private set; }

	/// <summary>
	/// The proxy box MeshInstance3D child.
	/// </summary>
	public MeshInstance3D ProxyBox => _proxyBox;

	/// <summary>
	/// Current voxel size (world-space size of one voxel).
	/// </summary>
	public float VoxelSize => _voxelSize;

	/// <summary>
	/// Current sigma (virtual pixels per voxel).
	/// </summary>
	public float Sigma => _sigma;

	/// <summary>
	/// Current model size in voxels.
	/// </summary>
	public Vector3I ModelSize => _modelSize;

	/// <summary>
	/// Current model index in the texture bridge.
	/// </summary>
	public int ModelIndex => _modelIndex;

	/// <summary>
	/// Model center position in world space.
	/// </summary>
	public Vector3 ModelCenterWorld => GlobalPosition + _proxyBox.Position;

	public VolumetricOrthoImpostor()
	{
		_proxyBox = new MeshInstance3D();
	}

	public override void _Ready()
	{
		AddChild(_proxyBox);
	}

	public override void _Process(double delta)
	{
		// Update camera-dependent shader parameters each frame
		if (_material != null && CameraInfoProvider != null)
		{
			ImpostorCameraInfo cameraInfo = CameraInfoProvider(ModelCenterWorld);
			_material.SetShaderParameter("ray_dir_local", cameraInfo.ForwardVoxel);
			_material.SetShaderParameter("camera_up_local", cameraInfo.UpVoxel);
			_material.SetShaderParameter("light_dir", cameraInfo.LightDirVoxel);
			_material.SetShaderParameter("camera_distance", cameraInfo.CameraDistance);
		}
	}

	/// <summary>
	/// Initialize the impostor with a texture bridge and model index.
	/// </summary>
	/// <param name="bridge">The texture bridge containing SVO data</param>
	/// <param name="modelIndex">Index of the model to display</param>
	/// <param name="voxelSize">World-space size of one voxel (meters)</param>
	/// <param name="sigma">Virtual pixels per voxel</param>
	/// <param name="anchorPoint">Optional custom anchor point in voxel space. If null, uses bottom-center.</param>
	public void Initialize(GpuSvoModelTextureBridge bridge, int modelIndex, float voxelSize, float sigma, Point3D? anchorPoint = null)
	{
		_bridge = bridge;
		_modelIndex = modelIndex;
		_modelSize = bridge.GetModelSize(modelIndex);
		_voxelSize = voxelSize;
		_sigma = sigma;

		// Compute anchor point (default: bottom-center in Z-up voxel space)
		AnchorPoint = anchorPoint ?? new Point3D(_modelSize.X >> 1, _modelSize.Y >> 1, 0);

		// Create shader material if needed
		if (_material == null)
		{
			_material = new ShaderMaterial
			{
				Shader = new Shader { Code = File.ReadAllText("volumetric_ortho_impostor.gdshader"), },
			};
			// Bind shared texture data
			bridge.BindToMaterial(_material);
		}

		// Bind model-specific data
		bridge.BindModelToMaterial(_material, modelIndex);

		// Compute box size using the 3D diagonal for worst-case orthographic projection
		float diagonal = Mathf.Sqrt(
			_modelSize.X * _modelSize.X +
			_modelSize.Y * _modelSize.Y +
			_modelSize.Z * _modelSize.Z);

		// Virtual pixel size in world units
		float deltaPxWorld = voxelSize / sigma;

		// Box side length: cube that contains orthographic projection from any angle + outline margin
		float boxSide = diagonal * voxelSize + 2f * deltaPxWorld;

		// Create cube mesh (same size on all axes)
		_proxyBox.Mesh = new BoxMesh { Size = new Vector3(boxSide, boxSide, boxSide) };
		_proxyBox.MaterialOverride = _material;

		// Offset the proxy box for anchor alignment and ground clearance
		Vector3 modelCenter = new Vector3(_modelSize.X, _modelSize.Y, _modelSize.Z) * 0.5f;
		Vector3 anchorToCenter = modelCenter - new Vector3(AnchorPoint.X, AnchorPoint.Y, AnchorPoint.Z);
		Vector3 anchorOffsetGodot = new Vector3(
			anchorToCenter.X,
			anchorToCenter.Z,
			-anchorToCenter.Y) * voxelSize;

		// Ground clearance: offset up by one virtual pixel
		float groundClearance = deltaPxWorld;

		// Total offset for the proxy box
		_proxyBox.Position = anchorOffsetGodot + new Vector3(0, groundClearance, 0);

		// Update shader uniforms for sizing
		_material.SetShaderParameter("voxel_size", voxelSize);
		_material.SetShaderParameter("sigma", sigma);
		_material.SetShaderParameter("anchor_point", new Vector3(AnchorPoint.X, AnchorPoint.Y, AnchorPoint.Z));

		GD.Print($"VolumetricOrthoImpostor: model {modelIndex}, size {_modelSize}, box_side={boxSide:F3}, anchor={AnchorPoint}");
	}

	/// <summary>
	/// Switch to a different model from the same texture bridge.
	/// </summary>
	public void SetModel(int modelIndex, Point3D? anchorPoint = null)
	{
		if (_bridge == null)
			throw new InvalidOperationException("Impostor must be initialized before switching models");
		Initialize(_bridge, modelIndex, _voxelSize, _sigma, anchorPoint);
	}

	/// <summary>
	/// Updates the impostor when voxel size or sigma changes.
	/// </summary>
	public void UpdateSizing(float voxelSize, float sigma)
	{
		if (_bridge == null)
			throw new InvalidOperationException("Impostor must be initialized before updating sizing");
		Initialize(_bridge, _modelIndex, voxelSize, sigma, AnchorPoint);
	}

	/// <summary>
	/// Sets a custom anchor point and reinitializes the impostor.
	/// </summary>
	public void SetAnchorPoint(Point3D anchorPoint)
	{
		if (_bridge == null)
			throw new InvalidOperationException("Impostor must be initialized before setting anchor");
		Initialize(_bridge, _modelIndex, _voxelSize, _sigma, anchorPoint);
	}
}
