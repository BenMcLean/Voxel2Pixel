namespace Voxel2Pixel.Model
{
	public interface IModel : IFetch
	{
		int SizeX { get; }
		int SizeY { get; }
		int SizeZ { get; }
		bool IsInside(int x, int y, int z);
		bool IsOutside(int x, int y, int z);
	}
}
