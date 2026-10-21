using Godot;

namespace BenVoxel.VR;

/// <summary>What a grip does to an interactable.</summary>
public enum GrabMode
{
	/// <summary>Grip does nothing.</summary>
	None,
	/// <summary>One hand drags the node's position; it keeps its orientation and scale.</summary>
	Drag,
	/// <summary>One hand carries the node (position and orientation); two hands also scale it.</summary>
	Free,
}

/// <summary>
/// Anything a hand can touch. Register it with <see cref="InteractionManager"/>, which resolves what each hand
/// is hovering, delivers hover and trigger events, and runs grabs according to <see cref="Grab"/>.
/// </summary>
public interface IInteractable
{
	/// <summary>The node moved by grabs.</summary>
	Node3D Node { get; }
	GrabMode Grab { get; }
	/// <summary>When several interactables are touched at once, the highest priority wins; the nearer wins a tie.</summary>
	int Priority { get; }
	/// <summary>True if the voxel edit cursor should hide while a hand hovers this.</summary>
	bool SuppressesEditCursor => false;
	/// <summary>Distance in metres at which the hand touches this, or null if it does not. Used to rank hits.</summary>
	float? HitTest(Hand hand);
	void HoverEnter(Hand hand) { }
	void HoverExit(Hand hand) { }
	void TriggerDown(Hand hand) { }
	/// <summary>
	/// Called when a grip is about to carry this. Returns what to carry instead, which lets an item that only stands
	/// for something (an icon in the toy box) bring the real thing into being under the hand. It must already be
	/// registered with the <see cref="InteractionManager"/>, and be in the same <see cref="Grab"/> mode.
	/// </summary>
	IInteractable BeginGrab(Hand hand) => this;
}
