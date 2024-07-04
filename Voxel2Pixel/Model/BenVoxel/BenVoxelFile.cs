using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml;
using System.Xml.Serialization;

namespace Voxel2Pixel.Model.BenVoxel
{
	[XmlRoot("BenVoxel")]
	public class BenVoxelFile : IXmlSerializable
	{
		public class Metadata : IXmlSerializable
		{
			public readonly SanitizedKeyDictionary<string> Properties = [];
			public readonly SanitizedKeyDictionary<Point3D> Points = [];
			public readonly SanitizedKeyDictionary<uint[]> Palettes = [];
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
		public Metadata Global = null;
		public class Model : IXmlSerializable
		{
			public Metadata Metadata = new();
			public SvoModel Geometry = new();
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
		public readonly SanitizedKeyDictionary<Model> Models = [];
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
	}
}
