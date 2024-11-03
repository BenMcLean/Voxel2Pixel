using System;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

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
	#region JSON
	/// <summary>
	/// I hate how Microsoft tried to force spaces over tabs
	/// </summary>
	public static string Tabs(this JsonObject jsonObject, byte depth = 0)
	{
		if (jsonObject is null)
			throw new ArgumentNullException(nameof(jsonObject));
		StringBuilder output = new();
		FormatJsonNode(
			node: jsonObject,
			output: output,
			depth: depth);
		return output.ToString();
	}
	/// <summary>
	/// I hate how Microsoft tried to force spaces over tabs
	/// </summary>
	public static string TabsJson(this string jsonString, byte depth = 0)
	{
		if (string.IsNullOrEmpty(jsonString))
			throw new ArgumentException("JSON string cannot be null or empty", nameof(jsonString));
		JsonNode jsonNode = JsonNode.Parse(jsonString, new()
		{
			PropertyNameCaseInsensitive = false,
		});
		if (jsonNode is not JsonObject jsonObject)
			throw new JsonException("Input JSON must be an object");
		return jsonObject.Tabs(depth);
	}
	private static readonly JsonSerializerOptions MinimalEscapingOptions = new()
	{
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
		WriteIndented = false,
	};
	private static void FormatJsonElement(JsonElement element, StringBuilder output, byte depth = 0)
	{
		string indent = new('\t', depth);
		switch (element.ValueKind)
		{
			case JsonValueKind.Object:
				output.AppendLine("{");
				JsonProperty[] properties = [.. element.EnumerateObject()];
				for (int i = 0; i < properties.Length; i++)
				{
					output.Append(indent + "\t" + JsonSerializer.Serialize(properties[i].Name) + ": ");
					FormatJsonElement(
						element: properties[i].Value,
						output: output,
						depth: (byte)(depth + 1));
					if (i < properties.Length - 1)
						output.AppendLine(",");
					else
						output.AppendLine();
				}
				output.Append(indent + "}");
				break;
			case JsonValueKind.Array:
				output.AppendLine("[");
				JsonElement[] elements = [.. element.EnumerateArray()];
				for (int i = 0; i < elements.Length; i++)
				{
					output.Append(indent + "\t");
					FormatJsonElement(
						element: elements[i],
						output: output,
						depth: (byte)(depth + 1));
					if (i < elements.Length - 1)
						output.AppendLine(",");
					else
						output.AppendLine();
				}
				output.Append(indent + "]");
				break;
			case JsonValueKind.String:
				output.Append(JsonSerializer.Serialize(element.GetString(), MinimalEscapingOptions));
				break;
			case JsonValueKind.Number:
				if (element.TryGetInt64(out long longValue))
					output.Append(longValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
				else if (element.TryGetDouble(out double doubleValue))
					output.Append(doubleValue.ToString("G17", System.Globalization.CultureInfo.InvariantCulture));
				else
					output.Append(element.GetRawText());
				break;
			case JsonValueKind.True:
			case JsonValueKind.False:
				output.Append(element.GetBoolean().ToString().ToLowerInvariant());
				break;
			case JsonValueKind.Null:
				output.Append("null");
				break;
		}
	}
	private static void FormatJsonNode(JsonNode node, StringBuilder output, byte depth = 0)
	{
		string indent = new('\t', depth);
		switch (node)
		{
			case JsonObject obj:
				output.AppendLine("{");
				int propertyIndex = 0, propertyCount = obj.Count;
				foreach (JsonProperty property in JsonSerializer.SerializeToElement(obj).EnumerateObject())
				{
					output.Append(indent + "\t" + JsonSerializer.Serialize(property.Name) + ": ");
					FormatJsonElement(
						element: property.Value,
						output: output,
						depth: (byte)(depth + 1));
					if (++propertyIndex < propertyCount)
						output.AppendLine(",");
					else
						output.AppendLine();
				}
				output.Append(indent + "}");
				break;
			case JsonArray arr:
				output.AppendLine("[");
				int elementIndex = 0, elementCount = arr.Count;
				foreach (JsonNode element in arr)
				{
					output.Append(indent + "\t");
					FormatJsonNode(
						node: element,
						output: output,
						depth: (byte)(depth + 1));
					if (++elementIndex < elementCount)
						output.AppendLine(",");
					else
						output.AppendLine();
				}
				output.Append(indent + "]");
				break;
			case JsonValue val:
				FormatJsonElement(
					element: JsonSerializer.SerializeToElement(val),
					output: output,
					depth: depth);
				break;
		}
	}
	#endregion JSON
	#region IBinaryWritable
	/// <summary>
	/// Creates a RIFF-formatted chunk from an IBinaryWritable object
	/// </summary>
	public static MemoryStream RIFF(this IBinaryWritable o, string fourCC)
	{
		using MemoryStream contentMemoryStream = new();
		using (BinaryWriter contentWriter = new(contentMemoryStream, Encoding.UTF8, leaveOpen: true))
			o.Write(contentWriter);
		MemoryStream memoryStream = new();
		using (BinaryWriter writer = new(memoryStream, Encoding.UTF8, leaveOpen: true))
		{
			writer.Write(Encoding.UTF8.GetBytes(fourCC[..4]), 0, 4);
			writer.Write((uint)contentMemoryStream.Length);
			contentMemoryStream.Position = 0;
			contentMemoryStream.CopyTo(memoryStream);
		}
		memoryStream.Position = 0;
		return memoryStream;
	}
	public static byte[] ArrayRIFF(this IBinaryWritable o, string fourCC)
	{
		using MemoryStream memoryStream = o.RIFF(fourCC);
		return memoryStream.ToArray();
	}
	public static void CopyRIFF(this IBinaryWritable o, string fourCC, Stream output)
	{
		using MemoryStream memoryStream = o.RIFF(fourCC);
		memoryStream.CopyTo(output);
	}
	/// <summary>
	/// Writes data as a RIFF chunk to a BinaryWriter
	/// </summary>
	public static BinaryWriter RIFF(this BinaryWriter writer, string fourCC, byte[] bytes)
	{
		writer.Write(Encoding.UTF8.GetBytes(fourCC[..4]), 0, 4);
		writer.Write((uint)bytes.Length);
		writer.Write(bytes);
		writer.Flush();
		return writer;
	}
	#endregion IBinaryWritable
}
