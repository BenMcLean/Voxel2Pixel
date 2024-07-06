using System.Text;
using System.Xml.Serialization;
using Voxel2Pixel.Model;
using Voxel2Pixel.Model.BenVoxel;
using Voxel2Pixel.Model.FileFormats;

namespace Voxel2Pixel.Test.Model.BenVoxel
{
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
				metadata.Points["Point" + i] = new Voxel2Pixel.Model.Point3D(i, i, i);
				metadata.Palettes["Palette" + i] = [i, i, i];
			}
			SvoModel model = new(new VoxFileModel(@"..\..\..\Sora.vox"));
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
			string s = ExtensionMethods.Utf8Xml(file);
			output.WriteLine(s);
			Assert.Equal(
				expected: s,
				actual: ExtensionMethods.Utf8Xml((BenVoxelFile)(new XmlSerializer(typeof(BenVoxelFile)).Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(s))) ?? throw new NullReferenceException())));
		}
		[Fact]
		public void RiffTest() => output.WriteLine(Convert.ToHexString(new Point3D(1, 2, 3).RIFF("PT3D").ToArray()));
	}
}
