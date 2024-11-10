using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace Voxel2Pixel.Render;

/// <summary>
/// This is an expansion upon the XML texture atlas metadata format used by Kenney. https://kenney.nl/
/// Supports both XML and JSON serialization.
/// </summary>
public class TextureAtlas
{
	[XmlAttribute("imagePath")]
	[JsonPropertyName("imagePath")]
	public string ImagePath { get; set; }
	public class SubTexture
	{
		[XmlAttribute("name")]
		[JsonPropertyName("name")]
		public string Name { get; set; }
		[XmlAttribute("x")]
		[JsonPropertyName("x")]
		public ushort X { get; set; } = 0;
		[XmlAttribute("y")]
		[JsonPropertyName("y")]
		public ushort Y { get; set; } = 0;
		[XmlAttribute("width")]
		[JsonPropertyName("width")]
		public ushort Width { get; set; } = 0;
		[XmlAttribute("height")]
		[JsonPropertyName("height")]
		public ushort Height { get; set; } = 0;
		#region Expansion beyond Kenney's format
		/// <summary>
		/// Points use signed 32-bit integers so that they can refer to areas outside of their subtextures.
		/// </summary>
		public class Point
		{
			[XmlAttribute("name")]
			[JsonPropertyName("name")]
			public string Name { get; set; }
			[XmlAttribute("x")]
			[JsonPropertyName("x")]
			public int X { get; set; }
			[XmlAttribute("y")]
			[JsonPropertyName("y")]
			public int Y { get; set; }
		}
		[XmlElement("Point")]
		[JsonPropertyName("points")]
		public Point[] Points { get; set; }
		#endregion Expansion beyond Kenney's format
	}
	[XmlElement("SubTexture")]
	[JsonPropertyName("subTextures")]
	public SubTexture[] SubTextures { get; set; }
}
