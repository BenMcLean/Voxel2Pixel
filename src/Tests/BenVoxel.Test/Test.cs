using System.Text.Json;
using System.Text.Json.Nodes;
using BenVoxel.Models;

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
		BenVoxelFile benVoxelFile = JsonSerializer.Deserialize<BenVoxelFile>(sourceJson)
			?? throw new NullReferenceException();
		benVoxelFile.Save("test.ben");
		BenVoxelFile.Load("test.ben")
			.Save("test.ben.json");
		using (FileStream jsonInputStream = new(
			path: "test.ben.json",
			mode: FileMode.Open,
			access: FileAccess.Read))
		{
			Assert.Equal(
				expected: NormalizeJson(sourceJson).ToJsonString(),
				actual: NormalizeJson(JsonSerializer.Deserialize<JsonObject>(jsonInputStream)
					?? throw new NullReferenceException()).ToJsonString());
		}
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
			foreach (KeyValuePair<string, JsonNode?> model in modelsNode?.AsObject()
				?? throw new NullReferenceException())
				if (model.Value is JsonObject modelObj &&
					modelObj.TryGetPropertyValue("geometry", out JsonNode? geometryNode) &&
					geometryNode is JsonObject geometry)
				{
					ushort[] size = JsonSerializer.Deserialize<ushort[]>(geometry["size"])
						?? throw new NullReferenceException();
					geometry["z85"] = JsonValue.Create(Cromulent.Encoding.Z85.ToZ85String(
						inArray: new SvoModel(
								z85: geometry["z85"]?.GetValue<string>()
									?? throw new NullReferenceException(),
								sizeX: size[0],
								sizeY: size[1],
								sizeZ: size[2])
							.Bytes(includeSizes: false),
						autoPad: true));
				}
		return normalized;
	}
	[Fact]
	public void OutputSvo()
	{
		using FileStream binaryOutputStream = new(
			path: "SORA.SVO",
			mode: FileMode.OpenOrCreate,
			access: FileAccess.Write);
		BenVoxelFile benVoxelFile = BenVoxelFile.Load(SourceFile);
		benVoxelFile.Models[""].SvoModel
			.Write(
				stream: binaryOutputStream,
				includeSizes: true);
	}
	[Fact]
	public void TryTryAgain()
	{
		SvoModel svoModel = new(2, 2, 2);
		svoModel[0, 0, 0] = 1;
		svoModel[1, 1, 1] = 2;
		File.WriteAllText(
			path: "whatever.json",
			contents: JsonSerializer.Serialize(svoModel, BenVoxelFile.JsonSerializerOptions));
		SvoModel svoModel2 = JsonSerializer.Deserialize<SvoModel>(File.ReadAllText("whatever.json"))
			?? throw new NullReferenceException();
		for (ushort x = 0; x < svoModel.SizeX; x++)
			for (ushort y = 0; y < svoModel.SizeY; y++)
				for (ushort z = 0; z < svoModel.SizeZ; z++)
					Assert.Equal(
						expected: svoModel[x, y, z],
						actual: svoModel2[x, y, z]);
	}
}
