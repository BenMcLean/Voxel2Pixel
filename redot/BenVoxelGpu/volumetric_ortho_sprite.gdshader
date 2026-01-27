// =============================================================================
// Volumetric Ortho-Sprite Fragment Shader
// =============================================================================
//
// PURPOSE
// -------
// Renders a 3D voxel model (stored as a GPU Sparse Voxel Octree) as a
// sprite-like entity inside a perspective 3D world. Each entity appears
// orthographically projected: parallel rays, a stable virtual pixel grid,
// and a 1-pixel cardinal outline — all in a single fragment pass with no
// post-processing.
//
// HOW IT WORKS (high level)
// -------------------------
// 1. The proxy QuadMesh is rasterized with the engine's perspective camera,
//    generating fragments.
// 2. Each fragment computes its position on a quantized virtual pixel grid
//    derived from screen coordinates, not from the box surface.
// 3. A parallel ray (shared direction, per-pixel origin) is fired through
//    the voxel volume using hierarchical SVO traversal.
// 4. If the ray hits a solid voxel, the voxel's palette color is output
//    with per-face directional lighting.
// 5. If the ray misses, four cardinal neighbor rays are tested; if any
//    neighbor hits, a black outline pixel is output.
// 6. All visible pixels share the same billboard depth (the depth of the
//    model center), preserving the 2D-sprite illusion.
//
// COORDINATE SPACES
// -----------------
// - World space:   Engine coordinates (Godot Y-up). The perspective camera
//                  lives here.
// - Voxel space:   Right-handed, Z-up. The SVO data and all ray math use
//                  this space. The CPU converts camera vectors from world
//                  space to voxel space before passing them as uniforms.
// - Anchor space:  Voxel space translated so the anchor point is at the
//                  origin. Ray origins are expressed in this space; the SVO
//                  lookup adds anchor_point back to get SVO coordinates.
// - Sprite space:  2D grid (U = camera right, V = camera up) in the plane
//                  perpendicular to the ray direction.
//
// ASSUMPTIONS / CONTRACTS
// -----------------------
// - ray_dir_local and camera_up_local are unit vectors (normalized on CPU).
// - light_dir is a unit vector (normalized on CPU).
// - The proxy quad is a camera-facing rectangle sized to the tight bounding
//   rectangle of the model's orthographic projection onto the camera plane,
//   plus a 1-virtual-pixel outline margin on each side. The CPU recomputes
//   the quad size and orientation each frame. See the rendering specification
//   for the exact sizing formula.
// - svo_texture packs uint32 node/payload data as RGBA8 pixels.
//   Byte order: R = bits 0–7, G = bits 8–15, B = bits 16–23, A = bits 24–31.
// - palette_texture is a 1D row of 256 RGBA colors indexed by material ID.
//   Material 0 means "empty / air".
// - sigma > 0 (virtual pixels per voxel; determines visual resolution).
// - camera_distance > 0 (distance from the camera to the anchor point).
// =============================================================================

shader_type spatial;

// "unshaded"  — we do our own lighting; skip engine lights entirely.
// "cull_back" — the proxy quad is billboard-oriented to face the camera
//               every frame, so back faces are never visible.
render_mode unshaded, cull_back;

// ─── SVO data uniforms ───────────────────────────────────────────────────────
// These describe the GPU-resident Sparse Voxel Octree and its color palette.

// Packed SVO node + payload texture.  Each texel is one RGBA8 pixel encoding
// a uint32.  Nodes come first (svo_nodes_count of them), then payloads.
uniform sampler2D svo_texture : filter_nearest;

// Color palette: row of 256 RGBA colors.  Index 0 is unused (empty voxel).
uniform sampler2D palette_texture : filter_nearest;

// Width of svo_texture in pixels.  Needed to convert a linear pixel index
// into a 2D (x, y) coordinate for texelFetch.
uniform int texture_width;

// Total number of node entries in svo_texture (shared across all models in
// the texture).  Payload data starts immediately after the nodes.
uniform uint svo_nodes_count;

// Dimensions of this model's voxel grid (e.g., 32×32×64).
uniform uvec3 svo_model_size;

// Maximum depth of the SVO tree.  Determines the number of levels to
// traverse and the size of octant cells at each level.
uniform uint svo_max_depth;

// Byte offset (in node units) to this model's first node in svo_texture.
// Allows multiple models to share one texture.
uniform uint node_offset;

// Byte offset (in payload units) to this model's first payload.
uniform uint payload_offset;

// ─── Volumetric sprite uniforms ──────────────────────────────────────────────
// These are set per-frame by VolumetricOrthoSprite.cs on the CPU.

// Camera forward direction in voxel space (Z-up), unit length.
// All rays for this entity share this direction (orthographic projection).
uniform vec3 ray_dir_local;

// Camera up direction in voxel space (Z-up), unit length.
// Together with ray_dir_local, defines the sprite plane orientation.
// Camera right is derived as cross(ray_dir_local, camera_up_local).
uniform vec3 camera_up_local;

// World-space size of one voxel in meters.  Controls how large sprites
// appear in the scene.  Shared across all entities.
uniform float voxel_size;

// Virtual pixels per voxel (scalar).  A higher sigma means more virtual
// pixels per voxel, yielding a finer pixel grid.  Shared across all entities.
uniform float sigma;

// Position of the anchor point in voxel space.  The anchor is the point
// in the voxel model that corresponds to the entity's world-space origin.
// Typically bottom-center: (sizeX/2, sizeY/2, 0).
uniform vec3 anchor_point;

// Directional light direction in voxel space (Z-up), unit length.
// Points FROM the surface TOWARD the light source (standard convention).
// Set by VolumetricOrthoSprite.cs, which converts from world space.
uniform vec3 light_dir;

// Distance from the camera to the anchor point in world units.
// Used to convert NDC offsets to world-space offsets for the sprite grid.
uniform float camera_distance;

// ─── Constants ───────────────────────────────────────────────────────────────

// SVO node flag: bit 31 set means "internal node" (has children).
// Clear means "leaf node" (uniform material or 2×2×2 payload).
const uint FLAG_INTERNAL = 0x80000000u;

// SVO leaf flag: bit 30 set means "payload leaf" (2×2×2 voxel data stored
// in a separate payload entry).  Clear means "uniform leaf" (all 8 children
// share one material, stored in bits 0–7 of the node word).
const uint FLAG_LEAF_TYPE = 0x40000000u;

// Outline color.  Black, matching classic 2D sprite outlines.
const vec3 OUTLINE_COLOR = vec3(0.0);

// ─── Vertex shader ───────────────────────────────────────────────────────────
// Nothing to do.  The proxy quad geometry only exists to generate fragments;
// all rendering math happens in the fragment shader.
void vertex() {}

// =============================================================================
// SVO Texture Access
// =============================================================================
// The SVO is stored as a flat array of uint32 values packed into an RGBA8
// texture.  Each pixel holds one uint32 in little-endian byte order:
//   R = byte 0 (bits  0– 7)
//   G = byte 1 (bits  8–15)
//   B = byte 2 (bits 16–23)
//   A = byte 3 (bits 24–31)
//
// To read entry N, we compute its 2D texel coordinate from the texture width
// and fetch with texelFetch (no filtering).

// Read one uint32 from the SVO texture at the given linear pixel index.
uint read_uint(int pixel_idx) {
	int x = pixel_idx % texture_width;
	int y = pixel_idx / texture_width;
	vec4 pixel = texelFetch(svo_texture, ivec2(x, y), 0);
	return uint(pixel.r * 255.0)
		| (uint(pixel.g * 255.0) << 8u)
		| (uint(pixel.b * 255.0) << 16u)
		| (uint(pixel.a * 255.0) << 24u);
}

// Read the SVO node at the given index (offset by this model's node_offset).
uint read_node(uint idx) {
	return read_uint(int(node_offset + idx));
}

// Read one byte from a 2×2×2 payload leaf.
// payload_idx: which payload entry (relative to this model's payload_offset).
// octant:      which of the 8 voxels (0–7) within the 2×2×2 block.
//              Bit layout: (z & 1) << 2 | (y & 1) << 1 | (x & 1).
// Each payload entry occupies 2 texture pixels (8 bytes); the octant selects
// which byte within those 8 bytes.
uint read_payload_byte(uint payload_idx, int octant) {
	int base_pixel = int(svo_nodes_count + (payload_offset + payload_idx) * 2u);
	int pixel_offset = octant / 4;      // Which of the 2 pixels
	int byte_in_pixel = octant % 4;     // Which byte within that pixel
	uint pixel_data = read_uint(base_pixel + pixel_offset);
	return (pixel_data >> uint(byte_in_pixel * 8)) & 0xFFu;
}

// =============================================================================
// Ray–AABB Intersection
// =============================================================================
// Standard slab method.  Returns (t_enter, t_exit) along the ray.
// If t_exit < max(t_enter, 0), the ray misses the box.
//
// Takes inv_rd (1/rd) instead of rd to avoid redundant divisions — the caller
// precomputes inv_rd once and reuses it across all AABB tests and traversal.

vec2 ray_aabb(vec3 ro, vec3 inv_rd, vec3 box_min, vec3 box_max) {
	vec3 t1 = (box_min - ro) * inv_rd;
	vec3 t2 = (box_max - ro) * inv_rd;
	vec3 t_min = min(t1, t2);  // Entry t per axis (handles negative rd components)
	vec3 t_max = max(t1, t2);  // Exit  t per axis
	float t_enter = max(max(t_min.x, t_min.y), t_min.z);  // Last axis to enter
	float t_exit  = min(min(t_max.x, t_max.y), t_max.z);  // First axis to exit
	return vec2(t_enter, t_exit);
}

// =============================================================================
// Hierarchical SVO Ray Traversal
// =============================================================================
// Traces a ray through the voxel model using the SVO hierarchy to skip empty
// regions.  This is the core rendering routine — called once for the primary
// ray and up to four more times for outline neighbor rays.
//
// PARAMETERS (all in voxel space, Z-up):
//   ro           – Ray origin in anchor-centered coordinates (anchor = origin).
//   rd           – Ray direction (unit vector, same for all rays in this entity).
//   anchor_offset– The anchor_point; added to ro to get SVO coordinates [0..size].
//   model_size   – vec3(svo_model_size), precomputed by the caller.
//   step_dir     – sign(rd), precomputed.  {-1, 0, +1} per axis.
//   inv_rd       – 1.0 / rd, precomputed.
//
// OUTPUTS:
//   last_axis    – Which axis (0=X, 1=Y, 2=Z) the ray crossed to enter the
//                  hit voxel.  Used to determine which face was hit for lighting.
//   return.x     – 1.0 if hit, 0.0 if miss.
//   return.y     – t value along ray at hit point (unused by caller currently).
//   return.z     – Material index (float-encoded uint, 1–255).
//   return.w     – Reserved (currently last_axis, but caller uses the out param).
//
// ALGORITHM:
//   1. Intersect ray with the model's axis-aligned bounding box [0, model_size].
//   2. Determine which AABB face the ray enters through (initial last_axis).
//   3. Step along the ray.  At each position:
//      a. Descend the SVO tree from root to leaf for the current voxel.
//      b. If a solid voxel is found, return hit + material.
//      c. If empty, compute the exit point of the current SVO node's bounding
//         box and advance the ray past it (empty-space skipping).
//   4. Stop when the ray exits the AABB or max iterations are reached.

vec4 trace_ray_hierarchical(vec3 ro, vec3 rd, vec3 anchor_offset, vec3 model_size, vec3 step_dir, vec3 inv_rd, out int last_axis) {
	// Convert ray origin from anchor-centered coords to SVO coords [0..size]
	vec3 ro_svo = ro + anchor_offset;

	// ── Ray–AABB intersection + entry face detection ──
	// Inline the slab test here so we can reuse t1/t2 to determine which
	// AABB face the ray enters through, without redundant divisions.
	// t1 = t at the box_min face (origin face, i.e. 0.0 on each axis)
	// t2 = t at the box_max face (model_size on each axis)
	vec3 t1 = -ro_svo * inv_rd;               // (vec3(0.0) - ro_svo) * inv_rd
	vec3 t2 = (model_size - ro_svo) * inv_rd;
	vec3 t_min_v = min(t1, t2);  // Entry t per axis
	vec3 t_max_v = max(t1, t2);  // Exit  t per axis
	float t_enter = max(max(t_min_v.x, t_min_v.y), t_min_v.z);
	float t_exit  = min(min(t_max_v.x, t_max_v.y), t_max_v.z);

	if (t_exit < max(t_enter, 0.0)) {
		last_axis = 0;
		return vec4(0.0, 0.0, 0.0, 0.0); // Miss — ray doesn't intersect model
	}

	// Start just inside the AABB (epsilon avoids landing exactly on a face)
	float t_current = max(t_enter, 0.0) + 0.001;

	// Iteration budget.  Each outer iteration does one SVO descent + one node
	// skip, so the cost is bounded by (tree depth × nodes visited along ray).
	int max_iterations = int(svo_max_depth) * 64;

	// The initial last_axis is the AABB entry face: the axis whose t_min
	// component equals t_enter (i.e., the axis that was entered last).
	if (t_min_v.x >= t_min_v.y && t_min_v.x >= t_min_v.z) {
		last_axis = 0;
	} else if (t_min_v.y >= t_min_v.z) {
		last_axis = 1;
	} else {
		last_axis = 2;
	}

	// ── Precompute loop-invariant values ──
	// Initial child node size (half the full grid, since root has 8 children).
	float root_child_size = float(1u << (svo_max_depth - 1u));
	// step_dir mapped to {0, 1}: selects between node_min (0) and node_max (1)
	// for computing the exit face of a node along each axis.
	vec3 step_positive = max(step_dir, vec3(0.0));

	// ── Main traversal loop ──
	for (int iter = 0; iter < max_iterations; iter++) {
		if (t_current >= t_exit) {
			return vec4(0.0, 0.0, 0.0, 0.0); // Exited AABB — miss
		}

		vec3 pos = ro_svo + rd * t_current;

		// Guard against floating-point edge cases at AABB boundaries
		if (any(lessThan(pos, vec3(0.0))) || any(greaterThanEqual(pos, model_size))) {
			return vec4(0.0, 0.0, 0.0, 0.0);
		}

		// Integer voxel coordinate for SVO octant calculations
		uvec3 upos = uvec3(pos);
		uint node_idx = 0u;
		float node_size = root_child_size;

		// ── Descend the SVO tree from root to leaf ──
		for (int depth = 0; depth < int(svo_max_depth) - 1; depth++) {
			uint node_data = read_node(node_idx);

			if ((node_data & FLAG_INTERNAL) == 0u) {
				// ── Leaf node ──
				if ((node_data & FLAG_LEAF_TYPE) == 0u) {
					// Uniform leaf: all 8 children share one material (bits 0–7).
					uint mat = node_data & 0xFFu;
					if (mat > 0u) {
						return vec4(1.0, t_current, float(mat), float(last_axis));
					}
					// Material 0 = empty.  Skip the entire node region.
					break;
				} else {
					// Payload leaf: 2×2×2 individually-addressed voxels.
					uint payload_idx = node_data & ~FLAG_LEAF_TYPE;
					int octant = ((int(upos.z) & 1) << 2) | ((int(upos.y) & 1) << 1) | (int(upos.x) & 1);
					uint mat = read_payload_byte(payload_idx, octant);
					if (mat > 0u) {
						return vec4(1.0, t_current, float(mat), float(last_axis));
					}
					// This specific voxel is empty.  Advance by 1 voxel.
					node_size = 1.0;
					break;
				}
			}

			// ── Internal node — descend to the occupied child ──
			// The node's low byte is an 8-bit occupancy mask (one bit per child).
			uint mask = node_data & 0xFFu;
			// Determine which octant the current position falls into at this depth.
			uint shift = uint(int(svo_max_depth) - 1 - depth);
			uint octant = (((upos.z >> shift) & 1u) << 2u)
						| (((upos.y >> shift) & 1u) << 1u)
						|  ((upos.x >> shift) & 1u);

			if (((mask >> octant) & 1u) == 0u) {
				// Child is empty — skip this entire octant region.
				break;
			}

			// Child is occupied.  Compute its index:
			// child_base is the starting index for this node's children;
			// offset is how many occupied children precede this octant (popcount).
			uint child_base = (node_data & ~FLAG_INTERNAL) >> 8u;
			uint offset = uint(bitCount(mask & ((1u << octant) - 1u)));
			node_idx = child_base + offset;
			node_size *= 0.5;
		}

		// ── Deepest level handling ──
		// If we descended to the bottom of the tree without breaking early,
		// node_size will be ~1.0.  Check the leaf at the final level.
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

		// ── Advance past the current (empty) node ──
		// Compute the axis-aligned bounding box of the SVO node we just found
		// empty, then find where the ray exits that box.
		float effective_size = max(node_size, 1.0);
		vec3 node_min = floor(pos / effective_size) * effective_size;
		vec3 node_max = node_min + vec3(effective_size);

		// For each axis, compute the ray t at the exit face of this node.
		// step_positive selects node_max for positive ray direction, node_min
		// for negative, via mix (linear interpolation with 0/1 weights).
		vec3 t_exit_axis = (mix(node_min, node_max, step_positive) - ro_svo) * inv_rd;

		// The ray exits through whichever axis has the smallest t (first exit).
		// That axis becomes last_axis for the next iteration (the face the ray
		// will enter the next node through).
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

		// Step just past the node boundary into the next node
		t_current = t_next + 0.001;
	}

	return vec4(0.0, 0.0, 0.0, 0.0); // Max iterations exceeded — treat as miss
}

// =============================================================================
// Fragment Shader
// =============================================================================
// Every fragment of the proxy box executes this.  Most fragments will discard
// (the box is much larger than the model's silhouette).  The survivors produce
// either a lit voxel color or a black outline pixel.

void fragment() {
	// ── Derive camera basis vectors in voxel space ──
	// ray_dir_local = camera forward (into screen).
	// camera_up_local = camera up.
	// camera_right_local = camera right, derived via cross product.
	// These define the sprite plane: right = U axis, up = V axis.
	vec3 camera_right_local = cross(ray_dir_local, camera_up_local);

	// ── Virtual pixel grid parameters ──
	// delta_px is the width of one virtual pixel in voxel units.
	// With sigma = 4, each voxel spans 4 virtual pixels, so delta_px = 0.25.
	float delta_px = 1.0 / sigma;

	// ── Model geometry in voxel space ──
	vec3 model_size = vec3(float(svo_model_size.x), float(svo_model_size.y), float(svo_model_size.z));
	float max_dim = max(max(model_size.x, model_size.y), model_size.z);

	// model_center: the center of the voxel grid in anchor-centered
	// coordinates (where anchor_point is at the origin).
	// Example: 32×32×64 model with anchor (16, 16, 0) → model_center = (0, 0, 32).
	vec3 model_center = model_size * 0.5 - anchor_point;

	// ── Ray direction + precomputed traversal constants ──
	// These are constant for all rays in this entity and are passed into
	// every trace_ray_hierarchical call to avoid redundant recomputation.
	vec3 rd = ray_dir_local;         // Already unit length (normalized on CPU)
	vec3 step_dir = sign(rd);        // {-1, 0, +1} per axis: DDA step direction
	vec3 inv_rd = 1.0 / rd;          // Reciprocal ray direction for slab tests

	// ── Clip-space position of the proxy box's local origin ──
	// The box's local origin is the anchor point in world space (the entity's
	// position).  This single matrix multiply is reused for both:
	//   computing the model center's screen position (NDC) for sprite grid alignment.
	//   Billboard depth is handled by the quad's rasterized depth (the quad is
	//   camera-facing and centered on the model center, so its natural depth is correct).
	vec4 origin_clip = PROJECTION_MATRIX * VIEW_MATRIX * MODEL_MATRIX * vec4(0.0, 0.0, 0.0, 1.0);

	// ── Screen-based sprite pixel determination ──
	// Instead of using the box surface position (which varies with perspective),
	// we derive the sprite pixel from the fragment's screen position relative
	// to the anchor's screen position.  This ensures:
	//   - All fragments at the same screen position compute the same sprite pixel
	//   - The grid is stable as the camera moves (no sub-pixel jitter)
	//   - The grid is independent of box geometry

	// Anchor's position in NDC (Normalized Device Coordinates, range [-1, +1])
	vec2 center_ndc = origin_clip.xy / origin_clip.w;

	// Fragment's position in NDC
	vec2 frag_ndc = (FRAGCOORD.xy / VIEWPORT_SIZE) * 2.0 - 1.0;

	// Signed offset from anchor to fragment in NDC
	vec2 offset_ndc = frag_ndc - center_ndc;

	// Convert NDC offset → voxel units in one step.
	// Derivation:
	//   world_offset = offset_ndc * camera_distance * (1 / projection_scale)
	//   voxel_offset = world_offset / voxel_size
	// Combined: voxel_offset = offset_ndc * camera_distance / (proj[c][c] * voxel_size)
	float u_coord = offset_ndc.x * camera_distance / (PROJECTION_MATRIX[0][0] * voxel_size);
	float v_coord = offset_ndc.y * camera_distance / (PROJECTION_MATRIX[1][1] * voxel_size);

	// Snap to the nearest virtual pixel center.
	// round() ensures each virtual pixel maps to a contiguous block of screen
	// fragments, producing the stable pixelated look.
	float u_snapped = round(u_coord / delta_px) * delta_px;
	float v_snapped = round(v_coord / delta_px) * delta_px;

	// ── Parallel ray origin ──
	// Start at the model center (in anchor-centered coords), offset by the
	// snapped sprite pixel position along camera right and up, then pull back
	// along the ray direction far enough to be in front of the entire model.
	// The factor 2× max_dim guarantees the ray starts outside the bounding box.
	vec3 ro_voxel = model_center
				  + u_snapped * camera_right_local
				  + v_snapped * camera_up_local
				  - max_dim * 2.0 * rd;

	// ── Pre-traversal AABB rejection (O(1) early exit) ──
	// Before any SVO traversal, test whether this ray can possibly intersect
	// the voxel volume.  This is critical for performance: the proxy box is
	// larger than the model (to accommodate worst-case orthographic projection),
	// so many fragments correspond to rays that miss entirely.

	// Convert ray origin to SVO coordinates for the AABB test
	vec3 ro_svo = ro_voxel + anchor_point;

	// Test primary ray against the exact model AABB [0, model_size]
	vec2 t_bounds = ray_aabb(ro_svo, inv_rd, vec3(0.0), model_size);
	bool ray_can_hit = (t_bounds.y >= max(t_bounds.x, 0.0));

	// Test against an expanded AABB that accounts for neighbor ray offsets.
	// If even the expanded box misses, no neighbor ray can hit either, so
	// this fragment cannot be an outline pixel.  Discard immediately.
	vec2 t_expanded = ray_aabb(ro_svo, inv_rd, -vec3(delta_px), model_size + vec3(delta_px));
	if (t_expanded.y < max(t_expanded.x, 0.0)) {
		discard; // O(1) exit — no ray (primary or neighbor) can hit
	}

	// ── Per-face lighting ──
	// A voxel cube has 6 faces, but from any given viewpoint only 3 are
	// visible — one per axis, the one whose outward normal points toward the
	// camera.  That visible face has normal = -sign(rd) on its axis.
	//
	// For axis i, the dot product of the visible face's normal with the light
	// direction simplifies to: dot(normal_i, light_dir) = -sign(rd[i]) * light_dir[i]
	//
	// We precompute these 3 values here (once per fragment, not per trace call)
	// and select the appropriate one based on last_axis after a hit.
	float face_light_0 = max(-step_dir.x * light_dir.x, 0.0); // X-axis face
	float face_light_1 = max(-step_dir.y * light_dir.y, 0.0); // Y-axis face
	float face_light_2 = max(-step_dir.z * light_dir.z, 0.0); // Z-axis face
	float ambient = 0.3; // Minimum brightness so unlit faces aren't pure black

	// ── Outline helper vectors ──
	// Offset to shift a ray by one virtual pixel in each cardinal direction.
	// Used for outline detection: if the primary ray misses but a neighbor hits,
	// this fragment is on the silhouette edge and gets a black outline pixel.
	vec3 right_offset = camera_right_local * delta_px;
	vec3 up_offset    = camera_up_local * delta_px;

	if (!ray_can_hit) {
		// ── Primary ray misses the exact AABB, but is within the expanded AABB ──
		// This fragment might be an outline pixel.  Test the 4 cardinal neighbor
		// rays (left, right, up, down — no diagonals, matching classic 2D sprite
		// outline convention).  Stop at the first hit for early exit.

		bool outline = false;
		int neighbor_axis; // unused, but required by the function signature

		vec3 neighbor_ro = ro_voxel - right_offset; // Left
		if (trace_ray_hierarchical(neighbor_ro, rd, anchor_point, model_size, step_dir, inv_rd, neighbor_axis).x > 0.5) { outline = true; }

		if (!outline) {
			neighbor_ro = ro_voxel + right_offset; // Right
			if (trace_ray_hierarchical(neighbor_ro, rd, anchor_point, model_size, step_dir, inv_rd, neighbor_axis).x > 0.5) { outline = true; }
		}
		if (!outline) {
			neighbor_ro = ro_voxel + up_offset; // Up
			if (trace_ray_hierarchical(neighbor_ro, rd, anchor_point, model_size, step_dir, inv_rd, neighbor_axis).x > 0.5) { outline = true; }
		}
		if (!outline) {
			neighbor_ro = ro_voxel - up_offset; // Down
			if (trace_ray_hierarchical(neighbor_ro, rd, anchor_point, model_size, step_dir, inv_rd, neighbor_axis).x > 0.5) { outline = true; }
		}

		if (outline) {
			ALBEDO = OUTLINE_COLOR;
		} else {
			discard;
		}
	} else {
		// ── Primary ray can hit — trace it ──
		int last_axis;
		vec4 hit = trace_ray_hierarchical(ro_voxel, rd, anchor_point, model_size, step_dir, inv_rd, last_axis);

		if (hit.x > 0.5) {
			// ── Solid voxel hit ──
			// Look up the palette color for this material index, then apply
			// directional lighting based on which cube face was hit.
			uint mat = uint(hit.z);

			// Select the precomputed face lighting for the hit axis
			float diff;
			if (last_axis == 0) diff = face_light_0;
			else if (last_axis == 1) diff = face_light_1;
			else diff = face_light_2;

			vec3 base_color = texelFetch(palette_texture, ivec2(int(mat), 0), 0).rgb;
			ALBEDO = base_color * (diff + ambient);
		} else {
			// ── Primary ray missed — check for outline ──
			// Same 4-neighbor test as the !ray_can_hit path above.
			bool outline = false;
			int neighbor_axis;

			vec3 neighbor_ro = ro_voxel - right_offset; // Left
			if (trace_ray_hierarchical(neighbor_ro, rd, anchor_point, model_size, step_dir, inv_rd, neighbor_axis).x > 0.5) { outline = true; }

			if (!outline) {
				neighbor_ro = ro_voxel + right_offset; // Right
				if (trace_ray_hierarchical(neighbor_ro, rd, anchor_point, model_size, step_dir, inv_rd, neighbor_axis).x > 0.5) { outline = true; }
			}
			if (!outline) {
				neighbor_ro = ro_voxel + up_offset; // Up
				if (trace_ray_hierarchical(neighbor_ro, rd, anchor_point, model_size, step_dir, inv_rd, neighbor_axis).x > 0.5) { outline = true; }
			}
			if (!outline) {
				neighbor_ro = ro_voxel - up_offset; // Down
				if (trace_ray_hierarchical(neighbor_ro, rd, anchor_point, model_size, step_dir, inv_rd, neighbor_axis).x > 0.5) { outline = true; }
			}

			if (outline) {
				ALBEDO = OUTLINE_COLOR;
			} else {
				discard;
			}
		}
	}
}
