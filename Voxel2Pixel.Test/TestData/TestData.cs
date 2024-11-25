using SixLabors.ImageSharp;
using static Voxel2Pixel.Draw.PixelDraw;
using System.Collections.ObjectModel;
using Voxel2Pixel.Model;

namespace Voxel2Pixel.Test.TestData;

public static class TestData
{
	public static readonly ReadOnlyCollection<uint> Rainbow = Array.AsReadOnly([//Just a color test, not a political statement.
		0xFF0000FFu,//Red
		0xFFA500FFu,//Orange
		0xFFFF00FFu,//Yellow
		0x00FF00FFu,//Green
		0x0000FFFFu,//Blue
		0x4B0082FFu,//Indigo
		0x8F00FFFFu,//Violet
	]);
	public static readonly uint[] RainbowPalette = [.. Enumerable.Range(0, byte.MaxValue)
		.Select(i => i == 0 ? 0 : Rainbow[(i - 1) % Rainbow.Count])];
	public static byte[][][] RainbowBox(int sizeX, int sizeY, int sizeZ)
	{
		byte[][][] model = Array3D.Initialize<byte>(sizeX, sizeY, sizeZ);
		for (int x = 0; x < sizeX; x++)
		{
			byte voxel = (byte)((sizeX - 1 - x) % Rainbow.Count + 1);
			model[x][0][0] = voxel;
			model[x][sizeY - 1][0] = voxel;
			model[x][0][sizeZ - 1] = voxel;
			model[x][sizeY - 1][sizeZ - 1] = voxel;
		}
		for (int y = 1; y < sizeY - 1; y++)
		{
			byte voxel = (byte)((sizeY - 1 - y) % Rainbow.Count + 1);
			model[0][y][0] = voxel;
			model[sizeX - 1][y][0] = voxel;
			model[0][y][sizeZ - 1] = voxel;
			model[sizeX - 1][y][sizeZ - 1] = voxel;
		}
		for (int z = 1; z < sizeZ - 1; z++)
		{
			byte voxel = (byte)((sizeZ - 1 - z) % Rainbow.Count + 1);
			model[0][0][z] = voxel;
			model[sizeX - 1][0][z] = voxel;
			model[0][sizeY - 1][z] = voxel;
			model[sizeX - 1][sizeY - 1][z] = voxel;
		}
		return model;
	}
	public static byte[][][] AltRainbowBox(int sizeX, int sizeY, int sizeZ)
	{
		byte[][][] model = Array3D.Initialize<byte>(sizeX, sizeY, sizeZ);
		model[0][3][0] = 1;
		model[3][0][0] = 1;
		for (int x = 1; x < sizeX; x++)
		{
			byte voxel = (byte)((sizeX - 1 - x) % Rainbow.Count + 1);
			model[x][1][0] = voxel;
			model[x][sizeY - 1][0] = voxel;
			model[x][1][sizeZ - 1] = voxel;
			model[x][sizeY - 1][sizeZ - 1] = voxel;
		}
		for (int y = 1; y < sizeY - 1; y++)
		{
			byte voxel = (byte)((sizeY - 1 - y) % Rainbow.Count + 1);
			model[1][y][0] = voxel;
			model[sizeX - 1][y][0] = voxel;
			model[1][y][sizeZ - 1] = voxel;
			model[sizeX - 1][y][sizeZ - 1] = voxel;
		}
		for (int z = 1; z < sizeZ - 1; z++)
		{
			byte voxel = (byte)((sizeZ - 1 - z) % Rainbow.Count + 1);
			model[1][1][z] = voxel;
			model[sizeX - 1][1][z] = voxel;
			model[1][sizeY - 1][z] = voxel;
			model[sizeX - 1][sizeY - 1][z] = voxel;
		}
		return model;
	}
	public static byte[][][] SmallerRainbowBox(int sizeX, int sizeY, int sizeZ)
	{
		byte[][][] model = Array3D.Initialize<byte>(sizeX, sizeY, sizeZ);
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
	public static byte[] TestTexture(ushort width, ushort height) =>
		new byte[16] {
			255,0,0,255,
			0,255,0,255,
			0,0,255,255,
			128,128,128,255}
		.Upscale(
			scaleX: (byte)(width >> 1),
			scaleY: (byte)(height >> 1),
			width: 2)
		.DrawRectangle(
			color: 0xFFu,
			x: width >> 2,
			y: height >> 2,
			rectWidth: width >> 1,
			rectHeight: height >> 1,
			width: width);
	public static byte[][][] Pyramid(int width, params byte[]? colors)
	{
		if (colors is null || colors.Length < 1)
			colors = [.. Enumerable.Range(0, 256).Select(i => (byte)i)];
		int halfWidth = width >> 1;
		byte[][][] voxels = Array3D.Initialize<byte>(width, width, halfWidth + 1);
		voxels[0][0][0] = colors[1];
		voxels[width - 1][0][0] = colors[2];
		voxels[0][width - 1][0] = colors[3];
		voxels[width - 1][width - 1][0] = colors[4];
		for (int i = 0; i <= halfWidth; i++)
		{
			voxels[i][i][i] = colors[1];
			voxels[width - 1 - i][i][i] = colors[2];
			voxels[i][width - 1 - i][i] = colors[3];
			voxels[width - 1 - i][width - 1 - i][i] = colors[4];
			voxels[halfWidth][halfWidth][i] = colors[5];
		}
		return voxels;
	}
	public static byte[][][] Pyramid2(int width, params byte[]? colors) => Pyramid2(width, width, colors);
	public static byte[][][] Pyramid2(int width, int depth, params byte[]? colors)
	{
		if (colors is null || colors.Length < 1)
			colors = [.. Enumerable.Range(0, 256).Select(i => (byte)i)];
		int halfWidth = width >> 1;
		byte[][][] voxels = Array3D.Initialize<byte>(width, depth, halfWidth + 1);
		voxels[width - 1][0][0] = colors[2];
		voxels[0][depth - 1][0] = colors[3];
		voxels[width - 1][depth - 1][0] = colors[4];
		for (int i = 0; i <= halfWidth; i++)
			voxels[0][0][i] = colors[1];
		return voxels;
	}
	public static byte[][][] Arch(int width, params byte[]? colors)
	{
		if (colors is null || colors.Length < 1)
			colors = [.. Enumerable.Range(0, 256).Select(i => (byte)i)];
		int halfWidth = width >> 1;
		byte[][][] voxels = Array3D.Initialize<byte>(width, width, halfWidth + 1);
		for (int i = 0; i <= halfWidth - 1; i++)
		{
			voxels[i][i][i] = colors[1];
			voxels[width - 1 - i][width - 1 - i][i] = colors[2];
			voxels[halfWidth][halfWidth][i] = colors[3];
		}
		return voxels;
	}
}
