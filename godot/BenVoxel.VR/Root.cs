using System;
using System.Buffers.Binary;
using System.Reflection;
using System.Runtime.InteropServices;
using BenVoxel.FileToVoxCore;
using BenVoxel.Models;
using Godot;

namespace BenVoxel.VR;

/// <summary>
/// Application root. Everything in the scene is created here in code; root.tscn contains only this node.
///
/// First-pass demo: loads Sora.vox, uploads it to the GPU through <see cref="VoxelBridge"/> and renders it
/// by raymarching inside a bounding box (see voxel_raymarch.gdshader), in VR if OpenXR is available and
/// otherwise on the flat screen with a free-look camera (also forced by the --flatscreen argument).
///
/// Coordinate systems: voxel data is Z-up (MagicaVoxel); Godot is Y-up. Everything on the C# side that
/// positions the box converts (x, y, z)_zup to (x, z, y)_yup. The shader converts back when it reads
/// voxels, so "voxel space" is Godot-oriented with one unit per voxel. The model is placed in the world
/// by the transform of <see cref="_modelPivot"/>, which maps voxel space to metres (see
/// <see cref="MetresPerVoxel"/>); the shader follows that transform through inv_model_matrix.
/// </summary>
public partial class Root : Node3D
{
	/// <summary>World size of one voxel in metres. 0.01 makes Sora about the size of a figurine.</summary>
	private const float MetresPerVoxel = 0.01f;
	/// <summary>Where the centre of the model sits relative to the floor-level origin, in metres: in front of the player at chest height.</summary>
	private static readonly Vector3 ModelWorldCenter = new(0f, 1.2f, -0.6f);

	private bool _vrActive;
	private XROrigin3D _xrOrigin;
	private XRCamera3D _xrCamera;
	private XRController3D _leftController;
	private XRController3D _rightController;
	/// <summary>Which controllers were gripping on the previous frame (bit 0 left, bit 1 right); a change re-baselines the grab.</summary>
	private int _gripMask;
	/// <summary>Grab frame and model pivot transform captured when the current grab started.</summary>
	private Transform3D _grabFrameStart;
	private Transform3D _pivotStart;
	private FreeLookCamera _flatCamera;

	/// <summary>Distance in metres from the grip pose to the controller tip (the marker box is 0.1 m deep, centred on the grip).</summary>
	private const float TipOffset = 0.05f;
	/// <summary>Seconds per blink phase of the edit cursor.</summary>
	private const double CursorBlinkPeriod = 0.4;
	/// <summary>One-voxel translucent cube, child of <see cref="_modelPivot"/> and snapped to the voxel under the right controller's tip.</summary>
	private MeshInstance3D _cursor;
	private StandardMaterial3D _cursorMaterial;
	private double _cursorTime;

	/// <summary>Radius of the light sphere in metres.</summary>
	private const float LightRadius = 0.025f;
	/// <summary>White sphere marking the point light; a child of the root (world space), so grab-scaling the model does not resize it.</summary>
	private MeshInstance3D _lightSphere;
	/// <summary>Translucent shell around the light sphere, shown while it is being dragged.</summary>
	private MeshInstance3D _lightHalo;
	/// <summary>True while the right grip is dragging the light instead of the model.</summary>
	private bool _draggingLight;
	/// <summary>True when the light sphere is on (white); when off (black) the viewer's head is the light source.</summary>
	private bool _lightOn = true;
	private StandardMaterial3D _lightMaterial;
	/// <summary>Right trigger state on the previous frame, for edge detection.</summary>
	private bool _triggerWasDown;
	/// <summary>Light position in the right controller's local space, captured when the drag started.</summary>
	private Vector3 _lightGrabOffset;

	/// <summary>Uploads the model's segments to GPU textures and keeps them in sync with edits.</summary>
	private VoxelBridge _voxelBridge;
	/// <summary>Material running voxel_raymarch.gdshader; carries every shader uniform.</summary>
	private ShaderMaterial _raymarchMaterial;
	/// <summary>256x1 RGBA8 palette lookup texture bound to the shader.</summary>
	private ImageTexture _paletteTexture;

	private SegmentedBrickModel _model;
	/// <summary>256 palette colours, each a uint packed as 0xRRGGBBAA ("rgba8888, big endian", see VoxFileModel.Argb2rgba). Entry 0 is empty.</summary>
	private uint[] _palette;

	/// <summary>Maps voxel space to world space: scale is <see cref="MetresPerVoxel"/>, position puts the model centre at <see cref="ModelWorldCenter"/>.</summary>
	private Node3D _modelPivot;
	/// <summary>
	/// Box mesh instance covering the model's bounds, child of <see cref="_modelPivot"/>. It is only the
	/// rasterisation footprint: the shader ignores its geometry and traces rays through the voxels inside it.
	/// </summary>
	private MeshInstance3D _boundingBox;
	private BoxMesh _boxMesh;

	// Toggling voxel demo state (adds/removes the voxel at (0,0,0) once a second)
	private double _toggleTimer;
	private bool _voxelPresent;
	/// <summary>Last segment count printed, so the log only reports changes.</summary>
	private int _lastSegmentCount;

	public override void _Ready()
	{
		SetupDisplay();
		LoadModel();
		SetupRaymarchBox();
		SetupLight();
		SetupShaderUniforms();
		// Subscribe to model changes to update shader when data changes
		_model.OnSegmentLoaded += _ => UpdateShaderSegments();
		_model.OnSegmentUnloaded += _ => UpdateShaderSegments();
		_model.OnBrickDirty += (_, _, _) => UpdateShaderSegments();
		_model.OnBoundsChanged += HandleBoundsChanged;
		GD.Print(_vrActive
			? "Demo ready in VR. Toggling voxel at (0,0,0) every second to demonstrate real-time editing."
			: "Demo ready on the flat screen. Right-click + WASDQE to fly around.");
	}

	#region Display

	/// <summary>
	/// Picks VR or flat screen. VR is used when the OpenXR interface initialises; otherwise (or with
	/// --flatscreen / --no-vr) a free-look camera is used so the shader can still be tested at a desk.
	/// </summary>
	private void SetupDisplay()
	{
		RenderingServer.SetDefaultClearColor(new Color(0.1f, 0.1f, 0.12f));
		// Arguments after "--" are only in the user args, so check both lists
		static bool IsFlatArg(string a) => a is "--flatscreen" or "--no-vr";
		bool forceFlat = Array.Exists(OS.GetCmdlineArgs(), IsFlatArg) || Array.Exists(OS.GetCmdlineUserArgs(), IsFlatArg);
		XRInterface xr = forceFlat ? null : XRServer.FindInterface("OpenXR");
		if (xr is not null && xr.Initialize())
		{
			GD.Print("OpenXR initialized successfully");
			// The headset display paces the frame loop; desktop vsync would only add latency.
			DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
			GetViewport().UseXR = true;
			_vrActive = true;
			EnablePassthrough(xr);
			SetupXRRig();
		}
		else
		{
			GD.Print(forceFlat
				? "Flatscreen mode forced"
				: "OpenXR not available, using flatscreen mode");
			SetupFlatCamera();
		}
	}

	/// <summary>
	/// Shows the real world behind the model if the runtime supports it, otherwise leaves the dark
	/// background. Two routes: the vendor passthrough API (Meta, via the OpenXR vendors plugin), or the
	/// standard OpenXR alpha-blend environment mode. Either way the viewport background must be
	/// transparent so the empty pixels are left for the compositor to fill with the camera image. The
	/// raymarch shader discards misses and outputs opaque hits, so the model itself is unaffected.
	/// </summary>
	private void EnablePassthrough(XRInterface xr)
	{
		bool enabled = false;
		if (xr.IsPassthroughSupported())
			enabled = xr.StartPassthrough();
		if (!enabled && xr.GetSupportedEnvironmentBlendModes().Contains((int)XRInterface.EnvironmentBlendModeEnum.AlphaBlend))
		{
			xr.EnvironmentBlendMode = XRInterface.EnvironmentBlendModeEnum.AlphaBlend;
			enabled = true;
		}
		GetViewport().TransparentBg = enabled;
		GD.Print(enabled ? "Passthrough enabled" : "Passthrough not available; using opaque background");
	}

	/// <summary>Builds the tracked rig: origin at floor level, head camera, and both controllers with small marker cubes.</summary>
	private void SetupXRRig()
	{
		_xrOrigin = new XROrigin3D { Name = "XROrigin" };
		_xrCamera = new XRCamera3D { Name = "XRCamera", Current = true };
		_xrOrigin.AddChild(_xrCamera);
		_leftController = CreateController("LeftController", "left_hand");
		_rightController = CreateController("RightController", "right_hand");
		_xrOrigin.AddChild(_leftController);
		_xrOrigin.AddChild(_rightController);
		AddChild(_xrOrigin);
	}

	private static XRController3D CreateController(string name, string tracker)
	{
		XRController3D controller = new()
		{
			Name = name,
			Tracker = tracker,
			Pose = "grip",
		};
		controller.AddChild(new MeshInstance3D
		{
			Name = "Marker",
			Mesh = new BoxMesh { Size = new Vector3(0.04f, 0.04f, 0.1f) },
			MaterialOverride = new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				AlbedoColor = new Color(0.7f, 0.7f, 0.75f),
			},
		});
		return controller;
	}

	/// <summary>Desktop fallback: free-look camera a metre from the model, looking at it.</summary>
	private void SetupFlatCamera()
	{
		_flatCamera = new FreeLookCamera
		{
			Name = "FlatCamera",
			Position = ModelWorldCenter + new Vector3(0f, 0.2f, 1.0f),
		};
		AddChild(_flatCamera);
		_flatCamera.LookAt(ModelWorldCenter);
		_flatCamera.Near = 0.02f;
		_flatCamera.Enabled = true; // after AddChild so it can find the previous camera
	}

	#endregion

	#region Grab manipulation

	/// <summary>
	/// Grip-to-grab. While one grip is held the model follows that controller (position and orientation).
	/// While both are held it follows the midpoint between them, is oriented by the line between the hands
	/// (with roll taken from the controllers' up directions) and is scaled by the change in hand distance.
	/// All cases use one "grab frame" whose basis is scaled by the hand distance; the pivot is moved by
	/// frame_now * frame_start^-1, so the model stays rigidly attached to the frame. Pressing or releasing a
	/// grip changes the frame's definition, so the grab is re-baselined at that moment to avoid a jump.
	/// </summary>
	private void UpdateGrab()
	{
		if (_leftController is null)
			return;
		bool left = _leftController.IsButtonPressed("grip_click");
		bool right = _rightController.IsButtonPressed("grip_click");
		if (UpdateLightDrag(left, right))
			return;
		int mask = (left ? 1 : 0) | (right ? 2 : 0);
		if (mask == 0)
		{
			_gripMask = 0;
			return;
		}
		Transform3D frame = GetGrabFrame(left, right);
		if (mask != _gripMask)
		{
			_gripMask = mask;
			_grabFrameStart = frame;
			_pivotStart = _modelPivot.GlobalTransform;
			return;
		}
		_modelPivot.GlobalTransform = frame * _grabFrameStart.AffineInverse() * _pivotStart;
	}

	/// <summary>
	/// Light dragging takes priority over model grabbing. A drag starts when the right grip goes down (with the
	/// left not held) while the cursor intersects the light sphere, and ends when that grip is released. While
	/// dragging, the model grab is suspended and its state reset, so a still-held left grip re-baselines cleanly
	/// afterwards. Returns true if the model grab should be skipped this frame.
	/// </summary>
	private bool UpdateLightDrag(bool left, bool right)
	{
		if (_draggingLight && !right)
			_draggingLight = false;
		else if (!_draggingLight && right && !left && _gripMask == 0 && CursorTouchesLight())
		{
			_draggingLight = true;
			_lightGrabOffset = _rightController.GlobalTransform.AffineInverse() * _lightSphere.GlobalPosition;
		}
		if (!_draggingLight)
			return false;
		_gripMask = 0;
		_lightSphere.GlobalPosition = _rightController.GlobalTransform * _lightGrabOffset;
		return true;
	}

	/// <summary>True if the (voxel-snapped) edit cursor cube overlaps the light sphere. Tested in voxel space, where the cursor is an axis-aligned unit cube.</summary>
	private bool CursorTouchesLight()
	{
		if (_cursor is null || !_rightController.GetHasTrackingData())
			return false;
		Vector3 centre = _modelPivot.ToLocal(_lightSphere.GlobalPosition);
		float radius = LightRadius / _modelPivot.GlobalTransform.Basis.Scale.X;
		Vector3 half = Vector3.One * 0.5f;
		Vector3 closest = centre.Clamp(_cursor.Position - half, _cursor.Position + half);
		return closest.DistanceSquaredTo(centre) <= radius * radius;
	}

	/// <summary>
	/// Where the shader's light is in world space: the sphere when it is on, otherwise the viewer's head (the
	/// XR camera is the midpoint between the eyes; on the flat screen it is the free-look camera).
	/// </summary>
	private Vector3 GetLightWorldPosition()
	{
		if (_lightOn)
			return _lightSphere.GlobalPosition;
		return (_vrActive ? _xrCamera : (Node3D)_flatCamera).GlobalPosition;
	}

	private Transform3D GetGrabFrame(bool left, bool right)
	{
		if (!(left && right))
			return (left ? _leftController : _rightController).GlobalTransform;
		Transform3D l = _leftController.GlobalTransform;
		Transform3D r = _rightController.GlobalTransform;
		Vector3 across = r.Origin - l.Origin;
		float distance = Mathf.Max(across.Length(), 0.01f);
		Vector3 x = across / distance;
		Vector3 up = l.Basis.Y + r.Basis.Y;
		Vector3 z = x.Cross(up);
		if (z.LengthSquared() < 1e-6f)
			z = x.Cross(Vector3.Up); // controllers' up is parallel to the hand line; any perpendicular will do
		z = z.Normalized();
		Vector3 y = z.Cross(x);
		return new Transform3D(new Basis(x, y, z).Scaled(Vector3.One * distance), (l.Origin + r.Origin) / 2);
	}

	#endregion

	#region Model

	/// <summary>Loads the embedded Sora.vox and builds the segmented model and its GPU bridge.</summary>
	private void LoadModel()
	{
		GD.Print("Loading voxel model...");
		using System.IO.Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Sora.vox")
			?? throw new InvalidOperationException("Embedded resource Sora.vox not found");
		VoxFileModel vox = new(stream);
		_palette = vox.Palette; // 256 RGBA8888 colors
		_model = new SegmentedBrickModel(vox);
		_voxelBridge = new(_model);
		GD.Print($"Model size: ({_model.SizeX}, {_model.SizeY}, {_model.SizeZ}), active segments: {_voxelBridge.ActiveSegmentCount}");
	}

	/// <summary>
	/// Creates the pivot and the bounding box mesh sized to the model's bounds and assigns the raymarch
	/// shader material. The BoxMesh is centred on its own origin, so the instance's Position is the box
	/// centre in voxel space, which is why the shader adds box_center to mesh-local coordinates.
	/// </summary>
	private void SetupRaymarchBox()
	{
		(Vector3 center, Vector3 size) = CalculateBoundingBox(_model.CurrentBounds);
		GD.Print($"Voxel-space bounds (Y-up): center={center}, size={size}");
		_modelPivot = new Node3D
		{
			Name = "ModelPivot",
			Scale = Vector3.One * MetresPerVoxel,
			// Put the model's centre at ModelWorldCenter
			Position = ModelWorldCenter - center * MetresPerVoxel,
		};
		AddChild(_modelPivot);
		SetupCursor();
		_boxMesh = new BoxMesh { Size = size };
		_raymarchMaterial = new ShaderMaterial { Shader = GD.Load<Shader>("res://voxel_raymarch.gdshader") };
		_boundingBox = new MeshInstance3D
		{
			Name = "VoxelBox",
			Mesh = _boxMesh,
			Position = center,
			MaterialOverride = _raymarchMaterial,
			// This is a raymarched volume, not geometry
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
		_modelPivot.AddChild(_boundingBox);
		SetBoxUniforms(center, size);
	}

	/// <summary>
	/// Creates the edit cursor: a translucent one-voxel cube that will preview where a voxel would be added
	/// or removed. It is ordinary geometry in the transparent pass, which runs after the opaque pass where the
	/// raymarch shader has written DEPTH at each hit voxel, so the depth test against the model just works.
	/// Being a child of the pivot it is one voxel in size at any grab scale.
	/// </summary>
	private void SetupCursor()
	{
		_cursorMaterial = new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			AlbedoColor = new Color(1f, 0.9f, 0.2f, 0.45f),
			CullMode = BaseMaterial3D.CullModeEnum.Disabled,
		};
		_cursor = new MeshInstance3D
		{
			Name = "EditCursor",
			Mesh = new BoxMesh { Size = Vector3.One * 1.02f }, // a hair oversize so it does not z-fight voxel faces
			MaterialOverride = _cursorMaterial,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			Visible = false, // until a controller exists
		};
		_modelPivot.AddChild(_cursor);
	}

	/// <summary>Creates the white light sphere, floating up and to the right of the model.</summary>
	private void SetupLight()
	{
		_lightMaterial = new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			AlbedoColor = Colors.White,
		};
		_lightSphere = new MeshInstance3D
		{
			Name = "LightSphere",
			Mesh = new SphereMesh { Radius = LightRadius, Height = LightRadius * 2f },
			MaterialOverride = _lightMaterial,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			Position = ModelWorldCenter + new Vector3(0.2f, 0.2f, 0.1f),
		};
		_lightHalo = new MeshInstance3D
		{
			Name = "LightHalo",
			Mesh = new SphereMesh { Radius = LightRadius * 2f, Height = LightRadius * 4f },
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
		_lightSphere.AddChild(_lightHalo);
		AddChild(_lightSphere);
	}

	/// <summary>
	/// Snaps the cursor to the voxel containing the right controller's tip and blinks it between two
	/// looks: depth-tested (parts behind the model are hidden) and drawn over everything (x-ray), so
	/// you can tell both where it is and whether the model is in front of it.
	/// </summary>
	private void UpdateCursor(double delta)
	{
		if (_cursor is null || _rightController is null)
			return;
		Vector3 tip = (_rightController.GlobalTransform * new Vector3(0f, 0f, -TipOffset));
		Vector3 voxel = _modelPivot.ToLocal(tip); // voxel space, one unit per voxel
		_cursor.Position = voxel.Floor() + Vector3.One * 0.5f;
		// Over the light the cursor gives way to the halo, which stays up during the drag
		bool overLight = _draggingLight || CursorTouchesLight();
		_cursor.Visible = _rightController.GetHasTrackingData() && !overLight;
		_lightHalo.Visible = overLight;
		bool triggerDown = _rightController.IsButtonPressed("trigger_click");
		if (triggerDown && !_triggerWasDown && overLight)
		{
			_lightOn = !_lightOn;
			_lightMaterial.AlbedoColor = _lightOn ? Colors.White : Colors.Black;
		}
		_triggerWasDown = triggerDown;
		_cursorTime += delta;
		_cursorMaterial.NoDepthTest = (int)(_cursorTime / CursorBlinkPeriod) % 2 == 1;
	}

	/// <summary>
	/// Calculates bounding box center and size in voxel space (Y-up) from model bounds (Z-up).
	/// The max is extended by 2 voxels because bounds are stored at brick granularity (2x2x2 voxels), so
	/// the last brick may contain voxels up to max + 1. Corners are therefore always integer coordinates.
	/// Returns a zero-sized box at the origin if the model has no bounds yet.
	/// </summary>
	private static (Vector3 Center, Vector3 Size) CalculateBoundingBox(SegmentedBrickModel.Bounds? bounds)
	{
		if (!bounds.HasValue)
			return (Vector3.Zero, Vector3.Zero);
		// (X, Y, Z)_zup -> (X, Z, Y)_yup
		SegmentedBrickModel.Bounds b = bounds.Value;
		Vector3 min = new(b.MinX, b.MinZ, b.MinY);
		Vector3 max = new(b.MaxX + 2, b.MaxZ + 2, b.MaxY + 2);
		return ((min + max) / 2, max - min);
	}

	/// <summary>
	/// Handles bounds changes (growth from edits, or shrinkage after GPU trimming) by resizing and
	/// repositioning the raymarching bounding box and updating the shader's copy of it. The pivot is
	/// left alone so the voxels do not move in the world when the bounds change.
	/// </summary>
	private void HandleBoundsChanged(SegmentedBrickModel.Bounds? oldBounds, SegmentedBrickModel.Bounds newBounds)
	{
		if (_boundingBox is null || _boxMesh is null)
			return;
		(Vector3 center, Vector3 size) = CalculateBoundingBox(newBounds);
		_boxMesh.Size = size;
		_boundingBox.Position = center;
		SetBoxUniforms(center, size);
		GD.Print($"Bounds changed: {oldBounds} -> {newBounds}");
	}

	/// <summary>
	/// Tells the shader the bounding box in voxel space; it intersects rays with this box analytically
	/// to find where each ray enters the volume. Must be called whenever the BoxMesh size or the
	/// instance Position changes, or rays will start in the wrong place.
	/// </summary>
	private void SetBoxUniforms(Vector3 center, Vector3 size)
	{
		_raymarchMaterial.SetShaderParameter("box_center", center);
		_raymarchMaterial.SetShaderParameter("box_size", size);
	}

	#endregion

	#region Shader uniforms

	/// <summary>Binds the segment count, directory texture, per-segment brick textures and palette to the shader.</summary>
	private void SetupShaderUniforms()
	{
		UpdateShaderSegments();
		SetupPaletteTexture();
	}

	/// <summary>
	/// Updates shader uniforms when segments are added or removed (also called from the model's
	/// segment and brick events). The brick textures are bound as an array in directory order, so the
	/// directory's texture index for a segment is its position in that array. The array holds at most
	/// <see cref="VoxelBridge.MaxSegments"/> textures (a temporary limit; see the note on that constant).
	/// </summary>
	private void UpdateShaderSegments()
	{
		if (_raymarchMaterial is null || _voxelBridge is null)
			return;
		_raymarchMaterial.SetShaderParameter("active_segment_count", _voxelBridge.ActiveSegmentCount);
		if (_voxelBridge.DirectoryTexture is not null)
			_raymarchMaterial.SetShaderParameter("segment_directory", _voxelBridge.DirectoryTexture);
		Godot.Collections.Array<Texture3D> textureArray = [];
		foreach (VoxelBridge.SegmentEntry entry in _voxelBridge.Directory)
		{
			uint segmentId = ((uint)entry.X << 18) | ((uint)entry.Y << 9) | entry.Z;
			if (_voxelBridge.Textures.TryGetValue(segmentId, out ImageTexture3D texture))
				textureArray.Add(texture);
			else
				GD.PrintErr($"Texture not found for segment ({entry.X}, {entry.Y}, {entry.Z})");
		}
		_raymarchMaterial.SetShaderParameter("segment_bricks", textureArray);
		if (_voxelBridge.ActiveSegmentCount != _lastSegmentCount)
		{
			_lastSegmentCount = _voxelBridge.ActiveSegmentCount;
			GD.Print($"Shader updated: {_voxelBridge.ActiveSegmentCount} segments");
		}
	}

	/// <summary>
	/// Builds the 256x1 RGBA8 palette texture. The model stores each colour as a 0xRRGGBBAA uint.
	/// Reversing the endianness makes it 0xAABBGGRR, whose little-endian bytes in memory are R, G, B, A,
	/// which is the byte order an RGBA8 image expects; the raw byte copy below relies on that, so the
	/// two steps must be changed together. The shader reads this texture as sRGB (source_color).
	/// </summary>
	private void SetupPaletteTexture()
	{
		uint[] converted = new uint[256];
		for (int i = 0; i < 256; i++)
			converted[i] = BinaryPrimitives.ReverseEndianness(_palette[i]);
		byte[] paletteData = MemoryMarshal.AsBytes(converted.AsSpan()).ToArray();
		_paletteTexture = ImageTexture.CreateFromImage(Image.CreateFromData(256, 1, false, Image.Format.Rgba8, paletteData));
		_raymarchMaterial.SetShaderParameter("palette", _paletteTexture);
	}

	#endregion

	/// <summary>
	/// Per-frame work: lets the bridge finish any pending GPU bounds trim, refreshes the inverse model
	/// matrix the shader needs, and runs the toggling-voxel editing demo.
	/// </summary>
	public override void _Process(double delta)
	{
		_voxelBridge?.Poll();
		UpdateGrab();
		UpdateCursor(delta);
		// The fragment stage has no inverse of MODEL_MATRIX, so supply it every frame. Reading it from
		// the box's GlobalTransform includes the pivot, so moving or scaling the pivot just works.
		_raymarchMaterial?.SetShaderParameter("inv_model_matrix", new Projection(_boundingBox.GlobalTransform.AffineInverse()));
		_raymarchMaterial?.SetShaderParameter("light_position", _modelPivot.ToLocal(GetLightWorldPosition()));
		_toggleTimer += delta;
		if (_toggleTimer >= 1.0)
		{
			_toggleTimer -= 1.0;
			_voxelPresent = !_voxelPresent;
			_model[0, 0, 0] = _voxelPresent ? (byte)1 : (byte)0;
			GD.Print($"Voxel at (0,0,0): {(_voxelPresent ? "ADDED" : "REMOVED")}");
		}
	}

	public override void _ExitTree() => _voxelBridge?.Dispose();
}
