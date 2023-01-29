namespace Voxel2Pixel.Model
{
	public class ArrayModel : IModel
	{
		public ArrayModel(byte[][][] model) => Model = model;
		public ArrayModel(int sizeX = 1, int sizeY = 1, int sizeZ = 1)
		{
			Model = new byte[sizeX][][];
			for (int x = 0; x < SizeX; x++)
			{
				Model[x] = new byte[sizeY][];
				for (int y = 0; y < SizeY; y++)
					Model[x][y] = new byte[sizeZ];
			}
		}
		public byte[][][] Model;
		#region IModel
		public int SizeX => Model.Length;
		public int SizeY => Model[0].Length;
		public int SizeZ => Model[0][0].Length;
		public byte? At(int x, int y, int z) => IsInside(x, y, z) ? Model[x][y][z] : (byte?)null;
		public bool IsInside(int x, int y, int z) => !IsOutside(x, y, z);
		public bool IsOutside(int x, int y, int z) => x < 0 || y < 0 || z < 0 || x >= SizeX || y >= SizeY || z >= SizeZ;
		#endregion IModel
	}
}
