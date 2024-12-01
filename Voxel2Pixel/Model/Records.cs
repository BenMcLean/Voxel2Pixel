namespace Voxel2Pixel.Model;

public readonly record struct Point(int X, int Y);
public enum VisibleFace : byte
{
	Front = 0,
	Top = 64,
	Left = 128,
	Right = 192,
}
public enum Perspective
{//May need revision to follow the correct terms shown in this graphic: https://en.wikipedia.org/wiki/File:Comparison_of_graphical_projections.svg
	Front, FrontPeak, Overhead, Underneath, Diagonal, DiagonalPeak, Above, Iso, IsoShadow, Stacked, StackedPeak, ZSlices, ZSlicesPeak,
}
public enum Turn
{
	Reset, ClockX, ClockY, ClockZ, CounterX, CounterY, CounterZ,
}
