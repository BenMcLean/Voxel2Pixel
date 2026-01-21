# Volumetric Ortho‑Impostor Rendering System

This document specifies a **GPU‑driven volumetric impostor rendering system** for games. The system renders 3D voxel models (stored as GPU Sparse Voxel Octrees, *GpuSvoModel*) as *sprite‑like entities* inside a fully perspective 3D world, while preserving a **fixed world‑space pixel grid**, **orthographic internal projection**, and a **1‑pixel silhouette outline defined in sprite space**.

This specification is written to be *implementation‑ready*. All ambiguity around coordinate spaces, ray setup, quantization, outlining, and depth output is explicitly resolved. An engineer implementing this system is assumed to have access to the `GpuSvoModel` specification and traversal routines.

---

## 1. Design Goals and Visual Contract

The system must satisfy the following constraints simultaneously:

1. **Macro‑Perspective**
   * Entities exist in a normal 3D perspective world.
   * They scale with distance, occlude other geometry, and are occluded correctly via the depth buffer.

2. **Micro‑Orthographic Projection**
   * Internally, each entity is rendered as if orthographically projected.
   * Parallel rays are used for all pixels of a single entity.
   * No perspective foreshortening occurs within the voxel model itself.

3. **World‑Space Virtual Pixel Grid**
   * The rendered appearance is quantized to a fixed world‑space pixel size.
   * Pixel size does *not* depend on screen resolution.
   * All entities share the same grid, preventing pixel shimmer.

4. **Sprite‑Space Cardinal Outline**
   * A 1‑pixel black outline surrounds the visible silhouette.
   * Outline logic is defined in **2D sprite space**, not voxel space.
   * Only cardinal neighbors (up, down, left, right) produce outlines.
   * Diagonals never produce outlines.

5. **Single‑Pass Volumetric Rendering**
   * Rendering occurs in a single fragment pass per entity.
   * No post‑processing step is required to generate outlines.

---

## 2. Coordinate Spaces and Conventions

The system operates across four coordinate spaces:

1. **World Space**
   * Engine coordinate system (e.g., Godot).
   * Perspective camera.

2. **Model Local Space**
   * Axis‑aligned space of the proxy BoxMesh.
   * Used for ray–box intersection.

3. **Voxel Space (Canonical)**
   * Right‑handed, **Z‑up** coordinate system.
   * This is the canonical space of `GpuSvoModel`.
   * All SVO traversal occurs exclusively in this space.

4. **Virtual Sprite Space (U/V)**
   * 2D grid perpendicular to the ray direction.
   * Axes:
	 * `U` → camera right
	 * `V` → camera up

### Coordinate Swizzling

If the engine uses a different up‑axis (e.g., Y‑up), the fragment shader **must** convert positions and directions into canonical voxel space **before traversal**.

---

## 3. Virtual Pixel Definition

### Parameters

* `VoxelSize` — world‑space size of one voxel (meters).
* `σ` — virtual pixels per voxel (scalar).

### Derived Quantity

The world‑space width of one virtual pixel is:

```
Δpx = VoxelSize / σ
```

`Δpx` is a **constant world‑space value** shared by all entities.

---

## 4. Proxy Geometry

Each entity is represented by an **axis‑aligned BoxMesh** in world space.

* The box encloses the voxel model’s full bounds.
* The box is expanded by `Δpx` in U and V to allow outline rendering.
* The box is rendered with a custom fragment shader.

The BoxMesh itself has no visual meaning; it exists only to define a rasterization region.

---

## 5. Ray Construction

### 5.1 Ray Direction (Per Instance)

All rays for a given entity share the same direction.

```
D_world = normalize(CameraForward)
D_local = normalize(ModelMatrix⁻¹ * D_world)
```

`D_local` is constant for all fragments of the instance.

---

### 5.2 Ray–Box Entry (Per Fragment)

The fragment shader **must not** use fragment world position as the ray origin.

Instead, the ray origin is computed via **ray–AABB intersection**.

#### Inputs

* Camera position `C_world`
* Ray direction `D_world`
* Box AABB in local space: `[Bmin, Bmax]`

#### Procedure

1. Transform camera into local space:

```
C_local = ModelMatrix⁻¹ * C_world
```

2. Perform slab‑based ray–AABB intersection:

```
t1 = (Bmin - C_local) / D_local
t2 = (Bmax - C_local) / D_local

t_enter = max(min(t1, t2))
t_exit  = min(max(t1, t2))
```

3. If `t_exit < max(t_enter, 0)`, discard fragment.

4. Ray origin:

```
O_local = C_local + max(t_enter, 0) * D_local
```

This `O_local` is the ray origin used for all subsequent logic.

---

## 6. Virtual Pixel Quantization

To enforce the world‑space pixel grid, the ray origin is quantized **in the sprite plane**.

### Sprite Plane Axes

```
U = normalize(CameraRight)
V = normalize(CameraUp)
```

### Quantization

Project the ray origin onto the sprite plane and snap:

```
Oq = O_world
Oq += round(dot(O_world, U) / Δpx) * Δpx * U
Oq += round(dot(O_world, V) / Δpx) * Δpx * V
```

This ensures all rays originate from centers of virtual pixels.

---

## 7. Primary Ray Traversal

Traversal uses a **3D Digital Differential Analyzer (DDA)** in voxel space, but only after a preliminary bounds test.

### Traversal Bounds

Two distinct bounding volumes are used:

* **Proxy Box** — Expanded world-space box used solely to generate fragments (including outline pixels).
* **Voxel AABB** — Tight axis-aligned bounding box enclosing the voxel model in voxel space, transformed to world space.

Voxel traversal is performed **only if the ray intersects the voxel AABB**. Rays that intersect the proxy box but miss the voxel AABB are treated as transparent and do not initiate DDA traversal.

### Ray–Voxel-AABB Intersection

Before DDA traversal, the fragment shader computes a ray–AABB intersection against the voxel bounds:

* If the ray does not intersect the voxel AABB, the fragment outputs transparency (or outline logic only).
* If the ray intersects, the entry distance `t_entry` is used as the starting point for DDA traversal.

This test is inexpensive and prevents unnecessary traversal for outline-only pixels.

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
* A fixed maximum traversal distance (derived from voxel bounds) is exceeded

The traversal step size and axis advancement follow standard voxel DDA rules.

Traversal uses a **3D Digital Differential Analyzer (DDA)** in voxel space.

### Traversal Model

* The ray is marched through the integer voxel grid.
* At each step, the voxel coordinate `(x, y, z)` currently intersected by the ray is queried via `GpuSvoModel`.
* A voxel is considered **solid** if its resolved material index ≠ 0.

### Hit Definition

A ray is considered to **hit** if it intersects any solid voxel. Partial voxel intersections count as hits. Uniform leaves are treated as fully solid regions.

### Termination Conditions

Traversal terminates when:

* A solid voxel is hit
* The ray exits the voxel bounds
* A fixed maximum traversal distance (derived from model bounds) is exceeded

The traversal step size and axis advancement follow standard voxel DDA rules.

A single ray is cast per virtual pixel.

* Origin: quantized `Oq`
* Direction: `D_local`

Traversal uses the `GpuSvoModel` SVO via DDA stepping.

### Outcomes

* **Hit** → return material color
* **Miss** → candidate for outline logic

Traversal terminates early on hit or on exiting bounds.

---

## 8. Sprite‑Space Outline Logic

The outline is defined **exactly** as a 2D sprite dilation applied in virtual sprite space, not in voxel space.

### Conceptual Model

Imagine the entity rendered to an orthographic sprite buffer at resolution `Δpx`. The outline is the result of adding a black pixel to any transparent pixel that has at least one opaque **cardinal neighbor** (up, down, left, right). Diagonal neighbors never contribute to outlines.

This behavior is reproduced implicitly via ray tests.

The outline is defined **exactly** as a 2D sprite dilation.

---

### 8.1 Neighbor Rays

For a fragment whose primary ray **misses**:

Construct up to four additional rays:

```
O_left  = Oq - U * Δpx
O_right = Oq + U * Δpx
O_up    = Oq + V * Δpx
O_down  = Oq - V * Δpx
```

Each neighbor ray:

* Shares the same direction `D_local`
* Performs identical SVO traversal

### Outline Rule

```
if primary ray hits:
	output material
else if any neighbor ray hits:
	output black
else:
	discard
```

Diagonal neighbors are never tested.

---

### 8.2 Performance Notes

* Neighbor rays are only evaluated for fragments whose primary ray misses.
* All rays are parallel and highly coherent.
* Worst‑case cost occurs only along silhouettes.

Optimized or approximate variants may replace this logic in future revisions.

---

## 9. Depth Output

Correct depth writing is mandatory.

### At Hit Time

When a ray intersects a voxel:

```
P_hit_local = O_local + t_hit * D_local
P_hit_world = ModelMatrix * P_hit_local
```

Convert to clip space:

```
P_clip = ProjectionMatrix * ViewMatrix * vec4(P_hit_world, 1)
DEPTH = P_clip.z / P_clip.w
```

### Border Pixels

Border pixels write depth equal to the **minimum hit distance** among all neighbor rays that triggered the outline.

A small depth bias may be applied to avoid z‑fighting.

Border pixels write depth equal to the **nearest solid voxel hit** of the neighbor ray that caused the outline.

A small depth bias may be applied to avoid z‑fighting.

---

## 10. CPU ↔ GPU Interface

Each instance provides:

* Model matrix
* Anchor voxel coordinate (any finite coordinate is valid)
* Anchor offset derived from box and voxel bounds
* SVO root index or texture layer
* `VoxelSize`
* `σ` (pixels per voxel)

The proxy box dimensions are computed to enclose both the voxel model and the anchor, regardless of anchor position. Extremely large anchor offsets are valid but may negatively impact performance.

Each instance provides:

* Model matrix
* Anchor offset (voxel pivot)
* SVO root index or texture layer
* `VoxelSize`
* `σ` (pixels per voxel)

Rendering is expected to use a custom `RenderingDevice` pipeline.

---

## 11. Constraints and Non‑Goals

### Camera Interaction

* If the camera lies inside a proxy box, the instance is culled and not rendered.
* Future revisions may relax this constraint.

### Scale Expectations

This system is designed for:

* Voxel models with maximum dimensions on the order of `128³` or smaller
* Proxy boxes covering a limited portion of the screen
* A limited number of visible instances

### Non‑Goals

* This system is not intended to replace polygonal meshes for large continuous terrain or smooth curved surfaces.
* Alternative rendering methods should be used for distant or very large objects.

* No screen‑space post‑processing
* No diagonal outline logic
* No continuous LOD or impostor fallback (deferred)
* Scenes are assumed to be bounded in size and entity count

---

## 12. Data Flow Overview

1. CPU computes voxel bounds, virtual pixel size, outline padding, and proxy box dimensions
2. CPU computes anchor offset and places the proxy box in world space
3. Proxy box is rasterized
4. Fragment shader:
   * Computes ray–box entry
   * Quantizes ray origin to the virtual pixel grid
   * Traverses the SVO via DDA
   * Applies sprite‑space outline logic
   * Writes color and depth

---

## 13. Glossary

* **Voxel Space** — Canonical Z‑up coordinate system used by `GpuSvoModel`
* **Virtual Pixel** — A world‑space pixel of width `Δpx`
* **Sprite Space** — 2D grid defined by camera‑right (U) and camera‑up (V)
* **Proxy Geometry** — The BoxMesh used to invoke the volumetric shader
* **Primary Ray** — The ray corresponding to the current virtual pixel
* **Neighbor Ray** — A ray offset by ±Δpx in sprite space for outline evaluation

---

## 14. Summary

This system renders voxel models as **volumetric orthographic sprites** embedded in a 3D perspective world. By combining:

* World‑space virtual pixels
* Parallel per‑instance rays
* Explicit ray–box entry math
* Sprite‑space silhouette logic
* Correct depth output

…the system achieves a visual result reminiscent of classic 2D sprites from the 16-bit gaming era, while remaining fully 3D, occlusion‑correct, and GPU‑driven.

This document constitutes the authoritative rendering specification for the Volumetric Ortho‑Impostor System.
