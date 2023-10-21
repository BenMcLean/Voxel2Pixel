using System.Collections;
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
				Main = new MainChunk(reader);
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
			Main.Write(writer);
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
			public byte[] Data;
			public uint ChildrenLength;
			public Chunk(Stream stream) : this(new BinaryReader(stream)) { }
			public Chunk(BinaryReader reader) : this(tagName: ReadString(reader), reader: reader) { }
			public Chunk(string tagName, BinaryReader reader)
			{
				//A chunk consists of 5 parts.
				//The chunk tag name, a 4 byte human readable character sequence.
				TagName = tagName;
				//An integer indicating the number of bytes in the chunk data.
				Data = new byte[reader.ReadUInt32()];
				//An integer indicating the number of bytes in the children chunks.
				ChildrenLength = reader.ReadUInt32();
				//The chunk data.
				Data = reader.ReadBytes(Data.Length);
			}
			public virtual void Write(BinaryWriter writer)
			{
				//A chunk consists of 5 parts.
				//The chunk tag name, a 4 byte human readable character sequence.
				WriteString(writer, TagName);
				//An integer indicating the number of bytes in the chunk data.
				writer.Write((uint)Data.Length);
				//An integer indicating the number of bytes in the children chunks.
				writer.Write(ChildrenLength);
				//The chunk data.
				writer.Write(Data);
			}
		}
		public class MainChunk : Chunk
		{
			public byte[] Children;
			public MainChunk(BinaryReader reader) : base(reader) => Children = reader.ReadBytes((int)ChildrenLength);
			public override void Write(BinaryWriter writer)
			{
				base.Write(writer);
				writer.Write(Children);
			}
			public IEnumerable Chunks()
			{
				using (MemoryStream ms = new MemoryStream(Children))
				using (BinaryReader reader = new BinaryReader(ms))
					while (reader.BaseStream.CanRead)
						yield return new Chunk(reader);
			}
		}
		public MainChunk Main;
		#endregion Chunk
	}
}
