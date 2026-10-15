using Voxel2Pixel.Draw;
namespace Voxel2Pixel.Test.Draw;

public class DrawCircleTest
{
	/// <summary>
	/// Independent reference built straight from the definition instead of the incremental midpoint loop.
	/// The outline in the octant from straight up to the diagonal is (a, round(sqrt(r^2 - a^2))), mirrored eight ways.
	/// The fill covers, on each row, everything up to the outline's widest pixel on that row.
	/// </summary>
	static bool[,] Reference(int w, int h, long cx, long cy, int r, bool fill)
	{
		bool[,] set = new bool[w, h];
		int[] rowMax = new int[r + 1]; // Widest outline pixel on the row |dy|
		List<(int, int)> offsets = [];
		for (int a = 0; ; a++)
		{
			int b = (int)Math.Round(Math.Sqrt((double)r * r - (double)a * a));
			if (a > b) break;
			foreach ((int dx, int dy) in new[] { (a, b), (b, a) })
			{
				offsets.Add((dx, dy));
				rowMax[dy] = Math.Max(rowMax[dy], dx);
			}
		}
		if (fill)
		{
			for (int y = 0; y < h; y++)
			{
				long dy = Math.Abs(y - cy);
				if (dy > r) continue;
				long from = Math.Max(0L, cx - rowMax[dy]), to = Math.Min(w - 1L, cx + rowMax[dy]);
				for (long x = from; x <= to; x++) set[x, y] = true;
			}
		}
		else
			foreach ((int dx, int dy) in offsets)
				foreach (int sx in new[] { -1, 1 })
					foreach (int sy in new[] { -1, 1 })
					{
						long x = cx + sx * dx, y = cy + sy * dy;
						if (x >= 0 && y >= 0 && x < w && y < h) set[x, y] = true;
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
		Random random = new(7);
		for (int i = 0; i < 20000; i++)
		{
			int w = random.Next(1, 21), h = random.Next(1, 21), cx = random.Next(-40, 60), cy = random.Next(-40, 60), r = random.Next(0, 41);
			bool fill = random.Next(2) == 0;
			byte[] texture = new byte[w * h * 4].DrawCircle(cx, cy, (ushort)r, fill, 0xFFFFFFFFu, (ushort)w);
			Same(Get(texture, w, h), Reference(w, h, cx, cy, r, fill), $"circle {cx},{cy} r{r} fill={fill} {w}x{h}");
		}
	}
	[Fact]
	public void HugeRadiusAndExtremeCenters()
	{
		Random random = new(9);
		int[] centers = [int.MinValue, int.MinValue + 1, -70000, -1, 0, 9, 70000, int.MaxValue - 1, int.MaxValue];
		System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
		foreach (int cx in centers)
			foreach (int cy in centers)
				foreach (int r in new[] { 0, 1, 40000, 65535 })
					foreach (bool fill in new[] { false, true })
					{
						int w = 13, h = 9;
						byte[] texture = new byte[w * h * 4].DrawCircle(cx, cy, (ushort)r, fill, 0xFFFFFFFFu, (ushort)w);
						Same(Get(texture, w, h), Reference(w, h, cx, cy, r, fill), $"extreme {cx},{cy} r{r} fill={fill}");
					}
		for (int i = 0; i < 200; i++)
		{
			int w = random.Next(1, 40), h = random.Next(1, 40), r = random.Next(1000, 65536), cx = random.Next(-80000, 80000), cy = random.Next(-80000, 80000);
			bool fill = random.Next(2) == 0;
			byte[] texture = new byte[w * h * 4].DrawCircle(cx, cy, (ushort)r, fill, 0xFFFFFFFFu, (ushort)w);
			Same(Get(texture, w, h), Reference(w, h, cx, cy, r, fill), $"huge {cx},{cy} r{r} fill={fill} {w}x{h}");
		}
		Assert.True(stopwatch.ElapsedMilliseconds < 20000, "slow: " + stopwatch.ElapsedMilliseconds);
	}
	[Fact]
	public void Properties()
	{
		// Radius 0 is one pixel, either way
		foreach (bool fill in new[] { false, true })
		{
			bool[,] one = Get(new byte[5 * 5 * 4].DrawCircle(2, 3, 0, fill, 0xFFFFFFFFu, 5), 5, 5);
			int count = 0;
			for (int x = 0; x < 5; x++)
				for (int y = 0; y < 5; y++)
					if (one[x, y]) { count++; Assert.True(x == 2 && y == 3); }
			Assert.Equal(1, count);
		}
		for (int r = 1; r <= 60; r++)
		{
			int n = r * 2 + 3, c = r + 1;
			bool[,] outline = Get(new byte[n * n * 4].DrawCircle(c, c, (ushort)r, false, 0xFFFFFFFFu, (ushort)n), n, n),
				filled = Get(new byte[n * n * 4].DrawCircle(c, c, (ushort)r, true, 0xFFFFFFFFu, (ushort)n), n, n);
			int area = 0;
			for (int x = 0; x < n; x++)
				for (int y = 0; y < n; y++)
				{
					if (outline[x, y]) Assert.True(filled[x, y], "fill includes outline");
					Assert.Equal(outline[x, y], outline[2 * c - x, y]); // mirror left-right
					Assert.Equal(outline[x, y], outline[x, 2 * c - y]); // mirror top-bottom
					Assert.Equal(outline[x, y], outline[y, x]); // mirror across the diagonal
					Assert.Equal(filled[x, y], filled[y, x]);
					if (filled[x, y]) area++;
				}
			Assert.True(outline[c + r, c] && outline[c - r, c] && outline[c, c + r] && outline[c, c - r], "extreme points are at distance radius");
			Assert.False(outline[c, c] && r > 0, "center is not on the outline");
			if (r >= 5) Assert.InRange(area, Math.PI * (r + 0.5) * (r + 0.5) * 0.95, Math.PI * (r + 0.5) * (r + 0.5) * 1.05); // (2r+1) pixels across
		}
		// A texture completely inside the circle is completely filled, and with only an outline it is untouched
		byte[] inside = new byte[16 * 16 * 4].DrawCircle(8, 8, 65535, true, 0xFFFFFFFFu, 16);
		Assert.DoesNotContain(inside.Chunk(4), pixel => pixel.All(b => b == 0));
		Assert.DoesNotContain(new byte[16 * 16 * 4].DrawCircle(8, 8, 65535, false, 0xFFFFFFFFu, 16), b => b != 0);
		// Completely missing the texture, and empty textures
		Assert.DoesNotContain(new byte[16 * 16 * 4].DrawCircle(1000, 1000, 10, true, 0xFFFFFFFFu, 16), b => b != 0);
		Assert.Empty(new byte[0].DrawCircle(0, 0, 5));
	}
	[Fact]
	public void ModifiesOriginal_SpanAndArray()
	{
		byte[] array = new byte[16 * 16 * 4];
		array.DrawCircle(8, 8, 5, true, 0xFFFFFFFFu, 16);
		Assert.Contains(array, b => b != 0);
		byte[] other = new byte[16 * 16 * 4];
		Span<byte> span = other;
		span.DrawCircle(8, 8, 5, true, 0xFFFFFFFFu, 16);
		Assert.True(array.AsSpan().SequenceEqual(other));
	}
}
