using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BenVoxel;

public class BenVoxelFile : IBinaryWritable
{
	public const string Version = "0.1";
	#region Nested classes
	public class Metadata : IBinaryWritable
	{
		#region Data
		public readonly SanitizedKeyDictionary<string> Properties = [];
		public readonly SanitizedKeyDictionary<Point3D> Points = [];
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
		public Metadata() { }
		public Metadata(Stream stream)
		{
			using BinaryReader reader = new(
				input: stream,
				encoding: Encoding.UTF8,
				leaveOpen: true);
			FromReader(reader);
		}
		public Metadata(BinaryReader reader) => FromReader(reader);
		private void FromReader(BinaryReader reader)
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
		public Metadata(JsonObject json)
		{
			if (json.TryGetPropertyValue("properties", out JsonNode properties))
				foreach (KeyValuePair<string, JsonNode> property in properties.AsObject())
					Properties[property.Key] = property.Value.GetValue<string>();
			if (json.TryGetPropertyValue("points", out JsonNode points))
				foreach (KeyValuePair<string, JsonNode> point in points.AsObject())
					Points[point.Key] = new Point3D(JsonSerializer.Deserialize<int[]>(point.Value));
			if (json.TryGetPropertyValue("palettes", out JsonNode palettes))
				foreach (KeyValuePair<string, JsonNode> palette in palettes.AsObject())
					Palettes[palette.Key] = [.. palette.Value.AsArray().Take(256).Select(color => new Color(color.AsObject()))];
		}
		public bool Any() => Properties.Any() || Points.Any() || Palettes.Any();
		#endregion Metadata
		#region IBinaryWritable
		public void Write(Stream stream)
		{
			using BinaryWriter writer = new(output: stream, encoding: Encoding.UTF8, leaveOpen: true);
			Write(writer);
		}
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
		#region JSON
		public JsonObject ToJson()
		{
			if (!Any()) return null;
			JsonObject metadata = [];
			if (Properties.Any())
				metadata.Add("properties", new JsonObject(Properties.Select(property => new KeyValuePair<string, JsonNode>(property.Key, JsonValue.Create(property.Value)))));
			if (Points.Any())
				metadata.Add("points", new JsonObject(Points.Select(point => new KeyValuePair<string, JsonNode>(point.Key, JsonSerializer.SerializeToNode(point.Value.ToArray())))));
			if (Palettes.Any())
				metadata.Add("palettes", new JsonObject(Palettes.Select(palette => new KeyValuePair<string, JsonNode>(palette.Key, new JsonArray([.. palette.Value.Take(256).Select(color => (JsonNode)color.ToJson())])))));
			return metadata;
		}
		public static Metadata FromJson(JsonObject json) => new Metadata(json) is Metadata metadata && metadata.Any() ? metadata : null;
		#endregion JSON
	}
	public class Color
	{
		#region Data
		public uint Rgba { get; set; } = 0u;
		public string Description { get; set; } = null;
		#endregion Data
		#region Color
		public Color() { }
		public Color(JsonObject json)
		{
			Rgba = uint.Parse(json["rgba"].GetValue<string>()[1..], System.Globalization.NumberStyles.HexNumber);
			if (json.TryGetPropertyValue("description", out JsonNode description))
				Description = description.GetValue<string>();
		}
		public static IEnumerable<Color> Colors(IEnumerable<uint> colors) => colors.Select(rgba => new Color { Rgba = rgba, });
		#endregion Color
		#region JSON
		public JsonObject ToJson()
		{
			JsonObject json = new() { { "rgba", JsonValue.Create($"#{Rgba:X8}") } };
			if (!string.IsNullOrWhiteSpace(Description))
				json.Add("description", JsonValue.Create(Description));
			return json;
		}
		public static Color FromJson(JsonObject json) => new(json);
		#endregion JSON
	}
	public class Model : IBinaryWritable
	{
		#region Data
		public Metadata Metadata = null;
		public SvoModel Geometry = null;
		#endregion Data
		#region Model
		public Model() { }
		public Model(Stream stream)
		{
			using BinaryReader reader = new(
				input: stream,
				encoding: Encoding.UTF8,
				leaveOpen: true);
			FromReader(reader);
		}
		public Model(BinaryReader reader) => FromReader(reader);
		private void FromReader(BinaryReader reader)
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
		public Model(JsonObject json)
		{
			if (json.TryGetPropertyValue("metadata", out JsonNode metadataNode))
				Metadata = new Metadata(metadataNode.AsObject());
			if (!json.TryGetPropertyValue("geometry", out JsonNode geometryNode))
				throw new InvalidDataException("Missing geometry data.");
			JsonObject geometry = geometryNode.AsObject();
			int[] size = JsonSerializer.Deserialize<int[]>(geometry["size"]);
			Geometry = new SvoModel(
				z85: geometry["z85"].GetValue<string>(),
				sizeX: (ushort)size[0],
				sizeY: (ushort)size[1],
				sizeZ: (ushort)size[2]
			);
		}
		#endregion Model
		#region IBinaryWritable
		public void Write(Stream stream)
		{
			if (Metadata is not null)
				using (MemoryStream metadata = Metadata.RIFF("DATA"))
					metadata.CopyTo(stream);
			using MemoryStream geometry = Geometry.RIFF("SVOG");
			geometry.CopyTo(stream);
		}
		public void Write(BinaryWriter writer)
		{
			writer.Flush();
			Write(writer.BaseStream);
		}
		#endregion IBinaryWritable
		#region JSON
		public JsonObject ToJson()
		{
			JsonObject model = [];
			if (Metadata?.ToJson() is JsonObject metadata)
				model.Add("metadata", metadata);
			if (Geometry is null)
				throw new NullReferenceException("Missing geometry data.");
			model.Add("geometry", new JsonObject
			{
				{ "size", JsonSerializer.SerializeToNode(new[] { Geometry.SizeX, Geometry.SizeY, Geometry.SizeZ }) },
				{ "z85", JsonValue.Create(Geometry.Z85()) },
			});
			return model;
		}
		public static Model FromJson(JsonObject json) => new(json);
		#endregion JSON
	}
	#endregion Nested classes
	#region Data
	public Metadata Global = null;
	public readonly SanitizedKeyDictionary<Model> Models = [];
	#endregion Data
	#region BenVoxelFile
	public BenVoxelFile() { }
	public BenVoxelFile(Stream stream)
	{
		using BinaryReader reader = new(
			input: stream,
			encoding: Encoding.UTF8,
			leaveOpen: true);
		FromReader(reader);
	}
	public BenVoxelFile(BinaryReader reader) => FromReader(reader);
	private void FromReader(BinaryReader reader)
	{
		if (!FourCC(reader).Equals("BENV"))
			throw new IOException("Expected \"BENV\"");
		uint totalLength = reader.ReadUInt32();
		string version = ReadKey(reader);
		string fourCC = FourCC(reader);
		if (fourCC.Equals("DATA"))
		{
			reader.ReadUInt32();
			Global = new(reader);
		}
		else
			reader.BaseStream.Position -= 4;
		ushort count = reader.ReadUInt16();
		for (ushort i = 0; i < count; i++)
		{
			string name = ReadKey(reader);
			fourCC = FourCC(reader);
			if (!"MODL".Equals(fourCC))
				throw new InvalidDataException($"Unexpected chunk type. Expected: \"MODL\", Actual: \"{fourCC}\".");
			reader.ReadUInt32();
			Models[name] = new(reader);
		}
	}
	public BenVoxelFile(JsonObject json)
	{
		if (json.TryGetPropertyValue("metadata", out JsonNode metadata))
			Global = Metadata.FromJson(metadata.AsObject());
		foreach (KeyValuePair<string, JsonNode> model in json["models"].AsObject())
			Models[model.Key] = new(model.Value.AsObject());
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
		byte[] uncompressedBytes;
		using (MemoryStream uncompressedStream = new())
		{
			using BinaryWriter contentWriter = new(uncompressedStream, Encoding.UTF8, leaveOpen: true);
			Global?.CopyRIFF("DATA", uncompressedStream);
			contentWriter.Write((ushort)Models.Count);
			foreach (KeyValuePair<string, Model> model in Models)
			{
				WriteKey(contentWriter, model.Key);
				model.Value.CopyRIFF("MODL", uncompressedStream);
			}
			uncompressedBytes = uncompressedStream.ToArray();
		}
		writer.Write(Encoding.UTF8.GetBytes("BENV"));
		writer.Write((uint)(Version.Length + 1 + uncompressedBytes.Length));
		WriteKey(writer, Version);
		writer.Write(uncompressedBytes);
	}
	#endregion IBinaryWritable
	#region JSON
	public JsonObject ToJson()
	{
		JsonObject root = new() { { "version", JsonValue.Create(Version) } };
		if (Global?.ToJson() is JsonObject global)
			root.Add("metadata", global);
		if (Models.Any())
			root.Add("models", new JsonObject(Models.Select(model => new KeyValuePair<string, JsonNode>(model.Key, model.Value.ToJson()))));
		return root;
	}
	public static BenVoxelFile FromJson(JsonObject json) => new(json);
	#endregion JSON
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
