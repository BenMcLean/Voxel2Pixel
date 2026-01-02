using SixLabors.ImageSharp;
using Voxel2Pixel.Color;
using Voxel2Pixel.ImageSharp;
using Voxel2Pixel.Model.FileFormats;
using Voxel2Pixel.Render;
using Voxel2Pixel.Test.TestData;

namespace Voxel2Pixel.Test.Render;

public class SpriteMakerTest
{
	[Fact]
	public async Task SoraGif()
	{
		VoxFileModel model = new(@"..\..\..\TestData\Models\Sora.vox");
		List<Sprite> sprites = [];
		await foreach (Sprite sprite in new SpriteMaker
		{
			Model = model,
			VoxelColor = new NaiveDimmer(model.Palette),
			Perspective = Voxel2Pixel.Model.Perspective.Iso,
			Peak = true,
			Outline = true,
			NumberOfSprites = 4,
			ScaleX = 3,
			ScaleY = 3,
			ScaleZ = 3,
		}.MakeGroupAsync())
			sprites.Add(sprite);
		sprites.Select(sprite => sprite.DrawPoint())
			.SameSize()
			.AddFrameNumbers()
			//.Parallelize(sprite => sprite.Upscale(8, 8))
			.AnimatedGif(frameDelay: 25)
			.SaveAsGif("Sora.gif");
	}
}
