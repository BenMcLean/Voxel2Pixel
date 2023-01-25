using Voxel2Pixel.Model;
using Xunit;

namespace Voxel2PixelTest
{
	public class VoxModelTest
	{
		const string path = @"..\..\..\Sora.vox";
		[Fact]
		public void ArrayRendererTest()
		{
			VoxModel voxModel = new VoxModel(path);
		}
	}
}
