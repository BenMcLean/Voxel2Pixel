# GPU Sparse Voxel Octree (GpuSvoModel) Format

This document specifies **exactly** how `GpuSvoModel` bakes a voxel model into a GPU‑friendly Sparse Voxel Octree (SVO), suitable for traversal in a shader. The goal is to remove all ambiguity so that an independent implementation (CPU or GPU) can reproduce identical behavior.

The description below is derived directly from `GpuSvoModel.cs` and should be treated as normative.

---

## 1. Coordinate System and Input Assumptions

* **Coordinate space**: right‑handed, **Z‑up**
* **Voxel coordinates**: `(x, y, z)` are **unsigned 16‑bit integers** (`ushort`)
	* **Voxel coordinate values** are limited to 16-bit unsigned integers. Derived quantities such as dimensions, counts, depths, and offsets are not constrained to 16-bit and may use wider integer types.
* **Voxel values**:
  * `0` → empty
  * `1–255` → material index
* **Model bounds**:
  * All voxels satisfy:
	* `0 ≤ x < SizeX`
	* `0 ≤ y < SizeY`
	* `0 ≤ z < SizeZ`
  * The model’s minimum corner is always aligned to `(0,0,0)`

This coordinate system conforms to the Magicavoxel (.vox) standard.
All voxel data is assumed to be authored and baked in Z-up space.

If the host engine uses a different up-axis (e.g., Y-up),
conversion MUST occur outside the GpuSvoModel baker.

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

The bit at position (MaxDepth - 1) determines the octant for the Root node (Depth 0). Traversal proceeds by shifting right toward the Least Significant Bit.

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

Where:
* x → local +X (east)
* y → local +Y (north)
* z → local +Z (up)

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

Payloads can also be stored as RG32_UINT, where the shader reads a uvec2 and bit-shifts to reconstruct the material indices. This increases compatibility significantly without changing the "bit-exact" requirement.

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

### 4.2 Packing rule

Packing rule (canonical, little-endian):

`payload_u64 = Σ_{i=0..7} (uint64(material[i]) << (i*8)).`

I.e.

- octant 0 → least-significant byte
- octant 7 → most-significant byte.

If using uvec2 (two u32s):

```
low = uint(payload_u64 & 0xFFFFFFFF);
high = uint(payload_u64 >> 32).
```

When reading in shader reconstruct:

```
uvec2 p = payloads[payloadIndex];
uint64_p_low = p.x;
uint64_p_high = p.y;
```

To extract material: `material = ((uint64)phigh << 32 | p_low ) >> (octant8) & 0xFF` — or simpler, if you only need one byte, choose:

```
if (octant < 4)
	byte = (p.x >> (octant8)) & 0xFF
else
	byte = (p.y >> ((octant-4)*8)) & 0xFF.
```

Uniform leaf material stored in node low bits (bits 7..0) is the same 8-bit material index.

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

Traversal coordinates (x, y, z) are 16-bit voxel indices in canonical Z-up voxel space promoted to wider integer types for traversal arithmetic.

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

## 10. GpuSvoModel Output Contract (Model-Local)

`GpuSvoModel` is responsible **only** for baking a *single voxel model*
into a GPU-traversable Sparse Voxel Octree.

It does **not** define GPU texture layout, inter-model packing, or resource
management.

The output of `GpuSvoModel` is a *model-local SVO description* that may be
embedded into larger GPU resources by a separate system.

### 10.1 Required Outputs

A completed `GpuSvoModel` bake produces the following logical outputs:

* `uint[] Nodes`
* `ulong[] Payloads`
* `uint MaxDepth`
* `uint3 VoxelDimensions`
* `uint RootSize` — length of one edge of the cubic root volume

Where:

* `Nodes` and `Payloads` are indexed **starting at zero**
* Index `0` in `Nodes` is always the root node
* All internal child indices are **relative to this local array**
* No offsets are applied at this stage

### 10.2 Root Volume Definition

The SVO root represents a cube of size:

`RootSize = 2 ^ MaxDepth`

This cube is aligned to (0,0,0) in canonical Z-up voxel space.

Voxel coordinates outside VoxelDimensions are implicitly empty.

---

## 11. Multi-Model Packing (Out of Scope for GpuSvoModel)

`GpuSvoModel` does **not** define how multiple voxel models are stored
together in GPU memory.

When multiple models are used simultaneously, their SVO data **must**
be packed into shared GPU buffers or textures by a higher-level system.

That system is referred to normatively as **GpuSvoModelTexture**.

`GpuSvoModelTexture` consumes one or more completed `GpuSvoModel` outputs
and produces GPU-ready byte streams and per-model descriptors.

This separation is intentional and required.

---

## 12. Atlas-Safe Indexing Guarantees

To allow multiple `GpuSvoModel` instances to coexist in shared GPU memory,
the following guarantees are provided by this specification:

1. All node indices stored in `Nodes[]` are relative to the start of that model’s `Nodes[]`
2. All payload indices stored in brick leaf nodes are relative to the start of that model’s `Payloads[]`
3. No absolute GPU addresses are ever embedded in node data
4. Node traversal logic remains valid when a constant offset is added externally

### 12.1 Offset Application Rule

When embedded into a larger buffer, traversal **must** begin at:

`nodeIndex = NodeOffset`

Where NodeOffset is supplied externally.

When resolving a brick payload:

`payloadIndex = PayloadOffset + LocalPayloadIndex`

No other modification to traversal logic is permitted.

---

## 13. GPU Texture Encoding Requirements

When SVO data is uploaded to the GPU:

* `Nodes` are 32-bit unsigned integers
* `Payloads` are two 32-bit unsigned integers per payload (uvec2) — GPU format RG32_UINT (or SSBO of uvec2). Define canonical mapping of the 64 payload bits into the two u32s (see packing rules below).
* Data **must** be tightly packed with no padding
* Endianness must match native GPU endianness

The intended GPU formats are:

| Data     | GPU Format   |
|---------:|--------------|
| Nodes    | R32_UINT     |
| Payloads | R32_UINT     |

Alternate formats may be used only if bit-exact behavior is preserved.

---

## 14. Dynamic Texture Sizing

When multiple models are packed together:

* The total number of nodes is the sum of all `Nodes.Length`
* The total number of payloads is the sum of all `Payloads.Length`

Textures may be sized dynamically to fit this data.

### 14.1 Linear Addressing Model

SVO data is conceptually addressed linearly.

If a 2D texture is used, the mapping from linear index `i` is:

```
x = i % TextureWidth
y = i / TextureWidth
```

No padding rows or per-model alignment gaps are permitted.

---

## 15. Per-Model Descriptor (Reference)

When using packed SVO data, each model instance should provide:

* `uint NodeOffset`
* `uint PayloadOffset`
* `uint MaxDepth`
* `uint3 VoxelDimensions`

These values are not stored in the SVO itself and must be supplied to traversal code externally (e.g., via uniforms or buffers).

Offsets are expressed in element units, not bytes.

---

## 16. Guarantees for GPU Implementation

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

## 17. Notes and Design Rationale

* 2×2×2 bricks minimize tree depth while keeping payloads cache‑friendly
* Morton ordering ensures spatial locality
* Popcount‑based child indexing removes the need for 8 pointers per node
* Uniform leaf collapsing significantly reduces memory and traversal cost

---

## 18. Summary

`GpuSvoModel` encodes a **compact, GPU‑friendly sparse voxel octree** with:

* Byte‑addressable materials
* 16‑bit coordinate support
* Explicit, deterministic binary layout
* Minimal memory fetches during traversal

This document can be used as the authoritative reference when implementing GPU shaders or alternative builders.
