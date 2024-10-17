using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Text;
using K4os.Compression.LZ4.Streams;

namespace BenVoxel;

[XmlRoot("BenVoxel")]
public class BenVoxelFile : IBinaryWritable, IXmlSerializable
{
	public const string Version = "0.1";
	#region Nested classes
	public class Metadata : IBinaryWritable, IXmlSerializable
	{
		#region Metadata
		public readonly SanitizedKeyDictionary<string> Properties = [];
		public readonly SanitizedKeyDictionary<Point3D> Points = [];
		public readonly SanitizedKeyDictionary<Color[]> Palettes = [];
		/// <summary>
		/// Gets and sets palletes without descriptions
		/// </summary>
		public uint[] this[string paletteName]
		{
			get => [.. Palettes[paletteName].Select(color => color.Argb)];
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
							Palettes[key] = [.. colors.Select(argb => new Color
							{
								Argb = argb,
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
						writer.Write(color.Argb);
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
		#region IXmlSerializable
		public XmlSchema GetSchema() => null;
		public void ReadXml(XmlReader reader)
		{
			XElement root = XElement.Load(reader);
			foreach (XElement property in root.Elements("Property"))
				Properties[property.Attribute("Name").Value] = property.Value;
			foreach (XElement point in root.Elements("Point"))
				Points[point.Attribute("Name").Value] = new Point3D(
					X: Convert.ToInt32(point.Attribute("X").Value),
					Y: Convert.ToInt32(point.Attribute("Y").Value),
					Z: Convert.ToInt32(point.Attribute("Z").Value));
			foreach (XElement palette in root.Elements("Palette"))
				Palettes[palette.Attribute("Name").Value] = [.. palette.Elements("Color").Take(256).Select(e =>
				{
					Color color = new();
					color.ReadXml(e.CreateReader());
					return color;
				})];
		}
		public void WriteXml(XmlWriter writer) => new XElement(XName.Get("Metadata"),
			Properties.Select(property =>
				new XElement(XName.Get("Property"),
					new XAttribute(XName.Get("Name"), property.Key),
					property.Value)),
			Points.Select(point =>
				new XElement(XName.Get("Point"),
					new XAttribute(XName.Get("Name"), point.Key),
					new XAttribute(XName.Get("X"), point.Value.X),
					new XAttribute(XName.Get("Y"), point.Value.Y),
					new XAttribute(XName.Get("Z"), point.Value.Z))),
			Palettes.Select(palette =>
				new XElement(XName.Get("Palette"),
					new XAttribute(XName.Get("Name"), palette.Key),
					palette.Value.Take(256)))
			).WriteTo(writer);
		#endregion IXmlSerializable
	}
	public class Color : IXmlSerializable
	{
		#region Color
		public uint Argb { get; set; } = 0u;
		public string Description { get; set; } = null;
		public Color() { }
		public static IEnumerable<Color> Colors(IEnumerable<uint> colors) => colors.Select(argb => new Color { Argb = argb, });
		#endregion Color
		#region IXmlSerializable
		public XmlSchema GetSchema() => null;
		public void ReadXml(XmlReader reader)
		{
			XElement root = XElement.Load(reader);
			Argb = uint.Parse(
				s: root.Attribute("Argb").Value.Replace("#", ""),
				style: System.Globalization.NumberStyles.HexNumber);
			Description = root.Attribute("Description")?.Value;
		}
		public void WriteXml(XmlWriter writer) => new XElement(XName.Get("Color"),
			new XAttribute(XName.Get("Argb"), Argb.ToString("X")),
			string.IsNullOrWhiteSpace(Description) ? null : new XAttribute(XName.Get("Description"), Description)
			).WriteTo(writer);
		#endregion IXmlSerializable
	}
	public class Model : IBinaryWritable, IXmlSerializable
	{
		#region Model
		public Metadata Metadata = new();
		public SvoModel Geometry = new();
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
			Geometry = new(reader);
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
		#region IXmlSerializable
		public XmlSchema GetSchema() => null;
		public void ReadXml(XmlReader reader)
		{
			XElement root = XElement.Load(reader);
			if (root.Element("Metadata") is XElement metadata)
				Metadata = (Metadata)new XmlSerializer(typeof(Metadata)).Deserialize(metadata.CreateReader());
			Geometry = (SvoModel)new XmlSerializer(typeof(SvoModel)).Deserialize(root.Element("Geometry").CreateReader());
		}
		public void WriteXml(XmlWriter writer) => new XElement(XName.Get("Model"), Metadata, Geometry).WriteTo(writer);
		#endregion IXmlSerializable
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
		if (writer.BaseStream.Position % 2 != 0)
			writer.Write((byte)0);
		long position = writer.BaseStream.Position;
		writer.BaseStream.Position = sizePosition;
		writer.Write((uint)(position - sizePosition + 4));
		writer.BaseStream.Position = position;
	}
	#endregion IBinaryWritable
	#region IXmlSerializable
	public XmlSchema GetSchema() => null;
	public void ReadXml(XmlReader reader)
	{
		XElement root = XElement.Load(reader);
		if (root.Elements("Metadata").FirstOrDefault() is XElement global)
			Global = (Metadata)new XmlSerializer(typeof(Metadata)).Deserialize(global.CreateReader());
		foreach (XElement model in root.Elements("Model"))
			Models[model.Attributes("Name").FirstOrDefault()?.Value ?? ""] = (Model)new XmlSerializer(typeof(Model)).Deserialize(model.CreateReader());
	}
	public void WriteXml(XmlWriter writer) => new XElement(XName.Get("BenVoxel"),
		new XAttribute(XName.Get("Version"), Version),
		Global,
		Models.Select(model =>
		{
			XElement xModel = model.Value.ToXElement();
			xModel.Add(new XAttribute(XName.Get("Name"), model.Key));
			return xModel;
		})).WriteTo(writer);
	#endregion IXmlSerializable
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
