namespace Voxel2Pixel.Interfaces
{
	public interface IFetch
	{
		byte this[ushort x, ushort y, ushort z] { get; }
	}
}