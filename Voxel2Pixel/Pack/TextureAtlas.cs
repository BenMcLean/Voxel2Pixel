using System;
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
			public int X { get; set; }
			[DataMember]
			[XmlAttribute("y")]
			public int Y { get; set; }
			[DataMember]
			[XmlAttribute("width")]
			public int Width { get; set; }
			[DataMember]
			[XmlAttribute("height")]
			public int Height { get; set; }
		}
		[DataMember]
		[XmlElement("SubTexture")]
		public SubTexture[] SubTextures { get; set; }
	}
}
