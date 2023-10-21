using System;
using System.Linq;
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
			Assert.Equal(
				expected: 200u,
				actual: voxModel.VersionNumber);
			voxModel.Write("test.vox");
			VoxFile test = new VoxFile("test.vox");
			Assert.Equal(
				expected: 200u,
				actual: test.VersionNumber);
		}
	}
}
