using System;
using BenVoxel.Models;
using BenVoxel.Structs;

namespace BenVoxel.Models;

public static class Array3D
{
	public static T[][][] Initialize<T>(int sizeX = 1, int sizeY = 1, int sizeZ = 1) => Initialize<T>((ushort)sizeX, (ushort)sizeY, (ushort)sizeZ);
	public static T[][][] Initialize<T>(ushort sizeX = 1, ushort sizeY = 1, ushort sizeZ = 1)
	{
		T[][][] model = new T[sizeX][][];
		for (ushort x = 0; x < sizeX; x++)
		{
			model[x] = new T[sizeY][];
			for (ushort y = 0; y < sizeY; y++)
				model[x][y] = new T[sizeZ];
		}
		return model;
	}
	public static T[][][] DeepCopy<T>(this T[][][] inputArray)
	{
		if (inputArray is null)
			return null;
		T[][][] outputArray = new T[inputArray.Length][][];
		for (ushort x = 0; x < inputArray.Length; x++)
			if (inputArray[x] is null)
				outputArray[x] = null;
			else
			{
				outputArray[x] = new T[inputArray[x].Length][];
				for (ushort y = 0; y < inputArray[x].Length; y++)
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
		for (ushort x = 0; x < voxels.Length; x++)
		{
			voxels[x][0][0] = voxel;
			voxels[x][voxels[0].Length - 1][0] = voxel;
			voxels[x][0][voxels[0][0].Length - 1] = voxel;
			voxels[x][voxels[0].Length - 1][voxels[0][0].Length - 1] = voxel;
		}
		for (ushort y = 1; y < voxels[0].Length - 1; y++)
		{
			voxels[0][y][0] = voxel;
			voxels[voxels.Length - 1][y][0] = voxel;
			voxels[0][y][voxels[0][0].Length - 1] = voxel;
			voxels[voxels.Length - 1][y][voxels[0][0].Length - 1] = voxel;
		}
		for (ushort z = 1; z < voxels[0][0].Length - 1; z++)
		{
			voxels[0][0][z] = voxel;
			voxels[voxels.Length - 1][0][z] = voxel;
			voxels[0][voxels[0].Length - 1][z] = voxel;
			voxels[voxels.Length - 1][voxels[0].Length - 1][z] = voxel;
		}
		return voxels;
	}
	#region Turns
	public static T[][][] Turn<T>(this T[][][] inputArray, params Turn[] turns)
	{
		T[][][] array = inputArray;
		foreach (Turn turn in turns)
			array = turn switch
			{
				Structs.Turn.CounterX => array.CounterX(),
				Structs.Turn.CounterY => array.CounterY(),
				Structs.Turn.CounterZ => array.CounterZ(),
				Structs.Turn.ClockX => array.ClockX(),
				Structs.Turn.ClockY => array.ClockY(),
				Structs.Turn.ClockZ => array.ClockZ(),
				_ => inputArray,
			};
		return array;
	}
	public static T[][][] CounterX<T>(this T[][][] inputArray)
	{
		T[][][] outputArray = Initialize<T>(inputArray[0].Length, inputArray.Length, inputArray[0][0].Length);
		for (ushort x = 0; x < inputArray.Length; x++)
			for (ushort y = 0; y < inputArray[x].Length; y++)
				for (ushort z = 0; z < inputArray[x][y].Length; z++)
					outputArray[inputArray[x].Length - 1 - y][x][z] = inputArray[x][y][z];
		return outputArray;
	}
	public static T[][][] CounterY<T>(this T[][][] inputArray)
	{
		T[][][] outputArray = Initialize<T>(inputArray[0].Length, inputArray.Length, inputArray[0][0].Length);
		for (ushort x = 0; x < inputArray.Length; x++)
			for (ushort y = 0; y < inputArray[x].Length; y++)
				for (ushort z = 0; z < inputArray[x][y].Length; z++)
					outputArray[x][y][inputArray[x][y].Length - 1 - z] = inputArray[x][y][z];
		return outputArray;
	}
	public static T[][][] CounterZ<T>(this T[][][] inputArray)
	{
		T[][][] outputArray = Initialize<T>(inputArray[0][0].Length, inputArray.Length, inputArray[0].Length);
		for (ushort x = 0; x < inputArray.Length; x++)
			for (ushort y = 0; y < inputArray[x].Length; y++)
				for (ushort z = 0; z < inputArray[x][y].Length; z++)
					outputArray[inputArray[x][y].Length - 1 - z][y][x] = inputArray[x][y][z];
		return outputArray;
	}
	public static T[][][] ClockX<T>(this T[][][] inputArray)
	{
		T[][][] outputArray = Initialize<T>(inputArray[0].Length, inputArray.Length, inputArray[0][0].Length);
		for (ushort x = 0; x < inputArray.Length; x++)
			for (ushort y = 0; y < inputArray[x].Length; y++)
				for (ushort z = 0; z < inputArray[x][y].Length; z++)
					outputArray[y][inputArray[x].Length - 1 - x][z] = inputArray[x][y][z];
		return outputArray;
	}
	public static T[][][] ClockY<T>(this T[][][] inputArray)
	{
		T[][][] outputArray = Initialize<T>(inputArray.Length, inputArray[0].Length, inputArray[0][0].Length);
		for (ushort x = 0; x < inputArray.Length; x++)
			for (ushort y = 0; y < inputArray[x].Length; y++)
				for (ushort z = 0; z < inputArray[x][y].Length; z++)
					outputArray[x][y][inputArray[x][y].Length - 1 - z] = inputArray[x][y][z];
		return outputArray;
	}
	public static T[][][] ClockZ<T>(this T[][][] inputArray)
	{
		T[][][] outputArray = Initialize<T>(inputArray[0].Length, inputArray[0][0].Length, inputArray.Length);
		for (ushort x = 0; x < inputArray.Length; x++)
			for (ushort y = 0; y < inputArray[x].Length; y++)
				for (ushort z = 0; z < inputArray[x][y].Length; z++)
					outputArray[z][y][inputArray.Length - 1 - x] = inputArray[x][y][z];
		return outputArray;
	}
	#endregion Turns
}
