using SixLabors.ImageSharp;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Model;
using Voxel2Pixel.Render;
using Xunit;
using static Voxel2Pixel.Draw.TextureMethods;

namespace Voxel2PixelTest
{
	public class ArrayModelTest
	{
		[Fact]
		public void ArrayRendererTest()
		{
			ArrayModel model = new ArrayModel(RainbowBox(
				sizeX: 7,
				sizeY: 7,
				sizeZ: 12));
			int xScale = 32,
				yScale = 32,
				width = VoxelDraw.AboveWidth(model),
				height = VoxelDraw.AboveHeight(model);
			ArrayRenderer arrayRenderer = new ArrayRenderer
			{
				Image = new byte[width * 4 * height],
				Width = width,
				IVoxelColor = new NaiveDimmer(RainbowPalette),
			};
			VoxelDraw.Above(model, arrayRenderer);
			Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(
				data: arrayRenderer.Image.Upscale(xScale, yScale, arrayRenderer.Width),
				width: arrayRenderer.Width * xScale,
				height: arrayRenderer.Height * yScale)
				.SaveAsPng("ArrayModel.png");
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
	}
}
