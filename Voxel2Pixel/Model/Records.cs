namespace Voxel2Pixel.Model
{
	public readonly record struct Point(ushort X, ushort Y);
	public readonly record struct Voxel(ushort X, ushort Y, ushort Z, byte Index);
}
