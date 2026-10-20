using System;
using Godot;

namespace BenVoxel.VR.Props;

/// <summary>
/// The point light, shown as a white sphere in world space (a child of the root, so grab-scaling the model does not
/// resize it). Dragging it with a grip moves it; hovering shows a halo; the trigger toggles it. When off (black) the
/// viewer's head is the light source instead (see <see cref="Root"/>).
/// </summary>
public sealed class LightProp : IInteractable
{
	/// <summary>Radius of the light sphere in metres.</summary>
	public const float Radius = 0.025f;

	private readonly Func<Vector3, Vector3, float, bool> _cellTouchesSphere;
	private readonly StandardMaterial3D _material;
	private readonly MeshInstance3D _sphere;
	private readonly MeshInstance3D _halo;
	/// <summary>Number of hands hovering; the halo stays up while any does.</summary>
	private int _hoverCount;

	/// <param name="cellTouchesSphere">
	/// Given a hand tip, a sphere centre and a radius (all world space, metres), says whether the voxel cell under the
	/// tip overlaps the sphere. The light is touched when the hand's edit cursor would be touching it, which is how
	/// the cursor's blinking cube tells you it is on the light.
	/// </param>
	public LightProp(Vector3 worldPosition, Func<Vector3, Vector3, float, bool> cellTouchesSphere)
	{
		_cellTouchesSphere = cellTouchesSphere;
		_material = new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			AlbedoColor = Colors.White,
		};
		_sphere = new MeshInstance3D
		{
			Name = "LightSphere",
			Mesh = new SphereMesh { Radius = Radius, Height = Radius * 2f },
			MaterialOverride = _material,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			Position = worldPosition,
		};
		_halo = new MeshInstance3D
		{
			Name = "LightHalo",
			Mesh = new SphereMesh { Radius = Radius * 2f, Height = Radius * 4f },
			MaterialOverride = new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				BlendMode = BaseMaterial3D.BlendModeEnum.Add,
				AlbedoColor = new Color(1f, 1f, 1f, 0.25f),
			},
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			Visible = false,
		};
		_sphere.AddChild(_halo);
	}

	public Node3D Node => _sphere;
	/// <summary>True when the light is on (white).</summary>
	public bool On { get; private set; } = true;
	public Vector3 WorldPosition => _sphere.GlobalPosition;

	public GrabMode Grab => GrabMode.Drag;
	/// <summary>Above the model and sprite, which are touched by much larger volumes.</summary>
	public int Priority => 1;
	public bool SuppressesEditCursor => true;

	public float? HitTest(Hand hand) =>
		hand.HasTracking && _cellTouchesSphere(hand.TipWorld, _sphere.GlobalPosition, Radius)
			? hand.TipWorld.DistanceTo(_sphere.GlobalPosition)
			: null;

	public void HoverEnter(Hand hand) => _halo.Visible = ++_hoverCount > 0;

	public void HoverExit(Hand hand) => _halo.Visible = --_hoverCount > 0;

	public void TriggerDown(Hand hand)
	{
		On = !On;
		_material.AlbedoColor = On ? Colors.White : Colors.Black;
	}
}
