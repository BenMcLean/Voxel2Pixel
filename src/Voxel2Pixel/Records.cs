using BenVoxel.Structs;

namespace Voxel2Pixel;

public readonly record struct Point(int X, int Y)
{
	public override string ToString() => $"[{X},{Y}]";
	public Point(Point3D point3d, ushort sizeY = 0) : this(
		X: point3d.X,
		Y: sizeY == 0 ? point3d.Y : sizeY - 1 - point3d.Y)
	{ }
}
public enum VisibleFace : byte
{
	Front = 0,
	Top = 64,
	Left = 128,
	Right = 192,
}
public enum Perspective
{//May need revision to follow the correct terms shown in this graphic: https://en.wikipedia.org/wiki/File:Comparison_of_graphical_projections.svg
	Front, Overhead, Underneath, Diagonal, Above, Iso, IsoUnderneath, IsoEight, IsoEightUnderneath, Stacked, StackedUnderneath, ZSlices,
}
