using System;
using System.Buffers.Binary;
using BenVoxel.FileToVoxCore;
using BenVoxel.Models;
using Godot;

namespace BenVoxelGpu;

public partial class Root : Node3D
{
	public const string SpatialShader = """
shader_type spatial;
render_mode unshaded;

uniform sampler2D svo_texture : filter_nearest;
uniform sampler2D palette_texture : filter_nearest;
uniform int texture_width;
uniform uint svo_nodes_count;
uniform uvec3 svo_model_size;
uniform uint svo_max_depth;
uniform int max_steps;
uniform vec3 ray_dir;
uniform float scale;
uniform vec3 step_dir;
uniform vec3 t_delta;
uniform mat4 rotation_matrix;
uniform vec3 light_dir;
uniform float ray_start_distance;
uniform float safety_distance;
uniform float target_resolution;

const uint FLAG_INTERNAL = 0x80000000u;
const uint FLAG_LEAF_TYPE = 0x40000000u;

// Read a uint32 from texture at pixel index
uint read_uint(int pixel_idx) {
	int x = pixel_idx % texture_width;
	int y = pixel_idx / texture_width;
	vec4 pixel = texelFetch(svo_texture, ivec2(x, y), 0);
	// Pack RGBA8 back to uint32 (little-endian)
	return uint(pixel.r * 255.0) | (uint(pixel.g * 255.0) << 8u) | (uint(pixel.b * 255.0) << 16u) | (uint(pixel.a * 255.0) << 24u);
}

uint read_node(uint idx) {
	return read_uint(int(idx));
}

uint read_payload_byte(uint payload_idx, int octant) {
	// Payloads start after nodes array
	int base_pixel = int(svo_nodes_count + payload_idx * 2u);

	// Each uint64 payload is stored as 2 uint32s (little-endian)
	// octant 0-3 are in first uint32, 4-7 in second
	int pixel_offset = octant / 4;
	int byte_in_pixel = octant % 4;

	uint pixel_data = read_uint(base_pixel + pixel_offset);
	return (pixel_data >> uint(byte_in_pixel * 8)) & 0xFFu;
}

uint sample_svo(uvec3 pos) {
	if (any(greaterThanEqual(pos, svo_model_size))) return 0u;
	uint node_idx = 0u;

	for(int depth = 0; depth < int(svo_max_depth) - 1; depth++) {
		uint node_data = read_node(node_idx);

		// Check if this is an internal node (bit 31)
		if ((node_data & FLAG_INTERNAL) == 0u) {
			// Leaf node - check type (bit 30)
			if ((node_data & FLAG_LEAF_TYPE) == 0u) {
				// Uniform leaf - material is in lower 8 bits
				return node_data & 0xFFu;
			} else {
				// Brick leaf - get payload index and extract voxel
				uint payload_idx = node_data & ~FLAG_LEAF_TYPE;
				int octant = ((int(pos.z) & 1) << 2) | ((int(pos.y) & 1) << 1) | (int(pos.x) & 1);
				return read_payload_byte(payload_idx, octant);
			}
		}

		// Internal node - traverse to child
		uint mask = node_data & 0xFFu;
		uint shift = uint(int(svo_max_depth) - 1 - depth);
		uint octant = (((pos.z >> shift) & 1u) << 2u) | (((pos.y >> shift) & 1u) << 1u) | ((pos.x >> shift) & 1u);

		if (((mask >> octant) & 1u) == 0u) return 0u;

		uint child_base = (node_data & ~FLAG_INTERNAL) >> 8u;
		uint offset = uint(bitCount(mask & ((1u << octant) - 1u)));
		node_idx = child_base + offset;
	}

	// Final level - must be a leaf
	uint node_data = read_node(node_idx);
	if ((node_data & FLAG_LEAF_TYPE) == 0u) {
		return node_data & 0xFFu;
	} else {
		uint payload_idx = node_data & ~FLAG_LEAF_TYPE;
		int octant = ((int(pos.z) & 1) << 2) | ((int(pos.y) & 1) << 1) | (int(pos.x) & 1);
		return read_payload_byte(payload_idx, octant);
	}
}

// Query octree at a specific depth, returns node data and index
// Returns: node_data in .x, node_idx in .y, 0 if out of bounds or empty child
uvec2 query_svo_at_depth(uvec3 pos, int target_depth) {
	if (any(greaterThanEqual(pos, svo_model_size))) return uvec2(0u);

	uint node_idx = 0u;
	uint cell_size = 1u << uint(int(svo_max_depth) - 1);

	for(int depth = 0; depth <= target_depth && depth < int(svo_max_depth) - 1; depth++) {
		uint node_data = read_node(node_idx);

		if (depth == target_depth) {
			return uvec2(node_data, node_idx);
		}

		// Check if this is an internal node
		if ((node_data & FLAG_INTERNAL) == 0u) {
			// Hit a leaf before reaching target depth
			return uvec2(node_data, node_idx);
		}

		// Internal node - traverse to child
		uint mask = node_data & 0xFFu;
		uint shift = uint(int(svo_max_depth) - 1 - depth);
		uint octant = (((pos.z >> shift) & 1u) << 2u) | (((pos.y >> shift) & 1u) << 1u) | ((pos.x >> shift) & 1u);

		// Check if child exists
		if (((mask >> octant) & 1u) == 0u) {
			// Empty child - return special marker
			return uvec2(0u);
		}

		uint child_base = (node_data & ~FLAG_INTERNAL) >> 8u;
		uint offset = uint(bitCount(mask & ((1u << octant) - 1u)));
		node_idx = child_base + offset;
		cell_size >>= 1u;
	}

	// If we reach here, we're at max depth
	uint node_data = read_node(node_idx);
	return uvec2(node_data, node_idx);
}

void fragment() {
	// Since we're rendering to exact low-res viewport, use UVs directly
	// Each fragment corresponds to exactly one low-res pixel
	vec2 uv_screen = UV * target_resolution;

	// 1. Calculate ray origin in "Camera Space"
	vec2 p = (uv_screen - target_resolution * 0.5) / scale;

	// Ray starts far away at +Z (orthographic camera)
	vec3 ro_view = vec3(p.x, p.y, ray_start_distance);

	// 2. Transform ray origin to "Model Space"
	vec3 ro = (rotation_matrix * vec4(ro_view, 1.0)).xyz + (vec3(svo_model_size) * 0.5);

	// Ray direction is pre-calculated and passed as uniform
	vec3 rd = ray_dir;

	vec4 color = vec4(0.03, 0.03, 0.05, 1.0); // Dark background

	// 3. Hierarchical DDA Traversal
	// Strategy: Use coarse steps through empty space, switch to voxel-level DDA near geometry
	int hierarchy_depth = max(0, int(svo_max_depth) - 6); // Hierarchical test depth
	int max_depth = int(svo_max_depth) - 1;

	vec3 pos = floor(ro);
	vec3 t_max = (floor(ro) + max(step_dir, 0.0) - ro) / rd;
	int last_axis = 0;

	for (int i = 0; i < max_steps; i++) {
		// Check bounds
		if (any(lessThan(pos, vec3(0.0))) || any(greaterThanEqual(pos, vec3(svo_model_size)))) {
			// Outside grid - advance to next voxel
			if (any(greaterThan(abs(pos - vec3(svo_model_size)*0.5), vec3(safety_distance)))) break;

			// Standard DDA step
			if (t_max.x < t_max.y) {
				if (t_max.x < t_max.z) { t_max.x += t_delta.x; pos.x += step_dir.x; last_axis = 0; }
				else { t_max.z += t_delta.z; pos.z += step_dir.z; last_axis = 2; }
			} else {
				if (t_max.y < t_max.z) { t_max.y += t_delta.y; pos.y += step_dir.y; last_axis = 1; }
				else { t_max.z += t_delta.z; pos.z += step_dir.z; last_axis = 2; }
			}
			continue;
		}

		// Sample at voxel level for exact geometry
		uint mat = sample_svo(uvec3(pos));

		if (mat > 0u) {
			// Hit solid voxel - render it with simple diffuse lighting
			vec3 normal = vec3(0.0);
			if (last_axis == 0) normal.x = -step_dir.x;
			else if (last_axis == 1) normal.y = -step_dir.y;
			else if (last_axis == 2) normal.z = -step_dir.z;

			float diff = max(dot(normal, light_dir), 0.0);
			vec3 base_color = texelFetch(palette_texture, ivec2(int(mat), 0), 0).rgb;
			color = vec4(base_color * (diff + 0.2), 1.0);
			break;
		}

		// Empty voxel - check if we can skip ahead using hierarchy
		// Align to coarse grid
		uint cell_shift = uint(max_depth - hierarchy_depth);
		uint cell_size = 1u << cell_shift;
		float cell_size_f = float(cell_size);
		vec3 grid_pos = floor(pos / cell_size_f) * cell_size_f;

		// Query at coarse level
		uvec2 node_info = query_svo_at_depth(uvec3(grid_pos), hierarchy_depth);
		uint node_data = node_info.x;

		if (node_data == 0u && cell_size > 1u) {
			// Entire coarse cell is empty - skip across it
			vec3 cell_min = grid_pos;
			vec3 cell_max = grid_pos + vec3(cell_size_f);

			// Calculate t values for cell boundaries
			vec3 t_near = (cell_min - pos) / rd;
			vec3 t_far = (cell_max - pos) / rd;

			// Get the exit point (maximum of entry t values)
			vec3 t_exit = max(t_near, t_far);
			float t_exit_min = min(min(t_exit.x, t_exit.y), t_exit.z);

			// Skip to just past cell boundary
			pos += rd * (t_exit_min + 0.01);

			// Recalculate t_max for DDA at new position
			vec3 pos_floor = floor(pos);
			t_max = (pos_floor + max(step_dir, 0.0) - pos) / rd;
		} else {
			// Near geometry or small cell - do normal voxel DDA step
			if (t_max.x < t_max.y) {
				if (t_max.x < t_max.z) { t_max.x += t_delta.x; pos.x += step_dir.x; last_axis = 0; }
				else { t_max.z += t_delta.z; pos.z += step_dir.z; last_axis = 2; }
			} else {
				if (t_max.y < t_max.z) { t_max.y += t_delta.y; pos.y += step_dir.y; last_axis = 1; }
				else { t_max.z += t_delta.z; pos.z += step_dir.z; last_axis = 2; }
			}
		}
	}

	ALBEDO = color.rgb;
}
""";
	/// <summary>
	/// Coordinate system transformation from Godot (Y-up, right-handed) to MagicaVoxel (Z-up, right-handed).
	/// A -90° rotation around X converts: Godot +Y (up) → Model +Z (up), Godot +Z (forward) → Model +Y (forward).
	/// This maintains Z+ as up in the model while displaying correctly in Godot's Y-up viewport.
	/// </summary>
	private static readonly Basis CoordTransform = Basis.FromEuler(new Vector3(Mathf.DegToRad(-90), 0, 0));
	private Camera3D _camera;
	private MeshInstance3D _screenQuad;
	private SubViewport _lowResViewport;
	private ImageTexture _svoTexture;
	private ShaderMaterial _material;
	private Vector3I _modelSize;
	private int _maxDepth;
	private int _textureWidth;
	private float _rotationAngle = 0f;
	// Configurable rendering parameters
	private float _rayStartDistance = 500f;  // Distance camera starts from model
	private float _safetyDistance = 600f;    // Maximum ray travel distance from model center
	private int _targetResolution = 240;     // Render resolution for pixel-perfect look (try 160, 240, 320, etc.)
	private float _pixelsPerVoxel = 4f;      // How many screen pixels per voxel (1=tiny, 2=small, 4=medium, 8=large, etc.)
	public override void _Ready()
	{
		// Load model and create texture
		VoxFileModel vox = new(@"..\..\src\Tests\Voxel2Pixel.Test\TestData\Models\Sora.vox");
		GpuSvoModel gpuModel = new(vox);
		uint[] palette = vox.Palette; // 256 RGBA8888 colors, ignore index 0, 0x00FF00FF is blue.
		GpuSvoModelTexture modelTexture = new(gpuModel);
		_modelSize = new Vector3I(modelTexture.SizeX, modelTexture.SizeY, modelTexture.SizeZ);
		_maxDepth = modelTexture.MaxDepth;
		_textureWidth = modelTexture.Width;
		// Create ImageTexture from the model texture data
		Image image = Image.CreateFromData(_textureWidth, _textureWidth, false, Image.Format.Rgba8, modelTexture.Data);
		_svoTexture = ImageTexture.CreateFromImage(image);
		// Create palette texture (256x1 RGBA8)
		// Use the same pipeline as the rest of the codebase: WriteUInt32BigEndian
		byte[] paletteBytes = new byte[256 << 2];
		for (int i = 0; i < 256; i++)
			BinaryPrimitives.WriteUInt32BigEndian(paletteBytes.AsSpan(i << 2, 4), palette[i]);
		Image paletteImage = Image.CreateFromData(256, 1, false, Image.Format.Rgba8, paletteBytes);
		ImageTexture paletteTexture = ImageTexture.CreateFromImage(paletteImage);
		// Create shader material for voxel rendering
		_material = new ShaderMaterial
		{
			Shader = new Shader { Code = SpatialShader, },
		};
		_material.SetShaderParameter("svo_texture", _svoTexture);
		_material.SetShaderParameter("palette_texture", paletteTexture);
		_material.SetShaderParameter("texture_width", _textureWidth);
		_material.SetShaderParameter("svo_nodes_count", (uint)modelTexture.NodesCount);
		_material.SetShaderParameter("svo_model_size", _modelSize);
		_material.SetShaderParameter("svo_max_depth", (uint)_maxDepth);
		_material.SetShaderParameter("scale", _pixelsPerVoxel);
		_material.SetShaderParameter("target_resolution", (float)_targetResolution);
		// Create low-res SubViewport for pixel-perfect rendering
		_lowResViewport = new SubViewport
		{
			Size = new Vector2I(_targetResolution, _targetResolution),
			RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
			TransparentBg = false,
		};
		AddChild(_lowResViewport);
		// Create camera inside the viewport
		_camera = new Camera3D
		{
			Projection = Camera3D.ProjectionType.Orthogonal,
			Size = 2f,
			Position = new Vector3(0f, 0f, 1f), // Proper position
			Rotation = Vector3.Zero, // Looking in -Z direction (default)
			Current = true,
		};
		_lowResViewport.AddChild(_camera);
		// Add WorldEnvironment to viewport
		WorldEnvironment env = new()
		{
			Environment = new Godot.Environment()
			{
				BackgroundMode = Godot.Environment.BGMode.Color,
				BackgroundColor = new Color(0.03f, 0.03f, 0.05f),
			}
		};
		_lowResViewport.AddChild(env);
		// Create screen quad inside viewport for voxel rendering
		_screenQuad = new MeshInstance3D
		{
			Mesh = new QuadMesh { Size = new Vector2(2f, 2f) },
			MaterialOverride = _material,
			Position = Vector3.Zero,
		};
		_lowResViewport.AddChild(_screenQuad);
		// Use 2D CanvasLayer to display the viewport - no 3D geometry issues!
		CanvasLayer canvas = new();
		AddChild(canvas);
		TextureRect display = new()
		{
			Texture = _lowResViewport.GetTexture(),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, // Maintain square aspect ratio
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest, // Pixel-perfect upscaling
			AnchorLeft = 0,
			AnchorTop = 0,
			AnchorRight = 1,
			AnchorBottom = 1,
		};
		canvas.AddChild(display);
		// Initialize shader parameters before first render
		UpdateShaderParameters();
	}
	private void UpdateShaderParameters()
	{
		// Spin around model's Z axis (up in MagicaVoxel coordinates)
		Basis modelSpin = Basis.FromEuler(new Vector3(0, 0, _rotationAngle)),
			// Tilt camera view for better perspective (in model space)
			viewTilt = Basis.FromEuler(new Vector3(Mathf.DegToRad(45), 0, 0)),
			// Combine: convert to model space, tilt, then spin (applied right to left)
			rotation = modelSpin * viewTilt * CoordTransform;
		Projection rotationMatrix = new(new Transform3D(rotation, Vector3.Zero));
		// Pre-calculate DDA constants for orthographic camera
		Vector3 rdView = new(0f, 0f, -1f),
			rdModel = (rotation * rdView).Normalized(),
			// Calculate step_dir and t_delta
			stepDir = new(
				Math.Sign(rdModel.X),
				Math.Sign(rdModel.Y),
				Math.Sign(rdModel.Z)),
			tDelta = new(
				Math.Abs(1f / rdModel.X),
				Math.Abs(1f / rdModel.Y),
				Math.Abs(1f / rdModel.Z));
		// Calculate max steps based on configurable geometry parameters
		// Maximum distance = ray start distance + safety break distance + model traversal
		int modelDiagonal = _modelSize.X + _modelSize.Y + _modelSize.Z;
		int maxSteps = (int)(_rayStartDistance + _safetyDistance) + modelDiagonal + 50; // Buffer for edge cases
		// Light direction: front upper right
		Vector3 lightDirView = new(1.0f, -1.0f, 1.0f), // Right(+X), Upper(-Y), Front(+Z)
			lightDirModel = (rotation * lightDirView).Normalized();
		// Update shader parameters
		_material.SetShaderParameter("ray_dir", rdModel);
		_material.SetShaderParameter("step_dir", stepDir);
		_material.SetShaderParameter("t_delta", tDelta);
		_material.SetShaderParameter("max_steps", maxSteps);
		_material.SetShaderParameter("rotation_matrix", rotationMatrix);
		_material.SetShaderParameter("light_dir", lightDirModel);
		_material.SetShaderParameter("ray_start_distance", _rayStartDistance);
		_material.SetShaderParameter("safety_distance", _safetyDistance);
	}
	public override void _Process(double delta)
	{
		// Allow experimenting with different settings at runtime
		// Up/Down: Adjust render resolution
		if (Input.IsActionJustPressed("ui_up"))
		{
			_targetResolution += 20;
			_lowResViewport.Size = new Vector2I(_targetResolution, _targetResolution);
			_material.SetShaderParameter("target_resolution", (float)_targetResolution);
			GD.Print($"Resolution: {_targetResolution}, Pixels/Voxel: {_pixelsPerVoxel}");
		}
		if (Input.IsActionJustPressed("ui_down"))
		{
			_targetResolution = Math.Max(80, _targetResolution - 20);
			_lowResViewport.Size = new Vector2I(_targetResolution, _targetResolution);
			_material.SetShaderParameter("target_resolution", (float)_targetResolution);
			GD.Print($"Resolution: {_targetResolution}, Pixels/Voxel: {_pixelsPerVoxel}");
		}
		// Left/Right: Adjust pixels per voxel (zoom)
		if (Input.IsActionJustPressed("ui_right"))
		{
			_pixelsPerVoxel = Mathf.Min(16f, _pixelsPerVoxel + 1f);
			_material.SetShaderParameter("scale", _pixelsPerVoxel);
			GD.Print($"Resolution: {_targetResolution}, Pixels/Voxel: {_pixelsPerVoxel}");
		}
		if (Input.IsActionJustPressed("ui_left"))
		{
			_pixelsPerVoxel = Mathf.Max(1f, _pixelsPerVoxel - 1f);
			_material.SetShaderParameter("scale", _pixelsPerVoxel);
			GD.Print($"Resolution: {_targetResolution}, Pixels/Voxel: {_pixelsPerVoxel}");
		}
		// Accumulate rotation over time
		_rotationAngle += (float)delta;
		// Update all shader parameters
		UpdateShaderParameters();
	}
}
