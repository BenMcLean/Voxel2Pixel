using VoxModel;
using Xunit;

namespace VoxModelTest
{
	public class VoxFileTest
	{
		private readonly Xunit.Abstractions.ITestOutputHelper output;
		public VoxFileTest(Xunit.Abstractions.ITestOutputHelper output) => this.output = output;
		public const string Path = @"..\..\..\..\Voxel2PixelTest\NumberCube.vox";
		[Fact]
		public void MagicaVoxelFileTest()
		{
			VoxFile voxModel = new VoxFile(Path);
			Asserts(voxModel);
			foreach (VoxFile.Chunk chunk in voxModel.Main.Chunks())
			{
				output.WriteLine(chunk.TagName);
			}
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
