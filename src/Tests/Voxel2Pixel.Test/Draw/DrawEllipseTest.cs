using System.Numerics;
using Voxel2Pixel.Draw;
namespace Voxel2Pixel.Test.Draw;

public class DrawEllipseTest
{
	/// <summary>
	/// Brute force from the definition, with exact big integer math: a pixel is part of the ellipse if the middle of its bottom edge or of its left edge (the ones facing the center) is inside or on the ideal ellipse.
	/// </summary>
	static bool Included(long dx, long dy, int a, int b)
	{
		dx = Math.Abs(dx);
		dy = Math.Abs(dy);
		if (dx > a || dy > b) return false;
		BigInteger a2 = (BigInteger)a * a, b2 = (BigInteger)b * b;
		bool Inside(long doubleDx, long doubleDy) => b2 * doubleDx * doubleDx + a2 * doubleDy * doubleDy <= 4 * a2 * b2;
		return Inside(2 * dx, 2 * dy - 1) || Inside(2 * dx - 1, 2 * dy);
	}
	/// <summary>
	/// The outline is every pixel of the shape that has a pixel not in the shape directly above, below, left or right of it.
	/// </summary>
	static bool Outline(long dx, long dy, int a, int b) =>
		Included(dx, dy, a, b)
		&& !(Included(dx + 1, dy, a, b) && Included(dx - 1, dy, a, b) && Included(dx, dy + 1, a, b) && Included(dx, dy - 1, a, b));
	static bool[,] Reference(int w, int h, long cx, long cy, int a, int b, bool fill)
	{
		bool[,] set = new bool[w, h];
		for (int y = 0; y < h; y++)
			for (int x = 0; x < w; x++)
				set[x, y] = fill ? Included(x - cx, y - cy, a, b) : Outline(x - cx, y - cy, a, b);
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
		Random random = new(3);
		for (int i = 0; i < 20000; i++)
		{
			int w = random.Next(1, 21), h = random.Next(1, 21), cx = random.Next(-40, 60), cy = random.Next(-40, 60), a = random.Next(0, 41), b = random.Next(0, 41);
			bool fill = random.Next(2) == 0;
			byte[] texture = new byte[w * h * 4].DrawEllipse(cx, cy, (ushort)a, (ushort)b, fill, 0xFFFFFFFFu, (ushort)w);
			Same(Get(texture, w, h), Reference(w, h, cx, cy, a, b, fill), $"ellipse {cx},{cy} rx{a} ry{b} fill={fill} {w}x{h}");
		}
	}
	[Fact]
	public void HugeRadiiAndExtremeCenters()
	{
		int[] centers = [int.MinValue, int.MinValue + 1, -70000, -1, 0, 9, 70000, int.MaxValue - 1, int.MaxValue],
			radii = [0, 1, 2, 40000, 65535];
		System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
		foreach (int cx in centers)
			foreach (int cy in centers)
				foreach (int a in radii)
					foreach (int b in radii)
						foreach (bool fill in new[] { false, true })
						{
							int w = 13, h = 9;
							byte[] texture = new byte[w * h * 4].DrawEllipse(cx, cy, (ushort)a, (ushort)b, fill, 0xFFFFFFFFu, (ushort)w);
							Same(Get(texture, w, h), Reference(w, h, cx, cy, a, b, fill), $"extreme {cx},{cy} rx{a} ry{b} fill={fill}");
						}
		Random random = new(8);
		for (int i = 0; i < 100; i++)
		{
			int w = random.Next(1, 40), h = random.Next(1, 40), a = random.Next(1000, 65536), b = random.Next(1, 65536), cx = random.Next(-70000, 70000), cy = random.Next(-70000, 70000);
			bool fill = random.Next(2) == 0;
			byte[] texture = new byte[w * h * 4].DrawEllipse(cx, cy, (ushort)a, (ushort)b, fill, 0xFFFFFFFFu, (ushort)w);
			Same(Get(texture, w, h), Reference(w, h, cx, cy, a, b, fill), $"huge {cx},{cy} rx{a} ry{b} fill={fill} {w}x{h}");
		}
		Assert.True(stopwatch.ElapsedMilliseconds < 60000, "slow: " + stopwatch.ElapsedMilliseconds);
	}
	[Fact]
	public void EqualRadiiDrawTheSamePixelsAsACircle()
	{
		for (int r = 0; r <= 300; r++)
			foreach (bool fill in new[] { false, true })
			{
				int n = r * 2 + 3, c = r + 1;
				byte[] ellipse = new byte[n * n * 4].DrawEllipse(c, c, (ushort)r, (ushort)r, fill, 0xFFFFFFFFu, (ushort)n),
					circle = new byte[n * n * 4].DrawCircle(c, c, (ushort)r, fill, 0xFFFFFFFFu, (ushort)n);
				Assert.True(ellipse.AsSpan().SequenceEqual(circle), $"radius {r} fill={fill}");
			}
	}
	[Fact]
	public void Properties()
	{
		for (int a = 0; a <= 30; a++)
			for (int b = 0; b <= 30; b++)
			{
				int n = Math.Max(a, b) * 2 + 3, cx = a + 1, cy = b + 1;
				bool[,] outline = Get(new byte[n * n * 4].DrawEllipse(cx, cy, (ushort)a, (ushort)b, false, 0xFFFFFFFFu, (ushort)n), n, n),
					filled = Get(new byte[n * n * 4].DrawEllipse(cx, cy, (ushort)a, (ushort)b, true, 0xFFFFFFFFu, (ushort)n), n, n);
				int outlineCount = 0;
				for (int x = 0; x < n; x++)
					for (int y = 0; y < n; y++)
					{
						if (outline[x, y]) { outlineCount++; Assert.True(filled[x, y], $"fill includes outline {a},{b}"); }
						// Mirror left-right and top-bottom around the center
						int mx = 2 * cx - x, my = 2 * cy - y;
						if (mx >= 0 && mx < n) { Assert.Equal(outline[x, y], outline[mx, y]); Assert.Equal(filled[x, y], filled[mx, y]); }
						if (my >= 0 && my < n) { Assert.Equal(outline[x, y], outline[x, my]); Assert.Equal(filled[x, y], filled[x, my]); }
					}
				// Its extremes are exactly the radii
				Assert.True(filled[cx + a, cy] && filled[cx - a, cy] && filled[cx, cy + b] && filled[cx, cy - b]);
				Assert.False(cx + a + 1 < n && filled[cx + a + 1, cy]);
				Assert.False(cy + b + 1 < n && filled[cx, cy + b + 1]);
				// The outline is one connected piece (counting diagonals)
				HashSet<(int, int)> remaining = [];
				for (int x = 0; x < n; x++)
					for (int y = 0; y < n; y++)
						if (outline[x, y]) remaining.Add((x, y));
				Stack<(int, int)> stack = new();
				(int, int) first = remaining.First();
				remaining.Remove(first);
				stack.Push(first);
				while (stack.Count > 0)
				{
					(int px, int py) = stack.Pop();
					for (int ox = -1; ox <= 1; ox++)
						for (int oy = -1; oy <= 1; oy++)
							if (remaining.Remove((px + ox, py + oy)))
								stack.Push((px + ox, py + oy));
				}
				Assert.True(remaining.Count == 0, $"outline of {a},{b} is in several pieces");
			}
		// Lines and a point
		bool[,] column = Get(new byte[11 * 11 * 4].DrawEllipse(5, 5, 0, 4, false, 0xFFFFFFFFu, 11), 11, 11),
			row = Get(new byte[11 * 11 * 4].DrawEllipse(5, 5, 4, 0, true, 0xFFFFFFFFu, 11), 11, 11),
			point = Get(new byte[11 * 11 * 4].DrawEllipse(5, 5, 0, 0, false, 0xFFFFFFFFu, 11), 11, 11);
		for (int x = 0; x < 11; x++)
			for (int y = 0; y < 11; y++)
			{
				Assert.Equal(x == 5 && y >= 1 && y <= 9, column[x, y]);
				Assert.Equal(y == 5 && x >= 1 && x <= 9, row[x, y]);
				Assert.Equal(x == 5 && y == 5, point[x, y]);
			}
		// Completely missing the texture, and empty textures
		Assert.DoesNotContain(new byte[16 * 16 * 4].DrawEllipse(1000, 1000, 10, 20, true, 0xFFFFFFFFu, 16), b => b != 0);
		Assert.Empty(new byte[0].DrawEllipse(0, 0, 5, 3));
	}
	[Fact]
	public void ModifiesOriginal_SpanAndArray()
	{
		byte[] array = new byte[16 * 16 * 4];
		array.DrawEllipse(8, 8, 6, 3, true, 0xFFFFFFFFu, 16);
		Assert.Contains(array, b => b != 0);
		byte[] other = new byte[16 * 16 * 4];
		Span<byte> span = other;
		span.DrawEllipse(8, 8, 6, 3, true, 0xFFFFFFFFu, 16);
		Assert.True(array.AsSpan().SequenceEqual(other));
	}
}
