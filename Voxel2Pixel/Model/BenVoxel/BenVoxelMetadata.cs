using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Voxel2Pixel.Model.BenVoxel
{
	[XmlRoot("Metadata")]
	public class BenVoxelMetadata : IXmlSerializable
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
				Properties[property.Attributes().Where(a => a.Name.Equals("Name")).FirstOrDefault()?.Value ?? ""] = property.Value;
			foreach (XElement point in root.Elements("Point"))
				Points[point.Attributes().Where(a => a.Name.Equals("Name")).FirstOrDefault()?.Value ?? ""] = new Point3D(
					X: Convert.ToInt32(point.Attributes().Where(a => a.Name.Equals("X")).First().Value),
					Y: Convert.ToInt32(point.Attributes().Where(a => a.Name.Equals("Y")).First().Value),
					Z: Convert.ToInt32(point.Attributes().Where(a => a.Name.Equals("Z")).First().Value));
			foreach (XElement palette in root.Elements("Palette"))
				Palettes[palette.Attributes().Where(a => a.Name.Equals("Name")).FirstOrDefault()?.Value ?? ""] = palette
					.Elements("Color")
					.Take(256)
					.Select(color =>
					{
						uint argb = uint.Parse(
							s: color
								.Attributes()
								.Where(a => a.Name.Equals("Hex"))
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
						new XAttribute(XName.Get("ARGB"), "#" + (rgba << 24 | rgba >> 8).ToString("X")))))
					.WriteTo(writer);
		}
		#endregion IXmlSerializable
	}
}
