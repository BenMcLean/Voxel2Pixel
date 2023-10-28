using System;
using System.Collections.Generic;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;

namespace Voxel2Pixel.SparseModel
{
	public class SparseModel : ISparseModel, IModel
	{
		#region ISparseModel
		public IEnumerable<Voxel> Voxels => throw new NotImplementedException();
		public ushort SizeX { get; set; }
		public ushort SizeY { get; set; }
		public ushort SizeZ { get; set; }
		#endregion ISparseModel
		#region IModel
		public byte? At(int x, int y, int z)
		{
			throw new NotImplementedException();
		}
		public bool IsInside(int x, int y, int z)
		{
			throw new NotImplementedException();
		}
		public bool IsOutside(int x, int y, int z)
		{
			throw new NotImplementedException();
		}
		#endregion IModel
	}
}
