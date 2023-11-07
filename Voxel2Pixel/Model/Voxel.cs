using System;

namespace Voxel2Pixel.Model
{
	public struct Voxel
	{
		public ushort X, Y, Z;
		public byte Index;
		public ushort this[int index]
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
						return Index;
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
						Index = (byte)value;
						break;
					default:
						throw new IndexOutOfRangeException();
				}
			}
		}
		public ushort[] @ushort => new ushort[4] { X, Y, Z, Index };
		public Voxel(params ushort[] @ushort) : this(@ushort, (byte)@ushort[3]) { }
		public Voxel(ushort[] @ushort, byte index) : this(@ushort[0], @ushort[1], @ushort[2], index) { }
		public Voxel(ushort x, ushort y, ushort z, byte index)
		{
			X = x;
			Y = y;
			Z = z;
			Index = index;
		}
	}
}
