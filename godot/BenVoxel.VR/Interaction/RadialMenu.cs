using System;
using System.Collections.Generic;
using Godot;

namespace BenVoxel.VR;

/// <summary>
/// A ring of floating 3D objects, one per item, driven by a hand's thumbstick: pushing the stick out of its dead zone
/// opens the menu, the item the stick points at is picked out, and clicking the stick chooses it. Letting the stick
/// return to its dead zone closes the menu without choosing anything.
///
/// Nothing here depends on how many items there are. Each item owns an equal sector of the circle (a full turn divided
/// by the item count), the first straight up and the rest clockwise, and the ring is made just wide enough that
/// neighbouring icons do not touch, so adding an item with <see cref="AddItem"/> re-lays-out the whole ring.
///
/// The menu stays attached to the hand that opened it, a little toward the viewer and always facing them, so that stick
/// up is always up for the viewer. A dot shows where the stick is, and only the picked icon turns.
///
/// How a menu is opened, picked from and closed lives in <see cref="Update"/> alone, so a menu that opens another way
/// (for example a per-tool menu opened by a stick click) can differ from this one without touching the layout. Call <see cref="Update"/> once per frame for each hand after <see cref="Hand.Update"/>.
/// </summary>
public sealed partial class RadialMenu : Node3D
{
	/// <summary>Length in metres of the diagonal of the box that every icon is scaled to fit, so any model works as an icon.</summary>
	private const float IconDiagonal = 0.045f;
	/// <summary>Least distance between neighbouring icon centres, in icon diagonals. It leaves room for the picked icon to grow.</summary>
	private const float Spacing = 1.6f;
	/// <summary>Ring radius in metres when there are so few items that spacing alone would give a tiny ring.</summary>
	private const float MinRadius = 0.05f;
	/// <summary>How much bigger the picked icon is drawn.</summary>
	private const float PickedScale = 1.4f;
	/// <summary>Stick deflection (0 to 1) that opens the menu.</summary>
	private const float OpenDeadZone = 0.6f;
	/// <summary>Stick deflection below which an open menu closes and nothing is picked; lower than <see cref="OpenDeadZone"/> so a stick hovering at the edge does not flicker the menu.</summary>
	private const float CloseDeadZone = 0.4f;
	/// <summary>How far in metres toward the viewer from the hand the ring's centre opens.</summary>
	private const float OpenDistance = 0.06f;
	private const float SpinRadiansPerSecond = 2.5f;
	/// <summary>Radius in metres of the dot that shows where the stick is.</summary>
	private const float DotRadius = 0.005f;
	/// <summary>Higher is snappier growing, shrinking and turning back of icons.</summary>
	private const float ScaleRate = 15f;

	private sealed class Item(string name, Node3D slot, Node3D spinner, Action<Hand> chosen)
	{
		public string Name { get; } = name;
		/// <summary>Placed on the ring and scaled when picked.</summary>
		public Node3D Slot { get; } = slot;
		/// <summary>Turns the icon about the vertical so its shape can be seen, but only while it is picked.</summary>
		public Node3D Spinner { get; } = spinner;
		/// <summary>Current turn of <see cref="Spinner"/> in radians.</summary>
		public float Angle { get; set; }
		public Action<Hand> Chosen { get; } = chosen;
	}

	private readonly List<Item> _items = [];
	private readonly Label3D _label = new()
	{
		Name = "Label",
		Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
		NoDepthTest = true,
		PixelSize = 0.00015f,
		FontSize = 64,
		OutlineSize = 16,
	};
	/// <summary>The hand that opened the menu; null when closed.</summary>
	private Hand _owner;
	/// <summary>A hand that has just chosen an item and so cannot reopen the menu until its stick returns to the dead zone; null if none.</summary>
	private Hand _awaitingNeutral;
	/// <summary>Index of the picked item, or -1.</summary>
	private int _picked = -1;
	/// <summary>Radius of the ring in metres, as laid out for the current item count.</summary>
	private float _radius = MinRadius;
	/// <summary>Shows where the thumbstick is, on the same circle the icons are on.</summary>
	private readonly MeshInstance3D _dot = new()
	{
		Name = "StickDot",
		Mesh = new SphereMesh { Radius = DotRadius, Height = DotRadius * 2f },
		MaterialOverride = new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			AlbedoColor = Colors.White,
			NoDepthTest = true,
		},
		CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
	};

	public bool IsOpen => _owner is not null;

	public RadialMenu()
	{
		Visible = false;
		AddChild(_label);
		AddChild(_dot);
	}

	/// <summary>
	/// Adds an item to the end of the ring (the next sector clockwise). The icon is measured, centred and scaled to fit
	/// whatever its own size and origin, and is only displayed: it should not be registered with the interaction manager.
	/// </summary>
	/// <param name="tiltDegrees">Leans the icon over by this much, so that turning it about the vertical shows something even for an upright object.</param>
	/// <param name="chosen">Called with the hand that opened the menu when the item is chosen.</param>
	public void AddItem(string name, Node3D icon, Action<Hand> chosen, float tiltDegrees = 0f)
	{
		Aabb bounds = ModelProp.MeshBounds(icon, Transform3D.Identity) ?? new Aabb(Vector3.Zero, Vector3.One);
		float scale = IconDiagonal / Mathf.Max(bounds.Size.Length(), 0.0001f);
		Node3D fit = new() { Name = "Fit", Scale = Vector3.One * scale, Position = -bounds.GetCenter() * scale };
		fit.AddChild(icon);
		Node3D tilt = new() { Name = "Tilt", Rotation = new Vector3(0f, 0f, Mathf.DegToRad(tiltDegrees)) };
		tilt.AddChild(fit);
		Node3D spinner = new() { Name = "Spinner" };
		spinner.AddChild(tilt);
		Node3D slot = new() { Name = name };
		slot.AddChild(spinner);
		AddChild(slot);
		_items.Add(new Item(name, slot, spinner, chosen));
		Layout();
	}

	/// <summary>Gives each item an equal sector and puts its icon on the ring through the sector's centre.</summary>
	private void Layout()
	{
		float sector = Mathf.Tau / _items.Count;
		// Neighbouring centres are 2 * radius * sin(sector / 2) apart; a lone item has no neighbour to keep clear of
		_radius = _items.Count < 2
			? MinRadius
			: Mathf.Max(MinRadius, IconDiagonal * Spacing / (2f * Mathf.Sin(sector / 2f)));
		for (int i = 0; i < _items.Count; i++)
		{
			float angle = i * sector;
			_items[i].Slot.Position = new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0f) * _radius;
		}
	}

	/// <summary>Opens, moves with and closes the menu according to one hand's thumbstick.</summary>
	/// <param name="headPosition">The viewer's position in world space; the menu opens facing it.</param>
	public void Update(Hand hand, Vector3 headPosition)
	{
		float deflection = hand.Stick.Length();
		if (_owner is null)
		{
			if (_awaitingNeutral == hand && deflection < CloseDeadZone)
				_awaitingNeutral = null;
			if (_awaitingNeutral != hand && deflection >= OpenDeadZone && _items.Count > 0)
				Open(hand, headPosition);
			return;
		}
		if (_owner != hand)
			return;
		// A click jolts the stick, so it chooses what was picked on the frame before, not what is under the stick now
		if (hand.StickJustPressed && _picked >= 0)
		{
			Close(choose: true);
			return;
		}
		if (deflection < CloseDeadZone)
		{
			Close(choose: false);
			return;
		}
		Place(hand, headPosition);
		Pick(PickSector(hand.Stick));
		_dot.Position = new Vector3(hand.Stick.X, hand.Stick.Y, 0f).LimitLength() * _radius;
	}

	/// <summary>Puts the menu a little toward the viewer from the hand. -Z looks away from the viewer, so the menu's front (+Z) faces them, with X right and Y up for them.</summary>
	private void Place(Hand hand, Vector3 headPosition)
	{
		Vector3 origin = hand.GlobalTransform.Origin;
		Vector3 towardHead = (headPosition - origin).Normalized();
		GlobalTransform = new Transform3D(Basis.LookingAt(-towardHead, Vector3.Up), origin + towardHead * OpenDistance);
	}

	private void Open(Hand hand, Vector3 headPosition)
	{
		_owner = hand;
		Place(hand, headPosition);
		Pick(PickSector(hand.Stick));
		_dot.Position = new Vector3(hand.Stick.X, hand.Stick.Y, 0f).LimitLength() * _radius;
		Visible = true;
	}

	private void Close(bool choose)
	{
		Hand owner = _owner;
		int chosen = _picked;
		_owner = null;
		Visible = false;
		Pick(-1);
		if (!choose || chosen < 0)
			return;
		_awaitingNeutral = owner;
		_items[chosen].Chosen(owner);
	}

	/// <summary>The item the stick points at, or -1 if it is too near the centre.</summary>
	private int PickSector(Vector2 stick)
	{
		if (stick.Length() < CloseDeadZone)
			return -1;
		// Clockwise from straight up, the way the items are laid out
		float angle = Mathf.PosMod(Mathf.Atan2(stick.X, stick.Y), Mathf.Tau);
		float sector = Mathf.Tau / _items.Count;
		return (int)Mathf.Round(angle / sector) % _items.Count;
	}

	private void Pick(int index)
	{
		_picked = index;
		_label.Text = index >= 0 ? _items[index].Name : "";
	}

	public override void _Process(double delta)
	{
		if (_owner is null)
			return;
		float blend = 1f - Mathf.Exp(-ScaleRate * (float)delta);
		for (int i = 0; i < _items.Count; i++)
		{
			Vector3 target = Vector3.One * (i == _picked ? PickedScale : 1f);
			_items[i].Slot.Scale = _items[i].Slot.Scale.Lerp(target, blend);
			// Turning is the reward for picking: the picked icon spins, and the others settle back to rest
			Item item = _items[i];
			item.Angle = i == _picked ? item.Angle + SpinRadiansPerSecond * (float)delta : Mathf.LerpAngle(item.Angle, 0f, blend);
			item.Spinner.Rotation = new Vector3(0f, item.Angle, 0f);
		}
	}
}
