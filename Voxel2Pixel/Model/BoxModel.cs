﻿using BenVoxel;
using System;
using System.Collections.Generic;

namespace Voxel2Pixel.Model;

public class BoxModel : ContainerModel
{
	public byte Voxel { get; set; } = 1;
	public bool Overwrite { get; set; } = true;
	public bool IsBorder(int x, int y, int z)
	{
		int borderCount = 0;
		if (x == 0 || x == SizeX - 1)
			borderCount++;
		if (y == 0 || y == SizeY - 1)
			borderCount++;
		if (z == 0 || z == SizeZ - 1)
			borderCount++;
		return borderCount >= 2;
	}
	#region IModel
	public override byte this[ushort x, ushort y, ushort z] =>
		Overwrite ?
			IsBorder(x, y, z) ?
				!this.IsOutside(x, y, z) ?
					Voxel
					: (byte)0
				: Model[x, y, z]
			: Model[x, y, z] is byte voxel && voxel != 0 ?
				voxel
				: !this.IsOutside(x, y, z) && IsBorder(x, y, z) ?
					Voxel
					: (byte)0;
	public override IEnumerator<Voxel> GetEnumerator() => throw new NotImplementedException();
	#endregion IModel
}
