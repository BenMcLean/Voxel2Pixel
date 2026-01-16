#[compute]
#version 450

// Workgroup size: 4×4×4 = 64 threads per workgroup
// Each thread processes a 16×16×16 region of bricks (4096 bricks per thread)
// Total: 64 threads × 4096 bricks = 262,144 bricks = one full segment
layout(local_size_x = 4, local_size_y = 4, local_size_z = 4) in;

// Segment brick texture (64³ bricks, RGBAF format)
// R = low 32 bits of brick, G = high 32 bits (as float-reinterpreted uint)
layout(set = 0, binding = 0) uniform sampler3D segment_bricks;

// Output buffer: 6 uints per segment (minX, maxX, minY, maxY, minZ, maxZ)
// We use atomics to combine results from all threads
layout(set = 0, binding = 1, std430) buffer BoundsBuffer {
    uint bounds[]; // [segmentIndex * 6 + 0..5]
};

// Push constants for segment info
layout(push_constant) uniform PushConstants {
    uint segment_index;      // Which segment we're processing
    uint segment_origin_x;   // Segment origin in world coordinates (segment coord × 128)
    uint segment_origin_y;
    uint segment_origin_z;
};

// Shared memory for workgroup reduction
shared uint local_min_x, local_max_x;
shared uint local_min_y, local_max_y;
shared uint local_min_z, local_max_z;
shared bool local_has_voxels;

void main() {
    uint local_id = gl_LocalInvocationIndex;

    // Initialize shared memory (first thread only)
    if (local_id == 0) {
        local_min_x = 0xFFFFFFFFu;
        local_max_x = 0u;
        local_min_y = 0xFFFFFFFFu;
        local_max_y = 0u;
        local_min_z = 0xFFFFFFFFu;
        local_max_z = 0u;
        local_has_voxels = false;
    }
    barrier();

    // Each thread processes a 16×16×16 region of bricks
    // Thread (tx, ty, tz) processes bricks from (tx*16, ty*16, tz*16) to (tx*16+15, ty*16+15, tz*16+15)
    uvec3 thread_id = gl_LocalInvocationID;
    uvec3 brick_start = thread_id * 16u;

    // Track this thread's local min/max
    uint thread_min_x = 0xFFFFFFFFu, thread_max_x = 0u;
    uint thread_min_y = 0xFFFFFFFFu, thread_max_y = 0u;
    uint thread_min_z = 0xFFFFFFFFu, thread_max_z = 0u;
    bool thread_has_voxels = false;

    // Iterate over this thread's brick region
    for (uint bz = 0u; bz < 16u; bz++) {
        for (uint by = 0u; by < 16u; by++) {
            for (uint bx = 0u; bx < 16u; bx++) {
                uvec3 brick_coord = brick_start + uvec3(bx, by, bz);

                // Sample the brick texture
                // Texture coordinates are normalized [0,1], so divide by 64
                vec3 tex_coord = (vec3(brick_coord) + 0.5) / 64.0;
                vec4 brick_data = texture(segment_bricks, tex_coord);

                // Reinterpret float bits as uint to get brick payload
                uint low = floatBitsToUint(brick_data.r);
                uint high = floatBitsToUint(brick_data.g);

                // Check if brick has any voxels (any non-zero byte)
                if (low != 0u || high != 0u) {
                    thread_has_voxels = true;

                    // Calculate world coordinates for this brick
                    // Brick coordinates are in segment space (0-63)
                    // Each brick covers 2 voxels, so multiply by 2
                    // Then add segment origin
                    uint world_x = segment_origin_x + brick_coord.x * 2u;
                    uint world_y = segment_origin_y + brick_coord.y * 2u;
                    uint world_z = segment_origin_z + brick_coord.z * 2u;

                    // Update thread-local bounds
                    thread_min_x = min(thread_min_x, world_x);
                    thread_max_x = max(thread_max_x, world_x);
                    thread_min_y = min(thread_min_y, world_y);
                    thread_max_y = max(thread_max_y, world_y);
                    thread_min_z = min(thread_min_z, world_z);
                    thread_max_z = max(thread_max_z, world_z);
                }
            }
        }
    }

    // Reduce to shared memory using atomics
    if (thread_has_voxels) {
        atomicMin(local_min_x, thread_min_x);
        atomicMax(local_max_x, thread_max_x);
        atomicMin(local_min_y, thread_min_y);
        atomicMax(local_max_y, thread_max_y);
        atomicMin(local_min_z, thread_min_z);
        atomicMax(local_max_z, thread_max_z);
        local_has_voxels = true;
    }

    barrier();

    // First thread writes to global buffer
    if (local_id == 0 && local_has_voxels) {
        uint base_idx = segment_index * 6u;
        atomicMin(bounds[base_idx + 0u], local_min_x);
        atomicMax(bounds[base_idx + 1u], local_max_x);
        atomicMin(bounds[base_idx + 2u], local_min_y);
        atomicMax(bounds[base_idx + 3u], local_max_y);
        atomicMin(bounds[base_idx + 4u], local_min_z);
        atomicMax(bounds[base_idx + 5u], local_max_z);
    }
}
