namespace Voxel2Pixel.Model
{
	public class ArrayModel : IModel, ITurnable
	{
		public ArrayModel(byte[][][] voxels) => Voxels = voxels;
		public ArrayModel(int sizeX = 1, int sizeY = 1, int sizeZ = 1) : this(MakeModel(sizeX, sizeY, sizeZ)) { }
		public ArrayModel(IModel model) : this(model.SizeX, model.SizeY, model.SizeZ)
		{
			for (int x = 0; x < SizeX; x++)
				for (int y = 0; y < SizeY; y++)
					for (int z = 0; z < SizeZ; z++)
						if (model.At(x, y, z) is byte voxel)
							Voxels[x][y][z] = voxel;
		}
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
		#region ITurnable
		public ITurnable CounterX()
		{
			Voxels = CounterX(Voxels);
			return this;
		}
		public static byte[][][] CounterX(byte[][][] inputArray)
		{
			byte[][][] outputArray = MakeModel(inputArray[0].Length, inputArray.Length, inputArray[0][0].Length);
			for (int x = 0; x < inputArray.Length; x++)
				for (int y = 0; y < inputArray[0].Length; y++)
					for (int z = 0; z < inputArray[0][0].Length; z++)
						outputArray[inputArray[0].Length - 1 - y][x][z] = inputArray[x][y][z];
			return outputArray;
		}
		public ITurnable CounterY()
		{
			Voxels = CounterY(Voxels);
			return this;
		}
		public static byte[][][] CounterY(byte[][][] inputArray)
		{
			byte[][][] outputArray = MakeModel(inputArray[0].Length, inputArray.Length, inputArray[0][0].Length);
			for (int x = 0; x < inputArray.Length; x++)
				for (int y = 0; y < inputArray[0].Length; y++)
					for (int z = 0; z < inputArray[0][0].Length; z++)
						outputArray[x][y][inputArray[0][0].Length - 1 - z] = inputArray[x][y][z];
			return outputArray;
		}
		public ITurnable CounterZ()
		{
			Voxels = CounterZ(Voxels);
			return this;
		}
		public static byte[][][] CounterZ(byte[][][] inputArray)
		{
			byte[][][] outputArray = MakeModel(inputArray[0][0].Length, inputArray.Length, inputArray[0].Length);
			for (int x = 0; x < inputArray.Length; x++)
				for (int y = 0; y < inputArray[0].Length; y++)
					for (int z = 0; z < inputArray[0][0].Length; z++)
						outputArray[inputArray[0][0].Length - 1 - z][y][x] = inputArray[x][y][z];
			return outputArray;
		}
		public ITurnable ClockX()
		{
			Voxels = ClockX(Voxels);
			return this;
		}
		public static byte[][][] ClockX(byte[][][] inputArray)
		{
			byte[][][] outputArray = MakeModel(inputArray[0].Length, inputArray.Length, inputArray[0][0].Length);
			for (int x = 0; x < inputArray.Length; x++)
				for (int y = 0; y < inputArray[0].Length; y++)
					for (int z = 0; z < inputArray[0][0].Length; z++)
						outputArray[y][inputArray.Length - 1 - x][z] = inputArray[x][y][z];
			return outputArray;
		}
		public ITurnable ClockY()
		{
			Voxels = ClockY(Voxels);
			return this;
		}
		public static byte[][][] ClockY(byte[][][] inputArray)
		{
			byte[][][] outputArray = MakeModel(inputArray[0].Length, inputArray.Length, inputArray[0][0].Length);
			for (int x = 0; x < inputArray.Length; x++)
				for (int y = 0; y < inputArray[0].Length; y++)
					for (int z = 0; z < inputArray[0][0].Length; z++)
						outputArray[x][y][inputArray[0][0].Length - 1 - z] = inputArray[z][y][x];
			return outputArray;
		}
		public ITurnable ClockZ()
		{
			Voxels = ClockZ(Voxels);
			return this;
		}
		public static byte[][][] ClockZ(byte[][][] inputArray)
		{
			byte[][][] outputArray = MakeModel(inputArray[0].Length, inputArray[0][0].Length, inputArray.Length);
			for (int x = 0; x < inputArray.Length; x++)
				for (int y = 0; y < inputArray[0].Length; y++)
					for (int z = 0; z < inputArray[0][0].Length; z++)
						outputArray[z][y][inputArray.Length - 1 - x] = inputArray[x][y][z];
			return outputArray;
		}
		public ITurnable Reset() => throw new System.NotImplementedException();
		#endregion ITurnable
	}
}
