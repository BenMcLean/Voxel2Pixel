using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using Voxel2Pixel.Interfaces;

namespace Voxel2Pixel.Model.FileFormats
{
	/// <summary>
	/// Vengi file format spec: https://vengi-voxel.github.io/vengi/FormatSpec/
	/// Vengi file format code: https://github.com/vengi-voxel/vengi/blob/master/src/modules/voxelformat/private/vengi/VENGIFormat.cpp
	/// </summary>
	public static class VengiFile
	{
		public static Node Read(Stream stream)
		{
			using BinaryReader reader = new(input: stream, encoding: System.Text.Encoding.UTF8, leaveOpen: false);
			if (!FourCC(reader).Equals("VENG"))
				throw new InvalidDataException("Vengi format must start with \"VENG\"");
			reader.BaseStream.Position += 2;//zlib header (0x78, 0xDA)
			stream = new DeflateStream(stream: stream, mode: CompressionMode.Decompress, leaveOpen: false);
			using BinaryReader deflatedReader = new(input: stream, encoding: System.Text.Encoding.UTF8, leaveOpen: false);
			if (deflatedReader.ReadUInt32() != 3)
				throw new InvalidDataException("Unexpected version!");
			if (!FourCC(deflatedReader).Equals("NODE"))
				throw new InvalidDataException("Did not find root node!");
			return Node.Read(deflatedReader);
		}
		#region Utilities
		public static uint FourCC(string four) => BinaryPrimitives.ReadUInt32BigEndian(System.Text.Encoding.UTF8.GetBytes(four[..4]));
		public static string FourCC(uint @uint) => System.Text.Encoding.UTF8.GetString(BitConverter.GetBytes(@uint));
		public static string FourCC(BinaryReader reader) => System.Text.Encoding.UTF8.GetString(reader.ReadBytes(4));
		public static string ReadPascalString(BinaryReader reader) => System.Text.Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadUInt16()));
		public static void WritePascalString(BinaryWriter writer, string s)
		{
			writer.Write((ushort)s.Length);
			writer.Write(System.Text.Encoding.UTF8.GetBytes(s));
		}
		#endregion Utilities
		#region Chunks
		public readonly record struct Node(
			Header Header,
			Prop? Properties,
			Data? Data,
			string PaletteIdentifier,
			Palc? Palette,
			Anim? Animation,
			Node[] Children) : IBinaryWritable
		{
			public static Node Read(Stream stream) => Read(new BinaryReader(input: stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true));
			public static Node Read(BinaryReader reader)
			{
				Header header = Header.Read(reader);
				Prop? properties = null;
				Data? data = null;
				string paletteIdentifier = null;
				Palc? palette = null;
				Anim? animation = null;
				List<Node> children = [];
				while (FourCC(reader) is string fourCC && !fourCC.Equals("ENDN"))
					switch (fourCC)
					{
						case "PROP":
							properties = Prop.Read(reader);
							break;
						case "DATA":
							data = VengiFile.Data.Read(reader);
							break;
						case "PALI":
							paletteIdentifier = ReadPascalString(reader);
							break;
						case "PALC":
							palette = Palc.Read(reader);
							break;
						case "ANIM":
							animation = Anim.Read(reader);
							break;
						case "NODE":
							children.Add(Read(reader));
							break;
						default: throw new InvalidDataException("FourCC was \"" + fourCC + "\".");
					}
				return new(
					Header: header,
					Properties: properties,
					Data: data,
					PaletteIdentifier: paletteIdentifier,
					Palette: palette,
					Animation: animation,
					Children: [.. children]);
			}
			#region IBinaryWritable
			public void Write(Stream stream) => Write(new BinaryWriter(output: stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true));
			public void Write(BinaryWriter writer) => throw new NotImplementedException();//TODO
			#endregion IBinaryWritable
		}
		public readonly record struct Header(
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
			public static Header Read(Stream stream) => Read(new BinaryReader(input: stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true));
			public static Header Read(BinaryReader reader) => new(
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
			#region IBinaryWritable
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
			#endregion IBinaryWritable
		}
		public readonly record struct Prop(
			KeyValuePair<string, string>[] Properties) : IBinaryWritable
		{
			public static Prop Read(Stream stream) => Read(new BinaryReader(input: stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true));
			public static Prop Read(BinaryReader reader)
			{
				KeyValuePair<string, string>[] properties = new KeyValuePair<string, string>[reader.ReadUInt32()];
				for (uint i = 0; i < properties.Length; i++)
					properties[i] = new(ReadPascalString(reader), ReadPascalString(reader));
				return new(properties);
			}
			#region IBinaryWritable
			public void Write(Stream stream) => Write(new BinaryWriter(output: stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true));
			public void Write(BinaryWriter writer)
			{
				writer.Write((uint)Properties.Length);
				foreach (KeyValuePair<string, string> property in Properties)
				{
					WritePascalString(writer, property.Key);
					WritePascalString(writer, property.Value);
				}
			}
			#endregion IBinaryWritable
		}
		public readonly record struct Palc(
			uint[] Colors,
			uint[] EmitColors,
			byte[] Indices,
			Material[] Materials) : IBinaryWritable
		{
			public static Palc Read(Stream stream) => Read(new BinaryReader(input: stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true));
			public static Palc Read(BinaryReader reader)
			{
				uint[] colors = new uint[reader.ReadUInt32()];
				for (int i = 0; i < colors.Length; i++)
					colors[i] = reader.ReadUInt32();
				uint[] emitColors = new uint[colors.Length];
				for (int i = 0; i < colors.Length; i++)
					emitColors[i] = reader.ReadUInt32();
				byte[] indices = reader.ReadBytes(colors.Length);
				Material[] materials = new Material[reader.ReadUInt32()];
				for (int i = 0; i < materials.Length; i++)
					materials[i] = Material.Read(reader);
				return new(
					Colors: colors,
					EmitColors: emitColors,
					Indices: indices,
					Materials: materials);
			}
			#region IBinaryWritable
			public void Write(Stream stream) => Write(new BinaryWriter(output: stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true));
			public void Write(BinaryWriter writer)
			{
				writer.Write((uint)Colors.Length);
				foreach (uint color in Colors)
					writer.Write(color);
				foreach (uint emitColor in EmitColors)
					writer.Write(emitColor);
				writer.Write(Indices);
				foreach (Material material in Materials)
					material.Write(writer);
			}
			#endregion IBinaryWritable
			public uint[] Palette => [0u, .. Colors.Take(255).Select(BinaryPrimitives.ReverseEndianness)];
		}
		public readonly record struct Material(
			uint Type,
			KeyValuePair<string, float>[] Properties) : IBinaryWritable
		{
			public static Material Read(Stream stream) => Read(new BinaryReader(input: stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true));
			public static Material Read(BinaryReader reader)
			{
				uint type = reader.ReadUInt32();
				KeyValuePair<string, float>[] properties = new KeyValuePair<string, float>[reader.ReadByte()];
				for (byte i = 0; i < properties.Length; i++)
					properties[i] = new(ReadPascalString(reader), reader.ReadSingle());
				return new Material(
					Type: type,
					Properties: properties);
			}
			#region IBinaryWritable
			public void Write(Stream stream) => Write(new BinaryWriter(output: stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true));
			public void Write(BinaryWriter writer)
			{
				writer.Write(Type);
				writer.Write((byte)Properties.Length);
				foreach (KeyValuePair<string, float> property in Properties)
				{
					WritePascalString(writer, property.Key);
					writer.Write(property.Value);
				}
			}
			#endregion IBinaryWritable
		}
		public readonly record struct Data(
			int LowerX,
			int LowerY,
			int LowerZ,
			int UpperX,
			int UpperY,
			int UpperZ,
			Voxel[] Voxels) : IBinaryWritable
		{
			public static Data Read(Stream stream) => Read(new BinaryReader(input: stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true));
			public static Data Read(BinaryReader reader)
			{
				int lowerX = reader.ReadInt32(),
					lowerY = reader.ReadInt32(),
					lowerZ = reader.ReadInt32(),
					upperX = reader.ReadInt32(),
					upperY = reader.ReadInt32(),
					upperZ = reader.ReadInt32();
				List<Voxel> voxels = [];
				for (int x = lowerX; x <= upperX; x++)
					for (int y = lowerY; y <= upperY; y++)
						for (int z = lowerZ; z <= upperZ; z++)
							voxels.Add(Voxel.Read(reader));
				return new(
					LowerX: lowerX,
					LowerY: lowerY,
					LowerZ: lowerZ,
					UpperX: upperX,
					UpperY: upperY,
					UpperZ: upperZ,
					Voxels: [.. voxels]);
			}
			#region IBinaryWritable
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
				for (int x = LowerX; x <= UpperX; x++)
					for (int y = LowerY; y <= UpperY; y++)
						for (int z = LowerZ; z <= UpperZ; z++)
							Voxels[index++].Write(writer);
			}
			#endregion IBinaryWritable
			/// <summary>
			/// In 3D space for voxels, I'm following the MagicaVoxel convention, which is Z+up, right-handed, so X+ means right/east, Y+ means forwards/north and Z+ means up.
			/// Vengu is Y-up, right-handed, so X+ means right/east, Y+ means up and Z+ means forwards/north. 
			/// </summary>
			public IEnumerable<Voxel2Pixel.Model.Voxel> GetVoxels()
			{
				int index = 0;
				for (int x = LowerX; x <= UpperX; x++)
					for (int y = LowerY; y <= UpperY; y++)
						for (int z = LowerZ; z <= UpperZ; z++)
							if (Voxels[index++] is Voxel voxel
								&& !voxel.Air
								&& voxel.Color != 0
								&& voxel.Color != 255)
								yield return new Voxel2Pixel.Model.Voxel(
									X: (ushort)(UpperX - x - LowerX),
									Y: (ushort)(z - LowerZ),
									Z: (ushort)(y - LowerY),
									Index: (byte)(voxel.Color + 1));
			}
			public Point3D Size => new(
				X: UpperX - LowerX + 1,
				Y: UpperZ - LowerZ + 1,
				Z: UpperY - LowerY + 1);
		}
		public readonly record struct Voxel(
			bool Air,
			byte Color) : IBinaryWritable
		{
			public static Voxel Read(Stream stream) => Read(new BinaryReader(input: stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true));
			public static Voxel Read(BinaryReader reader) => reader.ReadBoolean() ?
				new Voxel(Air: true, Color: 0)
				: new Voxel(Air: false, Color: reader.ReadByte());
			#region IBinaryWritable
			public void Write(Stream stream) => Write(new BinaryWriter(output: stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true));
			public void Write(BinaryWriter writer)
			{
				writer.Write(Air);
				if (!Air)
					writer.Write(Color);
			}
			#endregion IBinaryWritable
		}
		public readonly record struct Anim(
			string Name,
			Keyf[] Keyframes) : IBinaryWritable
		{
			public static Anim Read(Stream stream) => Read(new BinaryReader(input: stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true));
			public static Anim Read(BinaryReader reader)
			{
				string name = ReadPascalString(reader);
				List<Keyf> keyframes = [];
				while (FourCC(reader).Equals("KEYF"))
					keyframes.Add(Keyf.Read(reader));
				return new(
					Name: name,
					Keyframes: [.. keyframes]);
			}
			#region IBinaryWritable
			public void Write(Stream stream) => Write(new BinaryWriter(output: stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true));
			public void Write(BinaryWriter writer)
			{
				writer.Write(Name);
				foreach (Keyf keyframe in Keyframes)
				{
					writer.Write(FourCC("KEYF"));
					keyframe.Write(writer);
				}
				writer.Write(FourCC("ENDA"));
			}
			#endregion IBinaryWritable
		}
		public readonly record struct Keyf(
			uint Index,
			bool Rotation,
			string InterpolationType,
			Matrix4x4 LocalMatrix) : IBinaryWritable
		{
			public static Keyf Read(Stream stream) => Read(new BinaryReader(input: stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true));
			public static Keyf Read(BinaryReader reader) => new(
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
			#region IBinaryWritable
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
			#endregion IBinaryWritable
		}
		#endregion Chunks
	}
}
