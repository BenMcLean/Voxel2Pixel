using System.Collections.Generic;
using System.Linq;
using Voxel2Pixel.Interfaces;

namespace Voxel2Pixel.Model
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
		public byte At(ushort x, ushort y, ushort z) => IsInside(x, y, z) ?
			Voxels.Where(voxel => voxel.X == x && voxel.Y == y && voxel.Z == z)
				.Select(voxel => voxel.@byte)
				.FirstOrDefault()
			: (byte)0;
		#endregion IFetch
		#region IModel
		public ushort SizeX { get; set; }
		public ushort SizeY { get; set; }
		public ushort SizeZ { get; set; }
		public bool IsInside(ushort x, ushort y, ushort z) => !IsOutside(x, y, z);
		public bool IsOutside(ushort x, ushort y, ushort z) => x >= SizeX || y >= SizeY || z >= SizeZ;
		#endregion IModel
		#region ISparseModel
		public IEnumerable<Voxel> Voxels => List;
		#endregion ISparseModel
	}
}
