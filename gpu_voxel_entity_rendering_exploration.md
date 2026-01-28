# GPU Voxel Entity Rendering: Problem Space Exploration

This document explores the problem of rendering a large number of small voxel models — game entities like characters, enemies, tanks, hazards, and collectibles — as volumetric ortho-sprites in a 3D Godot 4 scene with a fully unlocked camera. The goal is pixel-identical output to the existing `BenVoxelGpu` demo, but scaled to hundreds or thousands of on-screen entities.

Animation is achieved by swapping entire models per animation frame, leveraging the existing ability to pack multiple models into a single GPU texture.

**Important constraint:** Each sprite's ray direction is computed as the direction from the viewer's position to the sprite's model center (not the camera's forward direction). This gives each sprite physical presence in the world but prevents batching multiple sprites into single draw calls, since each sprite requires unique per-instance shader uniforms. See Section 5.1 for details.

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

### 5.1 The Per-Instance Ray Direction Constraint

A critical design decision shapes the entire rendering architecture: **each sprite's ray direction is computed from the viewer's position to that sprite's model center**, not from the camera's forward direction.

This means:
- A sprite on the left side of the screen has rays pointing slightly leftward from the viewer
- A sprite on the right has rays pointing slightly rightward
- Each sprite appears as if the viewer is looking directly at it from their eye position

**Why this matters for physical presence:** If all sprites used the camera's forward direction, they would all show the same "face" regardless of where they are on screen. A character to your left would appear exactly the same as one directly ahead. By using viewer-to-model-center direction, each sprite maintains the illusion of being a real 3D object in the world.

**Why this prevents batching:** The ray direction (`ray_dir_local`) and sprite orientation (`sprite_up_local`) are shader uniforms that must be unique per sprite. Standard GPU instancing (e.g., `MultiMeshInstance3D`) cannot efficiently provide different uniform values to different instances within a single draw call. Each sprite requires its own draw call with its own uniforms.

### Per-Entity State

Each on-screen entity needs:

| Data | Per-Entity? | Changes Per-Frame? |
|------|-------------|-------------------|
| World position | Yes | Yes (movement) |
| Model slot / animation frame | Yes | Yes (animation) |
| Entity type (palette, size class) | Yes | Rarely |
| Ray direction (viewer → model center) | **Yes** | **Yes** |
| Sprite up (from entity transform) | **Yes** | **Yes** |
| Light direction (position-based default) | **Yes** | **Yes** |
| Camera distance | Yes | Yes |
| Voxel size, sigma | Global | Rarely |

The ray direction, sprite up, and light direction are all per-entity because they depend on each entity's unique position and orientation relative to the viewer. This is the fundamental reason batching is not possible.

### Why Instancing Cannot Help

Traditional instancing strategies fail for volumetric ortho-sprites:

**`MultiMeshInstance3D` limitation:**
- `MultiMesh` provides per-instance transform and `custom_data` (4 floats)
- The shader uniforms `ray_dir_local` (vec3), `sprite_up_local` (vec3), and `light_dir` (vec3) require 9 floats minimum
- Even if we could pack this data, the fragment shader needs these as proper uniforms for the ray tracing math, not as varying interpolated values

**Uniform buffer instancing:**
- Could theoretically pack per-instance data into a buffer and index by instance ID
- But the volumetric shader's fragment stage has no access to instance ID — only the vertex stage does
- Passing ray direction as a varying would require per-vertex ray direction, defeating the purpose

**Conclusion:** Each sprite requires an individual draw call. Scalability must come from other optimizations.

### Scalability Without Batching

Since batching is not possible, scalability must come from:

1. **Efficient per-entity update:** Minimize CPU cost of computing per-entity uniforms (ray direction, sprite orientation, quad sizing). Current implementation is already lightweight.

2. **Frustum culling:** Skip entities outside the camera frustum entirely. Godot handles this automatically for `MeshInstance3D` nodes.

3. **LOD / Distance culling:** Very distant entities can be skipped or rendered as simple billboards.

4. **Shader efficiency:** The DDA traversal approach (Section 6) reduces per-fragment cost, allowing more fragments per frame even with many draw calls.

5. **Draw call overhead reduction:** Use Godot's scene tree efficiently. Consider object pooling to avoid node creation/destruction overhead.

### Billboard Orientation

Each entity's quad is oriented per-frame to face the viewer's position (not the camera's forward direction). The quad's basis is:

- **X axis:** `spriteRight * quadWidth` — perpendicular to view direction and entity up
- **Y axis:** `spriteUp * quadHeight` — derived from entity's transform up
- **Z axis:** `-viewDir` — pointing from model center toward viewer

The tight quad sizing (projecting the AABB onto sprite right/up) is computed per entity, per frame.

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

**Batching is not possible** (see Section 5.1). Each entity requires its own draw call because the ray direction uniform is unique per entity (computed from viewer position to model center).

- 500 tanks = **500 draw calls per frame**
- This is the primary scalability constraint

Modern GPUs and drivers can handle hundreds of draw calls, but this will be the limiting factor rather than fragment processing. Mitigation strategies:

- **Frustum culling:** Godot automatically culls `MeshInstance3D` nodes outside the frustum
- **Distance culling:** Skip very distant entities entirely
- **LOD:** Distant entities could use simpler rendering (static billboards, lower sigma)
- **Object pooling:** Reuse nodes rather than creating/destroying them

### CPU Cost

- Per entity: ray direction computation (1 normalize) + sprite orientation (2 cross products) + AABB projection (6 abs + 6 mul + 4 add) + transform update
- 500 entities × ~50 operations = ~25,000 floating-point ops — negligible

### Verdict

**Feasible on current GPUs**, but draw call count is the bottleneck. The dense 3D texture approach keeps per-fragment cost low. VRAM usage is modest. The per-instance ray direction requirement means batching is not possible, so scalability depends on efficient scene management and culling.

Practical entity counts:
- **100–200 entities:** Should run smoothly on most hardware
- **200–500 entities:** Feasible with good culling; may stress lower-end GPUs
- **500+ entities:** Requires aggressive culling and LOD strategies

The main engineering effort shifts from instancing (which is not applicable) to efficient culling and LOD systems.

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

6. **New demo scene** — Multiple entities with different models, positions, and animation states. Individual `VolumetricOrthoSprite` nodes. Verify correctness and establish baseline performance.
7. **Animation system** — Swap model slot per entity per frame. Validate smooth animation playback.

### Phase 4: Scalability (Culling and LOD)

**Note:** Instanced rendering (e.g., `MultiMeshInstance3D`) is **not applicable** because each entity requires unique shader uniforms for ray direction and sprite orientation. Scalability must come from other strategies.

8. **Frustum culling verification** — Confirm Godot automatically culls off-screen `MeshInstance3D` nodes. If not, implement manual frustum culling.
9. **Distance culling** — Skip entities beyond a configurable distance threshold.
10. **LOD system** — Distant entities use lower sigma (larger virtual pixels), reducing fragment cost. Very distant entities could fall back to pre-rendered static billboards.
11. **Object pooling** — Reuse `VolumetricOrthoSprite` nodes rather than creating/destroying them during gameplay.

### Phase 5: Performance Testing

12. **Tank army demo** — Spawn 100, 200, 500 entities. Measure frame time, identify bottlenecks.
13. **Profile draw call overhead** — Determine the practical limit on entity count for target hardware.
14. **Tune culling aggressiveness** — Balance visual quality against performance.

### Phase 6: Optimization (If Needed)

15. **Occupancy bitmask acceleration** — A coarse 3D bitmask (1 bit per 4³ region) for O(1) empty-region rejection before DDA stepping. Small memory cost, potentially large speedup for sparse models.
16. **Shader micro-optimizations** — Profile fragment shader, optimize hot paths.
17. **Alternative rendering for distant entities** — Pre-rendered sprite sheets for entities below a certain screen size.

---

## 9. Open Questions

1. **Godot `Texture3D` support quality** — Does Godot 4's `Texture3D` support `texelFetch` with integer coordinates in shaders? Is `Image.Format.R8` supported for 3D textures? Need to verify.

2. **Maximum `Texture3D` size** — What is the maximum 3D texture dimension in Godot 4 / the target GPU? A 512³ atlas is 128 MB at R8 — large but feasible. Need to verify driver limits.

3. **Draw order and transparency** — Discarded fragments on the quad create an implicit alpha mask. Does Godot handle depth sorting correctly for many `MeshInstance3D` nodes with `discard`-heavy shaders? May need `depth_draw_always` or alpha-scissor hints.

4. **Animation frame rate** — At what cadence do animation frames advance? Per game tick? Per render frame? Does the CPU compute the current frame index?

5. **Practical draw call limit** — What is the practical limit on draw calls per frame in Godot 4 on target hardware? This is the primary scalability constraint given that batching is not possible.

6. **LOD transition smoothness** — When switching between LOD levels (different sigma values or static billboards), how can visual popping be minimized? Crossfade? Distance hysteresis?

7. **Object pool sizing** — For games with dynamic entity counts (spawning/despawning), what's the optimal pool size strategy? Pre-allocate maximum? Grow on demand?
