using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BenVoxel.Test;

public class Test
{
	const string SourceFile = @"..\..\..\TestData\Models\sora.ben.json";
	[Fact]
	public void Json2Binary2Json()
	{
		BenVoxelFile model;
		using (FileStream jsonInputStream = new(
			path: SourceFile,
			mode: FileMode.Open,
			access: FileAccess.Read))
		{
			model = new(JsonSerializer.Deserialize<JsonObject>(jsonInputStream));
		}
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
			expected: File.ReadAllText(SourceFile).Replace("\r\n", "\n"),
			actual: File.ReadAllText("test.ben.json").Replace("\r\n", "\n"));
		File.Delete("test.ben");
		File.Delete("test.ben.json");
	}
}
