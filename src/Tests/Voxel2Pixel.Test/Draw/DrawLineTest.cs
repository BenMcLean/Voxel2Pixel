using System.Numerics;
using Voxel2Pixel.Draw;
namespace Voxel2Pixel.Test.Draw;
public class DrawLineTest
{
	// Exact reference: same pixel rule, BigInteger math, no incremental error term, walks only the texture's major range.
	static bool[,] Ref(int w, int h, long xA, long yA, long xB, long yB)
	{
		var set = new bool[w, h];
		bool steep = BigInteger.Abs(yB - yA) > BigInteger.Abs(xB - xA);
		BigInteger a0 = steep ? yA : xA, a1 = steep ? yB : xB, b0 = steep ? xA : yA, b1 = steep ? xB : yB;
		if (a0 > a1) { (a0, a1) = (a1, a0); (b0, b1) = (b1, b0); }
		BigInteger dM = a1 - a0, dm = BigInteger.Abs(b1 - b0); int sign = b1 >= b0 ? 1 : -1;
		int majorSize = steep ? h : w, minorSize = steep ? w : h;
		for (int p = 0; p < majorSize; p++)
		{
			if (p < a0 || p > a1) continue;
			BigInteger i = p - a0;
			BigInteger q = dM.IsZero ? 0 : BigInteger.Divide(2 * dm * i + dM, 2 * dM);
			BigInteger minor = b0 + sign * q;
			if (minor < 0 || minor >= minorSize) continue;
			if (steep) set[(int)minor, p] = true; else set[p, (int)minor] = true;
		}
		return set;
	}
	static bool[,] Get(byte[] t, int w, int h)
	{
		var s = new bool[w, h];
		for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) s[x, y] = BitConverter.ToUInt32(t, (y * w + x) * 4) != 0;
		return s;
	}
	static void Same(bool[,] a, bool[,] b, string msg)
	{
		for (int x = 0; x < a.GetLength(0); x++) for (int y = 0; y < a.GetLength(1); y++)
			Assert.True(a[x, y] == b[x, y], $"{msg} differs at {x},{y}");
	}
	[Fact]
	public void MatchesExactReference_AndIsOrderIndependent()
	{
		Random r = new(42);
		for (int it = 0; it < 20000; it++)
		{
			int w = r.Next(1, 17), h = r.Next(1, 17), m = r.Next(0, 3) == 0 ? 1000 : 40;
			int xA = r.Next(-m, m), yA = r.Next(-m, m), xB = r.Next(-m, m), yB = r.Next(-m, m);
			var t = new byte[w * h * 4].DrawLine(xA, yA, xB, yB, 0xFFFFFFFFu, (ushort)w);
			var u = new byte[w * h * 4].DrawLine(xB, yB, xA, yA, 0xFFFFFFFFu, (ushort)w);
			Assert.True(t.AsSpan().SequenceEqual(u), $"order {xA},{yA} {xB},{yB} {w}x{h}");
			Same(Get(t, w, h), Ref(w, h, xA, yA, xB, yB), $"line {xA},{yA}->{xB},{yB} {w}x{h}");
		}
	}
	[Fact]
	public void Properties()
	{
		Random r = new(5);
		for (int it = 0; it < 5000; it++)
		{
			int n = 20, xA = r.Next(0, n), yA = r.Next(0, n), xB = r.Next(0, n), yB = r.Next(0, n);
			var s = Get(new byte[n * n * 4].DrawLine(xA, yA, xB, yB, 0xFFFFFFFFu, (ushort)n), n, n);
			Assert.True(s[xA, yA] && s[xB, yB], "endpoints");
			int dx = Math.Abs(xB - xA), dy = Math.Abs(yB - yA), count = 0;
			for (int x = 0; x < n; x++) for (int y = 0; y < n; y++) if (s[x, y]) count++;
			Assert.Equal(Math.Max(dx, dy) + 1, count);
			// every drawn pixel is within half a pixel of the ideal line on the minor axis (+ tiny epsilon)
			for (int x = 0; x < n; x++) for (int y = 0; y < n; y++) if (s[x, y])
			{
				if (dx >= dy && dx > 0) { double ideal = yA + (double)(yB - yA) * (x - xA) / (xB - xA); Assert.True(Math.Abs(ideal - y) <= 0.5 + 1e-9); }
				else if (dy > 0) { double ideal = xA + (double)(xB - xA) * (y - yA) / (yB - yA); Assert.True(Math.Abs(ideal - x) <= 0.5 + 1e-9); }
			}
		}
		// single point, point outside, zero-size texture
		Assert.True(Get(new byte[4 * 4 * 4].DrawLine(2, 1, 2, 1, 0xFFFFFFFFu, 4), 4, 4)[2, 1]);
		Assert.DoesNotContain(new byte[4 * 4 * 4].DrawLine(9, 9, 9, 9, 0xFFFFFFFFu, 4), v => v != 0);
		Assert.Empty(new byte[0].DrawLine(0, 0, 5, 5));
	}
	[Fact]
	public void ExtremeCoordinates()
	{
		int[] vals = [int.MinValue, int.MinValue + 1, -1_000_000_007, -1, 0, 1, 7, 1_000_000_007, int.MaxValue - 1, int.MaxValue];
		var sw = System.Diagnostics.Stopwatch.StartNew();
		foreach (int xA in vals) foreach (int yA in vals) foreach (int xB in vals) foreach (int yB in vals)
		{
			int w = 13, h = 9;
			var t = new byte[w * h * 4].DrawLine(xA, yA, xB, yB, 0xFFFFFFFFu, (ushort)w);
			Same(Get(t, w, h), Ref(w, h, xA, yA, xB, yB), $"extreme {xA},{yA}->{xB},{yB}");
		}
		Assert.True(sw.ElapsedMilliseconds < 20000, "slow: " + sw.ElapsedMilliseconds);
		var diag = Get(new byte[8 * 8 * 4].DrawLine(int.MinValue, int.MinValue, int.MaxValue, int.MaxValue, 0xFFFFFFFFu, 8), 8, 8);
		for (int i = 0; i < 8; i++) Assert.True(diag[i, i]);
	}
	[Fact]
	public void ModifiesOriginal_SpanAndArray()
	{
		var a = new byte[16 * 4]; a.DrawLine(0, 0, 3, 3, 0xFFFFFFFFu, 4); Assert.Contains(a, v => v != 0);
		var b = new byte[16 * 4]; Span<byte> s = b; s.DrawLine(0, 0, 3, 3, 0xFFFFFFFFu, 4); Assert.True(a.AsSpan().SequenceEqual(b));
	}
}
