namespace Voxel2Pixel.Model
{
	public class EmptyModel : IModel
	{
		public EmptyModel(int sizeX = 1, int sizeY = 1, int sizeZ = 1)
		{
			SizeX = sizeX;
			SizeY = sizeY;
			SizeZ = sizeZ;
		}
		#region IModel
		public int SizeX { get; set; }
		public int SizeY { get; set; }
		public int SizeZ { get; set; }
		public byte? At(int x, int y, int z) => 0;
		public bool IsInside(int x, int y, int z) => !IsOutside(x, y, z);
		public bool IsOutside(int x, int y, int z) => x < 0 || y < 0 || z < 0 || x >= SizeX || y >= SizeY || z >= SizeZ;
		#endregion IModel
	}
}
