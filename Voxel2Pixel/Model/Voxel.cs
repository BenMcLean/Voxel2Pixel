using System;

namespace Voxel2Pixel.Model
{
	public struct Voxel
	{
		public uint X, Y, Z, Data;
		public uint this[int index]
		{
			get
			{
				switch (index)
				{
					case 0:
						return X;
					case 1:
						return Y;
					case 2:
						return Z;
					case 3:
						return Data;
					default:
						throw new IndexOutOfRangeException();
				}
			}
			set
			{
				switch (index)
				{
					case 0:
						X = value;
						break;
					case 1:
						Y = value;
						break;
					case 2:
						Z = value;
						break;
					case 3:
						Data = value;
						break;
					default:
						throw new IndexOutOfRangeException();
				}
			}
		}
		public uint[] UInts => new uint[4] { X, Y, Z, Data };
		public Voxel(uint[] uints)
		{
			X = uints[0];
			Y = uints[1];
			Z = uints[2];
			Data = uints[3];
		}
		public Voxel(uint x, uint y, uint z, uint Data)
		{
			X = x;
			Y = y;
			Z = z;
			this.Data = Data;
		}
	}
}
