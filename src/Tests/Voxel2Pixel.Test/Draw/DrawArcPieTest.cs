using Voxel2Pixel.Draw;
namespace Voxel2Pixel.Test.Draw;

public class DrawArcPieTest
{
	const double Epsilon = 1e-6;
	static bool[,] Get(byte[] t, int w, int h)
	{
		bool[,] s = new bool[w, h];
		for (int y = 0; y < h; y++)
			for (int x = 0; x < w; x++)
				s[x, y] = BitConverter.ToUInt32(t, (y * w + x) * 4) != 0;
		return s;
	}
	/// <summary>
	/// Clockwise distance in radians from the start angle to the pixel's parametric angle, in [0, Tau)
	/// </summary>
	static double Offset(int dx, int dy, int a, int b, double start)
	{
		double offset = (Math.Atan2(dy / (double)Math.Max(b, 1), dx / (double)Math.Max(a, 1)) - start) % PixelDraw.Tau;
		return offset < 0 ? offset + PixelDraw.Tau : offset;
	}
	[Fact]
	public void FullTurnIsTheWholeEllipse()
	{
		foreach (bool fill in new[] { false, true })
			Assert.Equal(
				new byte[20 * 16 * 4].DrawEllipse(9, 8, 7, 5, fill, 0xFFFFFFFFu, 20),
				new byte[20 * 16 * 4].DrawPie(9, 8, 7, 5, 1d, 1d + PixelDraw.Tau, fill, 0xFFFFFFFFu, 20));
		Assert.Equal(
			new byte[20 * 16 * 4].DrawEllipse(9, 8, 7, 5, false, 0xFFFFFFFFu, 20),
			new byte[20 * 16 * 4].DrawArc(9, 8, 7, 5, -2d, 10d, 0xFFFFFFFFu, 20));
	}
	[Fact]
	public void CircleOverloadsMatchEllipseWithEqualRadii()
	{
		Assert.Equal(
			new byte[20 * 20 * 4].DrawArc(10, 10, 8, 0.5, 4d, 0xFFFFFFFFu),
			new byte[20 * 20 * 4].DrawArc(10, 10, 8, 8, 0.5, 4d, 0xFFFFFFFFu));
		Assert.Equal(
			new byte[20 * 20 * 4].DrawPie(10, 10, 8, 0.5, 4d, true, 0xFFFFFFFFu),
			new byte[20 * 20 * 4].DrawPie(10, 10, 8, 8, 0.5, 4d, true, 0xFFFFFFFFu));
	}
	[Fact]
	public void MatchesAngleDefinition()
	{
		Random random = new(7);
		for (int i = 0; i < 5000; i++)
		{
			int w = random.Next(1, 25), h = random.Next(1, 25), cx = random.Next(-10, 35), cy = random.Next(-10, 35), a = random.Next(0, 16), b = random.Next(0, 16);
			double start = random.NextDouble() * 4 * Math.PI - 2 * Math.PI,
				sweep = random.NextDouble() * (PixelDraw.Tau - 0.001);
			bool fill = random.Next(2) == 0;
			byte[] ellipse = new byte[w * h * 4].DrawEllipse(cx, cy, (ushort)a, (ushort)b, fill, 0xFFFFFFFFu, (ushort)w),
				pie = new byte[w * h * 4].DrawPie(cx, cy, (ushort)a, (ushort)b, start, start + sweep, fill, 0xFFFFFFFFu, (ushort)w),
				arc = new byte[w * h * 4].DrawArc(cx, cy, (ushort)a, (ushort)b, start, start + sweep, 0xFFFFFFFFu, (ushort)w);
			bool[,] ellipseSet = Get(ellipse, w, h), pieSet = Get(pie, w, h);
			bool[,] arcSet = fill ? Get(new byte[w * h * 4].DrawEllipse(cx, cy, (ushort)a, (ushort)b, false, 0xFFFFFFFFu, (ushort)w), w, h) : ellipseSet;
			bool[,] arcDrawn = Get(arc, w, h);
			for (int y = 0; y < h; y++)
				for (int x = 0; x < w; x++)
				{
					int dx = x - cx, dy = y - cy;
					double offset = Offset(dx, dy, a, b, start);
					if ((dx == 0 && dy == 0) || Math.Abs(offset) < Epsilon || Math.Abs(offset - sweep) < Epsilon || Math.Abs(offset - PixelDraw.Tau) < Epsilon)
						continue; // On the center or an edge of the range, where floating point decides
					bool inRange = offset < sweep;
					string message = $"{cx},{cy} rx{a} ry{b} start{start} sweep{sweep} fill={fill} {w}x{h} at {x},{y}";
					if (ellipseSet[x, y] && inRange)
						Assert.True(pieSet[x, y], "pie missing " + message);
					else if (fill && pieSet[x, y])
						Assert.Fail("pie extra " + message); // An outline pie also has its radius lines, which aren't checked here
					Assert.Equal(arcSet[x, y] && inRange, arcDrawn[x, y]);
				}
		}
	}
	[Fact]
	public void OutlinePieHasArcAndRadii()
	{
		byte[] arc = new byte[21 * 21 * 4].DrawArc(10, 10, 9, 0d, Math.PI / 2, 0xFFFFFFFFu),
			pie = new byte[21 * 21 * 4].DrawPie(10, 10, 9, 0d, Math.PI / 2, false, 0xFFFFFFFFu);
		bool[,] arcSet = Get(arc, 21, 21), pieSet = Get(pie, 21, 21);
		for (int y = 0; y < 21; y++)
			for (int x = 0; x < 21; x++)
				if (arcSet[x, y])
					Assert.True(pieSet[x, y]);
		Assert.True(pieSet[15, 10] && pieSet[10, 15]); // Along both radii
		Assert.False(pieSet[5, 10] || pieSet[10, 5]); // Outside the quarter
	}
	[Fact]
	public void NonFiniteAnglesDrawNothing()
	{
		Assert.All(new byte[10 * 10 * 4].DrawPie(5, 5, 4, double.NaN, 1d, true), b => Assert.Equal(0, b));
		Assert.All(new byte[10 * 10 * 4].DrawArc(5, 5, 4, 0d, double.PositiveInfinity), b => Assert.Equal(0, b));
	}
}
