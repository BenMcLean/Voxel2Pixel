using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VoxModel
{
	/// <summary>
	/// Following https://paulbourke.net/dataformats/vox/
	/// "vox files are a voxel based file description used to represent assets for 3D games and virtual environments. The layout is similar to tiff files, that is, a series of tags that contain data but also information about their length so tags that are not of interest can be skipped. It also means that particular software products can create their own tags for internal use without breaking other software that reads vox files."
	/// The vox format appears to use Little Endian byte order.
	/// </summary>
	public struct VoxFile
	{
		public VoxFile(string path) : this(new FileStream(path, FileMode.Open)) { }
		public VoxFile(Stream stream)
		{
			using (BinaryReader reader = new BinaryReader(stream))
			{
				VersionNumber = ReadVersionNumber(reader);
				Chunks = GetChunks(reader).ToList();
			}
		}
		public void Write(string path)
		{
			using (FileStream stream = new FileStream(path, FileMode.Create))
			using (BinaryWriter writer = new BinaryWriter(stream))
			{
				Write(writer);
			}
		}
		public void Write(BinaryWriter writer)
		{
			WriteVersionNumber(writer);
			foreach (Chunk chunk in Chunks)
				chunk.Write(writer);
		}
		public static string ReadString(BinaryReader reader, int length = 4) => new string(reader.ReadChars(length));
		public static void WriteString(BinaryWriter writer, string @string) => writer.Write(@string.ToArray());
		#region Header
		public uint VersionNumber;
		/// <summary>
		/// "The header of a vox file consists of a 4 byte magic string "VOX ". This is followed by a int (4 bytes) version number, typically 150 or 200."
		/// </summary>
		public static uint ReadVersionNumber(BinaryReader reader)
		{
			if (!"VOX ".Equals(ReadString(reader)))
				throw new InvalidDataException("\"" + reader + "\" has an invalid signature code!");
			return reader.ReadUInt32();
		}
		public void WriteVersionNumber(BinaryWriter writer) => WriteVersionNumber(writer, VersionNumber);
		public static void WriteVersionNumber(BinaryWriter writer, uint versionNumber)
		{
			WriteString(writer, "VOX ");
			writer.Write(versionNumber);
		}
		#endregion Header
		#region Chunk
		/// <summary>
		/// A chunk consists of 5 parts.
		/// The chunk tag name, a 4 byte human readable character sequence.
		/// An integer indicating the number of bytes in the chunk data.
		/// An integer indicating the number of bytes in the children chunks.
		/// The chunk data.
		/// The children chunks.
		/// </summary>
		public class Chunk
		{
			public string TagName
			{
				get => tagName;
				set => tagName = value.ExactPadRight(4);
			}
			private string tagName;
			public uint DataLength;
			public uint ChildrenLength;
			public Chunk() { }
			public Chunk(BinaryReader reader) : this(tagName: ReadString(reader), reader: reader) { }
			public Chunk(string tagName, BinaryReader reader)
			{
				TagName = tagName;
				DataLength = reader.ReadUInt32();
				ChildrenLength = reader.ReadUInt32();
			}
			public virtual void Write(BinaryWriter writer)
			{
				WriteString(writer, TagName);
				writer.Write(DataLength);
				writer.Write(ChildrenLength);
			}
		}
		public class UnknownChunk : Chunk
		{
			public byte[] Data;
			public UnknownChunk() { }
			public UnknownChunk(BinaryReader reader) : this(tagName: ReadString(reader), reader: reader) { }
			public UnknownChunk(string tagName, BinaryReader reader) : base(tagName, reader) => Data = reader.ReadBytes((int)DataLength);
			public override void Write(BinaryWriter writer)
			{
				base.Write(writer);
				writer.Write(Data);
			}
		}
		public List<Chunk> Chunks;
		public static IEnumerable<Chunk> GetChunks(byte[] bytes) => GetChunks(new BinaryReader(new MemoryStream(bytes)));
		public static IEnumerable<Chunk> GetChunks(BinaryReader reader)
		{
			while (reader.BaseStream.Position < reader.BaseStream.Length)
			{
				string name = ReadString(reader);
				switch (name)
				{
					case "MAIN":
						yield return new Chunk(name, reader);
						break;
					case "SIZE":
						yield return new SizeChunk(name, reader);
						break;
					default:
						yield return new UnknownChunk(name, reader);
						break;
				}
			}
		}
		#endregion Chunk
		#region SizeChunk
		public class SizeChunk : Chunk
		{
			public int SizeX, SizeY, SizeZ;
			public SizeChunk() { }
			public SizeChunk(BinaryReader reader) : this(tagName: ReadString(reader), reader: reader) { }
			public SizeChunk(string tagName, BinaryReader reader) : base(tagName, reader)
			{
				SizeX = reader.ReadInt32();
				SizeY = reader.ReadInt32();
				SizeZ = reader.ReadInt32();
			}
			public override void Write(BinaryWriter writer)
			{
				TagName = "SIZE";
				DataLength = 12;
				base.Write(writer);
				writer.Write(SizeX);
				writer.Write(SizeY);
				writer.Write(SizeZ);
			}
		}
		#endregion SizeChunk
	}
}
