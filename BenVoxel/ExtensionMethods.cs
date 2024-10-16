using System.IO;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;

namespace BenVoxel;

public static class ExtensionMethods
{
	#region IModel
	/// <summary>
	/// Checking for being out of bounds can involve fewer comparisons than checking for being in bounds.
	/// </summary>
	/// <param name="coordinates">3D coordinates</param>
	/// <returns>true if coordinates are outside the bounds of the model</returns>
	public static bool IsOutside(this IModel model, params ushort[] coordinates) => coordinates[0] >= model.SizeX || coordinates[1] >= model.SizeY || coordinates[2] >= model.SizeZ;
	public static Point3D Center(this IModel model) => new(model.SizeX >> 1, model.SizeY >> 1, model.SizeZ >> 1);
	public static Point3D BottomCenter(this IModel model) => new(model.SizeX >> 1, model.SizeY >> 1, 0);
	public static byte Set(this IEditableModel model, Voxel voxel) => model[voxel.X, voxel.Y, voxel.Z] = voxel.Index;
	#endregion IModel
	#region XML
	public static readonly XmlSerializerNamespaces EmptyNamespaces = new([XmlQualifiedName.Empty]);
	public static string Utf8Xml<T>(T o, bool indent = true)
	{
		Utf8StringWriter stringWriter = new();
		new XmlSerializer(typeof(T)).Serialize(
			xmlWriter: XmlWriter.Create(
				output: stringWriter,
				settings: new()
				{
					Encoding = Encoding.UTF8,
					Indent = indent,
					IndentChars = "\t",
				}),
			o: o,
			namespaces: EmptyNamespaces);
		return stringWriter.ToString();
	}
	public class Utf8StringWriter : StringWriter
	{
		public override Encoding Encoding => Encoding.UTF8;
	}
	public static T FromXml<T>(this string value) => (T)new XmlSerializer(typeof(T)).Deserialize(new StringReader(value));
	private static readonly XmlWriterSettings ToXElementXmlSettings = new()
	{
		ConformanceLevel = ConformanceLevel.Fragment,
		Encoding = Encoding.UTF8,
		OmitXmlDeclaration = true,
		Indent = false,
	};
	public static XElement ToXElement(this IXmlSerializable xmlSerializable)
	{
		if (xmlSerializable is null)
			return null;
		using Utf8StringWriter stringWriter = new();
		using XmlWriter xmlWriter = XmlWriter.Create(output: stringWriter, settings: ToXElementXmlSettings);
		new XmlSerializer(xmlSerializable.GetType()).Serialize(
			xmlWriter: xmlWriter,
			o: xmlSerializable,
			namespaces: EmptyNamespaces);
		return XElement.Parse(stringWriter.ToString());
	}
	public static IEnumerable<XElement> ToXElements<T>(this IEnumerable<T> xmlSerializables) where T : IXmlSerializable
	{
		if (xmlSerializables is null)
			yield break;
		XmlSerializer xmlSerializer = new(typeof(T));
		foreach (T xmlSerializable in xmlSerializables)
		{
			if (xmlSerializable is null)
				continue;
			using Utf8StringWriter stringWriter = new();
			using XmlWriter xmlWriter = XmlWriter.Create(output: stringWriter, settings: ToXElementXmlSettings);
			xmlSerializer.Serialize(
				xmlWriter: xmlWriter,
				o: xmlSerializable,
				namespaces: EmptyNamespaces);
			yield return XElement.Parse(stringWriter.ToString());
		}
	}
	#endregion XML
	#region IBinaryWritable
	public static MemoryStream RIFF(this IBinaryWritable o, string fourCC)
	{
		MemoryStream ms = new();
		BinaryWriter writer = new(ms);
		writer.Write(Encoding.UTF8.GetBytes(fourCC[..4]), 0, 4);
		writer.BaseStream.Position += 4;
		o.Write(writer);
		if (writer.BaseStream.Position % 2 != 0)
			writer.Write((byte)0);
		uint length = (uint)(writer.BaseStream.Position - 8);
		writer.BaseStream.Position = 4;
		writer.Write(length);
		writer.BaseStream.Position = 0;
		return ms;
	}
	public static BinaryWriter RIFF(this BinaryWriter writer, string fourCC, byte[] bytes)
	{
		writer.Write(Encoding.UTF8.GetBytes(fourCC[..4]), 0, 4);
		writer.Write((uint)bytes.Length);
		writer.Write(bytes);
		return writer;
	}
	#endregion IBinaryWritable
}
