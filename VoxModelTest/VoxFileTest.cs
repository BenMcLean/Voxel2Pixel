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
			VoxFile voxFile = new VoxFile(Path);
			Asserts(voxFile);
			foreach (VoxFile.Chunk chunk in voxFile.Chunks)
			{
				output.WriteLine(chunk.TagName);
			}
			voxFile.Write("test.vox");
			Asserts(new VoxFile("test.vox"));
		}
		private void Asserts(VoxFile voxFile)
		{
			Assert.Equal(
				expected: 200u,
				actual: voxFile.VersionNumber);
			Assert.Equal(
				expected: "MAIN",
				actual: voxFile.Chunks[0].TagName);
		}
	}
}
