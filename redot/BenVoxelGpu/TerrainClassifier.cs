namespace BenVoxelGpu;

/// <summary>
/// Classifies tiles in a heightfield using marching-squares-style neighborhood evaluation.
///
/// Key principle: Only LOWER neighbors (exactly 1 unit lower) trigger slopes.
/// Higher neighbors are handled by those neighbors sloping down toward us.
/// Cliffs (2+ units difference) generate vertical faces, not slopes.
/// </summary>
public static class TerrainClassifier
{
	/// <summary>
	/// Classifies tiles in a heightfield to produce a surface map.
	/// </summary>
	/// <param name="heightMap">Height values with one-tile apron. Size must be at least 3x3.</param>
	/// <returns>Surface type for each interior tile. Size is (width-2) x (height-2).</returns>
	public static TileSurfaceType[,] ClassifyTiles(byte[,] heightMap)
	{
		int fullWidth = heightMap.GetLength(0),
			fullHeight = heightMap.GetLength(1),
			interiorWidth = fullWidth - 2,
			interiorHeight = fullHeight - 2;

		if (interiorWidth < 1 || interiorHeight < 1)
			throw new System.ArgumentException("heightMap must be at least 3x3 to have a valid interior region");

		TileSurfaceType[,] surfaceMap = new TileSurfaceType[interiorWidth, interiorHeight];

		for (int iz = 0; iz < interiorHeight; iz++)
		{
			for (int ix = 0; ix < interiorWidth; ix++)
			{
				// Interior coordinate (ix, iz) maps to heightmap coordinate (ix+1, iz+1)
				int hx = ix + 1,
					hz = iz + 1;

				surfaceMap[ix, iz] = ClassifyTile(heightMap, hx, hz);
			}
		}

		return surfaceMap;
	}

	/// <summary>
	/// Classifies a single tile based on which neighbors are exactly 1 lower.
	/// </summary>
	/// <param name="heightMap">The full heightmap including apron</param>
	/// <param name="hx">X coordinate in heightmap (includes +1 offset for apron)</param>
	/// <param name="hz">Z coordinate in heightmap (includes +1 offset for apron)</param>
	private static TileSurfaceType ClassifyTile(byte[,] heightMap, int hx, int hz)
	{
		int hC = heightMap[hx, hz];

		// Compute height differences: neighbor - current
		// Negative means neighbor is LOWER, positive means neighbor is HIGHER
		int dN = heightMap[hx, hz - 1] - hC,
			dE = heightMap[hx + 1, hz] - hC,
			dS = heightMap[hx, hz + 1] - hC,
			dW = heightMap[hx - 1, hz] - hC;

		// Only care about neighbors that are exactly 1 lower (d == -1)
		// Higher neighbors (d > 0) are ignored - they handle the slope
		// Cliffs (d <= -2) are handled by vertical faces, not slopes
		bool nLower = dN == -1,
			eLower = dE == -1,
			sLower = dS == -1,
			wLower = dW == -1;

		int lowerCount = (nLower ? 1 : 0) + (eLower ? 1 : 0) + (sLower ? 1 : 0) + (wLower ? 1 : 0);

		return lowerCount switch
		{
			0 => TileSurfaceType.Flat,

			1 => (nLower, eLower, sLower, wLower) switch
			{
				(true, false, false, false) => TileSurfaceType.SlopeN,
				(false, true, false, false) => TileSurfaceType.SlopeE,
				(false, false, true, false) => TileSurfaceType.SlopeS,
				(false, false, false, true) => TileSurfaceType.SlopeW,
				_ => TileSurfaceType.Flat  // Shouldn't happen
			},

			2 => (nLower, eLower, sLower, wLower) switch
			{
				// Adjacent pairs -> corner
				(true, true, false, false) => TileSurfaceType.CornerNE,
				(false, true, true, false) => TileSurfaceType.CornerSE,
				(false, false, true, true) => TileSurfaceType.CornerSW,
				(true, false, false, true) => TileSurfaceType.CornerNW,
				// Opposite pairs -> treat as flat (vertical faces handle it)
				// This creates a "ridge" which isn't well-represented by slopes
				_ => TileSurfaceType.Flat
			},

			3 => (nLower, eLower, sLower, wLower) switch
			{
				// Three lower = valley toward the one that's NOT lower
				(false, true, true, true) => TileSurfaceType.ValleyN,  // N is not lower
				(true, false, true, true) => TileSurfaceType.ValleyE,  // E is not lower
				(true, true, false, true) => TileSurfaceType.ValleyS,  // S is not lower
				(true, true, true, false) => TileSurfaceType.ValleyW,  // W is not lower
				_ => TileSurfaceType.Flat  // Shouldn't happen
			},

			4 => TileSurfaceType.Peak,

			_ => TileSurfaceType.Flat
		};
	}
}
