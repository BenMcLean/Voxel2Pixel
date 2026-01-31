using Godot;

namespace BenVoxelGpu;

/// <summary>
/// Terrain generator and renderer using FastNoiseLite for heightmap generation.
/// Creates a marching-squares-style terrain mesh from procedural noise.
/// </summary>
public partial class Terrain : Node3D
{
	private MeshInstance3D _meshInstance;
	private FastNoiseLite _noise;

	/// <summary>
	/// Size of the terrain in tiles (width and depth).
	/// </summary>
	public int TerrainSize { get; set; } = 64;

	/// <summary>
	/// World-space size of each tile.
	/// </summary>
	public float TileSize { get; set; } = 1.0f;

	/// <summary>
	/// World-space height of one height unit.
	/// </summary>
	public float HeightStep { get; set; } = 0.5f;

	/// <summary>
	/// Maximum height value for the terrain (in height units).
	/// </summary>
	public int MaxHeight { get; set; } = 16;

	/// <summary>
	/// Noise frequency - lower values create smoother terrain.
	/// </summary>
	public float NoiseFrequency { get; set; } = 0.02f;

	/// <summary>
	/// Number of octaves for fractal noise.
	/// </summary>
	public int NoiseOctaves { get; set; } = 4;

	/// <summary>
	/// Seed for noise generation.
	/// </summary>
	public int Seed { get; set; } = 12345;

	public override void _Ready()
	{
		_meshInstance = new MeshInstance3D();
		AddChild(_meshInstance);

		// Create default material
		StandardMaterial3D material = new()
		{
			AlbedoColor = new Color(0.4f, 0.6f, 0.3f), // Grass green
			Roughness = 0.8f,
		};
		_meshInstance.MaterialOverride = material;

		GenerateTerrain();
	}

	/// <summary>
	/// Generates the terrain mesh using FastNoiseLite.
	/// </summary>
	public void GenerateTerrain()
	{
		// Configure noise
		_noise = new FastNoiseLite
		{
			Seed = Seed,
			NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth,
			Frequency = NoiseFrequency,
			FractalType = FastNoiseLite.FractalTypeEnum.Fbm,
			FractalOctaves = NoiseOctaves,
			FractalLacunarity = 2.0f,
			FractalGain = 0.5f,
		};

		// Generate heightmap with apron (terrainSize + 2 on each side)
		int fullSize = TerrainSize + 2;
		byte[,] heightMap = new byte[fullSize, fullSize];

		for (int z = 0; z < fullSize; z++)
		{
			for (int x = 0; x < fullSize; x++)
			{
				// Get noise value in range [-1, 1] and convert to height
				float noiseValue = _noise.GetNoise2D(x, z);
				// Map from [-1, 1] to [0, MaxHeight]
				float normalizedHeight = (noiseValue + 1f) * 0.5f * MaxHeight;
				heightMap[x, z] = (byte)Mathf.Clamp(Mathf.RoundToInt(normalizedHeight), 0, 255);
			}
		}

		// Classify tiles
		TileSurfaceType[,] surfaceMap = TerrainClassifier.ClassifyTiles(heightMap);

		// Build mesh
		ArrayMesh mesh = TerrainMeshBuilder.BuildChunkMesh(
			heightMap,
			surfaceMap,
			chunkX: 0,
			chunkZ: 0,
			chunkSize: TerrainSize,
			tileSize: TileSize,
			heightStep: HeightStep);

		_meshInstance.Mesh = mesh;
	}

	/// <summary>
	/// Regenerates terrain with a new random seed.
	/// </summary>
	public void Randomize()
	{
		Seed = (int)(GD.Randi() % int.MaxValue);
		GenerateTerrain();
	}

	/// <summary>
	/// Gets the world-space bounds of the terrain for camera positioning.
	/// </summary>
	public Aabb GetWorldBounds()
	{
		float width = TerrainSize * TileSize,
			height = MaxHeight * HeightStep;
		return new Aabb(
			new Vector3(0, 0, 0),
			new Vector3(width, height, width));
	}

	/// <summary>
	/// Gets the center of the terrain in world space.
	/// </summary>
	public Vector3 GetCenter()
	{
		float halfWidth = TerrainSize * TileSize * 0.5f,
			halfHeight = MaxHeight * HeightStep * 0.5f;
		return new Vector3(halfWidth, halfHeight, halfWidth);
	}

	/// <summary>
	/// Samples the height at a given world position.
	/// Returns the height in world units, or 0 if outside bounds.
	/// </summary>
	public float SampleHeight(float worldX, float worldZ)
	{
		// Convert to tile coordinates
		int tileX = Mathf.FloorToInt(worldX / TileSize),
			tileZ = Mathf.FloorToInt(worldZ / TileSize);

		if (tileX < 0 || tileX >= TerrainSize || tileZ < 0 || tileZ >= TerrainSize)
			return 0f;

		// Sample noise at this position
		float noiseValue = _noise.GetNoise2D(tileX + 1, tileZ + 1); // +1 for apron offset
		float normalizedHeight = (noiseValue + 1f) * 0.5f * MaxHeight;
		return Mathf.RoundToInt(normalizedHeight) * HeightStep;
	}
}
