using SixLabors.ImageSharp;
using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Model;
using Voxel2Pixel.Render;
using Xunit;
using static Voxel2Pixel.TextureMethods;

namespace Voxel2PixelTest
{
	public class VoxModelTest
	{
		const string path = @"..\..\..\Sora.vox";
		[Fact]
		public void ArrayRendererTest()
		{
			VoxModel model = new VoxModel(path);
			//EmptyModel model = new EmptyModel
			//{
			//	SizeX = 5,
			//	SizeY = 8,
			//	SizeZ = 3,
			//};
			int xScale = 12,
				yScale = 12,
				width = VoxelDraw.IsoWidth(model),
				height = VoxelDraw.IsoHeight(model);
			ArrayRenderer arrayRenderer = new ArrayRenderer
			{
				Image = new byte[width * 4 * height],
				Width = width,
				IVoxelColor = new NaiveDimmer(model.Palette),
			};
			VoxelDraw.Iso(model, arrayRenderer);
			Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(
				data: arrayRenderer.Image.Upscale(xScale, yScale, arrayRenderer.Width),
				width: arrayRenderer.Width * xScale,
				height: arrayRenderer.Height * yScale)
				.SaveAsPng("Sora.png");
		}
		[Fact]
		public void TextureModelTest()
		{
			int testTextureWidth = 32, testTextureHeight = 32;
			TextureModel model = new TextureModel(TestTexture(testTextureWidth, testTextureHeight), testTextureWidth);
			int xScale = 12,
				yScale = 12,
				width = VoxelDraw.AboveWidth(model),
				height = VoxelDraw.AboveHeight(model);
			ArrayRenderer arrayRenderer = new ArrayRenderer
			{
				Image = new byte[width * 4 * height],
				Width = width,
				IVoxelColor = new NaiveDimmer(model.Palette),
			};
			VoxelDraw.Above(model, arrayRenderer);
			Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(
				data: arrayRenderer.Image.Upscale(xScale, yScale, arrayRenderer.Width),
				width: arrayRenderer.Width * xScale,
				height: arrayRenderer.Height * yScale)
				.SaveAsPng("TextureModel.png");
		}
		public byte[] TestTexture(int width, int height) =>
			new byte[2 * 4 * 2]
				.DrawPixel(255, 0, 0, 255, 0, 0, 2)
				.DrawPixel(0, 255, 0, 255, 1, 0, 2)
				.DrawPixel(0, 0, 255, 255, 0, 1, 2)
				.DrawPixel(128, 128, 128, 255, 1, 1, 2)
				.Upscale(width / 2, height / 2, 2)
				.DrawRectangle(0, 0, 0, 255, width / 4, height / 4, width / 2, height / 2, width);
	}
}
