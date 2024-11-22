using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BenVoxel;

public class BenVoxelFile() : IBinaryWritable
{
	[JsonPropertyName("version")]
	public const string Version = "0.1";
	#region Nested classes
	public class Metadata() : IBinaryWritable
	{
		#region Data
		[JsonPropertyName("properties")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public readonly SanitizedKeyDictionary<string> Properties = [];
		[JsonPropertyName("points")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public readonly SanitizedKeyDictionary<Point3D> Points = [];
		[JsonPropertyName("palettes")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public readonly SanitizedKeyDictionary<Color[]> Palettes = [];
		#endregion Data
		#region Metadata
		/// <summary>
		/// Gets and sets palettes without descriptions
		/// </summary>
		public uint[] this[string paletteName]
		{
			get => [.. Palettes[paletteName].Take(256).Select(color => color.Rgba)];
			set => Palettes[paletteName] = [.. Color.Colors(value.Take(256))];
		}
		public Metadata(Stream stream) : this()
		{
			using BinaryReader reader = new(
				input: stream,
				encoding: Encoding.UTF8,
				leaveOpen: true);
			FromReader(reader);
		}
		public Metadata(BinaryReader reader) : this() => FromReader(reader);
		private void FromReader(BinaryReader reader)
		{
			while (reader.BaseStream.Position < reader.BaseStream.Length - 4
				&& reader.TryRIFF((reader, fourCC) =>
				{
					switch (fourCC)
					{
						case "PROP":
							ushort count = reader.ReadUInt16();
							for (ushort i = 0; i < count; i++)
								Properties[ReadKey(reader)] = ReadString(reader);
							return true;
						case "PT3D":
							count = reader.ReadUInt16();
							for (ushort i = 0; i < count; i++)
								Points[ReadKey(reader)] = new Point3D(reader);
							return true;
						case "PALC":
							count = reader.ReadUInt16();
							for (ushort i = 0; i < count; i++)
							{
								string key = ReadKey(reader);
								uint[] colors = [.. Enumerable.Range(0, reader.ReadByte() + 1).Select(i => reader.ReadUInt32())];
								bool hasDescriptions = reader.ReadByte() != 0;
								Palettes[key] = [.. colors.Select(rgba => new Color
								{
									Rgba = rgba,
									Description = hasDescriptions ? ReadString(reader) : null,
								})];
							}
							return true;
						default:
							return false;
					}
				})) { }
		}
		public bool Any() => Properties.Any() || Points.Any() || Palettes.Any();
		#endregion Metadata
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
			if (Properties.Any())
				writer.RIFF("PROP", (writer) =>
				{
					writer.Write((ushort)Properties.Count());
					foreach (KeyValuePair<string, string> property in Properties)
					{
						WriteKey(writer, property.Key);
						WriteString(writer, property.Value);
					}
				});
			if (Points.Any())
				writer.RIFF("PT3D", (writer) =>
				{
					writer.Write((ushort)Points.Count());
					foreach (KeyValuePair<string, Point3D> point in Points)
					{
						WriteKey(writer, point.Key);
						point.Value.Write(writer);
					}
				});
			if (Palettes.Any())
				writer.RIFF("PALC", (writer) =>
				{
					writer.Write((ushort)Palettes.Count());
					foreach (KeyValuePair<string, Color[]> palette in Palettes)
					{
						WriteKey(writer, palette.Key);
						writer.Write((byte)(palette.Value.Length - 1));
						foreach (Color color in palette.Value)
							writer.Write(color.Rgba);
						if (palette.Value.Any(color => !string.IsNullOrWhiteSpace(color.Description)))
						{
							writer.Write((byte)1);
							foreach (Color color in palette.Value)
								WriteString(writer, color.Description ?? "");
						}
						else
							writer.Write((byte)0);
					}
				});
		}
		#endregion IBinaryWritable
	}
	public readonly record struct Color()
	{
		#region Data
		[JsonIgnore]
		public uint Rgba { get; init; } = 0u;
		[JsonPropertyName("rgba")]
		public string RgbaHex
		{
			get => $"#{Rgba:X8}";
			init => Rgba = uint.Parse(value[1..], System.Globalization.NumberStyles.HexNumber);
		}
		[JsonPropertyName("description")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string Description { get; init; } = null;
		#endregion Data
		public static IEnumerable<Color> Colors(IEnumerable<uint> colors) => colors.Select(rgba => new Color { Rgba = rgba, });
	}
	public class Model() : IBinaryWritable
	{
		#region Data
		[JsonPropertyName("metadata")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public Metadata Metadata = null;
		[JsonPropertyName("geometry")]
		public SvoModel Geometry = null;
		#endregion Data
		#region Model
		public Model(Stream stream) : this()
		{
			using BinaryReader reader = new(
				input: stream,
				encoding: Encoding.UTF8,
				leaveOpen: true);
			FromReader(reader);
		}
		public Model(BinaryReader reader) : this() => FromReader(reader);
		private void FromReader(BinaryReader reader)
		{
			reader.TryRIFF((reader, fourCC) =>
				{
					if (!"DATA".Equals(fourCC))
						return false;
					Metadata = new(reader);
					return true;
				});
			if (!reader.TryRIFF((reader, fourCC) =>
				{
					if (!"SVOG".Equals(fourCC))
						return false;
					Geometry = new(reader);
					return true;
				}))
				throw new IOException("Expected \"SVOG\"");
		}
		#endregion Model
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
			Metadata?.RIFF("DATA", writer);
			Geometry.RIFF("SVOG", writer);
		}
		#endregion IBinaryWritable
	}
	#endregion Nested classes
	#region Data
	[JsonPropertyName("metadata")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public Metadata Global { get; set; } = null;
	[JsonPropertyName("models")]
	public readonly SanitizedKeyDictionary<Model> Models = [];
	#endregion Data
	#region BenVoxelFile
	public BenVoxelFile(Stream stream) : this()
	{
		using BinaryReader reader = new(
			input: stream,
			encoding: Encoding.UTF8,
			leaveOpen: true);
		FromReader(reader);
	}
	public BenVoxelFile(BinaryReader reader) : this() => FromReader(reader);
	private void FromReader(BinaryReader reader)
	{
		if (!FourCC(reader).Equals("BENV"))
			throw new IOException("Expected \"BENV\"");
		uint totalLength = reader.ReadUInt32();
		string version = ReadKey(reader);
		using MemoryStream decompressedStream = new();
		using (MemoryStream compressedStream = new(reader.ReadBytes((int)(totalLength - version.Length - 1))))
		using (DeflateStream deflateStream = new(
			stream: compressedStream,
			mode: CompressionMode.Decompress))
		{
			deflateStream.CopyTo(decompressedStream);
		}
		decompressedStream.Position = 0;
		using BinaryReader decompressedReader = new(
			input: decompressedStream,
			encoding: Encoding.UTF8);
		decompressedReader.TryRIFF((reader, fourCC) =>
		{
			if (!"DATA".Equals(fourCC))
				return false;
			Global = new(reader);
			return true;
		});
		ushort count = decompressedReader.ReadUInt16();
		for (ushort i = 0; i < count; i++)
		{
			string name = ReadKey(decompressedReader);
			if (!decompressedReader.TryRIFF((reader, fourCC) =>
			{
				if (!"MODL".Equals(fourCC))
					return false;
				Models[name] = new(reader);
				return true;
			}))
				throw new IOException("Expected \"MODL\"");
		}
	}
	public static BenVoxelFile Load(string path)
	{
		using FileStream fileStream = new(
			path: path,
			mode: FileMode.Open,
			access: FileAccess.Read);
		return ".json".Equals(Path.GetExtension(path), StringComparison.InvariantCultureIgnoreCase) ?
			JsonSerializer.Deserialize<BenVoxelFile>(fileStream)
			: new(fileStream);
	}
	public BenVoxelFile Save(string path)
	{
		if (".json".Equals(Path.GetExtension(path), StringComparison.InvariantCultureIgnoreCase))
			File.WriteAllText(path: path, contents: JsonSerializer.Serialize(this).TabsJson());
		else
			using (FileStream fileStream = new(
				path: path,
				mode: FileMode.OpenOrCreate,
				access: FileAccess.Write))
			{
				Write(fileStream);
			}
		return this;
	}
	public SvoModel Default(out uint[] palette)
	{
		if (!Models.TryGetValue("", out Model benVoxelFileModel))
			benVoxelFileModel = Models.FirstOrDefault().Value;
		SvoModel svoModel = benVoxelFileModel?.Geometry;
		if (!(benVoxelFileModel?.Metadata?.Palettes.TryGetValue("", out Color[] colors) ?? false)
			&& !(Global?.Palettes.TryGetValue("", out colors) ?? false))
			colors = benVoxelFileModel?.Metadata?.Palettes?.Any() ?? false ?
				benVoxelFileModel.Metadata.Palettes.First().Value
				: Global?.Palettes?.Any() ?? false ?
					Global.Palettes.First().Value
					: null;
		palette = colors is null ? null : [.. colors.Take(256).Select(color => color.Rgba)];
		return svoModel;
	}
	#endregion BenVoxelFile
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
		using MemoryStream compressedStream = new();
		using (DeflateStream deflateStream = new(
			stream: compressedStream,
			mode: CompressionMode.Compress,
			leaveOpen: true))
		using (BinaryWriter deflateWriter = new(
			output: deflateStream,
			encoding: Encoding.UTF8,
			leaveOpen: true))
		{
			Global?.RIFF("DATA", deflateWriter);
			deflateWriter.Write((ushort)Models.Count);
			foreach (KeyValuePair<string, Model> model in Models)
			{
				WriteKey(deflateWriter, model.Key);
				model.Value.RIFF("MODL", deflateWriter);
			}
			deflateWriter.Flush();
		}
		writer.Write(Encoding.UTF8.GetBytes("BENV"));
		writer.Write((uint)(Version.Length + 1 + compressedStream.Length));
		WriteKey(writer, Version);
		writer.Write(compressedStream.ToArray());
	}
	#endregion IBinaryWritable
	#region Utilities
	public static string FourCC(BinaryReader reader) => Encoding.UTF8.GetString(reader.ReadBytes(4));
	public static string ReadKey(BinaryReader reader) => Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadByte()));
	public static void WriteKey(BinaryWriter writer, string s)
	{
		writer.Write((byte)s.Length);
		writer.Write(Encoding.UTF8.GetBytes(s));
	}
	public static string ReadString(BinaryReader reader) => Encoding.UTF8.GetString(reader.ReadBytes((int)reader.ReadUInt32()));
	public static void WriteString(BinaryWriter writer, string s)
	{
		writer.Write((uint)s.Length);
		writer.Write(Encoding.UTF8.GetBytes(s));
	}
	#endregion Utilities
}
