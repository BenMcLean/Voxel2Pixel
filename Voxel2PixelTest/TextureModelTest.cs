using SixLabors.ImageSharp;
using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Model;
using Voxel2Pixel.Render;
using Xunit;
using static Voxel2Pixel.Draw.TextureMethods;

namespace Voxel2PixelTest
{
	public class TextureModelTest
	{
		[Fact]
		public void ArrayRendererTest()
		{
			int testTextureWidth = 10, testTextureHeight = 32;
			TextureModel model = new TextureModel(TestTexture(testTextureWidth, testTextureHeight), testTextureWidth)
			{
				SizeZ = 5,
			};
			int xScale = 1,
				yScale = 1,
				width = VoxelDraw.AboveWidth(model),
				height = VoxelDraw.AboveHeight(model);
			ArrayRenderer arrayRenderer = new ArrayRenderer
			{
				Image = new byte[width * 4 * height],
				Width = width,
				IVoxelColor = new NaiveDimmer(model.Palette),
			};
			VoxelDraw.Above(model, arrayRenderer);
			ImageMaker.Png(
				scaleX: xScale,
				scaleY: yScale,
				width: width,
				bytes: arrayRenderer.Image)
				.SaveAsPng("TextureModel.png");
		}
		public byte[] TestTexture(int width, int height) =>
			new byte[16] {
				255,0,0,255,
				0,255,0,255,
				0,0,255,255,
				128,128,128,255}
			.Upscale(width / 2, height / 2, 2)
			.DrawRectangle(0, 0, 0, 255, width / 4, height / 4, width / 2, height / 2, width);
	}
}
