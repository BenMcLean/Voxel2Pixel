using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using BenProgress;
using BenVoxel.FileToVoxCore;
using BenVoxel.Models;
using Godot;

namespace BenVoxelGpu;

/// <summary>
/// Volumetric Ortho-Impostor Rendering System
/// Renders voxel models as sprite-like entities in a 3D perspective world
/// with orthographic internal projection and 1-pixel silhouette outline.
/// </summary>
public partial class Root : Node3D
{
	public const string SpatialShader = """
shader_type spatial;
render_mode unshaded, depth_draw_always, cull_disabled;

// SVO data
uniform sampler2D svo_texture : filter_nearest;
uniform sampler2D palette_texture : filter_nearest;
uniform int texture_width;
uniform uint svo_nodes_count;
uniform uvec3 svo_model_size;
uniform uint svo_max_depth;
uniform uint node_offset;
uniform uint payload_offset;

// Volumetric impostor parameters
uniform vec3 ray_dir_local;       // Camera forward in voxel space (normalized)
uniform vec3 camera_up_local;     // Camera up (V) in voxel space (normalized)
uniform float voxel_size;         // World-space size of one voxel
uniform float sigma;              // Virtual pixels per voxel
uniform vec3 voxel_offset;        // Offset from box origin to voxel origin
uniform vec3 light_dir;           // Light direction for shading
uniform float camera_distance;    // Distance from camera to model center (world units)

const uint FLAG_INTERNAL = 0x80000000u;
const uint FLAG_LEAF_TYPE = 0x40000000u;
const vec3 OUTLINE_COLOR = vec3(0.0);

// Pass local position from vertex to fragment shader
varying vec3 local_vertex;

void vertex() {
	// VERTEX in vertex shader is in local/model space
	local_vertex = VERTEX;
}

// Read a uint32 from texture at pixel index
uint read_uint(int pixel_idx) {
	int x = pixel_idx % texture_width;
	int y = pixel_idx / texture_width;
	vec4 pixel = texelFetch(svo_texture, ivec2(x, y), 0);
	return uint(pixel.r * 255.0) | (uint(pixel.g * 255.0) << 8u) | (uint(pixel.b * 255.0) << 16u) | (uint(pixel.a * 255.0) << 24u);
}

uint read_node(uint idx) {
	return read_uint(int(node_offset + idx));
}

uint read_payload_byte(uint payload_idx, int octant) {
	int base_pixel = int(svo_nodes_count + (payload_offset + payload_idx) * 2u);
	int pixel_offset = octant / 4;
	int byte_in_pixel = octant % 4;
	uint pixel_data = read_uint(base_pixel + pixel_offset);
	return (pixel_data >> uint(byte_in_pixel * 8)) & 0xFFu;
}

uint sample_svo_no_bounds(vec3 pos) {
	// Same as sample_svo but without bounds check for debugging
	uvec3 upos = uvec3(pos);
	uint node_idx = 0u;

	for(int depth = 0; depth < int(svo_max_depth) - 1; depth++) {
		uint node_data = read_node(node_idx);
		if ((node_data & FLAG_INTERNAL) == 0u) {
			if ((node_data & FLAG_LEAF_TYPE) == 0u) {
				return node_data & 0xFFu;
			} else {
				uint payload_idx = node_data & ~FLAG_LEAF_TYPE;
				int octant = ((int(upos.z) & 1) << 2) | ((int(upos.y) & 1) << 1) | (int(upos.x) & 1);
				return read_payload_byte(payload_idx, octant);
			}
		}
		uint mask = node_data & 0xFFu;
		uint shift = uint(int(svo_max_depth) - 1 - depth);
		uint octant = (((upos.z >> shift) & 1u) << 2u) | (((upos.y >> shift) & 1u) << 1u) | ((upos.x >> shift) & 1u);
		if (((mask >> octant) & 1u) == 0u) return 0u;
		uint child_base = (node_data & ~FLAG_INTERNAL) >> 8u;
		uint offset = uint(bitCount(mask & ((1u << octant) - 1u)));
		node_idx = child_base + offset;
	}

	uint node_data = read_node(node_idx);
	if ((node_data & FLAG_LEAF_TYPE) == 0u) {
		return node_data & 0xFFu;
	} else {
		uint payload_idx = node_data & ~FLAG_LEAF_TYPE;
		uvec3 upos2 = uvec3(pos);
		int octant = ((int(upos2.z) & 1) << 2) | ((int(upos2.y) & 1) << 1) | (int(upos2.x) & 1);
		return read_payload_byte(payload_idx, octant);
	}
}

uint sample_svo(vec3 pos) {
	// Simple bounds check
	if (pos.x < 0.0 || pos.y < 0.0 || pos.z < 0.0) return 0u;
	if (pos.x >= float(svo_model_size.x) || pos.y >= float(svo_model_size.y) || pos.z >= float(svo_model_size.z)) return 0u;
	return sample_svo_no_bounds(pos);
}

// Ray-AABB intersection, returns (t_enter, t_exit), t_enter < 0 means inside or behind
vec2 ray_aabb(vec3 ro, vec3 rd, vec3 box_min, vec3 box_max) {
	vec3 t1 = (box_min - ro) / rd;
	vec3 t2 = (box_max - ro) / rd;
	vec3 t_min = min(t1, t2);
	vec3 t_max = max(t1, t2);
	float t_enter = max(max(t_min.x, t_min.y), t_min.z);
	float t_exit = min(min(t_max.x, t_max.y), t_max.z);
	return vec2(t_enter, t_exit);
}

// DDA traversal, returns (hit, t_hit, material, last_axis)
// hit: 1.0 if hit, 0.0 if miss
// t_hit: distance to hit point
// material: material index (0-255)
// last_axis: 0=X, 1=Y, 2=Z
vec4 trace_ray(vec3 ro, vec3 rd, vec3 box_min, vec3 box_max, out int last_axis) {
	// Intersect with voxel AABB
	vec2 t = ray_aabb(ro, rd, box_min, box_max);
	if (t.y < max(t.x, 0.0)) {
		last_axis = 0;
		return vec4(0.0, 0.0, 0.0, 0.0); // Miss
	}

	// Start at entry point
	float t_start = max(t.x, 0.0) + 0.001;
	vec3 pos = ro + rd * t_start;

	// DDA setup
	vec3 step_dir = sign(rd);
	vec3 t_delta = abs(1.0 / rd);
	vec3 t_max_val = (floor(pos) + max(step_dir, vec3(0.0)) - pos) / rd;
	last_axis = 0;

	int max_steps = int(svo_model_size.x + svo_model_size.y + svo_model_size.z) + 10;
	float t_current = t_start;

	for (int i = 0; i < max_steps; i++) {
		// Check bounds
		if (any(lessThan(pos, box_min)) || any(greaterThanEqual(pos, box_max))) {
			return vec4(0.0, 0.0, 0.0, 0.0); // Miss - exited bounds
		}

		// Sample voxel
		uint mat = sample_svo(pos);
		if (mat > 0u) {
			return vec4(1.0, t_current, float(mat), float(last_axis));
		}

		// DDA step
		if (t_max_val.x < t_max_val.y) {
			if (t_max_val.x < t_max_val.z) {
				t_current = t_max_val.x;
				t_max_val.x += t_delta.x;
				pos.x += step_dir.x;
				last_axis = 0;
			} else {
				t_current = t_max_val.z;
				t_max_val.z += t_delta.z;
				pos.z += step_dir.z;
				last_axis = 2;
			}
		} else {
			if (t_max_val.y < t_max_val.z) {
				t_current = t_max_val.y;
				t_max_val.y += t_delta.y;
				pos.y += step_dir.y;
				last_axis = 1;
			} else {
				t_current = t_max_val.z;
				t_max_val.z += t_delta.z;
				pos.z += step_dir.z;
				last_axis = 2;
			}
		}
	}

	return vec4(0.0, 0.0, 0.0, 0.0); // Miss - max steps exceeded
}

// Debug mode: 0=normal, 1=show frag_pos, 2=show ray dir, 3=show ray origin, 4=simple trace test
// 5=show material as grayscale, 6=show bounds check, 7=show voxel_offset
const int DEBUG_MODE = 0;

void fragment() {
	// Derive camera right from forward and up (standard graphics convention)
	vec3 camera_right_local = cross(ray_dir_local, camera_up_local);

	// Virtual pixel size in voxel units
	float delta_px = 1.0 / sigma;

	// Voxel/model bounds in voxel space
	vec3 voxel_min = vec3(0.0);
	vec3 voxel_max = vec3(float(svo_model_size.x), float(svo_model_size.y), float(svo_model_size.z));
	float max_dim = max(max(voxel_max.x, voxel_max.y), voxel_max.z);

	// Model center in voxel space
	vec3 model_center = voxel_offset;

	// === Ray direction (same for all fragments - orthographic internal projection) ===
	vec3 rd = normalize(ray_dir_local);

	// === SCREEN-BASED SPRITE PIXEL ===
	// Use screen coordinates to determine sprite pixel, making it independent of box geometry.
	// All fragments at the same screen position compute the same sprite pixel.

	// Get model center's screen position for reference
	vec4 center_clip = PROJECTION_MATRIX * VIEW_MATRIX * MODEL_MATRIX * vec4(0.0, 0.0, 0.0, 1.0);
	vec2 center_ndc = center_clip.xy / center_clip.w;

	// Current fragment's NDC position
	vec2 frag_ndc = (FRAGCOORD.xy / VIEWPORT_SIZE) * 2.0 - 1.0;

	// Offset from center in NDC
	vec2 offset_ndc = frag_ndc - center_ndc;

	// Convert NDC offset to world units using projection matrix and camera distance
	// For perspective: tan(fov/2) = 1/P[1][1], so at distance d, NDC 1.0 = d/P[1][1] world units
	float proj_scale_y = 1.0 / PROJECTION_MATRIX[1][1];
	float proj_scale_x = 1.0 / PROJECTION_MATRIX[0][0];

	// World-space offset at the sprite plane (using fixed camera distance)
	float world_offset_x = offset_ndc.x * camera_distance * proj_scale_x;
	float world_offset_y = offset_ndc.y * camera_distance * proj_scale_y;

	// Convert to voxel units
	float u_coord = world_offset_x / voxel_size;
	float v_coord = world_offset_y / voxel_size;

	// Quantize to virtual pixel grid
	float u_snapped = round(u_coord / delta_px) * delta_px;
	float v_snapped = round(v_coord / delta_px) * delta_px;

	// === Parallel Ray Origin ===
	// Fire parallel ray from quantized sprite position.
	// Offset along U (right) and V (up) in the sprite plane, then back up along
	// the ray direction so the ray starts in front of the model (toward camera)
	// and can properly intersect the voxel volume from its front face.
	vec3 ro_voxel = model_center
				  + u_snapped * camera_right_local
				  + v_snapped * camera_up_local
				  - max_dim * 2.0 * rd;

	// For debug modes
	vec3 frag_voxel = vec3(local_vertex.x, local_vertex.z, local_vertex.y) / voxel_size;
	vec3 frag_pos = frag_voxel + voxel_offset;

	// Debug visualizations (no return statements allowed in fragment)
	if (DEBUG_MODE == 1) {
		// Show fragment position normalized to model bounds
		ALBEDO = frag_pos / vec3(svo_model_size);
	} else if (DEBUG_MODE == 2) {
		// Show ray direction
		ALBEDO = abs(rd);
	} else if (DEBUG_MODE == 3) {
		// Show ray origin relative to model
		ALBEDO = (ro_voxel + max_dim * 2.0 * rd) / vec3(svo_model_size);
	} else if (DEBUG_MODE == 4) {
		// Simple sample test at fragment position
		uint mat = sample_svo(frag_pos);
		if (mat > 0u) {
			ALBEDO = texelFetch(palette_texture, ivec2(int(mat), 0), 0).rgb;
		} else {
			ALBEDO = vec3(0.2, 0.0, 0.0); // Dark red for empty
		}
	} else if (DEBUG_MODE == 5) {
		// Show material index as grayscale
		uint mat = sample_svo(frag_pos);
		ALBEDO = vec3(float(mat) / 255.0);
	} else if (DEBUG_MODE == 6) {
		// Show bounds check: green = inside, red = outside
		bool inside = all(greaterThanEqual(frag_pos, vec3(0.0))) && all(lessThan(frag_pos, vec3(svo_model_size)));
		ALBEDO = inside ? vec3(0.0, 1.0, 0.0) : vec3(1.0, 0.0, 0.0);
	} else if (DEBUG_MODE == 7) {
		// Show voxel_offset normalized
		ALBEDO = voxel_offset / vec3(svo_model_size);
	} else if (DEBUG_MODE == 8) {
		// Test raw texture read - show first node data as color
		uint node0 = read_node(0u);
		ALBEDO = vec3(float(node0 & 0xFFu) / 255.0, float((node0 >> 8u) & 0xFFu) / 255.0, float((node0 >> 16u) & 0xFFu) / 255.0);
	} else if (DEBUG_MODE == 9) {
		// Test bounds check - show model size as color (normalized by 256)
		ALBEDO = vec3(float(svo_model_size.x) / 256.0, float(svo_model_size.y) / 256.0, float(svo_model_size.z) / 256.0);
	} else if (DEBUG_MODE == 10) {
		// Test SVO sampling without bounds check
		uint mat = sample_svo_no_bounds(frag_pos);
		if (mat > 0u) {
			ALBEDO = texelFetch(palette_texture, ivec2(int(mat), 0), 0).rgb;
		} else {
			ALBEDO = vec3(0.2, 0.0, 0.0); // Dark red for empty
		}
	} else if (DEBUG_MODE == 11) {
		// Show bounds check results: R=x_ok, G=y_ok, B=z_ok
		float x_ok = (frag_pos.x >= 0.0 && frag_pos.x < float(svo_model_size.x)) ? 1.0 : 0.0;
		float y_ok = (frag_pos.y >= 0.0 && frag_pos.y < float(svo_model_size.y)) ? 1.0 : 0.0;
		float z_ok = (frag_pos.z >= 0.0 && frag_pos.z < float(svo_model_size.z)) ? 1.0 : 0.0;
		ALBEDO = vec3(x_ok, y_ok, z_ok);
	} else if (DEBUG_MODE == 12) {
		// Simple direct ray trace from fragment position (no virtual pixel quantization)
		// This tests pure ray tracing without the ortho-impostor complexity
		int last_axis;
		vec4 hit = trace_ray(frag_pos, rd, voxel_min, voxel_max, last_axis);

		if (hit.x > 0.5) {
			uint mat = uint(hit.z);
			vec3 base_color = texelFetch(palette_texture, ivec2(int(mat), 0), 0).rgb;
			ALBEDO = base_color;
		} else {
			discard;
		}
	} else {
		// Normal rendering (DEBUG_MODE == 0)
		// Primary ray trace
		int last_axis;
		vec4 hit = trace_ray(ro_voxel, rd, voxel_min, voxel_max, last_axis);

		if (hit.x > 0.5) {
			// Hit - output material color with lighting
			uint mat = uint(hit.z);

			// Compute normal in voxel space based on which axis we hit
			vec3 normal_voxel = vec3(0.0);
			vec3 step_dir = sign(rd);
			if (last_axis == 0) normal_voxel.x = -step_dir.x;
			else if (last_axis == 1) normal_voxel.y = -step_dir.y;
			else normal_voxel.z = -step_dir.z;

			// Directional lighting from upper left
			float diff = max(dot(normal_voxel, light_dir), 0.0);
			float ambient = 0.3;

			vec3 base_color = texelFetch(palette_texture, ivec2(int(mat), 0), 0).rgb;
			ALBEDO = base_color * (diff + ambient);

			// Compute depth from hit point
			// Hit point in voxel space
			vec3 hit_voxel = ro_voxel + hit.y * rd;

			// Transform back to box local space (Godot Y-up)
			// Voxel space: X=right, Y=forward, Z=up -> Godot: X=right, Y=up, Z=back
			vec3 hit_local = (hit_voxel - model_center);
			vec3 hit_godot = vec3(hit_local.x, hit_local.z, -hit_local.y) * voxel_size;

			vec4 hit_clip = PROJECTION_MATRIX * VIEW_MATRIX * MODEL_MATRIX * vec4(hit_godot, 1.0);
			DEPTH = (hit_clip.z / hit_clip.w) * 0.5 + 0.5;
		} else {
			// Miss - check neighbor rays for outline (cardinal directions only)
			vec3 offsets[4];
			offsets[0] = -camera_right_local * delta_px; // Left
			offsets[1] = camera_right_local * delta_px;  // Right
			offsets[2] = camera_up_local * delta_px;     // Up
			offsets[3] = -camera_up_local * delta_px;    // Down

			bool outline = false;
			float min_t = 1e10;
			int outline_axis = 0;

			for (int n = 0; n < 4; n++) {
				vec3 neighbor_ro = ro_voxel + offsets[n];
				int neighbor_axis;
				vec4 neighbor_hit = trace_ray(neighbor_ro, rd, voxel_min, voxel_max, neighbor_axis);
				if (neighbor_hit.x > 0.5) {
					outline = true;
					if (neighbor_hit.y < min_t) {
						min_t = neighbor_hit.y;
						outline_axis = neighbor_axis;
					}
				}
			}

			if (outline) {
				// Output black outline
				ALBEDO = OUTLINE_COLOR;

				// Depth from nearest neighbor hit
				vec3 hit_voxel = ro_voxel + min_t * rd;
				vec3 hit_local = hit_voxel - model_center;
				vec3 hit_godot = vec3(hit_local.x, hit_local.z, -hit_local.y) * voxel_size;
				vec4 hit_clip = PROJECTION_MATRIX * VIEW_MATRIX * MODEL_MATRIX * vec4(hit_godot, 1.0);
				DEPTH = (hit_clip.z / hit_clip.w) * 0.5 + 0.5 + 0.0001; // Small bias
			} else {
				// Transparent - discard
				discard;
			}
		}
	}
}
""";
	private Camera3D _camera;
	private MeshInstance3D _proxyBox;
	private ImageTexture _svoTexture;
	private ShaderMaterial _material;
	private Vector3I _modelSize;
	private int _maxDepth;
	private int _textureWidth;
	private float _rotationAngle = 0f;
	// Multi-model state
	private IReadOnlyList<SvoModelDescriptor> _descriptors;
	private ImageTexture[] _paletteTextures;
	private int _currentModelIndex = 0;
	private int _totalNodesCount;
	// Volumetric impostor parameters
	private float _voxelSize = 0.1f;     // World-space size of one voxel (meters)
	private float _sigma = 4f;           // Virtual pixels per voxel
	private float _cameraDistance = 20f; // Distance from camera to model center
	public override void _Ready()
	{
		// Load models and create SVO texture
		VoxFileModel[] vox = [
			new(@"..\..\src\Tests\Voxel2Pixel.Test\TestData\Models\Sora.vox"),
			new(@"..\..\src\Tests\Voxel2Pixel.Test\TestData\Models\Tree.vox"),
			new(@"..\..\src\Tests\Voxel2Pixel.Test\TestData\Models\NumberCube.vox")];
		GpuSvoModel[] gpuModel = [.. vox.Parallelize(vox => new GpuSvoModel(vox))];
		uint[][] palettes = [.. vox.Select(vox => vox.Palette)];
		GpuSvoModelTexture modelTexture = new(gpuModel);
		_descriptors = modelTexture.Descriptors;
		_totalNodesCount = modelTexture.TotalNodesCount;
		_textureWidth = modelTexture.Width;
		// Create ImageTexture from the model texture data
		Image image = Image.CreateFromData(_textureWidth, _textureWidth, false, Image.Format.Rgba8, modelTexture.Data);
		_svoTexture = ImageTexture.CreateFromImage(image);
		// Create palette textures for all models
		_paletteTextures = new ImageTexture[palettes.Length];
		for (int m = 0; m < palettes.Length; m++)
		{
			byte[] paletteBytes = new byte[256 << 2];
			for (int i = 0; i < 256; i++)
				BinaryPrimitives.WriteUInt32BigEndian(paletteBytes.AsSpan(i << 2, 4), palettes[m][i]);
			Image paletteImage = Image.CreateFromData(256, 1, false, Image.Format.Rgba8, paletteBytes);
			_paletteTextures[m] = ImageTexture.CreateFromImage(paletteImage);
		}
		// Initialize with first model
		SvoModelDescriptor descriptor = _descriptors[0];
		_modelSize = new Vector3I(descriptor.SizeX, descriptor.SizeY, descriptor.SizeZ);
		_maxDepth = descriptor.MaxDepth;
		// Create shader material
		_material = new ShaderMaterial
		{
			Shader = new Shader { Code = SpatialShader },
		};
		_material.SetShaderParameter("svo_texture", _svoTexture);
		_material.SetShaderParameter("texture_width", _textureWidth);
		_material.SetShaderParameter("svo_nodes_count", (uint)_totalNodesCount);
		// Create proxy box for the voxel model
		_proxyBox = new MeshInstance3D();
		AddChild(_proxyBox);
		// Create perspective camera
		_camera = new Camera3D
		{
			Projection = Camera3D.ProjectionType.Perspective,
			Fov = 45f,
			Current = true,
		};
		AddChild(_camera);
		// Add environment for background
		WorldEnvironment env = new()
		{
			Environment = new Godot.Environment()
			{
				BackgroundMode = Godot.Environment.BGMode.Color,
				BackgroundColor = new Color(0.1f, 0.1f, 0.15f),
			}
		};
		AddChild(env);
		// Initialize first model
		SwitchToModel(0);
		UpdateCamera();
	}
	private void SwitchToModel(int index)
	{
		_currentModelIndex = index;
		SvoModelDescriptor descriptor = _descriptors[index];
		_modelSize = new Vector3I(descriptor.SizeX, descriptor.SizeY, descriptor.SizeZ);
		_maxDepth = descriptor.MaxDepth;
		// Update SVO uniforms
		_material.SetShaderParameter("palette_texture", _paletteTextures[index]);
		_material.SetShaderParameter("svo_model_size", new Vector3I(descriptor.SizeX, descriptor.SizeY, descriptor.SizeZ));
		_material.SetShaderParameter("svo_max_depth", (uint)_maxDepth);
		_material.SetShaderParameter("node_offset", descriptor.NodeOffset);
		_material.SetShaderParameter("payload_offset", descriptor.PayloadOffset);
		// Calculate box size in world space
		// Voxel model size in world units
		Vector3 modelWorldSize = new Vector3(_modelSize.X, _modelSize.Y, _modelSize.Z) * _voxelSize;
		// Expand by 1 virtual pixel on each side for outline
		float deltaPx = _voxelSize / _sigma;
		Vector3 boxSize = modelWorldSize + Vector3.One * deltaPx * 2f;
		// voxel_offset is the center of the model in voxel space
		Vector3 voxelOffset = new Vector3(_modelSize.X, _modelSize.Y, _modelSize.Z) * 0.5f;
		// Create box mesh - swap Y and Z for Godot's Y-up coordinate system
		// Box in Godot: X=right, Y=up, Z=back
		// Voxel space: X=right, Y=forward, Z=up
		_proxyBox.Mesh = new BoxMesh { Size = new Vector3(boxSize.X, boxSize.Z, boxSize.Y) };
		_proxyBox.MaterialOverride = _material;
		// Update shader uniforms
		_material.SetShaderParameter("voxel_size", _voxelSize);
		_material.SetShaderParameter("sigma", _sigma);
		_material.SetShaderParameter("voxel_offset", voxelOffset);
		GD.Print($"Switched to model {index}: {descriptor.SizeX}x{descriptor.SizeY}x{descriptor.SizeZ}, box size: {boxSize}");
	}
	private void UpdateCamera()
	{
		// Position camera to look at model center
		// Rotate around Y axis (Godot up) for turntable effect
		float camX = Mathf.Sin(_rotationAngle) * _cameraDistance;
		float camZ = Mathf.Cos(_rotationAngle) * _cameraDistance;
		float camY = _cameraDistance * 0.5f; // Slight elevation
		_camera.Position = new Vector3(camX, camY, camZ);
		_camera.LookAt(Vector3.Zero, Vector3.Up);
		// Compute camera vectors
		Transform3D camTransform = _camera.GlobalTransform;
		Vector3 camPos = camTransform.Origin;
		Vector3 camForward = -camTransform.Basis.Z.Normalized(); // Camera looks down -Z
		Vector3 camRight = camTransform.Basis.X.Normalized();
		Vector3 camUp = camTransform.Basis.Y.Normalized();
		// Transform from Godot Y-up to voxel Z-up space
		// Godot: X=right, Y=up, Z=towards viewer (camera looks at -Z)
		// Voxel: X=right, Y=forward (into screen), Z=up
		// Transformation: voxel.X = godot.X, voxel.Y = -godot.Z, voxel.Z = godot.Y
		Vector3 camForwardVoxel = new Vector3(camForward.X, -camForward.Z, camForward.Y).Normalized();
		Vector3 camUpVoxel = new Vector3(camUp.X, -camUp.Z, camUp.Y).Normalized();
		// Light direction (from upper left in view space)
		Vector3 lightDirWorld = (-camRight + camUp * 0.5f - camForward).Normalized();
		Vector3 lightDirVoxel = new Vector3(lightDirWorld.X, -lightDirWorld.Z, lightDirWorld.Y).Normalized();
		// Update shader uniforms (camera_right derived in shader from cross product)
		_material.SetShaderParameter("ray_dir_local", camForwardVoxel);
		_material.SetShaderParameter("camera_up_local", camUpVoxel);
		_material.SetShaderParameter("light_dir", lightDirVoxel);
		_material.SetShaderParameter("camera_distance", camPos.Length());
	}
	public override void _Process(double delta)
	{
		// Up/Down: Adjust sigma (virtual pixels per voxel)
		if (Input.IsActionJustPressed("ui_up"))
		{
			_sigma = Mathf.Min(16f, _sigma + 1f);
			SwitchToModel(_currentModelIndex); // Rebuild box with new sigma
			GD.Print($"Sigma: {_sigma}, Voxel Size: {_voxelSize}");
		}
		if (Input.IsActionJustPressed("ui_down"))
		{
			_sigma = Mathf.Max(1f, _sigma - 1f);
			SwitchToModel(_currentModelIndex);
			GD.Print($"Sigma: {_sigma}, Voxel Size: {_voxelSize}");
		}
		// Left/Right: Adjust voxel size (zoom)
		if (Input.IsActionJustPressed("ui_right"))
		{
			_voxelSize = Mathf.Min(1f, _voxelSize * 1.25f);
			SwitchToModel(_currentModelIndex);
			GD.Print($"Sigma: {_sigma}, Voxel Size: {_voxelSize}");
		}
		if (Input.IsActionJustPressed("ui_left"))
		{
			_voxelSize = Mathf.Max(0.01f, _voxelSize / 1.25f);
			SwitchToModel(_currentModelIndex);
			GD.Print($"Sigma: {_sigma}, Voxel Size: {_voxelSize}");
		}
		// Space: Cycle to next model
		if (Input.IsActionJustPressed("ui_accept"))
			SwitchToModel((_currentModelIndex + 1) % _descriptors.Count);
		// Rotate camera around model
		_rotationAngle += (float)delta * 0.5f;
		UpdateCamera();
	}
}
