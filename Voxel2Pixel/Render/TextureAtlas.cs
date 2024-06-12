using System;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace Voxel2Pixel.Render
{
	/// <summary>
	/// This is an expansion upon the XML texture atlas metadata format used by Kenney. https://kenney.nl/
	/// </summary>
	[DataContract]
	[Serializable]
	[XmlRoot("TextureAtlas")]
	public class TextureAtlas
	{
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
	}
}
