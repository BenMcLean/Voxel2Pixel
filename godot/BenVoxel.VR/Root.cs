using System;
using System.Buffers.Binary;
using System.Reflection;
using System.Runtime.InteropServices;
using BenVoxel.FileToVoxCore;
using BenVoxel.Models;
using BenVoxel.VR.Props;
using BenVoxel.VR.Tools;
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
	/// <summary>Both hands and everything they can touch; null on the flat screen.</summary>
	private InteractionManager _interaction;
	private FreeLookCamera _flatCamera;

	/// <summary>Seconds per blink phase of the edit cursor.</summary>
	private const double CursorBlinkPeriod = 0.4;
	/// <summary>One-voxel translucent cube, child of <see cref="_modelPivot"/> and snapped to the voxel under the right controller's tip.</summary>
	private MeshInstance3D _cursor;
	private StandardMaterial3D _cursorMaterial;
	private double _cursorTime;

	/// <summary>World position of the ortho-sprite preview's centre when the demo starts: to the left of the model.</summary>
	private static readonly Vector3 SpriteWorldCenter = ModelWorldCenter + new Vector3(-0.4f, 0f, 0f);
	/// <summary>Non-editable ortho-sprite preview of the same model; also a grab target.</summary>
	private VoxelOrthoSprite _sprite;
	/// <summary>Model bounds in voxel space (Y-up), kept for the sprite and for grab hit-testing.</summary>
	private Vector3 _boxCenter;
	private Vector3 _boxSize;
	/// <summary>Where a summoned toy box lands: this far beyond the hand, and this far below it, in metres.</summary>
	private const float ToyBoxSummonDistance = 0.12f;
	private const float ToyBoxSummonDrop = 0.05f;
	/// <summary>Null until first summoned.</summary>
	private ToyBox _toyBox;
	/// <summary>The point light, and what a trigger on it does. Null until a way to spawn it exists, so for now the viewer's head lights the model.</summary>
	private LightProp _light;
	/// <summary>The tool menu, opened by clicking either thumbstick; null on the flat screen.</summary>
	private RadialMenu _menu;
	private EditTool _tool = EditTool.Pencil;

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
	private VoxelModelProp _modelPivot;
	/// <summary>
	/// Box mesh instance covering the model's bounds, child of <see cref="_modelPivot"/>. It is only the
	/// rasterisation footprint: the shader ignores its geometry and traces rays through the voxels inside it.
	/// </summary>
	private MeshInstance3D _boundingBox;
	private BoxMesh _boxMesh;

	/// <summary>Palette index written by the pencil. Index 0 is empty, so this must be 1 to 255.</summary>
	private byte _paintColor = 1;
	/// <summary>Last segment count printed, so the log only reports changes.</summary>
	private int _lastSegmentCount;

	public override void _Ready()
	{
		SetupDisplay();
		LoadModel();
		SetupRaymarchBox();
		SetupInteractables();
		SetupMenu();
		SetupShaderUniforms();
		// Subscribe to model changes to update shader when data changes
		_model.OnSegmentLoaded += _ => UpdateShaderSegments();
		_model.OnSegmentUnloaded += _ => UpdateShaderSegments();
		_model.OnBrickDirty += (_, _, _) => UpdateShaderSegments();
		_model.OnBoundsChanged += HandleBoundsChanged;
		GD.Print(_vrActive
			? "Ready in VR. Right trigger adds voxels at the cursor, A removes them."
			: "Ready on the flat screen. Right-click + WASDQE to fly around.");
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

	/// <summary>Builds the tracked rig: origin at floor level, head camera, and both controllers with small marker cubes, and the interaction manager that reads them.</summary>
	private void SetupXRRig()
	{
		_xrOrigin = new XROrigin3D { Name = "XROrigin" };
		_xrCamera = new XRCamera3D { Name = "XRCamera", Current = true };
		_xrOrigin.AddChild(_xrCamera);
		XRController3D leftController = CreateController("LeftController", "left_hand");
		XRController3D rightController = CreateController("RightController", "right_hand");
		_xrOrigin.AddChild(leftController);
		_xrOrigin.AddChild(rightController);
		AddChild(_xrOrigin);
		_interaction = new InteractionManager(new Hand(leftController), new Hand(rightController));
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
		_modelPivot = new VoxelModelProp
		{
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
	/// Creates the ortho-sprite preview. Its node maps voxel space to metres like the model pivot, and it is
	/// positioned so the model's centre lands on <see cref="SpriteWorldCenter"/>.
	/// </summary>
	private void SetupSprite()
	{
		_sprite = new VoxelOrthoSprite
		{
			Scale = Vector3.One * MetresPerVoxel,
			Position = SpriteWorldCenter - _boxCenter * MetresPerVoxel,
		};
		AddProp(_sprite);
		SetBoxUniforms(_boxCenter, _boxSize);
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

	/// <summary>Puts a prop in the world and makes it grabbable. This is all a new prop needs.</summary>
	private void AddProp(Prop prop)
	{
		AddChild(prop);
		_interaction?.Register(prop);
	}

	/// <summary>
	/// Registers what the hands can touch from the start: the model, which is also what a grab that touches nothing carries.
	/// </summary>
	private void SetupInteractables()
	{
		if (_interaction is null)
			return;
		_interaction.Register(_modelPivot, fallback: true);
	}

	/// <summary>
	/// Fills the tool menu. Each entry is an icon and what choosing it does; the menu lays out any number of entries,
	/// so a new tool is one more line here.
	/// </summary>
	private void SetupMenu()
	{
		if (_interaction is null)
			return;
		_menu = new RadialMenu { Name = "ToolMenu" };
		AddChild(_menu);
		_menu.AddItem("Pencil", new ModelProp("res://assets/pencil/pencil.glb", 1f), _ => _tool = EditTool.Pencil, tiltDegrees: 45f);
		_menu.AddItem("Select", new WireframeCube(), _ => _tool = EditTool.Select);
		_menu.AddItem("Toybox", new ToyBox(), SummonToyBox);
	}

	/// <summary>
	/// Fills the toy box. Each entry is an icon, the kind of grab its prop takes, and how to bring the prop into the
	/// world under a hand and take it out again; the box lays out any number of entries, so a new prop is one more call.
	/// </summary>
	private void SetupToyBoxItems(ToyBox box)
	{
		box.AddItem("Light", () => MiniIcon.LightSphere(LightProp.Radius), GrabMode.Drag, SpawnLight, DespawnLight);
		// The pencil stands in for props that will have real functions
		box.AddItem("Pencil", () => new ModelProp("res://assets/pencil/pencil.glb", 1f), GrabMode.Free, SpawnPencil, DespawnProp, tiltDegrees: 45f);
	}

	/// <summary>There is only one light; asking for it again brings it to the hand.</summary>
	private IInteractable SpawnLight(Hand hand)
	{
		if (_light is null)
		{
			_light = new LightProp(hand.TipWorld, CellTouchesSphere);
			AddChild(_light.Node);
			_interaction.Register(_light);
		}
		_light.Node.GlobalPosition = hand.TipWorld;
		return _light;
	}

	private void DespawnLight(IInteractable light)
	{
		_interaction.Unregister(light);
		light.Node.QueueFree();
		_light = null;
	}

	private IInteractable SpawnPencil(Hand hand)
	{
		ModelProp pencil = new("res://assets/pencil/pencil.glb", 0.2f);
		AddProp(pencil);
		pencil.GlobalPosition = hand.TipWorld;
		return pencil;
	}

	private void DespawnProp(IInteractable prop)
	{
		_interaction.Unregister(prop);
		prop.Node.QueueFree();
	}

	/// <summary>Moves the toy box to just beyond a hand, as seen by the viewer, turned to face them. It is created the first time.</summary>
	private void SummonToyBox(Hand hand)
	{
		Vector3 head = GetHeadWorldPosition();
		Vector3 away = hand.TipWorld - head;
		away.Y = 0f;
		away = away.LengthSquared() > 1e-4f ? away.Normalized() : Vector3.Forward;
		Vector3 position = hand.TipWorld + away * ToyBoxSummonDistance;
		position.Y = hand.TipWorld.Y - ToyBoxSummonDrop; // the chest's origin is the centre of its base
		if (_toyBox is null)
		{
			_toyBox = new ToyBox { Interaction = _interaction };
			SetupToyBoxItems(_toyBox);
			AddProp(_toyBox);
			_interaction.Released += _toyBox.OnReleased;
		}
		_toyBox.GlobalPosition = position;
		// The chest's front is +Z
		_toyBox.GlobalRotation = new Vector3(0f, Mathf.Atan2(head.X - position.X, head.Z - position.Z), 0f);
	}

	/// <summary>The centre, in voxel space, of the voxel containing a world point.</summary>
	private Vector3 SnapToVoxel(Vector3 world) => _modelPivot.ToLocal(world).Floor() + Vector3.One * 0.5f;

	/// <summary>True if the voxel containing <paramref name="tip"/> overlaps a sphere. Tested in voxel space, where the voxel is an axis-aligned unit cube.</summary>
	private bool CellTouchesSphere(Vector3 tip, Vector3 sphereWorld, float radiusMetres)
	{
		Vector3 cell = SnapToVoxel(tip);
		Vector3 centre = _modelPivot.ToLocal(sphereWorld);
		float radius = radiusMetres / _modelPivot.GlobalTransform.Basis.Scale.X;
		Vector3 half = Vector3.One * 0.5f;
		Vector3 closest = centre.Clamp(cell - half, cell + half);
		return closest.DistanceSquaredTo(centre) <= radius * radius;
	}

	/// <summary>
	/// Where the shader's light is in world space: the sphere when it exists and is on, otherwise the viewer's head (the
	/// XR camera is the midpoint between the eyes; on the flat screen it is the free-look camera).
	/// </summary>
	private Vector3 GetLightWorldPosition() => _light is { On: true } ? _light.WorldPosition : GetHeadWorldPosition();

	/// <summary>The viewer's position: the XR camera (midpoint between the eyes) in VR, otherwise the free-look camera.</summary>
	private Vector3 GetHeadWorldPosition() => (_vrActive ? _xrCamera : (Node3D)_flatCamera).GlobalPosition;

	/// <summary>
	/// Snaps the cursor to the voxel containing the right controller's tip and blinks it between two
	/// looks: depth-tested (parts behind the model are hidden) and drawn over everything (x-ray), so
	/// you can tell both where it is and whether the model is in front of it.
	/// </summary>
	private void UpdateCursor(double delta)
	{
		if (_cursor is null || _interaction is null)
			return;
		Hand hand = _interaction.Primary;
		_cursor.Position = SnapToVoxel(hand.TipWorld);
		// Over the light the cursor gives way to the halo, and it only belongs to the pencil, which the menu replaces while open
		_cursor.Visible = hand.HasTracking && _tool == EditTool.Pencil && _menu is not { IsOpen: true }
			&& hand.Hovered is not { SuppressesEditCursor: true };
		_cursorTime += delta;
		_cursorMaterial.NoDepthTest = (int)(_cursorTime / CursorBlinkPeriod) % 2 == 1;
	}

	/// <summary>
	/// While the edit cursor is showing, the primary hand's trigger writes <see cref="_paintColor"/> into the voxel
	/// under it and A clears it. Both act every frame they are held, so sweeping the hand draws a line. A write that
	/// would not change the voxel is skipped, because every change re-uploads the whole segment to the GPU.
	/// </summary>
	private void ApplyPencil()
	{
		if (_cursor is not { Visible: true })
			return;
		Hand hand = _interaction.Primary;
		bool add = hand.TriggerPressed, remove = hand.AxPressed;
		if (add == remove)
			return; // neither, or both
		// Voxel space is Y-up, storage is Z-up: (x, y, z)_yup is (x, z, y)_zup
		Vector3 cell = (_cursor.Position - Vector3.One * 0.5f).Round();
		if (cell.X < 0 || cell.Y < 0 || cell.Z < 0 || cell.X > ushort.MaxValue || cell.Y > ushort.MaxValue || cell.Z > ushort.MaxValue)
			return;
		ushort x = (ushort)cell.X, y = (ushort)cell.Z, z = (ushort)cell.Y;
		byte value = add ? _paintColor : (byte)0;
		if (_model[x, y, z] == value)
			return;
		_model[x, y, z] = value;
		hand.Pulse(0.2f, 0.02f);
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
		_boxCenter = center;
		_boxSize = size;
		_modelPivot.BoxCenter = center;
		_modelPivot.BoxSize = size;
		if (_sprite is not null)
		{
			_sprite.BoxCenter = center;
			_sprite.BoxSize = size;
		}
		SetSharedParameter("box_center", center);
		SetSharedParameter("box_size", size);
	}

	#endregion

	#region Shader uniforms

	/// <summary>Sets a uniform on both voxel materials: the editable raymarcher and the ortho sprite. Both read the same voxel data.</summary>
	private void SetSharedParameter(StringName name, Variant value)
	{
		_raymarchMaterial?.SetShaderParameter(name, value);
		_sprite?.Material.SetShaderParameter(name, value);
	}

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
		SetSharedParameter("active_segment_count", _voxelBridge.ActiveSegmentCount);
		if (_voxelBridge.DirectoryTexture is not null)
			SetSharedParameter("segment_directory", _voxelBridge.DirectoryTexture);
		Godot.Collections.Array<Texture3D> textureArray = [];
		foreach (VoxelBridge.SegmentEntry entry in _voxelBridge.Directory)
		{
			uint segmentId = ((uint)entry.X << 18) | ((uint)entry.Y << 9) | entry.Z;
			if (_voxelBridge.Textures.TryGetValue(segmentId, out ImageTexture3D texture))
				textureArray.Add(texture);
			else
				GD.PrintErr($"Texture not found for segment ({entry.X}, {entry.Y}, {entry.Z})");
		}
		SetSharedParameter("segment_bricks", textureArray);
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
		SetSharedParameter("palette", _paletteTexture);
	}

	#endregion

	/// <summary>
	/// Per-frame work: lets the bridge finish any pending GPU bounds trim, refreshes the inverse model
	/// matrix the shader needs, and applies the pencil.
	/// </summary>
	public override void _Process(double delta)
	{
		_voxelBridge?.Poll();
		_interaction?.Update();
		if (_menu is not null)
		{
			Vector3 head = GetHeadWorldPosition();
			_menu.Update(_interaction.Left, head);
			_menu.Update(_interaction.Right, head);
		}
		_toyBox?.Face(GetHeadWorldPosition());
		UpdateCursor(delta);
		ApplyPencil();
		// The fragment stage has no inverse of MODEL_MATRIX, so supply it every frame. Reading it from
		// the box's GlobalTransform includes the pivot, so moving or scaling the pivot just works.
		_raymarchMaterial?.SetShaderParameter("inv_model_matrix", new Projection(_boundingBox.GlobalTransform.AffineInverse()));
		Vector3 lightWorld = GetLightWorldPosition();
		_raymarchMaterial?.SetShaderParameter("light_position", _modelPivot.ToLocal(lightWorld));
		_sprite?.UpdateView(GetHeadWorldPosition(), lightWorld);
	}

	/// <summary>
	/// Screenshot keys: F9 and Ctrl+Shift+S. Steam's own F12 only works for apps it launched, and it may take other
	/// function keys too, hence the second one.
	/// </summary>
	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (@event is InputEventKey { Pressed: true, Echo: false } key
			&& (key.Keycode == Key.F9 || (key.Keycode == Key.S && key.CtrlPressed && key.ShiftPressed)))
			SaveScreenshot();
	}

	/// <summary>Offscreen viewport and camera for screenshots; null until the first one. The XR render target cannot be read back, so the scene is drawn again here from the head's pose.</summary>
	private SubViewport _shotViewport;
	private Camera3D _shotCamera;
	private bool _shotBusy;

	private const int ScreenshotWidth = 1920;
	private const int ScreenshotHeight = 1080;
	private const float ScreenshotFov = 90f;

	/// <summary>
	/// Renders the scene once from the viewer's head with a flat camera and saves it as a PNG in the user data folder,
	/// logging the path or the failure. It shows what the head sees, not the exact stereo image in the headset.
	/// </summary>
	private async void SaveScreenshot()
	{
		if (_shotBusy)
			return;
		_shotBusy = true;
		if (_shotViewport is null)
		{
			_shotViewport = new SubViewport
			{
				Name = "ScreenshotViewport",
				Size = new Vector2I(ScreenshotWidth, ScreenshotHeight),
				RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled,
			};
			_shotCamera = new Camera3D { Name = "ScreenshotCamera", Fov = ScreenshotFov, Near = 0.02f, Current = true };
			_shotViewport.AddChild(_shotCamera);
			AddChild(_shotViewport);
		}
		_shotCamera.GlobalTransform = (_vrActive ? _xrCamera : (Node3D)_flatCamera).GlobalTransform;
		_shotViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
		await ToSignal(RenderingServer.Singleton, RenderingServerInstance.SignalName.FramePostDraw);
		string path = $"user://screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
		Error error = _shotViewport.GetTexture().GetImage().SavePng(path);
		GD.Print(error == Error.Ok
			? $"Screenshot saved: {ProjectSettings.GlobalizePath(path)}"
			: $"Screenshot failed ({error}): {path}");
		_shotBusy = false;
	}

	public override void _ExitTree() => _voxelBridge?.Dispose();
}
