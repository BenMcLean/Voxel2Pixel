using Voxel2Pixel.Draw;
namespace Voxel2Pixel.Test.Draw;

/// <summary>
/// Checks the geometry of the transforms against their definitions, written out independently of how they're implemented.
/// Source pixels get unique, nonzero values so that anything that is moved to the wrong place, left out or drawn twice shows.
/// </summary>
public class TransformGeometryTest
{
	/// <summary>
	/// Pixel number i + 1 (so never all zero) in its red and green bytes, which allows 65535 unique pixels, with full alpha.
	/// </summary>
	static byte[] Numbered(int w, int h)
	{
		byte[] t = new byte[w * h * 4];
		for (int i = 0; i < w * h; i++)
		{
			t[i * 4] = (byte)((i + 1) & 0xFF);
			t[i * 4 + 1] = (byte)((i + 1) >> 8);
			t[i * 4 + 2] = (byte)(w * 7 + h);
			t[i * 4 + 3] = 255;
		}
		return t;
	}
	static uint At(byte[] t, int width, int x, int y) => BitConverter.ToUInt32(t, (y * width + x) * 4);
	/// <summary>
	/// A grid of pixels that can be painted on, in order, then compared with a texture.
	/// </summary>
	class Canvas(int width, int height)
	{
		public readonly uint[] Pixels = new uint[width * height];
		public int Width => width;
		public int Height => height;
		public void Paint(int x, int y, uint color)
		{
			Assert.True(x >= 0 && x < width && y >= 0 && y < height, $"painting outside the canvas at {x},{y} of {width}x{height}");
			Pixels[y * width + x] = color;
		}
		public void AssertSame(byte[] texture, string message)
		{
			Assert.True(texture.Length == width * height * 4, $"{message}: size {texture.Length / 4} pixels but expected {width}x{height}");
			for (int y = 0; y < height; y++)
				for (int x = 0; x < width; x++)
					Assert.True(Pixels[y * width + x] == At(texture, width, x, y), $"{message}: differs at {x},{y} of {width}x{height}");
		}
	}
	/// <summary>
	/// Turning by 45 or 135 degrees: the pixel at (x, y) goes to a column and row that are each plus or minus x plus or minus y, shifted so that nothing is left of or above 0.
	/// Every pixel is then drawn twice as wide ("fat") or twice as tall ("thin").
	/// </summary>
	static void CheckDiagonalRotation(string name, Func<byte[], ushort, byte[]> rotate, Func<int, int, int> column, Func<int, int, int> row, bool thin)
	{
		for (int w = 1; w <= 9; w++)
			for (int h = 1; h <= 9; h++)
			{
				byte[] t = Numbered(w, h);
				int minColumn = int.MaxValue, maxColumn = int.MinValue, minRow = int.MaxValue, maxRow = int.MinValue;
				for (int y = 0; y < h; y++)
					for (int x = 0; x < w; x++)
					{
						minColumn = Math.Min(minColumn, column(x, y));
						maxColumn = Math.Max(maxColumn, column(x, y));
						minRow = Math.Min(minRow, row(x, y));
						maxRow = Math.Max(maxRow, row(x, y));
					}
				Canvas canvas = new(maxColumn - minColumn + (thin ? 1 : 2), maxRow - minRow + (thin ? 2 : 1));
				for (int y = 0; y < h; y++)
					for (int x = 0; x < w; x++)
					{
						int c = column(x, y) - minColumn, r = row(x, y) - minRow;
						canvas.Paint(c, r, At(t, w, x, y));
						canvas.Paint(thin ? c : c + 1, thin ? r + 1 : r, At(t, w, x, y));
					}
				canvas.AssertSame(rotate(t, (ushort)w), $"{name} {w}x{h}");
			}
	}
	[Fact]
	public void RotateClockwise45() => CheckDiagonalRotation(nameof(RotateClockwise45), (t, w) => t.RotateClockwise45(w), (x, y) => x - y, (x, y) => x + y, false);
	[Fact]
	public void RotateClockwise45Thin() => CheckDiagonalRotation(nameof(RotateClockwise45Thin), (t, w) => t.RotateClockwise45Thin(w), (x, y) => x - y, (x, y) => x + y, true);
	[Fact]
	public void RotateCounter45() => CheckDiagonalRotation(nameof(RotateCounter45), (t, w) => t.RotateCounter45(w), (x, y) => x + y, (x, y) => y - x, false);
	[Fact]
	public void RotateCounter45Thin() => CheckDiagonalRotation(nameof(RotateCounter45Thin), (t, w) => t.RotateCounter45Thin(w), (x, y) => x + y, (x, y) => y - x, true);
	[Fact]
	public void RotateClockwise135() => CheckDiagonalRotation(nameof(RotateClockwise135), (t, w) => t.RotateClockwise135(w), (x, y) => -x - y, (x, y) => x - y, false);
	[Fact]
	public void RotateClockwise135Thin() => CheckDiagonalRotation(nameof(RotateClockwise135Thin), (t, w) => t.RotateClockwise135Thin(w), (x, y) => -x - y, (x, y) => x - y, true);
	[Fact]
	public void RotateCounter135() => CheckDiagonalRotation(nameof(RotateCounter135), (t, w) => t.RotateCounter135(w), (x, y) => y - x, (x, y) => -x - y, false);
	[Fact]
	public void RotateCounter135Thin() => CheckDiagonalRotation(nameof(RotateCounter135Thin), (t, w) => t.RotateCounter135Thin(w), (x, y) => y - x, (x, y) => -x - y, true);
	/// <summary>
	/// The isometric methods draw each source pixel as a small shape of pixels, starting at a base position that moves 2 columns for each x and some rows for each x and y.
	/// Source pixels are painted one row at a time from the top, left to right, so a later pixel paints over an earlier one where their shapes overlap.
	/// </summary>
	static void CheckIso(string name, Func<byte[], ushort, byte[]> iso, Func<int, int, int, int, int> baseRow, int rowsOffset, (int, int)[] shape, Func<int, int, int> rows)
	{
		for (int w = 1; w <= 8; w++)
			for (int h = 1; h <= 8; h++)
			{
				byte[] t = Numbered(w, h);
				Canvas canvas = new(w * 2 + (name == nameof(IsoTile) ? (h - 1) * 2 : 0), rows(w, h));
				for (int y = 0; y < h; y++)
					for (int x = 0; x < w; x++)
					{
						int column = 2 * x + (name == nameof(IsoTile) ? 2 * y : 0), row = baseRow(x, y, w, h) + rowsOffset;
						foreach ((int dc, int dr) in shape)
							canvas.Paint(column + dc, row + dr, At(t, w, x, y));
					}
				canvas.AssertSame(iso(t, (ushort)w), $"{name} {w}x{h}");
			}
	}
	[Fact]
	public void IsoSlantDown() => CheckIso(nameof(IsoSlantDown), (t, w) => t.IsoSlantDown(w), (x, y, w, h) => 2 * y + x, 0,
		[(0, 0), (0, 1), (1, 1), (0, 2), (1, 2), (1, 3)], (w, h) => h * 2 + w + 1);
	[Fact]
	public void IsoSlantDownShort() => CheckIso(nameof(IsoSlantDownShort), (t, w) => t.IsoSlantDownShort(w), (x, y, w, h) => y + x, 0,
		[(0, 0), (0, 1), (1, 1), (1, 2)], (w, h) => w + 1 + h);
	[Fact]
	public void IsoSlantUp() => CheckIso(nameof(IsoSlantUp), (t, w) => t.IsoSlantUp(w), (x, y, w, h) => w + 2 + 2 * y - x, 0,
		[(0, 0), (0, -1), (1, -1), (0, -2), (1, -2), (1, -3)], (w, h) => h * 2 + w + 1);
	[Fact]
	public void IsoSlantUpShort() => CheckIso(nameof(IsoSlantUpShort), (t, w) => t.IsoSlantUpShort(w), (x, y, w, h) => w + 1 + y - x, 0,
		[(0, 0), (0, -1), (1, -1), (1, -2)], (w, h) => w + 1 + h);
	[Fact]
	public void IsoTile() => CheckIso(nameof(IsoTile), (t, w) => t.IsoTile(w), (x, y, w, h) => w - 1 + y - x, 0,
		[(0, 0), (1, 0), (0, 1), (1, 1)], (w, h) => w + h);
	[Fact]
	public void Upscale()
	{
		for (int w = 1; w <= 6; w++)
			for (int h = 1; h <= 6; h++)
				for (byte scaleX = 1; scaleX <= 4; scaleX++)
					for (byte scaleY = 1; scaleY <= 4; scaleY++)
					{
						byte[] t = Numbered(w, h);
						Canvas canvas = new(w * scaleX, h * scaleY);
						for (int y = 0; y < h * scaleY; y++)
							for (int x = 0; x < w * scaleX; x++)
								canvas.Paint(x, y, At(t, w, x / scaleX, y / scaleY));
						canvas.AssertSame(t.Upscale(scaleX, scaleY, (ushort)w), $"Upscale {w}x{h} by {scaleX},{scaleY}");
					}
	}
	[Fact]
	public void Tile()
	{
		for (int w = 1; w <= 6; w++)
			for (int h = 1; h <= 6; h++)
				for (byte factorX = 1; factorX <= 4; factorX++)
					for (byte factorY = 1; factorY <= 4; factorY++)
					{
						byte[] t = Numbered(w, h);
						Canvas canvas = new(w * factorX, h * factorY);
						for (int y = 0; y < h * factorY; y++)
							for (int x = 0; x < w * factorX; x++)
								canvas.Paint(x, y, At(t, w, x % w, y % h));
						canvas.AssertSame(t.Tile((ushort)w, factorX, factorY), $"Tile {w}x{h} by {factorX},{factorY}");
					}
	}
	[Fact]
	public void Crop()
	{
		for (int w = 1; w <= 7; w++)
			for (int h = 1; h <= 7; h++)
				for (int x = -3; x < w; x++)
					for (int y = -3; y < h; y++)
						for (int croppedWidth = 1; croppedWidth <= w + 3; croppedWidth++)
							for (int croppedHeight = 1; croppedHeight <= h + 3; croppedHeight++)
							{
								// Whatever sticks out past the left or top is cut off from the size, and whatever sticks out past the right or bottom is cut off from the size
								int left = Math.Max(x, 0), top = Math.Max(y, 0),
									expectedWidth = Math.Min(croppedWidth + Math.Min(x, 0), w - left),
									expectedHeight = Math.Min(croppedHeight + Math.Min(y, 0), h - top);
								if (expectedWidth < 1 || expectedHeight < 1) continue;
								byte[] t = Numbered(w, h);
								Canvas canvas = new(expectedWidth, expectedHeight);
								for (int cy = 0; cy < expectedHeight; cy++)
									for (int cx = 0; cx < expectedWidth; cx++)
										canvas.Paint(cx, cy, At(t, w, left + cx, top + cy));
								canvas.AssertSame(t.Crop(x, y, croppedWidth, croppedHeight, (ushort)w), $"Crop {w}x{h} at {x},{y} {croppedWidth}x{croppedHeight}");
							}
	}
	[Fact]
	public void Resize()
	{
		for (int w = 1; w <= 6; w++)
			for (int h = 1; h <= 6; h++)
				for (ushort newWidth = 1; newWidth <= 9; newWidth++)
					for (ushort newHeight = 1; newHeight <= 9; newHeight++)
					{
						byte[] t = Numbered(w, h);
						Canvas canvas = new(newWidth, newHeight);
						for (int y = 0; y < Math.Min(h, newHeight); y++)
							for (int x = 0; x < Math.Min(w, newWidth); x++)
								canvas.Paint(x, y, At(t, w, x, y));
						canvas.AssertSame(t.Resize(newWidth, newHeight, (ushort)w), $"Resize {w}x{h} to {newWidth}x{newHeight}");
					}
	}
}
