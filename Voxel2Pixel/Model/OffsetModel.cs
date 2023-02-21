namespace Voxel2Pixel.Model
{
	public class OffsetModel : ContainerModel
	{
		public int OffsetX { get; set; } = 0;
		public int OffsetY { get; set; } = 0;
		public int OffsetZ { get; set; } = 0;
		#region IModel
		public override byte? At(int x, int y, int z) => IsInside(x + OffsetX, y + OffsetY, z + OffsetZ) ? Model.At(x + OffsetX, y + OffsetY, z + OffsetZ) : 0;
		#endregion IModel
	}
}
