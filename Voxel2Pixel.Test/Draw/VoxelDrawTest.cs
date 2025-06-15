using SixLabors.ImageSharp;
using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Model;
using Voxel2Pixel.Model.FileFormats;
using Voxel2Pixel.Render;
using Voxel2Pixel.Test.TestData;
using static Voxel2Pixel.Test.TestData.ImageMaker;

namespace Voxel2Pixel.Test.Draw;

public class VoxelDrawTest(Xunit.Abstractions.ITestOutputHelper output)
{
	[Fact]
	public async Task ZSliceTest()
	{
		VoxFileModel voxFileModel = new(@"..\..\..\TestData\Models\NumberCube.vox");
		Sprite sprite = new(voxFileModel.Size(Perspective.Iso))
		{
			VoxelColor = new NaiveDimmer(voxFileModel.Palette),
		};
		await VoxelDraw.DrawAsync(voxFileModel, sprite, Perspective.Iso);
		Voxel2Pixel.Model.Point dot = new(sprite.Width / 4, 3 * sprite.Height / 4);
		sprite.DrawBoundingBox();
		sprite.Png().SaveAsPng("ZSlice.png");
		TextureModel textureModel = new(sprite);
		int numSprites = 64;
		byte scaleX = 1, scaleY = 1;
		List<Sprite> sprites = [.. Enumerable.Range(0, numSprites)
			.Parallelize(i =>
			{
				double radians = Math.Tau * ((double)i / numSprites);
				Sprite sprite = new(VoxelDraw.ZSliceSize(
					model: textureModel,
					radians: radians,
					scaleX: scaleX,
					scaleY: scaleY))
				{
					VoxelColor = new NaiveDimmer(textureModel.Palette),
					["dot"] = VoxelDraw.ZSliceLocate(
						model: textureModel,
						point: dot,
						radians: radians,
						scaleX: scaleX,
						scaleY: scaleY),
				};
				VoxelDraw.ZSlice(
					model: textureModel,
					renderer: sprite,
					radians: radians,
					z: 0,
					scaleX: scaleX,
					scaleY: scaleY);
				return sprite.DrawPoint("dot").DrawBoundingBox();
			})];
		ushort i = 0;
		foreach (Sprite frame in sprites)
		{
			i++;
			output.WriteLine($"Frame {i}: {frame["dot"]}");
			frame.Png().SaveAsPng($"frame{i:D2}.png");
		}
		sprites.AnimatedGif(10).SaveAsGif("ZSlice.gif");
	}
}
