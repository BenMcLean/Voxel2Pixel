# GPU Sparse Voxel Octree (GpuSvoModel) Format

This document specifies **exactly** how `GpuSvoModel` bakes a voxel model into a GPU‑friendly Sparse Voxel Octree (SVO), suitable for traversal in a shader. The goal is to remove all ambiguity so that an independent implementation (CPU or GPU) can reproduce identical behavior.

The description below is derived directly from `GpuSvoModel.cs` and should be treated as normative.

---

## 1. Coordinate System and Input Assumptions

* **Coordinate space**: right‑handed, **Y‑up**
* **Voxel coordinates**: `(x, y, z)` are **unsigned 16‑bit integers** (`ushort`)
* **Voxel values**:
  * `0` → empty
  * `1–255` → material index
* **Model bounds**:
  * All voxels satisfy:
    * `0 ≤ x < SizeX`
    * `0 ≤ y < SizeY`
    * `0 ≤ z < SizeZ`
  * The model’s minimum corner is always aligned to `(0,0,0)`

---

## 2. Octree Dimensions and Depth

The SVO is built over a **power‑of‑two cubic volume** large enough to contain the model.

```text
maxDim = max(SizeX, SizeY, SizeZ)
MaxDepth = ceil(log2(maxDim))
```

* The root node represents a cube of size `2^MaxDepth` in all dimensions.
* Depth `0` is the root.
* The deepest internal traversal stops at depth `MaxDepth - 1`.
* **Leaves represent 2×2×2 voxel bricks**.

This guarantees that the final leaf resolution is exactly **one voxel per brick cell**.

---

## 3. Octant and Morton Ordering

All child indexing follows **Morton (Z‑order) convention**:

```text
octant = (z_bit << 2) | (y_bit << 1) | x_bit
```

Where each bit is extracted from the voxel coordinate at the current depth.

Octant index meaning:

| Index | x | y | z |
|------:|:-:|:-:|:-:|
| 0 | 0 | 0 | 0 |
| 1 | 1 | 0 | 0 |
| 2 | 0 | 1 | 0 |
| 3 | 1 | 1 | 0 |
| 4 | 0 | 0 | 1 |
| 5 | 1 | 0 | 1 |
| 6 | 0 | 1 | 1 |
| 7 | 1 | 1 | 1 |

This ordering is used consistently:
* During voxel insertion
* During tree flattening
* During traversal (`PopCount` prefix sum)

---

## 4. Build‑Time Tree Structure (CPU‑Only)

During construction, nodes are represented as:

* `Children[8]` – nullable references
* `IsLeaf` – marks a brick node
* `Payload` – 64‑bit value storing **8 materials × 8 bits**

### 4.1 Voxel Insertion

For each non‑zero voxel:

1. Traverse from depth `0` to `MaxDepth - 2`
2. Select child octant using the corresponding bit of `(x,y,z)`
3. Create nodes as needed
4. At depth `MaxDepth - 1`, mark node as a **leaf**
5. Store material in the brick payload:

```text
payload[octant] = material
```

Multiple voxels may contribute to the same leaf brick.

---

## 5. Tree Pruning (Homogenization)

After insertion, the tree is recursively pruned.

### 5.1 Leaf Homogeneity

A leaf is considered *homogeneous* if all 8 brick entries contain the same material.

### 5.2 Internal Node Collapse

An internal node is collapsed into a leaf if:

* All **8 children exist**
* All children are homogeneous leaves
* All children share the **same material**

The node becomes a leaf whose payload is replicated across all 8 slots.

This pruning dramatically reduces node count and GPU traversal cost.

---

## 6. Flattened GPU Representation

The final structure consists of two arrays:

```csharp
uint[]  Nodes;
ulong[] Payloads;
```

Index `0` is always the root node.

---

## 7. Node Encoding (32‑bit)

Each entry in `Nodes[]` is a **packed 32‑bit word**.

### 7.1 Bit Layout Overview

```text
31        30        29 ........ 8        7 ........ 0
+---------+---------+-------------------+-------------+
| Internal| LeafType|  Index / ChildBase|    Mask     |
+---------+---------+-------------------+-------------+
```

---

## 8. Node Types

### 8.1 Empty Node

```text
0x00000000
```

Represents a completely empty region.

---

### 8.2 Uniform Leaf

```text
[0 | 0 | unused | material]
```

* Bit 31 = 0 → leaf
* Bit 30 = 0 → uniform
* Bits 7–0 = material index

All voxels covered by this node have the same material.

---

### 8.3 Brick Leaf

```text
[0 | 1 | payloadIndex]
```

* Bit 31 = 0 → leaf
* Bit 30 = 1 → brick
* Bits 29–0 = index into `Payloads[]`

Each payload entry is:

```text
8 × 8‑bit materials, one per octant
```

---

### 8.4 Internal Node

```text
[1 | childBase | childMask]
```

* Bit 31 = 1 → internal
* Bits 30–8 = `childBase` (index into `Nodes[]`)
* Bits 7–0 = `childMask`

`childMask` bit `i` is set if child octant `i` exists.

Children are packed contiguously in Morton order.

Child index computation:

```text
childIndex = childBase + popcount(mask & ((1 << octant) - 1))
```

This avoids storing null pointers and enables compact traversal.

---

## 9. Traversal Algorithm (Shader‑Ready)

Given `(x,y,z)`:

1. Start at `nodeIndex = 0`
2. For depth = `0` to `MaxDepth - 2`:
   * Load node
   * If leaf → resolve voxel immediately
   * Compute octant from `(x,y,z)` bit at this depth
   * If mask bit not set → voxel is empty
   * Else compute child index via prefix popcount
3. At final level, resolve leaf voxel from uniform or brick payload

This traversal requires:

* 1 node fetch per level
* 1 payload fetch only for brick leaves
* No pointer chasing
* No recursion

---

## 10. Guarantees for GPU Implementation

* Deterministic layout
* No dynamic allocation
* No recursion required
* Single‑word node reads
* Brick payloads are tightly packed

The structure is **directly suitable** for:

* GLSL / HLSL compute shaders
* Raymarching
* Cone tracing
* Voxel rasterization

---

## 11. Notes and Design Rationale

* 2×2×2 bricks minimize tree depth while keeping payloads cache‑friendly
* Morton ordering ensures spatial locality
* Popcount‑based child indexing removes the need for 8 pointers per node
* Uniform leaf collapsing significantly reduces memory and traversal cost

---

## 12. Summary

`GpuSvoModel` encodes a **compact, GPU‑friendly sparse voxel octree** with:

* Byte‑addressable materials
* 16‑bit coordinate support
* Explicit, deterministic binary layout
* Minimal memory fetches during traversal

This document can be used as the authoritative reference when implementing GPU shaders or alternative builders.

