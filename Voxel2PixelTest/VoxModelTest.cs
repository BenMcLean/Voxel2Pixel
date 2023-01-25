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
			int xScale = 32, yScale = 32;
			VoxModel voxModel = new VoxModel(path);
			ArrayRenderer arrayRenderer = new ArrayRenderer
			{
				Image = new byte[voxModel.SizeX * 4 * voxModel.SizeY],
				Width = voxModel.SizeX,
				//Image = new byte[VoxelDraw.IsoWidth(voxModel) * 4 * VoxelDraw.IsoHeight(voxModel)],
				//Width = VoxelDraw.IsoWidth(voxModel),
				IVoxelColor = new FlatVoxelColor
				{
					Palette = voxModel.Palette,
				},
			};
			VoxelDraw.DrawRight(voxModel, arrayRenderer);
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
