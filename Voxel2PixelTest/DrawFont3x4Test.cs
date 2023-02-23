using SixLabors.ImageSharp;
using Voxel2Pixel.Draw;
using Xunit;

namespace Voxel2PixelTest
{
	public class DrawFont3x4Test
	{
		[Fact]
		public void A()
		{
			int width = 4, height = 4;
			byte[] texture = new byte[width * 4 * height]
				.Draw3x4(
					@char: 'A',
					width: width,
					x: 0,
					y: 0);
			ImageMaker.Png(
				scaleX: 16,
				scaleY: 16,
				width: width,
				bytes: texture)
				.SaveAsPng("A.png");
		}
	}
}
