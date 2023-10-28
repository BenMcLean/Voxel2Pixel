using SixLabors.ImageSharp;
using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Model;
using Voxel2Pixel.Render;
using Xunit;

namespace Voxel2PixelTest.Render
{
	public class TinyTriangleRendererTest
	{
		[Fact]
		public void TinyTest()
		{
			VoxFileModel model = new VoxFileModel(@"..\..\..\Sora.vox");
			int width = VoxelDraw.IsoWidth(model) / 2,
				height = VoxelDraw.IsoHeight(model) / 4;
			ArrayRenderer arrayRenderer = new ArrayRenderer
			{
				Image = new byte[width * 4 * height],
				Width = width,
				VoxelColor = new NaiveDimmer(model.Palette),
			};
			TinyTriangleRenderer tinyTriangleRenderer = new TinyTriangleRenderer
			{
				RectangleRenderer = arrayRenderer,
				VoxelColor = arrayRenderer.VoxelColor,
			};
			VoxelDraw.Iso(model, tinyTriangleRenderer);
			ImageMaker.Png(
				//scaleX: 32,
				//scaleY: 32,
				width: width,
				bytes: arrayRenderer.Image)
				.SaveAsPng("TinyTriangleRendererTest.png");
		}
	}
}
