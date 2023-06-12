using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Voxel2Pixel.Pack;
using Xunit;
using static Voxel2Pixel.Pack.TextureAtlas;

namespace Voxel2PixelTest.Pack
{
	public class TextureAtlasTest
	{
		//private readonly Xunit.Abstractions.ITestOutputHelper output;
		//public TextureAtlasTest(Xunit.Abstractions.ITestOutputHelper output) => this.output = output;
		[Fact]
		public void Test()
		{
			XmlSerializerNamespaces emptyNamespaces = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
			XmlWriterSettings settings = new XmlWriterSettings
			{
				Indent = true,
				OmitXmlDeclaration = true,
				IndentChars = "\t",
			};
			TextureAtlas textureAtlas = new TextureAtlas
			{
				ImagePath = "thin_double.png",
				SubTextures = new SubTexture[]
				{
					new SubTexture
					{
						Name = "pattern_0000.png",
						X = 1024,
						Y = 1024,
						Width = 512,
						Height = 512,
					},
					new SubTexture
					{
						Name = "pattern_0001.png",
						X = 2048,
						Y = 2048,
						Width = 512,
						Height = 512,
					},
				},
			};
			StringBuilder stringBuilder = new StringBuilder();
			XmlWriter xmlWriter = XmlWriter.Create(stringBuilder, settings);
			XmlSerializer xmlSerializer = new XmlSerializer(typeof(TextureAtlas));
			xmlSerializer.Serialize(xmlWriter, textureAtlas, emptyNamespaces);
			Assert.Equal(
				expected: @"<TextureAtlas imagePath=""thin_double.png"">
	<SubTexture name=""pattern_0000.png"" x=""1024"" y=""1024"" width=""512"" height=""512"" />
	<SubTexture name=""pattern_0001.png"" x=""2048"" y=""2048"" width=""512"" height=""512"" />
</TextureAtlas>",
				actual: stringBuilder.ToString());
		}
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
			SubTexture subTexture = new SubTexture
			{
				Name = "pattern_0000.png",
				X = 1024,
				Y = 1024,
				Width = 512,
				Height = 512,
			};
			StringBuilder stringBuilder = new StringBuilder();
			XmlWriter xmlWriter = XmlWriter.Create(stringBuilder, settings);
			XmlSerializer xmlSerializer = new XmlSerializer(typeof(SubTexture));
			xmlSerializer.Serialize(xmlWriter, subTexture, emptyNamespaces);
			string xml = stringBuilder.ToString();
			Assert.Equal(
				expected: "<SubTexture name=\"pattern_0000.png\" x=\"1024\" y=\"1024\" width=\"512\" height=\"512\" />",
				actual: xml);
			SubTexture subTexture2 = (SubTexture)xmlSerializer.Deserialize(new StringReader(xml));
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
