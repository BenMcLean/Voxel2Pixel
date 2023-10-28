using System.Collections.Generic;
using System.Linq;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;

namespace Voxel2Pixel.SparseModel
{
	public class ListModel : ISparseModel
	{
		public List<Voxel> List;
		public ListModel() { }
		public ListModel(ISparseModel model)
		{
			SizeX = model.SizeX;
			SizeY = model.SizeY;
			SizeZ = model.SizeZ;
			List = model.Voxels.ToList();
		}
		public ListModel(IModel model)
		{
			SizeX = model.SizeX;
			SizeY = model.SizeY;
			SizeZ = model.SizeZ;
			List = new List<Voxel>();
			for (ushort x = 0; x < SizeX; x++)
				for (ushort y = 0; y < SizeY; y++)
					for (ushort z = 0; z < SizeZ; z++)
						if (model.At(x, y, z) is byte @byte && @byte != 0)
							List.Add(new Voxel(x, y, z, @byte));
		}
		#region IFetch
		public byte? At(int x, int y, int z) => IsInside(x, y, z) ?
			Voxels.Where(voxel => voxel.X == x && voxel.Y == y && voxel.Z == z)
				.Select(voxel => voxel.@byte)
				.FirstOrDefault()
			: (byte?)null;
		#endregion IFetch
		#region IModel
		public ushort SizeX { get; set; }
		public ushort SizeY { get; set; }
		public ushort SizeZ { get; set; }
		public bool IsInside(int x, int y, int z) => !IsOutside(x, y, z);
		public bool IsOutside(int x, int y, int z) => x < 0 || y < 0 || z < 0 || x >= SizeX || y >= SizeY || z >= SizeZ;
		#endregion IModel
		#region ISparseModel
		public IEnumerable<Voxel> Voxels => List;
		#endregion ISparseModel
	}
}
