namespace BenVoxelGpu;

/// <summary>
/// Semantic description of how a tile's top surface behaves geometrically.
/// This enum is a semantic contract, not a rendering detail.
/// </summary>
public enum TileSurfaceType
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
