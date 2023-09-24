using System;

namespace Voxel2Pixel.Model
{
	public static class Bytes3D
	{
		public static T[][][] Initialize<T>(int sizeX = 1, int sizeY = 1, int sizeZ = 1)
		{
			T[][][] model = new T[sizeX][][];
			for (int x = 0; x < sizeX; x++)
			{
				model[x] = new T[sizeY][];
				for (int y = 0; y < sizeY; y++)
					model[x][y] = new T[sizeZ];
			}
			return model;
		}
		public static T[][][] DeepCopy<T>(this T[][][] inputArray)
		{
			if (inputArray is null)
				return null;
			T[][][] outputArray = new T[inputArray.Length][][];
			for (int x = 0; x < inputArray.Length; x++)
				if (inputArray[x] is null)
					outputArray[x] = null;
				else
				{
					outputArray[x] = new T[inputArray[x].Length][];
					for (int y = 0; y < inputArray[x].Length; y++)
						if (inputArray[x][y] is null)
							outputArray[x][y] = null;
						else
						{
							outputArray[x][y] = new T[inputArray[x][y].Length];
							Array.Copy(inputArray[x][y], outputArray[x][y], inputArray[x][y].Length);
						}
				}
			return outputArray;
		}
		public static T[][][] Box<T>(this T[][][] voxels, T voxel)
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
		public static T[][][] CounterX<T>(this T[][][] inputArray)
		{
			T[][][] outputArray = Initialize<T>(inputArray[0].Length, inputArray.Length, inputArray[0][0].Length);
			for (int x = 0; x < inputArray.Length; x++)
				for (int y = 0; y < inputArray[x].Length; y++)
					for (int z = 0; z < inputArray[x][y].Length; z++)
						outputArray[inputArray[x].Length - 1 - y][x][z] = inputArray[x][y][z];
			return outputArray;
		}
		public static T[][][] CounterY<T>(this T[][][] inputArray)
		{
			T[][][] outputArray = Initialize<T>(inputArray[0].Length, inputArray.Length, inputArray[0][0].Length);
			for (int x = 0; x < inputArray.Length; x++)
				for (int y = 0; y < inputArray[x].Length; y++)
					for (int z = 0; z < inputArray[x][y].Length; z++)
						outputArray[x][y][inputArray[x][y].Length - 1 - z] = inputArray[x][y][z];
			return outputArray;
		}
		public static T[][][] CounterZ<T>(this T[][][] inputArray)
		{
			T[][][] outputArray = Initialize<T>(inputArray[0][0].Length, inputArray.Length, inputArray[0].Length);
			for (int x = 0; x < inputArray.Length; x++)
				for (int y = 0; y < inputArray[x].Length; y++)
					for (int z = 0; z < inputArray[x][y].Length; z++)
						outputArray[inputArray[x][y].Length - 1 - z][y][x] = inputArray[x][y][z];
			return outputArray;
		}
		public static T[][][] ClockX<T>(this T[][][] inputArray)
		{
			T[][][] outputArray = Initialize<T>(inputArray[0].Length, inputArray.Length, inputArray[0][0].Length);
			for (int x = 0; x < inputArray.Length; x++)
				for (int y = 0; y < inputArray[x].Length; y++)
					for (int z = 0; z < inputArray[x][y].Length; z++)
						outputArray[y][inputArray[x].Length - 1 - x][z] = inputArray[x][y][z];
			return outputArray;
		}
		public static T[][][] ClockY<T>(this T[][][] inputArray)
		{
			T[][][] outputArray = Initialize<T>(inputArray.Length, inputArray[0].Length, inputArray[0][0].Length);
			for (int x = 0; x < inputArray.Length; x++)
				for (int y = 0; y < inputArray[x].Length; y++)
					for (int z = 0; z < inputArray[x][y].Length; z++)
						outputArray[x][y][inputArray[x][y].Length - 1 - z] = inputArray[x][y][z];
			return outputArray;
		}
		public static T[][][] ClockZ<T>(this T[][][] inputArray)
		{
			T[][][] outputArray = Initialize<T>(inputArray[0].Length, inputArray[0][0].Length, inputArray.Length);
			for (int x = 0; x < inputArray.Length; x++)
				for (int y = 0; y < inputArray[x].Length; y++)
					for (int z = 0; z < inputArray[x][y].Length; z++)
						outputArray[z][y][inputArray.Length - 1 - x] = inputArray[x][y][z];
			return outputArray;
		}
		#endregion Rotations
	}
}
