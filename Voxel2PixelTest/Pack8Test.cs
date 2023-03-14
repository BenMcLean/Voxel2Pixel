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
	}
}
