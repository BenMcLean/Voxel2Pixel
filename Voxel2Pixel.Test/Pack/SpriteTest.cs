using SixLabors.ImageSharp;
using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;
using Voxel2Pixel.Model.FileFormats;
using Voxel2Pixel.Render;
using static Voxel2Pixel.Web.ImageMaker;

namespace Voxel2Pixel.Test.Pack
{
	public class SpriteTest(Xunit.Abstractions.ITestOutputHelper output)
	{
		[Fact]
		public void SoraGif()
		{
			VoxFileModel model = new(@"..\..\..\Sora.vox");
			new SpriteMaker
			{
				Model = model,
				VoxelColor = new NaiveDimmer(model.Palette),
				Outline = true,
			}
				.Iso8()
				.Make()
				.Select(sprite => sprite.DrawPoint())
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
			new SpriteMaker
			{
				Model = model,
				VoxelColor = new NaiveDimmer(model.Palette),
				Shadow = true,
				Outline = true,
			}
				//.Set(new Point3D(0, 0, 0))
				.SetShadowColor(0xFFu)
				.Iso8()
				.Make()
				.Select(sprite => sprite.DrawPoint())
				//.Select(Origin0)
				.SameSize()
				.AddFrameNumbers()
				.Select(sprite => sprite.Upscale(8, 8))
				.AnimatedGif()
				.SaveAsGif("Shadows.gif");
		}
		[Fact]
		public void CropTest()
		{
			VoxFileModel voxFileModel = new(@"..\..\..\Tree.vox");
			output.WriteLine(string.Join(", ", voxFileModel.SizeX, voxFileModel.SizeY, voxFileModel.SizeZ));
			new SpriteMaker
			{
				Model = voxFileModel,
				VoxelColor = new NaiveDimmer(voxFileModel.Palette),
			}.Make()
				.Png()
				.SaveAsPng("Tree.png");
		}
		[Fact]
		public void ArchTest()
		{
			byte[][][] bytes = TestData.Arch(80);
			bytes[1][0][0] = 1;
			new SpriteMaker
			{
				Model = new ArrayModel(bytes),
				VoxelColor = new NaiveDimmer(TestData.RainbowPalette),
			}.Make()
				.Png()
				.SaveAsPng("Arch.png");
		}
		public static Sprite Origin0(Sprite sprite)
		{
			sprite[Sprite.Origin] = new Voxel2Pixel.Model.Point(0, 0);
			return sprite;
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
			Enumerable.Range(0, numSprites)
				.Select(i => sprite
					.Rotate(Math.Tau * ((double)i / numSprites))
					.DrawPoint("dot"))
				.Select(Origin0)
				.AnimatedGif(10)
				.SaveAsGif("Rotate.gif");
		}
		[Fact]
		public void Stacked()
		{
			VoxFileModel voxFileModel = new(@"..\..\..\Tree.vox");
			IVoxelColor voxelColor = new FlatVoxelColor(voxFileModel.Palette);
			new SpriteMaker
			{
				Model = voxFileModel,
				VoxelColor = voxelColor,
				Outline = true,
				Shadow = true,
			}
				.SetShadowColor(0xFFu)
				.Stacks(24)
				.Make()
				.Select(sprite => sprite.DrawPoint())
				//.Select(Origin0)
				.SameSize()
				.AddFrameNumbers()
				.Select(sprite => sprite.Upscale(8, 8))
				.AnimatedGif(50)
				.SaveAsGif("Stacked.gif");
		}
	}
}
