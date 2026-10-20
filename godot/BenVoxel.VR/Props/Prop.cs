using Godot;

namespace BenVoxel.VR.Props;

/// <summary>
/// Base class for every placeable object in the scene, including the models. A prop is automatically touched,
/// hovered and carried by hands: one hand moves and turns it, two hands also scale it (see <see cref="InteractionManager"/>).
/// A prop only says how big it is (<see cref="LocalSphere"/>); add it to the scene with <see cref="Root.AddProp"/>, which
/// also makes it grabbable, so none of that is written again for each prop.
/// </summary>
public abstract partial class Prop : Node3D, IInteractable
{
	/// <summary>The sphere a hand has to be inside to touch this prop, in the prop's local space (it follows the prop's scale).</summary>
	public abstract (Vector3 Centre, float Radius) LocalSphere { get; }

	/// <summary>Higher wins where props overlap; <see cref="VoxelProp"/> lowers this because it is touched by a much larger volume.</summary>
	public virtual int Priority => 1;

	Node3D IInteractable.Node => this;
	public GrabMode Grab => GrabMode.Free;

	public float? HitTest(Hand hand)
	{
		(Vector3 centre, float radius) = LocalSphere;
		float distance = ToGlobal(centre).DistanceTo(hand.TipWorld);
		return distance <= radius * GlobalTransform.Basis.Scale.X ? distance : null;
	}
}
