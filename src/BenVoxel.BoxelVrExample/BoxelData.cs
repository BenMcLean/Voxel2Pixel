using System;
using System.Text.Json.Serialization;

namespace BenVoxel.BoxelVrExample;

[Serializable]
public class BoxelData
{
	public Vector3Int intPosition = default;    // {int: x, int y, int z}.
	public Color normalColor = Color.Magenta;   // {float: r, float: g, float: b, float: a}.
	public Color hoverColor = Color.Cyan;       // It's historical. Plan to remove it in the future.
	[Serializable]
	public readonly record struct Vector3Int(int X, int Y, int Z)
	{
		[JsonPropertyName("x")]
		public int X { get; } = X;
		[JsonPropertyName("y")]
		public int Y { get; } = Y;
		[JsonPropertyName("z")]
		public int Z { get; } = Z;
	}
}
