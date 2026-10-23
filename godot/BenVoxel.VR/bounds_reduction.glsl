#[compute]
#version 450

// Workgroup size: 4×4×4 = 64 threads per workgroup
// Each thread processes a 16×16×16 region of voxels (4096 voxels per thread), so one workgroup covers a
// 64³ block. A segment is 128³, so it is dispatched as 2×2×2 workgroups.
layout(local_size_x = 4, local_size_y = 4, local_size_z = 4) in;

// Segment voxel texture (128³ texels, R8 format): one palette-index byte per voxel. Read with texelFetch.
layout(set = 0, binding = 0) uniform sampler3D segment_voxels;

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

    // Each thread processes a 16×16×16 region of voxels, inside the 64³ block this workgroup covers
    uvec3 thread_id = gl_LocalInvocationID;
    uvec3 voxel_start = gl_WorkGroupID * 64u + thread_id * 16u;

    // Track this thread's local min/max
    uint thread_min_x = 0xFFFFFFFFu, thread_max_x = 0u;
    uint thread_min_y = 0xFFFFFFFFu, thread_max_y = 0u;
    uint thread_min_z = 0xFFFFFFFFu, thread_max_z = 0u;
    bool thread_has_voxels = false;

    for (uint vz = 0u; vz < 16u; vz++) {
        for (uint vy = 0u; vy < 16u; vy++) {
            for (uint vx = 0u; vx < 16u; vx++) {
                uvec3 voxel_coord = voxel_start + uvec3(vx, vy, vz);

                // Any non-zero palette index is a solid voxel
                if (texelFetch(segment_voxels, ivec3(voxel_coord), 0).r > 0.0) {
                    thread_has_voxels = true;

                    // Bounds are kept at brick granularity (2×2×2 voxels), so snap to the brick origin
                    // by clearing bit 0, then add the segment origin
                    uint world_x = segment_origin_x + (voxel_coord.x & ~1u);
                    uint world_y = segment_origin_y + (voxel_coord.y & ~1u);
                    uint world_z = segment_origin_z + (voxel_coord.z & ~1u);

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
