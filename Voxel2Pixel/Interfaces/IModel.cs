namespace Voxel2Pixel.Interfaces
{
	public interface IModel : IFetch
	{
		ushort SizeX { get; }
		ushort SizeY { get; }
		ushort SizeZ { get; }
		bool IsInside(ushort x, ushort y, ushort z);
		bool IsOutside(ushort x, ushort y, ushort z);
	}
}
