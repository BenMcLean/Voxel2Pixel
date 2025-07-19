using System.Text.Json;
using BenVoxel;
using Voxel2Pixel.Model.FileFormats;
using static BenVoxel.ExtensionMethods;

namespace Voxel2Pixel.Test.Model.BenVoxelTest;

public class BenVoxelFileTest(Xunit.Abstractions.ITestOutputHelper output)
{
	private readonly Xunit.Abstractions.ITestOutputHelper output = output;
	[Fact]
	public void Test()
	{
		BenVoxelFile.Metadata metadata = new();
		for (byte i = 1; i < 4; i++)
		{
			metadata.Properties["Property" + i] = "PropertyValue" + i;
			metadata.Points["Point" + i] = new Point3D(i, i, i);
			metadata.Palettes["Palette" + i] = [.. Enumerable.Range(0, 3).Select(j => new BenVoxelFile.Color
			{
				Rgba = i,
				Description = "",
			})];
		}
		DictionaryModel model = new(new VoxFileModel(@"..\..\..\TestData\Models\Sora.vox"));
		BenVoxelFile file = new()
		{
			Global = metadata
		};
		file.Models[""] = new BenVoxelFile.Model()
		{
			Metadata = metadata,
			Geometry = new DictionaryModel(),
		};
		file.Models["Another"] = new BenVoxelFile.Model()
		{
			Metadata = metadata,
			Geometry = model,
		};
		string s = JsonSerializer.Serialize(file, BenVoxelFile.JsonSerializerOptions);
		output.WriteLine(s);
		Assert.Equal(
			expected: s,
			actual: JsonSerializer.Serialize(JsonSerializer.Deserialize<BenVoxelFile>(s), BenVoxelFile.JsonSerializerOptions));
	}
	//[Fact]
	//public void RiffTest() => output.WriteLine(Convert.ToHexString(new Point3D(1, 2, 3).RIFF("PT3D").ToArray()));
}
