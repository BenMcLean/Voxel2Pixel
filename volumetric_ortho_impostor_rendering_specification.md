# Volumetric Ortho-Impostor Rendering System

This document specifies a **GPU-driven volumetric impostor rendering system** for games. The system renders 3D voxel models (stored as GPU Sparse Voxel Octrees, *GpuSvoModel*) as *sprite-like entities* inside a fully perspective 3D world, while preserving **orthographic internal projection**, a **stable virtual pixel grid**, and a **1-pixel silhouette outline defined in sprite space**.

This specification is written to be *implementation-ready*. All ambiguity around coordinate spaces, ray setup, quantization, outlining, and depth output is explicitly resolved. An engineer implementing this system is assumed to have access to the `GpuSvoModel` specification and traversal routines.

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
   * The local coordinate system of the proxy BoxMesh.
   * The proxy box is centered at the origin.
   * Transforming the box (position, rotation, scale) transforms the voxel model accordingly.
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

This transformation is applied on the CPU when computing camera vectors passed to the shader. The shader operates entirely in voxel space.

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

Each entity is represented by a **BoxMesh** that serves two purposes:

1. **Transform Handle:** The box's position, orientation, and scale in world space define the corresponding transform of the voxel model inside. Moving, rotating, or scaling the box transforms the rendered voxels accordingly. The voxel model is rigidly attached to the box's local coordinate system.

2. **Rasterization Region:** The box's screen projection determines which fragments invoke the volumetric shader. No pixels outside the box's projection will be rendered.

### Box Dimensions

* The box encloses the voxel model's bounds in the box's local space.
* The box is **expanded by `Δpx_world` in all three local-space dimensions** to ensure outline pixels are captured from any camera angle.

**Rationale:** Since the sprite plane (U, V) rotates with the camera, uniform expansion in all three dimensions guarantees adequate margin for outline rendering regardless of viewing direction.

### Transform Behavior

* The voxel grid is aligned to the box's local axes, not to world axes.
* The box may be placed at any position and orientation in world space.
* Rotating the box rotates the voxel model; the internal voxel grid remains axis-aligned relative to the box.
* This enables intuitive manipulation: in a VR authoring tool, a user can physically grab and rotate the proxy box to view the model from different angles.

### What the Box Is Not

The box's triangles are not the rendered surface. The visual output is entirely determined by ray-traced voxel intersections inside the box volume. The box geometry merely triggers the fragment shader; the shader then ignores the box surface and traces rays through voxel space.

### Anchor Point (Origin)

The **anchor point** is the position in voxel space that corresponds to the entity's world-space position. The proxy box is offset so that the anchor point aligns with the box's local origin.

**Key points:**

* The anchor point is NOT necessarily the model center.
* **Default anchor: bottom center.** The most common use case places entities on the ground, so the anchor is at the bottom center of the voxel model (e.g., `(sizeX/2, sizeY/2, 0)` in Z-up voxel space).
* **Any point is valid.** The anchor may be anywhere in voxel space—inside the model, on its surface, or even outside the model bounds.
* The anchor determines where the entity "stands" in the world. When you set an entity's world position, you are positioning its anchor point.

**Ground placement note:** When placing entities on ground geometry, the entity should float slightly above the ground surface (by at least `Δpx_world`) to prevent ground geometry from occluding the bottom outline pixels.

### Entity Management Pattern

In Godot, the recommended pattern is:

```
Node3D (Entity root - position/rotation set here)
└── MeshInstance3D (Proxy BoxMesh - offset to align anchor with parent origin)
```

The parent `Node3D` represents the entity's position and orientation in the game world. The child `MeshInstance3D` holding the proxy box is offset so that the anchor point in voxel space aligns with the parent's origin.

This separation provides a clean contract:
* Game logic sets the entity's world transform on the parent node.
* The rendering system manages the box offset internally based on the anchor point.

---

## 5. Virtual Pixel Determination

Each fragment determines its virtual pixel position based on **screen coordinates**, not fragment world position. This approach directly maps screen position to sprite pixel position.

### 5.1 Screen-to-Sprite Mapping

1. Compute the anchor point's screen position (NDC). Since the box is offset so the anchor is at local origin:
   ```
   anchor_clip = ProjectionMatrix * ViewMatrix * ModelMatrix * vec4(0, 0, 0, 1)
   anchor_ndc = anchor_clip.xy / anchor_clip.w
   ```

2. Compute fragment's offset from anchor in NDC:
   ```
   frag_ndc = (FragCoord.xy / ViewportSize) * 2.0 - 1.0
   offset_ndc = frag_ndc - anchor_ndc
   ```

3. Convert to world units at the sprite plane:
   ```
   proj_scale_x = 1.0 / ProjectionMatrix[0][0]
   proj_scale_y = 1.0 / ProjectionMatrix[1][1]

   world_offset_x = offset_ndc.x * camera_distance * proj_scale_x
   world_offset_y = offset_ndc.y * camera_distance * proj_scale_y
   ```

4. Convert to voxel units:
   ```
   u_coord = world_offset_x / VoxelSize
   v_coord = world_offset_y / VoxelSize
   ```

5. Quantize to virtual pixel grid:
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
ray_origin = anchor_to_center
           + u_snapped * camera_right_local
           + v_snapped * camera_up_local
           - D_voxel * (2 * max_dimension)
```

Where:
* `anchor_to_center` — offset from anchor point to model center (the `anchor_to_center` uniform). Since the anchor is at voxel space origin, this equals the model center's position in voxel space.
* `max_dimension` — largest dimension of the voxel model

The ray starts well in front of the model (toward the camera) so that DDA traversal can properly intersect the voxel volume from its front face.

---

## 7. Primary Ray Traversal

Traversal uses a **3D Digital Differential Analyzer (DDA)** in voxel space.

### Ray-Voxel-AABB Intersection

Before DDA traversal, the fragment shader computes a ray-AABB intersection against the voxel bounds:
* If the ray does not intersect the voxel AABB, the fragment proceeds to outline logic.
* If the ray intersects, DDA traversal begins at the entry point.

### Traversal Model

* The ray is marched through the integer voxel grid starting at the voxel-AABB entry point.
* At each step, the voxel coordinate `(x, y, z)` currently intersected by the ray is queried via `GpuSvoModel`.
* A voxel is considered **solid** if its resolved material index ≠ 0.

### Hit Definition

A ray is considered to **hit** if it intersects any solid voxel. Partial voxel intersections count as hits. Uniform leaves are treated as fully solid regions.

### Termination Conditions

Traversal terminates when:
* A solid voxel is hit
* The ray exits the voxel AABB
* A fixed maximum traversal distance (derived from model bounds) is exceeded

The traversal step size and axis advancement follow standard voxel DDA rules.

### Outcomes

* **Hit** → return material color (and normal for lighting)
* **Miss** → candidate for outline logic

---

## 8. Sprite-Space Outline Logic

The outline is defined **exactly** as a 2D sprite dilation applied in virtual sprite space, not in voxel space.

### Conceptual Model

Imagine the entity rendered to an orthographic sprite buffer at resolution `Δpx`. The outline is the result of adding a black pixel to any transparent pixel that has at least one opaque **cardinal neighbor** (up, down, left, right). Diagonal neighbors never contribute to outlines.

This behavior is reproduced implicitly via ray tests.

### 8.1 Neighbor Rays

For a fragment whose primary ray **misses**, construct four additional rays:

```
O_left  = ray_origin - camera_right_local * Δpx_voxel
O_right = ray_origin + camera_right_local * Δpx_voxel
O_up    = ray_origin + camera_up_local * Δpx_voxel
O_down  = ray_origin - camera_up_local * Δpx_voxel
```

Each neighbor ray:
* Shares the same direction `D_voxel`
* Performs identical SVO traversal

### Outline Rule

```
if primary ray hits:
    output material color
else if any neighbor ray hits:
    output black (outline)
else:
    discard fragment
```

Diagonal neighbors are never tested.

### 8.2 Performance Notes

* Neighbor rays are only evaluated for fragments whose primary ray misses.
* All rays are parallel and highly coherent.
* Worst-case cost occurs only along silhouettes.

---

## 9. Depth Output

Correct depth writing is mandatory for proper occlusion.

### Billboard Depth Model

The sprite writes depth as if it were a **flat billboard** at the sprite plane (the plane passing through the **model center**, perpendicular to the camera). This means:

* All pixels of the sprite share the same depth value.
* The sprite is either entirely in front of or entirely behind other geometry.
* No partial occlusion occurs within a single sprite.

This matches the behavior of classic 2D sprites and maintains the visual coherence of the ortho-impostor effect. Attempting to compute per-voxel depth would cause visually confusing partial occlusion that breaks the 2D sprite illusion.

**Why model center, not anchor point:** The anchor point determines world positioning but may be anywhere in voxel space—even completely outside the model bounds (e.g., a flying character with an anchor at ground level). The sprite plane and depth must be at the model center where the visual content actually exists.

### Depth Calculation

The depth is the clip-space depth of the model center. Since the anchor point is at the box's local origin, the model center in local space is the `anchor_to_center` offset (converted to world units):

```
model_center_local = reverse_swizzle(anchor_to_center) * VoxelSize
center_clip = ProjectionMatrix * ViewMatrix * ModelMatrix * vec4(model_center_local, 1)
DEPTH = (center_clip.z / center_clip.w) * 0.5 + 0.5
```

This value is constant for all fragments of the sprite (both solid pixels and outlines).

### Optimization

Since the depth is constant per-instance, it can be computed once per frame on the CPU and passed as a uniform, rather than recomputed in every fragment.

---

## 10. CPU-GPU Interface

### Per-Instance Uniforms

Each instance provides:

| Uniform | Type | Description |
|---------|------|-------------|
| `svo_texture` | sampler2D | SVO node and payload data |
| `palette_texture` | sampler2D | Color palette (256 entries) |
| `svo_model_size` | uvec3 | Voxel model dimensions |
| `svo_max_depth` | uint | Maximum SVO tree depth |
| `node_offset` | uint | Offset to this model's nodes in texture |
| `payload_offset` | uint | Offset to this model's payloads |
| `voxel_size` | float | World-space size of one voxel |
| `sigma` | float | Virtual pixels per voxel |
| `anchor_to_center` | vec3 | Offset from anchor point to model center in voxel space |

**Anchor offset:** The `anchor_to_center` uniform represents the vector from the anchor point (at the box's local origin) to the center of the voxel model. For a bottom-center anchor on a model of size `(sizeX, sizeY, sizeZ)`, this would be `(sizeX/2, sizeY/2, sizeZ/2)` in Z-up voxel space.

### Per-Frame Uniforms

Updated each frame based on camera:

| Uniform | Type | Description |
|---------|------|-------------|
| `ray_dir_local` | vec3 | Camera forward in voxel space |
| `camera_up_local` | vec3 | Camera up in voxel space |
| `camera_distance` | float | Distance from camera to anchor point |
| `billboard_depth` | float | Pre-computed depth of sprite plane (optional optimization) |
| `light_dir` | vec3 | Light direction in voxel space (optional) |

The `camera_right_local` vector is derived in the shader via cross product.

If `billboard_depth` is not pre-computed on the CPU, the shader can compute it from the model center's clip-space position (see Section 9).

---

## 11. Constraints and Non-Goals

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

## 12. Data Flow Overview

1. **CPU Setup:**
   * Compute voxel bounds and model center
   * Compute proxy box dimensions (model bounds + outline padding in all 3 axes)
   * Place proxy box in world space

2. **Per-Frame CPU Update:**
   * Compute camera vectors in voxel space (with coordinate swizzle)
   * Compute camera distance
   * Optionally pre-compute sprite plane depth (billboard depth)
   * Update per-frame uniforms

3. **GPU Rasterization:**
   * Proxy box is rasterized, generating fragments

4. **Fragment Shader:**
   * Derive camera right from forward and up (cross product)
   * Compute screen offset from model center
   * Convert to voxel units and quantize to virtual pixel grid
   * Construct parallel ray origin
   * Traverse SVO via DDA
   * If hit: output material color
   * If miss: test cardinal neighbor rays for outline
   * If outline: output black
   * Otherwise: discard fragment
   * All visible pixels write the same billboard depth (sprite plane depth)

---

## 13. Glossary

* **Anchor Point** — The position in voxel space that corresponds to the entity's world-space position. Typically bottom center. Determines depth and world placement.
* **Voxel Space** — Canonical Z-up coordinate system used by `GpuSvoModel`
* **Virtual Pixel** — A world-space pixel of width `Δpx_world = VoxelSize / σ`
* **Sprite Space** — 2D grid defined by camera-right (U) and camera-up (V)
* **Sprite Plane** — The plane passing through the model center, perpendicular to the camera. Used for billboard depth.
* **Proxy Geometry** — The BoxMesh used to invoke the volumetric shader and define entity transform
* **Primary Ray** — The ray corresponding to the current virtual pixel
* **Neighbor Ray** — A ray offset by ±Δpx in sprite space for outline evaluation
* **DDA** — Digital Differential Analyzer, a voxel grid traversal algorithm

---

## 14. Summary

This system renders voxel models as **volumetric orthographic sprites** embedded in a 3D perspective world. By combining:

* Screen-based virtual pixel determination
* Parallel per-instance rays (orthographic internal projection)
* Quantization to a stable virtual pixel grid
* Sprite-space silhouette logic via neighbor ray tests
* Correct depth output for occlusion

...the system achieves a visual result reminiscent of classic 2D sprites from the 16-bit gaming era, while remaining fully 3D, occlusion-correct, and GPU-driven.

This document constitutes the authoritative rendering specification for the Volumetric Ortho-Impostor System.
