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
		public virtual byte? At(int x, int y, int z) => 0;
		public virtual bool IsInside(int x, int y, int z) => !IsOutside(x, y, z);
		public virtual bool IsOutside(int x, int y, int z) => x < 0 || y < 0 || z < 0 || x >= SizeX || y >= SizeY || z >= SizeZ;
		#endregion IModel
		#region ISparseModel
		public virtual IEnumerable<Voxel> Voxels => Enumerable.Empty<Voxel>();
		#endregion ISparseModel
	}
}
