using SixLabors.ImageSharp;
using Xunit;
using static Voxel2Pixel.TextureMethods;

namespace Voxel2PixelTest
{
	public class TextureMethodsTest
	{
		[Fact]
		public void Test1()
		{
			int width = 2, height = 2, xScale = 40, yScale = 30, xTile = 4, yTile = 3;
			byte[] bytes = new byte[width * height * 4]
			.DrawPixel(255, 0, 0, 255, 0, 0, width)
			.DrawPixel(0, 255, 0, 255, 1, 0, width)
			.DrawPixel(0, 0, 255, 255, 0, 1, width)
			.DrawPixel(255, 255, 255, 255, 1, 1, width)
			.Upscale(xScale, yScale, width)
			.DrawRectangle(0, 0, 0, 255, width * xScale / 4, height * yScale / 4, width * xScale / 4 * 2, height * yScale / 4 * 2, width * xScale)
			.Tile(xTile, yTile, width * xScale);
			Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(bytes, width * xScale * xTile, height * yScale * yTile)
				.SaveAsPng("output.png");
		}
	}
}
