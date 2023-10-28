using Voxel2Pixel.Interfaces;

namespace Voxel2Pixel.Model
{
    public class FlipModel : IModel
	{
		public IModel Model { get; set; }
		public bool FlipX { get; set; } = false;
		public bool FlipY { get; set; } = false;
		public bool FlipZ { get; set; } = false;
		public FlipModel Set(params bool?[] @bool)
		{
			if (@bool.Length > 0)
			{
				if (@bool[0] is bool flipX)
					FlipX = flipX;
				if (@bool.Length > 1)
				{
					if (@bool[1] is bool flipY)
						FlipY = flipY;
					if (@bool.Length > 2
						&& @bool[2] is bool flipZ)
						FlipZ = flipZ;
				}
			}
			return this;
		}
		public bool[] Get => new bool[3] { FlipX, FlipY, FlipZ };
		#region IModel
		public ushort SizeX => Model.SizeX;
		public ushort SizeY => (ushort)Model.SizeY;
		public ushort SizeZ => (ushort)Model.SizeZ;
		public byte? At(int x, int y, int z) => Model.At(
			x: FlipX ? SizeX - 1 - x : x,
			y: FlipY ? SizeY - 1 - y : y,
			z: FlipZ ? SizeZ - 1 - z : z);
		public bool IsInside(int x, int y, int z) => !IsOutside(x, y, z);
		public bool IsOutside(int x, int y, int z) => x < 0 || y < 0 || z < 0 || x >= SizeX || y >= SizeY || z >= SizeZ;
		#endregion IModel
	}
}
