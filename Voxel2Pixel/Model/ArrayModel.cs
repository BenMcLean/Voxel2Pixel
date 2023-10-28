﻿using Voxel2Pixel.Interfaces;

namespace Voxel2Pixel.Model
{
	public class ArrayModel : IModel, ITurnable
	{
		public ArrayModel(byte[][][] voxels) => Voxels = voxels;
		public ArrayModel(ArrayModel other) : this(other.Voxels.DeepCopy()) { }
		public ArrayModel(int sizeX = 1, int sizeY = 1, int sizeZ = 1) : this(Array3D.Initialize<byte>(sizeX, sizeY, sizeZ)) { }
		public ArrayModel(IModel model) : this(model.SizeX, model.SizeY, model.SizeZ)
		{
			for (int x = 0; x < SizeX; x++)
				for (int y = 0; y < SizeY; y++)
					for (int z = 0; z < SizeZ; z++)
						if (model.At(x, y, z) is byte voxel)
							Voxels[x][y][z] = voxel;
		}
		public byte[][][] Voxels { get; set; }
		#region IModel
		public ushort SizeX => (ushort)Voxels.Length;
		public ushort SizeY => (ushort)Voxels[0].Length;
		public ushort SizeZ => (ushort)Voxels[0][0].Length;
		public byte? At(int x, int y, int z) => IsInside(x, y, z) ? Voxels[x][y][z] : (byte?)null;
		public bool IsInside(int x, int y, int z) => !IsOutside(x, y, z);
		public bool IsOutside(int x, int y, int z) => x < 0 || y < 0 || z < 0 || x >= SizeX || y >= SizeY || z >= SizeZ;
		#endregion IModel
		#region ITurnable
		public ITurnable CounterX()
		{
			Voxels = Array3D.CounterX(Voxels);
			return this;
		}
		public ITurnable CounterY()
		{
			Voxels = Array3D.CounterY(Voxels);
			return this;
		}
		public ITurnable CounterZ()
		{
			Voxels = Array3D.CounterZ(Voxels);
			return this;
		}
		public ITurnable ClockX()
		{
			Voxels = Array3D.ClockX(Voxels);
			return this;
		}
		public ITurnable ClockY()
		{
			Voxels = Array3D.ClockY(Voxels);
			return this;
		}
		public ITurnable ClockZ()
		{
			Voxels = Array3D.ClockZ(Voxels);
			return this;
		}
		public ITurnable Reset() => throw new System.NotImplementedException();
		#endregion ITurnable
	}
}
