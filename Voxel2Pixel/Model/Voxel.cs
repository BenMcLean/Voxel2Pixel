using System;

namespace Voxel2Pixel.Model
{
	public struct Voxel
	{
		public ushort X, Y, Z, Data;
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
		public ushort[] Ushorts => new ushort[4] { X, Y, Z, Data };
		public byte Index => (byte)Data;
		public byte @byte => (byte)(Data >> 1);
		public Voxel(ushort[] @ushort)
		{
			X = @ushort[0];
			Y = @ushort[1];
			Z = @ushort[2];
			Data = @ushort[3];
		}
		public Voxel(ushort x, ushort y, ushort z, ushort data)
		{
			X = x;
			Y = y;
			Z = z;
			Data = data;
		}
	}
}
