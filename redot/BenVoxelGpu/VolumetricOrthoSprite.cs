using System;
using System.IO;
using BenVoxel.Structs;
using Godot;

namespace BenVoxelGpu;

/// <summary>
/// Container node for volumetric ortho-sprite entities.
/// Manages proxy quad sizing, anchor point offset, billboard orientation, and shader parameters.
///
/// Entity Management Pattern:
/// <code>
/// VolumetricOrthoSprite (Entity root - position/rotation set here)
/// └── MeshInstance3D (Proxy QuadMesh - billboard-oriented each frame, centered on model center)
/// </code>
/// </summary>
public partial class VolumetricOrthoSprite : Node3D
{
	#region Data
	private MeshInstance3D _proxyBox;
	private ShaderMaterial _material;
	private GpuSvoModelTextureBridge _bridge;
	private int _modelIndex;
	private Vector3I _modelSize;
	private float _voxelSize;
	private float _sigma;
	private Vector3 _modelCenterOffset;
	private float _deltaPxWorld;

	/// <summary>
	/// Delegate for providing camera transform to an sprite.
	/// </summary>
	/// <returns>The camera's global transform.</returns>
	public delegate Transform3D CameraTransformProviderDelegate();

	/// <summary>
	/// Callback that provides camera transform each frame.
	/// Must be set before the sprite can render correctly.
	/// </summary>
	public CameraTransformProviderDelegate CameraTransformProvider { get; set; }

	/// <summary>
	/// Light direction in world space (Godot Y-up, normalized).
	/// Default is from upper-right relative to camera view.
	/// Set to null to use automatic camera-relative lighting.
	/// </summary>
	public Vector3? LightDirection { get; set; } = null;

	/// <summary>
	/// The anchor point in voxel space (Z-up).
	/// Default is bottom-center: (sizeX/2, sizeY/2, 0).
	/// </summary>
	public Point3D AnchorPoint { get; private set; }

	/// <summary>
	/// The proxy quad MeshInstance3D child.
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
	public Vector3 ModelCenterWorld => GlobalTransform * _modelCenterOffset;
	#endregion Data
	public VolumetricOrthoSprite() => _proxyBox = new MeshInstance3D();
	public override void _Ready() => AddChild(_proxyBox);
	public override void _Process(double delta)
	{
		if (_material is null || CameraTransformProvider is null)
			return;
		Transform3D camTransform = CameraTransformProvider();
		Vector3 camPos = camTransform.Origin,
			camForward = -camTransform.Basis.Z.Normalized(),
			camRight = camTransform.Basis.X.Normalized(),
			camUp = camTransform.Basis.Y.Normalized();
		// Transform from Godot Y-up to voxel Z-up space
		// Godot: X=right, Y=up, Z=towards viewer
		// Voxel: X=right, Y=forward (into screen), Z=up
		// Transformation: voxel.X = godot.X, voxel.Y = -godot.Z, voxel.Z = godot.Y
		_material.SetShaderParameter("ray_dir_local", GodotToVoxel(camForward).Normalized());
		_material.SetShaderParameter("camera_up_local", GodotToVoxel(camUp).Normalized());
		_material.SetShaderParameter("light_dir", GodotToVoxel(LightDirection
			?? (camRight + camUp * 0.5f - camForward).Normalized()).Normalized());
		_material.SetShaderParameter("camera_distance", (camPos - ModelCenterWorld).Length());
		// Project model AABB extents onto camera right and up to get tight quad dimensions.
		// For an AABB with sizes (sX, sY, sZ) in voxel space, the extent along a direction d is:
		//   extent = |d.x| * sX + |d.y| * sY + |d.z| * sZ
		// We work in voxel space (Z-up) for the projection, then convert to world units.
		Vector3 voxelRight = GodotToVoxel(camRight),
			voxelUp = GodotToVoxel(camUp);
		float sX = _modelSize.X, sY = _modelSize.Y, sZ = _modelSize.Z,
			quadWidthVoxel = Mathf.Abs(voxelRight.X) * sX + Mathf.Abs(voxelRight.Y) * sY + Mathf.Abs(voxelRight.Z) * sZ,
			quadHeightVoxel = Mathf.Abs(voxelUp.X) * sX + Mathf.Abs(voxelUp.Y) * sY + Mathf.Abs(voxelUp.Z) * sZ,
			quadWidth = quadWidthVoxel * _voxelSize + 2f * _deltaPxWorld,
			quadHeight = quadHeightVoxel * _voxelSize + 2f * _deltaPxWorld;
		// Set billboard transform: quad centered on model center in world space
		Vector3 modelCenterWorld = ModelCenterWorld;
		_proxyBox.GlobalTransform = new Transform3D(
			new Basis(
				camRight * quadWidth,
				camUp * quadHeight,
				-camForward),
			modelCenterWorld);
	}
	/// <summary>
	/// Converts a vector from Godot space (Y-up) to voxel space (Z-up).
	/// </summary>
	private static Vector3 GodotToVoxel(Vector3 godot) =>
		new(godot.X, -godot.Z, godot.Y);
	/// <summary>
	/// Converts a vector from voxel space (Z-up) to Godot space (Y-up).
	/// </summary>
	private static Vector3 VoxelToGodot(Vector3 voxel) =>
		new(voxel.X, voxel.Z, -voxel.Y);
	/// <summary>
	/// Initialize the sprite with a texture bridge and model index.
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
		if (_material is null)
			bridge.BindToMaterial(_material = new ShaderMaterial
			{
				Shader = new Shader { Code = File.ReadAllText("volumetric_ortho_sprite.gdshader"), },
			});
		bridge.BindModelToMaterial(_material, modelIndex);
		// Virtual pixel size in world units (stored for per-frame quad sizing)
		_deltaPxWorld = voxelSize / sigma;
		// Unit quad; actual size is set per-frame via billboard transform in _Process
		_proxyBox.Mesh = new QuadMesh { Size = new Vector2(1, 1) };
		_proxyBox.MaterialOverride = _material;
		// Compute anchor-to-model-center offset in the entity's local space (Godot Y-up)
		Vector3 modelCenter = new Vector3(_modelSize.X, _modelSize.Y, _modelSize.Z) * 0.5f,
			anchorToCenter = modelCenter - new Vector3(AnchorPoint.X, AnchorPoint.Y, AnchorPoint.Z),
			anchorOffsetGodot = VoxelToGodot(anchorToCenter) * voxelSize;
		// Ground clearance: offset up by one virtual pixel
		float groundClearance = _deltaPxWorld;
		_modelCenterOffset = anchorOffsetGodot + new Vector3(0, groundClearance, 0);
		// Update shader uniforms for sizing
		_material.SetShaderParameter("voxel_size", voxelSize);
		_material.SetShaderParameter("sigma", sigma);
		_material.SetShaderParameter("anchor_point", new Vector3(AnchorPoint.X, AnchorPoint.Y, AnchorPoint.Z));
	}
	/// <summary>
	/// Switch to a different model from the same texture bridge.
	/// </summary>
	public void SetModel(int modelIndex, Point3D? anchorPoint = null)
	{
		if (_bridge is null)
			throw new InvalidOperationException("VolumetricOrthoSprite must be initialized before switching models");
		Initialize(_bridge, modelIndex, _voxelSize, _sigma, anchorPoint);
	}
	/// <summary>
	/// Updates the sprite when voxel size or sigma changes.
	/// </summary>
	public void UpdateSizing(float voxelSize, float sigma)
	{
		if (_bridge is null)
			throw new InvalidOperationException("VolumetricOrthoSprite must be initialized before updating sizing");
		Initialize(_bridge, _modelIndex, voxelSize, sigma, AnchorPoint);
	}
	/// <summary>
	/// Sets a custom anchor point and reinitializes the sprite.
	/// </summary>
	public void SetAnchorPoint(Point3D anchorPoint)
	{
		if (_bridge is null)
			throw new InvalidOperationException("VolumetricOrthoSprite must be initialized before setting anchor");
		Initialize(_bridge, _modelIndex, _voxelSize, _sigma, anchorPoint);
	}
}
