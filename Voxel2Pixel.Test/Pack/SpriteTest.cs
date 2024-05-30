using SixLabors.ImageSharp;
using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Interfaces;
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
			new SpriteFactory
			{
				Model = model,
				VoxelColor = new NaiveDimmer(model.Palette),
				Outline = true,
			}
				.Iso8()
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
			new SpriteFactory
			{
				Model = model,
				VoxelColor = new NaiveDimmer(model.Palette),
				Shadow = true,
				Outline = true,
			}
				.Iso8()
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
			new SpriteFactory
			{
				Model = voxFileModel,
				VoxelColor = new NaiveDimmer(voxFileModel.Palette),
			}.Build()
				.Png()
				.SaveAsPng("Tree.png");
		}
		[Fact]
		public void ArchTest()
		{
			byte[][][] bytes = TestData.Arch(80);
			bytes[1][0][0] = 1;
			new SpriteFactory
			{
				Model = new ArrayModel(bytes),
				VoxelColor = new NaiveDimmer(TestData.RainbowPalette),
			}.Build()
				.Png()
				.SaveAsPng("Arch.png");
		}
		[Fact]
		public void RotateTest()
		{
			VoxFileModel voxFileModel = new(@"..\..\..\NumberCube.vox");
			Sprite sprite = new(VoxelDraw.Size(Perspective.Iso, voxFileModel))
			{
				VoxelColor = new NaiveDimmer(voxFileModel.Palette),
			};
			sprite.Rect(0, 0, 0xFFFFu, sprite.Width, sprite.Height);
			VoxelDraw.Draw(Perspective.Iso, voxFileModel, sprite);
			sprite["dot"] = new Voxel2Pixel.Model.Point(sprite.Width / 4, 3 * sprite.Height / 4);
			int numSprites = 64;
			Sprite origin(Sprite sprite)
			{
				sprite[Sprite.Origin] = new Voxel2Pixel.Model.Point(0, 0);
				return sprite;
			}
			Enumerable.Range(0, numSprites)
				.Select(i => sprite
					.Rotate(Math.PI * 2d * ((double)i / numSprites))
					.DrawPoint("dot"))
				.Select(origin)
				.AnimatedGif()
				.SaveAsGif("Rotate.gif");
		}
		[Fact]
		public void Stacked()
		{
			VoxFileModel voxFileModel = new(@"..\..\..\Tree.vox");
			IVoxelColor voxelColor = new FlatVoxelColor(voxFileModel.Palette);
			int numSprites = 64;
			List<Sprite> sprites = [];
			for (int i = 0; i < numSprites; i++)
			{
				double radians = Math.PI * 2d * ((double)i / numSprites);
				Sprite sprite = new(VoxelDraw.StackedSize(voxFileModel, radians))
				{
					VoxelColor = voxelColor,
				};
				VoxelDraw.Stacked(voxFileModel, sprite, radians);
				sprites.Add(sprite.Upscale(8, 8));
			}
			sprites
				.AnimatedGif(10)
				.SaveAsGif("Stacked.gif");
		}
	}
}
