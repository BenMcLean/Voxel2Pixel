using Voxel2Pixel.Model.BenVoxel;

namespace Voxel2Pixel.Test.Model.BenVoxel
{
	public class BenVoxelMetadataTest(Xunit.Abstractions.ITestOutputHelper output)
	{
		private readonly Xunit.Abstractions.ITestOutputHelper output = output;
		[Fact]
		public void Test()
		{
			BenVoxelMetadata metadata = new();
			for (byte i = 1; i < 4; i++)
			{
				metadata.Properties["Property" + i] = "PropertyValue" + i;
				metadata.Points["Point" + i] = new Voxel2Pixel.Model.Point3D(i, i, i);
				metadata.Palettes["Palette" + i] = [i, i, i];
			}
			output.WriteLine(ExtensionMethods.Utf8Xml(metadata));
		}
	}
}
