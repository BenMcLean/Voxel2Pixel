using System.Collections.ObjectModel;
using Xunit.Abstractions;

namespace BenVoxel.Test;

public class CollapseTests(ITestOutputHelper testOutputHelper)
{
	private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;
	private static readonly ReadOnlyCollection<object[]> RegionSizesData = Array.AsReadOnly<object[]>([
		[(ushort)1, (ushort)1, (ushort)1],    // Single voxel
		[(ushort)2, (ushort)2, (ushort)2],    // Single leaf node
		[(ushort)4, (ushort)4, (ushort)4],    // Multiple leaf nodes
		[(ushort)8, (ushort)8, (ushort)8],    // Tests branch collapsing
		[(ushort)16, (ushort)16, (ushort)16], // Larger region
	]);
	public static IEnumerable<object[]> RegionSizes() => RegionSizesData;
	[Theory]
	[MemberData(nameof(RegionSizes))]
	public void TestBranchCollapse(ushort sizeX, ushort sizeY, ushort sizeZ)
	{
		const byte colorIndex = 1;
		SvoModel uniformModel = new(sizeX, sizeY, sizeZ),
			mixedModel = new(sizeX, sizeY, sizeZ);

		// Fill models - one uniform, one mixed
		for (ushort x = 0; x < sizeX; x++)
			for (ushort y = 0; y < sizeY; y++)
				for (ushort z = 0; z < sizeZ; z++)
				{
					uniformModel[x, y, z] = colorIndex;
					mixedModel[x, y, z] = (byte)((x + y + z) % 2 == 0 ? 1 : 2); // Checkerboard
				}

		// Get serialized sizes
		byte[] uniformBytes, mixedBytes;
		using (MemoryStream uniformStream = new(), mixedStream = new())
		{
			uniformModel.Write(uniformStream);
			mixedModel.Write(mixedStream);
			uniformBytes = uniformStream.ToArray();
			mixedBytes = mixedStream.ToArray();
		}

		// For models bigger than a leaf node, uniform fills should serialize smaller
		if (sizeX > 2 && sizeY > 2 && sizeZ > 2)
			Assert.True(
				condition: uniformBytes.Length < mixedBytes.Length,
				userMessage: $"Expected uniform model ({uniformBytes.Length} bytes) to be smaller than mixed model ({mixedBytes.Length} bytes) for size {sizeX}x{sizeY}x{sizeZ}");

		// Verify the models still work correctly after serialization/deserialization
		SvoModel deserializedModel;
		using (MemoryStream stream = new(uniformBytes))
		{
			deserializedModel = new(stream);
		}
		for (ushort x = 0; x < sizeX; x++)
			for (ushort y = 0; y < sizeY; y++)
				for (ushort z = 0; z < sizeZ; z++)
					Assert.Equal(
						expected: colorIndex,
						actual: deserializedModel[x, y, z]);
	}
	[Theory]
	[InlineData(4)]    // 4x4x4 should show significant compression
	[InlineData(8)]    // 8x8x8 should show even more compression
	[InlineData(16)]   // 16x16x16 should show dramatic compression
	public void TestCompressionRatios(ushort size)
	{
		// Create models of same size with contrasting fill patterns
		SvoModel uniformModel = new(size, size, size),
			checkerModel = new(size, size, size);

		// Fill with different patterns
		for (ushort x = 0; x < size; x++)
			for (ushort y = 0; y < size; y++)
				for (ushort z = 0; z < size; z++)
				{
					uniformModel[x, y, z] = 1;  // Completely uniform
					checkerModel[x, y, z] = (byte)((x + y + z) % 2 == 0 ? 1 : 2);  // Alternating
				}

		// Get serialized sizes
		byte[] uniformBytes = GetSerializedBytes(uniformModel),
			checkerBytes = GetSerializedBytes(checkerModel);

		// Uniform should be smaller than checker
		Assert.True(
			condition: uniformBytes.Length < checkerBytes.Length,
			userMessage: $"{size}^3: Uniform ({uniformBytes.Length} bytes) should be smaller than checker ({checkerBytes.Length} bytes)");

		// Log the compression ratios for analysis
		double uniformVoxelRatio = (double)uniformBytes.Length / (size * size * size),
			checkerVoxelRatio = (double)checkerBytes.Length / (size * size * size);

		_testOutputHelper.WriteLine($"Size {size}^3:");
		_testOutputHelper.WriteLine($"Uniform bytes per voxel: {uniformVoxelRatio:F3}");
		_testOutputHelper.WriteLine($"Checker bytes per voxel: {checkerVoxelRatio:F3}");
	}
	private static byte[] GetSerializedBytes(SvoModel model)
	{
		using MemoryStream stream = new();
		model.Write(stream);
		return stream.ToArray();
	}
}
