using System;
using Godot;

namespace BenVoxel.VR;

/// <summary>
/// An interactable touched anywhere inside a sphere around a point of its node, carried by <see cref="GrabMode.Free"/>
/// grabs. The sphere is given in the node's local space and follows the node's scale. Used for the model and the
/// ortho-sprite preview, whose bounds change as the model is edited, hence the delegate.
/// </summary>
public sealed class SphereGrabbable(Node3D node, Func<(Vector3 Centre, float Radius)> localSphere, int priority = 0) : IInteractable
{
	public Node3D Node { get; } = node;
	public GrabMode Grab => GrabMode.Free;
	public int Priority { get; } = priority;

	public float? HitTest(Hand hand)
	{
		(Vector3 centre, float radius) = localSphere();
		float distance = Node.ToGlobal(centre).DistanceTo(hand.TipWorld);
		return distance <= radius * Node.GlobalTransform.Basis.Scale.X ? distance : null;
	}
}
