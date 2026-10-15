using Voxel2Pixel.Draw;
namespace Voxel2Pixel.Test.Draw;

public class DrawRectangleTest
{
	/// <summary>
	/// The definition, in long math so that it can't overflow: the rectangle is every pixel from (x, y) across rectWidth and down rectHeight, and its outline is the pixels on its four border lines.
	/// A height below 1 means as tall as it is wide, and a rectangle with no area draws nothing.
	/// </summary>
	static bool[,] Reference(int w, int h, long x, long y, long rectWidth, long rectHeight, bool fill)
	{
		bool[,] set = new bool[w, h];
		if (rectHeight < 1) rectHeight = rectWidth;
		if (rectWidth < 1 || rectHeight < 1) return set;
		for (int py = 0; py < h; py++)
			for (int px = 0; px < w; px++)
			{
				bool inside = px >= x && px < x + rectWidth && py >= y && py < y + rectHeight;
				bool border = px == x || px == x + rectWidth - 1 || py == y || py == y + rectHeight - 1;
				set[px, py] = inside && (fill || border);
			}
		return set;
	}
	static bool[,] Get(byte[] t, int w, int h)
	{
		bool[,] s = new bool[w, h];
		for (int y = 0; y < h; y++)
			for (int x = 0; x < w; x++)
				s[x, y] = BitConverter.ToUInt32(t, (y * w + x) * 4) != 0;
		return s;
	}
	static void Same(bool[,] a, bool[,] b, string message)
	{
		for (int x = 0; x < a.GetLength(0); x++)
			for (int y = 0; y < a.GetLength(1); y++)
				Assert.True(a[x, y] == b[x, y], $"{message} differs at {x},{y}");
	}
	[Fact]
	public void MatchesReference()
	{
		Random random = new(12);
		for (int i = 0; i < 30000; i++)
		{
			int w = random.Next(1, 26), h = random.Next(1, 26), x = random.Next(-30, 50), y = random.Next(-30, 50), rectWidth = random.Next(-3, 41), rectHeight = random.Next(-3, 41);
			bool fill = random.Next(2) == 0;
			byte[] texture = new byte[w * h * 4].DrawRectangle(0xFFFFFFFFu, x, y, rectWidth, rectHeight, fill, (ushort)w);
			Same(Get(texture, w, h), Reference(w, h, x, y, rectWidth, rectHeight, fill), $"rectangle {x},{y} {rectWidth}x{rectHeight} fill={fill} on {w}x{h}");
		}
	}
	[Fact]
	public void ExtremeValues()
	{
		int[] values = [int.MinValue, int.MinValue + 1, -70000, -2, -1, 0, 1, 2, 3, 4, 7, 70000, int.MaxValue - 1, int.MaxValue];
		foreach (int x in values)
			foreach (int y in values)
				foreach (int rectWidth in values)
					foreach (int rectHeight in values)
						foreach (bool fill in new[] { false, true })
						{
							int w = 9, h = 7;
							byte[] texture = new byte[w * h * 4].DrawRectangle(0xFFFFFFFFu, x, y, rectWidth, rectHeight, fill, (ushort)w);
							Same(Get(texture, w, h), Reference(w, h, x, y, rectWidth, rectHeight, fill), $"extreme rectangle {x},{y} {rectWidth}x{rectHeight} fill={fill}");
						}
	}
	[Fact]
	public void OutlineIsTheDefault_AndLeavesTheInsideAlone()
	{
		byte[] byDefault = new byte[10 * 10 * 4].DrawRectangle(0xFFFFFFFFu, 2, 3, 5, 4, width: 10),
			outlined = new byte[10 * 10 * 4].DrawRectangle(0xFFFFFFFFu, 2, 3, 5, 4, false, 10),
			filled = new byte[10 * 10 * 4].DrawRectangle(0xFFFFFFFFu, 2, 3, 5, 4, true, 10);
		Assert.True(byDefault.AsSpan().SequenceEqual(outlined));
		Assert.False(byDefault.AsSpan().SequenceEqual(filled));
		bool[,] outline = Get(outlined, 10, 10);
		int count = 0;
		for (int x = 0; x < 10; x++)
			for (int y = 0; y < 10; y++)
				if (outline[x, y]) count++;
		Assert.Equal(2 * 5 + 2 * 2, count); // Top and bottom rows of 5 and two more pixels on each side
		Assert.False(outline[3, 4] || outline[4, 4] || outline[5, 4] || outline[3, 5] || outline[4, 5] || outline[5, 5]);
		// Rectangles only 1 or 2 pixels thick are all outline, so both ways draw the same thing
		foreach ((int rectWidth, int rectHeight) in new[] { (6, 1), (6, 2), (1, 6), (2, 6), (2, 2) })
			Assert.True(
				new byte[10 * 10 * 4].DrawRectangle(0xFFFFFFFFu, 1, 1, rectWidth, rectHeight, false, 10).AsSpan()
				.SequenceEqual(new byte[10 * 10 * 4].DrawRectangle(0xFFFFFFFFu, 1, 1, rectWidth, rectHeight, true, 10)),
				$"{rectWidth}x{rectHeight}");
	}
	[Fact]
	public void ModifiesOriginal_SpanAndArray()
	{
		foreach (bool fill in new[] { false, true })
		{
			byte[] array = new byte[16 * 16 * 4];
			array.DrawRectangle(0xFFFFFFFFu, 2, 2, 9, 7, fill, 16);
			Assert.Contains(array, b => b != 0);
			byte[] other = new byte[16 * 16 * 4];
			Span<byte> span = other;
			span.DrawRectangle(0xFFFFFFFFu, 2, 2, 9, 7, fill, 16);
			Assert.True(array.AsSpan().SequenceEqual(other));
		}
	}
}
