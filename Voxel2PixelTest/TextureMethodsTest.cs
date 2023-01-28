using SixLabors.ImageSharp;
using Voxel2Pixel;
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
			.DrawRectangle(0, 0, 0, 255, width * xScale / 4, height * yScale / 4, width * xScale / 4 * 2, height * yScale / 4 * 2, width * xScale);
			//.DrawTriangle(0, 128, 0, 255, 10, 10, 40, 40, width * xScale);
			//.Rotate180(width * xScale);
			//int swap = xScale;
			//xScale = yScale;
			//yScale = swap;
			//byte[] crop = bytes.Crop(65, 70, 25, 20, width * xScale * xTile);
			//Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(crop, 20, 20)
			//	.SaveAsPng("cropped.png");

			Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(bytes, width * xScale * xTile, height * yScale * yTile)
				.SaveAsPng("output.png");
			byte[] isoSlant = bytes.IsoSlantDown(width * xScale * xTile);
			Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(isoSlant, width * xScale * xTile * 2, isoSlant.Length / (width * xScale * xTile * 8))
			.SaveAsPng("IsoSlantDown.png");
			int isoWidth = (width * xScale * xTile + height * yScale * yTile);
			byte[] isoTile = bytes
				.DrawPixel(128, 0, 0, 255, 0, 0, width * xScale * xTile)
				.DrawPixel(0, 255, 0, 255, 1, 0, width * xScale * xTile)
				.DrawPixel(0, 0, 255, 255, 0, 1, width * xScale * xTile)
				.DrawPixel(128, 128, 128, 255, 1, 1, width * xScale * xTile)
				.RotateCounter45(width * xScale * xTile);
			int isoHeight = isoTile.Length / (isoWidth * 4);
			Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(isoTile, isoWidth, isoHeight)
			.SaveAsPng("rotated.png");
			//Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(bytes.Resize(800, 600, width * xScale * xTile), 800, 600)
			//	.SaveAsPng("800x600.png");
			byte[] stamp = (byte[])isoTile.Clone();
			isoTile = isoTile
				.DrawTransparentInsert(
					x: isoWidth / -2,
					y: isoHeight / -2,
					insert: stamp,
					insertWidth: isoWidth,
					threshold: 1,
					width: isoWidth)
				.DrawTransparentInsert(
					x: isoWidth / -2,
					y: isoHeight / 2,
					insert: stamp,
					insertWidth: isoWidth,
					threshold: 1,
					width: isoWidth)
				.DrawTransparentInsert(
					x: isoWidth / 2,
					y: isoHeight / -2,
					insert: stamp,
					insertWidth: isoWidth,
					threshold: 1,
					width: isoWidth)
				.DrawTransparentInsert(
					x: isoWidth / 2,
					y: isoHeight / 2,
					insert: stamp,
					insertWidth: isoWidth,
					threshold: 1,
					width: isoWidth);
			Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(isoTile, isoWidth, isoHeight)
			.SaveAsPng("stamped.png");
		}
		[Fact]
		public void DrawRectangleTest()
		{
			int width = 10, height = 10, xScale = 10, yScale = 10;
			byte[] bytes = new byte[width * height * 4]
				.DrawRectangle(0, 0, 0, 255, -5, -5, width + 5, height + 5, width)
				.DrawRectangle(0, 0, 255, 255, 0, 5, 5, 5, width);
			Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(bytes.Upscale(xScale, yScale), width * xScale, height * yScale)
				.SaveAsPng("DrawRectangleTest.png");
		}
	}
}
