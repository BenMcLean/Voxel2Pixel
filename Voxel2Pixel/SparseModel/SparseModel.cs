using System.Collections.Generic;
using System.Linq;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;

namespace Voxel2Pixel.SparseModel
{
	public class SparseModel : ISparseModel, IModel
	{
		#region ISparseModel
		public IEnumerable<Voxel> Voxels => List;
		public List<Voxel> List;
		public ushort SizeX { get; set; }
		public ushort SizeY { get; set; }
		public ushort SizeZ { get; set; }
		#endregion ISparseModel
		#region IModel
		public byte? At(int x, int y, int z) => IsInside(x, y, z) ?
			Voxels.Where(voxel => voxel.X == x && voxel.Y == y && voxel.Z == z)
				.Select(voxel => voxel.Index)
				.FirstOrDefault()
			: (byte?)null;
		public bool IsInside(int x, int y, int z) => !IsOutside(x, y, z);
		public bool IsOutside(int x, int y, int z) => x < 0 || y < 0 || z < 0 || x >= SizeX || y >= SizeY || z >= SizeZ;
		#endregion IModel
	}
}
