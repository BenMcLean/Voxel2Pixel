using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Voxel2Pixel.Draw;

/// <summary>
/// Most graphics-related libraries want you to use their custom classes and structs to represent pixels, textures and colors.
/// However, following that pattern tends very strongly towards undesirable vendor lock-in.
/// Switching libraries would require re-writing the same operations using different classes and structs.
/// After spending a lot of time with this, I decided that at the most absolute basic level of rendering individual pixels, I needed to do the actual work Jonathan Blow style, meaning no abstractions.
/// I don't apply Blow's anti-abstraction ideology to the rest of the codebase and I didn't know about Blow's views on this topic until afterwards but I find that Blow's anti-abstraction view matched my intuitions for the specific needs this class addresses, even though I do not agree with it for most tasks.
/// Directly using primitive types like this does limit the utility of the code to only work with 24-bit color (plus 8 bits of transparency) but I found this to be an acceptable compromise because I judged that adding abstraction for greater color depths involved too much overhead for such a niche use case as going beyond 24-bit color.
/// Future people might laugh at the idea that we only need 24-bit color but it pretty much covers how human eyes work for the purpose of making 1990s style pixel art for video games in the early 21st century.
/// All methods in this static class are stateless functions, meaning that they do not reference any modifiable variables besides their parameters.
/// Methods that start with "Draw" modify the original texture. Other methods that return a texture array are making a copy.
/// These methods assume rgba8888 a.k.a. rgba32 color. That means every four bytes represent one pixel.
/// A texture is a byte array of size = wdith * 4 * height.
/// Because of 0-based indexing, valid x values range from 0 to width-1 inclusive and valid y values range from 0 to height-1 inclusive.
/// x+ is right, y+ is down. 0, 0 is located in the upper left of a texture.
/// uint colors use a, b, g, r byte order so that they conform to C#'s little-endian standard, so that writing 0x00FF00FFu in the source code would indicate fully opaque green. (Yes, C# reverses the byte order in memory from its inline hex representation.)
/// However, byte array textures use r, g, b, a byte order, which requires reversing the endianness if writing to a byte array texture from a little-endian uint color using BinaryPrimitives.WriteUInt32BigEndian or similar.
/// Omitting the width to get a default value of 0 for texture manipulation methods indicates that the source texture is square.
/// Sometimes bitwise operations are hard to follow:
/// (i << 2 == i * 4)
/// (i >> 2 == i / 4) when i is a positive integer
/// </summary>
public static class PixelDraw
{
	//TODO: DrawFlood
	//TODO: DrawCircle for arcs and pies
	//TODO: DrawInsert XOR / OR / AND modes
	public const double Tau = 2d * Math.PI;
	public const uint Red = 0xFF0000FFu,
		Yellow = 0xFFFF00FFu,
		Black = 0x000000FFu,
		White = 0xFFFFFFFFu,
		Green = 0x00FF00FFu,
		Blue = 0x0000FFFFu,
		Orange = 0xFFA500FFu,
		Indigo = 0x4B0082FFu,
		Violet = 0x8F00FFFFu,
		Purple = 0xFF00FFFFu;
	public const byte DefaultTransparencyThreshold = 127;
	#region Drawing
	/// <summary>
	/// Draws one pixel of the specified color
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data</param>
	/// <param name="color">rgba color to draw</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>same texture with pixel drawn</returns>
	public static Span<byte> DrawPixel(this Span<byte> texture, int x, int y, uint color = White, ushort width = 0)
	{
		int xSide = PixelWidth(texture.Length, width) << 2,
			ySide = PixelHeight(texture.Length, width);
		if (x < 0 || y < 0 || x >= xSide >> 2 || y >= ySide) return texture;
		WritePixel(texture, y * xSide + (x << 2), color);
		return texture;
	}
	/// <inheritdoc cref="DrawPixel(Span{byte}, int, int, uint, ushort)"/>
	public static byte[] DrawPixel(this byte[] texture, int x, int y, uint color = White, ushort width = 0)
	{
		texture.AsSpan().DrawPixel(x, y, color, width);
		return texture;
	}
	/// <summary>
	/// Draws a rectangle of the specified color, either filled in or as a 1 pixel wide outline.
	/// The rectangle may extend past any edge of the texture, or be completely outside it. Only the part that overlaps the texture is drawn.
	/// An outline keeps its edges where they would be if the whole rectangle were on the texture, so for example a rectangle sticking out past the left edge has no left side.
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data to be modified</param>
	/// <param name="color">rgba color to draw</param>
	/// <param name="x">upper left corner of rectangle</param>
	/// <param name="y">upper left corner of rectangle</param>
	/// <param name="rectWidth">width of the rectangle in pixels</param>
	/// <param name="rectHeight">height of the rectangle in pixels or less than 1 to make it as tall as it is wide</param>
	/// <param name="fill">true to fill in the rectangle, false to only draw its outline</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>same texture with rectangle drawn</returns>
	public static Span<byte> DrawRectangle(this Span<byte> texture, uint color, int x, int y, int rectWidth = 1, int rectHeight = 1, bool fill = false, ushort width = 0)
	{
		if (rectWidth == 1 && rectHeight == 1)
			return texture.DrawPixel(x, y, color, width);
		if (rectHeight < 1) rectHeight = rectWidth;
		if (rectWidth < 1 || rectHeight < 1) return texture; // Nothing to draw, and this keeps the clipping math below from overflowing
		if (!fill && rectWidth > 2 && rectHeight > 2)
		{ // Four filled edges. Each one is clipped separately, so that the edges that are off the texture aren't drawn.
			long right = (long)x + rectWidth - 1,
				bottom = (long)y + rectHeight - 1;
			texture.DrawRectangle(color, x, y, rectWidth, 1, true, width);
			if (bottom <= int.MaxValue)
				texture.DrawRectangle(color, x, (int)bottom, rectWidth, 1, true, width);
			if (y < int.MaxValue) // Otherwise the sides start below the largest possible texture
			{
				texture.DrawRectangle(color, x, y + 1, 1, rectHeight - 2, true, width);
				if (right <= int.MaxValue)
					texture.DrawRectangle(color, (int)right, y + 1, 1, rectHeight - 2, true, width);
			}
			return texture;
		}
		if (x < 0)
		{
			rectWidth += x;
			x = 0;
		}
		if (y < 0)
		{
			rectHeight += y;
			y = 0;
		}
		width = (ushort)PixelWidth(texture.Length, width);
		int height = PixelHeight(texture.Length, width);
		if (rectWidth < 1 || rectHeight < 1 || x >= width || y >= height) return texture;
		rectWidth = Math.Min(rectWidth, width - x);
		rectHeight = Math.Min(rectHeight, height - y);
		int xSide = width << 2,
			x4 = x << 2,
			offset = y * xSide + x4,
			rectWidth4 = rectWidth << 2,
			yStop = offset + xSide * rectHeight;
		WritePixel(texture, offset, color);
		for (int filled = 4; filled < rectWidth4; filled <<= 1) // Double the filled run each time
			texture.Slice(offset, Math.Min(filled, rectWidth4 - filled)).CopyTo(texture[(offset + filled)..]);
		for (int y2 = offset + xSide; y2 < yStop; y2 += xSide)
			texture.Slice(offset, rectWidth4).CopyTo(texture[y2..]);
		return texture;
	}
	/// <inheritdoc cref="DrawRectangle(Span{byte}, uint, int, int, int, int, bool, ushort)"/>
	public static byte[] DrawRectangle(this byte[] texture, uint color, int x, int y, int rectWidth = 1, int rectHeight = 1, bool fill = false, ushort width = 0)
	{
		texture.AsSpan().DrawRectangle(color, x, y, rectWidth, rectHeight, fill, width);
		return texture;
	}
	/// <summary>
	/// Draws a line of the specified color using Bresenham's algorithm.
	/// Both points may be anywhere, including completely outside the texture in any direction. Only the part of the line that intersects the texture is drawn, and it consists of exactly the pixels that drawing the whole line would have put there.
	/// The same pixels are drawn no matter which point is A and which is B.
	/// Where the ideal line passes exactly halfway between two pixels, the pixel farther along the minor axis is chosen, going from the lower to the higher coordinate on the major axis.
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data to be modified</param>
	/// <param name="xA">x of the first point</param>
	/// <param name="yA">y of the first point</param>
	/// <param name="xB">x of the second point</param>
	/// <param name="yB">y of the second point</param>
	/// <param name="color">rgba color to draw</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>same texture with line drawn</returns>
	public static Span<byte> DrawLine(this Span<byte> texture, int xA, int yA, int xB, int yB, uint color = White, ushort width = 0)
	{
		int w = PixelWidth(texture.Length, width),
			h = PixelHeight(texture.Length, width);
		if (w < 1 || h < 1) return texture;
		if (yA == yB)
		{ // A horizontal line (or a single point) is one run of pixels, which draws the same pixels as the general code below would without its error term and bounds checks.
			int left = Math.Max(Math.Min(xA, xB), 0),
				right = Math.Min(Math.Max(xA, xB), w - 1);
			if (yA >= 0 && yA < h && left <= right)
				texture.DrawRectangle(color, left, yA, right - left + 1, 1, true, width);
			return texture;
		}
		// Work in (major, minor) coordinates, where major is the axis the line is longer on, so that one loop handles all eight octants.
		BresenhamLine line = new(xA, yA, xB, yB);
		int majorSize = line.Steep ? h : w,
			minorSize = line.Steep ? w : h,
			majorStride = line.Steep ? w << 2 : 4,
			minorStride = line.Steep ? 4 : w << 2;
		long twoMajorLength = line.MajorLength << 1,
			twoMinorLength = line.MinorLength << 1,
			first = Math.Max(0L, -line.Major0), // Clip to the texture on the major axis
			last = Math.Min(line.MajorLength, majorSize - 1L - line.Major0);
		if (first > last) return texture;
		// Start the usual incremental error term at the first step inside the texture instead of at step 0 so that clipping doesn't change which pixels are drawn.
		long minorOffset = line.MinorOffset(first, out long error);
		for (long step = first; ; step++)
		{
			long minor = line.Minor0 + line.MinorSign * minorOffset;
			if (minor >= 0 && minor < minorSize)
				WritePixel(texture, (int)(line.Major0 + step) * majorStride + (int)minor * minorStride, color);
			else if (line.MinorSign > 0 ? minor >= minorSize : minor < 0)
				break; // Left the texture on the minor axis and the line only moves farther away
			if (step == last) break;
			error += twoMinorLength;
			if (error >= twoMajorLength)
			{
				error -= twoMajorLength;
				minorOffset++;
			}
		}
		return texture;
	}
	/// <inheritdoc cref="DrawLine(Span{byte}, int, int, int, int, uint, ushort)"/>
	public static byte[] DrawLine(this byte[] texture, int xA, int yA, int xB, int yB, uint color = White, ushort width = 0)
	{
		texture.AsSpan().DrawLine(xA, yA, xB, yB, color, width);
		return texture;
	}
	/// <summary>
	/// Draws a circle using the midpoint circle algorithm, either as a 1 pixel wide outline or filled in.
	/// The center may be anywhere, including completely outside the texture in any direction. Only the part of the circle that overlaps the texture is drawn.
	/// The circle is (2 * radius + 1) pixels across, with the center pixel at the exact middle. A radius of 0 draws a single pixel.
	/// Each outline pixel is the nearest to the ideal circle on its octant's major axis. The fill covers exactly the pixels enclosed by that outline, including the outline.
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data to be modified</param>
	/// <param name="x">x of the center pixel</param>
	/// <param name="y">y of the center pixel</param>
	/// <param name="radius">distance in pixels from the center pixel to the outline pixels directly to its left, right, top and bottom</param>
	/// <param name="fill">true to fill in the circle, false to only draw its outline</param>
	/// <param name="color">rgba color to draw</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>same texture with circle drawn</returns>
	public static Span<byte> DrawCircle(this Span<byte> texture, int x, int y, ushort radius, bool fill = false, uint color = White, ushort width = 0)
	{
		int w = PixelWidth(texture.Length, width),
			h = PixelHeight(texture.Length, width);
		if (w < 1 || h < 1) return texture;
		// Draws one pixel, or nothing if it is outside the texture. Unlike DrawPixel, this already knows the size of the texture.
		// The span is a parameter because a local function can't capture a Span.
		void Plot(Span<byte> pixels, int px, int py)
		{
			if (px >= 0 && py >= 0 && px < w && py < h)
				WritePixel(pixels, (py * w + px) << 2, color);
		}
		// If the circle's bounding box misses the texture then there is nothing to draw.
		// Past this check the center is at most radius pixels outside the texture, so none of the int math below can overflow.
		if ((long)x + radius < 0 || (long)x - radius >= w || (long)y + radius < 0 || (long)y - radius >= h) return texture;
		// Walk one octant, from straight above the center to the 45 degree diagonal, and mirror every step into the other seven.
		// Work is bounded by the radius no matter how little of the circle is on the texture.
		int dx = 0,
			dy = radius,
			error = 1 - radius;
		while (dx <= dy)
		{
			int nextDx = dx + 1,
				nextDy = dy,
				nextError = error;
			if (error < 0)
				nextError += (nextDx << 1) + 1;
			else
			{
				nextDy--;
				nextError += ((nextDx - nextDy) << 1) + 1;
			}
			if (fill)
			{
				// Rows near the left and right sides get one span per step.
				texture.DrawRectangle(color, x - dy, y + dx, (dy << 1) + 1, 1, true, width);
				texture.DrawRectangle(color, x - dy, y - dx, (dy << 1) + 1, 1, true, width);
				// Rows near the top and bottom are shared by several steps, so only fill them once the widest step for that row is reached.
				if (nextDy != dy || nextDx > nextDy)
				{
					texture.DrawRectangle(color, x - dx, y + dy, (dx << 1) + 1, 1, true, width);
					texture.DrawRectangle(color, x - dx, y - dy, (dx << 1) + 1, 1, true, width);
				}
			}
			else
			{
				Plot(texture, x + dx, y + dy);
				Plot(texture, x - dx, y + dy);
				Plot(texture, x + dx, y - dy);
				Plot(texture, x - dx, y - dy);
				Plot(texture, x + dy, y + dx);
				Plot(texture, x - dy, y + dx);
				Plot(texture, x + dy, y - dx);
				Plot(texture, x - dy, y - dx);
			}
			dx = nextDx;
			dy = nextDy;
			error = nextError;
		}
		return texture;
	}
	/// <inheritdoc cref="DrawCircle(Span{byte}, int, int, ushort, bool, uint, ushort)"/>
	public static byte[] DrawCircle(this byte[] texture, int x, int y, ushort radius, bool fill = false, uint color = White, ushort width = 0)
	{
		texture.AsSpan().DrawCircle(x, y, radius, fill, color, width);
		return texture;
	}
	/// <summary>
	/// Draws an ellipse, either as a 1 pixel wide outline or filled in.
	/// The center may be anywhere, including completely outside the texture in any direction. Only the part of the ellipse that overlaps the texture is drawn.
	/// The ellipse is (2 * radiusX + 1) pixels wide and (2 * radiusY + 1) pixels tall, with the center pixel at the exact middle. A radius of 0 on an axis makes it a line, and 0 on both draws a single pixel.
	/// A pixel is part of the ellipse if the middle of its edge facing the center vertically, or the middle of its edge facing the center horizontally, is inside or on the ideal ellipse.
	/// The outline is every pixel of that shape that has a pixel outside the shape directly above, below, left or right of it.
	/// With equal radii this draws exactly the same pixels as <see cref="DrawCircle(Span{byte}, int, int, ushort, bool, uint, ushort)"/>.
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data to be modified</param>
	/// <param name="x">x of the center pixel</param>
	/// <param name="y">y of the center pixel</param>
	/// <param name="radiusX">distance in pixels from the center pixel to the outline pixels directly to its left and right</param>
	/// <param name="radiusY">distance in pixels from the center pixel to the outline pixels directly above and below it</param>
	/// <param name="fill">true to fill in the ellipse, false to only draw its outline</param>
	/// <param name="color">rgba color to draw</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>same texture with ellipse drawn</returns>
	public static Span<byte> DrawEllipse(this Span<byte> texture, int x, int y, ushort radiusX, ushort radiusY, bool fill = false, uint color = White, ushort width = 0)
	{
		int w = PixelWidth(texture.Length, width),
			h = PixelHeight(texture.Length, width);
		if (w < 1 || h < 1) return texture;
		// If the ellipse's bounding box misses the texture then there is nothing to draw.
		// Past this check the center is at most a radius pixels outside the texture, so none of the int math below can overflow.
		if ((long)x + radiusX < 0 || (long)x - radiusX >= w || (long)y + radiusY < 0 || (long)y - radiusY >= h) return texture;
		// Exact math needs more than 64 bits here: radius is up to 65535 so squaring both of them and multiplying by 4 reaches about 7.4E19.
		decimal radiusX2 = (decimal)radiusX * radiusX,
			radiusY2 = (decimal)radiusY * radiusY,
			limit = 4 * radiusX2 * radiusY2;
		// Is the point (doubleDx / 2, doubleDy / 2), relative to the center, inside or on the ellipse? Doubled so that pixel edge midpoints, which are half a pixel off, stay whole numbers.
		bool Inside(int doubleDx, int doubleDy) => radiusY2 * doubleDx * doubleDx + radiusX2 * doubleDy * doubleDy <= limit;
		// Walk the rows of the top half from the top down to the center row. The shape only gets wider as the rows get closer to the center, so both edge tests only ever move one way.
		int bottomEdgeDx = -1, // Widest pixel found so far by the test on the pixel's bottom edge middle
			leftEdgeDx = -1, // Widest pixel found so far by the test on the pixel's left edge middle
			extentAbove = -1; // The width of the row above, where -1 means no pixels
		for (int dy = radiusY; dy >= 0; dy--)
		{
			while (bottomEdgeDx < radiusX && Inside((bottomEdgeDx + 1) << 1, (dy << 1) - 1)) bottomEdgeDx++;
			while (leftEdgeDx < radiusX && Inside(((leftEdgeDx + 1) << 1) - 1, dy << 1)) leftEdgeDx++;
			int extent = Math.Max(bottomEdgeDx, leftEdgeDx), // This row goes from -extent to extent
				// Filled draws the whole row. The outline is only where the row sticks out past the row above.
				// Where it doesn't, the row's last pixel on each side is still outline.
				start = fill ? 0 : Math.Min(extentAbove + 1, extent);
			for (int mirror = 0; mirror < (dy > 0 ? 2 : 1); mirror++) // The row above the center and the same row below it
			{
				int row = mirror < 1 ? y + dy : y - dy;
				if (start < 1)
					texture.DrawRectangle(color, x - extent, row, (extent << 1) + 1, 1, true, width);
				else
				{
					texture.DrawRectangle(color, x - extent, row, extent - start + 1, 1, true, width);
					texture.DrawRectangle(color, x + start, row, extent - start + 1, 1, true, width);
				}
			}
			extentAbove = extent;
		}
		return texture;
	}
	/// <inheritdoc cref="DrawEllipse(Span{byte}, int, int, ushort, ushort, bool, uint, ushort)"/>
	public static byte[] DrawEllipse(this byte[] texture, int x, int y, ushort radiusX, ushort radiusY, bool fill = false, uint color = White, ushort width = 0)
	{
		texture.AsSpan().DrawEllipse(x, y, radiusX, radiusY, fill, color, width);
		return texture;
	}
	/// <summary>
	/// Draws a triangle, either as a 1 pixel wide outline or filled in.
	/// The points may be anywhere, including completely outside the texture in any direction. Only the part of the triangle that intersects the texture is drawn.
	/// The outline is exactly the three lines that <see cref="DrawLine(Span{byte}, int, int, int, int, uint, ushort)"/> would draw between the points.
	/// The fill covers everything between the outline's leftmost and rightmost pixels on each row, so it always includes the outline.
	/// The same pixels are drawn in whatever order the three points are given in. Points in a straight line draw that line.
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data to be modified</param>
	/// <param name="xA">x of the first point</param>
	/// <param name="yA">y of the first point</param>
	/// <param name="xB">x of the second point</param>
	/// <param name="yB">y of the second point</param>
	/// <param name="xC">x of the third point</param>
	/// <param name="yC">y of the third point</param>
	/// <param name="fill">true to fill in the triangle, false to only draw its outline</param>
	/// <param name="color">rgba color to draw</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>same texture with triangle drawn</returns>
	public static Span<byte> DrawTriangle(this Span<byte> texture, int xA, int yA, int xB, int yB, int xC, int yC, bool fill = false, uint color = White, ushort width = 0)
	{
		if (!fill)
			return texture
				.DrawLine(xA, yA, xB, yB, color, width)
				.DrawLine(xB, yB, xC, yC, color, width)
				.DrawLine(xC, yC, xA, yA, color, width);
		int w = PixelWidth(texture.Length, width),
			h = PixelHeight(texture.Length, width);
		if (w < 1 || h < 1) return texture;
		long minY = Math.Min(yA, Math.Min(yB, yC)),
			maxY = Math.Max(yA, Math.Max(yB, yC));
		if (Math.Max(xA, Math.Max(xB, xC)) < 0 || Math.Min(xA, Math.Min(xB, xC)) >= w || maxY < 0 || minY >= h) return texture;
		BresenhamLine edgeAB = new(xA, yA, xB, yB),
			edgeBC = new(xB, yB, xC, yC),
			edgeCA = new(xC, yC, xA, yA);
		// Only the rows on the texture are visited, so the work is bounded by the texture's height however large the triangle is.
		for (long row = Math.Max(0L, minY); row <= Math.Min(h - 1L, maxY); row++)
		{
			long left = long.MaxValue,
				right = long.MinValue;
			edgeAB.Widen(row, ref left, ref right);
			edgeBC.Widen(row, ref left, ref right);
			edgeCA.Widen(row, ref left, ref right);
			left = Math.Max(left, 0L);
			right = Math.Min(right, w - 1L);
			if (left <= right)
				texture.DrawRectangle(color, (int)left, (int)row, (int)(right - left) + 1, 1, true, width);
		}
		return texture;
	}
	/// <inheritdoc cref="DrawTriangle(Span{byte}, int, int, int, int, int, int, bool, uint, ushort)"/>
	public static byte[] DrawTriangle(this byte[] texture, int xA, int yA, int xB, int yB, int xC, int yC, bool fill = false, uint color = White, ushort width = 0)
	{
		texture.AsSpan().DrawTriangle(xA, yA, xB, yB, xC, yC, fill, color, width);
		return texture;
	}
	/// <summary>
	/// The rule for which pixels a Bresenham line puts on a grid, kept in one place so that everything that needs to know agrees.
	/// Everything is in (major, minor) coordinates, where major is the axis the line is longer on (y if <see cref="Steep"/>, otherwise x), counting from the lower to the higher end of the major axis.
	/// The pixel at major step i is i along the major axis and <see cref="MinorOffset"/>(i) along the minor axis, toward the other end of the line.
	/// Because the line always starts from the lower end, the same pixels come out whichever end the caller calls A.
	/// Where the ideal line is exactly halfway between two pixels, the pixel farther along the minor axis is chosen.
	/// <see cref="DrawLine(Span{byte}, int, int, int, int, uint, ushort)"/> walks the line with an incremental error term that must always agree with <see cref="MinorOffset"/>, and <see cref="Widen"/> answers per row questions from it without walking.
	/// </summary>
	private readonly struct BresenhamLine
	{
		/// <summary>
		/// true if y is the major axis
		/// </summary>
		public readonly bool Steep;
		/// <summary>
		/// Major and minor coordinates of the lower end of the line on the major axis
		/// </summary>
		public readonly long Major0, Minor0;
		/// <summary>
		/// Distances between the ends of the line along each axis. MajorLength is never smaller than MinorLength.
		/// </summary>
		public readonly long MajorLength, MinorLength;
		/// <summary>
		/// 1 if the minor coordinate increases along the line, otherwise -1
		/// </summary>
		public readonly int MinorSign;
		/// <summary>
		/// Lowest and highest y that the line reaches
		/// </summary>
		public readonly long FirstRow, LastRow;
		public BresenhamLine(int xA, int yA, int xB, int yB)
		{
			Steep = Math.Abs((long)yB - yA) > Math.Abs((long)xB - xA);
			long major1;
			Major0 = Steep ? yA : xA;
			major1 = Steep ? yB : xB;
			Minor0 = Steep ? xA : yA;
			long minor1 = Steep ? xB : yB;
			if (Major0 > major1)
			{
				(major1, Major0) = (Major0, major1);
				(minor1, Minor0) = (Minor0, minor1);
			}
			MajorLength = major1 - Major0;
			MinorLength = Math.Abs(minor1 - Minor0);
			MinorSign = minor1 >= Minor0 ? 1 : -1;
			FirstRow = Math.Min(yA, yB);
			LastRow = Math.Max(yA, yB);
		}
		/// <summary>
		/// The rule: how far along the minor axis the pixel at this step along the major axis is, which is floor((2 * MinorLength * step + MajorLength) / (2 * MajorLength)).
		/// </summary>
		/// <param name="step">steps along the major axis from the lower end of the line</param>
		/// <param name="error">what is left over from that division, which is where an incremental error term has to be at this step. It stays below 2 * MajorLength.</param>
		public long MinorOffset(long step, out long error)
		{
			if (step == 0)
			{ // The usual case, and the only one that is valid if the line is a single point
				error = MajorLength;
				return 0;
			}
			// decimal because 2 * MinorLength * step can need up to 65 bits
			decimal numerator = 2m * MinorLength * step + MajorLength,
				denominator = 2m * MajorLength,
				remainder = numerator % denominator;
			error = (long)remainder;
			return (long)((numerator - remainder) / denominator);
		}
		/// <summary>
		/// If the line has pixels on this row, widens left and right to include their columns.
		/// </summary>
		public void Widen(long row, ref long left, ref long right)
		{
			if (row < FirstRow || row > LastRow) return;
			long from, to;
			if (Steep)
				from = to = Minor0 + MinorSign * MinorOffset(row - Major0, out _); // One pixel per row
			else
			{ // A run of pixels on this row: all of the steps i where MinorOffset(i) is k, which is solving the rule for i
				long k = MinorSign * (row - Minor0),
					first = 0,
					last = MajorLength;
				if (MinorLength > 0)
				{
					first = Math.Max(first, CeilingDivide(2m * MajorLength * k - MajorLength, 2m * MinorLength));
					last = Math.Min(last, CeilingDivide(2m * MajorLength * k + MajorLength, 2m * MinorLength) - 1);
				}
				from = Major0 + first;
				to = Major0 + last;
			}
			left = Math.Min(left, from);
			right = Math.Max(right, to);
		}
		private static long CeilingDivide(decimal numerator, decimal denominator)
		{
			decimal remainder = numerator % denominator,
				quotient = (numerator - remainder) / denominator; // Exact, rounded toward zero
			return (long)(remainder > 0m ? quotient + 1m : quotient);
		}
	}
	public static Span<byte> DrawBoundingBox(this Span<byte> texture, ushort width = 0)
	{
		width = (ushort)PixelWidth(texture.Length, width);
		ushort height = Height(length: texture.Length, width: width);
		return texture.DrawRectangle(
				x: 0,
				y: 0,
				color: Yellow,
				rectWidth: (ushort)(width - 1),
				width: width)
			.DrawRectangle(
				x: 1,
				y: (ushort)(height - 1),
				color: Red,
				rectWidth: (ushort)(width - 1),
				width: width)
			.DrawRectangle(
				x: 0,
				y: 1,
				color: Blue,
				rectHeight: (ushort)(height - 1),
				width: width)
			.DrawRectangle(
				x: (ushort)(width - 1),
				y: 0,
				color: Green,
				rectHeight: (ushort)(height - 1),
				width: width);
	}
	/// <inheritdoc cref="DrawBoundingBox(Span{byte}, ushort)"/>
	public static byte[] DrawBoundingBox(this byte[] texture, ushort width = 0)
	{
		texture.AsSpan().DrawBoundingBox(width);
		return texture;
	}
	/// <summary>
	/// Works out which part of an insert goes where when it is drawn onto a texture at some position that may be partly or completely outside of it.
	/// Everything is in bytes, with four to a pixel, and rows are in whole pixels.
	/// </summary>
	private readonly struct InsertClip
	{
		/// <summary>
		/// false if none of the insert is on the texture
		/// </summary>
		public readonly bool Visible;
		/// <summary>
		/// true if the insert is as wide as the texture and not moved sideways, so that its rows can be copied as one block
		/// </summary>
		public readonly bool FullWidth;
		/// <summary>
		/// Index of the first pixel to draw in the texture, and of the first pixel to draw from in the insert
		/// </summary>
		public readonly int Start, InsertStart;
		/// <summary>
		/// Index of the start of the first row to draw on in the texture, and of the first row to draw from in the insert
		/// </summary>
		public readonly int RowStart, InsertRowStart;
		/// <summary>
		/// Bytes in one row of the texture, and in one row of the insert
		/// </summary>
		public readonly int TextureRowLength, InsertRowLength;
		/// <summary>
		/// Bytes of each row of the insert that are drawn
		/// </summary>
		public readonly int RowLength;
		public InsertClip(int textureLength, ushort width, int insertLength, ushort insertWidth, int x, int y)
		{
			int insertX = 0, insertY = 0;
			if (x < 0)
			{
				insertX = -x;
				insertX <<= 2;
				x = 0;
			}
			if (y < 0)
			{
				insertY = -y;
				y = 0;
			}
			TextureRowLength = PixelWidth(textureLength, width) << 2;
			x <<= 2; // x *= 4;
			InsertRowLength = PixelWidth(insertLength, insertWidth) << 2;
			RowLength = (x + InsertRowLength > TextureRowLength ? TextureRowLength - x : InsertRowLength) - insertX;
			Visible = x <= TextureRowLength && y <= PixelHeight(textureLength, width);
			FullWidth = TextureRowLength == InsertRowLength && x == 0 && insertX == 0;
			RowStart = y * TextureRowLength;
			InsertRowStart = insertY * InsertRowLength;
			Start = RowStart + x;
			InsertStart = InsertRowStart + insertX;
		}
	}
	/// <summary>
	/// Draws a texture onto a different texture
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data to be modified</param>
	/// <param name="x">upper left corner of where to insert</param>
	/// <param name="y">upper left corner of where to insert</param>
	/// <param name="insert">raw rgba888 pixel data to insert</param>
	/// <param name="insertWidth">width of insert or 0 to assume square texture</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>same texture with insert drawn</returns>
	public static Span<byte> DrawInsert(this Span<byte> texture, int x, int y, ReadOnlySpan<byte> insert, ushort insertWidth = 0, ushort width = 0)
	{
		InsertClip clip = new(texture.Length, width, insert.Length, insertWidth, x, y);
		if (!clip.Visible) return texture;
		if (clip.FullWidth)
			insert.Slice(clip.InsertRowStart, Math.Min(insert.Length - clip.InsertRowStart, texture.Length - clip.RowStart))
				.CopyTo(texture[clip.RowStart..]);
		else
			for (int y1 = clip.Start, y2 = clip.InsertStart; y1 + clip.RowLength <= texture.Length && y2 < insert.Length; y1 += clip.TextureRowLength, y2 += clip.InsertRowLength)
				insert.Slice(y2, clip.RowLength).CopyTo(texture[y1..]);
		return texture;
	}
	/// <inheritdoc cref="DrawInsert(Span{byte}, int, int, ReadOnlySpan{byte}, ushort, ushort)"/>
	public static byte[] DrawInsert(this byte[] texture, int x, int y, ReadOnlySpan<byte> insert, ushort insertWidth = 0, ushort width = 0)
	{
		texture.AsSpan().DrawInsert(x, y, insert, insertWidth, width);
		return texture;
	}
	/// <summary>
	/// Draws a texture onto a different texture with simple transparency
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data to be modified</param>
	/// <param name="x">upper left corner of where to insert</param>
	/// <param name="y">upper left corner of where to insert</param>
	/// <param name="insert">raw rgba888 pixel data to insert</param>
	/// <param name="insertWidth">width of insert or 0 to assume square texture</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <param name="threshold">only draws pixel if alpha is higher than or equal to threshold</param>
	/// <returns>same texture with insert drawn</returns>
	public static Span<byte> DrawTransparentInsert(this Span<byte> texture, int x, int y, ReadOnlySpan<byte> insert, ushort insertWidth = 0, ushort width = 0, byte threshold = DefaultTransparencyThreshold)
	{
		InsertClip clip = new(texture.Length, width, insert.Length, insertWidth, x, y);
		if (!clip.Visible) return texture;
		ReadOnlySpan<uint> from = AsPixels(insert);
		Span<uint> to = AsPixels(texture);
		for (int y1 = clip.Start, y2 = clip.InsertStart; y1 < texture.Length && y2 < insert.Length; y1 += clip.TextureRowLength, y2 += clip.InsertRowLength)
			for (int x1 = 0; x1 < clip.RowLength; x1 += 4)
				if (insert[y2 + x1 + 3] >= threshold) // The alpha byte is the same byte whatever order the machine stores a uint in
					to[(y1 + x1) >> 2] = from[(y2 + x1) >> 2];
		return texture;
	}
	/// <inheritdoc cref="DrawTransparentInsert(Span{byte}, int, int, ReadOnlySpan{byte}, ushort, ushort, byte)"/>
	public static byte[] DrawTransparentInsert(this byte[] texture, int x, int y, ReadOnlySpan<byte> insert, ushort insertWidth = 0, ushort width = 0, byte threshold = DefaultTransparencyThreshold)
	{
		texture.AsSpan().DrawTransparentInsert(x, y, insert, insertWidth, width, threshold);
		return texture;
	}
	/// <summary>
	/// Draws 1 pixel wide padding around the outside of a rectangular area by copying pixels from the edges of the area
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data to be modified</param>
	/// <param name="x">upper left corner of area</param>
	/// <param name="y">upper left corner of area</param>
	/// <param name="areaWidth">width of area</param>
	/// <param name="areaHeight">height of area or 0 to assume square area</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>same texture with padding drawn</returns>
	public static Span<byte> DrawPadding(this Span<byte> texture, ushort x, ushort y, ushort areaWidth, ushort areaHeight, ushort width = 0)
	{
		if (areaHeight < 1) areaHeight = areaWidth;
		int xSide = PixelWidth(texture.Length, width) << 2,
			ySide = PixelHeight(texture.Length, width),
			x4 = x * 4,
			offset = y > 0 ? (y - 1) * xSide + x4 : x4,
			stop = Math.Min((y + areaHeight) * xSide + x4, texture.Length),
			areaWidth4 = areaWidth << 2;
		if (x4 > xSide || offset > texture.Length)
			return texture;
		if (y > 0)
			texture.Slice(offset + xSide, Math.Min(areaWidth4, xSide - x4))
				.CopyTo(texture[offset..]);
		if (y + areaHeight < ySide)
			texture.Slice(stop - xSide, Math.Min(areaWidth4, xSide - x4))
				.CopyTo(texture[stop..]);
		for (int y1 = Math.Max(x4, offset); y1 <= stop; y1 += xSide)
		{
			if (x > 0)
				texture.Slice(y1, 4).CopyTo(texture[(y1 - 4)..]);
			if (x4 + areaWidth4 < xSide)
				texture.Slice(y1 + areaWidth4 - 4, 4).CopyTo(texture[(y1 + areaWidth4)..]);
		}
		return texture;
	}
	/// <inheritdoc cref="DrawPadding(Span{byte}, ushort, ushort, ushort, ushort, ushort)"/>
	public static byte[] DrawPadding(this byte[] texture, ushort x, ushort y, ushort areaWidth, ushort areaHeight, ushort width = 0)
	{
		texture.AsSpan().DrawPadding(x, y, areaWidth, areaHeight, width);
		return texture;
	}
	#endregion Drawing
	/// <summary>
	/// Views raw rgba8888 pixel data as one uint per pixel, so that a whole pixel can be moved with one read and one write, which is several times faster than copying four bytes.
	/// The byte order inside those uints depends on the machine, so they should only be moved around and compared with each other, never taken apart or built from a color.
	/// Any bytes past the last whole pixel are left out.
	/// </summary>
	private static ReadOnlySpan<uint> AsPixels(ReadOnlySpan<byte> texture) => MemoryMarshal.Cast<byte, uint>(texture);
	/// <inheritdoc cref="AsPixels(ReadOnlySpan{byte})"/>
	private static Span<uint> AsPixels(Span<byte> texture) => MemoryMarshal.Cast<byte, uint>(texture);
	#region Rotation
	/// <summary>
	/// Flips an image on the X axis
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>new raw rgba8888 pixel data of identical size to source texture</returns>
	public static byte[] FlipX(this ReadOnlySpan<byte> texture, ushort width = 0)
	{
		int xSide = PixelWidth(texture.Length, width) << 2;
		byte[] flipped = new byte[texture.Length];
		for (int y = 0; y < flipped.Length; y += xSide)
			texture.Slice(y, xSide).CopyTo(flipped.AsSpan(flipped.Length - xSide - y));
		return flipped;
	}
	/// <summary>
	/// Flips an image on the Y axis
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>new raw rgba8888 pixel data of identical size to source texture</returns>
	public static byte[] FlipY(this ReadOnlySpan<byte> texture, ushort width = 0)
	{
		int w = PixelWidth(texture.Length, width);
		byte[] flipped = new byte[texture.Length];
		ReadOnlySpan<uint> from = AsPixels(texture);
		Span<uint> to = AsPixels(flipped.AsSpan());
		for (int y = 0; y < from.Length; y += w)
			for (int x = 0; x < w; x++)
				to[y + w - 1 - x] = from[y + x];
		return flipped;
	}
	/// <summary>
	/// Based on https://iiif.io/api/annex/notes/rotation/
	/// </summary>
	public static void RotatedSize(ushort width, ushort height, out ushort rotatedWidth, out ushort rotatedHeight, double radians = 0d, byte scaleX = 1, byte scaleY = 1)
	{
		if (scaleX < 1) throw new ArgumentOutOfRangeException(nameof(scaleX));
		if (scaleY < 1) throw new ArgumentOutOfRangeException(nameof(scaleY));
		if (width > ushort.MaxValue / scaleX)
			throw new OverflowException("Scaled width exceeds maximum allowed size.");
		if (height > ushort.MaxValue / scaleY)
			throw new OverflowException("Scaled height exceeds maximum allowed size.");
		uint scaledWidth = (uint)(width * scaleX),
			scaledHeight = (uint)(height * scaleY);
		radians %= Tau;
		double absCos = Math.Abs(Math.Cos(radians)),
			absSin = Math.Abs(Math.Sin(radians));
		uint rWidth = (uint)(scaledWidth * absCos + scaledHeight * absSin),
			rHeight = (uint)(scaledWidth * absSin + scaledHeight * absCos);
		if (rWidth > ushort.MaxValue)
			throw new OverflowException("Rotated width exceeds maximum allowed size.");
		rotatedWidth = (ushort)rWidth;
		if (rHeight > ushort.MaxValue)
			throw new OverflowException("Rotated height exceeds maximum allowed size.");
		rotatedHeight = (ushort)rHeight;
	}
	public static void RotatedLocate(ushort width, ushort height, int x, int y, out int rotatedX, out int rotatedY, double radians = 0d, byte scaleX = 1, byte scaleY = 1)
	{
		if (scaleX < 1) throw new ArgumentOutOfRangeException(nameof(scaleX));
		if (scaleY < 1) throw new ArgumentOutOfRangeException(nameof(scaleY));
		if (width > ushort.MaxValue / scaleX)
			throw new OverflowException("Scaled width exceeds maximum allowed size.");
		if (height > ushort.MaxValue / scaleY)
			throw new OverflowException("Scaled height exceeds maximum allowed size.");
		radians %= Tau;
		double cos = Math.Cos(radians),
			sin = Math.Sin(radians),
			absCos = Math.Abs(cos),
			absSin = Math.Abs(sin);
		ushort scaledWidth = (ushort)(width * scaleX),
			scaledHeight = (ushort)(height * scaleY),
			rotatedWidth = (ushort)(scaledWidth * absCos + scaledHeight * absSin),
			rotatedHeight = (ushort)(scaledWidth * absSin + scaledHeight * absCos),
			halfRotatedWidth = (ushort)(rotatedWidth >> 1),
			halfRotatedHeight = (ushort)(rotatedHeight >> 1);
		double offsetX = (scaledWidth >> 1) - cos * halfRotatedWidth - sin * halfRotatedHeight,
			offsetY = (scaledHeight >> 1) - cos * halfRotatedHeight + sin * halfRotatedWidth;
		rotatedX = (int)(cos * (x * scaleX - offsetX) - sin * (y * scaleY - offsetY));
		rotatedY = (int)(sin * (x * scaleX - offsetX) + cos * (y * scaleY - offsetY));
	}
	/// <summary>
	/// Rotates a texture after optionally upscaling in one operation so that the resulting upscaled texture is aligned so that its leftmost pixel is on x=0 and its highest pixel is on y=0.
	/// </summary>
	public static byte[] Rotate(this ReadOnlySpan<byte> texture, out ushort rotatedWidth, out ushort rotatedHeight, double radians = 0d, byte scaleX = 1, byte scaleY = 1, ushort width = 0)
	{
		if (scaleX < 1) throw new ArgumentOutOfRangeException(nameof(scaleX));
		if (scaleY < 1) throw new ArgumentOutOfRangeException(nameof(scaleY));
		width = (ushort)PixelWidth(texture.Length, width);
		if (width > ushort.MaxValue / scaleX)
			throw new OverflowException("Scaled width exceeds maximum allowed size.");
		ushort height = Height(texture.Length, width);
		if (height > ushort.MaxValue / scaleY)
			throw new OverflowException("Scaled height exceeds maximum allowed size.");
		ushort scaledWidth = (ushort)(width * scaleX),
			scaledHeight = (ushort)(height * scaleY),
			halfScaledWidth = (ushort)(scaledWidth >> 1),
			halfScaledHeight = (ushort)(scaledHeight >> 1);
		radians %= Tau;
		double cos = Math.Cos(radians),
			sin = Math.Sin(radians),
			absCos = Math.Abs(cos),
			absSin = Math.Abs(sin);
		uint rWidth = (uint)(scaledWidth * absCos + scaledHeight * absSin),
			rHeight = (uint)(scaledWidth * absSin + scaledHeight * absCos);
		if (rWidth > ushort.MaxValue)
			throw new OverflowException("Rotated width exceeds maximum allowed size.");
		if (rHeight > ushort.MaxValue)
			throw new OverflowException("Rotated height exceeds maximum allowed size.");
		if (rWidth * rHeight > int.MaxValue >> 2)
			throw new OverflowException("Resulting image would be too large to allocate.");
		rotatedWidth = (ushort)rWidth;
		rotatedHeight = (ushort)rHeight;
		ushort halfRotatedWidth = (ushort)(rotatedWidth >> 1),
			halfRotatedHeight = (ushort)(rotatedHeight >> 1);
		double offsetX = halfScaledWidth - cos * halfRotatedWidth - sin * halfRotatedHeight,
			offsetY = halfScaledHeight - cos * halfRotatedHeight + sin * halfRotatedWidth;
		//Pre-calculate rotated corners in clockwise order:
		//0 = top-left (0,0)
		//1 = top-right (w,0)
		//2 = bottom-right (w,h)
		//3 = bottom-left (0,h)
		bool isNearZero = absCos < 1e-10 || absSin < 1e-10;
		double[] cornerX = isNearZero ? null : [-halfScaledWidth, halfScaledWidth, halfScaledWidth, -halfScaledWidth],
			cornerY = isNearZero ? null : [-halfScaledHeight, -halfScaledHeight, halfScaledHeight, halfScaledHeight];
		if (!isNearZero)
			for (byte cornerIndex = 0; cornerIndex < 4; cornerIndex++)
			{
				double unrotatedX = cornerX[cornerIndex];
				cornerX[cornerIndex] = unrotatedX * cos - cornerY[cornerIndex] * sin + halfRotatedWidth;
				cornerY[cornerIndex] = unrotatedX * sin + cornerY[cornerIndex] * cos + halfRotatedHeight;
			}
		byte[] rotated = new byte[rotatedWidth * rotatedHeight << 2];
		for (ushort y = 0; y < rotatedHeight; y++)
		{
			ushort startX = 0, endX = (ushort)(rotatedWidth - 1);
			if (!isNearZero)
			{
				double? minX = null, maxX = null;
				for (byte cornerIndex = 0; cornerIndex < 4; cornerIndex++)
				{//Check each edge of the rectangle
					if (Math.Abs(cornerY[cornerIndex] - y) <= 0.5d)
					{//If this corner is on this scanline
						minX = minX.HasValue ? Math.Min(minX.Value, cornerX[cornerIndex]) : cornerX[cornerIndex];
						maxX = maxX.HasValue ? Math.Max(maxX.Value, cornerX[cornerIndex]) : cornerX[cornerIndex];
					}
					byte nextCornerIndex = (byte)((cornerIndex + 1) % 4);
					double currentY = cornerY[cornerIndex],
						nextY = cornerY[nextCornerIndex];
					if ((currentY <= y && nextY >= y) || (currentY >= y && nextY <= y))
					{//If the edge crosses this scanline
						if (Math.Abs(nextY - currentY) > 1e-10)
						{//Only calculate intersection if denominator isn't zero
							double intersectX = cornerX[cornerIndex]
								+ (y - currentY) / (nextY - currentY)
								* (cornerX[nextCornerIndex] - cornerX[cornerIndex]);
							minX = minX.HasValue ? Math.Min(minX.Value, intersectX) : intersectX;
							maxX = maxX.HasValue ? Math.Max(maxX.Value, intersectX) : intersectX;
						}
					}
				}
				if (minX.HasValue) startX = (ushort)Math.Floor(minX.Value);
				if (maxX.HasValue) endX = (ushort)Math.Ceiling(maxX.Value);
			}
			for (ushort x = startX; x <= endX; x++)
			{
				double sourceX = (x * cos + y * sin + offsetX) / scaleX,
					sourceY = (y * cos - x * sin + offsetY) / scaleY;
				if (sourceX >= 0d && sourceX < width && sourceY >= 0d && sourceY < height)
					rotated.DrawPixel(
						x: x,
						y: y,
						color: texture.Pixel(
							x: (ushort)Math.Floor(sourceX),
							y: (ushort)Math.Floor(sourceY),
							width: width),
						width: rotatedWidth);
			}
		}
		return rotated;
	}
	/// <summary>
	/// Rotates image clockwise by 45 degrees
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>new raw rgba8888 pixel data of newWidth = width + height - 1</returns>
	public static byte[] RotateClockwise45Thin(this ReadOnlySpan<byte> texture, ushort width = 0)
	{
		int w = PixelWidth(texture.Length, width),
			h = PixelHeight(texture.Length, width),
			newWidth = w + h - 1;
		byte[] rotated = new byte[newWidth * (w + h) << 2];
		ReadOnlySpan<uint> from = AsPixels(texture);
		Span<uint> to = AsPixels(rotated.AsSpan());
		for (int y1 = from.Length - w, y2 = newWidth * h - newWidth; y1 < from.Length; y1++, y2 += newWidth + 1)
			for (int x1 = y1, x2 = y2; x1 >= 0; x1 -= w, x2 += -newWidth + 1)
			{
				to[x2] = from[x1];
				to[x2 + newWidth] = from[x1];
			}
		return rotated;
	}
	/// <summary>
	/// Rotates image clockwise by 45 degrees
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>new raw rgba8888 pixel data of newWidth = width + height</returns>
	public static byte[] RotateClockwise45(this ReadOnlySpan<byte> texture, ushort width = 0)
	{
		int w = PixelWidth(texture.Length, width),
			h = PixelHeight(texture.Length, width),
			newWidth = w + h;
		byte[] rotated = new byte[newWidth * (w + h - 1) << 2];
		ReadOnlySpan<uint> from = AsPixels(texture);
		Span<uint> to = AsPixels(rotated.AsSpan());
		for (int y1 = from.Length - w, y2 = newWidth * h - newWidth; y1 < from.Length; y1++, y2 += newWidth + 1)
			for (int x1 = y1, x2 = y2; x1 >= 0; x1 -= w, x2 += -newWidth + 1)
			{
				to[x2] = from[x1];
				to[x2 + 1] = from[x1];
			}
		return rotated;
	}
	/// <summary>
	/// Rotates image clockwise by 90 degrees
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>new raw rgba8888 pixel data where its width is the height of the source texture</returns>
	public static byte[] RotateClockwise90(this ReadOnlySpan<byte> texture, ushort width = 0)
	{
		int w = PixelWidth(texture.Length, width),
			h = PixelHeight(texture.Length, width);
		byte[] rotated = new byte[texture.Length];
		ReadOnlySpan<uint> from = AsPixels(texture);
		Span<uint> to = AsPixels(rotated.AsSpan());
		for (int y1 = 0, y2 = h - 1; y1 < from.Length; y1 += w, y2--)
			for (int x1 = y1, x2 = y2; x1 < y1 + w; x1++, x2 += h)
				to[x2] = from[x1];
		return rotated;
	}
	/// <summary>
	/// Rotates image clockwise by 135 degrees
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>new raw rgba8888 pixel data of newWidth = width + height - 1</returns>
	public static byte[] RotateClockwise135Thin(this ReadOnlySpan<byte> texture, ushort width = 0)
	{
		int w = PixelWidth(texture.Length, width),
			h = PixelHeight(texture.Length, width),
			newWidth = w + h - 1;
		byte[] rotated = new byte[newWidth * (w + h) << 2];
		ReadOnlySpan<uint> from = AsPixels(texture);
		Span<uint> to = AsPixels(rotated.AsSpan());
		for (int y1 = from.Length - 1, y2 = newWidth * w - newWidth; y1 >= 0; y1 -= w, y2 += newWidth + 1)
			for (int x1 = y1, x2 = y2; x1 > y1 - w; x1--, x2 += -newWidth + 1)
			{
				to[x2] = from[x1];
				to[x2 + newWidth] = from[x1];
			}
		return rotated;
	}
	/// <summary>
	/// Rotates image clockwise by 135 degrees
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>new raw rgba8888 pixel data of newWidth = width + height</returns>
	public static byte[] RotateClockwise135(this ReadOnlySpan<byte> texture, ushort width = 0)
	{
		int w = PixelWidth(texture.Length, width),
			h = PixelHeight(texture.Length, width),
			newWidth = w + h;
		byte[] rotated = new byte[newWidth * (w + h - 1) << 2];
		ReadOnlySpan<uint> from = AsPixels(texture);
		Span<uint> to = AsPixels(rotated.AsSpan());
		for (int y1 = from.Length - 1, y2 = newWidth * w - newWidth; y1 >= 0; y1 -= w, y2 += newWidth + 1)
			for (int x1 = y1, x2 = y2; x1 > y1 - w; x1--, x2 += -newWidth + 1)
			{
				to[x2] = from[x1];
				to[x2 + 1] = from[x1];
			}
		return rotated;
	}
	/// <summary>
	/// Rotates an image 180 degrees
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>new raw rgba8888 pixel data of identical size to source texture</returns>
	public static byte[] Rotate180(this ReadOnlySpan<byte> texture, ushort width = 0)
	{
		int w = PixelWidth(texture.Length, width);
		byte[] rotated = new byte[texture.Length];
		ReadOnlySpan<uint> from = AsPixels(texture);
		Span<uint> to = AsPixels(rotated.AsSpan());
		for (int y1 = 0, y2 = from.Length - w; y1 < from.Length; y1 += w, y2 -= w)
			for (int x1 = y1, x2 = y2 + w - 1; x1 < y1 + w; x1++, x2--)
				to[x2] = from[x1];
		return rotated;
	}
	/// <summary>
	/// Rotates image counter-clockwise by 135 degrees
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>new raw rgba8888 pixel data of newWidth = width + height - 1</returns>
	public static byte[] RotateCounter135Thin(this ReadOnlySpan<byte> texture, ushort width = 0)
	{
		int w = PixelWidth(texture.Length, width),
			h = PixelHeight(texture.Length, width),
			newWidth = w + h - 1;
		byte[] rotated = new byte[newWidth * (w + h) << 2];
		ReadOnlySpan<uint> from = AsPixels(texture);
		Span<uint> to = AsPixels(rotated.AsSpan());
		for (int y1 = w - 1, y2 = newWidth * h - newWidth; y1 >= 0; y1--, y2 += newWidth + 1)
			for (int x1 = y1, x2 = y2; x1 < from.Length; x1 += w, x2 += -newWidth + 1)
			{
				to[x2] = from[x1];
				to[x2 + newWidth] = from[x1];
			}
		return rotated;
	}
	/// <summary>
	/// Rotates image counter-clockwise by 135 degrees
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>new raw rgba8888 pixel data of newWidth = width + height</returns>
	public static byte[] RotateCounter135(this ReadOnlySpan<byte> texture, ushort width = 0)
	{
		int w = PixelWidth(texture.Length, width),
			h = PixelHeight(texture.Length, width),
			newWidth = w + h;
		byte[] rotated = new byte[newWidth * (w + h - 1) << 2];
		ReadOnlySpan<uint> from = AsPixels(texture);
		Span<uint> to = AsPixels(rotated.AsSpan());
		for (int y1 = w - 1, y2 = newWidth * h - newWidth; y1 >= 0; y1--, y2 += newWidth + 1)
			for (int x1 = y1, x2 = y2; x1 < from.Length; x1 += w, x2 += -newWidth + 1)
			{
				to[x2] = from[x1];
				to[x2 + 1] = from[x1];
			}
		return rotated;
	}
	/// <summary>
	/// Rotates image counter-clockwise by 90 degrees
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>new raw rgba8888 pixel data where its width is the height of the source texture</returns>
	public static byte[] RotateCounter90(this ReadOnlySpan<byte> texture, ushort width = 0)
	{
		int w = PixelWidth(texture.Length, width),
			h = PixelHeight(texture.Length, width);
		byte[] rotated = new byte[texture.Length];
		ReadOnlySpan<uint> from = AsPixels(texture);
		Span<uint> to = AsPixels(rotated.AsSpan());
		for (int y1 = 0, y2 = from.Length - h; y1 < from.Length; y1 += w, y2++)
			for (int x1 = y1, x2 = y2; x1 < y1 + w; x1++, x2 -= h)
				to[x2] = from[x1];
		return rotated;
	}
	/// <summary>
	/// Rotates image counter-clockwise by 45 degrees
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>new raw rgba8888 pixel data of newWidth = width + height - 1</returns>
	public static byte[] RotateCounter45Thin(this ReadOnlySpan<byte> texture, ushort width = 0)
	{
		int w = PixelWidth(texture.Length, width),
			h = PixelHeight(texture.Length, width),
			newWidth = w + h - 1;
		byte[] rotated = new byte[newWidth * (w + h) << 2];
		ReadOnlySpan<uint> from = AsPixels(texture);
		Span<uint> to = AsPixels(rotated.AsSpan());
		for (int y1 = 0, y2 = newWidth * w - newWidth; y1 < from.Length; y1 += w, y2 += newWidth + 1)
			for (int x1 = y1, x2 = y2; x1 < y1 + w; x1++, x2 += -newWidth + 1)
			{
				to[x2] = from[x1];
				to[x2 + newWidth] = from[x1];
			}
		return rotated;
	}
	/// <summary>
	/// Rotates image counter-clockwise by 45 degrees
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>new raw rgba8888 pixel data of newWidth = width + height</returns>
	public static byte[] RotateCounter45(this ReadOnlySpan<byte> texture, ushort width = 0)
	{
		int w = PixelWidth(texture.Length, width),
			h = PixelHeight(texture.Length, width),
			newWidth = w + h;
		byte[] rotated = new byte[newWidth * (w + h - 1) << 2];
		ReadOnlySpan<uint> from = AsPixels(texture);
		Span<uint> to = AsPixels(rotated.AsSpan());
		for (int y1 = 0, y2 = newWidth * w - newWidth; y1 < from.Length; y1 += w, y2 += newWidth + 1)
			for (int x1 = y1, x2 = y2; x1 < y1 + w; x1++, x2 += -newWidth + 1)
			{
				to[x2] = from[x1];
				to[x2 + 1] = from[x1];
			}
		return rotated;
	}
	#endregion Rotation
	#region Isometric
	/// <summary>
	/// Skews an image for use as an isometric wall tile sloping down
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>new raw rgba8888 pixel data of double the source texture width</returns>
	public static byte[] IsoSlantDown(this ReadOnlySpan<byte> texture, ushort width = 0)
	{
		int w = PixelWidth(texture.Length, width),
			h = PixelHeight(texture.Length, width),
			newWidth = w * 2;
		byte[] slanted = new byte[newWidth * (h * 2 + w + 1) << 2];
		ReadOnlySpan<uint> from = AsPixels(texture);
		Span<uint> to = AsPixels(slanted.AsSpan());
		for (int y1 = 0, y2 = 0; y1 < from.Length; y1 += w, y2 += newWidth * 2)
			for (int x1 = y1, x2 = y2; x1 < y1 + w; x1++, x2 += newWidth + 2)
			{
				to[x2] = from[x1];
				to[x2 + newWidth] = from[x1];
				to[x2 + newWidth + 1] = from[x1];
				to[x2 + newWidth * 2] = from[x1];
				to[x2 + newWidth * 2 + 1] = from[x1];
				to[x2 + newWidth * 3 + 1] = from[x1];
			}
		return slanted;
	}
	/// <summary>
	/// Skews an image for use as a short isometric wall tile sloping down
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>new raw rgba8888 pixel data of double the source texture width</returns>
	public static byte[] IsoSlantDownShort(this ReadOnlySpan<byte> texture, ushort width = 0)
	{
		int w = PixelWidth(texture.Length, width),
			h = PixelHeight(texture.Length, width),
			newWidth = w * 2;
		byte[] slanted = new byte[newWidth * (w + 1 + h) << 2];
		ReadOnlySpan<uint> from = AsPixels(texture);
		Span<uint> to = AsPixels(slanted.AsSpan());
		for (int y1 = 0, y2 = 0; y1 < from.Length; y1 += w, y2 += newWidth)
			for (int x1 = y1, x2 = y2; x1 < y1 + w; x1++, x2 += newWidth + 2)
			{
				to[x2] = from[x1];
				to[x2 + newWidth] = from[x1];
				to[x2 + newWidth + 1] = from[x1];
				to[x2 + newWidth * 2 + 1] = from[x1];
			}
		return slanted;
	}
	/// <summary>
	/// Skews an image for use as an isometric wall tile sloping up
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>new raw rgba8888 pixel data of double the source texture width</returns>
	public static byte[] IsoSlantUp(this ReadOnlySpan<byte> texture, ushort width = 0)
	{
		int w = PixelWidth(texture.Length, width),
			h = PixelHeight(texture.Length, width),
			newWidth = w * 2;
		byte[] slanted = new byte[newWidth * (h * 2 + w + 1) << 2];
		ReadOnlySpan<uint> from = AsPixels(texture);
		Span<uint> to = AsPixels(slanted.AsSpan());
		for (int y1 = 0, y2 = newWidth * (w + 2); y1 < from.Length; y1 += w, y2 += newWidth * 2)
			for (int x1 = y1, x2 = y2; x1 < y1 + w; x1++, x2 += -newWidth + 2)
			{
				to[x2] = from[x1];
				to[x2 - newWidth] = from[x1];
				to[x2 - newWidth + 1] = from[x1];
				to[x2 - newWidth * 2] = from[x1];
				to[x2 - newWidth * 2 + 1] = from[x1];
				to[x2 - newWidth * 3 + 1] = from[x1];
			}
		return slanted;
	}
	/// <summary>
	/// Skews an image for use as a short isometric wall tile sloping up
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>new raw rgba8888 pixel data of double the source texture width</returns>
	public static byte[] IsoSlantUpShort(this ReadOnlySpan<byte> texture, ushort width = 0)
	{
		int w = PixelWidth(texture.Length, width),
			h = PixelHeight(texture.Length, width),
			newWidth = w * 2;
		byte[] slanted = new byte[newWidth * (w + 1 + h) << 2];
		ReadOnlySpan<uint> from = AsPixels(texture);
		Span<uint> to = AsPixels(slanted.AsSpan());
		for (int y1 = 0, y2 = newWidth * (w + 1); y1 < from.Length; y1 += w, y2 += newWidth)
			for (int x1 = y1, x2 = y2; x1 < y1 + w; x1++, x2 += -newWidth + 2)
			{
				to[x2] = from[x1];
				to[x2 - newWidth] = from[x1];
				to[x2 - newWidth + 1] = from[x1];
				to[x2 - newWidth * 2 + 1] = from[x1];
			}
		return slanted;
	}
	/// <summary>
	/// Rotates and stretches an image for use as an isometric floor tile
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>new raw rgba8888 pixel data where the width is derived from the source image size by the formula "newWidth = (width + height - 1) * 2"</returns>
	public static byte[] IsoTile(this ReadOnlySpan<byte> texture, ushort width = 0)
	{
		int w = PixelWidth(texture.Length, width),
			h = PixelHeight(texture.Length, width),
			newWidth = (w + h) * 2 - 2;
		byte[] tile = new byte[newWidth * (w + h) << 2];
		ReadOnlySpan<uint> from = AsPixels(texture);
		Span<uint> to = AsPixels(tile.AsSpan());
		for (int y1 = 0, y2 = newWidth * w - newWidth; y1 < from.Length; y1 += w, y2 += newWidth + 2)
			for (int x1 = y1, x2 = y2; x1 < y1 + w; x1++, x2 += -newWidth + 2)
			{
				to[x2] = from[x1];
				to[x2 + 1] = from[x1];
				to[x2 + newWidth] = from[x1];
				to[x2 + newWidth + 1] = from[x1];
			}
		return tile;
	}
	#endregion Isometric
	#region Image manipulation
	/// <summary>
	/// Extracts a rectangular piece of a texture
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="x">upper left corner of selection</param>
	/// <param name="y">upper left corner of selection</param>
	/// <param name="croppedWidth">width of selection</param>
	/// <param name="croppedHeight">height of selection</param>
	/// <param name="width">width of source texture or 0 to assume square texture</param>
	/// <returns>new raw rgba8888 pixel data of width croppedWidth or smaller if x is smaller than zero or if x + croppedWidth extends outside the source texture</returns>
	public static byte[] Crop(this ReadOnlySpan<byte> texture, int x, int y, int croppedWidth, int croppedHeight, ushort width = 0)
	{
		width = (ushort)PixelWidth(texture.Length, width);
		if (x < 0)
		{
			croppedWidth += x;
			x = 0;
		}
		if (croppedWidth < 1) throw new ArgumentException("croppedWidth < 1. Was: \"" + croppedWidth + "\"");
		if (y < 0)
		{
			croppedHeight += y;
			y = 0;
		}
		if (croppedHeight < 1) throw new ArgumentException("croppedHeight < 1. Was: \"" + croppedHeight + "\"");
		int xSide = width << 2;
		x <<= 2; // x *= 4;
		if (x > xSide) throw new ArgumentException("x > xSide. x: \"" + x + "\", xSide: \"" + xSide + "\"");
		int ySide = PixelHeight(texture.Length, width);
		if (y > ySide) throw new ArgumentException("y > ySide. y: \"" + y + "\", ySide: \"" + ySide + "\"");
		if (y + croppedHeight > ySide)
			croppedHeight = ySide - y;
		croppedWidth <<= 2; // croppedWidth *= 4;
		if (x + croppedWidth > xSide)
			croppedWidth = xSide - x;
		byte[] cropped = new byte[croppedWidth * croppedHeight];
		for (int y1 = y * xSide + x, y2 = 0; y2 < cropped.Length; y1 += xSide, y2 += croppedWidth)
			texture.Slice(y1, croppedWidth).CopyTo(cropped.AsSpan(y2));
		return cropped;
	}
	/// <summary>
	/// Cuts off all edge rows and columns with an alpha channel lower than the threshold
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="cutLeft">number of columns of pixels that have been removed form the left side</param>
	/// <param name="cutTop">number of rows of pixels that have been removed from the top</param>
	/// <param name="croppedWidth">width of returned texture</param>
	/// <param name="croppedHeight">height of returned texture</param>
	/// <param name="width">width of source texture or 0 to assume square texture</param>
	/// <param name="threshold">alpha channel lower than this will be evaluated as transparent</param>
	/// <returns>cropped texture</returns>
	public static byte[] Crop2Content(this ReadOnlySpan<byte> texture, out ushort cutLeft, out ushort cutTop, out ushort croppedWidth, out ushort croppedHeight, ushort width = 0, byte threshold = DefaultTransparencyThreshold)
	{
		width = (ushort)PixelWidth(texture.Length, width);
		Crop2ContentInfo(
			texture: texture,
			cutLeft: out cutLeft,
			cutTop: out cutTop,
			croppedWidth: out croppedWidth,
			croppedHeight: out croppedHeight,
			width: width,
			threshold: threshold);
		return texture.Crop(
			x: cutLeft,
			y: cutTop,
			croppedWidth: croppedWidth,
			croppedHeight: croppedHeight,
			width: width);
	}
	public static void Crop2ContentInfo(this ReadOnlySpan<byte> texture, out ushort cutLeft, out ushort cutTop, out ushort croppedWidth, out ushort croppedHeight, ushort width = 0, byte threshold = DefaultTransparencyThreshold)
	{
		width = (ushort)PixelWidth(texture.Length, width);
		int xSide = width << 2,
			indexTop, indexBottom;
		for (indexTop = 3; indexTop < texture.Length && texture[indexTop] < threshold; indexTop += 4) { }
		cutTop = checked((ushort)(indexTop / xSide));
		indexTop = cutTop * xSide;
		for (indexBottom = texture.Length - 1; indexBottom > indexTop && texture[indexBottom] < threshold; indexBottom -= 4) { }
		int cutBottom = indexBottom / xSide + 1;
		croppedHeight = (ushort)(cutBottom - cutTop);
		static bool alpha(ReadOnlySpan<byte> texture, int xSide, byte threshold, ushort x, ushort y) => texture[y * xSide + (x << 2) + 3] < threshold;
		cutLeft = (ushort)(width - 1);
		ushort cutRight = 0;
		for (ushort y = cutTop; y < cutBottom; y++)
		{
			ushort left;
			for (left = 0; left < cutLeft && alpha(texture, xSide, threshold, left, y); left++) { }
			if (left < cutLeft)
				cutLeft = left;
			ushort right;
			for (right = (ushort)(width - 1); right > cutRight && alpha(texture, xSide, threshold, right, y); right--) { }
			if (right > cutRight)
				cutRight = right;
		}
		croppedWidth = (ushort)(cutRight - cutLeft + 1);
	}
	public static byte[] Crop2ContentPlus1(this ReadOnlySpan<byte> texture, out int cutLeft, out int cutTop, out ushort croppedWidth, out ushort croppedHeight, ushort width = 0, byte threshold = DefaultTransparencyThreshold)
	{
		width = (ushort)PixelWidth(texture.Length, width);
		Crop2ContentInfo(
			texture: texture,
			cutLeft: out ushort cutLeftShort,
			cutTop: out ushort cutTopShort,
			croppedWidth: out ushort croppedWidthShort,
			croppedHeight: out ushort croppedHeightShort,
			width: width,
			threshold: threshold);
		cutLeft = cutLeftShort - 1;
		cutTop = cutTopShort - 1;
		croppedWidth = (ushort)(croppedWidthShort + 2);
		croppedHeight = (ushort)(croppedHeightShort + 2);
		byte[] result = new byte[croppedWidth * 4 * croppedHeight];
		result.DrawInsert(
			x: 1,
			y: 1,
			insert: texture.Crop(
				x: cutLeft + 1,
				y: cutTop + 1,
				croppedWidth: croppedWidth - 2,
				croppedHeight: croppedHeight - 2,
				width: width),
			insertWidth: (ushort)(croppedWidth - 2),
			width: croppedWidth);
		return result;
	}
	/// <summary>
	/// Gives every transparent pixel the average color of its opaque neighbors, but keeps it transparent, so that scaling or filtering doesn't bleed a wrong color into the edges of the opaque area.
	/// Orthogonal neighbors are preferred. Diagonal neighbors are only used if there are no opaque orthogonal neighbors. Transparent pixels with no opaque neighbors at all become transparent black.
	/// Opaque pixels are copied unchanged. Neighbors are always read from the source texture, never from the result.
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <param name="threshold">alpha channel lower than this will be evaluated as transparent</param>
	/// <returns>new raw rgba8888 pixel data of identical size to source texture</returns>
	public static byte[] TransparentOutline(ReadOnlySpan<byte> texture, ushort width = 0, byte threshold = DefaultTransparencyThreshold)
	{
		width = (ushort)PixelWidth(texture.Length, width);
		int xSide = width << 2,
			height = texture.Length / xSide;
		byte[] result = [.. texture];
		for (int y = 0, row = 0; y < height; y++, row += xSide)
			for (int x = 0, index = row; x < width; x++, index += 4)
			{
				if (texture[index + 3] >= threshold) continue;
				if (!TryAverageNeighbors(texture, x, y, width, height, threshold, false, out byte r, out byte g, out byte b)
					&& !TryAverageNeighbors(texture, x, y, width, height, threshold, true, out r, out g, out b))
					r = g = b = 0; // Make non-border transparent pixels transparent black
				result[index] = r;
				result[index + 1] = g;
				result[index + 2] = b;
				result[index + 3] = 0;
			}
		return result;
	}
	/// <param name="diagonal">false to check the four orthogonal neighbors, true to check the four diagonal neighbors</param>
	/// <returns>true if at least one neighbor was opaque, in which case r, g and b hold the average of the opaque neighbors' colors</returns>
	private static bool TryAverageNeighbors(ReadOnlySpan<byte> texture, int x, int y, int width, int height, byte threshold, bool diagonal, out byte r, out byte g, out byte b)
	{
		int sumR = 0, sumG = 0, sumB = 0, count = 0;
		for (int dy = -1; dy <= 1; dy++)
		{
			int ny = y + dy;
			if (ny < 0 || ny >= height) continue;
			for (int dx = -1; dx <= 1; dx++)
			{
				int nx = x + dx;
				if (nx < 0 || nx >= width) continue;
				if (diagonal ? dx == 0 || dy == 0 : (dx == 0) == (dy == 0)) continue;
				int index = (ny * width + nx) << 2;
				if (texture[index + 3] < threshold) continue;
				sumR += texture[index];
				sumG += texture[index + 1];
				sumB += texture[index + 2];
				count++;
			}
		}
		if (count < 1)
		{
			r = g = b = 0;
			return false;
		}
		r = (byte)(sumR / count);
		g = (byte)(sumG / count);
		b = (byte)(sumB / count);
		return true;
	}
	/// <summary>
	/// Adds a 1 pixel wide transparent border around the edges of a texture only if the edges aren't already transparent
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="result">new texture with transparent border or null if unnecessary</param>
	/// <param name="addLeft">pixels added to the left in result</param>
	/// <param name="addTop">pixels added to the top in result</param>
	/// <param name="resultWidth">width of result or 0 if unnecessary</param>
	/// <param name="resultHeight">height of result or 0 if unnecessary</param>
	/// <param name="width">width of source texture or 0 to assume square texture</param>
	/// <param name="threshold">alpha channel lower than this will be evaluated as transparent</param>
	/// <returns>true if making a new texture was necessary</returns>
	public static bool NeedsTransparentBorder(ReadOnlySpan<byte> texture, out byte[] result, out ushort addLeft, out ushort addTop, out ushort resultWidth, out ushort resultHeight, ushort width = 0, byte threshold = DefaultTransparencyThreshold)
	{
		addLeft = 0;
		addTop = 0;
		width = (ushort)PixelWidth(texture.Length, width);
		ushort height = Height(texture.Length, width);
		int xSide = width << 2;
		resultWidth = width;
		resultHeight = height;
		for (int index = 3; index < xSide; index += 4)
			if (texture[index] >= threshold)
			{
				addTop++;
				resultHeight++;
				break;
			}
		for (int index = texture.Length - xSide + 3; index < texture.Length; index += 4)
			if (texture[index] >= threshold)
			{
				resultHeight++;
				break;
			}
		for (int index = 3; index < texture.Length; index += xSide)
			if (texture[index] >= threshold)
			{
				addLeft++;
				resultWidth++;
				break;
			}
		for (int index = xSide - 1; index < texture.Length; index += xSide)
			if (texture[index] >= threshold)
			{
				resultWidth++;
				break;
			}
		if (width == resultWidth && height == resultHeight)
		{
			resultWidth = 0;
			resultHeight = 0;
			result = null;
			return false;
		}
		result = new byte[resultWidth * 4 * resultHeight];
		result.DrawInsert(
			x: addLeft,
			y: addTop,
			insert: texture,
			insertWidth: width,
			width: resultWidth);
		return true;
	}
	public static byte[] Outline(this ReadOnlySpan<byte> texture, ushort width = 0, uint color = Black, byte threshold = DefaultTransparencyThreshold)
	{
		width = (ushort)PixelWidth(texture.Length, width);
		int xSide = width << 2;
		byte[] result = new byte[texture.Length];
		texture.CopyTo(result);
		for (int rowIndex = 0; rowIndex < texture.Length; rowIndex += xSide)
			for (int index = 3; index < xSide; index += 4)
				if (texture[rowIndex + index] < threshold && (
					(rowIndex > 0 && texture[rowIndex + index - xSide] >= threshold)
					|| (index + 4 < xSide && texture[rowIndex + index + 4] >= threshold)
					|| (rowIndex + index + xSide < texture.Length && texture[rowIndex + index + xSide] >= threshold)
					|| (index > 3 && texture[rowIndex + index - 4] >= threshold)))
					BinaryPrimitives.WriteUInt32BigEndian(
						destination: result.AsSpan(
							start: rowIndex + index - 3,
							length: 4),
						value: color);
		return result;
	}
	/// <summary>
	/// Makes a new texture and copies the old texture to its upper left corner
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="newWidth">width of newly resized texture</param>
	/// <param name="newHeight">height of newly resized texture</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>new raw rgba8888 pixel data of width newWidth</returns>
	public static byte[] Resize(this ReadOnlySpan<byte> texture, ushort newWidth = 0, ushort newHeight = 0, ushort width = 0)
	{
		int xSide = PixelWidth(texture.Length, width) << 2;
		if (newWidth < 1) newWidth = width;
		if (newHeight < 1) newHeight = newWidth;
		int newXside4 = newWidth << 2; // newWidth * 4, as an int so that widths over 16383 don't overflow
		byte[] resized = new byte[checked(newXside4 * newHeight)];
		if (newXside4 == xSide)
			texture[..Math.Min(texture.Length, resized.Length)].CopyTo(resized);
		else
		{
			int newXside = Math.Min(xSide, newXside4);
			for (int y1 = 0, y2 = 0; y1 < texture.Length && y2 < resized.Length; y1 += xSide, y2 += newXside4)
				texture.Slice(y1, newXside).CopyTo(resized.AsSpan(y2));
		}
		return resized;
	}
	/// <summary>
	/// Tile an image
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="factorX">number of times to tile horizontally</param>
	/// <param name="factorY">number of times to tile vertically</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>new raw rgba8888 pixel data of newWidth = width * factorX</returns>
	public static byte[] Tile(this ReadOnlySpan<byte> texture, ushort width = 0, byte factorX = 2, byte factorY = 2)
	{
		if (factorX < 1 || factorY < 1 || factorX < 2 && factorY < 2) return [.. texture];
		byte[] tiled = new byte[checked(texture.Length * factorX * factorY)];
		int xSide = PixelWidth(texture.Length, width) << 2,
			newXside = xSide * factorX;
		if (factorX > 1)
			for (int y1 = 0, y2 = 0; y1 < texture.Length; y1 += xSide, y2 += newXside)
				for (int x = 0; x < newXside; x += xSide)
					texture.Slice(y1, xSide).CopyTo(tiled.AsSpan(y2 + x));
		else
			texture[..].CopyTo(tiled.AsSpan());
		if (factorY > 1)
		{
			int xScaledLength = texture.Length * factorX;
			for (int y = xScaledLength; y < tiled.Length; y += xScaledLength)
				tiled.AsSpan(0, xScaledLength).CopyTo(tiled.AsSpan(y));
		}
		return tiled;
	}
	/// <summary>
	/// Simple nearest-neighbor upscaling by integer multipliers
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="scaleX">horizontal scaling factor</param>
	/// <param name="scaleY">vertical scaling factor</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>new raw rgba8888 pixel data of newWidth = width * scaleX</returns>
	public static byte[] Upscale(this ReadOnlySpan<byte> texture, byte scaleX = 1, byte scaleY = 1, ushort width = 0)
	{
		if (scaleX < 1 || scaleY < 1 || scaleX < 2 && scaleY < 2) return [.. texture];
		int xSide = PixelWidth(texture.Length, width) << 2,
			newXside = xSide * scaleX,
			newXsidefactorY = newXside * scaleY;
		byte[] scaled = new byte[checked(texture.Length * scaleY * scaleX)];
		if (scaleX < 2)
			for (int y1 = 0, y2 = 0; y1 < texture.Length; y1 += xSide, y2 += newXsidefactorY)
				for (int z = y2; z < y2 + newXsidefactorY; z += newXside)
					texture.Slice(y1, xSide).CopyTo(scaled.AsSpan(z));
		else
		{
			int w = xSide >> 2,
				newWidth = newXside >> 2,
				newWidthFactorY = newXsidefactorY >> 2;
			ReadOnlySpan<uint> from = AsPixels(texture);
			Span<uint> to = AsPixels(scaled.AsSpan());
			for (int y1 = 0, y2 = 0; y1 < from.Length; y1 += w, y2 += newWidthFactorY)
			{
				for (int x1 = y1, x2 = y2; x1 < y1 + w; x1++, x2 += scaleX)
					for (int z = 0; z < scaleX; z++)
						to[x2 + z] = from[x1];
				for (int z = y2 + newWidth; z < y2 + newWidthFactorY; z += newWidth)
					to.Slice(y2, newWidth).CopyTo(to.Slice(z));
			}
		}
		return scaled;
	}
	public static byte[] Upscale(this ReadOnlySpan<byte> texture, out ushort newWidth, byte scaleX = 1, byte scaleY = 1, ushort width = 0)
	{
		newWidth = (ushort)(PixelWidth(texture.Length, width) * scaleX);
		return texture.Upscale(scaleX, scaleY, width);
	}
	public static byte[][] UpscaleSprites(this byte[][] sprites, ushort[] widths, ushort[][] pixelOrigins, out ushort[] newWidths, out ushort[][] newPixelOrigins, byte scaleX = 1, byte scaleY = 1)
	{
		byte[][] newSprites = new byte[sprites.Length][];
		newWidths = new ushort[widths.Length];
		newPixelOrigins = new ushort[pixelOrigins.Length][];
		for (int i = 0; i < sprites.Length; i++)
		{
			newSprites[i] = sprites[i].Upscale(
				scaleX: scaleX,
				scaleY: scaleY,
				width: widths[i]);
			newWidths[i] = (ushort)(widths[i] * scaleX);
			newPixelOrigins[i] = [(ushort)(pixelOrigins[i][0] * scaleX), (ushort)(pixelOrigins[i][1] * scaleY)];
		}
		return newSprites;
	}
	#endregion Image manipulation
	#region Utilities
	/// <summary>
	/// The width of a texture in pixels, where a width of 0 means that the texture is square.
	/// </summary>
	/// <param name="length">length in bytes of the raw rgba8888 pixel data</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	private static int PixelWidth(int length, ushort width) => width < 1 ? (int)Math.Sqrt(length >> 2) : width;
	/// <summary>
	/// The height of a texture in pixels, where a width of 0 means that the texture is square.
	/// </summary>
	/// <param name="length">length in bytes of the raw rgba8888 pixel data</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	private static int PixelHeight(int length, ushort width) => width < 1 ? PixelWidth(length, width) : (length / width) >> 2;
	/// <summary>
	/// Writes one pixel's color, which is four bytes in the order red, green, blue, alpha, where index is the position in bytes of its red.
	/// For drawing, where the same color goes to many pixels, this measured 1.1 to 1.4 times faster than BinaryPrimitives.WriteUInt32BigEndian on a four byte slice.
	/// It is slower, about 0.8 times, when every pixel gets a different value, as when converting a whole texture, so those use BinaryPrimitives.
	/// The last byte goes first so that if the index is out of range nothing at all has been written, and so that the compiler knows the other three are in range.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void WritePixel(Span<byte> texture, int index, uint color)
	{
		texture[index + 3] = (byte)color;
		texture[index] = (byte)(color >> 24);
		texture[index + 1] = (byte)(color >> 16);
		texture[index + 2] = (byte)(color >> 8);
	}
	/// <summary>
	/// Reads one pixel's color, which is four bytes in the order red, green, blue, alpha, where index is the position in bytes of its red.
	/// Measured 1.3 to 1.5 times faster than BinaryPrimitives.ReadUInt32BigEndian on a four byte slice when reading through a whole texture.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint ReadPixel(ReadOnlySpan<byte> texture, int index) =>
		texture[index + 3]
		| (uint)texture[index + 2] << 8
		| (uint)texture[index + 1] << 16
		| (uint)texture[index] << 24;
	public static uint Pixel(this ReadOnlySpan<byte> texture, ushort x, ushort y, ushort width = 0) => ReadPixel(texture, y * (PixelWidth(texture.Length, width) << 2) + (x << 2));
	/// <summary>
	/// Compute power of two greater than or equal to `n`
	/// </summary>
	public static uint NextPowerOf2(this uint n)
	{
		n--; // decrement `n` (to handle the case when `n` itself is a power of 2)
			 // set all bits after the last set bit
		n |= n >> 1;
		n |= n >> 2;
		n |= n >> 4;
		n |= n >> 8;
		n |= n >> 16;
		return ++n; // increment `n` and return
	}
	public static ushort Height(int length, ushort width = 0) =>
		width > 0 ?
			checked((ushort)((length / width) >> 2))
			: (ushort)Math.Sqrt(length >> 2);
	public static uint Color(byte r, byte g, byte b, byte a) => (uint)(r << 24 | g << 16 | b << 8 | a);
	/// <param name="rgba">rgba8888, Big Endian</param>
	/// <returns>argb8888, Big Endian</returns>
	public static uint Rgba2argb(this uint rgba) => rgba << 24 | rgba >> 8;
	/// <param name="rgba">argb8888, Big Endian</param>
	/// <returns>rgba8888, Big Endian</returns>
	public static uint Argb2rgba(this uint argb) => argb << 8 | argb >> 24;
	public static int LerpColor(this int startColor, int endColor, float change)
	{
		int sA = startColor & 0xFF, sB = startColor >> 8 & 0xFF, sG = startColor >> 16 & 0xFF, sR = startColor >> 24 & 0xFF,
			eA = endColor & 0xFF, eB = endColor >> 8 & 0xFF, eG = endColor >> 16 & 0xFF, eR = endColor >> 24 & 0xFF;
		return ((int)(sR + change * (eR - sR)) & 0xFF) << 24
			| ((int)(sG + change * (eG - sG)) & 0xFF) << 16
			| ((int)(sB + change * (eB - sB)) & 0xFF) << 8
			| (int)(sA + change * (eA - sA)) & 0xFF;
	}
	public static uint LerpColor(this uint startColor, uint endColor, float change) => (uint)LerpColor((int)startColor, (int)endColor, change);
	/// <param name="indices">Palette indices (one byte per pixel)</param>
	/// <param name="palette">256 rgba8888 color values</param>
	/// <returns>rgba8888 texture (four bytes per pixel)</returns>
	public static byte[] Index2ByteArray(this ReadOnlySpan<byte> indices, uint[] palette)
	{
		byte[] bytes = new byte[indices.Length << 2];
		for (int i = 0, j = 0; i < indices.Length; i++, j += 4)
			BinaryPrimitives.WriteUInt32BigEndian(
				destination: bytes.AsSpan(
					start: j,
					length: 4),
				value: palette[indices[i]]);
		return bytes;
	}
	/// <summary>
	/// Collects the distinct colors of a texture into a palette of at most 256 colors.
	/// Index 0 is always transparent black (0u). If there are more distinct colors than fit, the most opaque ones are kept (highest alpha first, ties broken by highest rgba value).
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data</param>
	/// <returns>palette of rgba8888 colors, with 0 reserved for transparency, sorted first by descending opacity, then descending rgba value</returns>
	public static uint[] PaletteFromTexture(this ReadOnlySpan<byte> texture)
	{
		HashSet<uint> colors = [];
		for (int i = 0; i + 4 <= texture.Length; i += 4)
			colors.Add(ReadPixel(texture, i));
		colors.Remove(0u); // Reserved for index 0
		return [0u, .. colors
			.OrderByDescending(@uint => (byte)@uint) // alpha is the lowest byte
			.ThenByDescending(@uint => @uint)
			.Take(255)];
	}
	public static uint[] Opaque(params uint[] palette) => [.. palette.Select(color => color | 0xFFu)];
	public static byte[] Byte2IndexArray(this ReadOnlySpan<byte> bytes, params uint[] palette)
	{
		byte[] indices = new byte[bytes.Length >> 2];
		uint[] uints = bytes.Byte2UIntArray();
		for (int i = 0; i < indices.Length; i++)
			indices[i] = (byte)Math.Max(Array.IndexOf(palette, uints[i]), 0);
		return indices;
	}
	/// <param name="indices">Palette indices (one byte per pixel)</param>
	/// <param name="palette">256 rgba8888 color values</param>
	/// <returns>rgba8888 texture (one int per pixel)</returns>
	public static uint[] Index2UIntArray(this ReadOnlySpan<byte> indices, params uint[] palette)
	{
		uint[] uints = new uint[indices.Length];
		for (int i = 0; i < uints.Length; i++)
			uints[i] = palette[indices[i]];
		return uints;
	}
	/// <param name="ints">rgba8888 color values (one int per pixel)</param>
	/// <returns>rgba8888 texture (four bytes per pixel)</returns>
	public static byte[] UInt2ByteArray(this uint[] uints)
	{
		byte[] bytes = new byte[uints.Length << 2];
		for (int i = 0, j = 0; i < uints.Length; i++, j += 4)
			BinaryPrimitives.WriteUInt32BigEndian(
				destination: bytes.AsSpan(
					start: j,
					length: 4),
				value: uints[i]);
		return bytes;
	}
	public static byte[,] OneDToTwoD(this ReadOnlySpan<byte> bytes, ushort width = 0)
	{
		if (width < 1)
			width = (ushort)Math.Sqrt(bytes.Length);
		ushort height = (ushort)(bytes.Length / width);
		byte[,] twoD = new byte[width, height];
		int index = 0;
		for (ushort y = 0; y < height; y++)
			for (ushort x = 0; x < width; x++, index++)
				twoD[x, y] = bytes[index];
		return twoD;
	}
	public static byte[] TwoDToOneD(this byte[,] bytes)
	{
		int width = bytes.GetLength(0),
			height = bytes.GetLength(1);
		byte[] oneD = new byte[width * height];
		for (int y = 0, rowStart = 0; y < height; y++, rowStart += width)
			for (int x = 0; x < width; x++)
				oneD[rowStart + x] = bytes[x, y];
		return oneD;
	}
	/// <param name="bytes">rgba8888 color values (four bytes per pixel)</param>
	/// <returns>rgba8888 texture (one int per pixel)</returns>
	public static uint[] Byte2UIntArray(this ReadOnlySpan<byte> bytes)
	{
		uint[] uints = new uint[bytes.Length >> 2];
		for (int i = 0, j = 0; i < bytes.Length; i += 4)
			uints[j++] = ReadPixel(bytes, i);
		return uints;
	}
	public static uint[,] Texture2UInt2D(this ReadOnlySpan<byte> texture, ushort width = 0)
	{
		width = (ushort)PixelWidth(texture.Length, width);
		int height = PixelHeight(texture.Length, width),
			xSide = width << 2;
		uint[,] uints = new uint[width, height];
		for (int y = 0, y2 = 0; y < height; y++, y2 += xSide)
			for (int x = 0, x2 = 0; x < width; x++, x2 += 4)
				uints[x, y] = BinaryPrimitives.ReadUInt32BigEndian(texture.Slice(y2 + x2, 4));
		return uints;
	}
	public static byte[] UInt2D2Texture(this uint[,] uints)
	{
		int width = uints.GetLength(0),
			height = uints.GetLength(1),
			xSide = width << 2;
		byte[] texture = new byte[xSide * height];
		for (int y = 0, y2 = 0; y < height; y++, y2 += xSide)
			for (int x = 0, x2 = 0; x < width; x++, x2 += 4)
				BinaryPrimitives.WriteUInt32BigEndian(
					destination: texture.AsSpan(y2 + x2, 4),
					value: uints[x, y]);
		return texture;
	}
	public static T[] ConcatArrays<T>(params T[][] list)
	{
		T[] result = new T[list.Sum(a => a.Length)];
		for (int i = 0, offset = 0; i < list.Length; i++)
		{
			list[i].CopyTo(result, offset);
			offset += list[i].Length;
		}
		return result;
	}
	public static uint[] LoadPalette(string @string)
	{
		using MemoryStream memoryStream = new(Encoding.UTF8.GetBytes(@string));
		return LoadPalette(memoryStream);
	}
	public static uint[] LoadPalette(Stream stream)
	{
		uint[] result;
		using (StreamReader streamReader = new(stream))
		{
			string line;
			while (string.IsNullOrWhiteSpace(line = streamReader.ReadLine().Trim())) { }
			if (!line.Equals("JASC-PAL") || !streamReader.ReadLine().Trim().Equals("0100"))
				throw new InvalidDataException("Palette stream is an incorrectly formatted JASC palette.");
			if (!int.TryParse(streamReader.ReadLine()?.Trim(), out int numColors)
			 || numColors != 256)
				throw new InvalidDataException("Palette stream does not contain exactly 256 colors.");
			result = new uint[numColors];
			for (int i = 0; i < numColors; i++)
			{
				string[] tokens = streamReader.ReadLine()?.Trim().Split(' ');
				if (tokens == null || tokens.Length != 3
					|| !byte.TryParse(tokens[0], out byte r)
					|| !byte.TryParse(tokens[1], out byte g)
					|| !byte.TryParse(tokens[2], out byte b))
					throw new InvalidDataException("Palette stream is an incorrectly formatted JASC palette.");
				result[i] = (uint)(r << 24
					| g << 16
					| b << 8
					| (i == 0 ? 0 : 255));
			}
		}
		return result;
	}
	#endregion Utilities
}
