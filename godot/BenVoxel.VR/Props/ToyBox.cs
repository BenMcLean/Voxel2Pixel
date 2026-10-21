using System;
using System.Collections.Generic;
using Godot;

namespace BenVoxel.VR.Props;

/// <summary>
/// The toy box: a treasure chest (assets/chest/chest.glb, see assets/ASSETS.md). The node is the chest's pivot, at the
/// centre of its base, in metres. The model is a child scaled to
/// about <see cref="WidthMetres"/> wide.
///
/// A trigger on the chest opens or closes a grid of miniature icons floating above it, one per entry added with
/// <see cref="AddItem"/>. The grid wraps at <see cref="Columns"/> and grows upward, so an entry is added with one call
/// and nothing else changes. Gripping an icon brings the real prop into being under the hand at full size; letting go
/// of a prop that came from here with the hand on the chest puts it away again.
/// </summary>
public partial class ToyBox : Prop
{
	/// <summary>Chest width in metres.</summary>
	private const float WidthMetres = 0.14f;
	/// <summary>The model's width in its own units, and how far its base is below the model origin (both measured from the glb).</summary>
	private const float ModelWidth = 1.6446f * 1.7780f;
	private const float ModelBaseBelowOrigin = 0.4412f * 1.7780f;

	/// <summary>
	/// How far the model is turned about the vertical (counter-clockwise seen from above) so that its front faces this node's +Z,
	/// which is what <c>Root.SummonToyBox</c> relies on. The glb's own front faces a different way.
	/// </summary>
	private static readonly float FrontTurn = Mathf.DegToRad(90f);

	/// <summary>Icons per row of the grid.</summary>
	private const int Columns = 4;
	/// <summary>Distance in metres between neighbouring icon centres.</summary>
	private const float Cell = 0.09f;
	/// <summary>Length in metres of the diagonal of the box every icon is scaled to fit.</summary>
	private const float IconDiagonal = 0.06f;
	/// <summary>Height in metres of the grid's bottom row above the chest's base.</summary>
	private const float GridBase = 0.17f;
	/// <summary>Strength and length in seconds of the buzz when a hand moves onto an icon, and when it pulls a prop out.</summary>
	private const float HoverPulse = 0.25f, HoverPulseSeconds = 0.04f;
	private const float SpawnPulse = 0.7f, SpawnPulseSeconds = 0.1f;
	/// <summary>Buzz on touching the chest and on clicking it.</summary>
	private const float TouchPulse = 0.35f, TouchPulseSeconds = 0.05f;
	private const float ClickPulse = 0.6f, ClickPulseSeconds = 0.08f;
	private const float HoveredScale = 1.4f;
	private const float SpinRadiansPerSecond = 2.5f;
	private const float ScaleRate = 15f;

	/// <summary>What one icon in the grid stands for.</summary>
	/// <param name="Spawn">Brings the real thing into the world under the hand, registered for interaction, and returns it. It may return something that already exists.</param>
	/// <param name="Despawn">Removes something that <paramref name="Spawn"/> returned from the world.</param>
	private sealed record Entry(string Name, GrabMode Grab, Func<Hand, IInteractable> Spawn, Action<IInteractable> Despawn);

	private sealed class Slot(ToyBox owner, Entry entry, Node3D node, Node3D spinner) : IInteractable
	{
		public Node3D Node { get; } = node;
		public Node3D Spinner { get; } = spinner;
		public float Angle { get; set; }
		public int Hovers { get; private set; }
		public GrabMode Grab => entry.Grab;
		/// <summary>Above the chest itself, whose touch sphere the grid overlaps.</summary>
		public int Priority => 2;
		public bool SuppressesEditCursor => true;

		public float? HitTest(Hand hand)
		{
			if (!owner._open || !hand.HasTracking)
				return null;
			float distance = Node.GlobalPosition.DistanceTo(hand.TipWorld);
			return distance <= Cell * 0.5f ? distance : null;
		}

		public void HoverEnter(Hand hand)
		{
			Hovers++;
			hand.Pulse(HoverPulse, HoverPulseSeconds);
		}

		public void HoverExit(Hand hand) => Hovers--;

		public IInteractable BeginGrab(Hand hand)
		{
			IInteractable spawned = entry.Spawn(hand);
			owner._spawned[spawned] = entry;
			hand.Pulse(SpawnPulse, SpawnPulseSeconds);
			return spawned;
		}
	}

	private readonly List<Slot> _slots = [];
	/// <summary>Everything currently out of the box that came from it, and the entry that made it.</summary>
	private readonly Dictionary<IInteractable, Entry> _spawned = [];
	private readonly Node3D _panel = new() { Name = "Panel", Visible = false };
	private bool _open;
	/// <summary>Per hand (left, right): whether it was carrying a prop from the box inside the chest on the previous frame.</summary>
	private readonly bool[] _carryingInside = new bool[2];

	/// <summary>Where icons register so they can be hovered and gripped; set before adding items.</summary>
	public InteractionManager Interaction { get; set; }

	/// <summary>How far in metres beyond the model a hand still touches it, so the edge is easy to find.</summary>
	private const float TouchMargin = 0.01f;

	/// <summary>The chest model's bounds in this node's local space, grown by <see cref="TouchMargin"/>. A hand touches the chest inside this box.</summary>
	private readonly Aabb _touchBox;

	public override (Vector3 Centre, float Radius) LocalSphere => (_touchBox.GetCenter(), _touchBox.Size.Length() * 0.5f);

	/// <summary>True if a world point is inside the touch box, whatever the chest's position, turn and scale.</summary>
	private bool Touches(Vector3 world) => _touchBox.HasPoint(ToLocal(world));

	public override float? HitTest(Hand hand) =>
		hand.HasTracking && Touches(hand.TipWorld) ? hand.TipWorld.DistanceTo(ToGlobal(_touchBox.GetCenter())) : null;

	public ToyBox()
	{
		Name = "ToyBox";
		_panel.Position = new Vector3(0f, GridBase, 0f);
		AddChild(_panel);
		float scale = WidthMetres / ModelWidth;
		Node3D model = GD.Load<PackedScene>("res://assets/chest/chest.glb").Instantiate<Node3D>();
		model.Scale = Vector3.One * scale;
		model.Position = new Vector3(0f, ModelBaseBelowOrigin * scale, 0f);
		model.Rotation = new Vector3(0f, FrontTurn, 0f);
		AddChild(model);
		_touchBox = (ModelProp.MeshBounds(model, Transform3D.Identity) ?? new Aabb(Vector3.Zero, Vector3.One * WidthMetres)).Grow(TouchMargin);
		// Nothing in the scene is lit but the voxels, whose shader does its own lighting, so show the pixel-art texture as is
		foreach (Node child in model.FindChildren("*", nameof(MeshInstance3D), true, false))
		{
			MeshInstance3D mesh = (MeshInstance3D)child;
			if (mesh.GetActiveMaterial(0) is BaseMaterial3D source)
			{
				BaseMaterial3D material = (BaseMaterial3D)source.Duplicate();
				material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
				material.TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest;
				mesh.MaterialOverride = material;
			}
		}
	}

	/// <summary>Adds an icon to the end of the grid, which re-lays itself out.</summary>
	/// <param name="makeIcon">Makes a fresh model of the icon, measured and scaled to fit whatever its size.</param>
	/// <param name="grab">The grab mode of what <paramref name="spawn"/> returns, so the right kind of grab starts.</param>
	/// <param name="tiltDegrees">Leans the icon over so turning it shows something even for an upright object.</param>
	public void AddItem(string name, Func<Node3D> makeIcon, GrabMode grab, Func<Hand, IInteractable> spawn, Action<IInteractable> despawn, float tiltDegrees = 0f)
	{
		Node3D tilt = new() { Name = "Tilt", Rotation = new Vector3(0f, 0f, Mathf.DegToRad(tiltDegrees)) };
		tilt.AddChild(MiniIcon.Fit(makeIcon(), IconDiagonal));
		Node3D spinner = new() { Name = "Spinner" };
		spinner.AddChild(tilt);
		Node3D node = new() { Name = name };
		node.AddChild(spinner);
		_panel.AddChild(node);
		Slot slot = new(this, new Entry(name, grab, spawn, despawn), node, spinner);
		_slots.Add(slot);
		Interaction?.Register(slot);
		// Rows fill left to right and stack upward; a short last row is centred
		for (int i = 0; i < _slots.Count; i++)
		{
			int row = i / Columns;
			int inRow = Math.Min(Columns, _slots.Count - row * Columns);
			_slots[i].Node.Position = new Vector3((i % Columns - (inRow - 1) / 2f) * Cell, row * Cell, 0f);
		}
	}

	public override void HoverEnter(Hand hand) => hand.Pulse(TouchPulse, TouchPulseSeconds);

	public override void TriggerDown(Hand hand)
	{
		_panel.Visible = _open = !_open;
		hand.Pulse(ClickPulse, ClickPulseSeconds);
	}

	/// <summary>Turns the grid toward the viewer about the vertical and holds it above the chest. The grid is always life size, whatever the chest's scale. Call once per frame.</summary>
	public void Face(Vector3 headPosition)
	{
		Vector3 toHead = headPosition - GlobalPosition;
		toHead.Y = 0f;
		if (!_open || toHead.LengthSquared() < 1e-4f)
			return;
		_panel.GlobalTransform = new Transform3D(
			new Basis(Vector3.Up, Mathf.Atan2(toHead.X, toHead.Z)),
			ToGlobal(new Vector3(0f, GridBase, 0f)));
	}

	/// <summary>Puts away a prop that came from the box if it was let go with the hand on the chest. Wire to <see cref="InteractionManager.Released"/>.</summary>
	public void OnReleased(IInteractable item, Hand hand)
	{
		if (!_spawned.TryGetValue(item, out Entry entry) || !Touches(hand.TipWorld))
			return;
		_spawned.Remove(item);
		entry.Despawn(item);
	}

	/// <summary>Buzzes each hand that is carrying a prop from the box, according to whether it is inside the chest.</summary>
	private void UpdateCarrying()
	{
		if (Interaction is null)
			return;
		for (int i = 0; i < 2; i++)
		{
			Hand hand = i == 0 ? Interaction.Left : Interaction.Right;
			IInteractable carried = hand.GripPressed && hand.Hovered is { } held && _spawned.ContainsKey(held) ? held : null;
			bool inside = carried is not null && Touches(hand.TipWorld);
			// A prop that would be put away if let go now is hidden, so it is clear it is on its way into the chest
			if (carried is not null)
				carried.Node.Visible = !inside;
			// The same buzz as an empty hand touching the chest
			if (inside && !_carryingInside[i])
				hand.Pulse(TouchPulse, TouchPulseSeconds);
			_carryingInside[i] = inside;
		}
	}

	public override void _Process(double delta)
	{
		UpdateCarrying();
		if (!_open)
			return;
		float blend = 1f - Mathf.Exp(-ScaleRate * (float)delta);
		foreach (Slot slot in _slots)
		{
			bool hovered = slot.Hovers > 0;
			slot.Node.Scale = slot.Node.Scale.Lerp(Vector3.One * (hovered ? HoveredScale : 1f), blend);
			slot.Angle = hovered ? slot.Angle + SpinRadiansPerSecond * (float)delta : Mathf.LerpAngle(slot.Angle, 0f, blend);
			slot.Spinner.Rotation = new Vector3(0f, slot.Angle, 0f);
		}
	}
}
