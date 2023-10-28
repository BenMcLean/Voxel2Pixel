namespace Voxel2Pixel.Interfaces
{
	public interface IModel : IFetch
	{
		ushort SizeX { get; }
		ushort SizeY { get; }
		ushort SizeZ { get; }
		bool IsInside(int x, int y, int z);
		bool IsOutside(int x, int y, int z);
	}
}
