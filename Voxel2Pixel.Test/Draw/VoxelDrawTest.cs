using SixLabors.ImageSharp;
using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Model;
using Voxel2Pixel.Model.FileFormats;
using Voxel2Pixel.Render;
using static Voxel2Pixel.Web.ImageMaker;

namespace Voxel2Pixel.Test.Draw;

public class VoxelDrawTest
{
	[Fact]
	public void ZSliceTest()
	{
		VoxFileModel voxFileModel = new(@"..\..\..\TestData\Models\NumberCube.vox");
		Sprite sprite = new(voxFileModel.Size(Perspective.Iso))
		{
			VoxelColor = new NaiveDimmer(voxFileModel.Palette),
		};
		voxFileModel.Draw(sprite, Perspective.Iso);
		sprite["dot"] = new Voxel2Pixel.Model.Point(sprite.Width / 4, 3 * sprite.Height / 4);
		sprite.DrawBoundingBox();
		sprite.Png().SaveAsPng("ZSlice.png");
		TextureModel textureModel = new(sprite);
		int numSprites = 64;
		byte scaleX = 6, scaleY = 6;
		Enumerable.Range(0, numSprites)
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
				};
				VoxelDraw.ZSlice(
					model: textureModel,
					renderer: sprite,
					radians: radians,
					z: 0,
					scaleX: scaleX,
					scaleY: scaleY);
				return sprite.DrawBoundingBox();
			})
			.AnimatedGif(10)
			.SaveAsGif("ZSlice.gif");
	}
}
