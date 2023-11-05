using Voxel2Pixel.Model;
using Xunit;

namespace Voxel2PixelTest.Model
{
	public class FullModelTest
	{
		[Fact]
		public void FullModel()
		{
			FullModel model = new FullModel
			{
				SizeX = 10,
				SizeY = 10,
				SizeZ = 10,
				Voxel = 1,
			};
			int i = 0;
			foreach (Voxel voxel in model)
				i++;
			Assert.Equal(
				expected: 1000,
				actual: i);
		}
	}
}
