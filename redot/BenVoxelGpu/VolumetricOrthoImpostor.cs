using BenVoxel.Structs;
using Godot;

namespace BenVoxelGpu;

/// <summary>
/// Container node for volumetric ortho-impostor entities.
/// Manages proxy box sizing, anchor point offset, and ground clearance.
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
	private Vector3I _modelSize;
	private float _voxelSize;
	private float _sigma;

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

	public VolumetricOrthoImpostor()
	{
		_proxyBox = new MeshInstance3D();
	}

	public override void _Ready()
	{
		AddChild(_proxyBox);
	}

	/// <summary>
	/// Initialize the impostor with model parameters.
	/// </summary>
	/// <param name="modelSize">Size of the voxel model in voxels</param>
	/// <param name="voxelSize">World-space size of one voxel (meters)</param>
	/// <param name="sigma">Virtual pixels per voxel</param>
	/// <param name="material">Shader material for the proxy box</param>
	/// <param name="anchorPoint">Optional custom anchor point in voxel space. If null, uses bottom-center.</param>
	public void Initialize(Vector3I modelSize, float voxelSize, float sigma, ShaderMaterial material, Point3D? anchorPoint = null)
	{
		_modelSize = modelSize;
		_voxelSize = voxelSize;
		_sigma = sigma;

		// Compute anchor point (default: bottom-center in Z-up voxel space)
		AnchorPoint = anchorPoint ?? new Point3D(modelSize.X >> 1, modelSize.Y >> 1, 0);

		// Compute box size using the 3D diagonal for worst-case orthographic projection
		// The orthographic projection's maximum extent from any viewing angle is the model's 3D diagonal
		float diagonal = Mathf.Sqrt(
			modelSize.X * modelSize.X +
			modelSize.Y * modelSize.Y +
			modelSize.Z * modelSize.Z);

		// Virtual pixel size in world units
		float deltaPxWorld = voxelSize / sigma;

		// Box side length: cube that contains orthographic projection from any angle + outline margin
		float boxSide = diagonal * voxelSize + 2f * deltaPxWorld;

		// Create cube mesh (same size on all axes)
		_proxyBox.Mesh = new BoxMesh { Size = new Vector3(boxSide, boxSide, boxSide) };
		_proxyBox.MaterialOverride = material;

		// Offset the proxy box child for anchor alignment and ground clearance
		//
		// The box mesh is centered at (0,0,0) in local space.
		// We need the anchor point (in voxel space) to align with the parent Node3D's origin.
		// The voxel model goes from (0,0,0) to (sizeX, sizeY, sizeZ) in voxel space.
		//
		// The shader expects the box local origin (0,0,0) to represent the anchor point.
		// The model center is at (sizeX/2, sizeY/2, sizeZ/2).
		// anchor_to_center = modelCenter - anchorPoint
		//
		// Convert voxel space (Z-up) to Godot space (Y-up):
		//   godot.X = voxel.X
		//   godot.Y = voxel.Z
		//   godot.Z = -voxel.Y
		Vector3 modelCenter = new Vector3(modelSize.X, modelSize.Y, modelSize.Z) * 0.5f;
		Vector3 anchorToCenter = modelCenter - new Vector3(AnchorPoint.X, AnchorPoint.Y, AnchorPoint.Z);
		Vector3 anchorOffsetGodot = new Vector3(
			anchorToCenter.X,
			anchorToCenter.Z,
			-anchorToCenter.Y) * voxelSize;

		// Ground clearance: offset up by one virtual pixel to prevent ground geometry
		// from occluding the bottom outline pixels
		float groundClearance = deltaPxWorld;

		// Total offset for the proxy box
		_proxyBox.Position = anchorOffsetGodot + new Vector3(0, groundClearance, 0);

		// Update shader uniforms
		// Pass anchor point directly - shader computes anchor_to_center and bounds from this
		material.SetShaderParameter("voxel_size", voxelSize);
		material.SetShaderParameter("sigma", sigma);
		material.SetShaderParameter("anchor_point", new Vector3(AnchorPoint.X, AnchorPoint.Y, AnchorPoint.Z));

		GD.Print($"VolumetricOrthoImpostor: {modelSize}, box_side={boxSide:F3}, anchor={AnchorPoint}");
	}

	/// <summary>
	/// Updates the impostor when voxel size or sigma changes.
	/// </summary>
	public void UpdateSizing(float voxelSize, float sigma)
	{
		if (_proxyBox.MaterialOverride is ShaderMaterial material)
			Initialize(_modelSize, voxelSize, sigma, material, AnchorPoint);
	}

	/// <summary>
	/// Sets a custom anchor point and reinitializes the impostor.
	/// </summary>
	/// <param name="anchorPoint">New anchor point in voxel space (Z-up).</param>
	public void SetAnchorPoint(Point3D anchorPoint)
	{
		if (_proxyBox.MaterialOverride is ShaderMaterial material)
			Initialize(_modelSize, _voxelSize, _sigma, material, anchorPoint);
	}
}
