using SixLabors.ImageSharp;
using Voxel2Pixel.Color;
using Voxel2Pixel.Model;
using Voxel2Pixel.Pack;
using static Voxel2Pixel.Web.ImageMaker;

namespace Voxel2Pixel.Test.Pack
{
	public class SpriteTest
	{
		[Fact]
		public void SoraGif()
		{
			VoxFileModel model = new(@"..\..\..\Sora.vox");
			Sprite.Iso8(
					model: model,
					voxelColor: new NaiveDimmer(model.Palette),
					outline: true)
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
				.SameSize()
				.AddFrameNumbers()
				.Select(sprite => sprite.Upscale(8, 8))
				.AnimatedGif(frameDelay: 100)
				.SaveAsGif("Shadows.gif");
		}
	}
}
