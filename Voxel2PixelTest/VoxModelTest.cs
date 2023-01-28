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
	}
}
