using BenVoxel;
using System.Text;
using System.Xml.Serialization;
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
				Argb = i,
				Description = "",
			})];
		}
		SvoModel model = new(new VoxFileModel(@"..\..\..\TestData\Models\Sora.vox"));
		BenVoxelFile file = new()
		{
			Global = metadata
		};
		file.Models[""] = new BenVoxelFile.Model()
		{
			Metadata = metadata,
			Geometry = new SvoModel(),
		};
		file.Models["Another"] = new BenVoxelFile.Model()
		{
			Metadata = metadata,
			Geometry = model,
		};
		string s = file.Utf8Xml();
		output.WriteLine(s);
		Assert.Equal(
			expected: s,
			actual: ((BenVoxelFile)(new XmlSerializer(typeof(BenVoxelFile)).Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(s))) ?? throw new NullReferenceException())).Utf8Xml());
	}
	[Fact]
	public void RiffTest() => output.WriteLine(Convert.ToHexString(new Point3D(1, 2, 3).RIFF("PT3D").ToArray()));
}
