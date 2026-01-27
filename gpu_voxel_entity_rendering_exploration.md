# GPU Voxel Entity Rendering: Problem Space Exploration

This document explores the problem of rendering a large number of small voxel models — game entities like characters, enemies, tanks, hazards, and collectibles — as volumetric ortho-sprites in a 3D Godot 4 scene with a fully unlocked camera. The goal is pixel-identical output to the existing `BenVoxelGpu` demo, but scaled to hundreds or thousands of on-screen entities.

Animation is achieved by swapping entire models per animation frame, leveraging the existing ability to pack multiple models into a single GPU texture.

---

## 1. The Scalability Problem

The existing `BenVoxelGpu` demo renders a handful of models using a GPU Sparse Voxel Octree (SVO) traversed per-fragment in a shader. This works well for a small number of entities, but scaling to an army of tanks reveals a fundamental bottleneck: **dependent texture reads**.

### Why SVO Traversal Is Expensive for Small Models

The SVO stores voxel data as a tree. To determine whether a ray hits a solid voxel, the shader must descend the tree from root to leaf. Each level requires:

1. **Read** the current node from the SVO texture (`texelFetch`).
2. **Decode** the node to determine the child index.
3. **Read** the child node — but the address depends on step 2.

This creates a chain of **dependent texture reads**: the GPU cannot issue the next read until the previous one completes, because it doesn't know the address yet. Modern GPUs hide memory latency by running thousands of threads simultaneously, but dependent reads defeat this mechanism — every thread stalls at the same point in the pipeline.

For a model with `max_depth = 5`, a single ray hit requires ~5 dependent reads just for the descent, plus additional reads for empty-space skipping. With outline detection requiring up to 4 additional rays per fragment, the worst case is ~25 dependent read chains per fragment.

### Where SVO Shines (And Where It Doesn't)

SVO traversal excels at **large, sparse volumes** — terrain, buildings, world-scale geometry — where empty-space skipping at coarse tree levels saves orders of magnitude of work. A single high-level node can skip thousands of empty voxels.

For **small, dense game entities** (16³ to 64³ voxels), the calculus reverses:

- The tree is shallow, so empty-space skipping saves few steps.
- The dependent-read overhead per step is high relative to the total work.
- The branchy decode logic (occupancy masks, node type flags, popcount) causes GPU wavefront divergence.

---

## 2. Alternative: Dense 3D Texture with DDA Ray Marching

A dense 3D texture stores one byte per voxel (the material index). Ray traversal uses a **Digital Differential Analyzer (DDA)** — the same algorithm used in Wolfenstein 3D — to step through the grid one voxel at a time.

### Why DDA Is Faster for Small Models

| Property | SVO | Dense 3D + DDA |
|----------|-----|----------------|
| **Reads per step** | ~5 dependent reads (tree descent) | 1 independent read (`texelFetch`) |
| **Next-step address** | Depends on data (dependent) | Computed arithmetically (independent) |
| **GPU latency hiding** | Poor (stalls on dependent reads) | Good (next address known immediately) |
| **Branch divergence** | Heavy (node types, occupancy masks) | Minimal (hit/miss check) |
| **Empty-space skipping** | Yes (coarse level) | No (but steps are cheap) |
| **Memory** | Compact (sparse) | Full grid (dense) |
| **Cache coherence** | Poor (pointer-chasing) | Good (spatial locality in 3D texture) |

For a 32³ model, the worst-case ray travels ~55 voxels (the space diagonal). Most rays hit much sooner — a typical front-facing ray hits within 1–5 steps. Each step is a single `texelFetch` with an arithmetically computed address, so the GPU can prefetch and pipeline efficiently.

### Memory Cost

A dense 3D texture at 1 byte per voxel:

| Model Size | Memory per Frame | 60 Frames of Animation |
|------------|-----------------|----------------------|
| 16³ | 4 KB | 240 KB |
| 32³ | 32 KB | 1.9 MB |
| 64³ | 256 KB | 15.4 MB |

For game entities, 32³ is a practical sweet spot. An army of 100 unique entity types × 60 animation frames × 32 KB = ~192 MB of VRAM, which fits comfortably on modern GPUs.

---

## 3. Input Pipeline: IBrickModel to GPU

The existing pipeline is:

```
IBrickModel instances
    → GpuSvoModel (CPU: builds octree)
    → GpuSvoModelTexture (CPU: packs into 2D RGBA8 texture)
    → GpuSvoModelTextureBridge (GPU: binds texture + uniforms)
    → VolumetricOrthoSprite (GPU: renders via shader)
```

A dense 3D texture pipeline would be:

```
IBrickModel instances
    → Dense3DVoxelGrid (CPU: fills flat byte array from bricks)
    → Texture packing (CPU: packs multiple models into GPU-resident storage)
    → Bridge class (GPU: binds textures + uniforms)
    → VolumetricOrthoSprite (GPU: renders via DDA shader)
```

The `IBrickModel` interface enumerates `VoxelBrick` structs, each a 2×2×2 block with a `ulong` payload. Converting to a dense grid is straightforward: iterate bricks, write each voxel's material byte into a flat `byte[]` array.

---

## 4. Decisions to Make: Data Structure and Texture Layout

### Decision 1: 3D Texture Type

**Option A: Native `Texture3D`**
- Godot 4 supports `Image.CreateFromData()` for 3D images and `ImageTexture3D`.
- Natural `texelFetch(sampler3D, ivec3(x,y,z), 0)` in the shader.
- GPU hardware performs 3D spatial caching optimized for this access pattern.
- Packing multiple models requires either separate textures or a 3D atlas (subdividing a large 3D texture into slots).

**Option B: Flattened into a `Texture2D`**
- Encode 3D coordinates as a linear index into a 2D texture (same approach as the current SVO texture).
- `texelFetch(sampler2D, ivec2(idx % width, idx / width), 0)`.
- Loses 3D cache locality — adjacent voxels in Y/Z are not adjacent in texture memory.
- Simpler multi-model packing (concatenate linear data, use offset uniforms).

**Option C: `Texture2DArray`**
- Store each Z-slice as a layer. `texelFetch(sampler2DArray, ivec3(x, y, z), 0)`.
- Good cache behavior for rays mostly aligned with X/Y plane.
- Each model needs a separate array (or a range of layers within a shared array).

**Recommendation:** Start with **Option A** (`Texture3D`) for best cache performance. If Godot's `Texture3D` support proves limiting for multi-model packing, fall back to Option B with the existing linear-texture approach.

### Decision 2: Multi-Model Packing Strategy

All animation frames for all entity types must be GPU-resident simultaneously (no per-frame uploads). Options:

**Option A: One large 3D texture atlas**
- A single 3D texture subdivided into fixed-size slots.
- Example: 512×512×512 texture with 32³ slots = 4096 model slots.
- Uniform tells the shader which slot to read from.
- Pro: Single texture bind for all entities.
- Con: All models must share the same bounding box size (or waste space).

**Option B: One 3D texture per entity type**
- Each entity type (with all its animation frames) gets its own texture.
- Animation frames stacked along one axis (e.g., frame N starts at Z offset `N * modelDepth`).
- Pro: No wasted space for different-sized models.
- Con: Texture switches between entity types (mitigated by batching).

**Option C: Flattened 2D texture (SVO-style packing)**
- Pack dense voxel data linearly into a 2D RGBA8 texture, same as the current `GpuSvoModelTexture`.
- Each model has an offset and dimensions.
- Pro: Proven packing approach, easy multi-model management.
- Con: Loses 3D cache locality.

**Recommendation:** Start with **Option A** (3D atlas with uniform slot size) for simplicity and single-bind efficiency. The fixed slot size is acceptable because game entities are typically similar in scale.

### Decision 3: Palette Storage

The current system stores a 256-color palette per model as a 256×1 `Texture2D`. Options:

- **Shared global palette:** All entities use one 256-color palette. Simplest. Limits artistic variety.
- **Per-entity-type palette:** One palette per entity type. Current approach. Small overhead.
- **Palette atlas:** Pack all palettes into one texture (256×N), index by entity type.

**Recommendation:** Palette atlas. One texture bind, entity type index as a uniform. Minimal memory cost.

### Decision 4: Model Size Uniformity

If all entity models use the same bounding box dimensions (e.g., 32×32×48 for humanoid characters), the shader can use compile-time constants or a single set of uniforms. If sizes vary:

- Fixed maximum size with wasted voxels for smaller models (simple, some memory waste).
- Per-entity-type size uniforms (flexible, slightly more complex).
- Multiple shaders or shader variants for each size class.

**Recommendation:** Define 2–3 standard size classes (e.g., small 16³, medium 32³, large 64³). Each class gets one shader variant with baked constants.

---

## 5. Rendering Architecture for Many Entities

### Per-Entity State

Each on-screen entity needs:

| Data | Per-Entity? | Changes Per-Frame? |
|------|-------------|-------------------|
| World position | Yes | Yes (movement) |
| Model slot / animation frame | Yes | Yes (animation) |
| Entity type (palette, size class) | Yes | Rarely |
| Camera vectors (voxel space) | Global | Yes |
| Voxel size, sigma | Global | Rarely |

### Instancing vs. Individual Draw Calls

The current system uses one `MeshInstance3D` per entity — one draw call each. This is the primary scalability concern.

**Option A: Godot `MultiMeshInstance3D`**
- One draw call for all entities sharing the same material.
- Each instance gets a per-instance transform (position + billboard orientation).
- The shader receives instance-specific data via custom instance uniforms or encoded in the transform.
- Challenge: per-instance model slot and animation frame must be communicated to the shader. Godot's `MultiMesh` supports per-instance `custom_data` (a `Color` = 4 floats), which can encode model slot index and palette index.
- **This is likely the critical architectural decision.** Without instancing, draw call overhead will cap entity count at low hundreds.

**Option B: Individual draw calls with batching**
- Keep `MeshInstance3D` per entity but minimize state changes.
- Group entities by type, bind type-specific texture once, draw all entities of that type.
- Simpler but scales worse than instancing.

**Recommendation:** Target **MultiMeshInstance3D** for production. Prototype with individual draw calls first (like the current demo) to validate visual correctness, then migrate to instanced rendering.

### Billboard Orientation with Instancing

The current system sets each quad's `GlobalTransform` per-frame from C# to orient it toward the camera. With `MultiMesh`, the per-instance transform is set via `MultiMesh.SetInstanceTransform()`, which can encode the billboard orientation and quad size. This is the same data — just delivered through the instancing API instead of per-node transforms.

However, the tight quad sizing (projecting the AABB onto camera right/up) depends on the model dimensions, which vary per entity type. Options:

- Compute tight quad size per entity type on CPU (group by type, batch update).
- Use a conservative quad size per size class (simpler, slightly more fragments).

---

## 6. Shader Changes for Dense 3D Textures

The fragment shader's structure remains largely identical. Only the traversal inner loop changes:

### Current (SVO)

```glsl
// Hierarchical tree descent with dependent texture reads
for each level:
    node = texelFetch(svo_texture, nodeAddress)  // DEPENDENT READ
    decode node type, occupancy mask
    compute child address from node data          // DATA-DEPENDENT
    descend to child
```

### Proposed (DDA)

```glsl
// Standard DDA grid traversal
vec3 pos = ray_entry_point;
ivec3 voxel = ivec3(floor(pos));
vec3 tDelta = abs(1.0 / rd);             // step size in t per axis
vec3 tMax = initial_t_to_next_boundary;   // t to next voxel boundary per axis

for (int i = 0; i < max_steps; i++) {
    uint mat = texelFetch(voxel_texture, voxel_to_texcoord(voxel), 0).r;
    if (mat > 0u) return hit;             // 1 independent read per step

    // Advance to next voxel (branch-light: just compare 3 floats)
    if (tMax.x < tMax.y) {
        if (tMax.x < tMax.z) { voxel.x += step.x; tMax.x += tDelta.x; last_axis = 0; }
        else                 { voxel.z += step.z; tMax.z += tDelta.z; last_axis = 2; }
    } else {
        if (tMax.y < tMax.z) { voxel.y += step.y; tMax.y += tDelta.y; last_axis = 1; }
        else                 { voxel.z += step.z; tMax.z += tDelta.z; last_axis = 2; }
    }
}
```

Everything else — virtual pixel grid, outline logic, lighting, depth handling — is unchanged.

---

## 7. The Tank Army: Feasibility Estimate

**Target scene:** ~500 tanks visible simultaneously, each a 32×32×16 voxel model with 30 animation frames (tracks rolling, turret rotating).

### Memory

- 32×32×16 = 16,384 bytes per frame
- 30 frames × 16 KB = ~480 KB per tank type
- 10 unique tank types × 480 KB = ~4.8 MB total voxel data
- Palette: 10 types × 1 KB = negligible
- **Total VRAM for voxel data: ~5 MB** — trivial.

### Fragment Cost

- 500 tanks × ~50×30 pixel quad average (distant tanks are small) = ~750,000 fragments
- Per fragment: ~10 DDA steps average × 1 texelFetch = ~10 reads
- Outline fragments (silhouette only): ~20% of total, up to 4× extra rays = ~40 extra reads
- **Total texture reads: ~10–15 million per frame** — well within GPU capability.

### Draw Calls

- With `MultiMeshInstance3D`: **1 draw call per size class** (potentially just 1 total).
- Without instancing: 500 draw calls — likely the bottleneck.

### CPU Cost

- Per entity: AABB projection (6 abs + 6 mul + 4 add) + transform update.
- 500 entities × ~20 operations = ~10,000 floating-point ops — negligible.

### Verdict

**Feasible on current GPUs**, provided instanced rendering is used to avoid draw call bottleneck. The dense 3D texture approach keeps fragment shader cost low. VRAM usage is modest. The main engineering effort is the instancing integration with Godot's `MultiMesh`.

---

## 8. Implementation Roadmap

### Phase 1: Dense 3D Voxel Grid (C# Library)

Build the new data structure alongside the existing SVO, not replacing it.

1. **`Dense3DVoxelGrid`** — Takes an `IBrickModel`, produces a flat `byte[]` array in Z-up voxel order. One byte per voxel (material index).
2. **`Dense3DVoxelTexture`** — Packs multiple `Dense3DVoxelGrid` instances into a GPU-ready format. Decide on 3D atlas vs. flattened 2D at this stage.
3. **Texture bridge class** — Analogous to `GpuSvoModelTextureBridge`. Binds the dense texture and per-model metadata (slot offset, dimensions).

### Phase 2: DDA Shader

4. **New shader** — Fork `volumetric_ortho_sprite.gdshader`. Replace `trace_ray_hierarchical` with a DDA traversal function. Keep all surrounding logic (grid snapping, outline, lighting).
5. **Validate visual correctness** — Render the same models with both SVO and DDA shaders side-by-side. Verify pixel-identical output (same virtual pixel grid, same outline, same lighting).

### Phase 3: Entity Demo

6. **New demo scene** — Multiple entities with different models, positions, and animation states. Individual `VolumetricOrthoSprite` nodes (non-instanced). Verify correctness and establish baseline performance.
7. **Animation system** — Swap model slot per entity per frame. Validate smooth animation playback.

### Phase 4: Instanced Rendering

8. **`MultiMesh` integration** — Replace individual `MeshInstance3D` nodes with a `MultiMeshInstance3D`. Encode per-instance model slot and entity type in `custom_data`. Update transforms in bulk each frame.
9. **Shader modifications for instancing** — Read per-instance custom data to select model slot and palette. Billboard orientation may be computed in the vertex shader instead of on the CPU.
10. **Tank army demo** — Spawn 500+ entities, measure performance.

### Phase 5: Optimization (If Needed)

11. **Frustum culling** — Skip entities outside the camera frustum (Godot may handle this automatically for `MultiMesh` instances).
12. **LOD** — Distant entities could use lower sigma (larger virtual pixels), reducing ray count.
13. **Occupancy bitmask acceleration** — A coarse 3D bitmask (1 bit per 4³ region) for O(1) empty-region rejection before DDA stepping. Small memory cost, potentially large speedup for sparse models.

---

## 9. Open Questions

1. **Godot `Texture3D` support quality** — Does Godot 4's `Texture3D` support `texelFetch` with integer coordinates in shaders? Is `Image.Format.R8` supported for 3D textures? Need to verify.

2. **`MultiMesh` per-instance data limits** — Godot's `MultiMesh.custom_data` provides 4 floats per instance. Is this enough to encode model slot + palette index + animation frame? (Likely yes: 3 integers packed into 3 floats.)

3. **Vertex shader billboard** — With `MultiMesh`, can the vertex shader compute billboard orientation from the camera, or must transforms be set from the CPU? CPU-side is simpler and proven; vertex-shader-side would reduce CPU work.

4. **Maximum `Texture3D` size** — What is the maximum 3D texture dimension in Godot 4 / the target GPU? A 512³ atlas is 128 MB at R8 — large but feasible. Need to verify driver limits.

5. **Draw order and transparency** — Discarded fragments on the quad create an implicit alpha mask. Does Godot handle depth sorting correctly for `MultiMesh` instances with `discard`-heavy shaders? May need `depth_draw_always` or alpha-scissor hints.

6. **Animation frame rate** — At what cadence do animation frames advance? Per game tick? Per render frame? Does the CPU or a shader compute the current frame index?
