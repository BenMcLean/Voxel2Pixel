using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BenVoxel;

[JsonConverter(typeof(Point3DArrayConverter))]
public readonly record struct Point3D() : IBinaryWritable
{
	public int X { get; init; }
	public int Y { get; init; }
	public int Z { get; init; }
	public Point3D(int x, int y, int z) : this()
	{
		X = x;
		Y = y;
		Z = z;
	}
	public Point3D(params int[] coordinates) : this(coordinates[0], coordinates[1], coordinates[2]) { }
	public int[] ToArray() => [X, Y, Z];
	public int this[int axis] => axis switch
	{
		0 => X,
		1 => Y,
		2 => Z,
		_ => throw new IndexOutOfRangeException(),
	};
	public static explicit operator Point3D(Voxel voxel) => new(voxel.X, voxel.Y, voxel.Z);
	public Point3D(Stream stream) : this()
	{
		using BinaryReader reader = new(
			input: stream,
			encoding: Encoding.UTF8,
			leaveOpen: true);
		X = reader.ReadInt32();
		Y = reader.ReadInt32();
		Z = reader.ReadInt32();
	}
	public Point3D(BinaryReader reader) : this(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()) { }
	#region IBinaryWritable
	public void Write(Stream stream)
	{
		using BinaryWriter writer = new(
			output: stream,
			encoding: Encoding.UTF8,
			leaveOpen: true);
		Write(writer);
	}
	public void Write(BinaryWriter writer)
	{
		writer.Write(X);
		writer.Write(Y);
		writer.Write(Z);
	}
	#endregion IBinaryWritable
	#region JSON
	public class Point3DArrayConverter : JsonConverter<Point3D>
	{
		public override Point3D Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => new(JsonSerializer.Deserialize<int[]>(ref reader, options));
		public override void Write(Utf8JsonWriter writer, Point3D value, JsonSerializerOptions options) => JsonSerializer.Serialize(writer, value.ToArray(), options);
	}
	#endregion JSON
}
