using System.Linq;
using Voxel2Pixel;
using Voxel2Pixel.Color;
using Voxel2Pixel.Model;
using Voxel2Pixel.Pack;
using Xunit;
using SixLabors.ImageSharp;
using Voxel2Pixel.Draw;

namespace Voxel2PixelTest.Pack
{
	public class IndexedSpriteTest
	{
		/*
		private readonly Xunit.Abstractions.ITestOutputHelper output;
		public IndexedSpriteTest(Xunit.Abstractions.ITestOutputHelper output) => this.output = output;
		[Fact]
		public void NumberCubeGif()
		{
			VoxFileModel model = new VoxFileModel(@"..\..\..\NumberCube.vox");
			output.WriteLine(string.Join(",", model.Palette.Select(@uint => @uint.ToString("X"))));
			IndexedSprite.Above4(
				model: model,
				palette: new NaiveDimmer(model.Palette).CreatePalette())
				.Select(sprite => new Sprite(sprite).CropOutline())
				.SameSize()
				.AddFrameNumbers()
				.Select(sprite => sprite.Upscale(8, 8))
				.AnimatedGif(frameDelay: 100)
				.SaveAsGif("NumberCube.gif");
		}
		*/
		[Fact]
		public void RainbowPyramidGif()
		{
			ArrayModel model = new ArrayModel(ImageMaker.Pyramid(17));
			IndexedSprite.Iso4(
				model: model,
				palette: new NaiveDimmer(ImageMaker.RainbowPalette).CreatePalette())
				.Select(sprite => new Sprite(sprite).CropOutline())
				.SameSize()
				.AddFrameNumbers()
				.Select(sprite => sprite.Upscale(8, 8))
				.AnimatedGif(frameDelay: 100)
				.SaveAsGif("RainbowPyramid.gif");
		}
		//[Fact]
		//public void RainbowPyramidGif()
		//{
		//	VoxFileModel model = new VoxFileModel(@"..\..\..\Sora.vox");
		//	ushort[] voxelOrigin = model.BottomCenter();
		//	VoxelDraw.IsoLocate(
		//		pixelX: out int originX,
		//		pixelY: out int originY,
		//		model: model,
		//	Sprite sprite = new Sprite2x(VoxelDraw.IsoWidth(model), (ushort)(VoxelDraw.IsoHeight(model) << 1))
		//	{
		//		VoxelColor = new NaiveDimmer(model.Palette),
		//	};

		//}
	}
}
