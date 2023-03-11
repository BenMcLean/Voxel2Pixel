using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Voxel2Pixel.Pack;
using Xunit;
using Xunit.Sdk;
using static System.Net.Mime.MediaTypeNames;

namespace Voxel2PixelTest
{
	public class TextureAtlasTest
	{
		//private readonly Xunit.Abstractions.ITestOutputHelper output;
		//public TextureAtlasTest(Xunit.Abstractions.ITestOutputHelper output) => this.output = output;
		[Fact]
		public void SubTextureTest()
		{
			XmlSerializerNamespaces emptyNamespaces = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
			XmlWriterSettings settings = new XmlWriterSettings
			{
				Indent = true,
				OmitXmlDeclaration = true,
				IndentChars = "\t",
			};
			TextureAtlas.SubTexture subTexture = new TextureAtlas.SubTexture
			{
				Name = "pattern_0000.png",
				X = 1024,
				Y = 1024,
				Width = 512,
				Height = 512,
			};
			StringBuilder stringBuilder = new StringBuilder();
			XmlWriter xmlWriter = XmlWriter.Create(stringBuilder, settings);
			XmlSerializer xmlSerializer = new XmlSerializer(typeof(TextureAtlas.SubTexture));
			xmlSerializer.Serialize(xmlWriter, subTexture, emptyNamespaces);
			string xml = stringBuilder.ToString();
			Assert.Equal(
				expected: "<SubTexture name=\"pattern_0000.png\" x=\"1024\" y=\"1024\" width=\"512\" height=\"512\" />",
				actual: XDocument.Parse(xml).Root.ToString());
			TextureAtlas.SubTexture subTexture2 = (TextureAtlas.SubTexture)xmlSerializer.Deserialize(new StringReader(xml));
			Assert.Equal(
				expected: subTexture.Name,
				actual: subTexture2.Name);
			Assert.Equal(
				expected: subTexture.X,
				actual: subTexture2.X);
			Assert.Equal(
				expected: subTexture.Y,
				actual: subTexture2.Y);
			Assert.Equal(
				expected: subTexture.Width,
				actual: subTexture2.Width);
			Assert.Equal(
				expected: subTexture.Height,
				actual: subTexture2.Height);
		}
	}
}
