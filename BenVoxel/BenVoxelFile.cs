using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
	public readonly record struct Color(uint Rgba = 0u, string Description = null)
	{
		#region Color
		public Color(JsonObject json) : this(
			Rgba: uint.Parse(json["rgba"].GetValue<string>()[1..], System.Globalization.NumberStyles.HexNumber),
			Description: json.TryGetPropertyValue("description", out JsonNode description) ?
				description.GetValue<string>()
				: null)
		{ }
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
	public BenVoxelFile(JsonObject json) : this()
	{
		if (json.TryGetPropertyValue("metadata", out JsonNode metadata))
			Global = Metadata.FromJson(metadata.AsObject());
		foreach (KeyValuePair<string, JsonNode> model in json["models"].AsObject())
			Models[model.Key] = new(model.Value.AsObject());
	}
	public static BenVoxelFile Load(string path)
	{
		using FileStream fileStream = new(
			path: path,
			mode: FileMode.Open,
			access: FileAccess.Read);
		return ".json".Equals(Path.GetExtension(path), StringComparison.InvariantCultureIgnoreCase) ?
			new(JsonSerializer.Deserialize<JsonObject>(fileStream)
				?? throw new NullReferenceException())
			: new(fileStream);
	}
	public BenVoxelFile Save(string path)
	{
		if (".json".Equals(Path.GetExtension(path), StringComparison.InvariantCultureIgnoreCase))
			File.WriteAllText(path: path, contents: ToJson().Tabs());
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
