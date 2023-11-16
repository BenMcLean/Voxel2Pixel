using SixLabors.ImageSharp;
using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;
using Voxel2Pixel.Render;
using Xunit;

namespace Voxel2PixelTest.Draw
{
	public class VoxelDrawTest
	{
		[Fact]
		public void IsoSlantTest()
		{
			VoxFileModel voxFileModel = new VoxFileModel(@"..\..\..\UnevenSizes.vox");
			IVoxelColor voxelColor = new NaiveDimmer(voxFileModel.Palette);
			ushort width = voxFileModel.SizeX,
				height = voxFileModel.SizeZ;
			ArrayRenderer arrayRenderer = new ArrayRenderer
			{
				Image = new byte[width * 4 * height],
				Width = width,
				VoxelColor = voxelColor,
			};
			VoxelDraw.Front(voxFileModel, arrayRenderer);
			ushort isoWidth = (ushort)(VoxelDraw.IsoSlantWidth(arrayRenderer.Image.Length, width) << 1),
				isoHeight = VoxelDraw.IsoSlantHeight(arrayRenderer.Image.Length, width);
			Array2xRenderer array2xRenderer = new Array2xRenderer
			{
				Image = new byte[isoWidth * 4 * isoHeight],
				Width = isoWidth,
			};
			VoxelDraw.IsoSlantDown(
				renderer: array2xRenderer,
				texture: arrayRenderer.Image,
				width: width);
			Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(array2xRenderer.Image, isoWidth, isoHeight).SaveAsPng("VoxelDrawIsoSlant.png");
		}
		[Fact]
		public void IsoTileTest()
		{
			VoxFileModel voxFileModel = new VoxFileModel(@"..\..\..\UnevenSizes.vox");
			IVoxelColor voxelColor = new NaiveDimmer(voxFileModel.Palette);
			ushort width = voxFileModel.SizeX,
				height = voxFileModel.SizeZ;
			ArrayRenderer arrayRenderer = new ArrayRenderer
			{
				Image = new byte[width * 4 * height],
				Width = width,
				VoxelColor = voxelColor,
			};
			VoxelDraw.Front(voxFileModel, arrayRenderer);
			ushort isoWidth = (ushort)(VoxelDraw.IsoTileWidth(arrayRenderer.Image.Length, width) << 1),
				isoHeight = VoxelDraw.IsoTileHeight(arrayRenderer.Image.Length, width);
			Array2xRenderer array2xRenderer = new Array2xRenderer
			{
				Image = new byte[isoWidth * 4 * isoHeight],
				Width = isoWidth,
			};
			VoxelDraw.IsoTile(
				renderer: array2xRenderer,
				texture: arrayRenderer.Image,
				width: width);
			Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(array2xRenderer.Image, isoWidth, isoHeight).SaveAsPng("VoxelDrawIsoTile.png");
		}
	}
}
