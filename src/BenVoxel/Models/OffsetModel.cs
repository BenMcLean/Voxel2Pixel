using System;
using System.Collections.Generic;
using BenVoxel;
using BenVoxel.Structs;

namespace Voxel2Pixel.Model;

public class OffsetModel : ContainerModel
{
	public int OffsetX { get; set; } = 0;
	public int OffsetY { get; set; } = 0;
	public int OffsetZ { get; set; } = 0;
	public ushort ScaleX { get; set; } = 1;
	public ushort ScaleY { get; set; } = 1;
	public ushort ScaleZ { get; set; } = 1;
	#region IModel
	public override byte this[ushort x, ushort y, ushort z]
	{
		get
		{
			int x2 = (x / ScaleX) + OffsetX,
				y2 = (y / ScaleY) + OffsetY,
				z2 = (z / ScaleZ) + OffsetZ;
			return x2 >= 0 && y2 >= 0 && z2 >= 0
				&& !this.IsOutside((ushort)x2, (ushort)y2, (ushort)z2) ?
					Model[(ushort)x2, (ushort)y2, (ushort)z2]
					: (byte)0;
		}
	}
	public override ushort SizeX => (ushort)(base.SizeX * ScaleX);
	public override ushort SizeY => (ushort)(base.SizeY * ScaleY);
	public override ushort SizeZ => (ushort)(base.SizeZ * ScaleZ);
	public override IEnumerator<Voxel> GetEnumerator() => throw new NotImplementedException();
	#endregion IModel
}
