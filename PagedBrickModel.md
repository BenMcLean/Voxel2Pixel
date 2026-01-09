### **I. Program Objective**

To build a **C\# Voxel Engine Core** that enables the editing and rendering of a massive $65,536^3$ coordinate space using a fixed, low-memory footprint. The system must emulate virtual memory: keeping the full dataset on the "disk" (RAM) while only loading the active viewing area into the "cache" (GPU VRAM).

---

### **II. Core Technical Requirements**

* **Coordinate Space:** 16-bit Unsigned Integer ($0$ to $65,535$).  
* **Coordinate System:** Right-handed, Z-up.  
* **Voxel Unit:** 8-bit value (0 \= Empty, 1–255 \= Material).  
* **Atomic Data Unit:** The "Brick" ($2 \\times 2 \\times 2$ voxels \= 8 bytes).  
* **Performance:** Zero memory allocation during the update loop; reliance on bit-shifting over division.  
* **Dependency:** The Core library must be Engine-Agnostic (No references to Godot/Unity).  
  ---

  ### **III. Algorithmic Strategy: The "Virtual Address"**

III. Algorithmic Strategy: The "Virtual Address"

The system utilizes a Bit-Sliced Addressing Scheme to map a world coordinate $(x, y, z)$ into a physical memory address.

* **Bits 8–15 (8 bits):** **Segment ID**. Maps to the SegmentedDictionary. (256 Segments per axis).
* **Bits 1–7 (7 bits):** **Brick Index**. Position of brick within segment. (128 Bricks per Segment dimension).
* **Bit 0 (1 bit):** **Voxel Offset**. Maps to the byte index (0–7) inside the Brick.

**Total Coordinate Reach:** 256 segments times 128 bricks times 2 voxels/brick \= 65,536 voxels per axis.

---

### **IV. Class-Level Specification**

#### **A. Core Data Structures (Engine Agnostic)**

These classes manage the raw data and memory virtualization.

**1\. BrickPool (The Physical Memory)**

* **Purpose:** Emulates VRAM in system RAM using a pre-allocated flat array.  
* **Data Structure:**  
  * byte\[\] Data: A massive flat array storing bricks.  
  * Stack\<int\> FreeSlots: Tracks reusable Slot IDs to ensure zero-allocation.  
* **Key Behavior:**  
  * Stores data in **RG8 format** (Interleaved). Every 2 bytes represent a vertical pair of voxels ($Z=0$ and $Z=1$) to align with GPU texture formats.  
  * **Capacity:** Fixed size (Window) defining the maximum "active" geometry.

**2\. BrickTable (The Address Translator)**

* **Purpose:** Maps World Coordinates to BrickPool Slot IDs.
* **Data Structure:**
  * Dictionary\<ulong, uint\[\]\>: A sparse map where the key is the Segment ID and the value is a dense $128^3$ array of Slot IDs.
* **Key Behavior:**
  * **Lookup:** Takes the brick coordinates (bits 1-7 of x, y, z) to find the Slot ID.
  * **Output:** Effectively generates the data required for an R32\_UI texture lookup on the GPU.
  * **Slot Mapping:** Value 0 = unmapped (brick not in cache). Value N (N ≥ 1) maps to BrickPool slot (N-1).

**3\. SegmentedDictionary (The Persistent Store)**

* **Purpose:** The "Hard Drive." Holds the entire universe, including parts not currently visible.
* **Data Structure:**
  * Dictionary\<ulong, byte\[\]\>: Sparse storage where each key uniquely identifies one brick in the world, and each value is 8 bytes.
  * **Key Format:** Packed (segmentId, brickIndex) - See Section 1.1 for packing formula.
* **Key Behavior:**
  * **Sparse:** Only stores non-empty bricks. Empty/air bricks are omitted entirely.
  * Handles "brick faults" by retrieving data when the BrickTable requests a brick that isn't currently cached.
  * On eviction, receives dirty bricks written back from BrickPool.

  ---

  #### **B. The Controller (Engine Agnostic)**

**4\. PagedBrickModel (The Orchestrator)**

* **Purpose:** The public API for the voxel system. It coordinates the "Read/Write" cycle.
* **Methods:**
  * SetVoxel(x, y, z, value):
    1. **Check Cache:** Queries BrickTable for a Slot ID.
    2. **Miss (Brick Fault):** If value is 0 (unmapped), fetch 8 bytes from SegmentedDictionary (or create empty brick if not found), allocate a new Slot ID from BrickPool, and update BrickTable.
    3. **Hit:** Write the new voxel value directly into the BrickPool byte\[\].
    4. **Sync:** Push a DirtyEvent (Slot ID \+ New Data) to the observer.
* **Events:**
  * OnBrickDirty(int slotId, byte\[\] new8Bytes): Signals that the GPU needs to update specific pixels.

  ---

  #### **C. The Integration Layer (Engine Specific)**

This class bridges the pure C\# logic with the specific game engine (e.g., Godot).

**5\. GodotVoxelBridge (The Renderer)**

* **Purpose:** Listens to the Core and updates the GPU Texture.  
* **Dependencies:** Reference to PagedBrickModel and the Engine's Rendering Server.  
* **Key Behavior:**  
  * **Initialization:** Creates the Texture2DArray (The Mirror) on the GPU.  
  * **Event Handling:** Subscribes to OnBrickDirty.  
  * **Texture Packing:** Converts the linear SlotID into $(u, v, w)$ texture coordinates.  
  * **Update:** Calls the engine command (e.g., RenderingServer.TextureUpdate) to replace exactly 4 pixels (8 bytes) on the GPU.

  ---

  ### **V. Success Metrics**

The implementation is successful only if:

1. **Fixed VRAM:** You can edit voxels at $(0,0,0)$ and $(65000, 65000, 65000)$ without the GPU texture growing in size.  
2. **No Lag:** The SetVoxel method generates **zero garbage collection** pressure (no new keywords in the loop).  
3. **Correctness:** A modification in C\# reflects instantly on the GPU Mirror.

# **1\. Canonical Virtual Address Mapping**

### **Design Goals**

* Deterministic

* GPU-friendly

* Easy bit extraction

* Compatible with `256³` persistent regions and `128³` cache pages

* Avoids Morton complexity unless truly needed

### **Brick Coordinates**

Each voxel coordinate `(x, y, z)` is first snapped to a brick origin:

`bx = x & ~1;`

`by = y & ~1;`

`bz = z & ~1;`

Each brick occupies a `2×2×2` region.

---

### **Brick-Space Decomposition**

Each axis is decomposed as:

| Bits | Meaning |
| ----- | ----- |
| 0 | Local voxel bit (handled separately) |
| 1–7 | Brick position within segment (0–127) |
| 8–15 | Segment coordinate (0–255) |

Thus:

`World axis (16 bits)`

`┌───────────────┬───────────────┬───────┐`

`│ Segment (8)   │ Brick (7)     │ Voxel │`

`│ bits 8–15     │ bits 1–7      │ bit 0 │`

`└───────────────┴───────────────┴───────┘`

---

### **Segment ID (Persistent Store Key)**

Segments are **128×128×128 bricks**, i.e. **256³ voxels**.

All segmentation and paging operates in **brick space**; voxel space is derived.

Segment coordinates:

`sx = x >> 8;`

`sy = y >> 8;`

`sz = z >> 8;`

Packed into a 64-bit key:

`ulong segmentId =`

    `((ulong)sx << 32) |`

    `((ulong)sy << 16) |`

    `(ulong)sz;`

This directly indexes:

`Dictionary<ulong, byte[]> SegmentedDictionary;`

Each segment contains a volume of 128x128x128 bricks.

---

### **Brick Index (Cache-Level Address)**

Each segment contains **128×128×128 bricks**.

Brick coordinates within segment:

`px = (x >> 1) & 0x7F;`

`py = (y >> 1) & 0x7F;`

`pz = (z >> 1) & 0x7F;`

These index into the BrickTable entry for that segment.

---

### **Why this mapping is correct**

* **Bitwise only** (no division/modulo)

* Segment boundaries align with large streaming regions

* Brick-level addressing aligns with GPU cache locality

* Independent X/Y/Z addressing simplifies raymarch math

* Avoids Morton cost unless later required

---

# **1.1. Brick Key Packing (SegmentedDictionary)**

### **Purpose**

The SegmentedDictionary uses a flat sparse structure where each brick in the world has a unique 64-bit key. This section defines the canonical packing format.

### **Key Composition**

A brick is uniquely identified by:
* **Segment coordinates:** `(sx, sy, sz)` - 8 bits each, derived from bits 8-15 of world coordinates
* **Brick index:** Linear index within segment - 21 bits (128³ = 2,097,152 values)

### **Packing Formula**

```csharp
// Pack segment and brick coordinates into a 64-bit key
ulong MakeBrickKey(ushort sx, ushort sy, ushort sz, int brickIndex)
{
    // Segment ID: 24 bits (8 bits per axis)
    ulong segmentId = ((ulong)sx << 16) | ((ulong)sy << 8) | (ulong)sz;

    // Pack: high 43 bits for segmentId, low 21 bits for brickIndex
    return (segmentId << 21) | (ulong)brickIndex;
}

// Convenience: Compute from world coordinates
ulong MakeBrickKey(ushort x, ushort y, ushort z)
{
    ushort sx = (ushort)(x >> 8);
    ushort sy = (ushort)(y >> 8);
    ushort sz = (ushort)(z >> 8);

    int px = (x >> 1) & 0x7F;
    int py = (y >> 1) & 0x7F;
    int pz = (z >> 1) & 0x7F;
    int brickIndex = px | (py << 7) | (pz << 14);

    return MakeBrickKey(sx, sy, sz, brickIndex);
}
```

### **Unpacking (for debugging)**

```csharp
(uint segmentId, int brickIndex) UnpackBrickKey(ulong key)
{
    int brickIndex = (int)(key & 0x1FFFFF);  // Low 21 bits
    uint segmentId = (uint)(key >> 21);       // High 43 bits (24 used)
    return (segmentId, brickIndex);
}
```

### **Usage**

```csharp
// Store brick
ulong key = MakeBrickKey(x, y, z);
segmentedDictionary[key] = brickData; // 8 bytes

// Retrieve brick
if (segmentedDictionary.TryGetValue(key, out byte[] data))
{
    // Brick exists
}
else
{
    // Brick is empty/air
}
```

---

# **2\. BrickTable Layout**

Each segment has exactly **one brick table**:

`Dictionary<ulong, uint[]> BrickTable;`

### **Brick Table Entry**

`uint[] bricks = new uint[128 * 128 * 128];`

* `0` \= unmapped (brick fault - brick not in cache)

* `>0` \= BrickPool SlotID \+ 1
   (avoids ambiguity with default zero-initialized memory)

A BrickTable value of N corresponds to BrickPool slot (N - 1). All slots from 0 to Capacity-1 are usable.

---

### **Brick Index Linearization**

Canonical layout (X-major, then Y, then Z):

`brickIndex = px + (py << 7) + (pz << 14);`

Equivalently:

`brickIndex = px | (py << 7) | (pz << 14);`

This ordering:

* Matches natural memory traversal

* Is cache-friendly on CPU

* Is trivial to replicate in GPU code if needed

---

### **GPU Compatibility Note**

This linear index can be uploaded **directly** to an `R32_UINT` texture if you ever want a GPU-side brick table.

---

# **3\. Cache Eviction & Persistence Policy**

## **BrickPool Invariants**

* BrickPool has **fixed capacity**

* Slot IDs are reused

* No allocation during updates

**Initialization Logic:**

* Upon instantiation, the `FreeSlots` stack **must be pre-populated** with all integers from `Capacity - 1` down to `0`.
* **All slots are usable:** Slot 0 through Capacity-1 are all valid cache slots. The +1 offset in BrickTable handles the zero-initialization issue.

---

## **Eviction Strategy (Normative)**

### **Policy: Explicit Region-Based Eviction**

The core does **not** autonomously evict bricks.

Instead:

* The **editor camera / tool** defines an *active region*

* Bricks outside this region are explicitly released

This matches an editor \+ raymarcher workflow:

* Predictable

* Deterministic

* No surprise stalls

* No hidden GC or heuristics

---

### **Eviction Procedure**

When a slot is evicted:

1. Read the 8-byte payload from `BrickPool`

2. Write it back to `SegmentedDictionary` using packed key (segmentId, brickIndex)

3. Clear the BrickTable entry (set to 0)

4. Return SlotID to `FreeSlots`

Dirty tracking is implicit — **all cached bricks are authoritative**.

---

### **Why not LRU?**

* LRU requires metadata updates per access

* LRU introduces branching & GC risk

* Editor tools know spatial intent better than heuristics

---

# **6\. Godot 4 GPU Raymarcher Integration**

---

## **GPU Resources**

### **Brick Data Texture (Primary)**

* **Texture2DArray**

* Format: `RG8_UNORM`

* Each layer \= one BrickPool slot

* Each brick \= **2×2 pixels**

* Each pixel \= `(voxelZ0, voxelZ1)`

Layout:

`Pixel (x,y):`

`R = voxel (x,y,z=0)`

`G = voxel (x,y,z=1)`

---

### **SlotID → Texture Coordinates**

Given `slotId`:

`layer = slotId;`

`u = localX;`

`v = localY;`

GPU-side:

`vec2 uv = (vec2(localX, localY) + 0.5) / 2.0;`

`uint voxelPair = texelFetch(brickTexture, ivec3(localX, localY, layer), 0).rg;`

---

## **GPU Addressing Flow (Raymarcher)**

For each ray step:

1. Compute `(x, y, z)` in voxel space

2. Snap to brick

3. Compute `segmentId` (or region ID)

4. Lookup brick table (CPU-uploaded or implicit)

5. Fetch SlotID

6. Fetch RG8 brick data

7. Extract voxel via bit logic

---

## **CPU → GPU Sync**

`OnBrickDirty(slotId, byte[8])` results in:

`RenderingServer.TextureUpdate(`

    `textureRid,`

    `layer: slotId,`

    `rect: new Rect2I(0, 0, 2, 2),`

    `data: new byte[8]`

`);`

Exactly **4 pixels updated** — minimal bandwidth.

## **7\. BrickPool Memory Layout & GPU Texture Packing**

### **Design Goals**

* Exact byte-for-byte agreement between:

  * CPU BrickPool storage

  * GPU Texture2DArray representation

* Minimal bandwidth for updates

* Zero per-voxel GPU indirection

* Brick-local spatial coherence

* Compatibility with `VoxelBrick` bit layout

---

## **7.1 Brick Definition (Canonical)**

A **Brick** represents a `2×2×2` voxel cube:

| Local Coordinate | Meaning |
| ----- | ----- |
| `localX` | 0 or 1 |
| `localY` | 0 or 1 |
| `localZ` | 0 or 1 |

Voxel payload is an **8-bit material ID**.

A brick therefore occupies **exactly 8 bytes**.

---

## **7.2 BrickPool CPU Memory Layout**

`BrickPool.Data` is a flat byte array:

`byte[] Data;`

Each brick occupies a **contiguous 8-byte region**:

`int baseOffset = slotId * 8;`

### **Canonical Byte Order**

The bytes are ordered as:

`byte 0: (x=0, y=0, z=0)`

`byte 1: (x=0, y=0, z=1)`

`byte 2: (x=1, y=0, z=0)`

`byte 3: (x=1, y=0, z=1)`

`byte 4: (x=0, y=1, z=0)`

`byte 5: (x=0, y=1, z=1)`

`byte 6: (x=1, y=1, z=0)`

`byte 7: (x=1, y=1, z=1)`

Or equivalently:

`int byteIndex =`

    `((localY << 1) | localX) * 2 + localZ;`

This mapping is **isomorphic** to the bit layout used by `VoxelBrick`.

---

## **7.3 BrickPool ↔ VoxelBrick Equivalence**

A brick stored in `BrickPool` is bitwise equivalent to a `VoxelBrick.Payload`:

`ulong payload =`

    `(ulong)Data[offset + 0] << 0  |`

    `(ulong)Data[offset + 1] << 8  |`

    `(ulong)Data[offset + 2] << 16 |`

    `(ulong)Data[offset + 3] << 24 |`

    `(ulong)Data[offset + 4] << 32 |`

    `(ulong)Data[offset + 5] << 40 |`

    `(ulong)Data[offset + 6] << 48 |`

    `(ulong)Data[offset + 7] << 56;`

This guarantees:

* CPU editing logic

* Serialization logic

* GPU texture updates

all observe **exactly the same voxel ordering**.

---

## **7.4 GPU Texture Layout**

### **Texture Type**

* `Texture2DArray`

* Format: `RG8_UNORM`

* Dimensions: `2 × 2 × BrickPoolCapacity`

---

### **Brick → Texture Mapping**

Each **BrickPool slot** maps to **one texture layer**.

Within that layer:

| Texture Pixel | RG Channels | Meaning |
| ----- | ----- | ----- |
| `(0,0)` | R \= `(0,0,0)` G \= `(0,0,1)` |  |
| `(1,0)` | R \= `(1,0,0)` G \= `(1,0,1)` |  |
| `(0,1)` | R \= `(0,1,0)` G \= `(0,1,1)` |  |
| `(1,1)` | R \= `(1,1,0)` G \= `(1,1,1)` |  |

This corresponds exactly to the CPU byte order:

`pixelX = localX;`

`pixelY = localY;`

`channel = localZ; // R = 0, G = 1`

---

## **7.5 GPU Access Formula (Raymarcher)**

Given:

* `slotId`

* `localX, localY, localZ`

GPU fetch:

`uvec2 voxelPair =`

    `texelFetch(brickTexture, ivec3(localX, localY, slotId), 0).rg;`

`uint voxel =`

    `(localZ == 0) ? voxelPair.r : voxelPair.g;`

This requires:

* One texture fetch

* One conditional select

* No bit shifts on GPU

---

## **7.6 Brick Update Invariant**

When `OnBrickDirty(slotId, byte[8])` is raised:

* Exactly **4 pixels** are updated

* Exactly **8 bytes** are transferred

* No format conversion is required

This guarantees:

* Minimal bandwidth

* Stable performance

* No CPU-side swizzling

# 8\. Compatibility

To remain capable of effective serialization and deserialization, PagedBrickModel must implement IBrickModel:

/// \<summary\>

/// The VoxelBrick enumeration is sparse: it includes only all the non-empty bricks in an undefined order.

/// \</summary\>

public interface IBrickModel : IModel, IEnumerable\<VoxelBrick\>

{

	// Default implementation of the IModel byte-access

	// This makes any IBrickModel automatically compliant with IModel\!

	// byte IModel.this\[ushort x, ushort y, ushort z\] \=\> VoxelBrick.GetVoxel(GetBrick(x, y, z), x & 1, y & 1, z & 1);

	/// \<summary\>

	/// Implementation should snap x,y,z to the nearest multiple of 2

	/// internally using: x & ~1, y & ~1, z & ~1

	/// \</summary\>

	ulong GetBrick(ushort x, ushort y, ushort z);

}

public readonly record struct VoxelBrick(ushort X, ushort Y, ushort Z, ulong Payload)

{

	/// \<summary\>

	/// Extracts a single voxel byte from the 2x2x2 payload.

	/// localX, localY, localZ must be 0 or 1\.

	/// \</summary\>

	public byte GetVoxel(int localX, int localY, int localZ) \=\> GetVoxel(Payload, localX, localY, localZ);

	public static byte GetVoxel(ulong payload, int localX, int localY, int localZ) \=\>

		(byte)((payload \>\> ((((localZ & 1\) \<\< 2\) | ((localY & 1\) \<\< 1\) | (localX & 1)) \<\< 3)) & 0xFF);

	/// \<summary\>

	/// A helper to "edit" a brick by returning a new one with one voxel changed

	/// localX, localY, localZ must be 0 or 1\.

	/// \</summary\>

	public VoxelBrick WithVoxel(int localX, int localY, int localZ, byte material)

	{

		int shift \= ((localZ \<\< 2\) | (localY \<\< 1\) | localX) \<\< 3;

		ulong mask \= 0xFFUL \<\< shift,

			newPayload \= (Payload & \~mask) | ((ulong)material \<\< shift);

		return this with { Payload \= newPayload };

	}

}

/// \<summary\>

/// The Voxel enumeration is sparse: it includes only all the non-zero voxels in an undefined order.

/// \</summary\>

public interface IModel : IEnumerable\<Voxel\>, IEnumerable

{

	byte this\[ushort x, ushort y, ushort z\] { get; }

	ushort SizeX { get; }

	ushort SizeY { get; }

	ushort SizeZ { get; }

}

public readonly record struct Voxel(ushort X, ushort Y, ushort Z, byte Material)

{

	public static implicit operator Point3D(Voxel voxel) \=\> new()

	{

		X \= voxel.X,

		Y \= voxel.Y,

		Z \= voxel.Z,

	};

}

# 9\. Explicit Non-Goals

The `PagedBrickModel` data structure is intentionally **narrow in scope**. It exists to solve one specific problem: **low-latency, mutable voxel editing over a massive address space with fixed GPU memory usage**.

The following features are **explicitly out of scope** for this structure and are handled by other specialized data structures in the ecosystem:

* **Sparse Voxel Octrees (SVOs)**  
   This system does not implement hierarchical voxel storage, pointer-based trees, or multi-resolution traversal. Separate SVO implementations are used for:

  * Lossless compressed storage and network transmission

  * Read-only, Morton-ordered GPU raymarching in runtime/game contexts

* **Automatic Cache Eviction or Replacement Policies**  
   The core does not perform LRU, LFU, or heuristic-driven eviction. All cache residency decisions are driven explicitly by editor tools and camera-defined active regions.

* **GPU-Driven Page Faulting or Streaming**  
   Page faults are handled entirely on the CPU. The GPU is treated as a consumer of already-resident brick data, not as a participant in memory management.

* **Compression or Entropy Encoding**  
   Brick data in this structure is stored uncompressed to guarantee constant-time random access and minimal update latency.

* **Runtime Rendering Optimization**  
   This structure is not intended to be used directly in shipping game runtimes. Its purpose is to support *authoring*, not final-frame performance. Runtime rendering is handled by a separate, baked representation.

* **Spatial Ordering Guarantees**  
   Neither `SlotID` assignment nor brick enumeration order has spatial meaning. Any spatial ordering (including Morton/Z-order) is delegated to downstream conversion steps.

By excluding these concerns, `PagedBrickModel` remains:

* Deterministic

* Editor-friendly

* Easy to reason about

* Compatible with multiple downstream voxel representations

# 10\. Implementation Plan & Validation Stages

The implementation of `PagedBrickModel` should proceed in **clearly defined stages**, each producing a runnable, testable artifact. This reduces risk and ensures correctness before GPU integration.

---

### **Stage 1: Core Brick Primitives (CPU-Only)**

**Goal:** Establish byte-exact correctness of brick representation.

Deliverables:

* `VoxelBrick` implementation

* Brick byte ↔ payload conversion utilities

* Unit tests verifying:

  * Correct voxel extraction

  * Correct voxel mutation

  * Byte-order invariants

Validation:

* Given a brick payload, all 8 voxel values round-trip correctly

* CPU byte layout matches spec exactly

---

### **Stage 2: BrickPool Implementation**

**Goal:** Implement fixed-capacity physical storage with zero allocations during updates.

Deliverables:

* `BrickPool`

  * Flat `byte[] Data`

  * Slot allocation and reuse

* Unit tests verifying:

  * Slot reuse correctness

  * No memory allocation during steady-state edits

  * Correct offset math (`slotId * 8`)

Validation:

* Brick writes and reads are byte-exact

* Capacity limits are enforced deterministically

---

### **Stage 3: SegmentedDictionary (Persistent Store)**

**Goal:** Implement sparse, infinite backing storage.

Deliverables:

* `SegmentedDictionary`

  * `Dictionary<ulong, byte[]>`

  * Lazy segment creation

* Unit tests verifying:

  * Segment key correctness

  * Brick read/write persistence across eviction

Validation:

* Evicted bricks reappear with identical payloads when reloaded

---

### **Stage 4: BrickTable Implementation**

**Goal:** Implement deterministic address translation.

Deliverables:

* `BrickTable`

  * `Dictionary<ulong, uint[]>`

  * Brick index computation

* Unit tests verifying:

  * Correct mapping from `(x,y,z)` → brick index

  * Correct slot lookup behavior

  * Brick fault detection (value = 0)

Validation:

* BrickTable mapping matches spec for known coordinates

* SlotID \+ 1 invariant holds (value N maps to slot N-1)

---

### **Stage 5: PagedBrickModel Orchestration**

**Goal:** Integrate all core components into a usable editing API.

Deliverables:

* `PagedBrickModel`

  * `SetVoxel`

  * `GetBrick`

  * `IBrickModel` implementation

* Event dispatch for `OnBrickDirty`

Validation:

* Editing voxels updates the correct brick

* Brick faults correctly allocate and populate bricks

* No allocations during edit loop

---

### **Stage 6: Enumeration & Serialization Validation**

**Goal:** Ensure compatibility with downstream voxel representations.

Deliverables:

* `IEnumerable<VoxelBrick>` implementation

* Serialization test harness

Validation:

* Enumeration yields only non-empty bricks

* Enumeration order is undefined but stable per snapshot

* Data round-trips into other `IBrickModel` implementations

---

### **Stage 7: Godot 4 GPU Integration**

**Goal:** Validate CPU ↔ GPU correctness.

Deliverables:

* `GodotVoxelBridge`

* Texture2DArray allocation

* Brick upload logic

Validation:

* Single brick edits appear immediately on GPU

* Only 4 pixels are updated per brick change

* Texture memory size remains constant

---

### **Stage 8: GPU Raymarcher Demo**

**Goal:** End-to-end proof of concept.

Deliverables:

* Simple Godot 4 scene

* Camera-driven active region management

* GPU raymarch shader consuming brick texture

Validation:

* Interactive voxel editing

* Large coordinate jumps without GPU memory growth

* Stable frame rate under typical editor usage

---

# 11. Hardware Considerations & Scalability

### **Design Philosophy**

The `PagedBrickModel` is designed to scale gracefully from mobile VR headsets to high-end desktop workstations without imposing artificial limits in the data structure itself. The system will naturally hit hardware boundaries based on available memory and GPU capabilities, allowing users to create models as large as their hardware can support.

---

### **Memory Scaling Characteristics**

#### **BrickTable Memory (CPU RAM)**

Each active segment requires a BrickTable:
* **Size per segment:** 128³ entries × 4 bytes = **8 MB**
* **Scaling:** Linear with number of active segments
* **Typical usage:**
  * Small models (< 256³ voxels): 1 segment = 8 MB
  * Medium models (256³ to 512³ voxels): 4-8 segments = 32-64 MB
  * Large models (> 1024³ voxels): Dozens of segments = hundreds of MB

The system will naturally limit itself based on available RAM. Users working with extremely large models will need proportionally more system memory.

#### **BrickPool Memory (GPU VRAM)**

The BrickPool is a fixed-capacity cache of active bricks:
* **Size per brick:** 8 bytes
* **Typical configurations:**
  * Mobile VR (Quest 3): 128-256 MB → 16M-32M bricks → ~512³ voxel editing window
  * Desktop GPU: 1-4 GB → 128M-512M bricks → ~1024³-2048³ voxel editing window

The BrickPool size is configurable at initialization and determines the maximum "active" editing region before eviction is required.

---

### **GPU Texture Implementation Strategies**

The specification describes a Texture2DArray approach, but implementations may choose alternatives based on hardware:

#### **Option 1: Texture2DArray (Reference Implementation)**
* Format: RG8, 2×2 pixels per layer
* **Limitation:** Max layers typically 2048-16384 depending on GPU
* **Suitable for:** Smaller editing windows, prototyping
* **Mobile compatibility:** Good (native format support)

#### **Option 2: Texture3D (High-Capacity Alternative)**
* Format: R8, one voxel per texel
* **Advantages:**
  * No layer limit
  * Better cache coherence for raymarching
  * Can pack bricks in 3D space efficiently
* **Suitable for:** Large editing volumes, production use
* **Mobile compatibility:** Excellent

#### **Option 3: Compute Buffer (Maximum Flexibility)**
* Format: Raw SSBO/UBO
* **Advantages:**
  * No size limits beyond VRAM
  * Direct memory mapping possible
  * Maximum control over layout
* **Suitable for:** High-end implementations, research
* **Mobile compatibility:** Good (Vulkan/Metal)

---

### **Mobile VR Considerations (Meta Quest 3)**

**Target specifications:**
* **Available RAM:** ~6 GB (shared CPU/GPU)
* **Practical VRAM budget:** 256-512 MB for voxel data
* **Expected model sizes:** Majority < 256³ voxels

**Recommended configuration:**
* BrickPool capacity: 256 MB (32M bricks)
* Maximum active segments: 4-8 (32-64 MB BrickTables)
* Total memory footprint: ~300-600 MB
* Editing window: 512³ voxels comfortably

This configuration provides ample space for typical VR sculpting and building applications while leaving headroom for engine overhead and other game assets.

---

### **Graceful Degradation**

The system is designed to fail gracefully when hardware limits are reached:

1. **BrickTable allocation failure:** Indicates insufficient RAM for segment. User can:
   * Save and close distant regions
   * Reduce active editing area
   * Upgrade to machine with more RAM

2. **BrickPool exhaustion:** Triggers eviction of cached bricks. User can:
   * Work in smaller regions
   * Increase BrickPool size if VRAM available
   * Accept slower performance as paging increases

3. **GPU texture limits:** Implementation-specific. User can:
   * Switch to Texture3D implementation
   * Reduce BrickPool capacity
   * Use compute shader alternative

**No artificial limits are imposed.** The system will use available hardware to its fullest extent and fail with clear error messages when physical limitations are reached, allowing users to make informed decisions about their workflows.

