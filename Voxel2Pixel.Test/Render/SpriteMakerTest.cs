using SixLabors.ImageSharp;
using Voxel2Pixel.Color;
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
			Perspective = Voxel2Pixel.Model.Perspective.IsoEight,
			Outline = true,
			NumberOfSprites = 8,
		}.MakeGroupAsync())
			sprites.Add(sprite);
		sprites.Select(sprite => sprite.DrawPoint())
			.SameSize()
			.AddFrameNumbers()
			.Select(sprite => sprite.Upscale(8, 8))
			.AnimatedGif(frameDelay: 100)
			.SaveAsGif("Sora.gif");
	}
}
