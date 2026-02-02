using System;
using System.Collections.Generic;
using Godot;

namespace BenVoxelGpu;

/// <summary>
/// Generates terrain meshes from heightfield data using corner-based height computation.
///
/// Each tile has a discrete integer height from the heightmap. Corner heights are computed
/// from the 4 tiles sharing each corner, creating smooth slopes where tiles differ by 1 unit
/// and vertical cliff faces where they differ by 2+ units.
/// </summary>
public static class TerrainMeshBuilder
{
	/// <summary>
	/// Builds an ArrayMesh for a terrain chunk.
	/// </summary>
	/// <param name="heightMap">Height values with one-tile apron. Size must be (chunkSize+2) x (chunkSize+2).</param>
	/// <param name="chunkX">World X offset for this chunk (in tiles).</param>
	/// <param name="chunkZ">World Z offset for this chunk (in tiles).</param>
	/// <param name="chunkSize">Number of tiles in each dimension.</param>
	/// <param name="tileSize">World-space size of each tile.</param>
	/// <param name="heightStep">World-space height per height unit.</param>
	public static ArrayMesh BuildChunkMesh(
		byte[,] heightMap,
		int chunkX,
		int chunkZ,
		int chunkSize,
		float tileSize,
		float heightStep)
	{
		List<Vector3> vertices = [];
		List<Vector3> normals = [];
		List<int> indices = [];

		for (int iz = 0; iz < chunkSize; iz++)
		{
			for (int ix = 0; ix < chunkSize; ix++)
			{
				// Interior coordinate (ix, iz) maps to heightmap coordinate (hx, hz)
				int hx = ix + 1,
					hz = iz + 1;

				int tileHeight = heightMap[hx, hz];

				float worldX = (chunkX + ix) * tileSize,
					worldZ = (chunkZ + iz) * tileSize;

				// Compute corner heights from surrounding tiles
				float yNW = ComputeCornerHeight(heightMap, hx, hz, -1, -1, tileHeight) * heightStep;
				float yNE = ComputeCornerHeight(heightMap, hx, hz, 0, -1, tileHeight) * heightStep;
				float ySE = ComputeCornerHeight(heightMap, hx, hz, 0, 0, tileHeight) * heightStep;
				float ySW = ComputeCornerHeight(heightMap, hx, hz, -1, 0, tileHeight) * heightStep;

				// Corner world positions
				Vector3 nw = new(worldX, yNW, worldZ);
				Vector3 ne = new(worldX + tileSize, yNE, worldZ);
				Vector3 se = new(worldX + tileSize, ySE, worldZ + tileSize);
				Vector3 sw = new(worldX, ySW, worldZ + tileSize);

				// Generate top surface
				GenerateTopSurface(vertices, normals, indices, nw, ne, se, sw);

				// Generate cliff faces for each edge
				GenerateCliffFaces(vertices, normals, indices, heightMap,
					hx, hz, worldX, worldZ, tileSize, heightStep,
					yNW, yNE, ySE, ySW);
			}
		}

		ArrayMesh mesh = new();
		if (vertices.Count == 0)
			return mesh;

		Godot.Collections.Array arrays = [];
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
		arrays[(int)Mesh.ArrayType.Normal] = normals.ToArray();
		arrays[(int)Mesh.ArrayType.Index] = indices.ToArray();

		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		return mesh;
	}

	/// <summary>
	/// Computes the height of a corner based on all 4 tiles sharing it.
	/// Uses the second-lowest height to create symmetric slopes - both higher
	/// and lower tiles slope toward the shared corner height.
	/// </summary>
	/// <param name="heightMap">The heightmap with apron.</param>
	/// <param name="hx">Tile X in heightmap coordinates.</param>
	/// <param name="hz">Tile Z in heightmap coordinates.</param>
	/// <param name="dx">Corner offset: -1 for west corners (NW, SW), 0 for east corners (NE, SE).</param>
	/// <param name="dz">Corner offset: -1 for north corners (NW, NE), 0 for south corners (SW, SE).</param>
	/// <param name="tileHeight">The height of the current tile.</param>
	/// <returns>The computed corner height, clamped to within 1 unit of tile height.</returns>
	private static int ComputeCornerHeight(byte[,] heightMap, int hx, int hz, int dx, int dz, int tileHeight)
	{
		// Get all 4 tiles sharing this corner
		int h00 = heightMap[hx + dx, hz + dz];
		int h10 = heightMap[hx + dx + 1, hz + dz];
		int h01 = heightMap[hx + dx, hz + dz + 1];
		int h11 = heightMap[hx + dx + 1, hz + dz + 1];

		// Use second-lowest for symmetric slopes
		// Sort the 4 heights and pick index 1
		Span<int> heights = [h00, h10, h01, h11];
		heights.Sort();
		int globalCorner = heights[1];  // Second lowest

		// Clamp symmetrically: corner can't be more than 1 above OR below tile height
		// This creates cliffs when height difference is 2+
		return Math.Clamp(globalCorner, tileHeight - 1, tileHeight + 1);
	}

	/// <summary>
	/// Generates cliff faces on all edges where needed.
	/// </summary>
	private static void GenerateCliffFaces(
		List<Vector3> vertices,
		List<Vector3> normals,
		List<int> indices,
		byte[,] heightMap,
		int hx, int hz,
		float worldX, float worldZ,
		float tileSize, float heightStep,
		float yNW, float yNE, float ySE, float ySW)
	{
		int tileHeight = heightMap[hx, hz];

		// North edge - compare with north neighbor
		int nHeight = heightMap[hx, hz - 1];
		float nYSW = ComputeCornerHeight(heightMap, hx, hz - 1, -1, 0, nHeight) * heightStep;
		float nYSE = ComputeCornerHeight(heightMap, hx, hz - 1, 0, 0, nHeight) * heightStep;
		GenerateCliffFace(vertices, normals, indices,
			new Vector3(worldX, yNW, worldZ),
			new Vector3(worldX + tileSize, yNE, worldZ),
			new Vector3(worldX, nYSW, worldZ),
			new Vector3(worldX + tileSize, nYSE, worldZ),
			new Vector3(0, 0, -1));

		// East edge - compare with east neighbor
		int eHeight = heightMap[hx + 1, hz];
		float eYNW = ComputeCornerHeight(heightMap, hx + 1, hz, -1, -1, eHeight) * heightStep;
		float eYSW = ComputeCornerHeight(heightMap, hx + 1, hz, -1, 0, eHeight) * heightStep;
		GenerateCliffFace(vertices, normals, indices,
			new Vector3(worldX + tileSize, yNE, worldZ),
			new Vector3(worldX + tileSize, ySE, worldZ + tileSize),
			new Vector3(worldX + tileSize, eYNW, worldZ),
			new Vector3(worldX + tileSize, eYSW, worldZ + tileSize),
			new Vector3(1, 0, 0));

		// South edge - compare with south neighbor
		int sHeight = heightMap[hx, hz + 1];
		float sYNW = ComputeCornerHeight(heightMap, hx, hz + 1, -1, -1, sHeight) * heightStep;
		float sYNE = ComputeCornerHeight(heightMap, hx, hz + 1, 0, -1, sHeight) * heightStep;
		GenerateCliffFace(vertices, normals, indices,
			new Vector3(worldX + tileSize, ySE, worldZ + tileSize),
			new Vector3(worldX, ySW, worldZ + tileSize),
			new Vector3(worldX + tileSize, sYNE, worldZ + tileSize),
			new Vector3(worldX, sYNW, worldZ + tileSize),
			new Vector3(0, 0, 1));

		// West edge - compare with west neighbor
		int wHeight = heightMap[hx - 1, hz];
		float wYNE = ComputeCornerHeight(heightMap, hx - 1, hz, 0, -1, wHeight) * heightStep;
		float wYSE = ComputeCornerHeight(heightMap, hx - 1, hz, 0, 0, wHeight) * heightStep;
		GenerateCliffFace(vertices, normals, indices,
			new Vector3(worldX, ySW, worldZ + tileSize),
			new Vector3(worldX, yNW, worldZ),
			new Vector3(worldX, wYSE, worldZ + tileSize),
			new Vector3(worldX, wYNE, worldZ),
			new Vector3(-1, 0, 0));
	}

	/// <summary>
	/// Generates a cliff face quad if there's a height difference.
	/// </summary>
	/// <param name="ourTop1">Our corner 1 (top of cliff).</param>
	/// <param name="ourTop2">Our corner 2 (top of cliff).</param>
	/// <param name="neighborBottom1">Neighbor's corner 1 (bottom of cliff).</param>
	/// <param name="neighborBottom2">Neighbor's corner 2 (bottom of cliff).</param>
	/// <param name="normal">Face normal.</param>
	private static void GenerateCliffFace(
		List<Vector3> vertices,
		List<Vector3> normals,
		List<int> indices,
		Vector3 ourTop1, Vector3 ourTop2,
		Vector3 neighborBottom1, Vector3 neighborBottom2,
		Vector3 normal)
	{
		// Only generate if our edge is higher than neighbor's
		bool needsCliff1 = ourTop1.Y > neighborBottom1.Y + 0.001f;
		bool needsCliff2 = ourTop2.Y > neighborBottom2.Y + 0.001f;

		if (!needsCliff1 && !needsCliff2)
			return;

		int baseIndex = vertices.Count;

		// Quad vertices: bottom-left, bottom-right, top-right, top-left
		vertices.Add(neighborBottom1);
		vertices.Add(neighborBottom2);
		vertices.Add(ourTop2);
		vertices.Add(ourTop1);

		normals.Add(normal);
		normals.Add(normal);
		normals.Add(normal);
		normals.Add(normal);

		// Two triangles
		indices.Add(baseIndex);
		indices.Add(baseIndex + 1);
		indices.Add(baseIndex + 2);

		indices.Add(baseIndex);
		indices.Add(baseIndex + 2);
		indices.Add(baseIndex + 3);
	}

	/// <summary>
	/// Generates the top surface quad from corner positions.
	/// </summary>
	private static void GenerateTopSurface(
		List<Vector3> vertices,
		List<Vector3> normals,
		List<int> indices,
		Vector3 nw, Vector3 ne, Vector3 se, Vector3 sw)
	{
		int baseIndex = vertices.Count;

		// Two triangles along NW-SE diagonal
		Vector3 normal1 = ComputeNormal(nw, ne, se);
		vertices.Add(nw);
		vertices.Add(ne);
		vertices.Add(se);
		normals.Add(normal1);
		normals.Add(normal1);
		normals.Add(normal1);
		indices.Add(baseIndex);
		indices.Add(baseIndex + 1);
		indices.Add(baseIndex + 2);

		Vector3 normal2 = ComputeNormal(nw, se, sw);
		vertices.Add(nw);
		vertices.Add(se);
		vertices.Add(sw);
		normals.Add(normal2);
		normals.Add(normal2);
		normals.Add(normal2);
		indices.Add(baseIndex + 3);
		indices.Add(baseIndex + 4);
		indices.Add(baseIndex + 5);
	}

	private static Vector3 ComputeNormal(Vector3 v0, Vector3 v1, Vector3 v2)
	{
		Vector3 edge1 = v1 - v0;
		Vector3 edge2 = v2 - v0;
		Vector3 normal = edge1.Cross(edge2).Normalized();
		// Ensure normal points upward for top surfaces
		return normal.Y < 0 ? -normal : normal;
	}

	/// <summary>
	/// Gets the terrain height at a world position for object placement.
	/// Uses bilinear interpolation across tile corners.
	/// </summary>
	/// <param name="heightMap">The heightmap with apron.</param>
	/// <param name="worldX">World X position.</param>
	/// <param name="worldZ">World Z position.</param>
	/// <param name="tileSize">World-space size of each tile.</param>
	/// <param name="heightStep">World-space height per height unit.</param>
	/// <param name="chunkX">World X offset of the chunk (in tiles).</param>
	/// <param name="chunkZ">World Z offset of the chunk (in tiles).</param>
	/// <returns>Interpolated terrain height at the given position.</returns>
	public static float GetTerrainHeight(
		byte[,] heightMap,
		float worldX, float worldZ,
		float tileSize, float heightStep,
		int chunkX = 0, int chunkZ = 0)
	{
		// Convert world position to tile coordinates
		float tileXf = worldX / tileSize - chunkX;
		float tileZf = worldZ / tileSize - chunkZ;

		int tileX = (int)Math.Floor(tileXf);
		int tileZ = (int)Math.Floor(tileZf);

		// Position within tile (0 to 1)
		float tx = tileXf - tileX;
		float tz = tileZf - tileZ;

		// Clamp to valid range
		int maxX = heightMap.GetLength(0) - 3;
		int maxZ = heightMap.GetLength(1) - 3;
		tileX = Math.Clamp(tileX, 0, maxX);
		tileZ = Math.Clamp(tileZ, 0, maxZ);

		// Heightmap coordinates
		int hx = tileX + 1;
		int hz = tileZ + 1;
		int tileHeight = heightMap[hx, hz];

		// Get corner heights
		float yNW = ComputeCornerHeight(heightMap, hx, hz, -1, -1, tileHeight) * heightStep;
		float yNE = ComputeCornerHeight(heightMap, hx, hz, 0, -1, tileHeight) * heightStep;
		float ySW = ComputeCornerHeight(heightMap, hx, hz, -1, 0, tileHeight) * heightStep;
		float ySE = ComputeCornerHeight(heightMap, hx, hz, 0, 0, tileHeight) * heightStep;

		// Bilinear interpolation
		float yN = yNW + (yNE - yNW) * tx;
		float yS = ySW + (ySE - ySW) * tx;
		return yN + (yS - yN) * tz;
	}
}
