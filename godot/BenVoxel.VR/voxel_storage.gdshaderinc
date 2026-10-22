// Shared voxel storage access and traversal for voxel_raymarch.gdshader and voxel_ortho_sprite.gdshader.
// Both shaders read the same segment / brick textures, which Root.cs binds to both materials, so edits
// show up in both at once. Everything here works in Godot Y-up voxel space (one unit per voxel).
//
// VOXEL STORAGE (see VoxelBridge.cs):
//   The model is split into 128^3-voxel segments. Each segment is a 64^3 3D texture of "bricks"; one
//   brick is 2x2x2 voxels packed into 8 bytes (one byte of palette index per voxel). A small directory
//   texture lists which segments exist and which texture-array slot each one lives in.
//   Stored voxel data is Z-up (MagicaVoxel convention), so fetch_voxel() swaps Y and Z.

// Number of valid entries in segment_directory. Entries beyond this are zero padding.
uniform int active_segment_count = 0;

// Segment directory: a (MaxSegments x 1) RGBAF texture, one texel per loaded segment.
// Texel i = (segment X, segment Y, segment Z, texture-array slot), all stored as floats.
// Segment coordinates are in Z-up voxel storage space, in units of 128 voxels.
uniform sampler2D segment_directory;

// One 64^3 RGBAF 3D texture per loaded segment, indexed by the slot stored in the directory.
// Each texel is one 2x2x2-voxel brick: the R channel holds the low 32 bits and the G channel the
// high 32 bits of the 8-byte brick payload, bit-cast into floats (read back with floatBitsToUint).
//
// LIMIT OF THIS IMPLEMENTATION, NOT OF THE FORMAT: 64 is the most segments that can be resident at once,
// and it must match VoxelBridge.MaxSegments (which refuses to upload more). The format itself addresses
// 512 segments per axis. Supporting big scenes means replacing this array with a paged atlas texture;
// see the note on VoxelBridge.MaxSegments for what that would involve.
uniform sampler3D segment_bricks[64];

// 256x1 RGBA8 palette LUT: texel n is palette entry n. Entry 0 is "empty" and is never looked up.
// filter_nearest + texelFetch give exact integer lookups with no blending between entries.
// source_color tells Godot the data is sRGB so it is converted to linear on read; for an unshaded
// material Godot converts back on output, so the colours match the authored palette.
uniform sampler2D palette : filter_nearest, repeat_disable, source_color;

// Bounding box of the model in voxel space (Godot Y-up), set from Root.cs together with the BoxMesh size.
// The box is axis-aligned in voxel space and its corners are integer voxel coordinates.
// The BoxMesh is centred on its own origin, so voxel position = mesh-local position + box_center.
uniform vec3 box_center = vec3(0.0);
uniform vec3 box_size = vec3(1.0);

// =============================================================================
// PALETTE
// =============================================================================

// Returns the RGB colour for a palette index. Index 0 means "empty" and returns black; callers must
// not treat a result for 0 as a real colour. Alpha in the palette is ignored.
vec3 get_material_color(uint material_id) {
    if (material_id == 0u) return vec3(0.0);
    return texelFetch(palette, ivec2(int(material_id), 0), 0).rgb;
}

// =============================================================================
// VOXEL FETCH
// =============================================================================

// Returns the directory index of the segment containing a Z-up storage coordinate, or -1 if that
// segment is not loaded (which the caller treats as empty space).
//
// A segment spans 128 voxels per axis, so the segment coordinate is simply the voxel coordinate
// shifted right by 7. The directory is searched linearly; this is cheap for a handful of segments
// but is the main per-step cost, so a cache of the last segment would be the first optimisation.
//
// world_pos: voxel coordinate in Z-up storage space (already swizzled by fetch_voxel).
int find_segment(ivec3 world_pos) {
    ivec3 seg_coord = world_pos >> 7; // Segment coordinate = voxel coordinate / 128

    for (int i = 0; i < active_segment_count; i++) {
        vec4 entry = texelFetch(segment_directory, ivec2(i, 0), 0);
        // The directory stores integers as floats; convert back for comparison
        ivec3 seg_entry = ivec3(entry.rgb);
        if (seg_entry == seg_coord) {
            return i;
        }
    }
    return -1; // Segment not loaded
}

// Reads one brick texel from the segment texture in a given slot of segment_bricks.
//
// WHY THE LOOP: tex_idx differs from pixel to pixel (neighbouring pixels can be in different segments), and
// Vulkan requires the index into an array of samplers to be the same for every invocation in a group unless it
// is marked non-uniform, which Godot's shader language cannot do. Violating that is undefined behaviour; on
// some GPUs the whole group samples the texture of one pixel's index, so a segment shows another segment's
// voxels (a smaller ghost copy of the model, flickering as it changes with the view). The loop counter is
// uniform, so each segment_bricks[i] is a legal index, and only the pixels whose slot is i read the texture.
vec4 fetch_brick(int tex_idx, ivec3 brick_coord) {
    vec4 result = vec4(0.0);
    for (int i = 0; i < active_segment_count; i++) {
        // The read is inside the branch so only the matching slot costs a texture fetch; texelFetch needs no
        // derivatives, so it is safe in divergent control flow.
        if (i == tex_idx) result = texelFetch(segment_bricks[i], brick_coord, 0);
    }
    return result;
}

// Returns the palette index (0-255) of the voxel at a voxel-space position, or 0 if the voxel is
// empty or its segment is not loaded.
//
// world_pos_yup: integer voxel coordinate in Godot Y-up voxel space (the space the DDA runs in).
//
// Lookup path:
//   1. Swap Y and Z to get the Z-up storage coordinate.
//   2. find_segment() locates the segment and its texture-array slot.
//   3. Bits 1-6 of each axis select the brick (2x2x2 voxels) within the 64^3 segment texture;
//      bit 0 of each axis selects the voxel within that brick. (Bits 7+ were used for the segment.)
//   4. The brick's two floats are bit-cast to two uints (the 64-bit payload) and the voxel's byte is
//      extracted.
uint fetch_voxel(ivec3 world_pos_yup) {
    // Convert from Godot Y-up to voxel Z-up: (x, y, z)_yup -> (x, z, y)_zup
    ivec3 voxel_pos = ivec3(world_pos_yup.x, world_pos_yup.z, world_pos_yup.y);

    int seg_idx = find_segment(voxel_pos);
    if (seg_idx < 0) return 0u; // Segment not loaded

    // Texture-array slot for this segment (directory texel's alpha channel)
    vec4 entry = texelFetch(segment_directory, ivec2(seg_idx, 0), 0);
    int tex_idx = int(entry.w);

    ivec3 brick_coord = (voxel_pos >> 1) & ivec3(0x3F); // Brick within segment: 0-63 per axis
    ivec3 voxel_offset = voxel_pos & ivec3(1);           // Voxel within brick: 0 or 1 per axis

    // Fetch the whole brick (8 bytes packed into the R and G float channels)
    vec4 brick_data = fetch_brick(tex_idx, brick_coord);

    // Reinterpret the float bits as the original uints (a value cast would corrupt them)
    uint brick_low = floatBitsToUint(brick_data.r);   // Low 32 bits  = bytes 0-3
    uint brick_high = floatBitsToUint(brick_data.g);  // High 32 bits = bytes 4-7

    // Brick byte layout: [v000, v100, v010, v110, v001, v101, v011, v111]
    // i.e. byte index = (z << 2) | (y << 1) | x, with x/y/z the voxel offsets in storage (Z-up) space
    int byte_idx = (voxel_offset.z << 2) | (voxel_offset.y << 1) | voxel_offset.x;
    return (byte_idx < 4)
        ? (brick_low >> uint(byte_idx * 8)) & 0xFFu      // Bytes 0-3 live in the low 32 bits
        : (brick_high >> uint((byte_idx - 4) * 8)) & 0xFFu; // Bytes 4-7 live in the high 32 bits
}


// =============================================================================
// DDA RAYMARCHER
// =============================================================================

// Walks the voxel grid along a ray and returns the palette index of the first non-empty voxel it enters,
// or 0 if the ray leaves the bounding box.
//
// DDA ALGORITHM:
//   For each axis, side_dist holds the ray parameter t at which the ray next crosses a grid plane on
//   that axis. Each iteration advances along the axis with the smallest side_dist (the nearest plane),
//   moves voxel_pos one voxel that way, and adds delta_dist to that axis's side_dist so it points at
//   the following crossing. The newly entered voxel is then tested.
//   delta_dist[i] = |1 / ray_dir[i]| is the t-interval between consecutive planes on axis i. An axis
//   with ray_dir[i] = 0 gets delta_dist = infinity and is therefore never stepped.
//   Ties (equal side_dist) resolve by the strict < comparisons, preferring x, then y, then z. Ties
//   only occur exactly on voxel edges and are harmless.
//
// ray_origin:   start point in voxel space. Must lie inside [box_min, box_max); march_box() guarantees this.
// ray_dir:      normalised ray direction in voxel space.
// entry_normal: unit normal of the box face the ray entered through, or zero if the origin is inside the
//               box. Becomes `normal` if the very first voxel visited is already solid.
// box_min/max:  the bounding box as integer voxel coordinates; max is exclusive.
// normal:       out; unit normal of the face of the hit voxel that the ray entered through.
// hit_pos:      out; the point on the surface of the hit voxel where the ray entered it, in voxel
//               space. Equals ray_origin on a miss.
uint raymarch(vec3 ray_origin, vec3 ray_dir, vec3 entry_normal, ivec3 box_min, ivec3 box_max, out vec3 normal, out vec3 hit_pos) {
    hit_pos = ray_origin;

    // The voxel containing the origin; each voxel spans [v, v + 1) on every axis
    ivec3 voxel_pos = ivec3(floor(ray_origin));

    // t-interval between consecutive grid planes on each axis (always positive; direction is in ray_step)
    vec3 delta_dist = abs(vec3(1.0) / ray_dir);

    // Step direction per axis: +1, -1, or 0 for an axis-parallel ray
    ivec3 ray_step = ivec3(sign(ray_dir));

    // t from ray_origin to the first plane crossing on each axis. With s = sign(ray_dir):
    //   s = +1: next plane is at v + 1,  t = (v + 1 - o) * delta
    //   s = -1: next plane is at v,      t = (o - v) * delta
    //   s =  0: 0.5 * infinity = infinity, so the axis is never stepped
    // The unified expression (s * (v - o) + s * 0.5 + 0.5) * delta evaluates to all three cases.
    vec3 side_dist = (sign(ray_dir) * (vec3(voxel_pos) - ray_origin) + (sign(ray_dir) * 0.5) + 0.5) * delta_dist;

    // Normal of the face most recently crossed, used for lighting. It starts as the box entry face.
    normal = entry_normal;

    // t of the most recent plane crossing, measured from ray_origin: on a hit, the distance to the surface of
    // the current voxel.
    float distance = 0.0;

    // A DDA ray visits at most one voxel plus one per plane crossed, and a ray inside an axis-aligned box
    // crosses at most (size.x + size.y + size.z) planes before leaving it. The bounds check below ends the
    // march at that point, so this limit is only a guard against a non-terminating loop. It must scale with
    // the box: a fixed cap truncates rays through large models, which shows as parts of them vanishing.
    ivec3 box_extent = box_max - box_min;
    int max_steps = box_extent.x + box_extent.y + box_extent.z + 3;

    for (int step = 0; step < max_steps; step++) {
        // Primary exit: the ray has left the model's bounding box
        if (any(lessThan(voxel_pos, box_min)) || any(greaterThanEqual(voxel_pos, box_max))) {
            break;
        }

        uint material = fetch_voxel(voxel_pos);

        if (material > 0u) {
            // Hit: the ray entered this voxel at t = distance, which is on its surface
            hit_pos = ray_origin + ray_dir * distance;
            return material;
        }

        // Advance to the next voxel along the axis with the nearest plane crossing. The face just
        // crossed has a normal pointing back against the step direction on that axis.
        if (side_dist.x < side_dist.y) {
            if (side_dist.x < side_dist.z) {
                side_dist.x += delta_dist.x;
                voxel_pos.x += ray_step.x;
                distance = side_dist.x - delta_dist.x; // t of the crossing just made
                normal = vec3(-float(ray_step.x), 0.0, 0.0);
            } else {
                side_dist.z += delta_dist.z;
                voxel_pos.z += ray_step.z;
                distance = side_dist.z - delta_dist.z;
                normal = vec3(0.0, 0.0, -float(ray_step.z));
            }
        } else {
            if (side_dist.y < side_dist.z) {
                side_dist.y += delta_dist.y;
                voxel_pos.y += ray_step.y;
                distance = side_dist.y - delta_dist.y;
                normal = vec3(0.0, -float(ray_step.y), 0.0);
            } else {
                side_dist.z += delta_dist.z;
                voxel_pos.z += ray_step.z;
                distance = side_dist.z - delta_dist.z;
                normal = vec3(0.0, 0.0, -float(ray_step.z));
            }
        }
    }

    // Miss: left the box
    return 0u;
}

// Casts a ray from anywhere against the model's bounding box and marches it through the voxels.
// Returns the palette index of the first voxel hit, or 0 on a miss.
//
// RAY-AABB INTERSECTION (slab method):
//   For each axis the ray crosses the two bounding planes at t0 and t1. The ray is inside the box
//   while it is inside all three slabs: from t_enter (the latest slab entry) to t_exit (the earliest
//   slab exit). It misses if t_enter >= t_exit, and the box is behind the origin if t_exit < 0.
//   Axis-parallel rays give +-infinity per-axis times, which the min/max handle correctly, except
//   that 0 * infinity = NaN when the origin lies exactly on a bounding plane; the clamp on the
//   marching origin below recovers from that.
//
// The marching origin is the entry face, or the origin itself if it is already inside (max with 0).
// The clamp keeps floor(origin) within the box (the 1e-4 keeps the upper face inside the last voxel).
// The entry face is the axis with the latest slab entry; its normal points against the ray on that
// axis. If t_enter <= 0 the origin is inside the box and there is no entry face (zero normal).
uint march_box(vec3 origin, vec3 dir, vec3 box_min, vec3 box_max, out vec3 normal, out vec3 hit_pos) {
    normal = vec3(0.0);
    hit_pos = origin;
    vec3 inv_dir = 1.0 / dir;
    vec3 t0 = (box_min - origin) * inv_dir;
    vec3 t1 = (box_max - origin) * inv_dir;
    vec3 tmin_v = min(t0, t1); // Per-axis entry time
    vec3 tmax_v = max(t0, t1); // Per-axis exit time
    float t_enter = max(max(tmin_v.x, tmin_v.y), tmin_v.z);
    float t_exit = min(min(tmax_v.x, tmax_v.y), tmax_v.z);

    if (t_enter >= t_exit || t_exit < 0.0) return 0u;

    vec3 entry_normal = vec3(0.0);
    if (t_enter > 0.0) {
        if (tmin_v.x >= tmin_v.y && tmin_v.x >= tmin_v.z) {
            entry_normal = vec3(-sign(dir.x), 0.0, 0.0);
        } else if (tmin_v.y >= tmin_v.z) {
            entry_normal = vec3(0.0, -sign(dir.y), 0.0);
        } else {
            entry_normal = vec3(0.0, 0.0, -sign(dir.z));
        }
    }

    vec3 march_origin = clamp(origin + dir * max(t_enter, 0.0), box_min, box_max - vec3(1e-4));
    return raymarch(march_origin, dir, entry_normal, ivec3(box_min), ivec3(box_max), normal, hit_pos);
}
