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
			output.WriteLine(ExtensionMethods.Utf8Xml(file));
		}
	}
}
