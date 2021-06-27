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
			int width = 100, height = 100, scale = 1;
			byte[] bytes = new byte[width * height * 4]
				//.DrawPixel(255, 0, 0, 255, 0, 0, width)
				//.DrawPixel(0, 255, 0, 255, 0, 1, width)
				//.DrawPixel(0, 0, 255, 255, 1, 0, width)
				//.DrawPixel(255, 255, 255, 255, 1, 1, width)
				.DrawRectangle(0, 0, 255, 255, 10, 10, 80, 80, width);
			//.Upscale(scale, width);
			Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(bytes, width * scale, height * scale)
				.SaveAsPng("output.png");
		}
	}
}
