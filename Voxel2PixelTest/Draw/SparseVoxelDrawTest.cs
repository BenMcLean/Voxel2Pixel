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
		[Fact]
		public void Diagonal()
		{
			VoxFileModel voxFile = new VoxFileModel(@"..\..\..\Sora.vox");
			IVoxelColor voxelColor = new NaiveDimmer(voxFile.Palette);
			ListModel model = new ListModel(voxFile);
			int width = SparseVoxelDraw.DiagonalWidth(model);
			ArrayRenderer arrayRenderer = new ArrayRenderer
			{
				Image = new byte[width * 4 * model.SizeZ],
				Width = width,
				VoxelColor = voxelColor,
			};
			SparseVoxelDraw.Diagonal(model, arrayRenderer);
			ImageMaker.Png(
				scaleX: 32,
				scaleY: 32,
				width: width,
				bytes: arrayRenderer.Image)
				.SaveAsPng("SparseVoxelDrawDiagonal.png");
		}
		[Fact]
		public void TurnSparseModel()
		{
			VoxFileModel voxFile = new VoxFileModel(@"..\..\..\Sora.vox");
			ushort width = Math.Max(Math.Max(voxFile.SizeX, voxFile.SizeY), voxFile.SizeZ);
			IVoxelColor voxelColor = new NaiveDimmer(voxFile.Palette);
			TurnSparseModel model = new TurnSparseModel
			{
				SparseModel = new ListModel(voxFile),
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
				foreach (Voxel voxel in model.Voxels)
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
				SparseVoxelDraw.Front(model, arrayRenderer);
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
	}
}
