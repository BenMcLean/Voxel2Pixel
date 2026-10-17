using System.Collections.Generic;
using Godot;

namespace BenVoxel.VR;

/// <summary>
/// Owns everything that happens between the hands and the things in the scene. Each frame, <see cref="Update"/>:
/// resolves what each hand is hovering (sending hover events), delivers trigger presses to the hovered item, and runs
/// grip grabs according to the grabbed item's <see cref="IInteractable.Grab"/> mode.
///
/// Hovering: the highest <see cref="IInteractable.Priority"/> hit wins, the nearest on a tie, and the earlier
/// registered one if still tied. While a hand holds a grab, it keeps hovering what it grabbed.
///
/// Grabbing: <see cref="GrabMode.Drag"/> starts when exactly one grip goes down on a hovered item and ignores the other
/// hand until it is released. Otherwise a <see cref="GrabMode.Free"/> item is picked by the hand that gripped (the
/// primary hand if both did), or the fallback if it touches none, and stays the same until all grips are released.
/// While one grip is held the item follows that controller (position and orientation). While both are held it follows
/// the midpoint between them, is oriented by the line between the hands (with roll taken from the controllers' up
/// directions) and is scaled by the change in hand distance. All cases use one "grab frame" whose basis is scaled by
/// the hand distance; the item is moved by frame_now * frame_start^-1, so it stays rigidly attached to the frame.
/// Pressing or releasing a grip changes the frame's definition, so the grab is re-baselined at that moment to avoid
/// a jump.
/// </summary>
public sealed class InteractionManager(Hand left, Hand right)
{
	private readonly List<IInteractable> _items = [];
	private IInteractable _fallback;

	public Hand Left { get; } = left;
	public Hand Right { get; } = right;
	/// <summary>The dominant hand: the one that edits, and the one that picks a Free grab when both grip at once.</summary>
	public Hand Primary => Right;

	/// <summary>Which hands were gripping on the previous frame (bit 0 left, bit 1 right); a change re-baselines the grab.</summary>
	private int _gripMask;
	/// <summary>The item being carried by a Free grab; null when none.</summary>
	private IInteractable _grabTarget;
	/// <summary>Grab frame and item transform captured when the current Free grab was last re-baselined.</summary>
	private Transform3D _grabFrameStart;
	private Transform3D _targetStart;
	/// <summary>The item and hand of the current Drag, and the item's position in that hand's local space when it started.</summary>
	private IInteractable _dragTarget;
	private Hand _dragHand;
	private Vector3 _dragOffset;

	/// <summary>Adds an item to be hovered and grabbed. Registration order breaks ties between equally near items.</summary>
	/// <param name="fallback">If true, a Free grab that touches nothing carries this item instead.</param>
	public void Register(IInteractable item, bool fallback = false)
	{
		_items.Add(item);
		if (fallback)
			_fallback = item;
	}

	public void Update()
	{
		Left.Update();
		Right.Update();
		UpdateHover(Left);
		UpdateHover(Right);
		UpdateTriggers(Left);
		UpdateTriggers(Right);
		UpdateGrab();
	}

	private void UpdateHover(Hand hand)
	{
		IInteractable hovered;
		if (_dragHand == hand)
			hovered = _dragTarget;
		else if (_grabTarget is not null && hand.GripPressed)
			hovered = _grabTarget;
		else
			hovered = Pick(hand, freeOnly: false);
		SetHovered(hand, hovered);
	}

	private static void SetHovered(Hand hand, IInteractable hovered)
	{
		if (hand.Hovered == hovered)
			return;
		hand.Hovered?.HoverExit(hand);
		hand.Hovered = hovered;
		hovered?.HoverEnter(hand);
	}

	private static void UpdateTriggers(Hand hand)
	{
		if (hand.TriggerJustPressed)
			hand.Hovered?.TriggerDown(hand);
	}

	private IInteractable Pick(Hand hand, bool freeOnly)
	{
		IInteractable best = null;
		float bestDistance = 0f;
		foreach (IInteractable item in _items)
		{
			if (freeOnly && item.Grab != GrabMode.Free)
				continue;
			if (item.HitTest(hand) is not float distance)
				continue;
			if (best is null || item.Priority > best.Priority || (item.Priority == best.Priority && distance < bestDistance))
			{
				best = item;
				bestDistance = distance;
			}
		}
		return best;
	}

	private void UpdateGrab()
	{
		bool l = Left.GripPressed, r = Right.GripPressed;
		if (UpdateDrag(l, r))
			return;
		int mask = (l ? 1 : 0) | (r ? 2 : 0);
		if (mask == 0)
		{
			_gripMask = 0;
			_grabTarget = null;
			return;
		}
		if (_gripMask == 0)
		{
			// Both grips down at once means the primary hand chooses, and only Free items can be chosen
			Hand picker = r ? Right : Left;
			_grabTarget = Pick(picker, freeOnly: true) ?? _fallback;
			if (_grabTarget is null)
				return;
		}
		Transform3D frame = GetGrabFrame(l, r);
		if (mask != _gripMask)
		{
			_gripMask = mask;
			_grabFrameStart = frame;
			_targetStart = _grabTarget.Node.GlobalTransform;
			return;
		}
		_grabTarget.Node.GlobalTransform = frame * _grabFrameStart.AffineInverse() * _targetStart;
	}

	/// <summary>
	/// Runs Drag grabs, which take priority over Free grabs. Returns true if the Free grab should be skipped this
	/// frame. While dragging, the Free grab is suspended and its state reset, so a still-held other grip
	/// re-baselines cleanly afterwards.
	/// </summary>
	private bool UpdateDrag(bool l, bool r)
	{
		if (_dragHand is not null && !_dragHand.GripPressed)
		{
			_dragHand = null;
			_dragTarget = null;
		}
		else if (_dragHand is null && _gripMask == 0 && l != r)
		{
			Hand hand = r ? Right : Left;
			if (hand.Hovered is { Grab: GrabMode.Drag } target)
			{
				_dragHand = hand;
				_dragTarget = target;
				_dragOffset = hand.GlobalTransform.AffineInverse() * target.Node.GlobalPosition;
			}
		}
		if (_dragHand is null)
			return false;
		_gripMask = 0;
		_grabTarget = null;
		_dragTarget.Node.GlobalPosition = _dragHand.GlobalTransform * _dragOffset;
		return true;
	}

	private Transform3D GetGrabFrame(bool l, bool r)
	{
		if (!(l && r))
			return (l ? Left : Right).GlobalTransform;
		Transform3D lt = Left.GlobalTransform;
		Transform3D rt = Right.GlobalTransform;
		Vector3 across = rt.Origin - lt.Origin;
		float distance = Mathf.Max(across.Length(), 0.01f);
		Vector3 x = across / distance;
		Vector3 up = lt.Basis.Y + rt.Basis.Y;
		Vector3 z = x.Cross(up);
		if (z.LengthSquared() < 1e-6f)
			z = x.Cross(Vector3.Up); // controllers' up is parallel to the hand line; any perpendicular will do
		z = z.Normalized();
		Vector3 y = z.Cross(x);
		return new Transform3D(new Basis(x, y, z).Scaled(Vector3.One * distance), (lt.Origin + rt.Origin) / 2);
	}
}
