using System.Collections.Generic;
using Voxel2Pixel.Interfaces;

namespace Voxel2Pixel.Model
{
	public abstract class ContainerModel : IModel
	{
		public virtual IModel Model { get; set; }
		#region IModel
		public virtual IEnumerable<Voxel> Voxels => Model.Voxels;
		public virtual byte this[ushort x, ushort y, ushort z] => Model[x, y, z];
		public virtual ushort SizeX => Model.SizeX;
		public virtual ushort SizeY => Model.SizeY;
		public virtual ushort SizeZ => Model.SizeZ;
		public virtual bool IsOutside(ushort x, ushort y, ushort z) => Model.IsOutside(x, y, z);
		#endregion IModel
	}
}
