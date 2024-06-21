using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Numerics;
using Voxel2Pixel.Interfaces;

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
	/// Spec: https://vengi-voxel.github.io/vengi/FormatSpec/
	/// </summary>
	public class VengiModel : IBinaryWritable
	{
		public VengiModel(Stream stream)
		{
			BinaryReader reader = new(input: stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true);
			if (BinaryPrimitives.ReadUInt32BigEndian(reader.ReadBytes(4)) != FourCC("VENG"))
				throw new InvalidDataException("Vengi format must start with \"VENG\"");
			reader.BaseStream.Position += 2;//zlib header (0x78, 0xDA)
			stream = new DeflateStream(stream: stream, mode: CompressionMode.Decompress, leaveOpen: false);
			//stream.CopyTo(new FileStream("kingkong-deflated.bin", FileMode.Create)); return;
			reader = new BinaryReader(input: stream, encoding: System.Text.Encoding.UTF8, leaveOpen: false);
			if (reader.ReadUInt32() != 3)
				throw new InvalidDataException("Unexpected version!");
			if (BinaryPrimitives.ReadUInt32BigEndian(reader.ReadBytes(4)) != FourCC("NODE"))
				throw new InvalidDataException("expected NODE");
			Header = HeaderChunk.Read(stream);
			//stream.CopyTo(new FileStream("kingkong.bin", FileMode.Create)); return;
			if (BinaryPrimitives.ReadUInt32BigEndian(reader.ReadBytes(4)) != FourCC("PALC"))
				throw new InvalidDataException("expected PALC");
			Palc = PalcChunk.Read(stream);
		}
		public void Write(Stream stream) => Write(new BinaryWriter(output: stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true));
		public void Write(BinaryWriter writer)
		{
			writer.Write(FourCC("VENG"));
		}
		#region Utilities
		public static uint FourCC(string four) => BinaryPrimitives.ReadUInt32BigEndian(System.Text.Encoding.UTF8.GetBytes(four[..4]));
		public static string ReadPascalString(BinaryReader reader) => System.Text.Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadUInt16()));
		public static void WritePascalString(BinaryWriter writer, string s)
		{
			writer.Write((ushort)s.Length);
			writer.Write(System.Text.Encoding.UTF8.GetBytes(s));
		}
		#endregion Utilities
		#region Chunks
		public readonly record struct HeaderChunk(
			string Name,
			string Type,
			int ID,
			int ReferenceID,
			bool Visible,
			bool Locked,
			uint Color,
			float PivotX,
			float PivotY,
			float PivotZ) : IBinaryWritable
		{
			public static HeaderChunk Read(Stream stream) => Read(new BinaryReader(input: stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true));
			public static HeaderChunk Read(BinaryReader reader) => new(
				Name: ReadPascalString(reader),
				Type: ReadPascalString(reader),
				ID: reader.ReadInt32(),
				ReferenceID: reader.ReadInt32(),
				Visible: reader.ReadBoolean(),
				Locked: reader.ReadBoolean(),
				Color: reader.ReadUInt32(),
				PivotX: reader.ReadSingle(),
				PivotY: reader.ReadSingle(),
				PivotZ: reader.ReadSingle());
			public void Write(Stream stream) => Write(new BinaryWriter(output: stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true));
			public void Write(BinaryWriter writer)
			{
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
		public HeaderChunk Header;
		public readonly record struct PalcChunk(
			uint[] Colors,
			uint[] EmitColors,
			byte[] Indices,
			uint Type,
			KeyValuePair<string, float>[] Materials) : IBinaryWritable
		{
			public static PalcChunk Read(Stream stream) => Read(new BinaryReader(input: stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true));
			public static PalcChunk Read(BinaryReader reader)
			{
				uint[] colors = new uint[reader.ReadUInt32()];
				for (uint i = 0; i < colors.Length; i++)
					colors[i] = reader.ReadUInt32();
				uint[] emitColors = new uint[colors.Length];
				for (uint i = 0; i < colors.Length; i++)
					emitColors[i] = reader.ReadUInt32();
				byte[] indices = reader.ReadBytes(colors.Length);
				uint type = reader.ReadUInt32();
				KeyValuePair<string, float>[] materials = new KeyValuePair<string, float>[reader.ReadByte()];
				for (ushort i = 0; i < materials.Length; i++)
					materials[i] = new(ReadPascalString(reader), reader.ReadSingle());
				return new(
					Colors: colors,
					EmitColors: emitColors,
					Indices: indices,
					Type: type,
					Materials: materials);
			}
			public void Write(Stream stream) => Write(new BinaryWriter(output: stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true));
			public void Write(BinaryWriter writer)
			{
				writer.Write((uint)Colors.Length);
				foreach (uint color in Colors)
					writer.Write(color);
				foreach (uint emitColor in EmitColors)
					writer.Write(emitColor);
				writer.Write(Indices);
				writer.Write(Type);
				writer.Write((byte)Materials.Length);
				foreach (KeyValuePair<string, float> material in Materials)
				{
					WritePascalString(writer, material.Key);
					writer.Write(material.Value);
				}
			}
		}
		public PalcChunk Palc;
		public string Pali;
		public readonly record struct DataChunk(
			int LowerX,
			int LowerY,
			int LowerZ,
			int UpperX,
			int UpperY,
			int UpperZ,
			VengiVoxel[] Voxels) : IBinaryWritable
		{
			public static DataChunk Read(Stream stream) => Read(new BinaryReader(input: stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true));
			public static DataChunk Read(BinaryReader reader)
			{
				int lowerX = reader.ReadInt32(),
					lowerY = reader.ReadInt32(),
					lowerZ = reader.ReadInt32(),
					upperX = reader.ReadInt32(),
					upperY = reader.ReadInt32(),
					upperZ = reader.ReadInt32();
				List<VengiVoxel> voxels = [];
				for (int z = lowerZ; z < upperZ; z++)
					for (int y = lowerY; y < upperY; y++)
						for (int x = lowerX; x < upperX; x++)
							voxels.Add(VengiVoxel.Read(reader));
				return new(
					LowerX: lowerX,
					LowerY: lowerY,
					LowerZ: lowerZ,
					UpperX: upperX,
					UpperY: upperY,
					UpperZ: upperZ,
					Voxels: [.. voxels]);
			}
			public void Write(Stream stream) => Write(new BinaryWriter(output: stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true));
			public void Write(BinaryWriter writer)
			{
				writer.Write(LowerX);
				writer.Write(LowerY);
				writer.Write(LowerZ);
				writer.Write(UpperX);
				writer.Write(UpperY);
				writer.Write(UpperZ);
				int index = 0;
				for (int z = LowerZ; z < UpperZ; z++)
					for (int y = LowerY; y < UpperY; y++)
						for (int x = LowerX; x < UpperX; x++)
							Voxels[index++].Write(writer);
			}
		}
		public readonly record struct VengiVoxel(
			bool Air,
			byte Color) : IBinaryWritable
		{
			public static VengiVoxel Read(Stream stream) => Read(new BinaryReader(input: stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true));
			public static VengiVoxel Read(BinaryReader reader) => reader.ReadBoolean() ?
				new VengiVoxel(Air: true, Color: 0)
				: new VengiVoxel(Air: false, Color: reader.ReadByte());
			public void Write(Stream stream) => Write(new BinaryWriter(output: stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true));
			public void Write(BinaryWriter writer)
			{
				writer.Write(Air);
				if (!Air)
					writer.Write(Color);
			}
		}
		public readonly record struct AnimChunk(
			string Name,
			KeyfChunk[] Keyframes) : IBinaryWritable
		{
			public static AnimChunk Read(Stream stream) => Read(new BinaryReader(input: stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true));
			public static AnimChunk Read(BinaryReader reader)
			{
				string name = ReadPascalString(reader);
				List<KeyfChunk> keyframes = [];
				while (reader.ReadUInt32() == FourCC("KEYF"))
					keyframes.Add(KeyfChunk.Read(reader));
				return new(
					Name: name,
					Keyframes: [.. keyframes]);
			}
			public void Write(Stream stream) => Write(new BinaryWriter(output: stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true));
			public void Write(BinaryWriter writer)
			{
				writer.Write(Name);
				foreach (KeyfChunk keyframe in Keyframes)
				{
					writer.Write(FourCC("KEYF"));
					keyframe.Write(writer);
				}
				writer.Write(FourCC("ENDA"));
			}
		}
		public readonly record struct KeyfChunk(
			uint Index,
			bool Rotation,
			string InterpolationType,
			Matrix4x4 LocalMatrix) : IBinaryWritable
		{
			public static KeyfChunk Read(Stream stream) => Read(new BinaryReader(input: stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true));
			public static KeyfChunk Read(BinaryReader reader) => new(
				Index: reader.ReadUInt32(),
				Rotation: reader.ReadBoolean(),
				InterpolationType: ReadPascalString(reader),
				LocalMatrix: new Matrix4x4(
					m11: reader.ReadSingle(),
					m12: reader.ReadSingle(),
					m13: reader.ReadSingle(),
					m14: reader.ReadSingle(),
					m21: reader.ReadSingle(),
					m22: reader.ReadSingle(),
					m23: reader.ReadSingle(),
					m24: reader.ReadSingle(),
					m31: reader.ReadSingle(),
					m32: reader.ReadSingle(),
					m33: reader.ReadSingle(),
					m34: reader.ReadSingle(),
					m41: reader.ReadSingle(),
					m42: reader.ReadSingle(),
					m43: reader.ReadSingle(),
					m44: reader.ReadSingle()));
			public void Write(Stream stream) => Write(new BinaryWriter(output: stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true));
			public void Write(BinaryWriter writer)
			{
				writer.Write(Index);
				writer.Write(Rotation);
				WritePascalString(writer, InterpolationType);
				writer.Write(LocalMatrix.M11);
				writer.Write(LocalMatrix.M12);
				writer.Write(LocalMatrix.M13);
				writer.Write(LocalMatrix.M14);
				writer.Write(LocalMatrix.M21);
				writer.Write(LocalMatrix.M22);
				writer.Write(LocalMatrix.M23);
				writer.Write(LocalMatrix.M24);
				writer.Write(LocalMatrix.M31);
				writer.Write(LocalMatrix.M32);
				writer.Write(LocalMatrix.M33);
				writer.Write(LocalMatrix.M34);
				writer.Write(LocalMatrix.M41);
				writer.Write(LocalMatrix.M42);
				writer.Write(LocalMatrix.M43);
				writer.Write(LocalMatrix.M44);
			}
		}
		#endregion Chunks
	}
}
