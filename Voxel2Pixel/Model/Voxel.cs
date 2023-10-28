using System;

namespace Voxel2Pixel.Model
{
	public struct Voxel
	{
		public byte X, Y, Z, Index;
		public byte this[int index]
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
						Index = value;
						break;
					default:
						throw new IndexOutOfRangeException();
				}
			}
		}
		public byte[] Bytes => new byte[4] { X, Y, Z, Index };
		public Voxel(byte[] bytes)
		{
			X = bytes[0];
			Y = bytes[1];
			Z = bytes[2];
			Index = bytes[3];
		}
		public Voxel(byte x, byte y, byte z, byte index)
		{
			X = x;
			Y = y;
			Z = z;
			Index = index;
		}
	}
}
