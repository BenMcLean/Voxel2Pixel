using BenVoxel;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace Voxel2Pixel.Render;

/// <summary>
/// This is an expansion upon the XML texture atlas metadata format used by Kenney. https://kenney.nl/
/// Supports both XML and JSON serialization using sanitized dictionary keys.
/// </summary>
public class TextureAtlas
{
	[XmlAttribute("imagePath")]
	[JsonPropertyName("imagePath")]
	public string ImagePath { get; set; }
	public class SubTexture
	{
		[XmlAttribute("x")]
		[JsonPropertyName("x")]
		public ushort X { get; set; }
		[XmlAttribute("y")]
		[JsonPropertyName("y")]
		public ushort Y { get; set; }
		[XmlAttribute("width")]
		[JsonPropertyName("width")]
		public ushort Width { get; set; }
		[XmlAttribute("height")]
		[JsonPropertyName("height")]
		public ushort Height { get; set; }
		#region Expansion beyond Kenney's format
		/// <summary>
		/// Points use signed 32-bit integers so that they can refer to areas outside of their subtextures.
		/// </summary>
		public class Point
		{
			[XmlAttribute("x")]
			[JsonPropertyName("x")]
			public int X { get; set; }
			[XmlAttribute("y")]
			[JsonPropertyName("y")]
			public int Y { get; set; }
		}
		public class XmlPoint : Point
		{
			[XmlAttribute("name")]
			public string Name { get; set; }
		}
		[JsonPropertyName("points")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		[XmlIgnore]
		public SanitizedKeyDictionary<Point> Points { get; set; } = [];
		[XmlElement("Point")]
		[JsonIgnore]
		public XmlPoint[] PointsArray
		{
			get => Points.Any() ? [.. Points.Select(kvp => new XmlPoint { Name = kvp.Key, X = kvp.Value.X, Y = kvp.Value.Y })] : null;
			set
			{
				Points.Clear();
				if (value is null) return;
				foreach (XmlPoint point in value)
					Points[point.Name] = point;
			}
		}
		#endregion Expansion beyond Kenney's format
	}
	public class XmlSubTexture : SubTexture
	{
		[XmlAttribute("name")]
		public string Name { get; set; }
	}
	[JsonPropertyName("subTextures")]
	[XmlIgnore]
	public SanitizedKeyDictionary<SubTexture> SubTextures { get; set; } = [];
	[XmlElement("SubTexture")]
	[JsonIgnore]
	public XmlSubTexture[] SubTexturesArray
	{
		get => [.. SubTextures.Select(kvp => new XmlSubTexture { Name = kvp.Key, X = kvp.Value.X, Y = kvp.Value.Y, Width = kvp.Value.Width, Height = kvp.Value.Height, Points = kvp.Value.Points })];
		set
		{
			SubTextures.Clear();
			if (value is null) return;
			foreach (XmlSubTexture subTexture in value)
				SubTextures[subTexture.Name] = subTexture;
		}
	}
}
