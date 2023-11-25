using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace Voxel2Pixel.Pack
{
	/// <summary>
	/// This is an expansion upon the XML texture atlas metadata format used by Kenney. https://kenney.nl/
	/// </summary>
	[DataContract]
	[Serializable]
	[XmlRoot("TextureAtlas")]
	public class TextureAtlas
	{
		#region Data
		[DataMember]
		[XmlAttribute("imagePath")]
		public string ImagePath { get; set; }
		[DataContract]
		[Serializable]
		[XmlRoot("SubTexture")]
		public class SubTexture
		{
			[DataMember]
			[XmlAttribute("name")]
			public string Name { get; set; }
			[DataMember]
			[XmlAttribute("x")]
			public int X { get; set; } = 0;
			[DataMember]
			[XmlAttribute("y")]
			public int Y { get; set; } = 0;
			[DataMember]
			[XmlAttribute("width")]
			public int Width { get; set; } = 0;
			[DataMember]
			[XmlAttribute("height")]
			public int Height { get; set; } = 0;
			#region Expansion beyond Kenney's format
			[DataContract]
			[Serializable]
			[XmlRoot("Point")]
			public class Point
			{
				[DataMember]
				[XmlAttribute("name")]
				public string Name { get; set; }
				[DataMember]
				[XmlAttribute("x")]
				public int X { get; set; }
				[DataMember]
				[XmlAttribute("y")]
				public int Y { get; set; }
			}
			[DataMember]
			[XmlElement("Point")]
			public Point[] Points { get; set; }
			#endregion Expansion beyond Kenney's format
		}
		[DataMember]
		[XmlElement("SubTexture")]
		public SubTexture[] SubTextures { get; set; }
		#endregion Data
		#region Constructors
		public TextureAtlas() { }
		public TextureAtlas(RectpackSharp.PackingRectangle[] packingRectangles, ushort[][] origins = null) : this()
		{
			SubTextures = Enumerable.Range(0, packingRectangles.Length)
				.Select(i => new SubTexture
				{
					Name = packingRectangles[i].Id.ToString(),
					X = (int)packingRectangles[i].X + 1,
					Y = (int)packingRectangles[i].Y + 1,
					Width = (int)packingRectangles[i].Width - 2,
					Height = (int)packingRectangles[i].Height - 2,
					Points = new SubTexture.Point[] { new SubTexture.Point
					{
						Name = "origin",
						X = origins[i][0],
						Y = origins[i][1],
					}},
				})
				.ToArray();
		}
		#endregion Constructors
	}
}
