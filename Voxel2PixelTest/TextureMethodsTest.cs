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
			int width = 2, height = 2, xScale = 32, yScale = 32, xTile = 1, yTile = 1;
			byte[] bytes = new byte[width * height * 4]
			.DrawPixel(255, 0, 0, 255, 0, 0, width)
			.DrawPixel(0, 255, 0, 255, 1, 0, width)
			.DrawPixel(0, 0, 255, 255, 0, 1, width)
			.DrawPixel(128, 128, 128, 255, 1, 1, width)
			.Upscale(xScale, yScale, width)
			.DrawRectangle(0, 0, 0, 255, width * xScale / 4, height * yScale / 4, width * xScale / 4 * 2, height * yScale / 4 * 2, width * xScale)
			.Tile(xTile, yTile, width * xScale);
			byte[] crop = bytes.Crop(65, 70, 25, 20, width * xScale * xTile);
			Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(crop, 20, 20)
				.SaveAsPng("cropped.png");
			Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(bytes, width * xScale * xTile, height * yScale * yTile)
				.SaveAsPng("output.png");
			Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(bytes.IsoSlantDown(width * xScale * xTile), width * xScale * xTile * 2, height * yScale * yTile + width * xScale * xTile + 1)
			.SaveAsPng("IsoSlantDown.png");
			//Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(bytes.Resize(800, 600, width * xScale * xTile), 800, 600)
			//	.SaveAsPng("800x600.png");
		}
	}
}
