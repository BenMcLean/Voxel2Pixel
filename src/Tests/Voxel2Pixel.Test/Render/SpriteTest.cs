using SixLabors.ImageSharp;
using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;
using Voxel2Pixel.ImageSharp;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;
using Voxel2Pixel.Model.FileFormats;
using Voxel2Pixel.Render;
using Voxel2Pixel.Test.TestData;
using static Voxel2Pixel.Test.TestData.ImageMaker;

namespace Voxel2Pixel.Test.Render;

public class SpriteTest(Xunit.Abstractions.ITestOutputHelper output)
{
	/*
	[Fact]
	public void ShadowGif()
	{
		VoxFileModel model = new(@"..\..\..\TestData\Models\Sora.vox");
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
	*/
	[Fact]
	public void CropTest()
	{
		VoxFileModel voxFileModel = new(@"..\..\..\TestData\Models\Tree.vox");
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
		byte[][][] bytes = TestData.TestData.Arch(80);
		bytes[1][0][0] = 1;
		new SpriteMaker
		{
			Model = new ArrayModel(bytes),
			VoxelColor = new NaiveDimmer(TestData.TestData.RainbowPalette),
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
		VoxFileModel voxFileModel = new(@"..\..\..\TestData\Models\NumberCube.vox");
		Sprite sprite = new(voxFileModel.Size(Perspective.Iso))
		{
			VoxelColor = new NaiveDimmer(voxFileModel.Palette),
		};
		//voxFileModel.Draw(sprite, Perspective.Iso);
		sprite["dot"] = new Voxel2Pixel.Model.Point(sprite.Width / 4, 3 * sprite.Height / 4);
		sprite.DrawBoundingBox();
		int numSprites = 64;
		List<Sprite> frames = [.. Enumerable.Range(0, numSprites)
			.Parallelize(i => sprite
				.Rotate(Math.Tau * ((double)i / numSprites))
				.DrawPoint("dot"))];
		//byte i = 0;
		//foreach (Sprite frame in frames)
		//	frame.Png().SaveAsPng($"frame{i++}.png");
		frames.AnimatedGif(10)
			.SaveAsGif("Rotate.gif");
	}
	[Fact]
	public void Stacked()
	{
		VoxFileModel voxFileModel = new(@"..\..\..\TestData\Models\Tree.vox");
		IVoxelColor voxelColor = new FlatVoxelColor(voxFileModel.Palette);
		new SpriteMaker
		{
			Model = voxFileModel,
			VoxelColor = voxelColor,
			Outline = true,
			Shadow = true,
		}
			.SetShadowColor(0xFFu)
			.SetPeak(true)
			.SetNumberOfSprites(24)
			.Stacks()
			.Parallelize(spriteMaker => spriteMaker
				.Make()
				.DrawPoint())
			//.Select(Origin0)
			.SameSize()
			.AddFrameNumbers()
			.Parallelize(sprite => sprite.Upscale(8, 8))
			.AnimatedGif(50)
			.SaveAsGif("Stacked.gif");
	}
}
