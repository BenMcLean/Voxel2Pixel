using SixLabors.ImageSharp;
using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Model;
using Voxel2Pixel.Model.FileFormats;
using Voxel2Pixel.Render;
using static Voxel2Pixel.Draw.PixelDraw;

namespace Voxel2Pixel.Test.Draw;

public class PixelDrawTest
{
	[Fact]
	public void Test1()
	{
		byte width = 2, height = 2, xScale = 32, yScale = 32, xTile = 1, yTile = 1;
		byte[] bytes = new byte[width * height * 4]
			.DrawPixel(0, 0, 0xFF0000FFu, width)
			.DrawPixel(1, 0, 0x00FF00FFu, width)
			.DrawPixel(0, 1, 0x0000FFFFu, width)
			.DrawPixel(1, 1, 0x808080FFu, width)
			.Upscale(xScale, yScale, width)
			.DrawRectangle(0xFFu, width * xScale / 4, height * yScale / 4, width * xScale / 4 * 2, height * yScale / 4 * 2, (ushort)(width * xScale));
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
		byte[] isoSlant = bytes.IsoSlantDown((ushort)(width * xScale * xTile));
		Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(isoSlant, width * xScale * xTile * 2, isoSlant.Length / (width * xScale * xTile * 8))
			.SaveAsPng("IsoSlantDown.png");
		ushort isoWidth = (ushort)(width * xScale * xTile + height * yScale * yTile);
		byte[] isoTile = bytes
			.DrawPixel(0, 0, 0x800000FFu, (ushort)(width * xScale * xTile))
			.DrawPixel(1, 0, 0x00FF00FFu, (ushort)(width * xScale * xTile))
			.DrawPixel(0, 1, 0x0000FFFFu, (ushort)(width * xScale * xTile))
			.DrawPixel(1, 1, 0x808080FFu, (ushort)(width * xScale * xTile))
			.RotateCounter45((ushort)(width * xScale * xTile));
		ushort isoHeight = (ushort)(isoTile.Length / (isoWidth * 4));
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
		byte width = 10, height = 10, xScale = 10, yScale = 10;
		byte[] bytes = new byte[width * height * 4]
			.DrawRectangle(0xFFu, -5, -5, width + 5, height + 5, width)
			.DrawRectangle(0xFFFFu, 0, 5, 5, 5, width);
		Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(bytes.Upscale(xScale, yScale), width * xScale, height * yScale)
			.SaveAsPng("DrawRectangleTest.png");
	}
	[Fact]
	public void Texture2UInt2D()
	{
		VoxFileModel voxFileModel = new(@"..\..\..\TestData\Models\Tree.vox");
		Sprite sprite = new SpriteMaker
		{
			Model = voxFileModel,
			VoxelColor = new NaiveDimmer(voxFileModel.Palette),
		}.Make();
		byte[] stupid = sprite.Texture.Texture2UInt2D(sprite.Width).UInt2D2Texture();
		Assert.Equal(sprite.Texture.Length, stupid.Length);
		for (int x = 0; x < sprite.Texture.Length; x++)
			Assert.Equal(sprite.Texture[x], stupid[x]);
	}
	[Fact]
	public void Crop2ContentInfo()
	{
		byte[][][] bytes = TestData.TestData.Arch(46);
		bytes[1][0][0] = 1;
		Sprite sprite = new SpriteMaker
		{
			Model = new ArrayModel(bytes),
			VoxelColor = new NaiveDimmer(TestData.TestData.RainbowPalette),
		}.Make();
		sprite.Texture.Crop2ContentInfo(
			cutLeft: out ushort cutLeft,
			cutTop: out ushort cutTop,
			croppedWidth: out ushort croppedWidth,
			croppedHeight: out ushort croppedHeight,
			width: sprite.Width);
		Crop2ContentInfo2(
			texture: sprite.Texture,
			cutLeft: out ushort cutLeft2,
			cutTop: out ushort cutTop2,
			croppedWidth: out ushort croppedWidth2,
			croppedHeight: out ushort croppedHeight2,
			width: sprite.Width);
		Assert.Equal(cutLeft, cutLeft2);
		Assert.Equal(cutTop, cutTop2);
		Assert.Equal(croppedWidth, croppedWidth2);
		Assert.Equal(croppedHeight, croppedHeight2);
	}
	protected static void Crop2ContentInfo2(byte[] texture, out ushort cutLeft, out ushort cutTop, out ushort croppedWidth, out ushort croppedHeight, ushort width = 0, byte threshold = DefaultTransparencyThreshold)
	{
		if (width < 1)
			width = (ushort)Math.Sqrt(texture.Length >> 2);
		uint[,] uints = texture.Texture2UInt2D(width);
		int xSide = width << 2,
			indexTop, indexBottom;
		for (indexTop = 3; indexTop < texture.Length && texture[indexTop] < threshold; indexTop += 4) { }
		cutTop = (ushort)(indexTop / xSide);
		indexTop = (ushort)(cutTop * xSide);
		for (indexBottom = texture.Length - 1; indexBottom > indexTop && texture[indexBottom] < threshold; indexBottom -= 4) { }
		int cutBottom = indexBottom / xSide + 1;
		croppedHeight = (ushort)(cutBottom - cutTop);
		cutLeft = (ushort)(width - 1);
		ushort cutRight = 0;
		for (int y = cutTop; y < cutBottom; y++)
		{
			ushort left;
			for (left = 0; left < cutLeft && (byte)uints[left, y] < threshold; left++) { }
			if (left < cutLeft)
				cutLeft = left;
			ushort right;
			for (right = (ushort)(width - 1); right > cutRight && (byte)uints[right, y] < threshold; right--) { }
			if (right > cutRight)
				cutRight = right;
		}
		croppedWidth = (ushort)(cutRight - cutLeft + 1);
	}
}
