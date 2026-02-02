namespace BenVoxelGpu;

/// <summary>
/// Semantic description of how a tile's top surface behaves geometrically.
///
/// Key principle: The HIGHER tile always slopes DOWN to meet lower neighbors.
/// Lower tiles stay flat; the higher neighbor handles the transition.
/// This ensures seamless connections: shared corners are at the minimum height.
/// </summary>
public enum TileSurfaceType
{
	/// <summary>All corners at base height (no lower neighbors, or only cliffs)</summary>
	Flat,

	// === One Edge Lowered (one neighbor is exactly 1 lower) ===
	SlopeN,  // North edge lowered (north neighbor is 1 lower)
	SlopeE,  // East edge lowered
	SlopeS,  // South edge lowered
	SlopeW,  // West edge lowered

	// === One Corner Lowered (two adjacent neighbors are exactly 1 lower) ===
	CornerNE,  // NE corner lowered (north AND east neighbors are 1 lower)
	CornerSE,  // SE corner lowered
	CornerSW,  // SW corner lowered
	CornerNW,  // NW corner lowered

	// === Three Corners Lowered / Valley (three neighbors are exactly 1 lower) ===
	ValleyN,  // All corners except those on pure N edge lowered (E, S, W neighbors lower)
	ValleyE,  // All corners except those on pure E edge lowered
	ValleyS,  // All corners except those on pure S edge lowered
	ValleyW,  // All corners except those on pure W edge lowered

	// === All Four Corners Lowered / Peak (all four neighbors are exactly 1 lower) ===
	Peak,
}
