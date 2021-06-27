using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Voxel2Pixel.TextureMethods;

namespace Voxel2PixelTest
{
	[TestClass]
	public class TextureMethodsTest
	{
		[TestMethod]
		public void TestMethod1()
		{
			int x = 2, y = 2, scale = 100;
			new byte[x * y * 4]
				.DrawPixel(255, 0, 0, 255, 0, 0, x)
				.DrawPixel(0, 255, 0, 255, 0, 1, x)
				.DrawPixel(0, 0, 255, 255, 1, 0, x)
				.DrawPixel(255, 255, 255, 255, 1, 1, x)
				.Upscale(scale)
				.Bitmap(x * scale)
				.Save("output.png", System.Drawing.Imaging.ImageFormat.Png);
		}
	}
}
