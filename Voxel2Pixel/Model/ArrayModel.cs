﻿using System.Collections;
using System.Collections.Generic;
using Voxel2Pixel.Interfaces;

namespace Voxel2Pixel.Model
{
	public class ArrayModel : IEditableModel, ITurnable
	{
		public ArrayModel(byte[][][] voxels) => Array = voxels;
		public ArrayModel(ArrayModel other) : this(other.Array.DeepCopy()) { }
		public ArrayModel(ushort sizeX = 1, ushort sizeY = 1, ushort sizeZ = 1) : this(Array3D.Initialize<byte>(sizeX, sizeY, sizeZ)) { }
		public ArrayModel(IModel model) : this(model.SizeX, model.SizeY, model.SizeZ)
		{
			foreach (Voxel voxel in model)
				Array[voxel.X][voxel.Y][voxel.Z] = voxel.Index;
		}
		public byte[][][] Array { get; set; }
		#region IEditableModel
		public ushort SizeX => (ushort)Array.Length;
		public ushort SizeY => (ushort)Array[0].Length;
		public ushort SizeZ => (ushort)Array[0][0].Length;
		public byte this[ushort x, ushort y, ushort z]
		{
			get => !this.IsOutside(x, y, z) ? Array[x][y][z] : (byte)0;
			set => Array[x][y][z] = value;
		}
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		public IEnumerator<Voxel> GetEnumerator()
		{
			for (ushort x = 0; x < SizeX; x++)
				for (ushort y = 0; y < SizeY; y++)
					for (ushort z = 0; z < SizeZ; z++)
						if (Array[x][y][z] is byte index && index != 0)
							yield return new Voxel(x, y, z, index);
		}
		#endregion IEditableModel
		#region ITurnable
		public ITurnable CounterX()
		{
			Array = Array3D.CounterX(Array);
			return this;
		}
		public ITurnable CounterY()
		{
			Array = Array3D.CounterY(Array);
			return this;
		}
		public ITurnable CounterZ()
		{
			Array = Array3D.CounterZ(Array);
			return this;
		}
		public ITurnable ClockX()
		{
			Array = Array3D.ClockX(Array);
			return this;
		}
		public ITurnable ClockY()
		{
			Array = Array3D.ClockY(Array);
			return this;
		}
		public ITurnable ClockZ()
		{
			Array = Array3D.ClockZ(Array);
			return this;
		}
		public ITurnable Reset() => throw new System.NotImplementedException();
		#endregion ITurnable
	}
}
