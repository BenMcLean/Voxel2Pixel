﻿using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BenVoxel.Test;

public class Test
{
	const string SourceFile = @"..\..\..\TestData\Models\sora.ben.json";
	[Fact]
	public void Json2Binary2Json()
	{
		JsonObject sourceJson;
		using (FileStream jsonInputStream = new(
			path: SourceFile,
			mode: FileMode.Open,
			access: FileAccess.Read))
		{
			sourceJson = JsonSerializer.Deserialize<JsonObject>(jsonInputStream)
				?? throw new NullReferenceException();
		}
		BenVoxelFile model = new(sourceJson);
		using (FileStream binaryOutputStream = new(
			path: "test.ben",
			mode: FileMode.OpenOrCreate,
			access: FileAccess.Write))
		{
			model.Write(binaryOutputStream);
		}
		using (FileStream binaryInputStream = new(
			path: "test.ben",
			mode: FileMode.Open,
			access: FileAccess.Read))
		{
			model = new(binaryInputStream);
		}
		using (FileStream jsonOutputStream = new(
			path: "test.ben.json",
			mode: FileMode.OpenOrCreate,
			access: FileAccess.Write))
		{
			jsonOutputStream.Write(Encoding.UTF8.GetBytes(model.ToJson().Tabs()));
		}
		Assert.Equal(
			expected: NormalizeJson(sourceJson).ToJsonString(),
			actual: NormalizeJson(model.ToJson()).ToJsonString());
		File.Delete("test.ben");
		File.Delete("test.ben.json");
	}
	/// <summary>
	/// Stupidly, there is no way to make System.IO.Compression deterministic, so we have to remove the compression in order to test
	/// </summary>
	private static JsonObject NormalizeJson(JsonObject json)
	{
		JsonObject normalized = JsonSerializer.Deserialize<JsonObject>(json.ToJsonString())
			?? throw new NullReferenceException();
		if (normalized.TryGetPropertyValue("models", out JsonNode? modelsNode))
		{
			foreach (KeyValuePair<string, JsonNode?> model in modelsNode?.AsObject()
				?? throw new NullReferenceException())
			{
				if (model.Value is JsonObject modelObj &&
					modelObj.TryGetPropertyValue("geometry", out JsonNode? geometryNode) &&
					geometryNode is JsonObject geometry)
				{
					ushort[] size = JsonSerializer.Deserialize<ushort[]>(geometry["size"])
						?? throw new NullReferenceException();
					string z85 = geometry["z85"]?.GetValue<string>()
						?? throw new NullReferenceException();
					SvoModel svoModel = new(
						z85: z85,
						sizeX: size[0],
						sizeY: size[1],
						sizeZ: size[2]);
					geometry["z85"] = JsonValue.Create(svoModel.Z85());
				}
			}
		}
		return normalized;
	}
}
