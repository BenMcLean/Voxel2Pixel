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

A semantic description of how a tile's top surface behaves geometrically (flat, ramp, etc.).

### Corner Heights

Each tile has 4 corners (NW, NE, SE, SW). Corner heights are computed from the tile's base height plus adjustments based on neighboring tile heights. This ensures seamless connections between adjacent tiles.

### Marching Squares (Adapted)

A local neighborhood classification technique where each tile selects a surface type based on the relative heights of its neighbors.

---

## 3. Input Data

```csharp
byte[,] heightMap;
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

    // === One Edge Modified (Cardinal Directions) ===
    // Edge slopes down toward that direction (neighbor is lower)
    SlopeDownN, SlopeDownE, SlopeDownS, SlopeDownW,
    // Edge slopes up toward that direction (neighbor is higher)
    SlopeUpN, SlopeUpE, SlopeUpS, SlopeUpW,

    // === One Corner Modified (Diagonal Directions) ===
    // Corner lowered (two adjacent neighbors are lower)
    CornerDownNE, CornerDownSE, CornerDownSW, CornerDownNW,
    // Corner raised (two adjacent neighbors are higher)
    CornerUpNE, CornerUpSE, CornerUpSW, CornerUpNW,

    // === Two Opposite Edges Modified (Tilt) ===
    // One edge up, opposite edge down
    TiltNS_NorthUp,   // North edge raised, South edge lowered
    TiltNS_SouthUp,   // South edge raised, North edge lowered
    TiltEW_EastUp,    // East edge raised, West edge lowered
    TiltEW_WestUp,    // West edge raised, East edge lowered

    // === Saddle (Opposite Corners Modified) ===
    // Diagonal corners adjusted in opposite directions
    SaddleNE_SW_NEDown,  // NE corner down, SW corner up
    SaddleNE_SW_NEUp,    // NE corner up, SW corner down
    SaddleNW_SE_NWDown,  // NW corner down, SE corner up
    SaddleNW_SE_NWUp,    // NW corner up, SE corner down

    // === Three Edges Modified (Valley/Ridge) ===
    // Three neighbors different, forming a valley toward one corner
    ValleyNE, ValleySE, ValleySW, ValleyNW,  // Valley (3 down)
    RidgeNE, RidgeSE, RidgeSW, RidgeNW,      // Ridge (3 up)

    // === All Four Edges Modified ===
    Peak,  // All 4 neighbors lower - center is local maximum
    Pit,   // All 4 neighbors higher - center is local minimum
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
* Covers **all possible neighbor configurations** with no holes

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

Note: since height comes in as bytes, underflow must be prevented.

---

### Height Difference Categories

Each neighbor difference is categorized:

```
cliff_down:  dX <= -2   (generates vertical face, no slope)
slope_down:  dX == -1   (smooth slope down toward neighbor)
level:       dX == 0    (same height)
slope_up:    dX == +1   (smooth slope up toward neighbor)
cliff_up:    dX >= +2   (neighbor generates vertical face toward us)
```

---

### Classification Rules

Classification is based on counting neighbors in each category:

```
countDown = count of neighbors where dX == -1
countUp   = count of neighbors where dX == +1
```

Cliff neighbors (|dX| >= 2) are treated as level for classification purposes; vertical faces handle the height difference.

---

#### Rule 1 — All Level (Flat)

If all neighbors are level or cliffs:

```
TileSurfaceType = Flat
```

---

#### Rule 2 — Single Edge Slope

If exactly one neighbor is ±1 and all others are level or cliffs:

**Down variants** (one neighbor at -1):
- dN == -1: `SlopeDownN`
- dE == -1: `SlopeDownE`
- dS == -1: `SlopeDownS`
- dW == -1: `SlopeDownW`

**Up variants** (one neighbor at +1):
- dN == +1: `SlopeUpN`
- dE == +1: `SlopeUpE`
- dS == +1: `SlopeUpS`
- dW == +1: `SlopeUpW`

---

#### Rule 3 — Corner (Two Adjacent Neighbors Same Direction)

If exactly two **orthogonal** (adjacent) neighbors are both -1 or both +1:

**Down variants** (two adjacent at -1):
- N and E: `CornerDownNE`
- S and E: `CornerDownSE`
- S and W: `CornerDownSW`
- N and W: `CornerDownNW`

**Up variants** (two adjacent at +1):
- N and E: `CornerUpNE`
- S and E: `CornerUpSE`
- S and W: `CornerUpSW`
- N and W: `CornerUpNW`

---

#### Rule 4 — Tilt (Two Opposite Neighbors, Opposite Directions)

If two **opposite** neighbors are different by ±1 in opposite directions:

- dN == +1, dS == -1: `TiltNS_NorthUp`
- dN == -1, dS == +1: `TiltNS_SouthUp`
- dE == +1, dW == -1: `TiltEW_EastUp`
- dE == -1, dW == +1: `TiltEW_WestUp`

---

#### Rule 5 — Saddle (Two Adjacent Neighbors, Opposite Directions)

If two **adjacent** neighbors are different by ±1 in opposite directions:

- dN == -1, dE == +1 (or dE == -1, dN == +1): Saddle NE-SW axis
- dS == -1, dW == +1 (or dW == -1, dS == +1): Saddle NE-SW axis
- dN == -1, dW == +1 (or dW == -1, dN == +1): Saddle NW-SE axis
- dS == -1, dE == +1 (or dE == -1, dS == +1): Saddle NW-SE axis

The specific variant depends on which corner is lowered vs raised.

---

#### Rule 6 — Valley/Ridge (Three Neighbors Same Direction)

If exactly three neighbors are -1 (valley) or +1 (ridge):

**Valley** (3 at -1, 1 at 0 or cliff):
- Not N: `ValleyNE` (valley toward NE direction)
- Not E: `ValleySE`
- Not S: `ValleySW`
- Not W: `ValleyNW`

**Ridge** (3 at +1, 1 at 0 or cliff):
- Not N: `RidgeNE`
- Not E: `RidgeSE`
- Not S: `RidgeSW`
- Not W: `RidgeNW`

---

#### Rule 7 — Peak/Pit (All Four Neighbors Same Direction)

If all four neighbors are -1:
```
TileSurfaceType = Peak
```

If all four neighbors are +1:
```
TileSurfaceType = Pit
```

---

#### Rule 8 — Complex Cases (Fallback)

For any configuration not covered above (e.g., two opposite neighbors both down):

```
TileSurfaceType = Flat
```

Note: These cases are geometrically ambiguous. Flat with vertical faces provides acceptable results.

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

## 7. Corner Height Computation

Each tile corner's height is computed based on the tile's base height and the surface type:

| Corner | Affected by edges | Lowered when | Raised when |
|--------|------------------|--------------|-------------|
| NW | N, W | N or W slopes down | N or W slopes up |
| NE | N, E | N or E slopes down | N or E slopes up |
| SE | S, E | S or E slopes down | S or E slopes up |
| SW | S, W | S or W slopes down | S or W slopes up |

Corner offset = ±1 height step (or 0 for level edges).

---

## 8. Cliff Face Generation

For **any** tile edge where:

```
neighbor_height < current_corner_height
```

A vertical face is generated to cover the height difference.

Cliff faces:
* Are generated for ALL tile types, not just Flat
* Bridge the gap between adjacent tiles of different heights
* Are computed per-corner to handle sloped edges correctly
* May be trapezoidal when one end of the edge is higher than the other

---

## 9. Geometry Generation Rules

### Top Surfaces

Each surface type maps to a specific quad or triangle configuration:

* **Flat**: Single quad, all corners at base height
* **Edge slopes**: Quad with one edge raised/lowered by 1
* **Corner types**: Quad with one corner raised/lowered by 1
* **Tilt**: Quad with two opposite corners raised, two lowered
* **Saddle**: Two triangles meeting at the saddle point
* **Valley/Ridge**: Quad with three corners modified
* **Peak/Pit**: Quad with all four corners modified (pyramid tip)

---

### Cliff Faces

For each tile edge where there is a height difference:

Generate a vertical (or sloped) face connecting:
* This tile's edge corners at their computed heights
* The neighboring tile's edge corners at their computed heights

This ensures **no holes** regardless of tile surface types.

---

## 10. Public API Structure

### Tile Classification

```csharp
// heightMap includes a one-tile apron on all sides
public static TileSurfaceType[,] ClassifyTiles(byte[,] heightMap);
```

---

### Mesh Synthesis (Per Chunk)

```csharp
public static ArrayMesh BuildChunkMesh(
    byte[,] heightMap,
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

## 11. Properties of This Design

* **Complete coverage**: All neighbor configurations produce valid geometry
* **No holes**: Every height transition is covered by slopes or vertical faces
* **Seamless connections**: Corner heights are computed consistently across tiles
* **Tile semantics are explicit and reusable**
* **Mesh generation is deterministic and chunk-safe**
* **Continuous surfaces are naturally merged**
* **Rendering and gameplay concerns are decoupled**

---

## 12. Summary

This system implements a **comprehensive marching-squares–style tile classification** to convert a heightfield into explicit terrain semantics, followed by a **corner-based mesh synthesis** that produces clean, efficient 3D geometry with no gaps or holes.

The classification handles all combinations of neighbor height differences:
* Single-edge slopes (up and down)
* Corner adjustments (up and down)
* Tilts (opposite edges)
* Saddles (opposite corners)
* Valleys and ridges (three edges)
* Peaks and pits (all edges)

By computing corner heights consistently and generating appropriate vertical faces for all cliff edges, the system guarantees watertight terrain meshes.
