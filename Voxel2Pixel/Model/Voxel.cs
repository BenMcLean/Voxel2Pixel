using System;

namespace Voxel2Pixel.Model
{
	public struct Voxel
	{
		public ushort X, Y, Z;
		public byte @byte;
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
						return @byte;
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
						@byte = (byte)value;
						break;
					default:
						throw new IndexOutOfRangeException();
				}
			}
		}
		public ushort[] @ushort => new ushort[4] { X, Y, Z, @byte };
		public Voxel(params ushort[] @ushort) : this(@ushort, (byte)@ushort[3]) { }
		public Voxel(ushort[] @ushort, byte @byte) : this(@ushort[0], @ushort[1], @ushort[2], @byte) { }
		public Voxel(ushort x, ushort y, ushort z, byte @byte)
		{
			X = x;
			Y = y;
			Z = z;
			this.@byte = @byte;
		}
	}
}
