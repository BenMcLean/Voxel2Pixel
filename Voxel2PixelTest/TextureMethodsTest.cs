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
			int x = 2, y = 2, scale = 100;
			byte[] bytes = new byte[x * y * 4]
				.DrawPixel(255, 0, 0, 255, 0, 0, x)
				.DrawPixel(0, 255, 0, 255, 0, 1, x)
				.DrawPixel(0, 0, 255, 255, 1, 0, x)
				.DrawPixel(255, 255, 255, 255, 1, 1, x)
				.Upscale(scale);
			Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(bytes, x * scale, y * scale)
				.SaveAsPng("output.png");
		}
	}
}
