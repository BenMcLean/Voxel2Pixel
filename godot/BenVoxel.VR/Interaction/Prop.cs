using Godot;

namespace BenVoxel.VR;

/// <summary>
/// Base class for every placeable object that is not part of the model being edited. A prop is automatically touched,
/// hovered and carried by hands: one hand moves and turns it, two hands also scale it (see <see cref="InteractionManager"/>).
/// A prop only says how big it is (<see cref="LocalSphere"/>); add it to the scene with <see cref="Root.AddProp"/>, which
/// also makes it grabbable, so none of that is written again for each prop.
/// </summary>
public abstract partial class Prop : Node3D, IInteractable
{
	/// <summary>The sphere a hand has to be inside to touch this prop, in the prop's local space (it follows the prop's scale).</summary>
	public abstract (Vector3 Centre, float Radius) LocalSphere { get; }

	/// <summary>Props win over the model and the sprite preview where they overlap, since those are touched by much larger volumes.</summary>
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
