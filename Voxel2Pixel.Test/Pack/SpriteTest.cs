using SixLabors.ImageSharp;
using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Model;
using Voxel2Pixel.Pack;
using static Voxel2Pixel.Web.ImageMaker;

namespace Voxel2Pixel.Test.Pack
{
	public class SpriteTest(Xunit.Abstractions.ITestOutputHelper output)
	{
		[Fact]
		public void SoraGif()
		{
			VoxFileModel model = new(@"..\..\..\Sora.vox");
			Sprite.Iso8(
					model: model,
					voxelColor: new NaiveDimmer(model.Palette),
					outline: true)
				.Select(sprite => sprite.TransparentCrop())
				.SameSize()
				.AddFrameNumbers()
				.Select(sprite => sprite.Upscale(8, 8))
				.AnimatedGif(frameDelay: 100)
				.SaveAsGif("Sora.gif");
		}
		[Fact]
		public void ShadowGif()
		{
			VoxFileModel model = new(@"..\..\..\Sora.vox");
			Sprite.Iso8(
					model: model,
					voxelColor: new NaiveDimmer(model.Palette),
					shadow: true,
					outline: true)
				.Select(sprite => sprite.TransparentCrop())
				.SameSize()
				.AddFrameNumbers()
				.Select(sprite => sprite.Upscale(8, 8))
				.AnimatedGif(frameDelay: 100)
				.SaveAsGif("Shadows.gif");
		}
		[Fact]
		public void CropTest()
		{
			VoxFileModel voxFileModel = new(@"..\..\..\Tree.vox");
			output.WriteLine(string.Join(", ", voxFileModel.SizeX, voxFileModel.SizeY, voxFileModel.SizeZ));
			new Sprite(
					model: voxFileModel,
					voxelColor: new NaiveDimmer(voxFileModel.Palette))
				.TransparentCrop()
				.Png()
				.SaveAsPng("Tree.png");
		}
		[Fact]
		public void ArchTest()
		{
			byte[][][] bytes = TestData.Arch(80);
			bytes[1][0][0] = 1;
			new Sprite(
					model: new ArrayModel(bytes),
					voxelColor: new NaiveDimmer(TestData.RainbowPalette))
				.TransparentCrop()
				.Png()
				.SaveAsPng("Arch.png");
		}
		[Fact]
		public void TransparentCropInfo()
		{
			byte[][][] bytes = TestData.Arch(46);
			bytes[1][0][0] = 1;
			Sprite sprite = new(
					model: new ArrayModel(bytes),
					voxelColor: new NaiveDimmer(TestData.RainbowPalette));
			sprite.Texture.TransparentCropInfo(
				cutLeft: out ushort cutLeft,
				cutTop: out ushort cutTop,
				croppedWidth: out ushort croppedWidth,
				croppedHeight: out ushort croppedHeight,
				width: sprite.Width);
			TransparentCropInfo2(
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
		protected static void TransparentCropInfo2(byte[] texture, out ushort cutLeft, out ushort cutTop, out ushort croppedWidth, out ushort croppedHeight, ushort width = 0, byte threshold = PixelDraw.DefaultTransparencyThreshold)
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
}
