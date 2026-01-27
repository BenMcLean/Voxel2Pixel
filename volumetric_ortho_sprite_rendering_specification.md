# Volumetric Ortho-Sprite Rendering System

This document specifies a **GPU-driven volumetric sprite rendering system** for games. The system renders 3D voxel models (stored as GPU Sparse Voxel Octrees, *GpuSvoModel*) as *sprite-like entities* inside a fully perspective 3D world, while preserving **orthographic internal projection**, a **stable virtual pixel grid**, and a **1-pixel silhouette outline defined in sprite space**.

This specification is written to be *implementation-ready*. All ambiguity around coordinate spaces, ray setup, quantization, outlining, lighting, depth output, and performance is explicitly resolved. An engineer implementing this system is assumed to have access to the `GpuSvoModel` specification and traversal routines.

---

## 1. Design Goals and Visual Contract

The system must satisfy the following constraints simultaneously:

1. **Macro-Perspective**
   * Entities exist in a normal 3D perspective world.
   * They scale with distance, occlude other geometry, and are occluded correctly via the depth buffer.

2. **Micro-Orthographic Projection**
   * Internally, each entity is rendered as if orthographically projected.
   * Parallel rays are used for all pixels of a single entity.
   * No perspective foreshortening occurs within the voxel model itself.

3. **Stable Virtual Pixel Grid**
   * Each entity defines a virtual pixel grid in the sprite plane, centered on the entity's model center.
   * The grid is quantized to a fixed world-space pixel size.
   * Pixel size does *not* depend on screen resolution.
   * All entities share the same *parameters* (`VoxelSize`, `σ`), ensuring consistent visual density, but each entity's grid is independent—sprites do not attempt to align with each other.

4. **Sprite-Space Cardinal Outline**
   * A 1-pixel black outline surrounds the visible silhouette.
   * Outline logic is defined in **2D sprite space**, not voxel space.
   * Only cardinal neighbors (up, down, left, right) produce outlines.
   * Diagonals never produce outlines.

5. **Single-Pass Volumetric Rendering**
   * Rendering occurs in a single fragment pass per entity.
   * No post-processing step is required to generate outlines.

---

## 2. Coordinate Spaces and Conventions

The system operates across four coordinate spaces:

1. **World Space**
   * Engine coordinate system (e.g., Godot with Y-up).
   * Perspective camera.

2. **Model Local Space**
   * The local coordinate system of the proxy QuadMesh.
   * The proxy quad is centered at the model center in world space.
   * Transforming the entity (position, rotation, scale) transforms the voxel model accordingly.
   * The voxel grid is axis-aligned to this space, not to world space.

3. **Voxel Space (Canonical)**
   * Right-handed, **Z-up** coordinate system.
   * This is the canonical space of `GpuSvoModel`.
   * All SVO traversal occurs exclusively in this space.

4. **Virtual Sprite Space (U/V)**
   * 2D grid perpendicular to the ray direction.
   * Axes:
     * `U` → camera right
     * `V` → camera up

### Coordinate Swizzling

If the engine uses a different up-axis (e.g., Godot's Y-up), positions and directions **must** be converted into canonical voxel space.

For Godot (Y-up) to Voxel (Z-up):
```
voxel.X =  godot.X
voxel.Y = -godot.Z
voxel.Z =  godot.Y
```

This transformation is applied on the CPU when computing camera and light vectors passed to the shader. The shader operates entirely in voxel space.

---

## 3. Virtual Pixel Definition

Each entity defines a virtual pixel grid in the sprite plane, centered on the entity's model center.

### Parameters

* `VoxelSize` — world-space size of one voxel (meters). Fixed game-wide once artistic decisions are finalized.
* `σ` — virtual pixels per voxel (scalar). Fixed game-wide once artistic decisions are finalized.

### Derived Quantities

The world-space width of one virtual pixel:
```
Δpx_world = VoxelSize / σ
```

The voxel-space width of one virtual pixel (used in shader):
```
Δpx_voxel = 1 / σ
```

These parameters are constants shared by all entities, ensuring consistent visual density.

---

## 4. Proxy Geometry

Each entity is represented by a **QuadMesh** that serves two purposes:

1. **Transform Handle:** The entity's position, orientation, and scale in world space define the corresponding transform of the voxel model inside. Moving, rotating, or scaling the entity transforms the rendered voxels accordingly.

2. **Rasterization Region:** The quad's screen projection determines which fragments invoke the volumetric shader. No pixels outside the quad's projection will be rendered.

### Quad Sizing (Per-Frame)

The quad is sized each frame to the **tight bounding rectangle** of the model's orthographic projection onto the camera plane. This is computed by projecting the model's AABB extents onto the camera right and up vectors.

For an AABB with voxel-space dimensions `(sX, sY, sZ)` and camera basis vectors `right` and `up` (in voxel space), the projected extents are:

```
quad_width  = (|right.x| * sX + |right.y| * sY + |right.z| * sZ) * VoxelSize + 2 * Δpx_world
quad_height = (|up.x| * sX + |up.y| * sY + |up.z| * sZ) * VoxelSize + 2 * Δpx_world
```

Where `2 * Δpx_world` is the outline margin (one virtual pixel on each side).

This produces a tight-fitting rectangle that covers exactly the model's projection plus outline from the current camera angle, with zero wasted fragments. Unlike a worst-case cube (which would use the 3D diagonal), the tight quad adapts to the actual viewing direction.

### Billboard Orientation (Per-Frame)

The quad is oriented each frame to face the camera. The container sets the quad's `GlobalTransform` with:

* **Basis X** = `camRight * quadWidth` (quad's local X spans the world-space width)
* **Basis Y** = `camUp * quadHeight` (quad's local Y spans the world-space height)
* **Basis Z** = `camForward` (perpendicular to the quad face)
* **Origin** = model center in world space

Because the quad always faces the camera, back-face culling is enabled (`cull_back`), halving the rasterized triangle count compared to a box with `cull_disabled`.

### Quad Center = Model Center (Not Anchor)

The quad is centered on the **model center** in world space, not on the entity's anchor point. This is critical because:

* `MODEL_MATRIX * vec4(0,0,0,1)` in the shader gives the quad origin's clip position, used for **NDC grid center** (the virtual pixel grid is centered on the model center's screen position).
* The quad's rasterized depth naturally provides the correct **billboard depth** (sprites occlude at model center depth) because the quad is centered on the model center.

If the quad were centered on the anchor instead, a flying character with a ground-level anchor would have its depth and pixel grid computed at ground level—completely wrong.

The model center world position is computed as:
```
modelCenterWorld = GlobalTransform * _modelCenterOffset
```
where `_modelCenterOffset` is the anchor-to-model-center vector in the entity's local space (including ground clearance).

### What the Quad Is Not

The quad's triangles are not the rendered surface. The visual output is entirely determined by ray-traced voxel intersections. The quad geometry merely triggers the fragment shader; the shader then ignores the quad surface and traces rays through voxel space.

### Anchor Point (Origin)

The **anchor point** is the position in voxel space that corresponds to the entity's world-space position. The container computes the offset from the anchor to the model center and applies it when positioning the quad.

**Key points:**

* The anchor point is NOT necessarily the model center.
* **Default anchor: bottom center.** The most common use case places entities on the ground, so the anchor is at the bottom center of the voxel model (e.g., `(sizeX/2, sizeY/2, 0)` in Z-up voxel space).
* **Any point is valid.** The anchor may be anywhere in voxel space—inside the model, on its surface, or even outside the model bounds.
* The anchor determines where the entity "stands" in the world. When you set an entity's world position, you are positioning its anchor point.

### Entity Management Pattern

In Godot, the recommended pattern is:

```
Node3D (Entity root - position/rotation set here)
└── MeshInstance3D (Proxy QuadMesh - billboard-oriented each frame, centered on model center)
```

The parent `Node3D` represents the entity's position and orientation in the game world. The child `MeshInstance3D` holding the proxy quad is repositioned and reoriented each frame by the container to handle:

1. **Anchor alignment:** The quad is centered on the model center, offset from the anchor point.

2. **Ground clearance:** Offset upward by `Δpx_world` (one virtual pixel) to prevent ground geometry from occluding the bottom outline pixels.

3. **Billboard orientation:** The quad faces the camera, with its width and height matching the tight bounding rectangle of the model's projection.

This separation provides a clean contract:
* Game logic sets the entity's world transform on the parent node (no need to account for outline clearance or billboard orientation).
* The container manages anchor offset, ground clearance, sizing, and orientation internally.

---

## 5. Virtual Pixel Determination

Each fragment determines its virtual pixel position based on **screen coordinates**, not fragment world position. This approach directly maps screen position to sprite pixel position.

### 5.1 Screen-to-Sprite Mapping

1. Compute the anchor point's clip-space position. Since the box is offset so the anchor is at local origin:
   ```
   origin_clip = ProjectionMatrix * ViewMatrix * ModelMatrix * vec4(0, 0, 0, 1)
   ```
   This matrix multiply provides the NDC center for sprite grid alignment. Billboard depth is provided automatically by the quad's rasterized depth (see Section 10).

2. Compute anchor's NDC position and fragment's offset from it:
   ```
   center_ndc = origin_clip.xy / origin_clip.w
   frag_ndc = (FragCoord.xy / ViewportSize) * 2.0 - 1.0
   offset_ndc = frag_ndc - center_ndc
   ```

3. Convert NDC offset directly to voxel units (combining world-unit conversion and voxel-unit conversion into one step):
   ```
   u_coord = offset_ndc.x * camera_distance / (ProjectionMatrix[0][0] * VoxelSize)
   v_coord = offset_ndc.y * camera_distance / (ProjectionMatrix[1][1] * VoxelSize)
   ```

4. Quantize to virtual pixel grid:
   ```
   u_snapped = round(u_coord / Δpx_voxel) * Δpx_voxel
   v_snapped = round(v_coord / Δpx_voxel) * Δpx_voxel
   ```

### 5.2 Camera Distance

The `camera_distance` parameter is the distance from the camera to the anchor point in world units. This must be provided as a uniform and updated per-frame.

For a perspective camera, this value determines the scale at which NDC offsets are converted to world units.

---

## 6. Ray Construction

### 6.1 Ray Direction (Per Instance)

All rays for a given entity share the same direction, achieving orthographic internal projection.

The ray direction is the camera forward vector transformed to voxel space:
```
D_voxel = swizzle(normalize(CameraForward))
```

This direction is constant for all fragments of the instance.

### 6.2 Camera Orientation Vectors

Two camera vectors are passed as uniforms:
* `ray_dir_local` — camera forward in voxel space (normalized)
* `camera_up_local` — camera up in voxel space (normalized)

The camera right vector is derived in the shader using the standard graphics convention:
```
camera_right_local = cross(ray_dir_local, camera_up_local)
```

### 6.3 Parallel Ray Origin

The ray origin is constructed from the quantized sprite position:
```
ray_origin = model_center
           + u_snapped * camera_right_local
           + v_snapped * camera_up_local
           - D_voxel * (2 * max_dimension)
```

Where:
* `model_center` — the model center in anchor-centered coordinates: `model_size * 0.5 - anchor_point`. Since the anchor is at the voxel-space origin, this is the offset from the anchor to the model center.
* `max_dimension` — largest dimension of the voxel model

The ray starts well in front of the model (toward the camera) so that traversal can properly intersect the voxel volume from its front face.

---

## 7. Primary Ray Traversal

Traversal uses **hierarchical SVO descent** with empty-space skipping. The SVO structure functions as an acceleration structure, not merely an occupancy classifier.

### 7.1 Pre-Traversal AABB Rejection

Before any SVO traversal, the fragment shader tests whether the ray intersects the voxel model's axis-aligned bounding box using the **slab method**:

```
t1 = (box_min - ray_origin) * inv_rd
t2 = (box_max - ray_origin) * inv_rd
t_min = min(t1, t2)    // entry t per axis
t_max = max(t1, t2)    // exit  t per axis
t_enter = max(t_min.x, t_min.y, t_min.z)   // last axis to enter
t_exit  = min(t_max.x, t_max.y, t_max.z)   // first axis to exit
hit = (t_exit >= max(t_enter, 0))
```

Any fragment whose ray does not intersect the voxel AABB **must be rejected in constant time** — no voxel stepping, SVO traversal, or leaf evaluation may occur. Late rejection inside iterative traversal is insufficient. This guarantees that empty regions of the proxy volume outside the model's projection incur minimal, bounded cost.

For outline detection, a second AABB test is performed against an expanded box (padded by `Δpx_voxel` on each side). If even this expanded test misses, the fragment cannot be a primary hit or an outline pixel, and is discarded immediately at O(1) cost.

### 7.2 Entry Face Detection

When the ray does intersect the AABB, the shader determines which face the ray enters through by identifying which axis produced the `t_enter` value (the largest `t_min` component). This axis is recorded as `last_axis` — the face the voxel was entered through — which is used for per-face lighting (see Section 8).

The entry face detection reuses the `t_min` components already computed by the slab AABB test, requiring no additional divisions (see Section 11.2).

### 7.3 Hierarchical Traversal

At each position along the ray, the shader descends the SVO tree from root to leaf:

1. Start at the root node. The initial child size is `2^(max_depth - 1)` voxels.
2. Read the node. If internal, check the occupancy mask for the octant containing the current position.
   * If the octant is **empty**, break out of the descent — this entire region is known empty.
   * If the octant is **occupied**, compute the child index and descend. The child size halves.
3. If a leaf is reached:
   * **Uniform leaf** (all 8 children share one material): check bits 0–7 for the material index. If non-zero, the ray has hit a solid voxel.
   * **Payload leaf** (2×2×2 individually-addressed voxels): read the specific byte for the current octant. If non-zero, hit.

Empty space represented by higher-level SVO nodes **must be skipped as a whole**. Implementations must not step through known-empty regions at voxel scale. This bounds traversal cost independently of ray length through empty space.

### 7.4 Node Advancement

When a position is found empty (at any level of the tree), the ray must advance past the empty node's bounding box:

1. Compute the axis-aligned bounds of the empty node from the current position and node size.
2. For each axis, compute the ray `t` at the node's exit face (selected by the ray's step direction).
3. The minimum of these three `t` values identifies the first exit — that axis becomes the new `last_axis`.
4. Advance `t_current` just past the exit point (with a small epsilon) and repeat.

### 7.5 Termination

Traversal terminates when:
* A solid voxel is hit → return material index and `last_axis`
* The ray exits the voxel AABB (`t_current >= t_exit`) → miss
* A maximum iteration count is exceeded → miss (safety bound: `max_depth × 64`)

### 7.6 Exactness Requirement

All traversal optimizations are **mathematically exact**. They must not:

* Alter silhouettes
* Alter depth values
* Introduce resolution-dependent artifacts
* Approximate voxel boundaries
* Depend on screen resolution or distance-based heuristics

Performance improvements come exclusively from eliminating provably unnecessary work (empty-space skipping, constant-time AABB rejection), never from reducing precision.

### 7.7 Implementation Freedom

This specification does not prescribe a specific traversal algorithm, shader structure, data layout, or graphics API. Any implementation that satisfies the above invariants while preserving pixel-perfect output is considered compliant.

---

## 8. Lighting

Lighting is not a core concern of this specification. The system's defining features are its orthographic projection, stable pixel grid, outline logic, and billboard depth (provided by the proxy quad's rasterized depth) — all of which are independent of how lit pixels are colored. Any lighting model can be used, from unlit palette colors to sophisticated PBR shading, as long as it operates on the hit voxel's material and face information provided by the traversal.

The current implementation provides a simple default lighting model suitable for the retro sprite aesthetic. It is described here as a reference, not as a requirement.

### 8.1 Light Direction

The light direction is specified in **world space** (engine coordinates) on the CPU side. The container class converts it to voxel space internally before passing it to the shader. This keeps voxel space as an internal implementation detail that external code does not need to know about.

If no explicit light direction is set, the default is **upper-right relative to the camera view**:

```
light_world = normalize(camera_right + camera_up * 0.5 - camera_forward)
light_voxel = normalize(swizzle(light_world))
```

The light direction points **from the surface toward the light source** (standard convention).

### 8.2 Default Per-Face Lighting Model

The default lighting model is a minimal directional light with ambient. It exploits the geometry of axis-aligned voxel cubes for simplicity and efficiency.

A voxel cube has 6 faces, but from any given viewpoint only **3 faces are visible** — one per axis. The visible face on each axis is the one whose outward normal points toward the camera, which is:

```
normal_axis = -sign(ray_direction) on that axis
```

For axis `i`, the diffuse lighting contribution simplifies to:

```
face_light[i] = max(-sign(rd[i]) * light_dir[i], 0.0)
```

This produces 3 precomputed lighting values — one per axis — that are constant for all fragments of the entity. When a ray hits a voxel, the shader selects the lighting value for `last_axis` (the face the ray entered through).

Final pixel color:
```
color = palette_color * (face_light[last_axis] + ambient)
```

Where `ambient` (currently 0.3) prevents unlit faces from being pure black.

### 8.3 Why Per-Face, Not Per-Normal

Since every voxel face along a given axis shares the same normal direction, and the light direction is constant per entity, the dot product is identical for all faces on the same axis. Computing it per-hit would repeat the same calculation. Precomputing 3 values and selecting by axis index is both simpler and faster.

### 8.4 Alternative Lighting Models

The traversal provides all the information needed for more sophisticated lighting:

* **Material index** — can index into material property tables (roughness, metallic, emissive, etc.)
* **Face normal** — determined by `last_axis` and `sign(rd)`, available for any shading model
* **Hit position** — derivable from `t_hit` and the ray, available for position-dependent effects

Possible alternatives include (but are not limited to):

* Multiple directional lights or point lights
* Ambient occlusion (e.g., precomputed per-voxel or screen-space)
* Emissive materials (material index maps to an emissive color)
* Toon/cel shading with quantized light bands
* Entirely unlit rendering (palette colors only)

The lighting model can be changed in the shader without affecting any other part of the system.

---

## 9. Sprite-Space Outline Logic

The outline is defined **exactly** as a 2D sprite dilation applied in virtual sprite space, not in voxel space.

### Conceptual Model

Imagine the entity rendered to an orthographic sprite buffer at resolution `Δpx`. The outline is the result of adding a black pixel to any transparent pixel that has at least one opaque **cardinal neighbor** (up, down, left, right). Diagonal neighbors never contribute to outlines.

This behavior is reproduced implicitly via ray tests.

### 9.1 Neighbor Rays

For a fragment whose primary ray **misses**, four additional rays are constructed by offsetting the ray origin along the camera basis vectors:

```
O_left  = ray_origin - camera_right_local * Δpx_voxel
O_right = ray_origin + camera_right_local * Δpx_voxel
O_up    = ray_origin + camera_up_local * Δpx_voxel
O_down  = ray_origin - camera_up_local * Δpx_voxel
```

Each neighbor ray shares the same direction `D_voxel` and performs identical SVO traversal.

### 9.2 Outline Rule

```
if primary ray hits:
    output material color with lighting
else if any neighbor ray hits:
    output black (outline)
else:
    discard fragment
```

Diagonal neighbors are never tested.

### 9.3 Performance Notes

* Neighbor rays are only evaluated for fragments whose primary ray misses.
* The neighbor ray tests use **early exit**: testing stops at the first neighbor hit.
* All rays are parallel and highly coherent.
* Worst-case cost (4 neighbor traces) occurs only along silhouettes.

---

## 10. Depth Output

Correct depth is mandatory for proper occlusion.

### Billboard Depth Model

The sprite behaves as a **flat billboard** at the sprite plane (the plane passing through the **model center**, perpendicular to the camera). This means:

* All pixels of the sprite share approximately the same depth value.
* The sprite is either entirely in front of or entirely behind other geometry.
* No partial occlusion occurs within a single sprite.

This matches the behavior of classic 2D sprites and maintains the visual coherence of the ortho-sprite effect. Attempting to compute per-voxel depth would cause visually confusing partial occlusion that breaks the 2D sprite illusion.

**Why model center, not anchor point:** The anchor point determines world positioning but may be anywhere in voxel space—even completely outside the model bounds (e.g., a flying character with an anchor at ground level). The sprite plane and depth must be at the model center where the visual content actually exists.

### Depth from Rasterization

Because the proxy quad is a camera-facing rectangle centered on the model center (Section 4), the GPU's standard rasterized depth is already correct. The quad surface lies in the sprite plane, so every fragment receives the model center's depth automatically. The shader does not need to write a custom `DEPTH` value — the rasterizer provides it for free.

Fragments that miss all rays (primary and neighbor) are discarded, so they do not write to the depth buffer.

---

## 11. Performance

The system's aesthetic relies on exact voxel-defined imagery. Performance improvements come exclusively from eliminating provably unnecessary work, never from reducing precision. The invariants in Section 7.6 are absolute.

Within those constraints, the following efficiency principles have been identified through implementation.

### 11.1 Per-Fragment Common Subexpression Sharing

Several values are constant for all rays within a fragment invocation (and indeed for all fragments of the entity). Computing them once and reusing them avoids redundant work:

| Value | Computed once | Used by |
|-------|--------------|---------|
| `inv_rd = 1.0 / rd` | Per fragment | All AABB tests (2 in fragment + 1 per trace call), all node exit computations |
| `step_dir = sign(rd)` | Per fragment | Entry face detection, node exit face selection, per-face lighting |
| `model_size = vec3(svo_model_size)` | Per fragment | All AABB tests, all bounds checks inside traversal |
| `step_positive = max(step_dir, vec3(0.0))` | Per trace call | Node exit face computation (selects between node_min and node_max per axis) |
| `root_child_size = float(1 << (max_depth - 1))` | Per trace call | Initial node size for each tree descent |

The trace function accepts `inv_rd`, `step_dir`, and `model_size` as parameters so they are computed once in the fragment shader and shared across all 1–5 trace calls per fragment (1 primary + up to 4 neighbor rays).

### 11.2 Origin Clip-Space Position

The fragment shader computes the clip-space position of the proxy quad's local origin for **NDC center** computation (screen-to-sprite mapping, Section 5.1):

```
origin_clip = ProjectionMatrix × ViewMatrix × ModelMatrix × vec4(0,0,0,1)
```

Billboard depth is handled automatically by the quad's rasterized depth (Section 10), so `origin_clip` is only needed for NDC.

### 11.3 Inlined AABB + Entry Face Detection

Inside the trace function, the ray–AABB intersection and entry face detection share the same intermediate values. The slab test computes per-axis `t_min` and `t_max` vectors. The entry face is simply the axis with the largest `t_min` component — the same values already computed for the intersection test.

A naive implementation would call `ray_aabb()` for the intersection, then recompute the per-axis `t` values to determine the entry face. The implementation inlines the slab test so that the `t_min` components are computed once and used for both the hit/miss decision and the entry face.

### 11.4 Reciprocal Ray Direction

The slab-method AABB test and the node-exit computation both require dividing by the ray direction. Since the ray direction is constant for all rays in the entity, its reciprocal (`inv_rd = 1.0 / rd`) is computed once and multiplied instead. This converts divisions to multiplications throughout:

* `ray_aabb()` takes `inv_rd` instead of `rd`
* Node exit computation uses `inv_rd`
* The 6 divisions that would otherwise occur per AABB test become 6 multiplications

### 11.5 Per-Face Lighting Precomputation

Because only 3 face normals are visible from any viewpoint and the light direction is constant per entity, the diffuse lighting for each axis is a single scalar computed once per fragment. The hit path then selects by axis index — a branch on an integer — rather than computing a dot product per hit. See Section 8.2.

### 11.6 Neighbor Ray Early Exit

Outline detection requires testing up to 4 neighbor rays (left, right, up, down). The tests are **unrolled** (no loop, no array allocation) and executed with **early exit**: each test is guarded by `if (!outline)`, so the first neighbor hit skips the remaining tests. On average, silhouette fragments test fewer than 4 neighbors.

### 11.7 NDC-to-Voxel Conversion

The screen-to-sprite mapping (Section 5.1) converts an NDC offset to voxel units. The naive approach uses 4 intermediate values:

```
proj_scale_x = 1.0 / ProjectionMatrix[0][0]
proj_scale_y = 1.0 / ProjectionMatrix[1][1]
world_offset = offset_ndc * camera_distance * proj_scale
voxel_offset = world_offset / VoxelSize
```

The implementation collapses this into a single expression per axis:

```
u_coord = offset_ndc.x * camera_distance / (ProjectionMatrix[0][0] * VoxelSize)
v_coord = offset_ndc.y * camera_distance / (ProjectionMatrix[1][1] * VoxelSize)
```

This eliminates 2 intermediate divisions.

### 11.8 Empty Vertex Shader

The proxy quad geometry exists solely to generate fragments. The vertex shader passes no varyings to the fragment shader — no interpolated positions, normals, or texture coordinates. The fragment shader derives everything it needs from screen coordinates and uniforms. This eliminates per-vertex computation and per-fragment interpolation overhead.

### 11.9 Tight Quad Fitting

The camera-facing quad is sized each frame to the tight bounding rectangle of the model's orthographic projection onto the camera plane (Section 4). This eliminates the wasted fragments that a worst-case cube proxy would generate.

A cube proxy covers the bounding sphere of the model from any angle. Its screen-space projection is a hexagon whose area exceeds the tight bounding rectangle by up to ~15% (depending on viewing angle and model aspect ratio). The tight quad covers exactly the needed rectangle, so every rasterized fragment has a chance of producing a visible pixel.

The per-frame CPU cost is minimal: projecting the AABB onto two camera vectors requires 6 absolute values, 6 multiplications, and 4 additions per entity per frame. Back-face culling (`cull_back`) is also enabled since the quad always faces the camera, halving the rasterized triangle count compared to a box with `cull_disabled`.

---

## 12. CPU-GPU Interface

### Per-Instance Uniforms

Each instance provides:

| Uniform | Type | Description |
|---------|------|-------------|
| `svo_texture` | sampler2D | SVO node and payload data (RGBA8, packed uint32 per texel) |
| `palette_texture` | sampler2D | Color palette (256 RGBA entries, indexed by material ID) |
| `texture_width` | int | Width of `svo_texture` in pixels (for 2D index computation) |
| `svo_nodes_count` | uint | Total node entries in texture (payload data starts after) |
| `svo_model_size` | uvec3 | Voxel model dimensions |
| `svo_max_depth` | uint | Maximum SVO tree depth |
| `node_offset` | uint | Offset to this model's nodes in texture |
| `payload_offset` | uint | Offset to this model's payloads |
| `voxel_size` | float | World-space size of one voxel |
| `sigma` | float | Virtual pixels per voxel |
| `anchor_point` | vec3 | Anchor point position in voxel space |

**Anchor point:** The `anchor_point` uniform is the position of the anchor in voxel space (e.g., `(sizeX/2, sizeY/2, 0)` for bottom-center). The shader computes the model center in anchor-centered coordinates as `model_size * 0.5 - anchor_point`.

### Per-Frame Uniforms

Updated each frame based on camera:

| Uniform | Type | Description |
|---------|------|-------------|
| `ray_dir_local` | vec3 | Camera forward in voxel space (unit length) |
| `camera_up_local` | vec3 | Camera up in voxel space (unit length) |
| `camera_distance` | float | Distance from camera to anchor point (world units) |
| `light_dir` | vec3 | Light direction in voxel space (unit length, toward light) |

**All vec3 uniforms must be normalized on the CPU.** The shader does not re-normalize them.

The `camera_right_local` vector is derived in the shader via cross product.

Billboard depth is provided by the quad's rasterized depth (the quad is camera-facing and centered on the model center), so it requires no uniform or shader computation.

### Light Direction

The light direction is set in **world space** on the CPU and converted to voxel space internally by the container class. External code never needs to work in voxel space — it is an internal implementation detail.

---

## 13. Constraints and Non-Goals

### Camera Interaction

* If the camera lies inside a proxy box, the instance should be culled and not rendered.
* Future revisions may relax this constraint.

### Scale Expectations

This system is designed for:

* Voxel models with maximum dimensions on the order of `128³` or smaller
* Proxy boxes covering a limited portion of the screen
* A limited number of visible instances

### Non-Goals

* This system is not intended to replace polygonal meshes for large continuous terrain or smooth curved surfaces.
* Alternative rendering methods should be used for distant or very large objects.
* No screen-space post-processing
* No diagonal outline logic
* No continuous LOD or impostor fallback (deferred)
* Scenes are assumed to be bounded in size and entity count

---

## 14. Data Flow Overview

1. **CPU Setup:**
   * Compute voxel bounds and model center
   * Compute anchor-to-model-center offset (including ground clearance)
   * Store outline margin (`Δpx_world`) for per-frame use
   * Create unit QuadMesh (actual size set per-frame)

2. **Per-Frame CPU Update:**
   * Compute camera vectors in voxel space (with coordinate swizzle)
   * Compute light direction in voxel space (from world-space input, with coordinate swizzle)
   * Compute camera distance
   * Update per-frame uniforms (all vec3 uniforms normalized)
   * Project model AABB onto camera right/up to compute tight quad dimensions
   * Set quad GlobalTransform as a billboard centered on the model center

3. **GPU Rasterization:**
   * Camera-facing proxy quad is rasterized, generating fragments

4. **Fragment Shader:**
   * Compute `origin_clip` once (for NDC center; billboard depth comes from rasterized quad depth)
   * Derive camera right from forward and up (cross product)
   * Compute screen offset from anchor, convert to voxel units, quantize to virtual pixel grid
   * Construct parallel ray origin
   * Precompute `inv_rd`, `step_dir`, `model_size` (shared across all trace calls)
   * Precompute per-face lighting (3 values, one per axis)
   * Pre-traversal AABB rejection: discard if no ray (primary or neighbor) can hit
   * If primary ray can hit: trace via hierarchical SVO traversal
     * If hit: output palette color × (face lighting + ambient)
     * If miss: test cardinal neighbor rays (early exit on first hit)
   * If primary ray cannot hit but expanded AABB passes: test neighbor rays for outline
   * Outline hit: output black
   * All misses: discard fragment

---

## 15. Glossary

* **Anchor Point** — The position in voxel space that corresponds to the entity's world-space position. Typically bottom center. Determines world placement.
* **Voxel Space** — Canonical Z-up coordinate system used by `GpuSvoModel`
* **Virtual Pixel** — A world-space pixel of width `Δpx_world = VoxelSize / σ`
* **Sprite Space** — 2D grid defined by camera-right (U) and camera-up (V)
* **Sprite Plane** — The plane passing through the model center, perpendicular to the camera. The proxy quad lies in this plane, providing billboard depth via rasterization.
* **Proxy Geometry** — The camera-facing QuadMesh used to invoke the volumetric shader; sized per-frame to the tight bounding rectangle of the model's projection
* **Primary Ray** — The ray corresponding to the current virtual pixel
* **Neighbor Ray** — A ray offset by ±Δpx in sprite space for outline evaluation
* **SVO** — Sparse Voxel Octree, the hierarchical data structure storing voxel occupancy and materials
* **Slab Method** — Ray–AABB intersection algorithm using per-axis entry/exit t values
* **`last_axis`** — The axis (0=X, 1=Y, 2=Z) the ray crossed to enter the hit voxel; determines which face was hit for lighting
* **`inv_rd`** — Reciprocal ray direction (1/rd), precomputed to convert divisions to multiplications
* **Empty-Space Skipping** — Advancing the ray past an entire empty SVO node rather than stepping voxel-by-voxel

---

## 16. Summary

This system renders voxel models as **volumetric orthographic sprites** embedded in a 3D perspective world. By combining:

* Screen-based virtual pixel determination
* Parallel per-instance rays (orthographic internal projection)
* Quantization to a stable virtual pixel grid
* Hierarchical SVO traversal with empty-space skipping
* Per-face directional lighting with precomputed axis values
* Sprite-space silhouette logic via neighbor ray tests with early exit
* Correct billboard depth from camera-facing proxy quad rasterization
* Shared common subexpressions across all trace calls per fragment

...the system achieves a visual result reminiscent of classic 2D sprites from the 16-bit gaming era, while remaining fully 3D, occlusion-correct, and GPU-driven.

This document constitutes the authoritative rendering specification for the Volumetric Ortho-Sprite System.
