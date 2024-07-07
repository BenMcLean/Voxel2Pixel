using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml;
using System.Xml.Serialization;
using Voxel2Pixel.Interfaces;
using System.IO;
using System.Text;

namespace Voxel2Pixel.Model.BenVoxel
{
	[XmlRoot("BenVoxel")]
	public class BenVoxelFile : IBinaryWritable, IXmlSerializable
	{
		#region Nested classes
		public class Metadata : IBinaryWritable, IXmlSerializable
		{
			#region Metadata
			public readonly SanitizedKeyDictionary<string> Properties = [];
			public readonly SanitizedKeyDictionary<Point3D> Points = [];
			public readonly SanitizedKeyDictionary<uint[]> Palettes = [];
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
								Palettes[ReadKey(msReader)] = Enumerable.Range(0, msReader.ReadByte() + 1).Select(i => msReader.ReadUInt32()).ToArray();
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
			public void Write(Stream stream) => Write(new BinaryWriter(output: stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true));
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
					foreach (KeyValuePair<string, uint[]> palette in Palettes)
					{
						WriteKey(msWriter, palette.Key);
						writer.Write((byte)palette.Value.Length - 1);
						foreach (uint color in palette.Value)
							writer.Write(color);
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
					Properties[property.Attributes().Where(a => a.Name.ToString().Equals("Name")).FirstOrDefault()?.Value ?? ""] = property.Value;
				foreach (XElement point in root.Elements("Point"))
					Points[point.Attributes().Where(a => a.Name.ToString().Equals("Name")).FirstOrDefault()?.Value ?? ""] = new Point3D(
						X: Convert.ToInt32(point.Attributes().Where(a => a.Name.ToString().Equals("X")).First().Value),
						Y: Convert.ToInt32(point.Attributes().Where(a => a.Name.ToString().Equals("Y")).First().Value),
						Z: Convert.ToInt32(point.Attributes().Where(a => a.Name.ToString().Equals("Z")).First().Value));
				foreach (XElement palette in root.Elements("Palette"))
					Palettes[palette.Attributes().Where(a => a.Name.ToString().Equals("Name")).FirstOrDefault()?.Value ?? ""] = palette
						.Elements("Color")
						.Take(256)
						.Select(color =>
						{
							uint argb = uint.Parse(
								s: color
									.Attributes()
									.Where(a => a.Name.ToString().Equals("Argb"))
									.First()
									.Value
									.Replace("#", ""),
								style: System.Globalization.NumberStyles.HexNumber);
							return argb << 8 | argb >> 24;
						})
						.ToArray();
			}
			public void WriteXml(XmlWriter writer)
			{
				foreach (KeyValuePair<string, string> property in Properties)
					new XElement(XName.Get("Property"),
						new XAttribute(XName.Get("Name"), property.Key), property.Value)
						.WriteTo(writer);
				foreach (KeyValuePair<string, Point3D> point in Points)
					new XElement(XName.Get("Point"),
						new XAttribute(XName.Get("Name"), point.Key),
						new XAttribute(XName.Get("X"), point.Value.X),
						new XAttribute(XName.Get("Y"), point.Value.Y),
						new XAttribute(XName.Get("Z"), point.Value.Z))
						.WriteTo(writer);
				foreach (KeyValuePair<string, uint[]> palette in Palettes)
					new XElement(XName.Get("Palette"),
						new XAttribute(XName.Get("Name"), palette.Key),
						palette.Value.Take(256).Select(rgba => new XElement(XName.Get("Color"),
							new XAttribute(XName.Get("Argb"), "#" + (rgba << 24 | rgba >> 8).ToString("X")))))
						.WriteTo(writer);
			}
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
				if (!FourCC(reader).Equals("DATA"))
					throw new IOException("Couldn't parse model metadata!");
				Metadata = new(new MemoryStream(reader.ReadBytes((int)reader.ReadUInt32())));
				if (!FourCC(reader).Equals("SVOG"))
					throw new IOException("Couldn't parse model geometry!");
				Geometry = new(reader);
			}
			#endregion Model
			#region IBinaryWritable
			public void Write(Stream stream)
			{
				Metadata.RIFF("DATA").CopyTo(stream);
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
				Metadata = new();
				Metadata.ReadXml(root.CreateReader());
				Geometry = (SvoModel)new XmlSerializer(typeof(SvoModel)).Deserialize(root.Elements("Geometry").First().CreateReader());
			}
			public void WriteXml(XmlWriter writer)
			{
				Metadata.WriteXml(writer);
				new XmlSerializer(typeof(SvoModel)).Serialize(
					xmlWriter: writer,
					o: Geometry,
					namespaces: new([XmlQualifiedName.Empty]));
			}
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
			MemoryStream memoryStream = new(reader.ReadBytes((int)reader.ReadUInt32()));
			BinaryReader msReader = new(memoryStream);
			string fourCC = FourCC(msReader);
			if (fourCC.Equals("DATA"))
			{
				Global = new(new MemoryStream(msReader.ReadBytes((int)msReader.ReadUInt32())));
				fourCC = FourCC(msReader);
			}
			while (msReader.BaseStream.Position < msReader.BaseStream.Length - 4
				&& fourCC.Equals("MODL"))
			{
				MemoryStream modelStream = new(msReader.ReadBytes((int)reader.ReadUInt32()));
				BinaryReader modelReader = new(modelStream);
				Models[ReadKey(modelReader)] = new(modelReader);
				fourCC = FourCC(reader);
			}
		}
		#endregion BenVoxelFile
		#region IBinaryWritable
		public void Write(Stream stream) => Write(new BinaryWriter(output: stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true));
		public void Write(BinaryWriter writer)
		{
			writer.Write(Encoding.UTF8.GetBytes("BENV"));
			long sizePosition = writer.BaseStream.Position;
			writer.BaseStream.Position += 4;

			if (Global is not null)
			{
				writer.Flush();
				Global.RIFF("DATA").CopyTo(writer.BaseStream);
			}
			foreach (KeyValuePair<string, Model> model in Models)
			{
				MemoryStream ms = new();
				BinaryWriter msWriter = new(ms);
				WriteKey(msWriter, model.Key);
				model.Value.Write(msWriter);
				writer.RIFF("MODL", ms.ToArray());
			}
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
			if (root.Elements("Property").Any()
				|| root.Elements("Point").Any()
				|| root.Elements("Palette").Any())
			{
				Global = new Metadata();
				Global.ReadXml(root.CreateReader());
			}
			foreach (XElement model in root.Elements("Model"))
				Models[model.Attributes().Where(a => a.Name.ToString().Equals("Name")).FirstOrDefault()?.Value ?? ""] = (Model)new XmlSerializer(typeof(Model)).Deserialize(model.CreateReader());
		}
		public void WriteXml(XmlWriter writer)
		{
			Global?.WriteXml(writer);
			foreach (KeyValuePair<string, Model> model in Models)
			{
				XDocument doc = new();
				using (XmlWriter docWriter = doc.CreateWriter())
				{
					new XmlSerializer(typeof(Model)).Serialize(
						xmlWriter: docWriter,
						o: model.Value,
						namespaces: new([XmlQualifiedName.Empty]));
				}
				XElement element = doc.Root;
				element.Add(new XAttribute(XName.Get("Name"), model.Key));
				element.WriteTo(writer);
			}
		}
		#endregion IXmlSerializable
		#region Utilities
		public static string FourCC(BinaryReader reader) => System.Text.Encoding.UTF8.GetString(reader.ReadBytes(4));
		public static string ReadKey(BinaryReader reader) => System.Text.Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadByte()));
		public static void WriteKey(BinaryWriter writer, string s)
		{
			writer.Write((byte)s.Length);
			writer.Write(System.Text.Encoding.UTF8.GetBytes(s));
		}
		public static string ReadString(BinaryReader reader) => System.Text.Encoding.UTF8.GetString(reader.ReadBytes((int)reader.ReadUInt32()));
		public static void WriteString(BinaryWriter writer, string s)
		{
			writer.Write((uint)s.Length);
			writer.Write(System.Text.Encoding.UTF8.GetBytes(s));
		}
		#endregion Utilities
	}
}
