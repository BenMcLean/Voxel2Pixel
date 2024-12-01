using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Voxel2Pixel.Draw;

/// <summary>
/// All methods in this static class are actually stateless functions, meaning that they do not reference any modifiable variables besides their parameters.
/// These methods assume rgba8888 a.k.a. rgba32 color, stored Big-Endian.
/// That means every four bytes represent one pixel.
/// A texture is a byte array of size = wdith * 4 * height.
/// width is max x, height is max y.
/// x+ is right, y+ is down.
/// Methods that start with "Draw" modify the original array. Other methods return a copy.
/// Sometimes bitwise operations are hard to follow:
/// (i << 2 == i * 4)
/// (i >> 2 == i / 4) when i is a positive integer
/// </summary>
public static class PixelDraw
{
	//TODO: DrawLine
	//TODO: DrawCircle
	//TODO: DrawEllipse
	public const uint DefaultOutlineColor = 0xFFu;
	public const byte DefaultTransparencyThreshold = 127;
	#region Drawing
	/// <summary>
	/// Draws one pixel of the specified color
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data</param>
	/// <param name="color">rgba color to draw</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>same texture with pixel drawn</returns>
	public static byte[] DrawPixel(this byte[] texture, ushort x, ushort y, uint color = 0xFFFFFFFFu, ushort width = 0)
	{
		ushort xSide = (ushort)((width < 1 ? (ushort)Math.Sqrt(texture.Length >> 2) : width) << 2),
			ySide = (ushort)((width < 1 ? xSide : texture.Length / width) >> 2);
		x <<= 2;//x *= 4;
		if (x >= xSide || y >= ySide) return texture;
		BinaryPrimitives.WriteUInt32BigEndian(
			destination: texture.AsSpan(
				start: y * xSide + x,
				length: 4),
			value: color);
		return texture;
	}
	/// <summary>
	/// Draws a rectangle of the specified color
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data to be modified</param>
	/// <param name="color">rgba color to draw</param>
	/// <param name="x">upper left corner of rectangle</param>
	/// <param name="y">upper left corner of rectangle</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>same texture with rectangle drawn</returns>
	public static byte[] DrawRectangle(this byte[] texture, uint color, int x, int y, int rectWidth = 1, int rectHeight = 1, ushort width = 0)
	{
		if (rectWidth == 1 && rectHeight == 1)
			return texture.DrawPixel((ushort)x, (ushort)y, color, width);
		if (rectHeight < 1) rectHeight = rectWidth;
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
		width = width < 1 ? (ushort)Math.Sqrt(texture.Length >> 2) : width;
		int height = texture.Length / width >> 2;
		if (rectWidth < 1 || rectHeight < 1 || x >= width || y >= height) return texture;
		rectWidth = Math.Min(rectWidth, width - x);
		rectHeight = Math.Min(rectHeight, height - y);
		int xSide = width << 2,
			x4 = x << 2,
			offset = y * xSide + x4,
			rectWidth4 = rectWidth << 2,
			yStop = offset + xSide * rectHeight;
		for (int x2 = offset; x2 < offset + rectWidth4; x2 += 4)
			BinaryPrimitives.WriteUInt32BigEndian(
				destination: texture.AsSpan(
					start: x2,
					length: 4),
				value: color);
		for (int y2 = offset + xSide; y2 < yStop; y2 += xSide)
			Array.Copy(
				sourceArray: texture,
				sourceIndex: offset,
				destinationArray: texture,
				destinationIndex: y2,
				length: rectWidth4);
		return texture;
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
	public static byte[] DrawInsert(this byte[] texture, int x, int y, byte[] insert, ushort insertWidth = 0, ushort width = 0)
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
		int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2;
		x <<= 2; // x *= 4;
		if (x > xSide) return texture;
		int insertXside = (insertWidth < 1 ? (int)Math.Sqrt(insert.Length >> 2) : insertWidth) << 2,
			actualInsertXside = (x + insertXside > xSide ? xSide - x : insertXside) - insertX,
			ySide = (width < 1 ? xSide : texture.Length / width) >> 2;
		if (y > ySide) return texture;
		if (xSide == insertXside && x == 0 && insertX == 0)
			Array.Copy(
				sourceArray: insert,
				sourceIndex: insertY * insertXside,
				destinationArray: texture,
				destinationIndex: y * xSide,
				length: Math.Min(insert.Length - insertY * insertXside + insertX, texture.Length - y * xSide));
		else
			for (int y1 = y * xSide + x, y2 = insertY * insertXside + insertX; y1 + actualInsertXside < texture.Length && y2 < insert.Length; y1 += xSide, y2 += insertXside)
				Array.Copy(
					sourceArray: insert,
					sourceIndex: y2,
					destinationArray: texture,
					destinationIndex: y1,
					length: actualInsertXside);
		return texture;
	}
	public static byte[,] DrawInsert(this byte[,] bytes, byte[,] insert, ushort x = 0, ushort y = 0, bool skip0 = true)
	{
		ushort xStop = Math.Min((ushort)(x + insert.GetLength(0)), (ushort)bytes.GetLength(0)),
			yStop = Math.Min((ushort)(y + insert.GetLength(1)), (ushort)bytes.GetLength(1));
		for (ushort x1 = 0; x < xStop; x++, x1++)
			for (ushort y1 = 0; y < yStop; y++, y1++)
				if (insert[x1, y1] is byte @byte
					&& (!skip0 || @byte != 0))
					bytes[x, y] = @byte;
		return bytes;
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
	public static byte[] DrawTransparentInsert(this byte[] texture, int x, int y, byte[] insert, ushort insertWidth = 0, ushort width = 0, byte threshold = DefaultTransparencyThreshold)
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
		int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2;
		x <<= 2; // x *= 4;
		if (x > xSide) return texture;
		int insertXside = (insertWidth < 1 ? (int)Math.Sqrt(insert.Length >> 2) : insertWidth) << 2,
			actualInsertXside = (x + insertXside > xSide ? xSide - x : insertXside) - insertX,
			ySide = (width < 1 ? xSide : texture.Length / width) >> 2;
		if (y > ySide) return texture;
		for (int y1 = y * xSide + x, y2 = insertY * insertXside + insertX; y1 < texture.Length && y2 < insert.Length; y1 += xSide, y2 += insertXside)
			for (int x1 = 0; x1 < actualInsertXside; x1 += 4)
				if (insert[y2 + x1 + 3] >= threshold)
					Array.Copy(
						sourceArray: insert,
						sourceIndex: y2 + x1,
						destinationArray: texture,
						destinationIndex: y1 + x1,
						length: 4);
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
	public static byte[] DrawPadding(this byte[] texture, ushort x, ushort y, ushort areaWidth, ushort areaHeight, ushort width = 0)
	{
		if (areaHeight < 1) areaHeight = areaWidth;
		int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2,
			ySide = (width < 1 ? xSide : texture.Length / width) >> 2,
			x4 = x * 4,
			offset = y > 0 ? (y - 1) * xSide + x4 : x4,
			stop = Math.Min((y + areaHeight) * xSide + x4, texture.Length),
			areaWidth4 = areaWidth << 2;
		if (x4 > xSide || offset > texture.Length)
			return texture;
		if (y > 0)
			Array.Copy(
				sourceArray: texture,
				sourceIndex: offset + xSide,
				destinationArray: texture,
				destinationIndex: offset,
				length: Math.Min(areaWidth4, xSide - x4));
		if (y + areaHeight < ySide)
			Array.Copy(
				sourceArray: texture,
				sourceIndex: stop - xSide,
				destinationArray: texture,
				destinationIndex: stop,
				length: Math.Min(areaWidth4, xSide - x4));
		for (int y1 = Math.Max(x4, offset); y1 <= stop; y1 += xSide)
		{
			if (x > 0)
				Array.Copy(
					sourceArray: texture,
					sourceIndex: y1,
					destinationArray: texture,
					destinationIndex: y1 - 4,
					length: 4);
			if (x4 + areaWidth4 < xSide)
				Array.Copy(
					sourceArray: texture,
					sourceIndex: y1 + areaWidth4 - 4,
					destinationArray: texture,
					destinationIndex: y1 + areaWidth4,
					length: 4);
		}
		return texture;
	}
	#endregion Drawing
	#region Rotation
	/// <summary>
	/// Flips an image on the X axis
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>new raw rgba8888 pixel data of identical size to source texture</returns>
	public static byte[] FlipX(this byte[] texture, ushort width = 0)
	{
		int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2;
		byte[] flipped = new byte[texture.Length];
		for (int y = 0; y < flipped.Length; y += xSide)
			Array.Copy(
				sourceArray: texture,
				sourceIndex: y,
				destinationArray: flipped,
				destinationIndex: flipped.Length - xSide - y,
				length: xSide);
		return flipped;
	}
	/// <summary>
	/// Flips an image on the Y axis
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>new raw rgba8888 pixel data of identical size to source texture</returns>
	public static byte[] FlipY(this byte[] texture, ushort width = 0)
	{
		int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2;
		byte[] flipped = new byte[texture.Length];
		for (int y = 0; y < flipped.Length; y += xSide)
			for (int x = 0; x < xSide; x += 4)
				Array.Copy(
					sourceArray: texture,
					sourceIndex: y + x,
					destinationArray: flipped,
					destinationIndex: y + xSide - 4 - x,
					length: 4);
		return flipped;
	}
	public const double Tau = Math.PI * 2d;
	/// <summary>
	/// Based on https://iiif.io/api/annex/notes/rotation/
	/// </summary>
	public static void RotatedSize(ushort width, ushort height, out ushort rotatedWidth, out ushort rotatedHeight, double radians = 0d, byte scaleX = 1, byte scaleY = 1)
	{
		if (scaleX < 1) throw new ArgumentOutOfRangeException(nameof(scaleX));
		if (scaleY < 1) throw new ArgumentOutOfRangeException(nameof(scaleY));
		if (width * (uint)scaleX > ushort.MaxValue || height * (uint)scaleY > ushort.MaxValue)
			throw new OverflowException("Scaled dimensions exceed maximum allowed size.");
		ushort scaledWidth = (ushort)(width * scaleX),
			scaledHeight = (ushort)(height * scaleY);
		radians %= Tau;
		double absCos = Math.Abs(Math.Cos(radians)),
			absSin = Math.Abs(Math.Sin(radians)),
			calculatedWidth = scaledWidth * absCos + scaledHeight * absSin,
			calculatedHeight = scaledWidth * absSin + scaledHeight * absCos;
		if (calculatedWidth > ushort.MaxValue || calculatedHeight > ushort.MaxValue)
			throw new OverflowException("Resulting image dimensions exceed maximum allowed size.");
		rotatedWidth = (ushort)calculatedWidth;
		rotatedHeight = (ushort)calculatedHeight;
	}
	public static void RotatedLocate(ushort width, ushort height, ushort x, ushort y, out ushort rotatedX, out ushort rotatedY, double radians = 0d, byte scaleX = 1, byte scaleY = 1)
	{
		if (scaleX < 1) throw new ArgumentOutOfRangeException(nameof(scaleX));
		if (scaleY < 1) throw new ArgumentOutOfRangeException(nameof(scaleY));
		if (x >= width) throw new ArgumentOutOfRangeException(nameof(x));
		if (y >= height) throw new ArgumentOutOfRangeException(nameof(y));
		if (width * (uint)scaleX > ushort.MaxValue || height * (uint)scaleY > ushort.MaxValue)
			throw new OverflowException("Scaled dimensions exceed maximum allowed size.");
		ushort scaledWidth = (ushort)(width * scaleX),
			scaledHeight = (ushort)(height * scaleY);
		radians %= Tau;
		double cos = Math.Cos(radians),
			sin = Math.Sin(radians),
			absCos = Math.Abs(cos),
			absSin = Math.Abs(sin),
			calculatedWidth = scaledWidth * absCos + scaledHeight * absSin,
			calculatedHeight = scaledWidth * absSin + scaledHeight * absCos;
		if (calculatedWidth > ushort.MaxValue || calculatedHeight > ushort.MaxValue)
			throw new OverflowException("Resulting image dimensions exceed maximum allowed size.");
		double scaledX = (x + 0.5d) * scaleX - scaledWidth / 2d,
			scaledY = (y + 0.5d) * scaleY - scaledHeight / 2d,
			rotatedXd = scaledX * cos - scaledY * sin + calculatedWidth / 2d,
			rotatedYd = scaledX * sin + scaledY * cos + calculatedHeight / 2d;
		rotatedX = (ushort)rotatedXd;
		rotatedY = (ushort)rotatedYd;
	}
	/// <summary>
	/// Based on https://stackoverflow.com/a/6207833
	/// </summary>
	public static byte[] Rotate(this byte[] texture, out ushort rotatedWidth, out ushort rotatedHeight, double radians = 0d, byte scaleX = 1, byte scaleY = 1, ushort width = 0)
	{
		if (scaleX < 1) throw new ArgumentOutOfRangeException(nameof(scaleX));
		if (scaleY < 1) throw new ArgumentOutOfRangeException(nameof(scaleY));
		if (width < 1)
			width = (ushort)Math.Sqrt(texture.Length >> 2);
		if (width > ushort.MaxValue / scaleX)
			throw new OverflowException("Scaled width exceeds maximum allowed size.");
		ushort height = Height(texture.Length, width);
		if (height > ushort.MaxValue / scaleY)
			throw new OverflowException("Scaled height exceeds maximum allowed size.");
		ushort scaledWidth = (ushort)(width * scaleX),
			scaledHeight = (ushort)(height * scaleY);
		radians %= Tau;
		double cos = Math.Cos(radians),
			sin = Math.Sin(radians),
			absCos = Math.Abs(cos),
			absSin = Math.Abs(sin);
		uint rWidth = (uint)(scaledWidth * absCos + scaledHeight * absSin);
		uint rHeight = (uint)(scaledWidth * absSin + scaledHeight * absCos);
		if (rWidth > ushort.MaxValue || rHeight > ushort.MaxValue)
			throw new OverflowException("Rotated dimensions exceed maximum allowed size.");
		if (rWidth * rHeight > int.MaxValue >> 2)
			throw new OverflowException("Resulting image would be too large to allocate");
		rotatedWidth = (ushort)rWidth;
		rotatedHeight = (ushort)rHeight;
		double offsetX = (scaledWidth >> 1) - cos * (rotatedWidth >> 1) - sin * (rotatedHeight >> 1),
			offsetY = (scaledHeight >> 1) - cos * (rotatedHeight >> 1) + sin * (rotatedWidth >> 1);
		byte[] rotated = new byte[rotatedWidth * rotatedHeight << 2];
		bool isNearVertical = absCos < 1e-10;
		for (ushort y = 0; y < rotatedHeight; y++)
		{
			ushort startX, endX;
			if (isNearVertical)
			{
				startX = 0;
				endX = rotatedWidth;
			}
			else
			{
				double xLeft = (-offsetX - y * sin) / cos,
					xRight = (scaledWidth - offsetX - y * sin) / cos;
				if (cos < 0)
					(xLeft, xRight) = (xRight, xLeft);
				startX = (ushort)Math.Max(0, Math.Floor(xLeft));
				endX = (ushort)Math.Min(rotatedWidth, Math.Ceiling(xRight));
			}
			for (ushort x = startX; x < endX; x++)
			{
				ushort oldX = (ushort)((x * cos + y * sin + offsetX) / scaleX),
					oldY = (ushort)((y * cos - x * sin + offsetY) / scaleY);
				if (oldX < width && oldY < height)
					rotated.DrawPixel(
						x: x,
						y: y,
						color: texture.Pixel(
							x: oldX,
							y: oldY,
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
	public static byte[] RotateClockwise45Thin(this byte[] texture, ushort width = 0)
	{
		int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2,
			ySide = (width < 1 ? xSide : texture.Length / width) >> 2,
			newXside = xSide + (ySide << 2) - 4;
		byte[] rotated = new byte[newXside * ((xSide >> 2) + ySide)];
		for (int y1 = texture.Length - xSide, y2 = newXside * ySide - newXside; y1 < texture.Length; y1 += 4, y2 += newXside + 4)
			for (int x1 = y1, x2 = y2; x1 >= 0; x1 -= xSide, x2 += -newXside + 4)
			{
				Array.Copy(
					sourceArray: texture,
					sourceIndex: x1,
					destinationArray: rotated,
					destinationIndex: x2,
					length: 4);
				Array.Copy(
					sourceArray: texture,
					sourceIndex: x1,
					destinationArray: rotated,
					destinationIndex: x2 + newXside,
					length: 4);
			}
		return rotated;
	}
	/// <summary>
	/// Rotates image clockwise by 45 degrees
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>new raw rgba8888 pixel data of newWidth = width + height</returns>
	public static byte[] RotateClockwise45(this byte[] texture, ushort width = 0)
	{
		int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2,
			ySide = (width < 1 ? xSide : texture.Length / width) >> 2,
			newXside = xSide + (ySide << 2);
		byte[] rotated = new byte[newXside * ((xSide >> 2) + ySide - 1)];
		for (int y1 = texture.Length - xSide, y2 = newXside * ySide - newXside; y1 < texture.Length; y1 += 4, y2 += newXside + 4)
			for (int x1 = y1, x2 = y2; x1 >= 0; x1 -= xSide, x2 += -newXside + 4)
			{
				Array.Copy(
					sourceArray: texture,
					sourceIndex: x1,
					destinationArray: rotated,
					destinationIndex: x2,
					length: 4);
				Array.Copy(
					sourceArray: texture,
					sourceIndex: x1,
					destinationArray: rotated,
					destinationIndex: x2 + 4,
					length: 4);
			}
		return rotated;
	}
	/// <summary>
	/// Rotates image clockwise by 90 degrees
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>new raw rgba8888 pixel data where its width is the height of the source texture</returns>
	public static byte[] RotateClockwise90(this byte[] texture, ushort width = 0)
	{
		int ySide2 = width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width,
			xSide1 = ySide2 << 2,
			xSide2 = width < 1 ? ySide2 : texture.Length / width;
		byte[] rotated = new byte[texture.Length];
		for (int y1 = 0, y2 = xSide2 - 4; y1 < texture.Length; y1 += xSide1, y2 -= 4)
			for (int x1 = y1, x2 = y2; x1 < y1 + xSide1; x1 += 4, x2 += xSide2)
				Array.Copy(
					sourceArray: texture,
					sourceIndex: x1,
					destinationArray: rotated,
					destinationIndex: x2,
					length: 4);
		return rotated;
	}
	/// <summary>
	/// Rotates image clockwise by 135 degrees
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>new raw rgba8888 pixel data of newWidth = width + height - 1</returns>
	public static byte[] RotateClockwise135Thin(this byte[] texture, ushort width = 0)
	{
		int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2,
			ySide = (width < 1 ? xSide : texture.Length / width) >> 2,
			newXside = xSide + (ySide << 2) - 4;
		byte[] rotated = new byte[newXside * ((xSide >> 2) + ySide)];
		for (int y1 = texture.Length - 4, y2 = newXside * (xSide >> 2) - newXside; y1 > 0; y1 -= xSide, y2 += newXside + 4)
			for (int x1 = y1, x2 = y2; x1 > y1 - xSide; x1 -= 4, x2 += -newXside + 4)
			{
				Array.Copy(
					sourceArray: texture,
					sourceIndex: x1,
					destinationArray: rotated,
					destinationIndex: x2,
					length: 4);
				Array.Copy(
					sourceArray: texture,
					sourceIndex: x1,
					destinationArray: rotated,
					destinationIndex: x2 + newXside,
					length: 4);
			}
		return rotated;
	}
	/// <summary>
	/// Rotates image clockwise by 135 degrees
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>new raw rgba8888 pixel data of newWidth = width + height</returns>
	public static byte[] RotateClockwise135(this byte[] texture, ushort width = 0)
	{
		int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2,
			ySide = (width < 1 ? xSide : texture.Length / width) >> 2,
			newXside = xSide + (ySide << 2);
		byte[] rotated = new byte[newXside * ((xSide >> 2) + ySide - 1)];
		for (int y1 = texture.Length - 4, y2 = newXside * (xSide >> 2) - newXside; y1 > 0; y1 -= xSide, y2 += newXside + 4)
			for (int x1 = y1, x2 = y2; x1 > y1 - xSide; x1 -= 4, x2 += -newXside + 4)
			{
				Array.Copy(
					sourceArray: texture,
					sourceIndex: x1,
					destinationArray: rotated,
					destinationIndex: x2,
					length: 4);
				Array.Copy(
					sourceArray: texture,
					sourceIndex: x1,
					destinationArray: rotated,
					destinationIndex: x2 + 4,
					length: 4);
			}
		return rotated;
	}
	/// <summary>
	/// Rotates an image 180 degrees
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>new raw rgba8888 pixel data of identical size to source texture</returns>
	public static byte[] Rotate180(this byte[] texture, ushort width = 0)
	{
		int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2;
		byte[] rotated = new byte[texture.Length];
		for (int y1 = 0, y2 = texture.Length - xSide; y1 < texture.Length; y1 += xSide, y2 -= xSide)
			for (int x1 = y1, x2 = y2 + xSide - 4; x1 < y1 + xSide; x1 += 4, x2 -= 4)
				Array.Copy(
					sourceArray: texture,
					sourceIndex: x1,
					destinationArray: rotated,
					destinationIndex: x2,
					length: 4);
		return rotated;
	}
	/// <summary>
	/// Rotates image counter-clockwise by 135 degrees
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>new raw rgba8888 pixel data of newWidth = width + height - 1</returns>
	public static byte[] RotateCounter135Thin(this byte[] texture, ushort width = 0)
	{
		int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2,
			ySide = (width < 1 ? xSide : texture.Length / width) >> 2,
			newXside = xSide + (ySide << 2) - 4;
		byte[] rotated = new byte[newXside * ((xSide >> 2) + ySide)];
		for (int y1 = xSide - 4, y2 = newXside * ySide - newXside; y1 >= 0; y1 -= 4, y2 += newXside + 4)
			for (int x1 = y1, x2 = y2; x1 < texture.Length; x1 += xSide, x2 += -newXside + 4)
			{
				Array.Copy(
					sourceArray: texture,
					sourceIndex: x1,
					destinationArray: rotated,
					destinationIndex: x2,
					length: 4);
				Array.Copy(
					sourceArray: texture,
					sourceIndex: x1,
					destinationArray: rotated,
					destinationIndex: x2 + newXside,
					length: 4);
			}
		return rotated;
	}
	/// <summary>
	/// Rotates image counter-clockwise by 135 degrees
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>new raw rgba8888 pixel data of newWidth = width + height</returns>
	public static byte[] RotateCounter135(this byte[] texture, ushort width = 0)
	{
		int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2,
			ySide = (width < 1 ? xSide : texture.Length / width) >> 2,
			newXside = xSide + (ySide << 2);
		byte[] rotated = new byte[newXside * ((xSide >> 2) + ySide - 1)];
		for (int y1 = xSide - 4, y2 = newXside * ySide - newXside; y1 >= 0; y1 -= 4, y2 += newXside + 4)
			for (int x1 = y1, x2 = y2; x1 < texture.Length; x1 += xSide, x2 += -newXside + 4)
			{
				Array.Copy(
					sourceArray: texture,
					sourceIndex: x1,
					destinationArray: rotated,
					destinationIndex: x2,
					length: 4);
				Array.Copy(
					sourceArray: texture,
					sourceIndex: x1,
					destinationArray: rotated,
					destinationIndex: x2 + 4,
					length: 4);
			}
		return rotated;
	}
	/// <summary>
	/// Rotates image counter-clockwise by 90 degrees
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>new raw rgba8888 pixel data where its width is the height of the source texture</returns>
	public static byte[] RotateCounter90(this byte[] texture, ushort width = 0)
	{
		int ySide2 = width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width,
			xSide1 = ySide2 << 2,
			xSide2 = width < 1 ? ySide2 : texture.Length / width;
		byte[] rotated = new byte[texture.Length];
		for (int y1 = 0, y2 = texture.Length - xSide2; y1 < texture.Length; y1 += xSide1, y2 += 4)
			for (int x1 = y1, x2 = y2; x1 < y1 + xSide1; x1 += 4, x2 -= xSide2)
				Array.Copy(
					sourceArray: texture,
					sourceIndex: x1,
					destinationArray: rotated,
					destinationIndex: x2,
					length: 4);
		return rotated;
	}
	/// <summary>
	/// Rotates image counter-clockwise by 45 degrees
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>new raw rgba8888 pixel data of newWidth = width + height - 1</returns>
	public static byte[] RotateCounter45Thin(this byte[] texture, ushort width = 0)
	{
		int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2,
			ySide = (width < 1 ? xSide : texture.Length / width) >> 2,
			newXside = xSide + (ySide << 2) - 4;
		byte[] rotated = new byte[newXside * ((xSide >> 2) + ySide)];
		for (int y1 = 0, y2 = newXside * (xSide >> 2) - newXside; y1 < texture.Length; y1 += xSide, y2 += newXside + 4)
			for (int x1 = y1, x2 = y2; x1 < y1 + xSide; x1 += 4, x2 += -newXside + 4)
			{
				Array.Copy(
					sourceArray: texture,
					sourceIndex: x1,
					destinationArray: rotated,
					destinationIndex: x2,
					length: 4);
				Array.Copy(
					sourceArray: texture,
					sourceIndex: x1,
					destinationArray: rotated,
					destinationIndex: x2 + newXside,
					length: 4);
			}
		return rotated;
	}
	/// <summary>
	/// Rotates image counter-clockwise by 45 degrees
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>new raw rgba8888 pixel data of newWidth = width + height</returns>
	public static byte[] RotateCounter45(this byte[] texture, ushort width = 0)
	{
		int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2,
			ySide = (width < 1 ? xSide : texture.Length / width) >> 2,
			newXside = xSide + (ySide << 2);
		byte[] rotated = new byte[newXside * ((xSide >> 2) + ySide - 1)];
		for (int y1 = 0, y2 = newXside * (xSide >> 2) - newXside; y1 < texture.Length; y1 += xSide, y2 += newXside + 4)
			for (int x1 = y1, x2 = y2; x1 < y1 + xSide; x1 += 4, x2 += -newXside + 4)
			{
				Array.Copy(
					sourceArray: texture,
					sourceIndex: x1,
					destinationArray: rotated,
					destinationIndex: x2,
					length: 4);
				Array.Copy(
					sourceArray: texture,
					sourceIndex: x1,
					destinationArray: rotated,
					destinationIndex: x2 + 4,
					length: 4);
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
	public static byte[] IsoSlantDown(this byte[] texture, ushort width = 0)
	{
		int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2,
			ySide = (width < 1 ? xSide : texture.Length / width) >> 2,
			newXside = xSide * 2,
			newXside2 = xSide << 2;
		byte[] slanted = new byte[newXside * (ySide * 2 + (xSide >> 2) + 1)];
		for (int y1 = 0, y2 = 0; y1 < texture.Length; y1 += xSide, y2 += newXside2)
			for (int x1 = y1, x2 = y2; x1 < y1 + xSide; x1 += 4, x2 += newXside + 8)
			{
				Array.Copy(
					sourceArray: texture,
					sourceIndex: x1,
					destinationArray: slanted,
					destinationIndex: x2,
					length: 4);
				Array.Copy(
					sourceArray: texture,
					sourceIndex: x1,
					destinationArray: slanted,
					destinationIndex: x2 + newXside,
					length: 4);
				Array.Copy(
					sourceArray: texture,
					sourceIndex: x1,
					destinationArray: slanted,
					destinationIndex: x2 + newXside + 4,
					length: 4);
				Array.Copy(
					sourceArray: slanted,
					sourceIndex: x2 + newXside,
					destinationArray: slanted,
					destinationIndex: x2 + newXside2,
					length: 8);
				Array.Copy(
					sourceArray: texture,
					sourceIndex: x1,
					destinationArray: slanted,
					destinationIndex: x2 + newXside2 + newXside + 4,
					length: 4);
			}
		return slanted;
	}
	/// <summary>
	/// Skews an image for use as a short isometric wall tile sloping down
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>new raw rgba8888 pixel data of double the source texture width</returns>
	public static byte[] IsoSlantDownShort(this byte[] texture, ushort width = 0)
	{
		int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2,
			ySide = (width < 1 ? xSide : texture.Length / width) >> 2,
			newXside = xSide * 2;
		byte[] slanted = new byte[newXside * ((xSide >> 2) + 1 + ySide)];
		for (int y1 = 0, y2 = 0; y1 < texture.Length; y1 += xSide, y2 += newXside)
			for (int x1 = y1, x2 = y2; x1 < y1 + xSide; x1 += 4, x2 += newXside + 8)
			{
				Array.Copy(
					sourceArray: texture,
					sourceIndex: x1,
					destinationArray: slanted,
					destinationIndex: x2,
					length: 4);
				Array.Copy(
					sourceArray: texture,
					sourceIndex: x1,
					destinationArray: slanted,
					destinationIndex: x2 + newXside,
					length: 4);
				Array.Copy(
					sourceArray: texture,
					sourceIndex: x1,
					destinationArray: slanted,
					destinationIndex: x2 + newXside + 4,
					length: 4);
				Array.Copy(
					sourceArray: texture,
					sourceIndex: x1,
					destinationArray: slanted,
					destinationIndex: x2 + newXside + newXside + 4,
					length: 4);
			}
		return slanted;
	}
	/// <summary>
	/// Skews an image for use as an isometric wall tile sloping up
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>new raw rgba8888 pixel data of double the source texture width</returns>
	public static byte[] IsoSlantUp(this byte[] texture, ushort width = 0)
	{
		int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2,
			ySide = (width < 1 ? xSide : texture.Length / width) >> 2,
			newXside = xSide * 2,
			newXside2 = xSide << 2;
		byte[] slanted = new byte[newXside * (ySide * 2 + (xSide >> 2) + 1)];
		for (int y1 = 0, y2 = newXside * ((xSide >> 2) + 2); y1 < texture.Length; y1 += xSide, y2 += newXside2)
			for (int x1 = y1, x2 = y2; x1 < y1 + xSide; x1 += 4, x2 += -newXside + 8)
			{
				Array.Copy(
					sourceArray: texture,
					sourceIndex: x1,
					destinationArray: slanted,
					destinationIndex: x2,
					length: 4);
				Array.Copy(
					sourceArray: texture,
					sourceIndex: x1,
					destinationArray: slanted,
					destinationIndex: x2 - newXside,
					length: 4);
				Array.Copy(
					sourceArray: texture,
					sourceIndex: x1,
					destinationArray: slanted,
					destinationIndex: x2 - newXside + 4,
					length: 4);
				Array.Copy(
					sourceArray: slanted,
					sourceIndex: x2 - newXside,
					destinationArray: slanted,
					destinationIndex: x2 - newXside2,
					length: 8);
				Array.Copy(
					sourceArray: texture,
					sourceIndex: x1,
					destinationArray: slanted,
					destinationIndex: x2 - newXside2 - newXside + 4,
					length: 4);
			}
		return slanted;
	}
	/// <summary>
	/// Skews an image for use as a short isometric wall tile sloping up
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>new raw rgba8888 pixel data of double the source texture width</returns>
	public static byte[] IsoSlantUpShort(this byte[] texture, ushort width = 0)
	{
		int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2,
			ySide = (width < 1 ? xSide : texture.Length / width) >> 2,
			newXside = xSide * 2;
		byte[] slanted = new byte[newXside * ((xSide >> 2) + 1 + ySide)];
		for (int y1 = 0, y2 = newXside * ((xSide >> 2) + 1); y1 < texture.Length; y1 += xSide, y2 += newXside)
			for (int x1 = y1, x2 = y2; x1 < y1 + xSide; x1 += 4, x2 += -newXside + 8)
			{
				Array.Copy(
					sourceArray: texture,
					sourceIndex: x1,
					destinationArray: slanted,
					destinationIndex: x2,
					length: 4);
				Array.Copy(
					sourceArray: texture,
					sourceIndex: x1,
					destinationArray: slanted,
					destinationIndex: x2 - newXside,
					length: 4);
				Array.Copy(
					sourceArray: texture,
					sourceIndex: x1,
					destinationArray: slanted,
					destinationIndex: x2 - newXside + 4,
					length: 4);
				Array.Copy(
					sourceArray: texture,
					sourceIndex: x1,
					destinationArray: slanted,
					destinationIndex: x2 - newXside - newXside + 4,
					length: 4);
			}
		return slanted;
	}
	/// <summary>
	/// Rotates and stretches an image for use as an isometric floor tile
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="width">width of texture or 0 to assume square texture</param>
	/// <returns>new raw rgba8888 pixel data where the width is derived from the source image size by the formula "newWidth = (width + height - 1) * 2"</returns>
	public static byte[] IsoTile(this byte[] texture, ushort width = 0)
	{
		int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2,
			ySide = (width < 1 ? xSide : texture.Length / width) >> 2,
			newXside = (xSide + (ySide << 2)) * 2 - 8;
		byte[] tile = new byte[newXside * ((xSide >> 2) + ySide)];
		for (int y1 = 0, y2 = newXside * (xSide >> 2) - newXside; y1 < texture.Length; y1 += xSide, y2 += newXside + 8)
			for (int x1 = y1, x2 = y2; x1 < y1 + xSide; x1 += 4, x2 += -newXside + 8)
			{
				Array.Copy(
					sourceArray: texture,
					sourceIndex: x1,
					destinationArray: tile,
					destinationIndex: x2,
					length: 4);
				Array.Copy(
					sourceArray: texture,
					sourceIndex: x1,
					destinationArray: tile,
					destinationIndex: x2 + 4,
					length: 4);
				Array.Copy(
					sourceArray: tile,
					sourceIndex: x2,
					destinationArray: tile,
					destinationIndex: x2 + newXside,
					length: 8);
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
	public static byte[] Crop(this byte[] texture, int x, int y, int croppedWidth, int croppedHeight, ushort width = 0)
	{
		if (width < 1)
			width = (ushort)Math.Sqrt(texture.Length >> 2);
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
		int ySide = (width < 1 ? xSide : texture.Length / width) >> 2;
		if (y > ySide) throw new ArgumentException("y > ySide. y: \"" + y + "\", ySide: \"" + ySide + "\"");
		if (y + croppedHeight > ySide)
			croppedHeight = ySide - y;
		croppedWidth <<= 2; // croppedWidth *= 4;
		if (x + croppedWidth > xSide)
			croppedWidth = xSide - x;
		byte[] cropped = new byte[croppedWidth * croppedHeight];
		for (int y1 = y * xSide + x, y2 = 0; y2 < cropped.Length; y1 += xSide, y2 += croppedWidth)
			Array.Copy(
				sourceArray: texture,
				sourceIndex: y1,
				destinationArray: cropped,
				destinationIndex: y2,
				length: croppedWidth);
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
	public static byte[] Crop2Content(this byte[] texture, out ushort cutLeft, out ushort cutTop, out ushort croppedWidth, out ushort croppedHeight, ushort width = 0, byte threshold = DefaultTransparencyThreshold)
	{
		if (width < 1)
			width = (ushort)Math.Sqrt(texture.Length >> 2);
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
	public static void Crop2ContentInfo(this byte[] texture, out ushort cutLeft, out ushort cutTop, out ushort croppedWidth, out ushort croppedHeight, ushort width = 0, byte threshold = DefaultTransparencyThreshold)
	{
		if (width < 1)
			width = (ushort)Math.Sqrt(texture.Length >> 2);
		int xSide = width << 2,
			indexTop, indexBottom;
		for (indexTop = 3; indexTop < texture.Length && texture[indexTop] < threshold; indexTop += 4) { }
		cutTop = (ushort)(indexTop / xSide);
		indexTop = (ushort)(cutTop * xSide);
		for (indexBottom = texture.Length - 1; indexBottom > indexTop && texture[indexBottom] < threshold; indexBottom -= 4) { }
		int cutBottom = indexBottom / xSide + 1;
		croppedHeight = (ushort)(cutBottom - cutTop);
		bool alpha(ushort x, ushort y) => texture[y * xSide + (x << 2) + 3] < threshold;
		cutLeft = (ushort)(width - 1);
		ushort cutRight = 0;
		for (ushort y = cutTop; y < cutBottom; y++)
		{
			ushort left;
			for (left = 0; left < cutLeft && alpha(left, y); left++) { }
			if (left < cutLeft)
				cutLeft = left;
			ushort right;
			for (right = (ushort)(width - 1); right > cutRight && alpha(right, y); right--) { }
			if (right > cutRight)
				cutRight = right;
		}
		croppedWidth = (ushort)(cutRight - cutLeft + 1);
	}
	public static byte[] Crop2ContentPlus1(this byte[] texture, out int cutLeft, out int cutTop, out ushort croppedWidth, out ushort croppedHeight, ushort width = 0, byte threshold = DefaultTransparencyThreshold)
	{
		if (width < 1)
			width = (ushort)Math.Sqrt(texture.Length >> 2);
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
		return new byte[croppedWidth * 4 * croppedHeight]
			.DrawInsert(
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
	}
	public static byte[] TransparentOutline(byte[] texture, ushort width = 0, byte threshold = DefaultTransparencyThreshold) => UInt2ByteArray(TransparentOutline(Byte2UIntArray(texture), width, threshold));
	public static uint[] TransparentOutline(uint[] texture, ushort width = 0, byte threshold = DefaultTransparencyThreshold)
	{
		if (width < 1)
			width = (ushort)Math.Sqrt(texture.Length >> 2);
		uint[] result = new uint[texture.Length];
		Array.Copy(
			sourceArray: texture,
			destinationArray: result,
			length: result.Length);
		int height = texture.Length / width;
		int Index(int x, int y) => x * width + y;
		List<uint> neighbors = new(9);
		void Add(int x, int y)
		{
			if (x >= 0 && y >= 0 && x < width && y < height
				&& texture[Index(x, y)] is uint pixel
				&& (byte)pixel >= threshold)
				neighbors.Add(pixel);
		}
		uint Average()
		{
			int count = neighbors.Count();
			if (count == 1)
				return neighbors.First() & 0xFFFFFF00u;
			int r = 0, g = 0, b = 0;
			foreach (uint color in neighbors)
			{
				r += (byte)(color >> 24);
				g += (byte)(color >> 16);
				b += (byte)(color >> 8);
			}
			return Color((byte)(r / count), (byte)(g / count), (byte)(b / count), 0);
		}
		for (int x = 0; x < width; x++)
			for (int y = 0; y < height; y++)
				if ((byte)texture[Index(x, y)] < threshold)
				{
					neighbors.Clear();
					Add(x - 1, y);
					Add(x + 1, y);
					Add(x, y - 1);
					Add(x, y + 1);
					if (neighbors.Count > 0)
						result[Index(x, y)] = Average();
					else
					{
						Add(x - 1, y - 1);
						Add(x + 1, y - 1);
						Add(x - 1, y + 1);
						Add(x + 1, y + 1);
						if (neighbors.Count > 0)
							result[Index(x, y)] = Average();
						else // Make non-border transparent pixels transparent black
							result[Index(x, y)] = 0;
					}
				}
		return result;
	}
	/// <summary>
	/// Adds a 1 pixel wide transparent border around the edges of a texture only if the edges aren't already transparent
	/// </summary>
	/// <param name="texture">raw rgba8888 pixel data of source image</param>
	/// <param name="result">new texture with transparent border or null if unnecessary</param>
	/// <param name="addLeft">pixels added to the top in result</param>
	/// <param name="addTop">pixels added to the top in result</param>
	/// <param name="resultWidth">width of result or 0 if unnecessary</param>
	/// <param name="resultHeight">height of result or 0 if unnecessary</param>
	/// <param name="width">width of source texture or 0 to assume square texture</param>
	/// <param name="threshold">alpha channel lower than this will be evaluated as transparent</param>
	/// <returns>true if making a new texture was necessary</returns>
	public static bool NeedsTransparentBorder(byte[] texture, out byte[] result, out ushort addLeft, out ushort addTop, out ushort resultWidth, out ushort resultHeight, ushort width = 0, byte threshold = DefaultTransparencyThreshold)
	{
		addLeft = 0;
		addTop = 0;
		if (width < 1)
			width = (ushort)Math.Sqrt(texture.Length >> 2);
		ushort height = (ushort)(texture.Length / width);
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
		result = new byte[resultWidth * 4 * resultHeight]
			.DrawInsert(
				x: addTop,
				y: addLeft,
				insert: texture,
				insertWidth: width,
				width: resultWidth);
		return true;
	}
	public static byte[] Outline(this byte[] texture, ushort width = 0, uint color = DefaultOutlineColor, byte threshold = DefaultTransparencyThreshold)
	{
		if (width < 1)
			width = (ushort)Math.Sqrt(texture.Length >> 2);
		int xSide = width << 2;
		byte[] result = new byte[texture.Length];
		Array.Copy(texture, result, result.Length);
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
	public static byte[] Resize(this byte[] texture, ushort newWidth, ushort newHeight, ushort width = 0)
	{
		if (newWidth < 1) throw new ArgumentOutOfRangeException("newWidth cannot be smaller than 1. Was: \"" + newWidth + "\"");
		if (newHeight < 1) throw new ArgumentOutOfRangeException("newHeight cannot be smaller than 1. Was: \"" + newHeight + "\"");
		newWidth <<= 2; // newWidth *= 4;
		int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2;
		byte[] resized = new byte[newWidth * newHeight];
		if (newWidth == xSide)
			Array.Copy(
				sourceArray: texture,
				destinationArray: resized,
				length: Math.Min(texture.Length, resized.Length));
		else
		{
			int newXside = Math.Min(xSide, newWidth);
			for (int y1 = 0, y2 = 0; y1 < texture.Length && y2 < resized.Length; y1 += xSide, y2 += newWidth)
				Array.Copy(
					sourceArray: texture,
					sourceIndex: y1,
					destinationArray: resized,
					destinationIndex: y2,
					length: newXside);
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
	public static byte[] Tile(this byte[] texture, ushort width = 0, byte factorX = 2, byte factorY = 2)
	{
		if (factorX < 1 || factorY < 1 || factorX < 2 && factorY < 2) return (byte[])texture.Clone();
		byte[] tiled = new byte[texture.Length * factorX * factorY];
		int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2,
			newXside = xSide * factorX;
		if (factorX > 1)
			for (int y1 = 0, y2 = 0; y1 < texture.Length; y1 += xSide, y2 += newXside)
				for (int x = 0; x < newXside; x += xSide)
					Array.Copy(
						sourceArray: texture,
						sourceIndex: y1,
						destinationArray: tiled,
						destinationIndex: y2 + x,
						length: xSide);
		else
			Array.Copy(
				sourceArray: texture,
				destinationArray: tiled,
				length: texture.Length);
		if (factorY > 1)
		{
			int xScaledLength = texture.Length * factorX;
			for (int y = xScaledLength; y < tiled.Length; y += xScaledLength)
				Array.Copy(
					sourceArray: tiled,
					sourceIndex: 0,
					destinationArray: tiled,
					destinationIndex: y,
					length: xScaledLength);
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
	/// <returns>new raw rgba8888 pixel data of newWidth = width * factorX</returns>
	public static byte[] Upscale(this byte[] texture, byte scaleX = 1, byte scaleY = 1, ushort width = 0)
	{
		if (scaleX < 1 || scaleY < 1 || scaleX < 2 && scaleY < 2) return (byte[])texture.Clone();
		int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2,
			newXside = xSide * scaleX,
			newXsidefactorY = newXside * scaleY;
		byte[] scaled = new byte[texture.Length * scaleY * scaleX];
		if (scaleX < 2)
			for (int y1 = 0, y2 = 0; y1 < texture.Length; y1 += xSide, y2 += newXsidefactorY)
				for (int z = y2; z < y2 + newXsidefactorY; z += newXside)
					Array.Copy(
						sourceArray: texture,
						sourceIndex: y1,
						destinationArray: scaled,
						destinationIndex: z,
						length: xSide);
		else
		{
			int factorX4 = scaleX << 2;
			for (int y1 = 0, y2 = 0; y1 < texture.Length; y1 += xSide, y2 += newXsidefactorY)
			{
				for (int x1 = y1, x2 = y2; x1 < y1 + xSide; x1 += 4, x2 += factorX4)
					for (int z = 0; z < factorX4; z += 4)
						Array.Copy(
							sourceArray: texture,
							sourceIndex: x1,
							destinationArray: scaled,
							destinationIndex: x2 + z,
							length: 4);
				for (int z = y2 + newXside; z < y2 + newXsidefactorY; z += newXside)
					Array.Copy(
						sourceArray: scaled,
						sourceIndex: y2,
						destinationArray: scaled,
						destinationIndex: z,
						length: newXside);
			}
		}
		return scaled;
	}
	public static byte[] Upscale(this byte[] texture, out ushort newWidth, byte scaleX = 1, byte scaleY = 1, ushort width = 0)
	{
		newWidth = (ushort)((width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) * scaleX);
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
	public static byte[,] Upscale(this byte[,] bytes, byte scaleX = 1, byte scaleY = 1)
	{
		ushort width = (ushort)bytes.GetLength(0),
			height = (ushort)bytes.GetLength(1);
		byte[,] scaled = new byte[width * scaleX, height * scaleY];
		for (ushort x1 = 0, x2 = 0; x1 < width; x1++, x2 += scaleX)
			for (ushort y1 = 0, y2 = 0; y1 < height; y1++, y2 += scaleX)
			{
				byte @byte = bytes[x1, y1];
				for (ushort x = 0; x < scaleX; x++)
					for (ushort y = 0; y < scaleY; y++)
						scaled[x2 + x, y2 + y] = @byte;
			}
		return scaled;
	}
	#endregion Image manipulation
	#region Utilities
	public static uint Pixel(this byte[] texture, ushort x, ushort y, ushort width = 0) => BinaryPrimitives.ReadUInt32BigEndian(texture.AsSpan(
		start: y * ((width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2) + (x << 2),
		length: 4));
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
			(ushort)(length / width >> 2)
			: (ushort)Math.Sqrt(length >> 2);
	public static uint Color(byte r, byte g, byte b, byte a) => (uint)(r << 24 | g << 16 | b << 8 | a);
	/// <param name="rgba">rgba8888, Big Endian</param>
	/// <returns>argb8888, Big Endian</returns>
	public static uint Rgba2argb(this uint rgba) => (rgba << 24) | (rgba >> 8);
	/// <param name="rgba">argb8888, Big Endian</param>
	/// <returns>rgba8888, Big Endian</returns>
	public static uint Argb2rgba(this uint argb) => (argb << 8) | (argb >> 24);
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
	public static byte[] Index2ByteArray(this byte[] indices, uint[] palette)
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
	public static HashSet<T> Append<T>(this HashSet<T> hashSet, params T[] other) => hashSet.Append(other.AsEnumerable());
	/// <summary>
	/// I understand why they decided to name it HashSet.UnionWith instead of HashSet.AddRange.
	/// But I do not understand why UnionWith returns void instead of returning the HashSet.
	/// </summary>
	public static HashSet<T> Append<T>(this HashSet<T> hashSet, IEnumerable<T> other)
	{
		hashSet.UnionWith(other);
		return hashSet;
	}
	public static List<T> Append<T>(this List<T> list, params T[] other) => list.Append(other.AsEnumerable());
	public static List<T> Append<T>(this List<T> list, IEnumerable<T> other)
	{
		list.AddRange(other);
		return list;
	}
	public static uint[] PaletteFromTexture(this byte[] texture)
	{
		uint[] palette = [.. new HashSet<uint> { 0u }
				.Append(texture.Byte2UIntArray())
				.OrderBy(@uint => @uint)
				.Take(byte.MaxValue)],
			result = new uint[byte.MaxValue];
		Array.Copy(
			sourceArray: palette,
			destinationArray: result,
			length: palette.Length);
		return result;
	}
	public static byte[] Byte2IndexArray(this byte[] bytes, uint[] palette)
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
	public static uint[] Index2UIntArray(this byte[] indices, uint[] palette) => [.. indices.Select(@byte => palette[@byte])];
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
	public static byte[,] OneDToTwoD(this byte[] bytes, ushort width = 0)
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
	public static uint[] Byte2UIntArray(this byte[] bytes)
	{
		uint[] uints = new uint[bytes.Length >> 2];
		for (int i = 0, j = 0; i < bytes.Length; i += 4)
			uints[j++] = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(
				start: i,
				length: 4));
		return uints;
	}
	public static uint[,] Texture2UInt2D(this byte[] texture, ushort width = 0)
	{
		if (width < 1)
			width = (ushort)Math.Sqrt(texture.Length >> 2);
		int height = (texture.Length >> 2) / width,
			xSide = width << 2;
		uint[,] uints = new uint[width, height];
		for (int y = 0, y2 = 0; y < height; y++, y2 += xSide)
			for (int x = 0, x2 = 0; x < width; x++, x2 += 4)
				uints[x, y] = BinaryPrimitives.ReadUInt32BigEndian(texture.AsSpan(
					start: y2 + x2,
					length: 4));
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
					destination: texture.AsSpan(
						start: y2 + x2,
						length: 4),
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
	public static uint[] LoadPalette(string @string) => LoadPalette(new MemoryStream(Encoding.UTF8.GetBytes(@string)));
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
