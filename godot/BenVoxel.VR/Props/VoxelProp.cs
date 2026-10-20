using Godot;

namespace BenVoxel.VR.Props;

/// <summary>
/// A prop that shows a voxel model. Its node's local space is Godot Y-up voxel space, so scaling it to metres-per-voxel
/// and moving it places the model in the world. It is touched anywhere inside a sphere around the model's bounds, which
/// change as the model is edited, so <see cref="Root"/> keeps <see cref="BoxCenter"/> and <see cref="BoxSize"/> current.
/// </summary>
public abstract partial class VoxelProp : Prop
{
	/// <summary>Centre of the model bounds in voxel space.</summary>
	public Vector3 BoxCenter { get; set; }
	/// <summary>Size of the model bounds in voxel space.</summary>
	public Vector3 BoxSize { get; set; }

	public override (Vector3 Centre, float Radius) LocalSphere => (BoxCenter, BoxSize.Length() * 0.5f);

	/// <summary>Voxel models are touched by much larger volumes than other props, which therefore win where they overlap.</summary>
	public override int Priority => 0;
}
