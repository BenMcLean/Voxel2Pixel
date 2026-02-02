using System.Collections.Generic;
using Godot;

namespace BenVoxelGpu;

/// <summary>
/// Generates renderable terrain meshes from heightfield and surface map data.
/// Uses corner-height computation for seamless tile connections.
/// </summary>
public static class TerrainMeshBuilder
{
	/// <summary>
	/// Builds an ArrayMesh for a terrain chunk.
	/// </summary>
	public static ArrayMesh BuildChunkMesh(
		byte[,] heightMap,
		TileSurfaceType[,] surfaceMap,
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
				int hx = ix + 1,
					hz = iz + 1;

				TileSurfaceType surfaceType = surfaceMap[ix, iz];
				int baseHeight = heightMap[hx, hz];

				float worldX = (chunkX + ix) * tileSize,
					worldZ = (chunkZ + iz) * tileSize,
					baseY = baseHeight * heightStep;

				// Compute corner height offsets based on surface type
				GetCornerOffsets(surfaceType, out int oNW, out int oNE, out int oSE, out int oSW);

				// Compute actual corner heights
				float yNW = baseY + oNW * heightStep,
					yNE = baseY + oNE * heightStep,
					ySE = baseY + oSE * heightStep,
					ySW = baseY + oSW * heightStep;

				// Corner positions
				Vector3 nw = new(worldX, yNW, worldZ),
					ne = new(worldX + tileSize, yNE, worldZ),
					se = new(worldX + tileSize, ySE, worldZ + tileSize),
					sw = new(worldX, ySW, worldZ + tileSize);

				// Generate top surface
				GenerateTopSurface(vertices, normals, indices, nw, ne, se, sw);

				// Generate cliff faces for each edge
				int nHeight = heightMap[hx, hz - 1],
					eHeight = heightMap[hx + 1, hz],
					sHeight = heightMap[hx, hz + 1],
					wHeight = heightMap[hx - 1, hz];

				// Get neighbor corner offsets for proper cliff face generation
				TileSurfaceType nType = hz > 1 ? surfaceMap[ix, iz - 1] : TileSurfaceType.Flat;
				TileSurfaceType eType = ix < chunkSize - 1 ? surfaceMap[ix + 1, iz] : TileSurfaceType.Flat;
				TileSurfaceType sType = iz < chunkSize - 1 ? surfaceMap[ix, iz + 1] : TileSurfaceType.Flat;
				TileSurfaceType wType = ix > 0 ? surfaceMap[ix - 1, iz] : TileSurfaceType.Flat;

				// North edge cliff
				GenerateCliffFace(vertices, normals, indices,
					worldX, worldZ, tileSize, heightStep,
					yNW, yNE, nHeight, nType, Direction.North);

				// East edge cliff
				GenerateCliffFace(vertices, normals, indices,
					worldX, worldZ, tileSize, heightStep,
					yNE, ySE, eHeight, eType, Direction.East);

				// South edge cliff
				GenerateCliffFace(vertices, normals, indices,
					worldX, worldZ, tileSize, heightStep,
					ySE, ySW, sHeight, sType, Direction.South);

				// West edge cliff
				GenerateCliffFace(vertices, normals, indices,
					worldX, worldZ, tileSize, heightStep,
					ySW, yNW, wHeight, wType, Direction.West);
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

	private enum Direction { North, East, South, West }

	/// <summary>
	/// Gets corner height offsets (0 or -1) for a surface type.
	/// Key principle: Higher tiles slope DOWN to meet lower neighbors.
	/// Offsets are always 0 (at base) or -1 (lowered to meet neighbor).
	/// Order: NW, NE, SE, SW
	/// </summary>
	private static void GetCornerOffsets(TileSurfaceType type, out int oNW, out int oNE, out int oSE, out int oSW)
	{
		// Default: all corners at base height
		oNW = oNE = oSE = oSW = 0;

		switch (type)
		{
			// === Single Edge Slopes (one neighbor is 1 lower) ===
			// The edge shared with the lower neighbor goes down
			case TileSurfaceType.SlopeN:
				oNW = oNE = -1;
				break;
			case TileSurfaceType.SlopeE:
				oNE = oSE = -1;
				break;
			case TileSurfaceType.SlopeS:
				oSE = oSW = -1;
				break;
			case TileSurfaceType.SlopeW:
				oSW = oNW = -1;
				break;

			// === Corner (two adjacent neighbors are 1 lower) ===
			// Both edges to lower neighbors go down; only the opposite corner stays at base
			case TileSurfaceType.CornerNE:
				// N and E neighbors are lower; N edge (NW,NE) and E edge (NE,SE) go down
				oNW = oNE = oSE = -1;  // Only SW stays at base
				break;
			case TileSurfaceType.CornerSE:
				// E and S neighbors are lower
				oNE = oSE = oSW = -1;  // Only NW stays at base
				break;
			case TileSurfaceType.CornerSW:
				// S and W neighbors are lower
				oSE = oSW = oNW = -1;  // Only NE stays at base
				break;
			case TileSurfaceType.CornerNW:
				// N and W neighbors are lower
				oSW = oNW = oNE = -1;  // Only SE stays at base
				break;

			// === Valley (three neighbors are 1 lower) and Peak (all four are 1 lower) ===
			// All corners go down to meet the lower neighbors
			case TileSurfaceType.ValleyN:
			case TileSurfaceType.ValleyE:
			case TileSurfaceType.ValleyS:
			case TileSurfaceType.ValleyW:
			case TileSurfaceType.Peak:
				oNW = oNE = oSE = oSW = -1;
				break;

			// Flat and unknown
			case TileSurfaceType.Flat:
			default:
				// All at base height (already set to 0)
				break;
		}
	}

	/// <summary>
	/// Generates the top surface geometry from corner positions.
	/// </summary>
	private static void GenerateTopSurface(
		List<Vector3> vertices,
		List<Vector3> normals,
		List<int> indices,
		Vector3 nw, Vector3 ne, Vector3 se, Vector3 sw)
	{
		int baseIndex = vertices.Count;

		// Standard quad - split along NW-SE diagonal for consistent triangulation
		AddTriangle(vertices, normals, indices, baseIndex,
			nw, ne, se, ComputeNormal(nw, ne, se));
		AddTriangle(vertices, normals, indices, baseIndex + 3,
			nw, se, sw, ComputeNormal(nw, se, sw));
	}

	/// <summary>
	/// Generates a cliff face on the specified edge if needed.
	/// </summary>
	private static void GenerateCliffFace(
		List<Vector3> vertices,
		List<Vector3> normals,
		List<int> indices,
		float x, float z,
		float tileSize, float heightStep,
		float ourCorner1Y, float ourCorner2Y,
		int neighborBaseHeight, TileSurfaceType neighborType,
		Direction edge)
	{
		// Get neighbor's corner heights on the shared edge
		GetCornerOffsets(neighborType, out int nONW, out int nONE, out int nOSE, out int nOSW);
		float neighborBaseY = neighborBaseHeight * heightStep;

		float neighborCorner1Y, neighborCorner2Y;

		switch (edge)
		{
			case Direction.North:
				// Our N edge (NW, NE) meets neighbor's S edge (SW, SE)
				neighborCorner1Y = neighborBaseY + nOSW * heightStep;  // Their SW = our NW side
				neighborCorner2Y = neighborBaseY + nOSE * heightStep;  // Their SE = our NE side
				break;
			case Direction.East:
				// Our E edge (NE, SE) meets neighbor's W edge (NW, SW)
				neighborCorner1Y = neighborBaseY + nONW * heightStep;  // Their NW = our NE side
				neighborCorner2Y = neighborBaseY + nOSW * heightStep;  // Their SW = our SE side
				break;
			case Direction.South:
				// Our S edge (SE, SW) meets neighbor's N edge (NE, NW)
				neighborCorner1Y = neighborBaseY + nONE * heightStep;  // Their NE = our SE side
				neighborCorner2Y = neighborBaseY + nONW * heightStep;  // Their NW = our SW side
				break;
			case Direction.West:
				// Our W edge (SW, NW) meets neighbor's E edge (SE, NE)
				neighborCorner1Y = neighborBaseY + nOSE * heightStep;  // Their SE = our SW side
				neighborCorner2Y = neighborBaseY + nONE * heightStep;  // Their NE = our NW side
				break;
			default:
				return;
		}

		// Check if we need a cliff face (our edge higher than neighbor's)
		bool needsCliff1 = ourCorner1Y > neighborCorner1Y + 0.001f;
		bool needsCliff2 = ourCorner2Y > neighborCorner2Y + 0.001f;

		if (!needsCliff1 && !needsCliff2)
			return;

		int baseIndex = vertices.Count;

		// Generate cliff face vertices
		Vector3 v0, v1, v2, v3, normal;

		switch (edge)
		{
			case Direction.North:
				normal = new Vector3(0, 0, -1);
				v0 = new Vector3(x, neighborCorner1Y, z);                    // Bottom-left
				v1 = new Vector3(x + tileSize, neighborCorner2Y, z);         // Bottom-right
				v2 = new Vector3(x + tileSize, ourCorner2Y, z);              // Top-right
				v3 = new Vector3(x, ourCorner1Y, z);                         // Top-left
				break;
			case Direction.East:
				normal = new Vector3(1, 0, 0);
				v0 = new Vector3(x + tileSize, neighborCorner1Y, z);         // Bottom-left
				v1 = new Vector3(x + tileSize, neighborCorner2Y, z + tileSize);
				v2 = new Vector3(x + tileSize, ourCorner2Y, z + tileSize);
				v3 = new Vector3(x + tileSize, ourCorner1Y, z);
				break;
			case Direction.South:
				normal = new Vector3(0, 0, 1);
				v0 = new Vector3(x + tileSize, neighborCorner1Y, z + tileSize);
				v1 = new Vector3(x, neighborCorner2Y, z + tileSize);
				v2 = new Vector3(x, ourCorner2Y, z + tileSize);
				v3 = new Vector3(x + tileSize, ourCorner1Y, z + tileSize);
				break;
			case Direction.West:
				normal = new Vector3(-1, 0, 0);
				v0 = new Vector3(x, neighborCorner1Y, z + tileSize);
				v1 = new Vector3(x, neighborCorner2Y, z);
				v2 = new Vector3(x, ourCorner2Y, z);
				v3 = new Vector3(x, ourCorner1Y, z + tileSize);
				break;
			default:
				return;
		}

		// Only add the quad if there's actual height difference
		if ((v3.Y - v0.Y) > 0.001f || (v2.Y - v1.Y) > 0.001f)
		{
			AddQuad(vertices, normals, indices, baseIndex, v0, v1, v2, v3, normal);
		}
	}

	private static void AddQuad(
		List<Vector3> vertices,
		List<Vector3> normals,
		List<int> indices,
		int baseIndex,
		Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3,
		Vector3 normal)
	{
		vertices.Add(v0);
		vertices.Add(v1);
		vertices.Add(v2);
		vertices.Add(v3);

		normals.Add(normal);
		normals.Add(normal);
		normals.Add(normal);
		normals.Add(normal);

		indices.Add(baseIndex);
		indices.Add(baseIndex + 1);
		indices.Add(baseIndex + 2);

		indices.Add(baseIndex);
		indices.Add(baseIndex + 2);
		indices.Add(baseIndex + 3);
	}

	private static void AddTriangle(
		List<Vector3> vertices,
		List<Vector3> normals,
		List<int> indices,
		int baseIndex,
		Vector3 v0, Vector3 v1, Vector3 v2,
		Vector3 normal)
	{
		vertices.Add(v0);
		vertices.Add(v1);
		vertices.Add(v2);

		normals.Add(normal);
		normals.Add(normal);
		normals.Add(normal);

		indices.Add(baseIndex);
		indices.Add(baseIndex + 1);
		indices.Add(baseIndex + 2);
	}

	private static Vector3 ComputeNormal(Vector3 v0, Vector3 v1, Vector3 v2)
	{
		Vector3 edge1 = v1 - v0,
			edge2 = v2 - v0;
		Vector3 normal = edge1.Cross(edge2).Normalized();
		// Ensure normal points upward for top surfaces
		return normal.Y < 0 ? -normal : normal;
	}
}
