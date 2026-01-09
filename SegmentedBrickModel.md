# 1. Program Objective

To build a **C# Voxel Engine Core** that enables the editing and rendering of a massive 65,536³ coordinate space using a fixed, low-memory footprint. The system must emulate virtual memory: keeping the full dataset on the "disk" (RAM) while only loading the active viewing area into the "cache" (GPU VRAM).

# 2. Core Technical Requirements

* **Coordinate Space:** 16-bit Unsigned Integer 0 to 65,535.
* **Coordinate System:** Right-handed, Z-up.
* **Voxel Unit:** 8-bit value (0 = Empty, 1–255 = Material).
* **Logical Data Unit:** The "Brick" (2×2×2 voxels = 8 bytes, stored as `ulong`).
  * **Purpose:** Bricks enable fast bulk operations and maintain compatibility with `IBrickModel`.
  * **Storage:** Each segment stores a dense 64³ array of bricks (2 MB per segment).
* **Performance:** Zero memory allocation during the update loop; reliance on bit-shifting over division.
* **Dependency:** The Core library must be Engine-Agnostic (No references to Godot/Unity).

# 3. Algorithmic Strategy: Virtual Address Mapping

## 3.1 Bit-Sliced Addressing Scheme

The system utilizes a Bit-Sliced Addressing Scheme to map a world coordinate `(x, y, z)` into a physical memory address.

* **Bits 7–15 (9 bits):** **Segment ID**. Maps to the SegmentedDictionary. (512 Segments per axis).
* **Bits 1–6 (6 bits):** **Brick Index**. Position of brick within segment. (64 Bricks per Segment dimension).
* **Bit 0 (1 bit):** **Voxel Offset**. Maps to the byte index (0–7) inside the Brick.

**Total Coordinate Reach:** 512 segments × 64 bricks × 2 voxels/brick = 65,536 voxels per axis.

## 3.2 Design Goals

* Deterministic
* GPU-friendly
* Easy bit extraction
* Sparse at world level (only allocate segments as needed)
* Dense within segments (no brick-level indirection overhead)
* Compatible with `512³` segment addressing and `64³` bricks per segment
* Avoids Morton complexity unless truly needed
* Fast 64-bit bulk operations for brick-level I/O

## 3.3 Brick Coordinates

Each voxel coordinate `(x, y, z)` is first snapped to a brick origin:

```csharp
bx = x & ~1;
by = y & ~1;
bz = z & ~1;
```

Each brick occupies a `2×2×2` region.

## 3.4 Brick-Space Decomposition

Each axis is decomposed as:

| Bits | Meaning |
| ----- | ----- |
| 0 | Local voxel bit (handled separately) |
| 1–6 | Brick position within segment (0–63) |
| 7–15 | Segment coordinate (0–511) |

Thus:

```
World axis (16 bits)
┌───────────────┬───────────────┬───────┐
│ Segment (9)   │ Brick (6)     │ Voxel │
│ bits 7–15     │ bits 1–6      │ bit 0 │
└───────────────┴───────────────┴───────┘
```

## 3.5 Segment ID (Persistent Store Key)

Segments are **64×64×64 bricks**, i.e. **128³ voxels**.

All segmentation and paging operates in **brick space**; voxel space is derived.

Segment coordinates:

```csharp
ushort sx = (ushort)(x >> 7);  // Bits 7-15 (9 bits)
ushort sy = (ushort)(y >> 7);
ushort sz = (ushort)(z >> 7);
```

Packed into a 32-bit key (27 bits used):

```csharp
uint segmentId =
	((uint)sx << 18) |
	((uint)sy << 9) |
	(uint)sz;
```

This directly indexes the sparse segment dictionary.

Each segment contains a volume of **64×64×64 bricks** = **128×128×128 voxels**.

## 3.6 Brick Index Within Segment

Each segment contains **64×64×64 bricks**.

Brick coordinates within segment:

```csharp
int px = (x >> 1) & 0x3F;  // Bits 1-6 (6 bits = 0-63)
int py = (y >> 1) & 0x3F;
int pz = (z >> 1) & 0x3F;
```

These coordinates are used to index directly into the segment's dense brick array.

### Brick Index Linearization

Within a segment, bricks are stored in a flat array using X-major ordering:

```csharp
brickIndex = px | (py << 6) | (pz << 12);
```

This ordering:
* Matches natural memory traversal
* Is cache-friendly on CPU
* Is trivial to replicate in GPU code
* Fits in 18 bits (0 to 262,143)

### Why This Mapping is Correct

* **Bitwise only** (no division/modulo)
* Segment boundaries align with large streaming regions
* Brick-level addressing aligns with GPU cache locality
* Independent X/Y/Z addressing simplifies raymarch math
* Avoids Morton complexity

# 4. Data Structure Specification

## 4.1 Core Architecture

The system uses **sparse segments with dense brick storage**:

- **World level:** Segments are allocated on-demand (sparse)
- **Segment level:** Each segment stores a dense 64³ array of bricks
- **No indirection:** Direct indexing from coordinates to brick data

### 4.1.1 Segment Storage

```csharp
// Primary storage: sparse segment map
Dictionary<uint, ulong[]> segments;

// Each segment contains:
// - 64³ = 262,144 bricks
// - Each brick = 1 ulong = 8 bytes
// - Total per segment = 262,144 × 8 bytes = 2,097,152 bytes (2 MB)
```

**Segment ID Calculation:**

```csharp
uint ComputeSegmentId(ushort sx, ushort sy, ushort sz)
{
	return ((uint)sx << 18) | ((uint)sy << 9) | (uint)sz;
}
```

**Note:** Segment IDs use only 27 bits (9+9+9) but are stored in `uint` (32 bits) for alignment and efficiency.

## 4.2 SegmentedBrickModel (The Orchestrator)

> **Note on Naming:** The class is called `SegmentedBrickModel` because it implements **architectural segmentation with direct brick storage**, not generic data partitioning. Each "segment" is a precisely defined 64³ brick region (128³ voxels) that enables:
> - **Sparse allocation**: Only active segments consume memory
> - **Direct addressing**: World coordinates map to segment → brick via bit-slicing
> - **Fast bulk I/O**: Bricks are stored as ulongs for efficient copying
>
> This differs from arbitrary chunking schemes where divisions are primarily for spatial organization rather than memory management and API compatibility (`IBrickModel`).

### Implementation

```csharp
public class SegmentedBrickModel : IBrickModel
{
	private Dictionary<uint, ulong[]> segments = new();

	public ulong GetBrick(ushort x, ushort y, ushort z)
	{
		// 1. Snap to brick origin
		x = (ushort)(x & ~1);
		y = (ushort)(y & ~1);
		z = (ushort)(z & ~1);

		// 2. Compute segment coordinates
		ushort sx = (ushort)(x >> 7);
		ushort sy = (ushort)(y >> 7);
		ushort sz = (ushort)(z >> 7);
		uint segmentId = ((uint)sx << 18) | ((uint)sy << 9) | (uint)sz;

		// 3. Check if segment exists
		if (!segments.TryGetValue(segmentId, out ulong[] bricks))
			return 0UL;  // Empty segment

		// 4. Compute brick index within segment
		int bx = (x >> 1) & 0x3F;
		int by = (y >> 1) & 0x3F;
		int bz = (z >> 1) & 0x3F;
		int brickIndex = bx | (by << 6) | (bz << 12);

		// 5. Return brick
		return bricks[brickIndex];
	}

	public void SetBrick(ushort x, ushort y, ushort z, ulong brickPayload)
	{
		// 1-2. Same as GetBrick
		x = (ushort)(x & ~1);
		y = (ushort)(y & ~1);
		z = (ushort)(z & ~1);

		ushort sx = (ushort)(x >> 7);
		ushort sy = (ushort)(y >> 7);
		ushort sz = (ushort)(z >> 7);
		uint segmentId = ((uint)sx << 18) | ((uint)sy << 9) | (uint)sz;

		// 3. Get or create segment
		if (!segments.TryGetValue(segmentId, out ulong[] bricks))
		{
			bricks = new ulong[64 * 64 * 64];  // 2 MB allocation
			segments[segmentId] = bricks;
		}

		// 4-5. Same as GetBrick
		int bx = (x >> 1) & 0x3F;
		int by = (y >> 1) & 0x3F;
		int bz = (z >> 1) & 0x3F;
		int brickIndex = bx | (by << 6) | (bz << 12);

		bricks[brickIndex] = brickPayload;

		// 6. Notify GPU of change
		OnBrickDirty?.Invoke(segmentId, brickIndex, brickPayload);
	}

	public event Action<uint, int, ulong> OnBrickDirty;
}
```

## 4.3 Integration Layer (Engine Specific)

### 4.3.1 GodotVoxelBridge (The Renderer)

* **Purpose:** Listens to the Core and updates GPU textures.
* **Dependencies:** Reference to SegmentedBrickModel and Godot's RenderingServer.
* **Key Behavior:**
  * **Initialization:** Creates GPU textures for active segments.
  * **Event Handling:** Subscribes to `OnBrickDirty`.
  * **Update:** Uploads modified bricks to appropriate GPU texture.

## 4.4 Success Metrics

The implementation is successful only if:

1. **Fixed VRAM per segment:** Segments use consistent 2 MB each regardless of content.
2. **No Lag:** SetBrick/GetBrick generate **zero garbage collection** pressure during edits.
3. **Correctness:** A brick modification in C# reflects instantly on the GPU.

# 5. Segment Management

## 5.1 Allocation Policy

Segments are allocated **on-demand** when the first brick within them is written:

```csharp
// In SetBrick implementation
if (!segments.TryGetValue(segmentId, out ulong[] bricks))
{
	bricks = new ulong[64 * 64 * 64];  // 2 MB allocation
	segments[segmentId] = bricks;
}
```

**Key properties:**
* Each segment is **self-contained** (no cross-segment dependencies)
* Segments are **independent** (can be loaded/unloaded individually)
* No brick-level cache management (entire segment loaded or not)

## 5.2 Deallocation Strategy

### Policy: Explicit Region-Based Management

The core does **not** autonomously unload segments.

Instead:

* The **editor tool** defines an *active region* (typically around the camera/cursor)
* Segments outside this region are explicitly unloaded via:

```csharp
public void UnloadSegment(ushort sx, ushort sy, ushort sz)
{
	uint segmentId = ((uint)sx << 18) | ((uint)sy << 9) | (uint)sz;
	segments.Remove(segmentId);

	// Notify GPU to release textures for this segment
	OnSegmentUnloaded?.Invoke(segmentId);
}
```

This approach ensures:
* **Predictable** memory usage
* **Deterministic** behavior (no hidden eviction heuristics)
* **No surprise stalls** during editing
* **No GC pressure** from automatic management

### Typical Editor Workflow

```csharp
// Every frame or on camera move:
void UpdateActiveRegion(Vector3 cameraPos)
{
	// 1. Determine segments within edit radius (e.g., 256-512 voxels)
	HashSet<uint> desiredSegments = ComputeNearbySegments(cameraPos, editRadius);

	// 2. Unload segments far from camera
	foreach (var segId in segments.Keys.ToList())
	{
		if (!desiredSegments.Contains(segId))
		{
			// Unpack segment ID to coordinates
			ushort sx = (ushort)(segId & 0x1FF);
			ushort sy = (ushort)((segId >> 9) & 0x1FF);
			ushort sz = (ushort)((segId >> 18) & 0x1FF);
			UnloadSegment(sx, sy, sz);
		}
	}

	// 3. Segments within radius are automatically loaded on first edit
}
```

## 5.3 Persistence and Serialization

Segments remain in memory until explicitly unloaded. For persistence:

```csharp
// Save to disk
public void SaveSegment(uint segmentId, string filePath)
{
	if (segments.TryGetValue(segmentId, out ulong[] bricks))
	{
		// Write 2 MB segment to disk
		File.WriteAllBytes(filePath,
			MemoryMarshal.AsBytes(bricks.AsSpan()).ToArray());
	}
}

// Load from disk
public void LoadSegment(uint segmentId, string filePath)
{
	byte[] data = File.ReadAllBytes(filePath);
	ulong[] bricks = new ulong[64 * 64 * 64];
	MemoryMarshal.Cast<byte, ulong>(data).CopyTo(bricks);
	segments[segmentId] = bricks;

	OnSegmentLoaded?.Invoke(segmentId);
}
```

## 5.4 Why Not LRU or Automatic Eviction?

**Explicit management is superior for editing workflows:**

* **No hidden costs:** Editor tools know exactly when allocation happens
* **Spatial intent:** Camera position predicts access patterns better than LRU
* **Stable performance:** No sudden frame drops from unexpected evictions
* **Simpler debugging:** Segment residency is explicit, not heuristic-driven

**Comparison:**

| Strategy | Pros | Cons |
|----------|------|------|
| LRU Cache | Automatic | Unpredictable stalls, metadata overhead |
| Automatic GC | Hands-free | Non-deterministic, GC pressure |
| Explicit Region | Predictable, deterministic | Requires editor integration |

The explicit approach matches user expectations in content authoring tools where control and predictability are paramount.

# 6. GPU Integration

## 6.1 Overview

In the dense segment architecture, GPU integration is straightforward:
* Each active segment uploads its 64³ brick array as GPU texture data
* The GPU maintains a directory of active segments for lookup
* Raymarching performs: world coord → segment lookup → brick index → texture fetch

**No indirection layer** (no SlotIDs, no BrickTable on CPU for caching).

## 6.2 Brick Data Format

### Memory Layout (CPU and GPU)

Each brick is stored as a `ulong` (8 bytes) with canonical byte order:

```
byte 0: voxel (x=0, y=0, z=0)
byte 1: voxel (x=0, y=0, z=1)
byte 2: voxel (x=1, y=0, z=0)
byte 3: voxel (x=1, y=0, z=1)
byte 4: voxel (x=0, y=1, z=0)
byte 5: voxel (x=0, y=1, z=1)
byte 6: voxel (x=1, y=1, z=0)
byte 7: voxel (x=1, y=1, z=1)
```

**Formula:**
```csharp
int byteIndex = ((localY << 1) | localX) * 2 + localZ;
```

This mapping is **isomorphic** to `VoxelBrick.Payload` bit layout, ensuring CPU/GPU consistency.

## 6.3 GPU Texture Options

### Option A: Texture3D Per Segment (Recommended)

Upload each segment as a separate `Texture3D`:

- Format: RG8_UNORM
- Dimensions: 64 × 64 × 32
  - X: 64 bricks
  - Y: 64 bricks
  - Z: 32 layers (each layer stores 2 Z-slices of bricks via RG channels)
- Size: 64 × 64 × 32 × 2 bytes = 262,144 bytes = 256 KB per segment texture

**Advantages:**
* Natural 3D indexing
* Good cache coherence
* No layer limits
* Direct brick coordinate mapping

**Upload:**
```csharp
void UploadSegment(uint segmentId, ulong[] bricks)
{
	byte[] textureData = new byte[64 * 64 * 32 * 2];  // 256 KB

	for (int bz = 0; bz < 64; bz++)
	for (int by = 0; by < 64; by++)
	for (int bx = 0; bx < 64; bx++)
	{
		int brickIndex = bx | (by << 6) | (bz << 12);
		ulong brick = bricks[brickIndex];

		// Pack into RG8: each Z-pair of voxels per pixel
		int texelIndex = (bx + by * 64 + (bz / 2) * 64 * 64) * 2;
		// Store 4 voxels in RG format based on Z parity
		// (Detailed packing logic here)
	}

	Texture3D texture = CreateTexture3D(64, 64, 32, RG8_UNORM, textureData);
	segmentTextures[segmentId] = texture;
}
```

### Option B: Single Large Texture3D (High Capacity)

Pack all segments into one large texture:

```
Format: R8_UNORM
Dimensions: 128 × 128 × 1024 (example for 8 segments)
  - Each 64³ region = one segment
Size: Configurable based on max active segments
```

**Advantages:**
* Single bind point
* Better for many segments
* Simpler shader code

## 6.4 CPU → GPU Sync Strategy

The GPU receives two types of updates with different strategies:

### Upload Strategy Overview

**Segment Directory (small - <1 KB):**
- Re-uploaded **entirely** when segments load/unload
- Happens **infrequently** (camera moves, segment boundary crossings)
- Too small to optimize incremental updates

**Voxel Data Textures (large - 256 KB per segment):**
- **Initial upload:** Full segment on first load (256 KB)
- **Incremental updates:** Individual brick modifications (8 bytes)
- Happens **frequently** (every voxel edit)

### Implementation

**Event fired by SegmentedBrickModel:**
```csharp
public event Action<uint, int, ulong> OnBrickDirty;  // segmentId, brickIndex, payload
public event Action<uint> OnSegmentLoaded;
public event Action<uint> OnSegmentUnloaded;

// In SetBrick:
OnBrickDirty?.Invoke(segmentId, brickIndex, brickPayload);
```

**GPU bridge handles updates:**

```csharp
void OnBrickModified(uint segmentId, int brickIndex, ulong brick)
{
	if (!segmentTextures.TryGetValue(segmentId, out Texture3D texture))
	{
		// First brick in segment: create texture and upload entire segment (256 KB)
		UploadSegment(segmentId, model.GetSegmentData(segmentId));
		RebuildAndUploadDirectory();  // Re-upload entire directory (<1 KB)
		return;
	}

	// Incremental update: compute texture coordinates from brickIndex
	int bx = brickIndex & 0x3F;
	int by = (brickIndex >> 6) & 0x3F;
	int bz = (brickIndex >> 12) & 0x3F;

	// Update small texel region (8 bytes worth of data)
	UpdateTextureRegion(texture, bx, by, bz, brick);
	// Directory unchanged - no upload needed
}

void OnSegmentLoaded(uint segmentId)
{
	UploadSegment(segmentId, model.GetSegmentData(segmentId));
	RebuildAndUploadDirectory();  // Directory changed - re-upload all
}

void OnSegmentUnloaded(uint segmentId)
{
	ReleaseTexture(segmentId);
	RebuildAndUploadDirectory();  // Directory changed - re-upload all
}

void RebuildAndUploadDirectory()
{
	// Convert CPU Dictionary to GPU array
	List<SegmentEntry> gpuDirectory = new();

	foreach (var (segmentId, _) in segmentTextures)
	{
		// Unpack segment ID back to coordinates
		ushort sx = (ushort)(segmentId & 0x1FF);
		ushort sy = (ushort)((segmentId >> 9) & 0x1FF);
		ushort sz = (ushort)((segmentId >> 18) & 0x1FF);

		int texIndex = GetTextureIndex(segmentId);
		gpuDirectory.Add(new SegmentEntry(sx, sy, sz, texIndex));
	}

	// Upload entire array to GPU (<1 KB total)
	UploadArrayToGPU(gpuDirectory);
}
```

**Key insight:** Directory is so small (<1 KB) that rebuilding and re-uploading it entirely is cheaper than tracking incremental changes.

## 6.5 GPU Segment Directory & Lookup

### The Challenge

The CPU has a sparse `Dictionary<uint, ulong[]>` where only active segments exist. The GPU raymarcher needs to:
1. Determine which segment a world coordinate belongs to
2. Access that segment's brick data texture
3. Fetch the specific brick and voxel
4. Do this efficiently during raymarching (millions of lookups per frame)

### Solution: Active Segment Directory

Upload a small **directory** of active segments to the GPU, mapping each to its texture resource.

#### CPU-Side Structure

```csharp
// Compact list of currently active segments
struct ActiveSegment
{
	public ushort segmentX;  // 0-511
	public ushort segmentY;
	public ushort segmentZ;
	public int textureIndex;  // Index into texture array or texture ID
}

List<ActiveSegment> activeSegments;  // Typically 8-64 segments
```

#### GPU-Side Structures

**Upload Format (Godot 4):**

```gdshader
// Small uniform buffer - list of active segments
struct SegmentEntry {
	uvec3 segmentCoord;      // (sx, sy, sz)
	uint textureIndex;       // Which texture contains this segment's data
};

uniform int activeSegmentCount;
uniform SegmentEntry segmentDirectory[MAX_ACTIVE_SEGMENTS];  // e.g., 64 entries

// Segment brick data - one of:
// Option A: Array of Texture3D (one per segment)
uniform sampler3D segmentTextures[MAX_ACTIVE_SEGMENTS];  // RG8, 64×64×32 each

// Option B: Single large Texture3D with packed segments
uniform sampler3D packedSegmentTexture;  // Larger dimensions, multiple segments
```

**Memory Usage (Option A - typical Quest 3):**
- Segment directory: 64 segments × 16 bytes = **1 KB**
- Segment textures: 8 active × 256 KB = **2 MB**
- Total: **~2 MB** for 8 active segments

**Memory Usage (Option B - larger scenes):**
- Segment directory: 64 segments × 16 bytes = **1 KB**
- Packed texture: ~16-32 MB (depends on segment count and packing)

### GPU Lookup Algorithm

```gdshader
// Step 1: Find which segment contains this world coordinate
int findSegment(ivec3 worldPos) {
	ivec3 segCoord = worldPos >> 7;  // Bits 7-15 = segment coordinates

	// Linear search (fast for small N)
	for (int i = 0; i < activeSegmentCount; i++) {
		if (all(equal(segmentDirectory[i].segmentCoord, uvec3(segCoord)))) {
			return i;
		}
	}
	return -1;  // Segment not loaded
}

// Step 2: Fetch voxel directly from segment texture
uint fetchVoxel(ivec3 worldPos) {
	// Find segment
	int segIdx = findSegment(worldPos);
	if (segIdx < 0) return 0u;  // Segment not loaded

	// Extract brick coordinates within segment (bits 1-6)
	ivec3 brickCoord = (worldPos >> 1) & 0x3F;  // 0-63 on each axis

	// Extract voxel offset within brick (bit 0)
	ivec3 voxelOffset = worldPos & 1;  // 0 or 1 on each axis

	// Option A: Fetch from segment's dedicated Texture3D
	int texIndex = int(segmentDirectory[segIdx].textureIndex);

	// Brick coords map to texture coords
	// Z is packed: 64 Z-layers → 32 texture layers (2 per layer via RG)
	ivec3 texCoord = ivec3(brickCoord.x, brickCoord.y, brickCoord.z / 2);
	uvec2 voxelPair = texelFetch(segmentTextures[texIndex], texCoord, 0).rg * 255.0;

	// Select R or G channel based on brick Z parity and voxel Z offset
	bool useGreen = ((brickCoord.z & 1) == 1);
	uint brick_z0_z1 = useGreen ? voxelPair.g : voxelPair.r;

	// Now we have 4 voxels packed in brick_z0_z1 based on X,Y
	// Extract specific voxel using X,Y,Z offset
	// (Detailed bit extraction here based on canonical layout)

	return brick_z0_z1;  // Simplified - full impl would unpack properly
}
```

**Simplified Direct Voxel Fetch (Alternative):**

```gdshader
uint fetchVoxel(ivec3 worldPos) {
	int segIdx = findSegment(worldPos);
	if (segIdx < 0) return 0u;

	// Brick and voxel coordinates
	ivec3 brickCoord = (worldPos >> 1) & 0x3F;
	ivec3 localVoxel = worldPos & 1;

	// Fetch brick from texture (implementation dependent on packing)
	int texIdx = int(segmentDirectory[segIdx].textureIndex);

	// Direct 3D texture fetch at voxel resolution (if using R8 format)
	// worldPos within segment = worldPos - (segmentCoord << 7)
	ivec3 segmentCoord = ivec3(segmentDirectory[segIdx].segmentCoord);
	ivec3 voxelInSegment = worldPos - (segmentCoord << 7);

	return uint(texelFetch(segmentTextures[texIdx], voxelInSegment, 0).r * 255.0);
}
```

### Optimization Notes

**For ≤64 active segments:**
- Linear search is fastest (no branching, vectorizable)
- ~10-30 GPU clock cycles

**For >64 segments:**
- Consider hash table or binary search
- Trade-off: complexity vs segment count

**Spatial coherence:**
- Adjacent rays often hit same segment
- Cache last segment index in thread local
- Can reduce lookups by 50-80%

## 6.6 Complete GPU Raymarching Algorithm

This section describes the **full raymarching loop** that renders voxels using the SegmentedBrickModel data uploaded to the GPU.

### Overview

The raymarcher uses **DDA (Digital Differential Analyzer)** traversal to step through voxel space along a ray. At each step, it:
1. Determines current voxel coordinates
2. Looks up the voxel material via segment→texture fetch (direct access, no indirection)
3. Applies lighting and accumulates color
4. Steps to next voxel

### Shader Inputs

```gdshader
// Camera/Ray
uniform mat4 inverseViewProjection;
uniform vec3 cameraPosition;

// Data structures (from section 6.5)
uniform int activeSegmentCount;
uniform SegmentEntry segmentDirectory[64];
uniform sampler3D segmentTextures[64];  // One texture per segment

// Raymarch parameters
uniform float maxDistance = 100.0;
uniform int maxSteps = 256;
```

### Main Raymarch Function

```gdshader
vec4 raymarch(vec2 screenUV) {
	// 1. Generate ray from camera
	vec3 rayOrigin = cameraPosition;
	vec3 rayDir = getRayDirection(screenUV, inverseViewProjection);

	// 2. Initialize DDA traversal
	vec3 rayPos = rayOrigin;
	ivec3 voxelPos = ivec3(floor(rayPos));
	vec3 deltaDist = abs(vec3(1.0) / rayDir);
	ivec3 rayStep = ivec3(sign(rayDir));
	vec3 sideDist = (sign(rayDir) * (vec3(voxelPos) - rayPos) + (sign(rayDir) * 0.5) + 0.5) * deltaDist;

	vec3 accumulatedColor = vec3(0.0);
	float accumulatedAlpha = 0.0;
	float travelDistance = 0.0;

	// 3. Step through voxels
	for (int step = 0; step < maxSteps && travelDistance < maxDistance; step++) {
		// Fetch voxel material
		uint material = fetchVoxel(voxelPos);

		if (material > 0u) {
			// 4. Hit solid voxel - compute lighting
			vec3 normal = computeNormal(voxelPos, sideDist);
			vec3 color = getMaterialColor(material);
			float lighting = computeLighting(vec3(voxelPos), normal);

			// 5. Accumulate color (simple front-to-back compositing)
			float alpha = 1.0;  // Opaque voxels
			accumulatedColor += (1.0 - accumulatedAlpha) * color * lighting * alpha;
			accumulatedAlpha += (1.0 - accumulatedAlpha) * alpha;

			// Early exit if fully opaque
			if (accumulatedAlpha >= 0.99) break;
		}

		// 6. DDA step to next voxel
		if (sideDist.x < sideDist.y) {
			if (sideDist.x < sideDist.z) {
				sideDist.x += deltaDist.x;
				voxelPos.x += rayStep.x;
				travelDistance = sideDist.x;
			} else {
				sideDist.z += deltaDist.z;
				voxelPos.z += rayStep.z;
				travelDistance = sideDist.z;
			}
		} else {
			if (sideDist.y < sideDist.z) {
				sideDist.y += deltaDist.y;
				voxelPos.y += rayStep.y;
				travelDistance = sideDist.y;
			} else {
				sideDist.z += deltaDist.z;
				voxelPos.z += rayStep.z;
				travelDistance = sideDist.z;
			}
		}

		// 7. Bounds check (optional - depends on use case)
		if (any(lessThan(voxelPos, ivec3(0))) ||
			any(greaterThanEqual(voxelPos, ivec3(65536)))) {
			break;
		}
	}

	// 8. Return final color
	return vec4(accumulatedColor, accumulatedAlpha);
}
```

### Helper Functions

```gdshader
// Compute surface normal from DDA step direction
vec3 computeNormal(ivec3 voxelPos, vec3 sideDist) {
	vec3 normal = vec3(0.0);
	if (sideDist.x < sideDist.y && sideDist.x < sideDist.z) {
		normal = vec3(-sign(rayStep.x), 0, 0);
	} else if (sideDist.y < sideDist.z) {
		normal = vec3(0, -sign(rayStep.y), 0);
	} else {
		normal = vec3(0, 0, -sign(rayStep.z));
	}
	return normal;
}

// Simple directional lighting
float computeLighting(vec3 worldPos, vec3 normal) {
	vec3 lightDir = normalize(vec3(0.5, 1.0, 0.3));
	float diffuse = max(dot(normal, lightDir), 0.0);
	float ambient = 0.3;
	return ambient + diffuse * 0.7;
}

// Map material ID to color (placeholder)
vec3 getMaterialColor(uint materialId) {
	// In production, use a palette texture
	return vec3(float(materialId) / 255.0);
}
```

### Performance Characteristics

**Quest 3 (720p per eye @ 72 Hz):**
- Resolution: 1440×1584 per eye = 2.28M pixels
- Budget: 13.9ms per frame
- Typical performance:
  - Empty space: 200-500 rays/sec per pixel (fast traversal)
  - Dense voxels: 50-100 steps per ray = 4-8ms raymarch time
  - Segment lookups: ~20 cycles each, amortized by coherence

**Optimization tips:**
1. **Early ray termination:** Stop after max alpha or distance
2. **Spatial coherence:** Cache last segment/brick per thread
3. **LOD:** Use mipmaps or octree for distant voxels (not in this spec, but compatible)
4. **Compute shaders:** Can be faster than fragment shaders for raymarching

### What GPU Actually Receives (Summary)

**Per Segment Upload:**
1. **Segment Directory Entry:** 16 bytes (segment coordinates + texture index)
2. **Segment Texture:** 256 KB (64³ bricks × 8 bytes, packed as 64×64×32 RG8 texture)

**Typical Quest 3 Session:**
- Active segments: 8-16
- Segment directory: <1 KB
- Segment textures: 8 × 256 KB = **2 MB**
- **Total GPU Memory: ~2-4 MB** (vs 270+ MB in old architecture!)

**Lookup Chain Per Voxel (Simplified):**
```
World Coordinate (x,y,z)
	↓
Segment Lookup (linear search, ~20 cycles)
	↓
Brick Index Calculation (bit shifts, 3 cycles)
	↓
Texture Fetch (cached read, ~50-100 cycles)
	↓
Voxel Material (8-bit value)
```

**Total latency per voxel:** ~100-150 GPU cycles

# 7. Compatibility & Interfaces

To remain capable of effective serialization and deserialization, SegmentedBrickModel must implement `IBrickModel`.

## 7.1 `IBrickModel` Interface

```csharp
/// <summary>
/// The VoxelBrick enumeration is sparse: it includes only all the non-empty bricks in an undefined order.
/// </summary>
public interface IBrickModel : IModel, IEnumerable<VoxelBrick>
{
	// Default implementation of the IModel byte-access
	// This makes any IBrickModel automatically compliant with IModel!
	// byte IModel.this[ushort x, ushort y, ushort z] => VoxelBrick.GetVoxel(GetBrick(x, y, z), x & 1, y & 1, z & 1);
	/// <summary>
	/// Implementation should snap x,y,z to the nearest multiple of 2 
	/// internally using: x & -1, y & -1, z & -1
	/// </summary>
	ulong GetBrick(ushort x, ushort y, ushort z);
}
```

## 7.2 `VoxelBrick` Structure

```csharp
public readonly record struct VoxelBrick(ushort X, ushort Y, ushort Z, ulong Payload)
{
	/// <summary>
	/// Extracts a single voxel byte from the 2x2x2 payload.
	/// localX, localY, localZ must be 0 or 1.
	/// </summary>
	public byte GetVoxel(int localX, int localY, int localZ) => GetVoxel(Payload, localX, localY, localZ);
	public static byte GetVoxel(ulong payload, int localX, int localY, int localZ) =>
		(byte)((payload >> ((((localZ & 1) << 2) | ((localY & 1) << 1) | (localX & 1)) << 3)) & 0xFF);
	/// <summary>
	/// A helper to "edit" a brick by returning a new one with one voxel changed
	/// localX, localY, localZ must be 0 or 1.
	/// </summary>
	public VoxelBrick WithVoxel(int localX, int localY, int localZ, byte material)
	{
		int shift = ((localZ << 2) | (localY << 1) | localX) << 3;
		ulong mask = 0xFFUL << shift,
			newPayload = (Payload & ~mask) | ((ulong)material << shift);
		return this with { Payload = newPayload };
	}
}
```

## 7.3 `IModel` Interface

```csharp
/// <summary>
/// The Voxel enumeration is sparse: it includes only all the non-zero voxels in an undefined order.
/// </summary>
public interface IModel : IEnumerable<Voxel>, IEnumerable
{
	byte this[ushort x, ushort y, ushort z] { get; }
	ushort SizeX { get; }
	ushort SizeY { get; }
	ushort SizeZ { get; }
}
```

## 7.4 `Voxel` Structure

```csharp
public readonly record struct Voxel(ushort X, ushort Y, ushort Z, byte Material)
```

# 8. Implementation Plan & Validation Stages

The implementation of `SegmentedBrickModel` should proceed in **clearly defined stages**, each producing a runnable, testable artifact. This reduces risk and ensures correctness before GPU integration.

## 8.1 Stage 1: Core Brick Primitives (CPU-Only)

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

## 8.2 Stage 2: SegmentedBrickModel Core Implementation

**Goal:** Implement the dense segment architecture with direct brick storage.

Deliverables:

* `SegmentedBrickModel` class
  * `Dictionary<uint, ulong[]> segments` storage
  * `GetBrick(x, y, z)` method
  * `SetBrick(x, y, z, payload)` method
  * Segment ID calculation
  * Brick index calculation
  * On-demand segment allocation
* Unit tests verifying:
  * Correct segment ID computation from coordinates
  * Correct brick index within segment
  * Segment allocation only on first write
  * GetBrick returns 0 for non-existent segments
  * SetBrick/GetBrick round-trip correctly
  * No allocations during edits to existing segments

Validation:

* Bricks can be written and read from any coordinate
* Segment boundaries are respected (128³ voxel regions)
* Memory usage scales linearly with active segments (2 MB per segment)

## 8.3 Stage 3: IBrickModel Interface Implementation

**Goal:** Ensure compatibility with existing voxel tooling and serialization.

Deliverables:

* `IBrickModel` interface implementation
  * `IEnumerable<VoxelBrick>` enumeration
  * Coordinate snapping (x & ~1)
* Unit tests verifying:
  * Enumeration yields only non-empty bricks
  * Enumeration covers all modified bricks
  * Brick coordinates are properly snapped

Validation:

* Enumeration order is undefined but stable per snapshot
* Data round-trips into other `IBrickModel` implementations
* Empty segments are not enumerated

## 8.4 Stage 4: Event System & Change Tracking

**Goal:** Enable GPU synchronization through change notifications.

Deliverables:

* `OnBrickDirty` event
  * Signature: `Action<uint segmentId, int brickIndex, ulong payload>`
* Event dispatch in `SetBrick`
* Unit tests verifying:
  * Event fires on every SetBrick call
  * Correct segment ID and brick index in event
  * Event payload matches stored brick

Validation:

* Subscribers receive notifications for all changes
* No events fired for GetBrick or failed operations

## 8.5 Stage 5: Segment Load/Unload Management

**Goal:** Implement explicit segment lifecycle management.

Deliverables:

* `UnloadSegment(sx, sy, sz)` method
* `LoadSegment(segmentId, data)` method
* `SaveSegment(segmentId, filePath)` method
* `OnSegmentLoaded` / `OnSegmentUnloaded` events

Validation:

* Segments can be saved to disk and reloaded with identical data
* UnloadSegment frees memory (verified via memory profiler)
* Events fire correctly on load/unload

## 8.6 Stage 6: Godot 4 GPU Integration

**Goal:** Validate CPU ↔ GPU correctness with direct segment texture uploads.

Deliverables:

* `GodotVoxelBridge` class
  * Segment directory management
  * Texture3D allocation per segment (or packed approach)
  * Segment upload: `ulong[]` → GPU texture
  * Incremental brick updates
* GPU shader setup:
  * Segment directory uniform buffer
  * Texture3D array or packed texture
* Subscribe to `OnBrickDirty` and `OnSegmentLoaded`/`OnSegmentUnloaded` events

Validation:

* Segment uploads contain correct brick data
* Individual brick edits update GPU immediately
* Segment unload releases GPU textures
* GPU memory usage = (active segments × 256 KB)

## 8.7 Stage 7: GPU Raymarcher Shader

**Goal:** Implement DDA raymarching that consumes segment textures.

Deliverables:

* Fragment or compute shader with:
  * Segment lookup function (linear search)
  * Voxel fetch from segment texture
  * DDA traversal loop
  * Basic lighting and compositing
* Integration with Godot 4 viewport

Validation:

* Voxels render correctly at expected world positions
* Segment boundaries are invisible (no seams)
* Performance is acceptable for target platform (Quest 3)

## 8.8 Stage 8: Interactive Editor Demo

**Goal:** End-to-end proof of concept with user interaction.

Deliverables:

* Simple Godot 4 scene with:
  * Camera controller
  * Voxel editing tool (place/remove)
  * Active region management (load segments near camera)
* Real-time voxel editing display

Validation:

* Interactive voxel editing appears on screen immediately
* Camera movement triggers appropriate segment load/unload
* Large coordinate jumps don't cause memory growth
* Stable frame rate under typical editor usage (60+ fps desktop, 72+ fps Quest 3)

# 9. Explicit Non-Goals

The `SegmentedBrickModel` data structure is intentionally **narrow in scope**. It exists to solve one specific problem: **low-latency, mutable voxel editing over a massive address space with fixed GPU memory usage**.

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

By excluding these concerns, `SegmentedBrickModel` remains:

* Deterministic
* Editor-friendly
* Easy to reason about
* Compatible with multiple downstream voxel representations

# 10. Hardware Considerations & Scalability

## 10.1 Design Philosophy

The `SegmentedBrickModel` is designed to scale gracefully from mobile VR headsets to high-end desktop workstations without imposing artificial limits in the data structure itself. The system will naturally hit hardware boundaries based on available memory and GPU capabilities, allowing users to create models as large as their hardware can support.

## 10.2 Memory Scaling Characteristics

### CPU Memory (System RAM)

Each active segment stores a dense 64³ brick array:
* **Size per segment:** 64³ bricks × 8 bytes = 262,144 × 8 = **2,097,152 bytes (2 MB)**
* **Scaling:** Linear with number of active segments
* **Typical usage:**
  * Small models (< 256³ voxels): 1-8 segments = **2-16 MB**
  * Medium models (256³ to 512³ voxels): 8-64 segments = **16-128 MB**
  * Large models (640³ voxels): ~125 segments = **250 MB**
  * Very large models (> 1024³ voxels): Hundreds of segments = **hundreds of MB**

The system will naturally limit itself based on available RAM. Users working with extremely large models will need proportionally more system memory.

**Key advantage:** Only loaded segments consume memory. Sparse worlds with localized editing use minimal RAM.

### GPU Memory (VRAM)

Each uploaded segment becomes a GPU texture:
* **Size per segment texture:** 64³ bricks packed as 64×64×32 RG8 = **256 KB**
* **Overhead:** Segment directory ~1 KB (negligible)
* **Scaling:** Linear with active segments
* **Typical configurations:**
  * Mobile VR (Quest 3): 8-16 segments = **2-4 MB** (extremely efficient!)
  * Desktop GPU: 32-128 segments = **8-32 MB** (room for large editing regions)

**Memory comparison vs old architecture:**
- Old: 256 MB BrickPool + 8 MB BrickTables = **264 MB**
- New: 8 segments × 256 KB = **2 MB** (130× reduction!)

The new architecture uses GPU memory proportional to active editing area, not a fixed cache size.

## 10.3 GPU Texture Implementation Strategies

The specification describes a Texture2DArray approach, but implementations may choose alternatives based on hardware:

### Option 1: Texture2DArray (Reference Implementation)
* Format: RG8, 2×2 pixels per layer
* **Limitation:** Max layers typically 2048-16384 depending on GPU
* **Suitable for:** Smaller editing windows, prototyping
* **Mobile compatibility:** Good (native format support)

### Option 2: Texture3D (High-Capacity Alternative)
* Format: R8, one voxel per texel
* **Advantages:**
  * No layer limit
  * Better cache coherence for raymarching
  * Can pack bricks in 3D space efficiently
* **Suitable for:** Large editing volumes, production use
* **Mobile compatibility:** Excellent

### Option 3: Compute Buffer (Maximum Flexibility)
* Format: Raw SSBO/UBO
* **Advantages:**
  * No size limits beyond VRAM
  * Direct memory mapping possible
  * Maximum control over layout
* **Suitable for:** High-end implementations, research
* **Mobile compatibility:** Good (Vulkan/Metal)

## 10.4 Mobile VR Considerations (Meta Quest 3)

**Target specifications:**
* **Available RAM:** ~6 GB (shared CPU/GPU)
* **Practical VRAM budget:** 256-512 MB for voxel data
* **Expected model sizes:** Majority < 256³ voxels

**Recommended configuration:**
* CPU: 8-16 active segments = **16-32 MB** RAM
* GPU: 8-16 segment textures = **2-4 MB** VRAM
* Editing window: 256-384 voxels per axis (2-4 segments per axis)
* Total memory footprint: **<40 MB** (extremely efficient!)

**This represents a massive improvement over the old architecture:**
- Old: 264+ MB minimum footprint
- New: <40 MB typical usage
- **Result:** Quest 3 can handle much larger editing sessions or allocate memory to other features

This configuration provides ample space for typical VR sculpting and building applications while leaving substantial headroom for engine overhead, gameplay assets, and physics simulation.

## 10.5 Graceful Degradation

The system is designed to fail gracefully when hardware limits are reached:

1. **Segment allocation failure (CPU):** Indicates insufficient RAM. User can:
   * Unload distant segments (explicit via editor tools)
   * Save and close regions not currently being edited
   * Reduce active editing radius
   * Upgrade to machine with more RAM

2. **GPU texture allocation failure:** Indicates VRAM exhaustion. User can:
   * Reduce number of simultaneously loaded segments
   * Use more aggressive segment culling based on camera distance
   * Switch to lower-resolution GPU texture format (if applicable)

3. **Performance degradation:** Too many segments or excessive segment thrashing. User can:
   * Reduce active region size
   * Implement segment caching/predictive loading
   * Profile segment access patterns

**No artificial limits are imposed.** The system will use available hardware to its fullest extent and fail with clear error messages when physical limitations are reached, allowing users to make informed decisions about their workflows.
