using System;
using System.IO;
using System.Text;
using Voxel2Pixel.Interfaces;

namespace Voxel2Pixel.Model
{
	public readonly record struct Point(int X, int Y);
	public readonly record struct Point3D(int X, int Y, int Z) : IBinaryWritable
	{
		public Point3D(params int[] coordinates) : this(coordinates[0], coordinates[1], coordinates[2]) { }
		public int[] ToArray() => [X, Y, Z];
		public int this[int axis] => axis switch
		{
			0 => X,
			1 => Y,
			2 => Z,
			_ => throw new IndexOutOfRangeException(),
		};
		public static explicit operator Point3D(Voxel voxel) => new()
		{
			X = voxel.X,
			Y = voxel.Y,
			Z = voxel.Z,
		};
		public Point3D(Stream stream) : this(new BinaryReader(input: stream, encoding: Encoding.UTF8, leaveOpen: true)) { }
		public Point3D(BinaryReader reader) : this(X: reader.ReadInt32(), Y: reader.ReadInt32(), Z: reader.ReadInt32()) { }
		#region IBinaryWritable
		public void Write(Stream stream) => Write(new BinaryWriter(output: stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true));
		public void Write(BinaryWriter writer)
		{
			writer.Write(X);
			writer.Write(Y);
			writer.Write(Z);
		}
		#endregion IBinaryWritable
	}
	public readonly record struct Voxel(ushort X, ushort Y, ushort Z, byte Index)
	{
		public static implicit operator Point3D(Voxel voxel) => new()
		{
			X = voxel.X,
			Y = voxel.Y,
			Z = voxel.Z,
		};
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
		Front, FrontPeak, Overhead, Underneath, Diagonal, DiagonalPeak, Above, Iso, IsoShadow, Stacked, ZSlices,
	}
	public enum Turn
	{
		Reset, ClockX, ClockY, ClockZ, CounterX, CounterY, CounterZ,
	}
}
