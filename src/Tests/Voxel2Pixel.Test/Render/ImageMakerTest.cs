using Voxel2Pixel.Color;
using Voxel2Pixel.Model.FileFormats;
using Voxel2Pixel.Render;
using Voxel2Pixel.Test.TestData;

namespace Voxel2Pixel.Test.Render;

public class ImageMakerTest
{
	[Fact]
	public void PngTest()
	{
		VoxFileModel model = new(@"..\..\..\TestData\Models\Sora.vox");
		new SpriteMaker
		{
			Model = model,
			VoxelColor = new NaiveDimmer(model.Palette),
			Shadow = true,
			Outline = true,
			ScaleX = 2,
		}
			.Make()
			.Png("SkiaSharp.png");
	}
}
