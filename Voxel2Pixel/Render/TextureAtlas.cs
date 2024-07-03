using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Voxel2Pixel.Render
{
	/// <summary>
	/// This is an expansion upon the XML texture atlas metadata format used by Kenney. https://kenney.nl/
	/// </summary>
	[XmlRoot("TextureAtlas")]
	public class TextureAtlas
	{
		[XmlAttribute("imagePath")]
		public string ImagePath { get; set; }
		[XmlRoot("SubTexture")]
		public class SubTexture
		{
			[XmlAttribute("name")]
			public string Name { get; set; }
			[XmlAttribute("x")]
			public int X { get; set; } = 0;
			[XmlAttribute("y")]
			public int Y { get; set; } = 0;
			[XmlAttribute("width")]
			public int Width { get; set; } = 0;
			[XmlAttribute("height")]
			public int Height { get; set; } = 0;
			#region Expansion beyond Kenney's format
			[XmlRoot("Point")]
			public class Point
			{
				[XmlAttribute("name")]
				public string Name { get; set; }
				[XmlAttribute("x")]
				public int X { get; set; }
				[XmlAttribute("y")]
				public int Y { get; set; }
			}
			[XmlElement("Point")]
			public Point[] Points { get; set; }
			#endregion Expansion beyond Kenney's format
		}
		[XmlElement("SubTexture")]
		public SubTexture[] SubTextures { get; set; }
		#region Serialize
		public string XML(bool indent = true)
		{
			Utf8StringWriter stringWriter = new();
			new XmlSerializer(typeof(TextureAtlas)).Serialize(
				xmlWriter: XmlWriter.Create(
					output: stringWriter,
					settings: new()
					{
						Encoding = Encoding.UTF8,
						Indent = indent,
						IndentChars = "\t",
					}),
				o: this,
				namespaces: new([XmlQualifiedName.Empty]));
			return stringWriter.ToString();
		}
		public class Utf8StringWriter : StringWriter
		{
			public override Encoding Encoding => Encoding.UTF8;
		}
		#endregion Serialize
	}
}
