using System.Collections.Generic;
using System.Linq;
using Voxel2Pixel.Interfaces;

namespace Voxel2Pixel.Model
{
	public class EmptyModel : IModel
	{
		#region IModel
		public virtual ushort SizeX { get; set; }
		public virtual ushort SizeY { get; set; }
		public virtual ushort SizeZ { get; set; }
		public virtual byte this[ushort x, ushort y, ushort z] => 0;
		public virtual IEnumerable<Voxel> Voxels => Enumerable.Empty<Voxel>();
		#endregion IModel
	}
}
