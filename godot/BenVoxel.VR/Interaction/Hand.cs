using Godot;

namespace BenVoxel.VR;

/// <summary>
/// One tracked hand. Wraps a controller so nothing else reads buttons or poses from the controller directly:
/// a different input source (for example optical hand tracking) could replace this class without touching
/// anything that interacts with it. <see cref="Update"/> must be called once per frame, before reading state.
/// </summary>
public sealed class Hand(XRController3D controller)
{
	/// <summary>Distance in metres from the grip pose to the controller tip (the marker box is 0.1 m deep, centred on the grip).</summary>
	public const float TipOffset = 0.05f;

	public XRController3D Controller { get; } = controller;
	public Transform3D GlobalTransform => Controller.GlobalTransform;
	/// <summary>World position of the point this hand touches things with.</summary>
	public Vector3 TipWorld => Controller.GlobalTransform * new Vector3(0f, 0f, -TipOffset);
	public bool HasTracking => Controller.GetHasTrackingData();
	public bool GripPressed { get; private set; }
	public bool TriggerPressed { get; private set; }
	/// <summary>True only on the frame the trigger went down.</summary>
	public bool TriggerJustPressed { get; private set; }
	/// <summary>The A/X face button, held.</summary>
	public bool AxPressed { get; private set; }
	/// <summary>Thumbstick deflection, each axis -1 to 1: x right, y up.</summary>
	public Vector2 Stick { get; private set; }
	public bool StickPressed { get; private set; }
	/// <summary>True only on the frame the thumbstick was clicked down.</summary>
	public bool StickJustPressed { get; private set; }
	/// <summary>True only on the frame the thumbstick click let go.</summary>
	public bool StickJustReleased { get; private set; }
	/// <summary>What this hand is currently pointing at, as resolved by <see cref="InteractionManager"/>; null if nothing.</summary>
	public IInteractable Hovered { get; internal set; }

	/// <summary>Buzzes the controller. Amplitude is 0 to 1.</summary>
	public void Pulse(float amplitude, float seconds) => Controller.TriggerHapticPulse("haptic", 0f, amplitude, seconds, 0f);

	public void Update()
	{
		GripPressed = Controller.IsButtonPressed("grip_click");
		bool trigger = Controller.IsButtonPressed("trigger_click");
		TriggerJustPressed = trigger && !TriggerPressed;
		TriggerPressed = trigger;
		AxPressed = Controller.IsButtonPressed("ax_button");
		Stick = Controller.GetVector2("primary");
		bool stick = Controller.IsButtonPressed("primary_click");
		StickJustPressed = stick && !StickPressed;
		StickJustReleased = !stick && StickPressed;
		StickPressed = stick;
	}
}
