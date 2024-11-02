using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.Json;
using K4os.Compression.LZ4.Streams;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace BenVoxel;

public class BenVoxelFile : IBinaryWritable
{
	public const string Version = "0.1";
	#region Nested classes
	public class Metadata : IBinaryWritable
	{
		#region Metadata
		public readonly SanitizedKeyDictionary<string> Properties = [];
		public readonly SanitizedKeyDictionary<Point3D> Points = [];
		public readonly SanitizedKeyDictionary<Color[]> Palettes = [];
		/// <summary>
		/// Gets and sets palettes without descriptions
		/// </summary>
		public uint[] this[string paletteName]
		{
			get => [.. Palettes[paletteName].Select(color => color.Rgba)];
			set => Palettes[paletteName] = [.. Color.Colors(value.Take(256))];
		}
		public Metadata() { }
		public Metadata(Stream stream) : this(new BinaryReader(input: stream, encoding: Encoding.UTF8, leaveOpen: true)) { }
		public Metadata(BinaryReader reader)
		{
			bool valid = true;
			while (valid
				&& reader.BaseStream.Position < reader.BaseStream.Length - 4
				&& FourCC(reader) is string fourCC)
				switch (fourCC)
				{
					case "PROP":
						MemoryStream ms = new(reader.ReadBytes((int)reader.ReadUInt32()));
						BinaryReader msReader = new(ms);
						ushort count = msReader.ReadUInt16();
						for (ushort i = 0; i < count; i++)
							Properties[ReadKey(msReader)] = ReadString(msReader);
						break;
					case "PT3D":
						ms = new(reader.ReadBytes((int)reader.ReadUInt32()));
						msReader = new(ms);
						count = msReader.ReadUInt16();
						for (ushort i = 0; i < count; i++)
							Points[ReadKey(msReader)] = new Point3D(msReader);
						break;
					case "PALC":
						ms = new(reader.ReadBytes((int)reader.ReadUInt32()));
						msReader = new(ms);
						count = msReader.ReadUInt16();
						for (ushort i = 0; i < count; i++)
						{
							string key = ReadKey(msReader);
							uint[] colors = [.. Enumerable.Range(0, msReader.ReadByte() + 1).Select(i => msReader.ReadUInt32())];
							bool hasDescriptions = msReader.ReadByte() != 0;
							Palettes[key] = [.. colors.Select(rgba => new Color
							{
								Rgba = rgba,
								Description = hasDescriptions ? ReadString(msReader) : null,
							})];
						}
						break;
					default:
						valid = false;
						break;
				}
			if (!valid)
				reader.BaseStream.Position -= 4;
		}
		public bool Any() => Properties.Any() || Points.Any() || Palettes.Any();
		#endregion Metadata
		#region IBinaryWritable
		public void Write(Stream stream) => Write(new BinaryWriter(output: stream, encoding: Encoding.UTF8, leaveOpen: true));
		public void Write(BinaryWriter writer)
		{
			if (Properties.Any())
			{
				MemoryStream ms = new();
				BinaryWriter msWriter = new(ms);
				msWriter.Write((ushort)Properties.Count());
				foreach (KeyValuePair<string, string> property in Properties)
				{
					WriteKey(msWriter, property.Key);
					WriteString(msWriter, property.Value);
				}
				writer.RIFF("PROP", ms.ToArray());
			}
			if (Points.Any())
			{
				MemoryStream ms = new();
				BinaryWriter msWriter = new(ms);
				msWriter.Write((ushort)Points.Count());
				foreach (KeyValuePair<string, Point3D> point in Points)
				{
					WriteKey(msWriter, point.Key);
					point.Value.Write(msWriter);
				}
				writer.RIFF("PT3D", ms.ToArray());
			}
			if (Palettes.Any())
			{
				MemoryStream ms = new();
				BinaryWriter msWriter = new(ms);
				msWriter.Write((ushort)Palettes.Count());
				foreach (KeyValuePair<string, Color[]> palette in Palettes)
				{
					WriteKey(msWriter, palette.Key);
					writer.Write((byte)palette.Value.Length - 1);
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
				writer.RIFF("PALC", ms.ToArray());
			}
		}
		#endregion IBinaryWritable
		#region JSON Serialization
		public JsonObject ToJson()
		{
			JsonObject metadata = [];
			if (Properties.Any())
			{
				JsonObject properties = [];
				foreach (KeyValuePair<string, string> property in Properties)
					properties.Add(property.Key, JsonValue.Create(property.Value));
				metadata.Add("properties", properties);
			}
			if (Points.Any())
			{
				JsonObject points = [];
				foreach (KeyValuePair<string, Point3D> point in Points)
				{
					int[] coordinates = [point.Value.X, point.Value.Y, point.Value.Z];
					points.Add(point.Key, JsonSerializer.SerializeToNode(coordinates));
				}
				metadata.Add("points", points);
			}
			if (Palettes.Any())
			{
				JsonObject palettes = [];
				foreach (KeyValuePair<string, Color[]> palette in Palettes)
				{
					JsonArray colors = [];
					foreach (Color color in palette.Value)
					{
						JsonObject colorObject = new()
						{
							{ "rgba", JsonValue.Create($"#{color.Rgba:X8}") }
						};
						if (!string.IsNullOrWhiteSpace(color.Description))
							colorObject.Add("description", JsonValue.Create(color.Description));
						colors.Add(colorObject);
					}
					palettes.Add(palette.Key, colors);
				}
				metadata.Add("palettes", palettes);
			}
			return metadata;
		}
		public static Metadata FromJson(JsonObject json)
		{
			Metadata metadata = new();
			if (json.TryGetPropertyValue("properties", out JsonNode propertiesNode))
			{
				JsonObject properties = propertiesNode.AsObject();
				foreach (KeyValuePair<string, JsonNode> property in properties)
					metadata.Properties[property.Key] = property.Value.GetValue<string>();
			}
			if (json.TryGetPropertyValue("points", out JsonNode pointsNode))
			{
				JsonObject points = pointsNode.AsObject();
				foreach (KeyValuePair<string, JsonNode> point in points)
				{
					int[] coordinates = JsonSerializer.Deserialize<int[]>(point.Value);
					metadata.Points[point.Key] = new Point3D(coordinates[0], coordinates[1], coordinates[2]);
				}
			}
			if (json.TryGetPropertyValue("palettes", out JsonNode palettesNode))
			{
				JsonObject palettes = palettesNode.AsObject();
				foreach (KeyValuePair<string, JsonNode> palette in palettes)
				{
					JsonArray colors = palette.Value.AsArray();
					metadata.Palettes[palette.Key] = new Color[colors.Count];
					for (int i = 0; i < colors.Count; i++)
					{
						JsonObject colorObject = colors[i].AsObject();
						metadata.Palettes[palette.Key][i] = new Color
						{
							Rgba = uint.Parse(colorObject["rgba"].GetValue<string>()[1..], System.Globalization.NumberStyles.HexNumber),
							Description = colorObject.TryGetPropertyValue("description", out JsonNode description) ?
								description.GetValue<string>()
								: null,
						};
					}
				}
			}
			return metadata;
		}
		#endregion JSON Serialization
	}
	public class Color
	{
		#region Data
		public uint Rgba { get; set; } = 0u;
		public string Description { get; set; } = null;
		#endregion Data
		#region Color
		public Color() { }
		public static IEnumerable<Color> Colors(IEnumerable<uint> colors) => colors.Select(rgba => new Color { Rgba = rgba, });
		#endregion Color
		#region JSON Serialization
		public JsonObject ToJson()
		{
			JsonObject json = new() { { "rgba", JsonValue.Create($"#{Rgba:X8}") } };
			if (!string.IsNullOrWhiteSpace(Description))
				json.Add("description", JsonValue.Create(Description));
			return json;
		}
		public static Color FromJson(JsonObject json)
		{
			string rgba = json["rgba"].GetValue<string>();
			uint value = uint.Parse(rgba[1..], System.Globalization.NumberStyles.HexNumber);
			return new Color
			{
				Rgba = value,
				Description = json.TryGetPropertyValue("description", out JsonNode description) ?
					description.GetValue<string>() : null
			};
		}
		#endregion JSON Serialization
	}
	public class Model : IBinaryWritable
	{
		#region Model
		public Metadata Metadata = null;
		public SvoModel Geometry = null;
		public Model() { }
		public Model(Stream stream) : this(new BinaryReader(input: stream, encoding: Encoding.UTF8, leaveOpen: true)) { }
		public Model(BinaryReader reader)
		{
			string fourCC = FourCC(reader);
			if (fourCC.Equals("DATA"))
			{
				Metadata = new(new MemoryStream(reader.ReadBytes((int)reader.ReadUInt32())));
				fourCC = FourCC(reader);
			}
			if (!fourCC.Equals("SVOG"))
				throw new IOException("Couldn't parse model geometry!");
			Geometry = new(new MemoryStream(reader.ReadBytes((int)reader.ReadUInt32())));
		}
		#endregion Model
		#region IBinaryWritable
		public void Write(Stream stream)
		{
			Metadata?.RIFF("DATA").CopyTo(stream);
			Geometry.RIFF("SVOG").CopyTo(stream);
		}
		public void Write(BinaryWriter writer)
		{
			writer.Flush();
			Write(writer.BaseStream);
		}
		#endregion IBinaryWritable
		#region JSON Serialization
		public JsonObject ToJson()
		{
			JsonObject model = [];
			if (Metadata != null)
				model.Add("metadata", Metadata.ToJson());
			if (Geometry != null)
			{
				JsonObject geometry = new()
				{
					{ "size", JsonSerializer.SerializeToNode(new[] { Geometry.SizeX, Geometry.SizeY, Geometry.SizeZ }) },
					{ "z85", JsonValue.Create(Geometry.Z85()) }
				};
				model.Add("geometry", geometry);
			}
			return model;
		}
		public static Model FromJson(JsonObject json)
		{
			Model model = new();
			if (json.TryGetPropertyValue("metadata", out JsonNode metadataNode))
				model.Metadata = Metadata.FromJson(metadataNode.AsObject());
			if (json.TryGetPropertyValue("geometry", out JsonNode geometryNode))
			{
				JsonObject geometry = geometryNode.AsObject();
				int[] size = JsonSerializer.Deserialize<int[]>(geometry["size"]);
				string z85 = geometry["z85"].GetValue<string>();
				model.Geometry = new SvoModel(
					z85: z85,
					sizeX: (ushort)size[0],
					sizeY: (ushort)size[1],
					sizeZ: (ushort)size[2]
				);
			}
			return model;
		}
		#endregion JSON Serialization
	}
	#endregion Nested classes
	#region BenVoxelFile
	public Metadata Global = null;
	public readonly SanitizedKeyDictionary<Model> Models = [];
	public BenVoxelFile() { }
	public BenVoxelFile(Stream stream) : this(new BinaryReader(input: stream, encoding: Encoding.UTF8, leaveOpen: true)) { }
	public BenVoxelFile(BinaryReader reader)
	{
		if (!FourCC(reader).Equals("BENV"))
			throw new IOException("Expected \"BENV\"");
		reader.ReadUInt32();
		using LZ4DecoderStream decodingStream = LZ4Stream.Decode(
			stream: reader.BaseStream,
			leaveOpen: true);
		using BinaryReader decodingReader = new(decodingStream);
		string fourCC = FourCC(decodingReader);
		if (fourCC.Equals("DATA"))
			Global = new(new MemoryStream(decodingReader.ReadBytes((int)decodingReader.ReadUInt32())));
		else
			decodingStream.Position -= 4;
		ushort count = decodingReader.ReadUInt16();
		for (ushort i = 0; i < count; i++)
		{
			string name = ReadKey(decodingReader);
			if (!"MODL".Equals(FourCC(reader)))
				throw new InvalidDataException($"Unexpected chunk type. Expected: \"MODL\", Actual: \"{fourCC}\".");
			Models[name] = new(new MemoryStream(decodingReader.ReadBytes((int)decodingReader.ReadUInt32())));
		}
	}
	#endregion BenVoxelFile
	#region IBinaryWritable
	public void Write(Stream stream) => Write(new BinaryWriter(output: stream, encoding: Encoding.UTF8, leaveOpen: true));
	public void Write(BinaryWriter writer)
	{
		writer.Write(Encoding.UTF8.GetBytes("BENV"));
		long sizePosition = writer.BaseStream.Position;
		writer.BaseStream.Position += 4;
		WriteKey(writer, Version);
		writer.Flush();
		using (LZ4EncoderStream encoderStream = LZ4Stream.Encode(
			stream: writer.BaseStream,
			level: K4os.Compression.LZ4.LZ4Level.L12_MAX,
			leaveOpen: true))
		{
			using BinaryWriter encoderWriter = new(encoderStream);
			Global?.RIFF("DATA").CopyTo(encoderStream);
			encoderWriter.Write((ushort)Models.Count());
			foreach (KeyValuePair<string, Model> model in Models)
			{
				WriteKey(encoderWriter, model.Key);
				model.Value.RIFF("MODL").CopyTo(encoderStream);
			}
		}
		long position = writer.BaseStream.Position;
		writer.BaseStream.Position = sizePosition;
		writer.Write((uint)(position - sizePosition + 4));
		writer.BaseStream.Position = position;
	}
	#endregion IBinaryWritable
	#region JSON Serialization
	public string ToJson()
	{
		JsonObject root = new()
		{
			{ "version", JsonValue.Create(Version) }
		};
		if (Global != null)
			root.Add("metadata", Global.ToJson());
		if (Models.Any())
		{
			JsonObject models = [];
			foreach (KeyValuePair<string, Model> model in Models)
				models.Add(model.Key, model.Value.ToJson());
			root.Add("models", models);
		}
		return JsonSerializer.Serialize(root, new JsonSerializerOptions
		{
			WriteIndented = true
		});
	}
	public static BenVoxelFile FromJson(string json)
	{
		JsonObject root = JsonSerializer.Deserialize<JsonObject>(json);
		BenVoxelFile file = new();
		// Version is required by schema
		string version = root["version"].GetValue<string>();
		if (version != Version)
			throw new InvalidDataException($"Unsupported version: {version}. Expected: {Version}");
		if (root.TryGetPropertyValue("metadata", out JsonNode metadataNode))
			file.Global = Metadata.FromJson(metadataNode.AsObject());
		// Models are required by schema
		JsonObject models = root["models"].AsObject();
		foreach (KeyValuePair<string, JsonNode> model in models)
			file.Models[model.Key] = Model.FromJson(model.Value.AsObject());
		return file;
	}
	#endregion JSON Serialization
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
