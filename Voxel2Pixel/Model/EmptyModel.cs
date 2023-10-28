﻿using System.Collections.Generic;
using System.Linq;
using Voxel2Pixel.Interfaces;

namespace Voxel2Pixel.Model
{
	public class EmptyModel : IModel, ISparseModel
	{
		#region IModel
		public virtual ushort SizeX { get; set; }
		public virtual ushort SizeY { get; set; }
		public virtual ushort SizeZ { get; set; }
		public virtual byte At(ushort x, ushort y, ushort z) => 0;
		public virtual bool IsInside(ushort x, ushort y, ushort z) => !IsOutside(x, y, z);
		public virtual bool IsOutside(ushort x, ushort y, ushort z) => x >= SizeX || y >= SizeY || z >= SizeZ;
		#endregion IModel
		#region ISparseModel
		public virtual IEnumerable<Voxel> Voxels => Enumerable.Empty<Voxel>();
		#endregion ISparseModel
	}
}
