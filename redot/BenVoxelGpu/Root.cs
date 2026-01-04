using System;
using BenVoxel;
using Godot;
using Voxel2Pixel.Model.FileFormats;

namespace BenVoxelGpu;

public partial class Root : Node3D
{
	public const string SpatialShader = """
shader_type spatial;
render_mode unshaded;

uniform sampler2D svo_texture : filter_nearest;
uniform int texture_width;
uniform vec3 ray_dir;
uniform float scale;
uniform vec3 step_dir;
uniform vec3 t_delta;
uniform mat4 rotation_matrix;

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

// Read header info
uvec3 read_model_size() {
	uint packed_xy = read_uint(2);
	uint packed_zd = read_uint(3);
	return uvec3(packed_xy & 0xFFFFu, packed_xy >> 16u, packed_zd & 0xFFFFu);
}

uint read_max_depth() {
	uint packed_zd = read_uint(3);
	return packed_zd >> 16u;
}

uint read_node(uint idx) {
	return read_uint(int(4u + idx));
}

uint read_payload_byte(uint payload_idx, int octant) {
	// Payloads start after header (4 pixels) and nodes array
	uint nodes_count = read_uint(0);
	int base_pixel = int(4u + nodes_count + payload_idx * 2u);

	// Each uint64 payload is stored as 2 uint32s (little-endian)
	// octant 0-3 are in first uint32, 4-7 in second
	int pixel_offset = octant / 4;
	int byte_in_pixel = octant % 4;

	uint pixel_data = read_uint(base_pixel + pixel_offset);
	return (pixel_data >> uint(byte_in_pixel * 8)) & 0xFFu;
}

uint sample_svo(uvec3 pos, uvec3 model_size, uint max_depth) {
	if (any(greaterThanEqual(pos, model_size))) return 0u;
	uint node_idx = 0u;

	for(int depth = 0; depth < int(max_depth) - 1; depth++) {
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
		uint shift = uint(int(max_depth) - 1 - depth);
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

void fragment() {
	// Use mesh UV coordinates (0-1 range)
	// Convert to centered pixel coordinates (-256 to 256 for a 512x512 equivalent)
	float render_size = 512.0;
	vec2 uv_screen = UV * render_size;

	// 1. Calculate ray origin in "Camera Space"
	vec2 p = (uv_screen - render_size * 0.5) / scale;

	// Read model metadata from texture
	uvec3 model_size = read_model_size();
	uint max_depth = read_max_depth();

	// Ray starts far away at +Z (orthographic camera)
	vec3 ro_view = vec3(p.x, p.y, 500.0);

	// 2. Transform ray origin to "Model Space"
	vec3 ro = (rotation_matrix * vec4(ro_view, 1.0)).xyz + (vec3(model_size) * 0.5);

	// Ray direction is pre-calculated and passed as uniform
	vec3 rd = ray_dir;

	vec4 color = vec4(0.03, 0.03, 0.05, 1.0); // Dark background

	// 3. DDA Traversal with pre-calculated constants
	vec3 pos = floor(ro);
	vec3 t_max = (floor(ro) + max(step_dir, 0.0) - ro) / rd;
	int last_axis = 0;

	// Use a large enough step count to cross the bounding box from any angle
	for (int i = 0; i < 1000; i++) {
		// Only sample if we are inside the actual grid
		if (all(greaterThanEqual(pos, vec3(0.0))) && all(lessThan(pos, vec3(model_size)))) {
			uint mat = sample_svo(uvec3(pos), model_size, max_depth);
			if (mat > 0u) {
				// --- DIRECTIONAL LIGHTING ---
				vec3 normal = vec3(0.0);
				if (last_axis == 0) normal.x = -step_dir.x;
				else if (last_axis == 1) normal.y = -step_dir.y;
				else if (last_axis == 2) normal.z = -step_dir.z;

				// Light from top-right-front
				vec3 light_dir = normalize(vec3(0.5, 1.0, 0.7));
				float diff = max(dot(normal, light_dir), 0.0);

				vec3 base_color = vec3(float(mat) / 255.0);
				color = vec4(base_color * (diff + 0.2), 1.0);
				break;
			}
		}

		// Advance DDA using pre-calculated t_delta
		if (t_max.x < t_max.y) {
			if (t_max.x < t_max.z) { t_max.x += t_delta.x; pos.x += step_dir.x; last_axis = 0; }
			else { t_max.z += t_delta.z; pos.z += step_dir.z; last_axis = 2; }
		} else {
			if (t_max.y < t_max.z) { t_max.y += t_delta.y; pos.y += step_dir.y; last_axis = 1; }
			else { t_max.z += t_delta.z; pos.z += step_dir.z; last_axis = 2; }
		}

		// Safety break if we leave the "danger zone" around the model
		if (any(greaterThan(abs(pos - vec3(model_size)*0.5), vec3(600.0)))) break;
	}

	ALBEDO = color.rgb;
}
""";
	private Camera3D _camera;
	private MeshInstance3D _screenQuad;
	private ImageTexture _svoTexture;
	private ShaderMaterial _material;
	private Vector3I _modelSize;
	private int _maxDepth;
	private int _textureWidth;
	private float _rotationAngle = 0f;
	public override void _Ready()
	{
		// Load model and create texture
		GpuSvoModel gpuModel = new(new VoxFileModel(@"..\..\src\Tests\Voxel2Pixel.Test\TestData\Models\Sora.vox"));
		GpuSvoModelTexture modelTexture = new(gpuModel);
		_modelSize = new Vector3I(modelTexture.SizeX, modelTexture.SizeY, modelTexture.SizeZ);
		_maxDepth = modelTexture.MaxDepth;
		_textureWidth = modelTexture.Width;
		// Create ImageTexture from the model texture data
		Image image = Image.CreateFromData(_textureWidth, _textureWidth, false, Image.Format.Rgba8, modelTexture.Data);
		_svoTexture = ImageTexture.CreateFromImage(image);
		// Create shader material
		_material = new ShaderMaterial
		{
			Shader = new Shader { Code = SpatialShader, },
		};
		_material.SetShaderParameter("svo_texture", _svoTexture);
		_material.SetShaderParameter("texture_width", _textureWidth);
		_material.SetShaderParameter("scale", 4f);
		AddChild(_camera = new Camera3D
		{
			Projection = Camera3D.ProjectionType.Orthogonal,
			Size = 2f,
			Position = new Vector3(0f, 0f, 1f),
		});
		_camera.LookAt(Vector3.Zero);

		AddChild(_screenQuad = new MeshInstance3D
		{
			Mesh = new QuadMesh { Size = new Vector2(2f, 2f) },
			MaterialOverride = _material,
			Position = Vector3.Zero,
		});
	}
	public override void _Process(double delta)
	{
		// Accumulate rotation over time
		_rotationAngle += (float)delta;
		// Create a spinning rotation matrix
		Basis rotation = Basis.FromEuler(new Vector3(Mathf.DegToRad(30), _rotationAngle, 0));
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
		// Update shader parameters
		_material.SetShaderParameter("ray_dir", rdModel);
		_material.SetShaderParameter("step_dir", stepDir);
		_material.SetShaderParameter("t_delta", tDelta);
		_material.SetShaderParameter("rotation_matrix", rotationMatrix);
	}
}
