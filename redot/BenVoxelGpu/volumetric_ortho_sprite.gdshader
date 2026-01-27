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

// Volumetric sprite parameters
uniform vec3 ray_dir_local;       // Camera forward in voxel space (normalized)
uniform vec3 camera_up_local;     // Camera up (V) in voxel space (normalized)
uniform float voxel_size;         // World-space size of one voxel
uniform float sigma;              // Virtual pixels per voxel
uniform vec3 anchor_point;        // Anchor point position in voxel space (e.g., bottom center)
uniform vec3 light_dir;           // Light direction for shading (normalized)
uniform float camera_distance;    // Distance from camera to model center (world units)

const uint FLAG_INTERNAL = 0x80000000u;
const uint FLAG_LEAF_TYPE = 0x40000000u;
const vec3 OUTLINE_COLOR = vec3(0.0);

void vertex() {}

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

// Ray-AABB intersection, returns (t_enter, t_exit), t_enter < 0 means inside or behind
vec2 ray_aabb(vec3 ro, vec3 inv_rd, vec3 box_min, vec3 box_max) {
	vec3 t1 = (box_min - ro) * inv_rd;
	vec3 t2 = (box_max - ro) * inv_rd;
	vec3 t_min = min(t1, t2);
	vec3 t_max = max(t1, t2);
	float t_enter = max(max(t_min.x, t_min.y), t_min.z);
	float t_exit = min(min(t_max.x, t_max.y), t_max.z);
	return vec2(t_enter, t_exit);
}

// Hierarchical SVO traversal with empty-space skipping
// Returns (hit, t_hit, material, unused)
// Uses SVO structure to skip empty nodes instead of voxel-by-voxel stepping
vec4 trace_ray_hierarchical(vec3 ro, vec3 rd, vec3 anchor_offset, vec3 model_size, vec3 step_dir, vec3 inv_rd, out int last_axis) {
	// Work in SVO coordinates (0 to model_size)
	vec3 ro_svo = ro + anchor_offset;

	// Inline ray-AABB intersection and entry face detection, sharing intermediates
	vec3 t1 = -ro_svo * inv_rd;
	vec3 t2 = (model_size - ro_svo) * inv_rd;
	vec3 t_min_v = min(t1, t2);
	vec3 t_max_v = max(t1, t2);
	float t_enter = max(max(t_min_v.x, t_min_v.y), t_min_v.z);
	float t_exit = min(min(t_max_v.x, t_max_v.y), t_max_v.z);

	if (t_exit < max(t_enter, 0.0)) {
		last_axis = 0;
		return vec4(0.0, 0.0, 0.0, 0.0); // Miss - ray doesn't intersect model
	}

	float t_current = max(t_enter, 0.0) + 0.001;

	// Maximum iterations bounded by tree depth traversals
	int max_iterations = int(svo_max_depth) * 64;

	// Entry face is the axis that produced t_enter (the largest t_min component)
	if (t_min_v.x >= t_min_v.y && t_min_v.x >= t_min_v.z) {
		last_axis = 0;
	} else if (t_min_v.y >= t_min_v.z) {
		last_axis = 1;
	} else {
		last_axis = 2;
	}

	for (int iter = 0; iter < max_iterations; iter++) {
		if (t_current >= t_exit) {
			return vec4(0.0, 0.0, 0.0, 0.0); // Exited bounds
		}

		vec3 pos = ro_svo + rd * t_current;

		// Clamp to valid bounds (handle edge cases)
		if (any(lessThan(pos, vec3(0.0))) || any(greaterThanEqual(pos, model_size))) {
			return vec4(0.0, 0.0, 0.0, 0.0);
		}

		uvec3 upos = uvec3(pos);
		uint node_idx = 0u;
		float node_size = float(1u << (svo_max_depth - 1u)); // Size of root node children

		// Traverse down the tree
		for (int depth = 0; depth < int(svo_max_depth) - 1; depth++) {
			uint node_data = read_node(node_idx);

			if ((node_data & FLAG_INTERNAL) == 0u) {
				// Leaf node - check material
				if ((node_data & FLAG_LEAF_TYPE) == 0u) {
					// Uniform leaf
					uint mat = node_data & 0xFFu;
					if (mat > 0u) {
						return vec4(1.0, t_current, float(mat), float(last_axis));
					}
					// Empty uniform leaf - skip entire node
					break;
				} else {
					// 2x2x2 payload leaf - sample exact voxel
					uint payload_idx = node_data & ~FLAG_LEAF_TYPE;
					int octant = ((int(upos.z) & 1) << 2) | ((int(upos.y) & 1) << 1) | (int(upos.x) & 1);
					uint mat = read_payload_byte(payload_idx, octant);
					if (mat > 0u) {
						return vec4(1.0, t_current, float(mat), float(last_axis));
					}
					// Empty voxel in leaf - advance by 1 voxel
					node_size = 1.0;
					break;
				}
			}

			// Internal node - check child occupancy
			uint mask = node_data & 0xFFu;
			uint shift = uint(int(svo_max_depth) - 1 - depth);
			uint octant = (((upos.z >> shift) & 1u) << 2u) | (((upos.y >> shift) & 1u) << 1u) | ((upos.x >> shift) & 1u);

			if (((mask >> octant) & 1u) == 0u) {
				// Child is empty - skip this entire octant
				break;
			}

			// Descend into occupied child
			uint child_base = (node_data & ~FLAG_INTERNAL) >> 8u;
			uint offset = uint(bitCount(mask & ((1u << octant) - 1u)));
			node_idx = child_base + offset;
			node_size *= 0.5;
		}

		// Handle deepest level if we didn't break early
		if (node_size < 1.5) {
			uint node_data = read_node(node_idx);
			if ((node_data & FLAG_LEAF_TYPE) == 0u) {
				uint mat = node_data & 0xFFu;
				if (mat > 0u) {
					return vec4(1.0, t_current, float(mat), float(last_axis));
				}
			} else {
				uint payload_idx = node_data & ~FLAG_LEAF_TYPE;
				int octant = ((int(upos.z) & 1) << 2) | ((int(upos.y) & 1) << 1) | (int(upos.x) & 1);
				uint mat = read_payload_byte(payload_idx, octant);
				if (mat > 0u) {
					return vec4(1.0, t_current, float(mat), float(last_axis));
				}
			}
			node_size = 1.0;
		}

		// Compute distance to exit current node and advance ray
		float effective_size = max(node_size, 1.0);
		vec3 node_min = floor(pos / effective_size) * effective_size;
		vec3 node_max = node_min + vec3(effective_size);

		// Compute t values to exit the node on each axis
		vec3 t_exit_axis = (mix(node_min, node_max, max(step_dir, vec3(0.0))) - ro_svo) * inv_rd;

		// Find the minimum exit t (first axis we exit)
		float t_next;
		if (t_exit_axis.x < t_exit_axis.y) {
			if (t_exit_axis.x < t_exit_axis.z) {
				t_next = t_exit_axis.x;
				last_axis = 0;
			} else {
				t_next = t_exit_axis.z;
				last_axis = 2;
			}
		} else {
			if (t_exit_axis.y < t_exit_axis.z) {
				t_next = t_exit_axis.y;
				last_axis = 1;
			} else {
				t_next = t_exit_axis.z;
				last_axis = 2;
			}
		}

		// Advance ray with small epsilon to enter next node
		t_current = t_next + 0.001;
	}

	return vec4(0.0, 0.0, 0.0, 0.0); // Max iterations exceeded
}

void fragment() {
	// Derive camera right from forward and up (standard graphics convention)
	vec3 camera_right_local = cross(ray_dir_local, camera_up_local);

	// Virtual pixel size in voxel units
	float delta_px = 1.0 / sigma;

	// Model size and center in voxel space
	vec3 model_size = vec3(float(svo_model_size.x), float(svo_model_size.y), float(svo_model_size.z));
	float max_dim = max(max(model_size.x, model_size.y), model_size.z);

	// Anchor-centered model center
	vec3 model_center = model_size * 0.5 - anchor_point;

	// Ray direction and precomputed traversal constants (shared across all trace calls)
	vec3 rd = ray_dir_local;
	vec3 step_dir = sign(rd);
	vec3 inv_rd = 1.0 / rd;

	// Clip-space position of local origin (used for NDC and billboard depth)
	vec4 origin_clip = PROJECTION_MATRIX * VIEW_MATRIX * MODEL_MATRIX * vec4(0.0, 0.0, 0.0, 1.0);

	// === SCREEN-BASED SPRITE PIXEL ===
	vec2 center_ndc = origin_clip.xy / origin_clip.w;
	vec2 frag_ndc = (FRAGCOORD.xy / VIEWPORT_SIZE) * 2.0 - 1.0;
	vec2 offset_ndc = frag_ndc - center_ndc;

	// Convert NDC offset to voxel units
	float u_coord = offset_ndc.x * camera_distance / (PROJECTION_MATRIX[0][0] * voxel_size);
	float v_coord = offset_ndc.y * camera_distance / (PROJECTION_MATRIX[1][1] * voxel_size);

	// Quantize to virtual pixel grid
	float u_snapped = round(u_coord / delta_px) * delta_px;
	float v_snapped = round(v_coord / delta_px) * delta_px;

	// === Parallel Ray Origin ===
	// Fire parallel ray from quantized sprite position, starting in front of the model
	vec3 ro_voxel = model_center
				  + u_snapped * camera_right_local
				  + v_snapped * camera_up_local
				  - max_dim * 2.0 * rd;

	// === PRE-TRAVERSAL AABB REJECTION (O(1) early exit) ===
	vec3 ro_svo = ro_voxel + anchor_point;
	vec2 t_bounds = ray_aabb(ro_svo, inv_rd, vec3(0.0), model_size);
	bool ray_can_hit = (t_bounds.y >= max(t_bounds.x, 0.0));

	// Check if any neighbor ray could possibly hit (for outline detection)
	vec2 t_expanded = ray_aabb(ro_svo, inv_rd, -vec3(delta_px), model_size + vec3(delta_px));
	if (t_expanded.y < max(t_expanded.x, 0.0)) {
		discard;
	}

	// Billboard depth from the already-computed clip position
	float billboard_depth = (origin_clip.z / origin_clip.w) * 0.5 + 0.5;

	// Precompute per-face lighting for the 3 visible faces.
	// Each axis has one visible face whose normal points toward the camera.
	// normal = -sign(rd) on that axis, so dot(normal, light) = -sign(rd.c) * light.c
	float face_light_0 = max(-step_dir.x * light_dir.x, 0.0); // X-axis face
	float face_light_1 = max(-step_dir.y * light_dir.y, 0.0); // Y-axis face
	float face_light_2 = max(-step_dir.z * light_dir.z, 0.0); // Z-axis face
	float ambient = 0.3;

	if (!ray_can_hit) {
		// Primary ray misses, but neighbor rays might hit - check for outline
		vec3 right_offset = camera_right_local * delta_px;
		vec3 up_offset = camera_up_local * delta_px;

		bool outline = false;

		vec3 neighbor_ro = ro_voxel - right_offset;
		int neighbor_axis;
		if (trace_ray_hierarchical(neighbor_ro, rd, anchor_point, model_size, step_dir, inv_rd, neighbor_axis).x > 0.5) { outline = true; }

		if (!outline) {
			neighbor_ro = ro_voxel + right_offset;
			if (trace_ray_hierarchical(neighbor_ro, rd, anchor_point, model_size, step_dir, inv_rd, neighbor_axis).x > 0.5) { outline = true; }
		}
		if (!outline) {
			neighbor_ro = ro_voxel + up_offset;
			if (trace_ray_hierarchical(neighbor_ro, rd, anchor_point, model_size, step_dir, inv_rd, neighbor_axis).x > 0.5) { outline = true; }
		}
		if (!outline) {
			neighbor_ro = ro_voxel - up_offset;
			if (trace_ray_hierarchical(neighbor_ro, rd, anchor_point, model_size, step_dir, inv_rd, neighbor_axis).x > 0.5) { outline = true; }
		}

		if (outline) {
			ALBEDO = OUTLINE_COLOR;
			DEPTH = billboard_depth;
		} else {
			discard;
		}
	} else {
		// Primary ray can hit - use hierarchical traversal
		int last_axis;
		vec4 hit = trace_ray_hierarchical(ro_voxel, rd, anchor_point, model_size, step_dir, inv_rd, last_axis);

		if (hit.x > 0.5) {
			// Hit - output material color with per-face lighting
			uint mat = uint(hit.z);

			float diff;
			if (last_axis == 0) diff = face_light_0;
			else if (last_axis == 1) diff = face_light_1;
			else diff = face_light_2;

			vec3 base_color = texelFetch(palette_texture, ivec2(int(mat), 0), 0).rgb;
			ALBEDO = base_color * (diff + ambient);

			// Billboard depth - all pixels share the same depth at sprite plane
			DEPTH = billboard_depth;
		} else {
			// Miss - check neighbor rays for outline (cardinal directions only)
			vec3 right_offset = camera_right_local * delta_px;
			vec3 up_offset = camera_up_local * delta_px;

			bool outline = false;

			vec3 neighbor_ro = ro_voxel - right_offset;
			int neighbor_axis;
			if (trace_ray_hierarchical(neighbor_ro, rd, anchor_point, model_size, step_dir, inv_rd, neighbor_axis).x > 0.5) { outline = true; }

			if (!outline) {
				neighbor_ro = ro_voxel + right_offset;
				if (trace_ray_hierarchical(neighbor_ro, rd, anchor_point, model_size, step_dir, inv_rd, neighbor_axis).x > 0.5) { outline = true; }
			}
			if (!outline) {
				neighbor_ro = ro_voxel + up_offset;
				if (trace_ray_hierarchical(neighbor_ro, rd, anchor_point, model_size, step_dir, inv_rd, neighbor_axis).x > 0.5) { outline = true; }
			}
			if (!outline) {
				neighbor_ro = ro_voxel - up_offset;
				if (trace_ray_hierarchical(neighbor_ro, rd, anchor_point, model_size, step_dir, inv_rd, neighbor_axis).x > 0.5) { outline = true; }
			}

			if (outline) {
				ALBEDO = OUTLINE_COLOR;
				DEPTH = billboard_depth;
			} else {
				discard;
			}
		}
	}
}
