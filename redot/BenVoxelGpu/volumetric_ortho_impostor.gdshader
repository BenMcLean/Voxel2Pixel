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
uniform vec3 anchor_point;        // Anchor point position in voxel space (e.g., bottom center)
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
// anchor_offset: offset to add to convert from anchor-centered to SVO coordinates
vec4 trace_ray(vec3 ro, vec3 rd, vec3 box_min, vec3 box_max, vec3 anchor_offset, out int last_axis) {
	// Intersect with voxel AABB (in anchor-centered coordinates)
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
		// Check bounds (in anchor-centered coordinates)
		if (any(lessThan(pos, box_min)) || any(greaterThanEqual(pos, box_max))) {
			return vec4(0.0, 0.0, 0.0, 0.0); // Miss - exited bounds
		}

		// Sample voxel (convert to SVO coordinates by adding anchor offset)
		uint mat = sample_svo(pos + anchor_offset);
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
// 5=show material as grayscale, 6=show bounds check, 7=show anchor_to_center
const int DEBUG_MODE = 0;

void fragment() {
	// Derive camera right from forward and up (standard graphics convention)
	vec3 camera_right_local = cross(ray_dir_local, camera_up_local);

	// Virtual pixel size in voxel units
	float delta_px = 1.0 / sigma;

	// Model size and center in voxel space
	vec3 model_size = vec3(float(svo_model_size.x), float(svo_model_size.y), float(svo_model_size.z));
	vec3 model_center_voxel = model_size * 0.5;
	float max_dim = max(max(model_size.x, model_size.y), model_size.z);

	// Compute anchor_to_center: offset from anchor point to model center
	vec3 anchor_to_center = model_center_voxel - anchor_point;

	// In anchor-centered coordinates, the anchor is at origin (0,0,0)
	// The model center is at anchor_to_center
	// The voxel volume bounds shift accordingly
	vec3 voxel_min = -anchor_point;
	vec3 voxel_max = model_size - anchor_point;

	// Model center in anchor-centered coordinates
	vec3 model_center = anchor_to_center;

	// === Ray direction (same for all fragments - orthographic internal projection) ===
	vec3 rd = normalize(ray_dir_local);

	// === SCREEN-BASED SPRITE PIXEL ===
	// Use screen coordinates to determine sprite pixel, making it independent of box geometry.
	// All fragments at the same screen position compute the same sprite pixel.

	// Get model center's screen position for reference (box local origin = box center = model center)
	vec4 center_clip = PROJECTION_MATRIX * VIEW_MATRIX * MODEL_MATRIX * vec4(0.0, 0.0, 0.0, 1.0);
	vec2 center_ndc = center_clip.xy / center_clip.w;

	// Current fragment's NDC position
	vec2 frag_ndc = (FRAGCOORD.xy / VIEWPORT_SIZE) * 2.0 - 1.0;

	// Offset from model center in NDC
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

	// For debug modes - frag_pos in SVO coordinates (0 to size)
	// local_vertex is box-local (centered at box center), convert to voxel units and add model center
	vec3 frag_voxel = vec3(local_vertex.x, local_vertex.z, local_vertex.y) / voxel_size;
	vec3 frag_pos = frag_voxel + model_center_voxel;

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
		// Show anchor_to_center normalized
		ALBEDO = anchor_to_center / vec3(svo_model_size);
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
		vec4 hit = trace_ray(frag_pos, rd, voxel_min, voxel_max, anchor_point, last_axis);

		if (hit.x > 0.5) {
			uint mat = uint(hit.z);
			vec3 base_color = texelFetch(palette_texture, ivec2(int(mat), 0), 0).rgb;
			ALBEDO = base_color;
		} else {
			discard;
		}
	} else {
		// Normal rendering (DEBUG_MODE == 0)

		// Compute billboard depth once - all visible pixels share this depth
		// The depth is the clip-space depth of the model center
		// Box center = model center (box is positioned so its center aligns with model center)
		// So box local (0,0,0) = model center in world space after MODEL_MATRIX transform
		vec4 model_center_clip = PROJECTION_MATRIX * VIEW_MATRIX * MODEL_MATRIX * vec4(0.0, 0.0, 0.0, 1.0);
		float billboard_depth = (model_center_clip.z / model_center_clip.w) * 0.5 + 0.5;

		// Primary ray trace
		int last_axis;
		vec4 hit = trace_ray(ro_voxel, rd, voxel_min, voxel_max, anchor_point, last_axis);

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

			// Billboard depth - all pixels share the same depth at sprite plane
			DEPTH = billboard_depth;
		} else {
			// Miss - check neighbor rays for outline (cardinal directions only)
			vec3 offsets[4];
			offsets[0] = -camera_right_local * delta_px; // Left
			offsets[1] = camera_right_local * delta_px;  // Right
			offsets[2] = camera_up_local * delta_px;     // Up
			offsets[3] = -camera_up_local * delta_px;    // Down

			bool outline = false;

			for (int n = 0; n < 4; n++) {
				vec3 neighbor_ro = ro_voxel + offsets[n];
				int neighbor_axis;
				vec4 neighbor_hit = trace_ray(neighbor_ro, rd, voxel_min, voxel_max, anchor_point, neighbor_axis);
				if (neighbor_hit.x > 0.5) {
					outline = true;
					break; // Found an outline, no need to check more neighbors
				}
			}

			if (outline) {
				// Output black outline with billboard depth
				ALBEDO = OUTLINE_COLOR;
				DEPTH = billboard_depth;
			} else {
				// Transparent - discard fragment
				// discard;
				// Debug: show proxy box extent as grey
				ALBEDO = vec3(0.5);
				DEPTH = billboard_depth;
			}
		}
	}
}
