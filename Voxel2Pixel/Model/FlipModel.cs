namespace Voxel2Pixel.Model
{
	public class FlipModel : IModel
	{
		public IModel Model { get; set; }
		public bool FlipX { get; set; } = false;
		public bool FlipY { get; set; } = false;
		public bool FlipZ { get; set; } = false;
		#region IModel
		public int SizeX => Model.SizeX;
		public int SizeY => Model.SizeY;
		public int SizeZ => Model.SizeZ;
		public byte? At(int x, int y, int z) => Model.At(
			x: FlipX ? SizeX - 1 - x : x,
			y: FlipY ? SizeY - 1 - y : y,
			z: FlipZ ? SizeZ - 1 - z : z);
		public bool IsInside(int x, int y, int z) => !IsOutside(x, y, z);
		public bool IsOutside(int x, int y, int z) => x < 0 || y < 0 || z < 0 || x >= SizeX || y >= SizeY || z >= SizeZ;
		#endregion IModel
	}
}
