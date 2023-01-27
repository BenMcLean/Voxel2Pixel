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
			int xScale = 6,
				yScale = 6,
				width = VoxelDraw.DrawWidth(model),
				height = VoxelDraw.DrawHeight(model);
			ArrayRenderer arrayRenderer = new ArrayRenderer
			{
				Image = new byte[width * 4 * height],
				Width = width,
				//Image = new byte[VoxelDraw.IsoWidth(voxModel) * 4 * VoxelDraw.IsoHeight(voxModel)],
				//Width = VoxelDraw.IsoWidth(voxModel),
				IVoxelColor = new NaiveDimmer(model.Palette),
			};
			VoxelDraw.Draw(model, arrayRenderer);
			//VoxelDraw.DrawIso(voxModel, arrayRenderer);
			Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(
				//data: arrayRenderer.Image,
				//width: arrayRenderer.Width,
				//height: arrayRenderer.Height)
				data: arrayRenderer.Image.Upscale(xScale, yScale, arrayRenderer.Width),
				width: arrayRenderer.Width * xScale,
				height: arrayRenderer.Height * yScale)
				.SaveAsPng("Sora.png");
		}
	}
}
