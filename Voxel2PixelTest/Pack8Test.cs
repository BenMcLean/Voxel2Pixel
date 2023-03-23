using SixLabors.ImageSharp;
using System.Linq;
using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Model;
using Voxel2Pixel.Pack;
using Xunit;

namespace Voxel2PixelTest
{
	public class Pack8Test
	{
		[Fact]
		public void Test()
		{
			VoxModel model = (VoxModel)new VoxModel(@"..\..\..\Sora.vox").DrawBox(1);
			byte[] texture = IsoPacker.Pack8(
				model: model,
				voxelColor: new NaiveDimmer(model.Palette),
				width: out int width,
				packingRectangles: out _);
			ImageMaker.Png(
				width: width,
				bytes: texture)
			.SaveAsPng("Pack8Test.png");
		}
		[Fact]
		public void IsoSpritesTest()
		{
			VoxModel model = new VoxModel(@"..\..\..\Sora.vox");
			IsoPacker.IsoSprites(
				model: model,
				voxelColor: new NaiveDimmer(model.Palette),
				sprites: out byte[][] sprites,
				widths: out int[] widths,
				origins: out _);
			int width = widths.Max(),
				height = Enumerable.Range(0, sprites.Length).Select(i => PixelDraw.Height(sprites[i].Length, widths[i])).Max();
			ImageMaker.AnimatedGifScaled(
				scaleX: 4,
				scaleY: 4,
				width: width,
				frames: Enumerable.Range(0, sprites.Length)
					.Select(i => sprites[i].Resize(
						newWidth: width,
						newHeight: height,
						width: widths[i]))
					.ToArray(),
				frameDelay: 200)
			.SaveAsGif("IsoSpritesTest.gif");
		}
		[Fact]
		public void PyramidTest()
		{
			ArrayModel model = new ArrayModel(Pyramid(17));
			IsoPacker.IsoSprites(
				model: model,
				voxelColor: new NaiveDimmer(ArrayModelTest.RainbowPalette),
				sprites: out byte[][] sprites,
				widths: out int[] widths,
				origins: out _);
			int width = widths.Max(),
				height = Enumerable.Range(0, sprites.Length).Select(i => PixelDraw.Height(sprites[i].Length, widths[i])).Max();
			ImageMaker.AnimatedGifScaled(
				scaleX: 4,
				scaleY: 4,
				width: width,
				frames: Enumerable.Range(0, sprites.Length)
					.Select(i => sprites[i].Resize(
						newWidth: width,
						newHeight: height,
						width: widths[i]))
					.ToArray(),
				frameDelay: 200)
			.SaveAsGif("PyramidTest.gif");
		}
		public static byte[][][] Pyramid(int width, params byte[] colors)
		{
			if (colors is null || colors.Length < 1)
				colors = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
			int halfWidth = width >> 1;
			byte[][][] voxels = ArrayModel.MakeModel(width, width, halfWidth + 1);
			voxels[0][0][0] = colors[1];
			voxels[width - 1][0][0] = colors[2];
			voxels[0][width - 1][0] = colors[3];
			voxels[width - 1][width - 1][0] = colors[4];
			for (int i = 0; i <= halfWidth; i++)
			{
				//voxels[i][i][i] = colors[1];
				//voxels[width - 1 - i][i][i] = colors[2];
				//voxels[i][width - 1 - i][i] = colors[3];
				//voxels[width - 1 - i][width - 1 - i][i] = colors[4];
				voxels[halfWidth][halfWidth][i] = colors[5];
			}
			return voxels;
		}
	}
}
