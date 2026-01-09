using System;
using System.IO;
using System.Text;

namespace BenVoxel;

public static class ExtensionMethods
{
	#region IModel
	/// <summary>
	/// Checking for being out of bounds can involve fewer comparisons than checking for being in bounds.
	/// </summary>
	/// <param name="coordinates">3D coordinates</param>
	/// <returns>true if coordinates are outside the bounds of the model</returns>
	public static bool IsOutside(this IModel model, params ushort[] coordinates) => coordinates[0] >= model.SizeX || coordinates[1] >= model.SizeY || coordinates[2] >= model.SizeZ;
	public static Point3D Center(this IModel model) => new(model.SizeX >> 1, model.SizeY >> 1, model.SizeZ >> 1);
	public static Point3D BottomCenter(this IModel model) => new(model.SizeX >> 1, model.SizeY >> 1, 0);
	public static byte Set(this IEditableModel model, Voxel voxel) => model[voxel.X, voxel.Y, voxel.Z] = voxel.Material;
	#endregion IModel
	#region IBinaryWritable
	/// <summary>
	/// Writes a RIFF chunk with content provided by a delegate
	/// </summary>
	/// <param name="writer">The BinaryWriter to write to</param>
	/// <param name="fourCC">4-character ASCII identifier for the chunk</param>
	/// <param name="writeContent">Action that writes the chunk content</param>
	/// <returns>The writer for method chaining</returns>
	public static BinaryWriter RIFF(this BinaryWriter writer, string fourCC, Action<BinaryWriter> writeContent)
	{
		if (fourCC.Length != 4)
			throw new ArgumentException(message: "RIFF chunk identifier must be exactly 4 characters", paramName: nameof(fourCC));
		using MemoryStream contentStream = new();
		using (BinaryWriter contentWriter = new(
			output: contentStream,
			encoding: Encoding.UTF8,
			leaveOpen: true))
			writeContent(contentWriter);
		byte[] content = contentStream.ToArray();
		writer.Write(Encoding.ASCII.GetBytes(fourCC), 0, 4);
		writer.Write((uint)content.Length);
		writer.Write(content);
		writer.Flush();
		return writer;
	}
	/// <summary>
	/// Writes an IBinaryWritable object as a RIFF chunk
	/// </summary>
	/// <param name="writer">The BinaryWriter to write to</param>
	/// <param name="fourCC">4-character ASCII identifier for the chunk</param> 
	/// <param name="writable">The object to write as chunk content</param>
	/// <returns>The writer for method chaining</returns>
	public static BinaryWriter RIFF(this IBinaryWritable writable, string fourCC, BinaryWriter writer) => writer.RIFF(fourCC, writable.Write);
	/// <summary>
	/// Reads a RIFF chunk header and provides a reader limited to the chunk's content
	/// </summary>
	/// <param name="reader">The source reader</param>
	/// <param name="readContent">Delegate that receives a length-limited reader for the chunk content</param>
	/// <returns>The FourCC identifier of the chunk that was read</returns>
	public static string RIFF(this BinaryReader reader, Action<BinaryReader, string> readContent)
	{
		string fourCC = Encoding.ASCII.GetString(reader.ReadBytes(4));
		uint length = reader.ReadUInt32();
		using MemoryStream limitedStream = new(reader.ReadBytes((int)length));
		using BinaryReader limitedReader = new(
			input: limitedStream,
			encoding: Encoding.UTF8,
			leaveOpen: true);
		readContent(limitedReader, fourCC);
		return fourCC;
	}
	public static bool TryRIFF(this BinaryReader reader, Func<BinaryReader, string, bool> readContent)
	{
		long startPosition = reader.BaseStream.Position;
		string fourCC = Encoding.ASCII.GetString(reader.ReadBytes(4));
		uint length = reader.ReadUInt32();
		using MemoryStream limitedStream = new(reader.ReadBytes((int)length));
		using BinaryReader limitedReader = new(
			input: limitedStream,
			encoding: Encoding.UTF8,
			leaveOpen: true);
		if (readContent(limitedReader, fourCC))
			return true;
		reader.BaseStream.Position = startPosition;
		return false;
	}
	#endregion IBinaryWritable
}
