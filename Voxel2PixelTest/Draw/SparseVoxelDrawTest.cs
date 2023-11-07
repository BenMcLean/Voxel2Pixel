using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
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
			VoxFileModel model = new VoxFileModel(@"..\..\..\Sora.vox");
			IVoxelColor voxelColor = new NaiveDimmer(model.Palette);
			ushort width = model.SizeX;
			ArrayRenderer arrayRenderer = new ArrayRenderer
			{
				Image = new byte[width * 4 * model.SizeZ],
				Width = width,
				VoxelColor = voxelColor,
			};
			VoxelDraw.Front(model, arrayRenderer);
			ImageMaker.Png(
				scaleX: 32,
				scaleY: 32,
				width: width,
				bytes: arrayRenderer.Image)
				.SaveAsPng("SparseVoxelDrawFront.png");
		}
		[Fact]
		public void FrontPeek()
		{
			VoxFileModel model = new VoxFileModel(@"..\..\..\Sora.vox");
			IVoxelColor voxelColor = new NaiveDimmer(model.Palette);
			byte scaleX = 6, scaleY = 6;
			ushort width = (ushort)(model.SizeX * scaleX);
			ArrayRenderer arrayRenderer = new ArrayRenderer
			{
				Image = new byte[width * 4 * model.SizeZ * scaleY],
				Width = width,
				VoxelColor = voxelColor,
			};
			VoxelDraw.FrontPeek(model, arrayRenderer, scaleX, scaleY);
			ImageMaker.Png(
				scaleX: 1,
				scaleY: 1,
				width: width,
				bytes: arrayRenderer.Image)
				.SaveAsPng("SparseVoxelDrawFrontPeek.png");
		}
		[Fact]
		public void Diagonal()
		{
			VoxFileModel model = new VoxFileModel(@"..\..\..\Sora.vox");
			IVoxelColor voxelColor = new NaiveDimmer(model.Palette);
			int width = VoxelDraw.DiagonalWidth(model);
			ArrayRenderer arrayRenderer = new ArrayRenderer
			{
				Image = new byte[width * 4 * model.SizeZ],
				Width = width,
				VoxelColor = voxelColor,
			};
			VoxelDraw.Diagonal(model, arrayRenderer);
			ImageMaker.Png(
				scaleX: 32,
				scaleY: 32,
				width: width,
				bytes: arrayRenderer.Image)
				.SaveAsPng("SparseVoxelDrawDiagonal.png");
		}
		[Fact]
		public void DiagonalPeek()
		{
			VoxFileModel model = new VoxFileModel(@"..\..\..\Sora.vox");
			IVoxelColor voxelColor = new NaiveDimmer(model.Palette);
			int width = VoxelDraw.DiagonalPeekWidth(model);
			ArrayRenderer arrayRenderer = new ArrayRenderer
			{
				Image = new byte[width * 4 * VoxelDraw.DiagonalPeekHeight(model)],
				Width = width,
				VoxelColor = voxelColor,
			};
			VoxelDraw.DiagonalPeek(model, arrayRenderer);
			ImageMaker.Png(
				scaleX: 6,
				scaleY: 6,
				width: width,
				bytes: arrayRenderer.Image)
				.SaveAsPng("SparseVoxelDrawDiagonalPeek.png");
		}
		[Fact]
		public void Above()
		{
			VoxFileModel model = new VoxFileModel(@"..\..\..\Sora.vox");
			IVoxelColor voxelColor = new NaiveDimmer(model.Palette);
			int width = VoxelDraw.AboveWidth(model),
				height = VoxelDraw.AboveHeight(model);
			ArrayRenderer arrayRenderer = new ArrayRenderer
			{
				Image = new byte[width * 4 * height],
				Width = width,
				VoxelColor = voxelColor,
			};
			VoxelDraw.Above(model, arrayRenderer);
			ImageMaker.Png(
				scaleX: 32,
				scaleY: 32,
				width: width,
				bytes: arrayRenderer.Image)
				.SaveAsPng("SparseVoxelDrawAbove.png");
		}
		[Fact]
		public void TurnSparseModel()
		{
			VoxFileModel voxFile = new VoxFileModel(@"..\..\..\Sora.vox");
			ushort width = Math.Max(Math.Max(voxFile.SizeX, voxFile.SizeY), voxFile.SizeZ);
			IVoxelColor voxelColor = new NaiveDimmer(voxFile.Palette);
			TurnModel model = new TurnModel
			{
				Model = voxFile,
			};
			List<byte[]> frames = new List<byte[]>();
			foreach (CuboidOrientation cuboidOrientation in CuboidOrientation.Values)
			{
				model.CuboidOrientation = cuboidOrientation;
				for (ushort x = 0; x < model.SizeX; x++)
					for (ushort y = 0; y < model.SizeY; y++)
						for (ushort z = 0; z < model.SizeZ; z++)
						{
							model.Rotate(out ushort x2, out ushort y2, out ushort z2, x, y, z);
							Assert.True(x2 >= 0);
							Assert.True(y2 >= 0);
							Assert.True(z2 >= 0);
						}
				foreach (Voxel voxel in model)
				{
					Assert.True(voxel.X < model.SizeX);
					Assert.True(voxel.Y < model.SizeY);
					Assert.True(voxel.Z < model.SizeZ);
				}
				ArrayRenderer arrayRenderer = new ArrayRenderer
				{
					Image = new byte[width * 4 * (width + 4)],
					Width = width,
					VoxelColor = voxelColor,
				};
				VoxelDraw.Front(model, arrayRenderer);
				frames.Add(arrayRenderer.Image);
			}
			Assert.Equal(
				expected: 24,
				actual: frames.Count);
			ImageMaker.AnimatedGif(
				scaleX: 16,
				scaleY: 16,
				width: width,
				frames: frames.ToArray()
					.AddFrameNumbers(width),
				frameDelay: 150)
			.SaveAsGif("SparseVoxelDrawFront.gif");
		}
		[Fact]
		public void Iso()
		{
			VoxFileModel model = new VoxFileModel(@"..\..\..\Sora.vox");
			IVoxelColor voxelColor = new NaiveDimmer(model.Palette);
			int width = (int)VoxelDraw.IsoWidth(model),
				height = (int)VoxelDraw.IsoHeight(model);
			ArrayRenderer arrayRenderer = new ArrayRenderer
			{
				Image = new byte[width * 4 * height],
				Width = width,
				VoxelColor = voxelColor,
			};
			VoxelDraw.Iso(model, arrayRenderer);
			ImageMaker.Png(
				scaleX: 2,
				scaleY: 1,
				width: width,
				bytes: arrayRenderer.Image)
				.SaveAsPng("SparseVoxelDrawIso.png");
		}
	}
}
