using Voxel2Pixel.Color;
using Voxel2Pixel.Model.FileFormats;
using Voxel2Pixel.Render;
using Voxel2Pixel.Web;

namespace Voxel2Pixel.Test.Render
{
	public class ImageMakerTest
	{
		[Fact]
		public void PngTest()
		{
			using FileStream fs = new(path: "SkiaSharp.png", mode: FileMode.Create, access: FileAccess.Write);
			VoxFileModel model = new(@"..\..\..\Sora.vox");
			new SpriteMaker
			{
				Model = model,
				VoxelColor = new NaiveDimmer(model.Palette),
				Shadow = true,
				Outline = true,
				ScaleX = 2,
			}
				.Make()
				.PngStream()
				.CopyTo(fs);
		}
	}
}
