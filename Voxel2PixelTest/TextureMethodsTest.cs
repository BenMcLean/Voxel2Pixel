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
			int width = 2, height = 2, xScale = 50, yScale = 50, xTile = 1, yTile = 1;
			byte[] bytes = new byte[width * height * 4]
			.DrawPixel(255, 0, 0, 255, 0, 0, width)
			.DrawPixel(0, 255, 0, 255, 1, 0, width)
			.DrawPixel(0, 0, 255, 255, 0, 1, width)
			.DrawPixel(255, 255, 255, 255, 1, 1, width)
			.Upscale(xScale, yScale, width)
			.DrawRectangle(0, 0, 0, 255, width * xScale / 4, height * yScale / 4, width * xScale / 4 * 2, height * yScale / 4 * 2, width * xScale)
			.Tile(xTile, yTile, width * xScale);
			byte[] crop = bytes.Crop(10, 15, 25, 20, width * xScale * xTile);
			Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(crop, 20, 20)
				.SaveAsPng("cropped.png");
			Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(bytes.Insert(95, 95, crop, 25, width * xScale * xTile), width * xScale * xTile, height * yScale * yTile)
				.SaveAsPng("output.png");
		}
	}
}
