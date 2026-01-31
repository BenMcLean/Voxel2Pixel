# Discrete Heightfield Terrain System

## Tile Classification + Mesh Synthesis

### Marching-Squares–Style Terrain (Godot 4 / C#)

---

## 1. System Overview

This system converts an integer heightfield into:

1. A **Tile Surface Map** describing the surface type and orientation of each tile
2. A **renderable terrain mesh** synthesized from that semantic map

The tile surface map is a stable, reusable intermediate representation that may be used by gameplay systems (placement, alignment, AI, etc.) independently of rendering.

This approach is equivalent to a tile-archetype heightfield renderer using marching-squares–style neighborhood classification.

---

## 2. Core Concepts and Terminology

### Tile

A square cell in a 2D grid with an associated integer height.

### Heightfield

A 2D scalar field `h(x, z)` representing terrain elevation.

### Tile Surface Type

A semantic description of how a tile’s top surface behaves geometrically (flat, ramp, etc.).

### Marching Squares (Adapted)

A local neighborhood classification technique where each tile selects a surface type based on the relative heights of its neighbors.

This system uses marching-squares–style logic to classify tiles, **not** to extract contours.

---

## 3. Input Data

```csharp
int[,] heightMap;
```

* Rectangular grid
* Heights are integers
* Adjacent tiles may differ by any amount
* The height map includes a one-tile apron on all sides
* Only the interior region is classified and rendered

---

## 4. Tile Surface Types

Each tile is assigned **exactly one** of the following types:

```csharp
enum TileSurfaceType
{
	Flat,
	CardinalSlopeNorth,
	CardinalSlopeEast,
	CardinalSlopeSouth,
	CardinalSlopeWest,
	DiagonalSlopeNE,
	DiagonalSlopeSE,
	DiagonalSlopeSW,
	DiagonalSlopeNW
}
```

This enum is a **semantic contract**, not a rendering detail.

---

## 5. Phase 1: Tile Classification

### Purpose

Convert the heightfield into a `TileSurfaceType[,]` map that:

* Is deterministic
* Depends only on local neighbors
* Can be reused by non-rendering systems

Tile classification is performed only on the interior region of the height map; apron tiles are never classified or rendered.

---

### Neighborhood Evaluation

For tile `(x, z)`:

```
hC = heightMap[x, z]

dN = heightMap[x, z - 1] - hC
dE = heightMap[x + 1, z] - hC
dS = heightMap[x, z + 1] - hC
dW = heightMap[x - 1, z] - hC
```

---

### Classification Rules (Evaluated in Order)

#### Rule 1 — Cliff Dominance

If **any** neighbor satisfies:

```
dX ≤ -2
```

Then:

```
TileSurfaceType = Flat
```

No slope is permitted on this tile.

---

#### Rule 2 — Cardinal Slope

If **exactly one** neighbor satisfies:

```
dX == -1
```

And all other neighbors satisfy:

```
dY ≥ 0
```

Then:

```
TileSurfaceType = CardinalSlope<Direction>
```

---

#### Rule 3 — Diagonal Slope

If **exactly two orthogonal neighbors** satisfy:

```
dX == -1
```

And all other neighbors satisfy:

```
dY ≥ 0
```

Then:

```
TileSurfaceType = DiagonalSlope<Directions>
```

---

#### Rule 4 — Flat

If none of the above rules apply:

```
TileSurfaceType = Flat
```

---

### Output of Phase 1

```csharp
TileSurfaceType[,] surfaceMap;
```

This map is authoritative for all later stages.

---

## 6. Phase 2: Mesh Synthesis

### Purpose

Generate a renderable terrain mesh using:

* `heightMap`
* `surfaceMap`

No tile reclassification occurs in this phase.

---

## 7. Chunking and Boundary Handling

Mesh synthesis may operate per chunk.

For a chunk of size `N × N`, the height map provided to classification and mesh synthesis must be at least `(N + 2) × (N + 2)` to supply a one-tile apron.

### Rules

* Tile classification must consider neighbors outside the chunk
* Mesh synthesis may emit geometry only for tiles inside the chunk
* Chunk boundaries are **cuts through continuous geometry**, not semantic edges

This requires access to a **1-tile ghost border** of height and surface data.

---

## 8. Intermediate Representation: Surface Patches

Mesh synthesis uses an intermediate **surface patch** representation.

Surface patches are an internal mesh-generation construct and do not affect tile classification or gameplay semantics.

### Surface Patch Definition

A surface patch is a maximal connected set of tiles that:

* Have the same `TileSurfaceType`
* Lie on the same geometric plane
* Can be rendered without internal edges

---

### Patch Formation Rules

* Flat tiles at the same height may form multi-tile patches
* Slope tiles may form patches **only if**:

  * They have identical slope orientation
  * Their geometry lies on the same plane
  * They share the same base height
* Patches may be truncated by chunk boundaries

This replaces all statements about “unique vertices per tile”.

---

## 9. Geometry Generation Rules

### Top Surfaces

* One polygonal surface is generated per surface patch
* Vertices are generated only along patch boundaries
* Internal tile edges within a patch produce no vertices

---

### Cliff Faces

For each tile edge:

If:

```
neighbor_height < current_height
```

And **no slope occupies that edge**, generate a vertical face with height:

```
(current_height - neighbor_height) * heightStep
```

Cliff faces:

* Are generated after surface patches
* May merge vertically along the Y axis when adjacent tiles share the same drop height, but are not merged horizontally across tile edges. 
* Are independent of tile surface type

---

## 10. Vertex Sharing Rules (Revised)

Vertices may be shared **whenever**:

* Positions coincide
* Normals coincide
* Surfaces lie on the same plane

This applies equally to flat surfaces and slope patches.

Vertex duplication is a *consequence* of topology, not a rule.

---

## 11. Public API Structure

### Tile Classification

```csharp
// heightMap includes a one-tile apron on all sides
public static TileSurfaceType[,] ClassifyTiles(int[,] heightMap);
```

---

### Mesh Synthesis (Per Chunk)

```csharp
public static ArrayMesh BuildChunkMesh(
	int[,] heightMap,
	TileSurfaceType[,] surfaceMap,
	int chunkX,
	int chunkZ,
	int chunkSize,
	float tileSize,
	float heightStep
);
```

Instantiation into `MeshInstance3D` is handled externally.

---

## 12. Properties of This Design

* Tile semantics are explicit and reusable
* Mesh generation is deterministic and chunk-safe
* Continuous surfaces are naturally merged
* Slopes are well-defined and never ambiguous
* Rendering and gameplay concerns are decoupled

---

## 13. Summary

This system implements a **marching-squares–style tile classification** to convert a heightfield into explicit terrain semantics, followed by a **surface-patch–based mesh synthesis** that produces clean, efficient 3D geometry.

By separating classification from synthesis, it supports both visual correctness and gameplay alignment while remaining simple enough for a solo developer to implement and reason about.
