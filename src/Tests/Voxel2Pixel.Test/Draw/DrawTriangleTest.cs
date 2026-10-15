using System.Numerics;
using Voxel2Pixel.Draw;
namespace Voxel2Pixel.Test.Draw;

public class DrawTriangleTest
{
	/// <summary>
	/// Which columns do the pixels of the line from A to B occupy on this row, going by the rule DrawLine uses?
	/// Found by binary searching the exact (big integer) formula instead of solving it, so it doesn't share the closed form that DrawTriangle uses.
	/// </summary>
	static bool RowRange(long xA, long yA, long xB, long yB, long row, out BigInteger left, out BigInteger right)
	{
		left = right = 0;
		if (row < Math.Min(yA, yB) || row > Math.Max(yA, yB)) return false;
		bool steep = BigInteger.Abs(yB - yA) > BigInteger.Abs(xB - xA);
		BigInteger major0 = steep ? yA : xA, major1 = steep ? yB : xB, minor0 = steep ? xA : yA, minor1 = steep ? xB : yB;
		if (major0 > major1)
		{
			(major0, major1) = (major1, major0);
			(minor0, minor1) = (minor1, minor0);
		}
		BigInteger majorLength = major1 - major0, minorLength = BigInteger.Abs(minor1 - minor0);
		int sign = minor1 >= minor0 ? 1 : -1;
		if (majorLength.IsZero)
		{
			left = right = steep ? minor0 : major0;
			return true;
		}
		BigInteger Offset(BigInteger i) => BigInteger.Divide(2 * minorLength * i + majorLength, 2 * majorLength);
		if (steep)
		{
			left = right = minor0 + sign * Offset(row - major0);
			return true;
		}
		BigInteger k = sign * (row - minor0);
		BigInteger low = 0, high = majorLength; // Smallest i with Offset(i) >= k
		while (low < high)
		{
			BigInteger middle = (low + high) / 2;
			if (Offset(middle) >= k) high = middle; else low = middle + 1;
		}
		BigInteger first = low;
		high = majorLength; // Largest i with Offset(i) <= k
		while (low < high)
		{
			BigInteger middle = (low + high + 1) / 2;
			if (Offset(middle) <= k) low = middle; else high = middle - 1;
		}
		left = major0 + first;
		right = major0 + low;
		return true;
	}
	static bool[,] ReferenceFill(int w, int h, long xA, long yA, long xB, long yB, long xC, long yC)
	{
		bool[,] set = new bool[w, h];
		for (int y = 0; y < h; y++)
		{
			BigInteger left = BigInteger.Pow(2, 80), right = -BigInteger.Pow(2, 80);
			foreach ((long x0, long y0, long x1, long y1) in new[] { (xA, yA, xB, yB), (xB, yB, xC, yC), (xC, yC, xA, yA) })
				if (RowRange(x0, y0, x1, y1, y, out BigInteger l, out BigInteger r))
				{
					left = BigInteger.Min(left, l);
					right = BigInteger.Max(right, r);
				}
			for (int x = 0; x < w; x++)
				set[x, y] = x >= left && x <= right;
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
	static void FillIncludesOutline(bool[,] outline, bool[,] filled, string message)
	{
		for (int x = 0; x < outline.GetLength(0); x++)
			for (int y = 0; y < outline.GetLength(1); y++)
				if (outline[x, y]) Assert.True(filled[x, y], $"{message}: fill is missing outline pixel {x},{y}");
	}
	[Fact]
	public void FillMatchesReference()
	{
		Random random = new(21);
		for (int i = 0; i < 20000; i++)
		{
			int w = random.Next(1, 41), h = random.Next(1, 41), m = random.Next(0, 4) == 0 ? 500 : 50;
			int xA = random.Next(-m, m), yA = random.Next(-m, m), xB = random.Next(-m, m), yB = random.Next(-m, m), xC = random.Next(-m, m), yC = random.Next(-m, m);
			bool[,] filled = Get(new byte[w * h * 4].DrawTriangle(xA, yA, xB, yB, xC, yC, true, 0xFFFFFFFFu, (ushort)w), w, h),
				outline = Get(new byte[w * h * 4].DrawTriangle(xA, yA, xB, yB, xC, yC, false, 0xFFFFFFFFu, (ushort)w), w, h);
			string message = $"{xA},{yA} {xB},{yB} {xC},{yC} {w}x{h}";
			Same(filled, ReferenceFill(w, h, xA, yA, xB, yB, xC, yC), "fill " + message);
			FillIncludesOutline(outline, filled, message);
		}
	}
	[Fact]
	public void HugeCoordinates()
	{
		int[] values = [int.MinValue, int.MinValue + 1, -1_000_000_007, -70000, -1, 0, 5, 70000, 1_000_000_007, int.MaxValue - 1, int.MaxValue];
		Random random = new(4);
		System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
		for (int i = 0; i < 6000; i++)
		{
			int w = random.Next(1, 20), h = random.Next(1, 20);
			int[] p = new int[6];
			for (int j = 0; j < 6; j++) p[j] = random.Next(3) == 0 ? values[random.Next(values.Length)] : random.Next(-100000, 100000);
			bool[,] filled = Get(new byte[w * h * 4].DrawTriangle(p[0], p[1], p[2], p[3], p[4], p[5], true, 0xFFFFFFFFu, (ushort)w), w, h),
				outline = Get(new byte[w * h * 4].DrawTriangle(p[0], p[1], p[2], p[3], p[4], p[5], false, 0xFFFFFFFFu, (ushort)w), w, h);
			string message = string.Join(",", p) + $" {w}x{h}";
			Same(filled, ReferenceFill(w, h, p[0], p[1], p[2], p[3], p[4], p[5]), "fill " + message);
			FillIncludesOutline(outline, filled, message);
		}
		Assert.True(stopwatch.ElapsedMilliseconds < 60000, "slow: " + stopwatch.ElapsedMilliseconds);
	}
	[Fact]
	public void InsideTheTexture_FillIsEverythingBetweenTheOutlinesLeftAndRightOnEachRow()
	{
		Random random = new(33);
		int n = 40;
		for (int i = 0; i < 5000; i++)
		{
			int[] p = [.. Enumerable.Range(0, 6).Select(_ => random.Next(0, n))];
			bool[,] filled = Get(new byte[n * n * 4].DrawTriangle(p[0], p[1], p[2], p[3], p[4], p[5], true, 0xFFFFFFFFu, (ushort)n), n, n),
				outline = Get(new byte[n * n * 4].DrawTriangle(p[0], p[1], p[2], p[3], p[4], p[5], false, 0xFFFFFFFFu, (ushort)n), n, n);
			for (int y = 0; y < n; y++)
			{
				int left = Enumerable.Range(0, n).Where(x => outline[x, y]).DefaultIfEmpty(-1).First(),
					right = Enumerable.Range(0, n).Where(x => outline[x, y]).DefaultIfEmpty(-1).Last();
				for (int x = 0; x < n; x++)
					Assert.True(filled[x, y] == (left >= 0 && x >= left && x <= right), $"{string.Join(",", p)} at {x},{y}");
			}
		}
	}
	[Fact]
	public void OrderOfThePointsDoesNotMatter()
	{
		Random random = new(2);
		int[][] orders = [[0, 1, 2], [0, 2, 1], [1, 0, 2], [1, 2, 0], [2, 0, 1], [2, 1, 0]];
		for (int i = 0; i < 2000; i++)
		{
			int w = random.Next(1, 30), h = random.Next(1, 30), m = 40;
			(int X, int Y)[] points = [.. Enumerable.Range(0, 3).Select(_ => (random.Next(-m, m), random.Next(-m, m)))];
			foreach (bool fill in new[] { false, true })
			{
				byte[]? first = null;
				foreach (int[] order in orders)
				{
					byte[] texture = new byte[w * h * 4].DrawTriangle(points[order[0]].X, points[order[0]].Y, points[order[1]].X, points[order[1]].Y, points[order[2]].X, points[order[2]].Y, fill, 0xFFFFFFFFu, (ushort)w);
					if (first is null) first = texture;
					else Assert.True(first.AsSpan().SequenceEqual(texture), $"fill={fill} {string.Join(" ", points)}");
				}
			}
		}
	}
	[Fact]
	public void SpecialCases()
	{
		// A point, and straight lines
		foreach (bool fill in new[] { false, true })
		{
			bool[,] point = Get(new byte[9 * 9 * 4].DrawTriangle(4, 4, 4, 4, 4, 4, fill, 0xFFFFFFFFu, 9), 9, 9),
				horizontal = Get(new byte[9 * 9 * 4].DrawTriangle(1, 4, 5, 4, 7, 4, fill, 0xFFFFFFFFu, 9), 9, 9),
				vertical = Get(new byte[9 * 9 * 4].DrawTriangle(4, 1, 4, 5, 4, 7, fill, 0xFFFFFFFFu, 9), 9, 9),
				diagonal = Get(new byte[9 * 9 * 4].DrawTriangle(1, 1, 4, 4, 7, 7, fill, 0xFFFFFFFFu, 9), 9, 9);
			for (int x = 0; x < 9; x++)
				for (int y = 0; y < 9; y++)
				{
					Assert.Equal(x == 4 && y == 4, point[x, y]);
					Assert.Equal(y == 4 && x >= 1 && x <= 7, horizontal[x, y]);
					Assert.Equal(x == 4 && y >= 1 && y <= 7, vertical[x, y]);
					Assert.Equal(x == y && x >= 1 && x <= 7, diagonal[x, y]);
				}
		}
		// A right triangle with a known pixel count: legs of 4 on a grid, rows have 5, 4, 3, 2, 1 pixels
		bool[,] right = Get(new byte[9 * 9 * 4].DrawTriangle(0, 0, 4, 0, 0, 4, true, 0xFFFFFFFFu, 9), 9, 9);
		for (int y = 0; y < 9; y++)
			for (int x = 0; x < 9; x++)
				Assert.Equal(y <= 4 && x <= 4 - y, right[x, y]);
		// Covers the whole texture, and misses it completely
		byte[] covered = new byte[16 * 16 * 4].DrawTriangle(-1_000_000, -1_000_000, 1_000_000, 0, 0, 1_000_000, true, 0xFFFFFFFFu, 16);
		Assert.DoesNotContain(covered.Chunk(4), pixel => pixel.All(b => b == 0));
		byte[] extremeCover = new byte[16 * 16 * 4].DrawTriangle(int.MinValue, int.MinValue, int.MaxValue, 0, 0, int.MaxValue, true, 0xFFFFFFFFu, 16);
		Assert.DoesNotContain(extremeCover.Chunk(4), pixel => pixel.All(b => b == 0));
		Assert.DoesNotContain(new byte[16 * 16 * 4].DrawTriangle(100, 100, 200, 100, 150, 200, true, 0xFFFFFFFFu, 16), b => b != 0);
		Assert.DoesNotContain(new byte[16 * 16 * 4].DrawTriangle(-100, 5, -50, 8, -80, 12, true, 0xFFFFFFFFu, 16), b => b != 0);
		Assert.Empty(new byte[0].DrawTriangle(0, 0, 5, 5, 0, 5, true));
	}
	[Fact]
	public void ModifiesOriginal_SpanAndArray()
	{
		foreach (bool fill in new[] { false, true })
		{
			byte[] array = new byte[16 * 16 * 4];
			array.DrawTriangle(2, 2, 12, 4, 6, 13, fill, 0xFFFFFFFFu, 16);
			Assert.Contains(array, b => b != 0);
			byte[] other = new byte[16 * 16 * 4];
			Span<byte> span = other;
			span.DrawTriangle(2, 2, 12, 4, 6, 13, fill, 0xFFFFFFFFu, 16);
			Assert.True(array.AsSpan().SequenceEqual(other));
		}
	}
}
