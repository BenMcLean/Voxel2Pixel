namespace Voxel2Pixel.Model
{
	public class OffsetModel : IModel
	{
		public IModel Model
		{
			get => model;
			set
			{
				model = value;
				if (SizeX < 1)
					SizeX = model.SizeX;
				if (SizeY < 1)
					SizeY = model.SizeY;
				if (SizeZ < 1)
					SizeZ = model.SizeZ;
			}
		}
		private IModel model;
		public int OffsetX { get; set; } = 0;
		public int OffsetY { get; set; } = 0;
		public int OffsetZ { get; set; } = 0;
		#region IModel
		public int SizeX { get; set; } = 0;
		public int SizeY { get; set; } = 0;
		public int SizeZ { get; set; } = 0;
		public byte? At(int x, int y, int z) => Model.At(x + OffsetX, y + OffsetY, z + OffsetZ);
		public bool IsInside(int x, int y, int z) => !IsOutside(x, y, z);
		public bool IsOutside(int x, int y, int z) => x < 0 || y < 0 || z < 0 || x >= SizeX || y >= SizeY || z >= SizeZ;
		#endregion IModel
	}
}
