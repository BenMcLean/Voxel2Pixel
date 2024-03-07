using SixLabors.ImageSharp;
using Voxel2Pixel.Color;
using Voxel2Pixel.Model;
using Voxel2Pixel.Pack;
using static Voxel2Pixel.ExtensionMethods;
using Xunit;
using System.Linq;
using Voxel2Pixel.Draw;
using System.IO;

namespace Voxel2PixelTest.Pack
{
	public class SpriteTest
	{
		[Fact]
		public void SoraGif()
		{
			VoxFileModel model = new(@"..\..\..\Sora.vox");
			Sprite.Iso8(
					model: model,
					voxelColor: new NaiveDimmer(model.Palette))
				.Select(sprite => sprite.CropOutline())
				.SameSize()
				.AddFrameNumbers()
				.Select(sprite => sprite.Upscale(8, 8))
				.AnimatedGif(frameDelay: 100)
				.SaveAsGif("Sora.gif");
		}
		[Fact]
		public void OverheadTest()
		{
			VoxFileModel model = new(@"..\..\..\Sora.vox");
			Sprite sprite = new(VoxelDraw.OverheadWidth(model), VoxelDraw.OverheadHeight(model))
			{
				VoxelColor = new NaiveDimmer(model.Palette),
			};
			VoxelDraw.Overhead(model, sprite);
			sprite.Png().SaveAsPng("OverheadTest.png");
		}
		[Fact]
		public void ShadowTest()
		{
			VoxFileModel model = new(@"..\..\..\NumberCube.vox");
			Sprite sprite = new(VoxelDraw.IsoShadowWidth(model), VoxelDraw.IsoShadowHeight(model))
			{
				VoxelColor = new NaiveDimmer(model.Palette),
			};
			VoxelDraw.IsoShadow(model, sprite);
			sprite.Png().SaveAsPng("IsoShadowTest.png");
		}
		[Fact]
		public void IsoTest()
		{
			VoxFileModel model = new(@"..\..\..\Sora.vox");
			Sprite sprite = new(VoxelDraw.IsoWidth(model), VoxelDraw.IsoHeight(model))
			{
				VoxelColor = new NaiveDimmer(model.Palette),
			};
			VoxelDraw.Iso(model, sprite);
			sprite.Png().SaveAsPng("IsoTest.png");
		}
	}
}
