using System.Linq;
using Voxel2Pixel;
using Voxel2Pixel.Color;
using Voxel2Pixel.Model;
using Voxel2Pixel.Pack;
using Xunit;
using SixLabors.ImageSharp;

namespace Voxel2PixelTest.Pack
{
	public class IndexedSpriteTest
	{
		[Fact]
		public void NumberCubeGif()
		{
			VoxFileModel model = new VoxFileModel(@"..\..\..\NumberCube.vox");
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
	}
}
