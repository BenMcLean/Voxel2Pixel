using System;

namespace Voxel2Pixel.Model
{
	public static class Bytes3D
	{
		public static byte[][][] Initialize(int sizeX = 1, int sizeY = 1, int sizeZ = 1)
		{
			byte[][][] model = new byte[sizeX][][];
			for (int x = 0; x < sizeX; x++)
			{
				model[x] = new byte[sizeY][];
				for (int y = 0; y < sizeY; y++)
					model[x][y] = new byte[sizeZ];
			}
			return model;
		}
		public static byte[][][] DeepCopy(this byte[][][] inputArray)
		{
			if (inputArray is null)
				return null;
			byte[][][] outputArray = new byte[inputArray.Length][][];
			for (int x = 0; x < inputArray.Length; x++)
				if (inputArray[x] is null)
					outputArray[x] = null;
				else
				{
					outputArray[x] = new byte[inputArray[x].Length][];
					for (int y = 0; y < inputArray[x].Length; y++)
						if (inputArray[x][y] is null)
							outputArray[x][y] = null;
						else
						{
							outputArray[x][y] = new byte[inputArray[x][y].Length];
							Array.Copy(inputArray[x][y], outputArray[x][y], inputArray[x][y].Length);
						}
				}
			return outputArray;
		}
		public static byte[][][] Box(this byte[][][] voxels, byte voxel)
		{
			for (int x = 0; x < voxels.Length; x++)
			{
				voxels[x][0][0] = voxel;
				voxels[x][voxels[0].Length - 1][0] = voxel;
				voxels[x][0][voxels[0][0].Length - 1] = voxel;
				voxels[x][voxels[0].Length - 1][voxels[0][0].Length - 1] = voxel;
			}
			for (int y = 1; y < voxels[0].Length - 1; y++)
			{
				voxels[0][y][0] = voxel;
				voxels[voxels.Length - 1][y][0] = voxel;
				voxels[0][y][voxels[0][0].Length - 1] = voxel;
				voxels[voxels.Length - 1][y][voxels[0][0].Length - 1] = voxel;
			}
			for (int z = 1; z < voxels[0][0].Length - 1; z++)
			{
				voxels[0][0][z] = voxel;
				voxels[voxels.Length - 1][0][z] = voxel;
				voxels[0][voxels[0].Length - 1][z] = voxel;
				voxels[voxels.Length - 1][voxels[0].Length - 1][z] = voxel;
			}
			return voxels;
		}
		#region Rotations
		public static byte[][][] CounterX(this byte[][][] inputArray)
		{
			byte[][][] outputArray = Initialize(inputArray[0].Length, inputArray.Length, inputArray[0][0].Length);
			for (int x = 0; x < inputArray.Length; x++)
				for (int y = 0; y < inputArray[x].Length; y++)
					for (int z = 0; z < inputArray[x][y].Length; z++)
						outputArray[inputArray[x].Length - 1 - y][x][z] = inputArray[x][y][z];
			return outputArray;
		}
		public static byte[][][] CounterY(this byte[][][] inputArray)
		{
			byte[][][] outputArray = Initialize(inputArray[0].Length, inputArray.Length, inputArray[0][0].Length);
			for (int x = 0; x < inputArray.Length; x++)
				for (int y = 0; y < inputArray[x].Length; y++)
					for (int z = 0; z < inputArray[x][y].Length; z++)
						outputArray[x][y][inputArray[x][y].Length - 1 - z] = inputArray[x][y][z];
			return outputArray;
		}
		public static byte[][][] CounterZ(this byte[][][] inputArray)
		{
			byte[][][] outputArray = Initialize(inputArray[0][0].Length, inputArray.Length, inputArray[0].Length);
			for (int x = 0; x < inputArray.Length; x++)
				for (int y = 0; y < inputArray[x].Length; y++)
					for (int z = 0; z < inputArray[x][y].Length; z++)
						outputArray[inputArray[x][y].Length - 1 - z][y][x] = inputArray[x][y][z];
			return outputArray;
		}
		public static byte[][][] ClockX(this byte[][][] inputArray)
		{
			byte[][][] outputArray = Initialize(inputArray[0].Length, inputArray.Length, inputArray[0][0].Length);
			for (int x = 0; x < inputArray.Length; x++)
				for (int y = 0; y < inputArray[x].Length; y++)
					for (int z = 0; z < inputArray[x][y].Length; z++)
						outputArray[y][inputArray[x].Length - 1 - x][z] = inputArray[x][y][z];
			return outputArray;
		}
		public static byte[][][] ClockY(this byte[][][] inputArray)
		{
			byte[][][] outputArray = Initialize(inputArray.Length, inputArray[0].Length, inputArray[0][0].Length);
			for (int x = 0; x < inputArray.Length; x++)
				for (int y = 0; y < inputArray[x].Length; y++)
					for (int z = 0; z < inputArray[x][y].Length; z++)
						outputArray[x][y][inputArray[x][y].Length - 1 - z] = inputArray[x][y][z];
			return outputArray;
		}
		public static byte[][][] ClockZ(this byte[][][] inputArray)
		{
			byte[][][] outputArray = Initialize(inputArray[0].Length, inputArray[0][0].Length, inputArray.Length);
			for (int x = 0; x < inputArray.Length; x++)
				for (int y = 0; y < inputArray[x].Length; y++)
					for (int z = 0; z < inputArray[x][y].Length; z++)
						outputArray[z][y][inputArray.Length - 1 - x] = inputArray[x][y][z];
			return outputArray;
		}
		#endregion Rotations
	}
}
