using System.IO;

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
			}
		}
		public void Write(string path)
		{
			using (FileStream stream = new FileStream(path, FileMode.Create))
			{
				WriteVersionNumber(stream);
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
				if (reader.ReadUInt32() != 0x20584F56)//"VOX "
					throw new InvalidDataException("\"" + stream + "\" has an invalid signature code!");
				return reader.ReadUInt32();
			}
		}
		public void WriteVersionNumber(Stream stream) => WriteVersionNumber(stream, VersionNumber);
		public static void WriteVersionNumber(Stream stream, uint versionNumber)
		{
			using (BinaryWriter writer = new BinaryWriter(stream))
			{
				writer.Write(0x20584F56);//"VOX "
				writer.Write(versionNumber);
			}
		}
		#endregion Header
	}
}
