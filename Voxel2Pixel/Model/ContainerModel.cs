using Voxel2Pixel.Interfaces;

namespace Voxel2Pixel.Model
{
	public abstract class ContainerModel : IModel
	{
		public virtual IModel Model { get; set; }
		#region IFetch
		public virtual byte this[ushort x, ushort y, ushort z] => Model[x, y, z];
		#endregion IFetch
		#region IModel
		public virtual ushort SizeX => Model.SizeX;
		public virtual ushort SizeY => Model.SizeY;
		public virtual ushort SizeZ => Model.SizeZ;
		public virtual bool IsOutside(ushort x, ushort y, ushort z) => Model.IsOutside(x, y, z);
		#endregion IModel
	}
}
