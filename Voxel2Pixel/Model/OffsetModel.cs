using System;
using System.Collections.Generic;

namespace Voxel2Pixel.Model
{
	public class OffsetModel : ContainerModel
	{
		public int OffsetX { get; set; } = 0;
		public int OffsetY { get; set; } = 0;
		public int OffsetZ { get; set; } = 0;
		#region IModel
		public override byte this[ushort x, ushort y, ushort z] =>
			x + OffsetX >= 0 && y + OffsetY >= 0 && z + OffsetZ >= 0
			&& !IsOutside((ushort)(x + OffsetX), (ushort)(y + OffsetY), (ushort)(z + OffsetZ)) ?
				Model[(ushort)(x + OffsetX), (ushort)(y + OffsetY), (ushort)(z + OffsetZ)]
				: (byte)0;
		public override IEnumerable<Voxel> Voxels => throw new NotImplementedException();
		#endregion IModel
	}
}
