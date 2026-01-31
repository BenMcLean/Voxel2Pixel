namespace BenVoxelGpu;

/// <summary>
/// Classifies tiles in a heightfield using marching-squares-style neighborhood evaluation.
/// Converts an integer heightfield into a TileSurfaceType map that describes the surface
/// type and orientation of each tile.
/// </summary>
public static class TerrainClassifier
{
	/// <summary>
	/// Classifies tiles in a heightfield to produce a surface map.
	/// The heightMap must include a one-tile apron on all sides.
	/// Only the interior region is classified.
	/// </summary>
	/// <param name="heightMap">Height values with one-tile apron. Size must be at least 3x3.</param>
	/// <returns>Surface type for each interior tile. Size is (heightMap width - 2) x (heightMap height - 2).</returns>
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
				// Convert interior coordinates to heightmap coordinates (offset by 1 for apron)
				int x = ix + 1,
					z = iz + 1;

				surfaceMap[ix, iz] = ClassifyTile(heightMap, x, z);
			}
		}

		return surfaceMap;
	}

	/// <summary>
	/// Classifies a single tile based on its neighbors.
	/// </summary>
	/// <param name="heightMap">The full heightmap with apron</param>
	/// <param name="x">X coordinate in heightmap (not interior coordinate)</param>
	/// <param name="z">Z coordinate in heightmap (not interior coordinate)</param>
	private static TileSurfaceType ClassifyTile(byte[,] heightMap, int x, int z)
	{
		int hC = heightMap[x, z];

		// Compute height differences to neighbors
		// Using int to prevent underflow when subtracting bytes
		int dN = heightMap[x, z - 1] - hC,
			dE = heightMap[x + 1, z] - hC,
			dS = heightMap[x, z + 1] - hC,
			dW = heightMap[x - 1, z] - hC;

		// Rule 1: Cliff Dominance
		// If any neighbor is 2+ lower than this tile, it's a cliff edge - must be flat
		if (dN <= -2 || dE <= -2 || dS <= -2 || dW <= -2)
			return TileSurfaceType.Flat;

		// Count which neighbors are exactly 1 lower (candidates for slope direction)
		bool nLower = dN == -1,
			eLower = dE == -1,
			sLower = dS == -1,
			wLower = dW == -1;

		int lowerCount = (nLower ? 1 : 0) + (eLower ? 1 : 0) + (sLower ? 1 : 0) + (wLower ? 1 : 0);

		// Check that all non-slope neighbors are at same level or higher
		bool nOk = dN >= 0,
			eOk = dE >= 0,
			sOk = dS >= 0,
			wOk = dW >= 0;

		// Rule 2: Cardinal Slope
		// Exactly one neighbor is 1 lower, all others are >= 0
		if (lowerCount == 1)
		{
			if (nLower && eOk && sOk && wOk) return TileSurfaceType.CardinalSlopeNorth;
			if (eLower && nOk && sOk && wOk) return TileSurfaceType.CardinalSlopeEast;
			if (sLower && nOk && eOk && wOk) return TileSurfaceType.CardinalSlopeSouth;
			if (wLower && nOk && eOk && sOk) return TileSurfaceType.CardinalSlopeWest;
		}

		// Rule 3: Diagonal Slope
		// Exactly two orthogonal neighbors are 1 lower, all others are >= 0
		if (lowerCount == 2)
		{
			// Check for orthogonal pairs (N+E, E+S, S+W, W+N)
			// Opposite pairs (N+S or E+W) don't form valid diagonal slopes
			if (nLower && eLower && sOk && wOk) return TileSurfaceType.DiagonalSlopeNE;
			if (eLower && sLower && nOk && wOk) return TileSurfaceType.DiagonalSlopeSE;
			if (sLower && wLower && nOk && eOk) return TileSurfaceType.DiagonalSlopeSW;
			if (wLower && nLower && eOk && sOk) return TileSurfaceType.DiagonalSlopeNW;
		}

		// Rule 4: Default to Flat
		return TileSurfaceType.Flat;
	}
}
