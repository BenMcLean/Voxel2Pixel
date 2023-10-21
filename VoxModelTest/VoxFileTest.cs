using System.Linq;
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
			VoxFile voxFile1 = new VoxFile(Path);
			Asserts(voxFile1);
			foreach (VoxFile.Chunk chunk in voxFile1.Chunks)
			{
				output.WriteLine(chunk.TagName);
			}
			voxFile1.Write("test.vox");
			VoxFile voxFile2 = new VoxFile("test.vox");
			Asserts(voxFile2);
			VoxFile.SizeChunk sizeA = (VoxFile.SizeChunk)voxFile1.Chunks.Where(chunk => chunk.TagName.Equals("SIZE")).First(),
				sizeB = (VoxFile.SizeChunk)voxFile1.Chunks.Where(chunk => chunk.TagName.Equals("SIZE")).First();
			Assert.Equal(sizeA.SizeX, sizeB.SizeX);
			Assert.Equal(sizeA.SizeY, sizeB.SizeY);
			Assert.Equal(sizeA.SizeZ, sizeB.SizeZ);
		}
		private void Asserts(VoxFile voxFile)
		{
			Assert.Equal(
				expected: 200u,
				actual: voxFile.VersionNumber);
			Assert.Equal(
				expected: "MAIN",
				actual: voxFile.Chunks[0].TagName);
			VoxFile.SizeChunk sizeChunk = (VoxFile.SizeChunk)voxFile.Chunks.Where(chunk => chunk.TagName.Equals("SIZE")).First();
			Assert.Equal(
				expected: 40,
				actual: sizeChunk.SizeX);
			Assert.Equal(
				expected: 40,
				actual: sizeChunk.SizeY);
			Assert.Equal(
				expected: 40,
				actual: sizeChunk.SizeZ);
		}
	}
}
