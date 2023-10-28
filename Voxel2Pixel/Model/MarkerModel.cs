﻿namespace Voxel2Pixel.Model
{
	/// <summary>
	/// Adds one voxel on top of another model to mark a coordinate
	/// </summary>
	public class MarkerModel : ContainerModel
	{
		#region Data members
		public byte Voxel { get; set; } = 1;
		public bool Overwrite { get; set; } = true;
		public ushort X { get; set; } = 0;
		public ushort Y { get; set; } = 0;
		public ushort Z { get; set; } = 0;
		#endregion Data members
		#region IFetch
		public override byte At(ushort x, ushort y, ushort z) =>
			x == X && y == Y && z == Z ?
				Overwrite ?
					Voxel
					: Model.At(x, y, z) is byte @byte && @byte != 0 ?
						@byte
						: Voxel
				: Model.At(x, y, z);
		#endregion IFetch
	}
}
