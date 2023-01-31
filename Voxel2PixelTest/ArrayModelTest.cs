using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Model;
using Voxel2Pixel.Render;
using Xunit;

namespace Voxel2PixelTest
{
	public class ArrayModelTest
	{
		[Fact]
		public void ArrayRendererTest()
		{
			ArrayModel model = new ArrayModel(RainbowBox(
				sizeX: 12,
				sizeY: 6,
				sizeZ: 7));
			int width = VoxelDraw.IsoWidth(model),
				height = VoxelDraw.IsoHeight(model);
			ArrayRenderer arrayRenderer = new ArrayRenderer
			{
				Image = new byte[width * 4 * height],
				Width = width,
				IVoxelColor = new NaiveDimmer(RainbowPalette),
			};
			VoxelDraw.Iso(model, arrayRenderer);
			ImageMaker.Png(
				scaleX: 32,
				scaleY: 32,
				width: width,
				bytes: arrayRenderer.Image)
				.SaveAsPng("ArrayModel.png");
		}
		[Fact]
		public void StupidTest()
		{
			ArrayModel model = new ArrayModel(RainbowBox(
				sizeX: 12,
				sizeY: 6,
				sizeZ: 7));
			int width = VoxelDraw.IsoWidth(model),
				height = VoxelDraw.IsoHeight(model);
			IVoxelColor voxelColor = new NaiveDimmer(RainbowPalette);
			List<byte[]> frames = new List<byte[]>();
			for (int stupid = -model.SizeZ; stupid <= model.SizeZ; stupid++)
			{
				ArrayRenderer arrayRenderer = new ArrayRenderer
				{
					Image = new byte[width * 4 * height],
					Width = width,
					IVoxelColor = voxelColor,
				};
				VoxelDraw.Iso(model, arrayRenderer, stupid);
				frames.Add(arrayRenderer.Image);
			}
			ImageMaker.AnimatedGifScaled(
				scaleX: 32,
				scaleY: 32,
				frameDelay: 50,
				width: width,
				frames: frames.ToArray())
				.SaveAsGif("stupid.gif");
		}
		public static readonly ReadOnlyCollection<int> Rainbow = Array.AsReadOnly(new int[7]
		{ // Just a color test, not a political statement.
			unchecked((int)0xFF0000FF),//Red
			unchecked((int)0xFFA500FF),//Orange
			unchecked((int)0xFFFF00FF),//Yellow
			0x00FF00FF,//Green
			0x0000FFFF,//Blue
			0x4B0082FF,//Indigo
			unchecked((int)0x8F00FFFF)//Violet
		});
		public static readonly int[] RainbowPalette =
			Enumerable.Range(0, byte.MaxValue)
			.Select(i => i == 0 ? 0 : Rainbow[(i - 1) % Rainbow.Count])
			.ToArray();
		public byte[][][] RainbowBox(int sizeX, int sizeY, int sizeZ)
		{
			byte[][][] model = ArrayModel.MakeModel(sizeX, sizeY, sizeZ);
			for (int x = 0; x < sizeX; x++)
			{
				byte voxel = (byte)(x % Rainbow.Count + 1);
				model[x][0][0] = voxel;
				model[x][sizeY - 1][0] = voxel;
				model[x][0][sizeZ - 1] = voxel;
				model[x][sizeY - 1][sizeZ - 1] = voxel;
			}
			for (int y = 1; y < sizeY - 1; y++)
			{
				byte voxel = (byte)(y % Rainbow.Count + 1);
				model[0][y][0] = voxel;
				model[sizeX - 1][y][0] = voxel;
				model[0][y][sizeZ - 1] = voxel;
				model[sizeX - 1][y][sizeZ - 1] = voxel;
			}
			for (int z = 1; z < sizeZ - 1; z++)
			{
				byte voxel = (byte)(z % Rainbow.Count + 1);
				model[0][0][z] = voxel;
				model[sizeX - 1][0][z] = voxel;
				model[0][sizeY - 1][z] = voxel;
				model[sizeX - 1][sizeY - 1][z] = voxel;
			}
			return model;
		}
		public byte[][][] SmallerRainbowBox(int sizeX, int sizeY, int sizeZ)
		{
			byte[][][] model = ArrayModel.MakeModel(sizeX, sizeY, sizeZ);
			for (int x = 1; x < sizeX - 1; x++)
			{
				byte voxel = (byte)(x % Rainbow.Count);
				model[x][1][1] = voxel;
				model[x][sizeY - 2][1] = voxel;
				model[x][1][sizeZ - 2] = voxel;
				model[x][sizeY - 2][sizeZ - 2] = voxel;
			}
			for (int y = 1; y < sizeY - 2; y++)
			{
				byte voxel = (byte)(y % Rainbow.Count);
				model[1][y][1] = voxel;
				model[sizeX - 2][y][1] = voxel;
				model[1][y][sizeZ - 2] = voxel;
				model[sizeX - 2][y][sizeZ - 2] = voxel;
			}
			for (int z = 1; z < sizeZ - 2; z++)
			{
				byte voxel = (byte)(z % Rainbow.Count);
				model[1][1][z] = voxel;
				model[sizeX - 2][1][z] = voxel;
				model[1][sizeY - 2][z] = voxel;
				model[sizeX - 2][sizeY - 2][z] = voxel;
			}
			return model;
		}
	}
}
