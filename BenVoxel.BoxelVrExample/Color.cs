using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace BenVoxel.BoxelVrExample;

[Serializable]
public readonly record struct Color(float Red, float Green, float Blue, float Alpha = 1f)
{
	[JsonPropertyName("r")]
	public float Red { get; } = Red;
	[JsonPropertyName("g")]
	public float Green { get; } = Green;
	[JsonPropertyName("b")]
	public float Blue { get; } = Blue;
	[JsonPropertyName("a")]
	public float Alpha { get; } = Alpha;
	public Color(uint value) : this(
		Red: (byte)(value >> 24) / 255f,
		Green: (byte)(value >> 16) / 255f,
		Blue: (byte)(value >> 8) / 255f,
		Alpha: (byte)value / 255f)
	{ }
	public Color(string value) : this(uint.Parse(value[1..], System.Globalization.NumberStyles.HexNumber)) { }
	public static byte Byte(float value) => (byte)Math.Round(Math.Min(Math.Max(value, 0f), 1f) * 255f);
	public static uint Uint(float Red, float Green, float Blue, float Alpha = 1f) => ((uint)Byte(Red) << 24) | ((uint)Byte(Green) << 16) | ((uint)Byte(Blue) << 8) | Byte(Alpha);
	public uint Uint() => Uint(Red, Green, Blue, Alpha);
	public static uint[] GetPalette(IEnumerable<Color> colors) => [.. GetDistinctColors(colors).Take(255).Prepend(0u)];
	public static IEnumerable<uint> GetDistinctColors(IEnumerable<Color> colors)
	{
		HashSet<uint> palette = [0u];
		foreach (uint color in colors.Select(color => color.Uint()))
			if (palette.Add(color))
				yield return color;
	}
	public readonly static Color Magenta = new(0xFF00FFFFu),
		Cyan = new(0x00FFFFFFu);
	public override string ToString() => $"#{Uint():X8}";
	public override int GetHashCode() => (int)Uint();
	public bool Equals(Color other) => Uint() == other.Uint();
}
