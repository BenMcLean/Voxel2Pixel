namespace Voxel2Pixel.Model
{
	public readonly record struct Point(int X, int Y);
	public readonly record struct Voxel(ushort X, ushort Y, ushort Z, byte Index);
	public enum VisibleFace : byte
	{
		Front = 0,
		Top = 64,
		Left = 128,
		Right = 192,
	}
	public enum Perspective
	{
		Front, FrontPeak, Overhead, Underneath, Diagonal, DiagonalPeak, Above, Iso, IsoShadow,
	}
}
