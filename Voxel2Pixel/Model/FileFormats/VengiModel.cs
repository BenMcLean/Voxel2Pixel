using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Xml;

namespace Voxel2Pixel.Model.FileFormats
{
	/*
	VENGI Format Specification
	File Overview

	The VENGI file format is used to save and load scene graphs, nodes, animations, and related data for voxel-based models. The format supports features such as node properties, animations, palette colors, and hierarchical structures.
	File Structure
	Magic Header

		Magic Number: 0x56454E47 ('VENG')

	-- from here everything is zipped --

	Version

		Version Number: uint32 (latest version is 3)

	Node Chunks

	Each node in the scene graph is saved as a chunk. Chunks are identified by specific magic numbers:

		Node Chunk: 0x4E4F4445 ('NODE')
		Property Chunk: 0x50524F50 ('PROP')
		Data Chunk: 0x44415441 ('DATA')
		Palette Colors Chunk: 0x50414C43 ('PALC')
		Palette Identifier Chunk: 0x50414C49 ('PALI')
		Animation Chunk: 0x414E494D ('ANIM')
		Key Frame Chunk: 0x4B455946 ('KEYF')
		End Node Chunk: 0x454E444E ('ENDN')
		End Animation Chunk: 0x454E4441 ('ENDA')

	Node Structure
	Node Header

		Name: Pascal String (UInt16LE length prefix)
		Type: Pascal String (UInt16LE length prefix)
		ID: int32
		Reference ID: int32 (for reference nodes)
		Visible: bool
		Locked: bool
		Color: uint32 (RGBA)
		Pivot: float[3] (x, y, z)

	Node Properties (Chunk: 'PROP')

		Property Count: uint32
		Properties: List of key-value pairs (Pascal String, UInt16LE length prefix for both key and value)

	Node Data (Chunk: 'DATA')

		Region Lower Bound: int32[3] (x, y, z)
		Region Upper Bound: int32[3] (x, y, z)
		Volume Data: List of voxels
			Is Air: bool
			Color: uint8 (if not air)

	Node Palette Colors (Chunk: 'PALC')

		Color Count: uint32
		Colors: uint32[Color Count] (RGBA)
		Emit Colors: uint32[Color Count]
		Indices: uint8[Color Count]
		Material Count: uint32
		Materials: List of materials
			Type: uint32
			Property Count: uint8
			Properties: List of key-value pairs (Pascal String for name, float for value)

	Node Palette Identifier (Chunk: 'PALI')

		Palette Name: Pascal String (UInt16LE length prefix)

	Node Animations (Chunk: 'ANIM')

		Animation Name: Pascal String (UInt16LE length prefix)
		Key Frames: List of key frames

	Node Key Frames (Chunk: 'KEYF')

		Frame Index: uint32
		Long Rotation: bool
		Interpolation Type: Pascal String (UInt16LE length prefix)
		Transform Matrix: float[16] (4x4 matrix)
		Pivot: float[3] (x, y, z) [only in version <= 2]

	Node End (Chunk: 'ENDN')

	Indicates the end of a node chunk.
	Top-Level Node

	The scene graph starts with a root node which can contain multiple child nodes. Child nodes are recursively saved and loaded.
	Animation End (Chunk: 'ENDA')

	Indicates the end of an animation chunk.
	Error Handling

		The file format uses wrap macros (wrap, wrapBool) to handle errors during read/write operations. If an error occurs, a log message is recorded, and the operation returns false.

	Example Workflow
	Saving

		Write Magic Header: Write 'VENG'.
		Write Version: Write version number (3).
		Save Nodes: Recursively save nodes starting from the root node, including properties, data, palette, animations, and children.
		Write End Node: Write 'ENDN' to mark the end of each node.

	Loading

		Read Magic Header: Verify 'VENG'.
		Read Version: Verify version number (must be ≤ 3).
		Load Nodes: Recursively load nodes, properties, data, palette, animations, and children.
		Handle References: Resolve references after loading all nodes.
		Update Transforms: Update node transforms based on the hierarchy.

	This format allows for efficient and organized saving/loading of complex voxel-based scenes with properties, animations, and hierarchical structures.
	*/
	/// <summary>
	/// Vengi file format: https://github.com/vengi-voxel/vengi/blob/master/src/modules/voxelformat/private/vengi/VENGIFormat.cpp
	/// </summary>
	public class VengiModel//: IModel
	{
		public enum MagicNumber : uint
		{
			VENG = 0x56454E47u,//Vengi
			NODE = 0x4E4F4445u,//Node
			PROP = 0x50524F50u,//Property
			DATA = 0x44415441u,//Data
			PALC = 0x50414C43u,//Palette Colors
			PALI = 0x50414C49u,//Palette Identifier
			ANIM = 0x414E494Du,//Animation
			KEYF = 0x4B455946u,//Key Frame
			ENDN = 0x454E444Eu,//End Node
			ENDA = 0x454E4441u,//End Animation
		}
		public static string ReadPascalString(BinaryReader reader) => System.Text.Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadUInt16()));
		public static void WritePascalString(BinaryWriter writer, string s)
		{
			writer.Write((ushort)s.Length);
			writer.Write(System.Text.Encoding.UTF8.GetBytes(s));
		}
		public readonly record struct Node(
			string Name,
			string Type,
			int ID,
			int ReferenceID,
			bool Visible,
			bool Locked,
			uint Color,
			float PivotX,
			float PivotY,
			float PivotZ)
		{
			public static Node Read(Stream stream)
			{
				BinaryReader reader = new(stream);
				return new Node(
					Name: ReadPascalString(reader),
					Type: ReadPascalString(reader),
					ID: reader.ReadUInt16(),
					ReferenceID: reader.ReadUInt16(),
					Visible: reader.ReadBoolean(),
					Locked: reader.ReadBoolean(),
					Color: reader.ReadUInt32(),
					PivotX: reader.ReadSingle(),
					PivotY: reader.ReadSingle(),
					PivotZ: reader.ReadSingle());
			}
			public void Write(Stream stream)
			{
				BinaryWriter writer = new(stream);
				WritePascalString(writer, Name);
				WritePascalString(writer, Type);
				writer.Write(ID);
				writer.Write(ReferenceID);
				writer.Write(Visible);
				writer.Write(Locked);
				writer.Write(Color);
				writer.Write(PivotX);
				writer.Write(PivotY);
				writer.Write(PivotZ);
			}
		}
		public VengiModel(Stream stream)
		{
			BinaryReader reader = new(stream);
			if (BinaryPrimitives.ReadUInt32BigEndian(reader.ReadBytes(4)) != (uint)MagicNumber.VENG)
				throw new InvalidDataException("Vengi format must start with \"VENG\"");
			reader.BaseStream.Position += 2;//zlib header (0x78, 0xDA)
			Stream deflated = new DeflateStream(stream, CompressionMode.Decompress);
			reader = new(deflated);
			uint version = reader.ReadUInt32();
			if (version != 3)
				throw new InvalidDataException("Unexpected version!");
			if (BinaryPrimitives.ReadUInt32BigEndian(reader.ReadBytes(4)) != (uint)MagicNumber.NODE)
				throw new InvalidDataException("Node must start with \"NODE\"");
			Node node = Node.Read(deflated);
			//FileStream o = new("kingkong.bin", FileMode.Create, FileAccess.Write);
			//deflated.CopyTo(o);
			//o.Close();
		}
		public Node N;
		public void Write(Stream stream)
		{
			BinaryWriter writer = new(stream);
			writer.Write((uint)MagicNumber.VENG);
		}
	}
}
