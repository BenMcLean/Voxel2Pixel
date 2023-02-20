namespace Voxel2Pixel.Model
{
	public class ArrayModel : IModel
	{
		public ArrayModel(byte[][][] voxels) => Voxels = voxels;
		public ArrayModel(int sizeX = 1, int sizeY = 1, int sizeZ = 1) : this(MakeModel(sizeX, sizeY, sizeZ)) { }
		public byte[][][] Voxels;
		public static byte[][][] MakeModel(int sizeX = 1, int sizeY = 1, int sizeZ = 1)
		{
			byte[][][] model = new byte[sizeX][][];
			for (int x = 0; x < sizeX; x++)
			{
				model[x] = new byte[sizeY][];
				for (int y = 0; y < sizeY; y++)
					model[x][y] = new byte[sizeZ];
			}
			return model;
		}
		public ArrayModel DrawBox(byte voxel)
		{
			for (int x = 0; x < SizeX; x++)
			{
				Voxels[x][0][0] = voxel;
				Voxels[x][SizeY - 1][0] = voxel;
				Voxels[x][0][SizeZ - 1] = voxel;
				Voxels[x][SizeY - 1][SizeZ - 1] = voxel;
			}
			for (int y = 1; y < SizeY - 1; y++)
			{
				Voxels[0][y][0] = voxel;
				Voxels[SizeX - 1][y][0] = voxel;
				Voxels[0][y][SizeZ - 1] = voxel;
				Voxels[SizeX - 1][y][SizeZ - 1] = voxel;
			}
			for (int z = 1; z < SizeZ - 1; z++)
			{
				Voxels[0][0][z] = voxel;
				Voxels[SizeX - 1][0][z] = voxel;
				Voxels[0][SizeY - 1][z] = voxel;
				Voxels[SizeX - 1][SizeY - 1][z] = voxel;
			}
			return this;
		}
		#region IModel
		public int SizeX => Voxels.Length;
		public int SizeY => Voxels[0].Length;
		public int SizeZ => Voxels[0][0].Length;
		public byte? At(int x, int y, int z) => IsInside(x, y, z) ? Voxels[x][y][z] : (byte?)null;
		public bool IsInside(int x, int y, int z) => !IsOutside(x, y, z);
		public bool IsOutside(int x, int y, int z) => x < 0 || y < 0 || z < 0 || x >= SizeX || y >= SizeY || z >= SizeZ;
		#endregion IModel
	}
}
