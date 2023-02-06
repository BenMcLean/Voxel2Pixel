using SixLabors.ImageSharp;
using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Model;
using Voxel2Pixel.Render;
using Xunit;

namespace Voxel2PixelTest
{
	public class VoxModelTest
	{
		[Fact]
		public void ArrayRendererTest()
		{
			VoxModel model = new VoxModel(@"..\..\..\Sora.vox");
			int width = VoxelDraw.IsoWidth(model),
				height = VoxelDraw.IsoHeight(model);
			ArrayRenderer arrayRenderer = new ArrayRenderer
			{
				Image = new byte[width * 4 * height],
				Width = width,
				IVoxelColor = new NaiveDimmer(model.Palette),
			};
			VoxelDraw.Iso(model, arrayRenderer);
			ImageMaker.Png(
				scaleX: 32,
				scaleY: 32,
				width: width,
				bytes: arrayRenderer.Image)
				.SaveAsPng("Sora.png");
		}
		[Fact]
		public void CropTest()
		{
			VoxModel model = new VoxModel(@"..\..\..\Sora.vox");
			int width = VoxelDraw.IsoWidth(model),
				height = VoxelDraw.IsoHeight(model);
			ArrayRenderer arrayRenderer = new ArrayRenderer
			{
				Image = new byte[width * 4 * height],
				Width = width,
				IVoxelColor = new NaiveDimmer(model.Palette),
			};
			VoxelDraw.Iso(model, arrayRenderer);
			ImageMaker.Png(
				width: width,
				bytes: arrayRenderer.Image)
				.SaveAsPng("CropNo.png");
			byte[] cropped = arrayRenderer.Image.TransparentCrop(
				out _,
				out _,
				out int croppedWidth,
				out int _,
				threshold: 128,
				width: width
				);
			ImageMaker.Png(
				width: croppedWidth,
				bytes: cropped)
				.SaveAsPng("CropYes.png");
		}
	}
}
