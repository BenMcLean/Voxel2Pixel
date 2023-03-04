using SixLabors.ImageSharp;
using Voxel2Pixel.Color;
using Voxel2Pixel.Model;
using Voxel2Pixel.Pack;
using Xunit;

namespace Voxel2PixelTest
{
	public class Pack8Test
	{
		[Fact]
		public void Test()
		{
			VoxModel model = new VoxModel(@"..\..\..\Sora.vox");
			byte[] texture = IsoPacker.Pack8(
				model: model,
				voxelColor: new NaiveDimmer(model.Palette),
				width: out int width,
				packingRectangles: out _);
			ImageMaker.Png(
				width: width,
				bytes: texture)
			.SaveAsPng("Pack8Test.png");
		}
	}
}
