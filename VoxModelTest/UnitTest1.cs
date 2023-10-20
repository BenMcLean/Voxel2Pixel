using VoxModel;
using Xunit;

namespace VoxModelTest
{
	public class UnitTest1
	{
		public const string Path = @"..\..\..\..\Voxel2PixelTest\NumberCube.vox";
		[Fact]
		public void Test1()
		{
			VoxFile voxModel = new VoxFile(Path);
			Assert.Equal(
				expected: 200u,
				actual: voxModel.VersionNumber);
		}
	}
}
