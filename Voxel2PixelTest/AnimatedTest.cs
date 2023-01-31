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
		//[Fact]
		public void GifTest()
		{
			ArrayModel model = new ArrayModel(
				sizeX: 7,
				sizeY: 4,
				sizeZ: 7);
			int xScale = 32,
				yScale = 32,
				width = VoxelDraw.AboveWidth(model),
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
						frames.Add(arrayRenderer.Image.Upscale(xScale, yScale, width));
						model.Voxels[randomX][randomY][randomZ] = 0;
						model.Voxels[x][y][z] = 2;
					}
			ImageMaker.AnimatedGif(
				width: width * xScale,
				frames: frames.ToArray())
				.SaveAsGif("AnimatedTest.gif");
		}
	}
}
