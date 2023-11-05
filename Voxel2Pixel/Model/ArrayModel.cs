using System.Collections.Generic;
using Voxel2Pixel.Interfaces;

namespace Voxel2Pixel.Model
{
	public class ArrayModel : IModel, ITurnable
	{
		public ArrayModel(byte[][][] voxels) => Array = voxels;
		public ArrayModel(ArrayModel other) : this(other.Array.DeepCopy()) { }
		public ArrayModel(ushort sizeX = 1, ushort sizeY = 1, ushort sizeZ = 1) : this(Array3D.Initialize<byte>(sizeX, sizeY, sizeZ)) { }
		public ArrayModel(IModel model) : this(model.SizeX, model.SizeY, model.SizeZ)
		{
			foreach (Voxel voxel in model.Voxels)
				Array[voxel.X][voxel.Y][voxel.Z] = voxel.@byte;
		}
		public byte[][][] Array { get; set; }
		#region IModel
		public ushort SizeX => (ushort)Array.Length;
		public ushort SizeY => (ushort)Array[0].Length;
		public ushort SizeZ => (ushort)Array[0][0].Length;
		public byte this[ushort x, ushort y, ushort z] => !IsOutside(x, y, z) ? Array[x][y][z] : (byte)0;
		public bool IsOutside(ushort x, ushort y, ushort z) => x >= SizeX || y >= SizeY || z >= SizeZ;
		public IEnumerable<Voxel> Voxels
		{
			get
			{
				for (ushort x = 0; x < SizeX; x++)
					for (ushort y = 0; y < SizeY; y++)
						for (ushort z = 0; z < SizeZ; z++)
							if (Array[x][y][z] is byte @byte && @byte != 0)
								yield return new Voxel(x, y, z, @byte);
			}
		}
		#endregion IModel
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
