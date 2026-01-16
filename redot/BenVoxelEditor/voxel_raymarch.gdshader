shader_type spatial;
render_mode unshaded, depth_draw_always, cull_disabled;

// Segment directory and voxel data
uniform int active_segment_count = 0;
uniform sampler2D segment_directory; // RGBAF: (segX, segY, segZ, texIndex) as floats
uniform sampler3D segment_bricks[64]; // RGBAF: each texel = one brick (RG channels contain uint data as floats)

// Palette
uniform sampler2D palette; // 256x1 RGBA8 texture

// Raymarch parameters
uniform float max_distance = 200.0;
uniform int max_steps = 512;

// Get color from palette using material ID
vec3 get_material_color(uint material_id) {
    if (material_id == 0u) return vec3(0.0);

    // Look up color in palette texture
    // Material IDs are 1-255, but palette is indexed 0-255
    float u = (float(material_id) + 0.5) / 256.0; // Center of texel
    vec4 color = texture(palette, vec2(u, 0.5));

    return color.rgb;
}

// Find which segment contains this world coordinate
int find_segment(ivec3 world_pos) {
    ivec3 seg_coord = world_pos >> 7; // Bits 7-15 = segment coordinates

    // Linear search through directory texture
    for (int i = 0; i < active_segment_count; i++) {
        vec4 entry = texelFetch(segment_directory, ivec2(i, 0), 0);
        // Convert floats back to ints
        ivec3 seg_entry = ivec3(entry.rgb);
        if (seg_entry == seg_coord) {
            return i;
        }
    }
    return -1; // Segment not loaded
}

// Fetch voxel from brick texture
// world_pos is in Godot Y-up coordinates
uint fetch_voxel(ivec3 world_pos_yup) {
    // Convert from Godot Y-up to voxel Z-up: (x, y, z)_yup -> (x, z, y)_zup
    ivec3 voxel_pos = ivec3(world_pos_yup.x, world_pos_yup.z, world_pos_yup.y);

    // Find segment
    int seg_idx = find_segment(voxel_pos);
    if (seg_idx < 0) return 0u; // Segment not loaded

    // Get texture index
    vec4 entry = texelFetch(segment_directory, ivec2(seg_idx, 0), 0);
    int tex_idx = int(entry.w);

    // Extract brick coordinates (bits 1-6) and voxel offset (bit 0)
    ivec3 brick_coord = (voxel_pos >> 1) & ivec3(0x3F); // 0-63 per axis
    ivec3 voxel_offset = voxel_pos & ivec3(1);           // 0 or 1 per axis

    // Fetch entire brick (stored as RGBAF, RG channels contain uint data as floats)
    vec4 brick_data = texelFetch(segment_bricks[tex_idx], brick_coord, 0);

    // Convert float bits back to uint (reinterpret)
    uint brick_low = floatBitsToUint(brick_data.r);   // Low 32 bits
    uint brick_high = floatBitsToUint(brick_data.g);  // High 32 bits

    // Extract specific voxel byte
    // Brick byte layout: [v000, v100, v010, v110, v001, v101, v011, v111]
    int byte_idx = (voxel_offset.z << 2) | (voxel_offset.y << 1) | voxel_offset.x;
    return (byte_idx < 4)
        ? (brick_low >> uint(byte_idx * 8)) & 0xFFu      // Bytes 0-3 in low 32 bits
        : (brick_high >> uint((byte_idx - 4) * 8)) & 0xFFu; // Bytes 4-7 in high 32 bits
}

// DDA Raymarching
// Operates in Godot Y-up world space
// Coordinate conversion to voxel Z-up space happens inside fetch_voxel()
vec4 raymarch(vec3 ray_origin, vec3 ray_dir) {
    // Initialize DDA (in Godot Y-up coordinates)
    ivec3 voxel_pos = ivec3(floor(ray_origin));
    vec3 delta_dist = abs(vec3(1.0) / ray_dir);
    ivec3 ray_step = ivec3(sign(ray_dir));
    vec3 side_dist = (sign(ray_dir) * (vec3(voxel_pos) - ray_origin) + (sign(ray_dir) * 0.5) + 0.5) * delta_dist;

    vec3 normal = vec3(0.0);
    float distance = 0.0;

    // Step through voxels
    for (int step = 0; step < max_steps && distance < max_distance; step++) {
        // Bounds check (16-bit unsigned coordinates: 0-65535)
        if (any(lessThan(voxel_pos, ivec3(0))) || any(greaterThanEqual(voxel_pos, ivec3(65536)))) {
            break;
        }

        // Fetch voxel (converts Y-up to Z-up internally)
        uint material = fetch_voxel(voxel_pos);

        if (material > 0u) {
            // Hit solid voxel
            vec3 color = get_material_color(material);

            // Simple lighting based on normal
            float lighting = max(dot(normal, normalize(vec3(1.0, 1.0, 0.5))), 0.0) * 0.7 + 0.3;

            return vec4(color * lighting, 1.0);
        }

        // DDA step to next voxel
        if (side_dist.x < side_dist.y) {
            if (side_dist.x < side_dist.z) {
                side_dist.x += delta_dist.x;
                voxel_pos.x += ray_step.x;
                distance = side_dist.x;
                normal = vec3(-float(ray_step.x), 0.0, 0.0);
            } else {
                side_dist.z += delta_dist.z;
                voxel_pos.z += ray_step.z;
                distance = side_dist.z;
                normal = vec3(0.0, 0.0, -float(ray_step.z));
            }
        } else {
            if (side_dist.y < side_dist.z) {
                side_dist.y += delta_dist.y;
                voxel_pos.y += ray_step.y;
                distance = side_dist.y;
                normal = vec3(0.0, -float(ray_step.y), 0.0);
            } else {
                side_dist.z += delta_dist.z;
                voxel_pos.z += ray_step.z;
                distance = side_dist.z;
                normal = vec3(0.0, 0.0, -float(ray_step.z));
            }
        }
    }

    // Miss - return transparent (so we can see through empty space)
    return vec4(0.0, 0.0, 0.0, 0.0);
}

varying vec3 world_vertex;

void vertex() {
    // Pass world-space vertex position to fragment shader
    world_vertex = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
    // Get camera position in world space
    vec3 camera_pos = (INV_VIEW_MATRIX * vec4(0.0, 0.0, 0.0, 1.0)).xyz;

    vec3 ray_origin;
    vec3 ray_dir;

    if (FRONT_FACING) {
        // Camera is OUTSIDE the box, looking at a front face
        // Start raymarching from the box surface (entry point)
        ray_dir = normalize(world_vertex - camera_pos);
        ray_origin = world_vertex;
    } else {
        // Camera is INSIDE the box, looking at a back face
        // Start raymarching from the camera position toward the back face
        ray_dir = normalize(world_vertex - camera_pos);
        ray_origin = camera_pos;
    }

    // Raymarch through the bounding box
    vec4 color = raymarch(ray_origin, ray_dir);

    ALBEDO = color.rgb;
    ALPHA = color.a;
}
