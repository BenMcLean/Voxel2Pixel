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
				ArrayRenderer arrayRenderer = new ArrayRenderer
				{
					Image = new byte[width * 4 * width],
					Width = width,
					VoxelColor = voxelColor,
				};
				SparseVoxelDraw.Front(model, arrayRenderer);
				frames.Add(arrayRenderer.Image);
			}
			ImageMaker.AnimatedGif(
				scaleX: 32,
				scaleY: 32,
				width: width,
				frameDelay: 150,
				repeatCount: 0,
				frames: frames.ToArray())
				.SaveAsPng("SparseVoxelDrawFront.gif");
		}
	}
}
