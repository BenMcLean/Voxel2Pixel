﻿using System;
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
		return output.ToString() + Environment.NewLine;
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
				if (obj.Count == 0)
				{
					output.Append("{}");
					return;
				}
				output.AppendLine("{");
				int propertyIndex = 0;
				foreach (JsonProperty property in JsonSerializer.SerializeToElement(obj).EnumerateObject())
				{
					output.Append(indent + "\t" + JsonSerializer.Serialize(property.Name) + ": ");
					FormatJsonElement(
						element: property.Value,
						output: output,
						depth: (byte)(depth + 1));
					if (++propertyIndex < obj.Count)
						output.AppendLine(",");
					else
						output.AppendLine();
				}
				output.Append(indent + "}");
				break;
			case JsonArray arr:
				if (arr.Count == 0)
				{
					output.Append("[]");
					return;
				}
				output.AppendLine("[");
				int elementIndex = 0;
				foreach (JsonNode element in arr)
				{
					output.Append(indent + "\t");
					FormatJsonNode(
						node: element,
						output: output,
						depth: (byte)(depth + 1));
					if (++elementIndex < arr.Count)
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
	/// Writes a RIFF chunk with content provided by a delegate
	/// </summary>
	/// <param name="writer">The BinaryWriter to write to</param>
	/// <param name="fourCC">4-character ASCII identifier for the chunk</param>
	/// <param name="writeContent">Action that writes the chunk content</param>
	/// <returns>The writer for method chaining</returns>
	public static BinaryWriter RIFF(this BinaryWriter writer, string fourCC, Action<BinaryWriter> writeContent)
	{
		if (fourCC.Length != 4)
			throw new ArgumentException("RIFF chunk identifier must be exactly 4 characters", nameof(fourCC));
		using MemoryStream contentStream = new();
		using (BinaryWriter contentWriter = new(contentStream, Encoding.UTF8, leaveOpen: true))
			writeContent(contentWriter);
		byte[] content = contentStream.ToArray();
		writer.Write(Encoding.ASCII.GetBytes(fourCC), 0, 4);
		writer.Write((uint)content.Length);
		writer.Write(content);
		writer.Flush();
		return writer;
	}
	/// <summary>
	/// Writes an IBinaryWritable object as a RIFF chunk
	/// </summary>
	/// <param name="writer">The BinaryWriter to write to</param>
	/// <param name="fourCC">4-character ASCII identifier for the chunk</param> 
	/// <param name="writable">The object to write as chunk content</param>
	/// <returns>The writer for method chaining</returns>
	public static BinaryWriter RIFF(this IBinaryWritable writable, string fourCC, BinaryWriter writer) => writer.RIFF(fourCC, writable.Write);
	/// <summary>
	/// Reads a RIFF chunk header and provides a reader limited to the chunk's content
	/// </summary>
	/// <param name="reader">The source reader</param>
	/// <param name="readContent">Delegate that receives a length-limited reader for the chunk content</param>
	/// <returns>The FourCC identifier of the chunk that was read</returns>
	public static string RIFF(this BinaryReader reader, Action<BinaryReader, string> readContent)
	{
		string fourCC = Encoding.ASCII.GetString(reader.ReadBytes(4));
		uint length = reader.ReadUInt32();
		using MemoryStream limitedStream = new(reader.ReadBytes((int)length));
		using BinaryReader limitedReader = new(limitedStream, Encoding.UTF8, leaveOpen: true);
		readContent(limitedReader, fourCC);
		return fourCC;
	}
	public static bool TryRIFF(this BinaryReader reader, Func<BinaryReader, string, bool> readContent)
	{
		long startPosition = reader.BaseStream.Position;
		string fourCC = Encoding.ASCII.GetString(reader.ReadBytes(4));
		uint length = reader.ReadUInt32();
		using MemoryStream limitedStream = new(reader.ReadBytes((int)length));
		using BinaryReader limitedReader = new(limitedStream, Encoding.UTF8, leaveOpen: true);
		if (readContent(limitedReader, fourCC))
			return true;
		reader.BaseStream.Position = startPosition;
		return false;
	}
	#endregion IBinaryWritable
}
