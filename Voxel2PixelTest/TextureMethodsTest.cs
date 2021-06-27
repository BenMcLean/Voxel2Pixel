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
			int width = 2, height = 2, scale = 20, tileX = 7, tileY = 6;
			byte[] bytes = new byte[width * height * 4]
			.DrawPixel(255, 0, 0, 255, 0, 0, width)
			.DrawPixel(0, 255, 0, 255, 1, 0, width)
			.DrawPixel(0, 0, 255, 255, 0, 1, width)
			.DrawPixel(255, 255, 255, 255, 1, 1, width)
			.Upscale(scale, width)
			.DrawRectangle(0, 0, 0, 255, width * scale / 4, height * scale / 4, width * scale / 4 * 2, height * scale / 4 * 2, width * scale)
			.TileX(tileX, width * scale)
			.TileY(tileY);
			Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(bytes, width * scale * tileX, height * scale * tileY)
				.SaveAsPng("output.png");
		}
	}
}
