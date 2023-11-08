﻿using SixLabors.ImageSharp;
using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;
using Voxel2Pixel.Render;
using Xunit;

namespace Voxel2PixelTest.Model
{
	public class VoxFileModelTest
	{
		[Fact]
		public void ArrayRendererTest()
		{
			VoxFileModel model = new VoxFileModel(@"..\..\..\Sora.vox");
			ushort width = VoxelDraw.IsoWidth(model),
				height = VoxelDraw.IsoHeight(model);
			ArrayRenderer arrayRenderer = new ArrayRenderer
			{
				Image = new byte[width * 4 * height],
				Width = width,
				VoxelColor = new NaiveDimmer(model.Palette),
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
			VoxFileModel model = new VoxFileModel(@"..\..\..\Sora.vox");
			ushort width = VoxelDraw.IsoWidth(model),
				height = VoxelDraw.IsoHeight(model);
			ArrayRenderer arrayRenderer = new ArrayRenderer
			{
				Image = new byte[width * 4 * height],
				Width = width,
				VoxelColor = new NaiveDimmer(model.Palette),
			};
			VoxelDraw.Iso(model, arrayRenderer);
			ImageMaker.Png(
				width: width,
				bytes: arrayRenderer.Image)
				.SaveAsPng("CropNo.png");
			byte[] cropped = arrayRenderer.Image
				.Outline(width)
				.TransparentCrop(
					out _,
					out _,
					out ushort croppedWidth,
					out ushort _,
					threshold: 128,
					width: width
					);
			ImageMaker.Png(
				width: croppedWidth,
				bytes: cropped)
				.SaveAsPng("CropYes.png");
		}
		[Fact]
		public void VoxelDrawTest()
		{
			//VoxModel voxModel = (VoxModel)new VoxModel(@"..\..\..\Sora.vox").DrawBox(1);
			//VoxModel voxModel = new VoxModel(@"..\..\..\Sora.vox");
			//IVoxelColor voxelColor = new NaiveDimmer(voxModel.Palette);
			//TurnModel model = new TurnModel
			//{
			//	Model = voxModel,
			//	CuboidOrientation = CuboidOrientation.SOUTH0,
			//};
			ArrayModel model = new ArrayModel(ArrayModelTest.RainbowBox(7, 4, 7));
			IVoxelColor voxelColor = new NaiveDimmer(ArrayModelTest.RainbowPalette);
			ushort width = VoxelDraw.DiagonalWidth(model),
				height = VoxelDraw.DiagonalHeight(model);
			ArrayRenderer arrayRenderer = new ArrayRenderer
			{
				Image = new byte[width * 4 * height],
				Width = width,
				VoxelColor = voxelColor,
			};
			VoxelDraw.Diagonal(model, arrayRenderer);
			ImageMaker.Png(
				scaleX: 16,
				scaleY: 16,
				width: width,
				bytes: arrayRenderer.Image)
				.SaveAsPng("VoxelDrawTest.png");
		}
	}
}
