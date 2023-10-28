using SixLabors.ImageSharp;
using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;
using Voxel2Pixel.Render;
using Xunit;

namespace Voxel2PixelTest.Draw
{
	public class SparseVoxelDrawTest
	{
		[Fact]
		public void Front()
		{
			VoxFileModel voxFile = new VoxFileModel(@"..\..\..\Sora.vox");
			IVoxelColor voxelColor = new NaiveDimmer(voxFile.Palette);
			ListModel model = new ListModel(voxFile);
			ushort width = model.SizeX;
			ArrayRenderer arrayRenderer = new ArrayRenderer
			{
				Image = new byte[width * 4 * model.SizeZ],
				Width = width,
				VoxelColor = voxelColor,
			};
			SparseVoxelDraw.Front(model, arrayRenderer);
			ImageMaker.Png(
				scaleX: 32,
				scaleY: 32,
				width: width,
				bytes: arrayRenderer.Image)
				.SaveAsPng("SparseVoxelDrawFront.png");
		}
	}
}
