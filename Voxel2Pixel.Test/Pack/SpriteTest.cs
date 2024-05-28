using SixLabors.ImageSharp;
using Voxel2Pixel.Color;
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
		[Fact]
		public void CropTest()
		{
			VoxFileModel voxFileModel = new(@"..\..\..\Tree.vox");
			output.WriteLine(string.Join(", ", voxFileModel.SizeX, voxFileModel.SizeY, voxFileModel.SizeZ));
			new Sprite(
					model: voxFileModel,
					voxelColor: new NaiveDimmer(voxFileModel.Palette))
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
				.Png()
				.SaveAsPng("Arch.png");
		}
		[Fact]
		public void RotateTest()
		{
			VoxFileModel voxFileModel = new(@"..\..\..\NumberCube.vox");
			output.WriteLine(string.Join(", ", voxFileModel.SizeX, voxFileModel.SizeY, voxFileModel.SizeZ));
			Sprite sprite = new(
					model: voxFileModel,
					voxelColor: new NaiveDimmer(voxFileModel.Palette));
			int numSprites = 16;
			Enumerable.Range(0, 6)
				.Select(i => sprite.Rotate(Math.PI * 2d * ((double)i / numSprites)))
				.AnimatedGif()
				.SaveAsGif("Rotate.gif");
		}
	}
}
