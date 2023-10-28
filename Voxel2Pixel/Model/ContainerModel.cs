using Voxel2Pixel.Interfaces;

namespace Voxel2Pixel.Model
{
	public abstract class ContainerModel : IModel
	{
		public virtual IModel Model { get; set; }
		#region IModel
		public virtual ushort SizeX => Model.SizeX;
		public virtual ushort SizeY => Model.SizeY;
		public virtual ushort SizeZ => Model.SizeZ;
		public virtual byte? At(int x, int y, int z) => Model.At(x, y, z);
		public virtual bool IsInside(int x, int y, int z) => Model.IsInside(x, y, z);
		public virtual bool IsOutside(int x, int y, int z) => Model.IsOutside(x, y, z);
		#endregion IModel
	}
}
