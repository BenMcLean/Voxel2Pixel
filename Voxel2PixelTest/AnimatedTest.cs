using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Model;
using Voxel2Pixel.Render;
using Xunit;

namespace Voxel2PixelTest
{
	public class AnimatedTest
	{
		[Fact]
		public void GifTest()
		{
			ArrayModel model = new ArrayModel(
				sizeX: 7,
				sizeY: 4,
				sizeZ: 7);
			int width = VoxelDraw.AboveWidth(model),
				height = VoxelDraw.AboveHeight(model);
			Random random = new System.Random();
			IVoxelColor voxelColor = new NaiveDimmer(ArrayModelTest.RainbowPalette);
			List<byte[]> frames = new List<byte[]>();
			for (int x = 0; x < model.SizeX; x++)
				for (int y = model.SizeY - 1; y >= 0; y--)
					for (int z = 0; z < model.SizeZ; z++)
					{
						model.Voxels[x][y][z] = 1;
						int randomX = random.Next(0, model.SizeX),
							randomY = random.Next(0, model.SizeY),
							randomZ = random.Next(0, model.SizeZ);
						model.Voxels[randomX][randomY][randomZ] = (byte)(random.Next(0, ArrayModelTest.Rainbow.Count) + 1);
						ArrayRenderer arrayRenderer = new ArrayRenderer
						{
							Image = new byte[width * 4 * height],
							Width = width,
							IVoxelColor = voxelColor,
						};
						VoxelDraw.Above(model, arrayRenderer);
						frames.Add(arrayRenderer.Image);
						model.Voxels[randomX][randomY][randomZ] = 0;
						model.Voxels[x][y][z] = 2;
					}
			ImageMaker.AnimatedGifScaled(
				scaleX: 32,
				scaleY: 32,
				width: width,
				frames: frames.ToArray())
				.SaveAsGif("AnimatedTest.gif");
		}
		[Fact]
		public void SizeTest()
		{
			EmptyModel empty = new EmptyModel(
				sizeX: 8,
				sizeY: 8,
				sizeZ: 8);
			int width = VoxelDraw.IsoWidth(empty),
				height = VoxelDraw.IsoHeight(empty);
			int start = 6;
			List<byte[]> frames = new List<byte[]>();
			for (int sizeX = start; sizeX <= empty.SizeX; sizeX++)
				for (int sizeY = start; sizeY <= empty.SizeY; sizeY++)
					for (int sizeZ = start; sizeZ <= empty.SizeZ; sizeZ++)
					{
						ArrayModel model = new ArrayModel(ArrayModelTest.RainbowBox(
							sizeX: sizeX,
							sizeY: sizeY,
							sizeZ: sizeZ));
						ArrayRenderer arrayRenderer = new ArrayRenderer
						{
							Image = new byte[width * 4 * height],
							Width = width,
							IVoxelColor = new NaiveDimmer(ArrayModelTest.RainbowPalette),
						};
						VoxelDraw.Iso(model, arrayRenderer);
						frames.Add(arrayRenderer.Image);
					}
			ImageMaker.AnimatedGifScaled(
				scaleX: 32,
				scaleY: 32,
				width: width,
				frames: frames.ToArray())
				.SaveAsGif("SizeTest.gif");
		}
	}
}
