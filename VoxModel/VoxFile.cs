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
		public VoxFile(string path)
		{
			using (FileStream stream = new FileStream(path, FileMode.Open))
			{
				VersionNumber = ReadVersionNumber(stream);
				Main = new MainChunk(stream);
			}
		}
		public void Write(string path)
		{
			using (FileStream stream = new FileStream(path, FileMode.Create))
			{
				WriteVersionNumber(stream);
				Main.Write(stream);
			}
		}
		#region Header
		public uint VersionNumber;
		/// <summary>
		/// "The header of a vox file consists of a 4 byte magic string "VOX ". This is followed by a int (4 bytes) version number, typically 150 or 200."
		/// </summary>
		public static uint ReadVersionNumber(Stream stream)
		{
			using (BinaryReader reader = new BinaryReader(stream))
			{
				if ("VOX ".Equals(new string(reader.ReadChars(4).Reverse().ToArray())))
					throw new InvalidDataException("\"" + stream + "\" has an invalid signature code!");
				return reader.ReadUInt32();
			}
		}
		public void WriteVersionNumber(Stream stream) => WriteVersionNumber(stream, VersionNumber);
		public static void WriteVersionNumber(Stream stream, uint versionNumber)
		{
			using (BinaryWriter writer = new BinaryWriter(stream))
			{
				writer.Write("VOX ".ToArray());
				writer.Write(versionNumber);
			}
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
			public Chunk(Stream stream)
			{
				using (BinaryReader reader = new BinaryReader(stream))
				{
					//A chunk consists of 5 parts.
					//The chunk tag name, a 4 byte human readable character sequence.
					tagName = new string(reader.ReadChars(4).Reverse().ToArray());
					//An integer indicating the number of bytes in the chunk data.
					Data = new byte[reader.ReadUInt32()];
					//An integer indicating the number of bytes in the children chunks.
					ChildrenLength = reader.ReadUInt32();
					//The chunk data.
					Data = reader.ReadBytes(Data.Length);
				}
			}
			public virtual void Write(Stream stream)
			{
				using (BinaryWriter writer = new BinaryWriter(stream))
				{
					//A chunk consists of 5 parts.
					//The chunk tag name, a 4 byte human readable character sequence.
					writer.Write(TagName.ToArray());
					//An integer indicating the number of bytes in the chunk data.
					writer.Write((uint)Data.Length);
					//An integer indicating the number of bytes in the children chunks.
					writer.Write(ChildrenLength);
					//The chunk data.
					writer.Write(Data);
				}
			}
		}
		public class MainChunk : Chunk
		{
			public byte[] Children;
			public MainChunk(Stream stream) : base(stream)
			{
				using (BinaryReader reader = new BinaryReader(stream))
				{
					Children = reader.ReadBytes((int)ChildrenLength);
				}
			}
			public override void Write(Stream stream)
			{
				base.Write(stream);
				using (BinaryWriter writer = new BinaryWriter(stream))
				{
					writer.Write(Children);
				}
			}
		}
		MainChunk Main;
		#endregion Chunk
	}
}
