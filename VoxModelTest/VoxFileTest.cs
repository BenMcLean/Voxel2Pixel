using VoxModel;
using Xunit;

namespace VoxModelTest
{
	public class VoxFileTest
	{
		public const string Path = @"..\..\..\..\Voxel2PixelTest\NumberCube.vox";
		[Fact]
		public void MagicaVoxelFileTest()
		{
			VoxFile voxModel = new VoxFile(Path);
			Asserts(voxModel);
			voxModel.Write("test.vox");
			Asserts(new VoxFile("test.vox"));
		}
		private void Asserts(VoxFile voxFile)
		{
			Assert.Equal(
				expected: 200u,
				actual: voxFile.VersionNumber);
			Assert.Equal(
				expected: "MAIN",
				actual: voxFile.Main.TagName);
		}
	}
}
