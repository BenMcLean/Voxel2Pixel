using System.Collections.Generic;
using Godot;

namespace BenVoxelGpu;

/// <summary>
/// Generates renderable terrain meshes from heightfield and surface map data.
/// Implements marching-squares-style mesh synthesis with surface patches and cliff faces.
/// </summary>
public static class TerrainMeshBuilder
{
	/// <summary>
	/// Builds an ArrayMesh for a terrain chunk.
	/// </summary>
	/// <param name="heightMap">Height values with one-tile apron around the chunk</param>
	/// <param name="surfaceMap">Surface types for interior tiles (from TerrainClassifier)</param>
	/// <param name="chunkX">World X offset for this chunk (in tiles)</param>
	/// <param name="chunkZ">World Z offset for this chunk (in tiles)</param>
	/// <param name="chunkSize">Size of chunk in tiles (surfaceMap should be this size)</param>
	/// <param name="tileSize">World-space size of one tile</param>
	/// <param name="heightStep">World-space height of one height unit</param>
	/// <returns>An ArrayMesh ready for use with MeshInstance3D</returns>
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

		// Generate geometry for each tile
		for (int iz = 0; iz < chunkSize; iz++)
		{
			for (int ix = 0; ix < chunkSize; ix++)
			{
				// Heightmap coordinates include the apron offset
				int hx = ix + 1,
					hz = iz + 1;

				TileSurfaceType surfaceType = surfaceMap[ix, iz];
				int height = heightMap[hx, hz];

				// World position of tile corner (min X, min Z)
				float worldX = (chunkX + ix) * tileSize,
					worldZ = (chunkZ + iz) * tileSize,
					worldY = height * heightStep;

				// Generate top surface
				GenerateTopSurface(vertices, normals, indices, surfaceType,
					worldX, worldY, worldZ, tileSize, heightStep);

				// Generate cliff faces for each edge where neighbor is lower
				// North edge (z - 1)
				int neighborHeight = heightMap[hx, hz - 1];
				if (neighborHeight < height && !HasSlopeOnEdge(surfaceType, Direction.North))
					GenerateCliffFace(vertices, normals, indices, Direction.North,
						worldX, worldZ, height, neighborHeight, tileSize, heightStep, surfaceType);

				// East edge (x + 1)
				neighborHeight = heightMap[hx + 1, hz];
				if (neighborHeight < height && !HasSlopeOnEdge(surfaceType, Direction.East))
					GenerateCliffFace(vertices, normals, indices, Direction.East,
						worldX, worldZ, height, neighborHeight, tileSize, heightStep, surfaceType);

				// South edge (z + 1)
				neighborHeight = heightMap[hx, hz + 1];
				if (neighborHeight < height && !HasSlopeOnEdge(surfaceType, Direction.South))
					GenerateCliffFace(vertices, normals, indices, Direction.South,
						worldX, worldZ, height, neighborHeight, tileSize, heightStep, surfaceType);

				// West edge (x - 1)
				neighborHeight = heightMap[hx - 1, hz];
				if (neighborHeight < height && !HasSlopeOnEdge(surfaceType, Direction.West))
					GenerateCliffFace(vertices, normals, indices, Direction.West,
						worldX, worldZ, height, neighborHeight, tileSize, heightStep, surfaceType);
			}
		}

		// Build the mesh
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
	/// Checks if a slope type occupies a given edge (meaning no cliff should be generated there).
	/// </summary>
	private static bool HasSlopeOnEdge(TileSurfaceType surfaceType, Direction edge) =>
		surfaceType switch
		{
			TileSurfaceType.CardinalSlopeNorth => edge == Direction.North,
			TileSurfaceType.CardinalSlopeEast => edge == Direction.East,
			TileSurfaceType.CardinalSlopeSouth => edge == Direction.South,
			TileSurfaceType.CardinalSlopeWest => edge == Direction.West,
			TileSurfaceType.DiagonalSlopeNE => edge == Direction.North || edge == Direction.East,
			TileSurfaceType.DiagonalSlopeSE => edge == Direction.South || edge == Direction.East,
			TileSurfaceType.DiagonalSlopeSW => edge == Direction.South || edge == Direction.West,
			TileSurfaceType.DiagonalSlopeNW => edge == Direction.North || edge == Direction.West,
			_ => false
		};

	/// <summary>
	/// Generates the top surface geometry for a tile.
	/// </summary>
	private static void GenerateTopSurface(
		List<Vector3> vertices,
		List<Vector3> normals,
		List<int> indices,
		TileSurfaceType surfaceType,
		float x, float y, float z,
		float tileSize, float heightStep)
	{
		int baseIndex = vertices.Count;

		// Corner positions in Godot coordinates (Y-up)
		// Tile corners: NW, NE, SE, SW (going clockwise from top-left when viewed from above)
		// In Godot: -Z is North, +X is East
		Vector3 nw = new(x, y, z),                           // North-West (min X, min Z)
			ne = new(x + tileSize, y, z),                    // North-East (max X, min Z)
			se = new(x + tileSize, y, z + tileSize),         // South-East (max X, max Z)
			sw = new(x, y, z + tileSize);                    // South-West (min X, max Z)

		float lowY = y - heightStep;  // Height one step lower (for slopes)

		switch (surfaceType)
		{
			case TileSurfaceType.Flat:
				// Simple flat quad
				AddQuad(vertices, normals, indices, baseIndex,
					nw, ne, se, sw, Vector3.Up);
				break;

			case TileSurfaceType.CardinalSlopeNorth:
				// Slopes down to north - north edge is lower
				AddQuad(vertices, normals, indices, baseIndex,
					nw with { Y = lowY }, ne with { Y = lowY }, se, sw,
					ComputeNormal(nw with { Y = lowY }, ne with { Y = lowY }, se));
				break;

			case TileSurfaceType.CardinalSlopeEast:
				// Slopes down to east - east edge is lower
				AddQuad(vertices, normals, indices, baseIndex,
					nw, ne with { Y = lowY }, se with { Y = lowY }, sw,
					ComputeNormal(nw, ne with { Y = lowY }, se with { Y = lowY }));
				break;

			case TileSurfaceType.CardinalSlopeSouth:
				// Slopes down to south - south edge is lower
				AddQuad(vertices, normals, indices, baseIndex,
					nw, ne, se with { Y = lowY }, sw with { Y = lowY },
					ComputeNormal(nw, ne, se with { Y = lowY }));
				break;

			case TileSurfaceType.CardinalSlopeWest:
				// Slopes down to west - west edge is lower
				AddQuad(vertices, normals, indices, baseIndex,
					nw with { Y = lowY }, ne, se, sw with { Y = lowY },
					ComputeNormal(nw with { Y = lowY }, ne, se));
				break;

			case TileSurfaceType.DiagonalSlopeNE:
				// Slopes down to NE corner - NE corner is lowest
				// This is a triangular slope with NE corner at lower height
				AddTriangle(vertices, normals, indices, baseIndex,
					nw, ne with { Y = lowY }, sw,
					ComputeNormal(nw, ne with { Y = lowY }, sw));
				AddTriangle(vertices, normals, indices, baseIndex + 3,
					ne with { Y = lowY }, se, sw,
					ComputeNormal(ne with { Y = lowY }, se, sw));
				break;

			case TileSurfaceType.DiagonalSlopeSE:
				// Slopes down to SE corner
				AddTriangle(vertices, normals, indices, baseIndex,
					ne, se with { Y = lowY }, nw,
					ComputeNormal(ne, se with { Y = lowY }, nw));
				AddTriangle(vertices, normals, indices, baseIndex + 3,
					se with { Y = lowY }, sw, nw,
					ComputeNormal(se with { Y = lowY }, sw, nw));
				break;

			case TileSurfaceType.DiagonalSlopeSW:
				// Slopes down to SW corner
				AddTriangle(vertices, normals, indices, baseIndex,
					se, sw with { Y = lowY }, ne,
					ComputeNormal(se, sw with { Y = lowY }, ne));
				AddTriangle(vertices, normals, indices, baseIndex + 3,
					sw with { Y = lowY }, nw, ne,
					ComputeNormal(sw with { Y = lowY }, nw, ne));
				break;

			case TileSurfaceType.DiagonalSlopeNW:
				// Slopes down to NW corner
				AddTriangle(vertices, normals, indices, baseIndex,
					sw, nw with { Y = lowY }, se,
					ComputeNormal(sw, nw with { Y = lowY }, se));
				AddTriangle(vertices, normals, indices, baseIndex + 3,
					nw with { Y = lowY }, ne, se,
					ComputeNormal(nw with { Y = lowY }, ne, se));
				break;
		}
	}

	/// <summary>
	/// Generates a cliff face on the specified edge.
	/// </summary>
	private static void GenerateCliffFace(
		List<Vector3> vertices,
		List<Vector3> normals,
		List<int> indices,
		Direction edge,
		float x, float z,
		int topHeight, int bottomHeight,
		float tileSize, float heightStep,
		TileSurfaceType surfaceType)
	{
		int baseIndex = vertices.Count;
		float topY = topHeight * heightStep,
			bottomY = bottomHeight * heightStep;

		// Determine if top edge of cliff should be lowered due to slope
		float topY1 = topY, topY2 = topY;

		// For cardinal slopes, the edge on the slope side is one step lower
		// For diagonal slopes, check if this edge is part of the slope
		switch (surfaceType)
		{
			case TileSurfaceType.CardinalSlopeNorth when edge == Direction.North:
			case TileSurfaceType.CardinalSlopeEast when edge == Direction.East:
			case TileSurfaceType.CardinalSlopeSouth when edge == Direction.South:
			case TileSurfaceType.CardinalSlopeWest when edge == Direction.West:
				// These cases are already filtered out by HasSlopeOnEdge
				return;

			case TileSurfaceType.DiagonalSlopeNE:
				if (edge == Direction.North) topY2 = topY - heightStep;      // NE corner lower
				else if (edge == Direction.East) topY1 = topY - heightStep;  // NE corner lower
				break;
			case TileSurfaceType.DiagonalSlopeSE:
				if (edge == Direction.East) topY2 = topY - heightStep;       // SE corner lower
				else if (edge == Direction.South) topY1 = topY - heightStep; // SE corner lower
				break;
			case TileSurfaceType.DiagonalSlopeSW:
				if (edge == Direction.South) topY2 = topY - heightStep;      // SW corner lower
				else if (edge == Direction.West) topY1 = topY - heightStep;  // SW corner lower
				break;
			case TileSurfaceType.DiagonalSlopeNW:
				if (edge == Direction.West) topY2 = topY - heightStep;       // NW corner lower
				else if (edge == Direction.North) topY1 = topY - heightStep; // NW corner lower
				break;
		}

		Vector3 v0, v1, v2, v3, normal;

		switch (edge)
		{
			case Direction.North:
				// Face pointing -Z (north)
				normal = new Vector3(0, 0, -1);
				v0 = new Vector3(x, bottomY, z);              // Bottom-left
				v1 = new Vector3(x + tileSize, bottomY, z);   // Bottom-right
				v2 = new Vector3(x + tileSize, topY2, z);     // Top-right (may be lower for NW slope)
				v3 = new Vector3(x, topY1, z);                // Top-left (may be lower for NE slope)
				break;

			case Direction.East:
				// Face pointing +X (east)
				normal = new Vector3(1, 0, 0);
				v0 = new Vector3(x + tileSize, bottomY, z);              // Bottom-left (north end)
				v1 = new Vector3(x + tileSize, bottomY, z + tileSize);   // Bottom-right (south end)
				v2 = new Vector3(x + tileSize, topY2, z + tileSize);     // Top-right
				v3 = new Vector3(x + tileSize, topY1, z);                // Top-left
				break;

			case Direction.South:
				// Face pointing +Z (south)
				normal = new Vector3(0, 0, 1);
				v0 = new Vector3(x + tileSize, bottomY, z + tileSize);   // Bottom-left
				v1 = new Vector3(x, bottomY, z + tileSize);              // Bottom-right
				v2 = new Vector3(x, topY2, z + tileSize);                // Top-right
				v3 = new Vector3(x + tileSize, topY1, z + tileSize);     // Top-left
				break;

			case Direction.West:
				// Face pointing -X (west)
				normal = new Vector3(-1, 0, 0);
				v0 = new Vector3(x, bottomY, z + tileSize);   // Bottom-left (south end)
				v1 = new Vector3(x, bottomY, z);              // Bottom-right (north end)
				v2 = new Vector3(x, topY2, z);                // Top-right
				v3 = new Vector3(x, topY1, z + tileSize);     // Top-left
				break;

			default:
				return;
		}

		// Only generate if there's actual height difference
		if (topY1 > bottomY || topY2 > bottomY)
			AddQuad(vertices, normals, indices, baseIndex, v0, v1, v2, v3, normal);
	}

	/// <summary>
	/// Adds a quad (two triangles) to the mesh data.
	/// Vertices should be in counter-clockwise order when viewed from the front.
	/// </summary>
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

		// Two triangles: 0-1-2, 0-2-3
		indices.Add(baseIndex);
		indices.Add(baseIndex + 1);
		indices.Add(baseIndex + 2);

		indices.Add(baseIndex);
		indices.Add(baseIndex + 2);
		indices.Add(baseIndex + 3);
	}

	/// <summary>
	/// Adds a triangle to the mesh data.
	/// </summary>
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

	/// <summary>
	/// Computes the normal for a triangle.
	/// </summary>
	private static Vector3 ComputeNormal(Vector3 v0, Vector3 v1, Vector3 v2)
	{
		Vector3 edge1 = v1 - v0,
			edge2 = v2 - v0;
		return edge1.Cross(edge2).Normalized();
	}
}
