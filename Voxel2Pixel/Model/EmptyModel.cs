namespace Voxel2Pixel.Model
{
	public class EmptyModel : IModel
	{
		public virtual int SizeX { get; set; }
		public virtual int SizeY { get; set; }
		public virtual int SizeZ { get; set; }
		public virtual byte? At(int x, int y, int z) => 0;
		public virtual bool IsInside(int x, int y, int z) => !IsOutside(x, y, z);
		public virtual bool IsOutside(int x, int y, int z) => x < 0 || y < 0 || z < 0 || x >= SizeX || y >= SizeY || z >= SizeZ;
	}
}
