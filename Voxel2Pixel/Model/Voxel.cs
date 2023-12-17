using System;

namespace Voxel2Pixel.Model
{
	public struct Voxel : IEquatable<Voxel>
	{
		public ushort X, Y, Z;
		public byte Index;
		public ushort this[int @int]
		{
			get
			{
				switch (@int)
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
				switch (@int)
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
		public ushort[] ToArray => new ushort[4] { X, Y, Z, Index };
		public Voxel(params ushort[] @ushort) : this(@ushort, (byte)@ushort[3]) { }
		public Voxel(ushort[] @ushort, byte index) : this(@ushort[0], @ushort[1], @ushort[2], index) { }
		public Voxel(Voxel voxel) : this(voxel.X, voxel.Y, voxel.Z, voxel.Index) { }
		public Voxel(ushort x, ushort y, ushort z, byte index)
		{
			X = x;
			Y = y;
			Z = z;
			Index = index;
		}
		public static bool operator ==(Voxel a, Voxel b) => a.Equals(b);
		public static bool operator !=(Voxel a, Voxel b) => !a.Equals(b);
		public override bool Equals(object o) => o is Voxel v && Equals(v);
		public bool Equals(Voxel other) =>
			X == other.X
			&& Y == other.Y
			&& Z == other.Z
			&& Index == other.Index;
		public override int GetHashCode() => HashCode.Combine(X, Y, Z, Index);
	}
}
