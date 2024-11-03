using BenVoxel;
using SixLabors.ImageSharp;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Voxel2Pixel.Color;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model.FileFormats;
using Voxel2Pixel.Render;
using static Voxel2Pixel.Render.TextureAtlas;
using static Voxel2Pixel.Web.ImageMaker;
using static BenVoxel.ExtensionMethods;

namespace Voxel2Pixel.Test.Render;

public class TextureAtlasTest
{
	//private readonly Xunit.Abstractions.ITestOutputHelper output;
	//public TextureAtlasTest(Xunit.Abstractions.ITestOutputHelper output) => this.output = output;
	public const string TestData = """
<?xml version="1.0" encoding="utf-8"?>
<TextureAtlas imagePath="thin_double.png">
	<SubTexture name="pattern_0000.png" x="1024" y="1024" width="512" height="512" />
	<SubTexture name="pattern_0001.png" x="2048" y="2048" width="512" height="512" />
</TextureAtlas>
""";
	//[Fact]
	//public void Test() => Assert.Equal(
	//	expected: TestData,
	//	actual: new TextureAtlas()
	//	{
	//		ImagePath = "thin_double.png",
	//		SubTextures = [
	//			new() {
	//				Name = "pattern_0000.png",
	//				X = 1024,
	//				Y = 1024,
	//				Width = 512,
	//				Height = 512,
	//			},
	//			new() {
	//				Name = "pattern_0001.png",
	//				X = 2048,
	//				Y = 2048,
	//				Width = 512,
	//				Height = 512,
	//			},
	//		],
	//	}.Utf8Xml());
	//[Fact]
	//public void Test2() => Assert.Equal(
	//	expected: TestData,
	//	actual: ((TextureAtlas)(new XmlSerializer(typeof(TextureAtlas)).Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(TestData)))
	//	?? throw new NullReferenceException())).Utf8Xml());
	[Fact]
	public void SubTextureTest()
	{
		XmlSerializerNamespaces emptyNamespaces = new([XmlQualifiedName.Empty]);
		XmlWriterSettings settings = new()
		{
			Indent = true,
			OmitXmlDeclaration = true,
			IndentChars = "\t",
		};
		SubTexture subTexture = new()
		{
			Name = "pattern_0000.png",
			X = 1024,
			Y = 1024,
			Width = 512,
			Height = 512,
		};
		StringBuilder stringBuilder = new();
		XmlWriter xmlWriter = XmlWriter.Create(stringBuilder, settings);
		XmlSerializer xmlSerializer = new(typeof(SubTexture));
		xmlSerializer.Serialize(xmlWriter, subTexture, emptyNamespaces);
		string xml = stringBuilder.ToString();
		Assert.Equal(
			expected: "<SubTexture name=\"pattern_0000.png\" x=\"1024\" y=\"1024\" width=\"512\" height=\"512\" />",
			actual: xml);
		SubTexture? subTexture2 = xmlSerializer.Deserialize(new StringReader(xml)) as SubTexture;
		Assert.Equal(
			expected: subTexture.Name,
			actual: subTexture2?.Name);
		Assert.Equal(
			expected: subTexture.X,
			actual: subTexture2?.X);
		Assert.Equal(
			expected: subTexture.Y,
			actual: subTexture2?.Y);
		Assert.Equal(
			expected: subTexture.Width,
			actual: subTexture2?.Width);
		Assert.Equal(
			expected: subTexture.Height,
			actual: subTexture2?.Height);
	}
	[Fact]
	public void Iso8()
	{
		VoxFileModel model = new(@"..\..\..\TestData\Models\Husk_64.vox");
		IVoxelColor voxelColor = new NaiveDimmer(model.Palette);
		Sprite atlas = new SpriteMaker
		{
			Model = model,
			VoxelColor = voxelColor,
			Outline = true,
			Shadow = true,
		}.Iso8TextureAtlas(out TextureAtlas textureAtlas, "Sora");
		textureAtlas.ImagePath = "TextureAtlasIso8.png";
		atlas.Png().SaveAsPng(textureAtlas.ImagePath);
		StringBuilder stringBuilder = new();
		new XmlSerializer(typeof(TextureAtlas))
			.Serialize(XmlWriter.Create(stringBuilder, new XmlWriterSettings()
			{
				Indent = true,
				IndentChars = "\t",
			}), textureAtlas);
		File.WriteAllText(
			path: Path.GetFileNameWithoutExtension(textureAtlas.ImagePath) + ".xml",
			contents: stringBuilder.ToString());
	}
	[Fact]
	public void Stacks()
	{
		VoxFileModel model = new(@"..\..\..\TestData\Models\Husk_64.vox");
		IVoxelColor voxelColor = new NaiveDimmer(model.Palette);
		Sprite atlas = new SpriteMaker
		{
			Model = model,
			VoxelColor = voxelColor,
			Outline = true,
			Shadow = true,
		}.StacksTextureAtlas(out TextureAtlas textureAtlas, "Sora");
		textureAtlas.ImagePath = "TextureAtlasStacks.png";
		atlas.Png().SaveAsPng(textureAtlas.ImagePath);
		StringBuilder stringBuilder = new();
		new XmlSerializer(typeof(TextureAtlas))
			.Serialize(XmlWriter.Create(stringBuilder, new XmlWriterSettings()
			{
				Indent = true,
				IndentChars = "\t",
			}), textureAtlas);
		File.WriteAllText(
			path: Path.GetFileNameWithoutExtension(textureAtlas.ImagePath) + ".xml",
			contents: stringBuilder.ToString());
	}
}
