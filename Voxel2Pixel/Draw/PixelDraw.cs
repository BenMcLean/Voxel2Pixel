using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Voxel2Pixel.Draw
{
	/// <summary>
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
		//TODO: DrawTriangle
		//TODO: DrawLine
		//TODO: DrawCircle
		//TODO: DrawEllipse
		#region Drawing
		/// <summary>
		/// Draws one pixel of the specified color
		/// </summary>
		/// <param name="texture">raw rgba8888 pixel data</param>
		/// <param name="color">rgba color to draw</param>
		/// <param name="width">width of texture or 0 to assume square texture</param>
		/// <returns>same texture with pixel drawn</returns>
		public static byte[] DrawPixel(this byte[] texture, uint color, ushort x, ushort y, ushort width = 0)
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
		public static byte[] DrawRectangle(this byte[] texture, uint color, int x, int y, int rectWidth, int rectHeight, ushort width = 0) => texture.DrawRectangle((byte)(color >> 24), (byte)(color >> 16), (byte)(color >> 8), (byte)color, x, y, rectWidth, rectHeight, width);
		/// <summary>
		/// Draws a rectangle of the specified color
		/// </summary>
		/// <param name="texture">raw rgba8888 pixel data to be modified</param>
		/// <param name="x">upper left corner of rectangle</param>
		/// <param name="y">upper left corner of rectangle</param>
		/// <param name="width">width of texture or 0 to assume square texture</param>
		/// <returns>same texture with rectangle drawn</returns>
		public static byte[] DrawRectangle(this byte[] texture, byte red, byte green, byte blue, byte alpha, int x, int y, int rectWidth = 1, int rectHeight = 1, ushort width = 0)
		{
			//if (rectWidth == 1 && rectHeight == 1)
			//	return texture.DrawPixel(red, green, blue, alpha, (ushort)x, (ushort)y, width);
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
			texture[offset] = red;
			texture[offset + 1] = green;
			texture[offset + 2] = blue;
			texture[offset + 3] = alpha;
			for (int x2 = offset + 4; x2 < offset + rectWidth4; x2 += 4)
				Array.Copy(texture, offset, texture, x2, 4);
			for (int y2 = offset + xSide; y2 < yStop; y2 += xSide)
				Array.Copy(texture, offset, texture, y2, rectWidth4);
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
				Array.Copy(insert, insertY * insertXside, texture, y * xSide, Math.Min(insert.Length - insertY * insertXside + insertX, texture.Length - y * xSide));
			else
				for (int y1 = y * xSide + x, y2 = insertY * insertXside + insertX; y1 + actualInsertXside < texture.Length && y2 < insert.Length; y1 += xSide, y2 += insertXside)
					Array.Copy(insert, y2, texture, y1, actualInsertXside);
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
		public static byte[] DrawTransparentInsert(this byte[] texture, int x, int y, byte[] insert, int insertWidth = 0, ushort width = 0, byte threshold = 128)
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
						Array.Copy(insert, y2 + x1, texture, y1 + x1, 4);
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
				Array.Copy(texture, offset + xSide, texture, offset, Math.Min(areaWidth4, xSide - x4));
			if (y + areaHeight < ySide)
				Array.Copy(texture, stop - xSide, texture, stop, Math.Min(areaWidth4, xSide - x4));
			for (int y1 = Math.Max(x4, offset); y1 <= stop; y1 += xSide)
			{
				if (x > 0)
					Array.Copy(texture, y1, texture, y1 - 4, 4);
				if (x4 + areaWidth4 < xSide)
					Array.Copy(texture, y1 + areaWidth4 - 4, texture, y1 + areaWidth4, 4);
			}
			return texture;
		}
		/*
		public static byte[] DrawTriangle(this byte[] texture, int color, int x, int y, int triangleWidth, int triangleHeight, ushort width = 0) => DrawTriangle(texture, (byte)(color >> 24), (byte)(color >> 16), (byte)(color >> 8), (byte)color, x, y, triangleWidth, triangleHeight, width);
		public static byte[] DrawTriangle(this byte[] texture, byte red, byte green, byte blue, byte alpha, int x, int y, int triangleWidth, int triangleHeight, ushort width = 0)
		{
			int textureWidth = width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width,
				xSide = textureWidth << 2,
				ySide = (width < 1 ? xSide : texture.Length / width) >> 2;
			if ((x < 0 && x + triangleWidth < 0)
				|| (x > textureWidth && x + triangleWidth > textureWidth)
				|| (y < 0 && y + triangleHeight < 0)
				|| (y > ySide && y + triangleHeight > ySide))
				return texture; // Triangle is completely outside the texture bounds.
			int realX = x < 1 ? 0 : Math.Min(xSide, x << 2),
				realY = y < 1 ? 0 : (Math.Min(y, ySide) * xSide);
			bool isWide = triangleWidth > 0,
				isTall = triangleHeight > 0;
			triangleWidth = Math.Abs(triangleWidth);
			triangleHeight = Math.Abs(triangleHeight);
			int triangleWidth4 = triangleWidth << 2;
			//if ((x + triangleWidth) >> 2 > xSide || y > ySide) throw new NotImplementedException();
			int offset = realY * xSide + (realX << 2);
			texture[offset] = red;
			texture[offset + 1] = green;
			texture[offset + 2] = blue;
			texture[offset + 3] = alpha;
			int xStop, yStop, longest;
			if (isWide)
			{
				xStop = Math.Min(Math.Min((realY + 1) * xSide, offset + triangleWidth4), texture.Length - 4);
				longest = xStop - offset;
				for (int x1 = offset + 4; x1 < xStop; x1 += 4)
					Array.Copy(texture, offset, texture, x1, 4);
			}
			else
			{
				xStop = Math.Max(Math.Max(realY * xSide, offset - triangleWidth4), 0);
				longest = offset - xStop;
				for (int x1 = offset - 4; x1 > xStop; x1 -= 4)
					Array.Copy(texture, offset, texture, x1, 4);
			}
			//int yStop = offset - triangleHeight * xSide;
			//float @float = (float)(triangleWidth - 1) / triangleHeight;
			//for (int y1 = offset - xSide, y2 = triangleHeight - 1; y1 > 0 && y1 > yStop; y1 -= xSide, y2--)
			//	Array.Copy(texture, offset, texture, y1, Math.Min(longest, ((int)(@float * y2) + 1) << 2));
			return texture;
		}
		*/
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
				Array.Copy(texture, y, flipped, flipped.Length - xSide - y, xSide);
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
					Array.Copy(texture, y + x, flipped, y + xSide - 4 - x, 4);
			return flipped;
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
					Array.Copy(texture, x1, rotated, x2, 4);
					Array.Copy(texture, x1, rotated, x2 + newXside, 4);
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
					Array.Copy(texture, x1, rotated, x2, 4);
					Array.Copy(texture, x1, rotated, x2 + 4, 4);
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
					Array.Copy(texture, x1, rotated, x2, 4);
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
					Array.Copy(texture, x1, rotated, x2, 4);
					Array.Copy(texture, x1, rotated, x2 + newXside, 4);
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
					Array.Copy(texture, x1, rotated, x2, 4);
					Array.Copy(texture, x1, rotated, x2 + 4, 4);
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
					Array.Copy(texture, x1, rotated, x2, 4);
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
					Array.Copy(texture, x1, rotated, x2, 4);
					Array.Copy(texture, x1, rotated, x2 + newXside, 4);
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
					Array.Copy(texture, x1, rotated, x2, 4);
					Array.Copy(texture, x1, rotated, x2 + 4, 4);
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
					Array.Copy(texture, x1, rotated, x2, 4);
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
					Array.Copy(texture, x1, rotated, x2, 4);
					Array.Copy(texture, x1, rotated, x2 + newXside, 4);
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
					Array.Copy(texture, x1, rotated, x2, 4);
					Array.Copy(texture, x1, rotated, x2 + 4, 4);
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
					Array.Copy(texture, x1, slanted, x2, 4);
					Array.Copy(texture, x1, slanted, x2 + newXside, 4);
					Array.Copy(texture, x1, slanted, x2 + newXside + 4, 4);
					Array.Copy(slanted, x2 + newXside, slanted, x2 + newXside2, 8);
					Array.Copy(texture, x1, slanted, x2 + newXside2 + newXside + 4, 4);
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
					Array.Copy(texture, x1, slanted, x2, 4);
					Array.Copy(texture, x1, slanted, x2 + newXside, 4);
					Array.Copy(texture, x1, slanted, x2 + newXside + 4, 4);
					Array.Copy(texture, x1, slanted, x2 + newXside + newXside + 4, 4);
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
					Array.Copy(texture, x1, slanted, x2, 4);
					Array.Copy(texture, x1, slanted, x2 - newXside, 4);
					Array.Copy(texture, x1, slanted, x2 - newXside + 4, 4);
					Array.Copy(slanted, x2 - newXside, slanted, x2 - newXside2, 8);
					Array.Copy(texture, x1, slanted, x2 - newXside2 - newXside + 4, 4);
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
					Array.Copy(texture, x1, slanted, x2, 4);
					Array.Copy(texture, x1, slanted, x2 - newXside, 4);
					Array.Copy(texture, x1, slanted, x2 - newXside + 4, 4);
					Array.Copy(texture, x1, slanted, x2 - newXside - newXside + 4, 4);
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
					Array.Copy(texture, x1, tile, x2, 4);
					Array.Copy(texture, x1, tile, x2 + 4, 4);
					Array.Copy(tile, x2, tile, x2 + newXside, 8);
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
			int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2;
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
				Array.Copy(texture, y1, cropped, y2, croppedWidth);
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
		public static byte[] TransparentCrop(this byte[] texture, out ushort cutLeft, out ushort cutTop, out ushort croppedWidth, out ushort croppedHeight, ushort width = 0, byte threshold = 128)
		{
			if (width < 1)
				width = (ushort)Math.Sqrt(texture.Length >> 2);
			TransparentCropInfo(texture, out cutLeft, out cutTop, out croppedWidth, out croppedHeight, width, threshold);
			return texture.Crop(
				x: cutLeft,
				y: cutTop,
				croppedWidth: croppedWidth,
				croppedHeight: croppedHeight,
				width: width);
		}
		public static void TransparentCropInfo(this byte[] texture, out ushort cutLeft, out ushort cutTop, out ushort croppedWidth, out ushort croppedHeight, ushort width = 0, byte threshold = 128)
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
			indexBottom = cutBottom * xSide;
			int indexLeft = xSide, indexRight = 0;
			for (int indexRow = indexTop; indexRow < indexBottom; indexRow += xSide)
			{
				int left;
				for (left = 3; left < indexLeft && texture[indexRow + left] < threshold; left += 4) { }
				indexLeft = Math.Min(indexLeft, left);
				int right;
				for (right = xSide - 1; right > indexRight && texture[indexRow + right] < threshold; right -= 4) { }
				indexRight = Math.Max(indexRight, right);
			}
			if (indexLeft >> 2 < 0)
				throw new Exception(indexLeft.ToString());
			cutLeft = (ushort)(indexLeft >> 2);
			croppedWidth = (ushort)(width - ((xSide - indexRight) >> 2) - cutLeft);
			croppedHeight = (ushort)(cutBottom - cutTop);
		}
		public static byte[] TransparentCropPlusOne(this byte[] texture, out int cutLeft, out int cutTop, out int croppedWidth, out int croppedHeight, ushort width = 0, byte threshold = 128)
		{
			if (width < 1)
				width = (ushort)Math.Sqrt(texture.Length >> 2);
			TransparentCropInfo(texture, out ushort cutLeftShort, out ushort cutTopShort, out ushort croppedWidthShort, out ushort croppedHeightShort, width, threshold);
			cutLeft = cutLeftShort - 1;
			cutTop = cutTopShort - 1;
			croppedWidth = croppedWidthShort + 2;
			croppedHeight = croppedHeightShort + 2;
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
					width: (ushort)croppedWidth);
		}
		public static byte[] TransparentOutline(byte[] texture, ushort width = 0, byte threshold = 128) => UInt2ByteArray(TransparentOutline(Byte2UIntArray(texture), width, threshold));
		public static uint[] TransparentOutline(uint[] texture, ushort width = 0, byte threshold = 128)
		{
			if (width < 1)
				width = (ushort)Math.Sqrt(texture.Length);
			uint[] result = new uint[texture.Length];
			Array.Copy(texture, result, result.Length);
			int height = texture.Length / width;
			int Index(int x, int y) => x * width + y;
			List<uint> neighbors = new List<uint>(9);
			void Add(int x, int y)
			{
				if (x >= 0 && y >= 0 && x < width && y < height
					&& texture[Index(x, y)] is uint pixel
					&& pixel.A() >= threshold)
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
					r += color.R();
					g += color.G();
					b += color.B();
				}
				return Color((byte)(r / count), (byte)(g / count), (byte)(b / count), 0);
			}
			for (int x = 0; x < width; x++)
				for (int y = 0; y < height; y++)
					if (texture[Index(x, y)].A() < threshold)
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
		public static bool NeedsTransparentBorder(byte[] texture, out byte[] result, out ushort addLeft, out ushort addTop, out ushort resultWidth, out ushort resultHeight, ushort width = 0, byte threshold = 128)
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
		public static byte[] Outline(this byte[] texture, ushort width = 0, uint color = 0x000000FFu, byte threshold = 128)
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
		public static byte[] CropSprite(this byte[] sprite, ushort width, out ushort newWidth, out ushort[] newOrigin, params ushort[] origin)
		{
			byte[] result = TransparentCrop(
				texture: sprite,
				cutLeft: out ushort cutLeft,
				cutTop: out ushort cutTop,
				croppedWidth: out newWidth,
				croppedHeight: out _,
				width: width);
			newOrigin = new ushort[] { (ushort)(origin[0] - cutLeft), (ushort)(origin[1] - cutTop) };
			return result;
		}
		public static byte[][] CropSprites(this byte[][] sprites, ushort[] widths, ushort[][] pixelOrigins, out ushort[] newWidths, out ushort[][] newPixelOrigins)
		{
			byte[][] newSprites = new byte[sprites.Length][];
			newWidths = new ushort[widths.Length];
			newPixelOrigins = new ushort[pixelOrigins.Length][];
			for (int i = 0; i < sprites.Length; i++)
			{
				newSprites[i] = sprites[i].CropSprite(
					width: widths[i],
					newWidth: out newWidths[i],
					newOrigin: out newPixelOrigins[i],
					origin: pixelOrigins[i]);
			}
			return newSprites;
		}
		public static byte[] CropOutlineSprite(this byte[] sprite, ushort width, out ushort newWidth, out ushort[] newOrigin, params ushort[] origin)
		{
			byte[] result = TransparentCropPlusOne(
				texture: sprite,
				cutLeft: out int cutLeft,
				cutTop: out int cutTop,
				croppedWidth: out int croppedWidth,
				croppedHeight: out _,
				width: width)
				.Outline((ushort)croppedWidth);
			newWidth = (ushort)croppedWidth;
			newOrigin = new ushort[] { (ushort)(origin[0] - cutLeft), (ushort)(origin[1] - cutTop) };
			return result;
		}
		public static byte[][] CropOutlineSprites(this byte[][] sprites, ushort[] widths, ushort[][] pixelOrigins, out ushort[] newWidths, out ushort[][] newPixelOrigins)
		{
			byte[][] newSprites = new byte[sprites.Length][];
			newWidths = new ushort[widths.Length];
			newPixelOrigins = new ushort[pixelOrigins.Length][];
			for (int i = 0; i < sprites.Length; i++)
			{
				newSprites[i] = sprites[i].CropOutlineSprite(
					width: widths[i],
					newWidth: out newWidths[i],
					newOrigin: out newPixelOrigins[i],
					origin: pixelOrigins[i]);
			}
			return newSprites;
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
				Array.Copy(texture, resized, Math.Min(texture.Length, resized.Length));
			else
			{
				int newXside = Math.Min(xSide, newWidth);
				for (int y1 = 0, y2 = 0; y1 < texture.Length && y2 < resized.Length; y1 += xSide, y2 += newWidth)
					Array.Copy(texture, y1, resized, y2, newXside);
			}
			return resized;
		}
		/// <summary>
		/// Tile an image
		/// </summary>
		/// <param name="texture">raw rgba8888 pixel data of source image</param>
		/// <param name="xFactor">number of times to tile horizontally</param>
		/// <param name="yFactor">number of times to tile vertically</param>
		/// <param name="width">width of texture or 0 to assume square texture</param>
		/// <returns>new raw rgba8888 pixel data of newWidth = width * xFactor</returns>
		public static byte[] Tile(this byte[] texture, ushort width = 0, ushort xFactor = 2, ushort yFactor = 2)
		{
			if (xFactor < 1 || yFactor < 1 || xFactor < 2 && yFactor < 2) return (byte[])texture.Clone();
			byte[] tiled = new byte[texture.Length * xFactor * yFactor];
			int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2,
				newXside = xSide * xFactor;
			if (xFactor > 1)
				for (int y1 = 0, y2 = 0; y1 < texture.Length; y1 += xSide, y2 += newXside)
					for (int x = 0; x < newXside; x += xSide)
						Array.Copy(texture, y1, tiled, y2 + x, xSide);
			else
				Array.Copy(texture, tiled, texture.Length);
			if (yFactor > 1)
			{
				int xScaledLength = texture.Length * xFactor;
				for (int y = xScaledLength; y < tiled.Length; y += xScaledLength)
					Array.Copy(tiled, 0, tiled, y, xScaledLength);
			}
			return tiled;
		}
		/// <summary>
		/// Simple nearest-neighbor upscaling by integer multipliers
		/// </summary>
		/// <param name="texture">raw rgba8888 pixel data of source image</param>
		/// <param name="xFactor">horizontal scaling factor</param>
		/// <param name="yFactor">vertical scaling factor</param>
		/// <param name="width">width of texture or 0 to assume square texture</param>
		/// <returns>new raw rgba8888 pixel data of newWidth = width * xFactor</returns>
		public static byte[] Upscale(this byte[] texture, ushort xFactor, ushort yFactor, ushort width = 0)
		{
			if (xFactor < 1 || yFactor < 1 || xFactor < 2 && yFactor < 2) return (byte[])texture.Clone();
			int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2,
				newXside = xSide * xFactor,
				newXsideYfactor = newXside * yFactor;
			byte[] scaled = new byte[texture.Length * yFactor * xFactor];
			if (xFactor < 2)
				for (int y1 = 0, y2 = 0; y1 < texture.Length; y1 += xSide, y2 += newXsideYfactor)
					for (int z = y2; z < y2 + newXsideYfactor; z += newXside)
						Array.Copy(texture, y1, scaled, z, xSide);
			else
			{
				int xFactor4 = xFactor << 2;
				for (int y1 = 0, y2 = 0; y1 < texture.Length; y1 += xSide, y2 += newXsideYfactor)
				{
					for (int x1 = y1, x2 = y2; x1 < y1 + xSide; x1 += 4, x2 += xFactor4)
						for (int z = 0; z < xFactor4; z += 4)
							Array.Copy(texture, x1, scaled, x2 + z, 4);
					for (int z = y2 + newXside; z < y2 + newXsideYfactor; z += newXside)
						Array.Copy(scaled, y2, scaled, z, newXside);
				}
			}
			return scaled;
		}
		public static byte[][] UpscaleSprites(this byte[][] sprites, ushort[] widths, ushort[][] pixelOrigins, ushort xFactor, ushort yFactor, out ushort[] newWidths, out ushort[][] newPixelOrigins)
		{
			byte[][] newSprites = new byte[sprites.Length][];
			newWidths = new ushort[widths.Length];
			newPixelOrigins = new ushort[pixelOrigins.Length][];
			for (int i = 0; i < sprites.Length; i++)
			{
				newSprites[i] = sprites[i].Upscale(
					xFactor: xFactor,
					yFactor: yFactor,
					width: widths[i]);
				newWidths[i] = (ushort)(widths[i] * xFactor);
				newPixelOrigins[i] = new ushort[] { (ushort)(pixelOrigins[i][0] * xFactor), (ushort)(pixelOrigins[i][1] * yFactor) };
			}
			return newSprites;
		}
		#endregion Image manipulation
		#region Utilities
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
		public static byte R(this uint color) => (byte)(color >> 24);
		public static byte G(this uint color) => (byte)(color >> 16);
		public static byte B(this uint color) => (byte)(color >> 8);
		public static byte A(this uint color) => (byte)color;
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
		/// <param name="index">Palette indexes (one byte per pixel)</param>
		/// <param name="palette">256 rgba8888 color values</param>
		/// <returns>rgba8888 texture (four bytes per pixel)</returns>
		public static byte[] Index2ByteArray(this byte[] index, uint[] palette)
		{
			byte[] bytes = new byte[index.Length << 2];
			for (int i = 0, j = 0; i < index.Length; i++)
			{
				bytes[j++] = (byte)(palette[index[i]] >> 24);
				bytes[j++] = (byte)(palette[index[i]] >> 16);
				bytes[j++] = (byte)(palette[index[i]] >> 8);
				bytes[j++] = (byte)palette[index[i]];
			}
			return bytes;
		}
		public static uint[] PaletteFromTexture(this byte[] texture)
		{
			List<uint> palette = new List<uint> { 0 };
			foreach (uint pixel in texture.Byte2UIntArray())
				if (!palette.Contains(pixel))
				{
					palette.Add(pixel);
					if (palette.Count >= byte.MaxValue)
						break;
				}
			uint[] result = new uint[byte.MaxValue];
			Array.Copy(palette.ToArray(), result, palette.Count);
			return result;
		}
		public static byte[] Byte2IndexArray(this byte[] bytes) => bytes.Byte2IndexArray(bytes.PaletteFromTexture());
		public static byte[] Byte2IndexArray(this byte[] bytes, uint[] palette)
		{
			byte[] indexes = new byte[bytes.Length >> 2];
			uint[] uints = bytes.Byte2UIntArray();
			for (int i = 0; i < indexes.Length; i++)
				indexes[i] = (byte)Math.Max(Array.IndexOf(palette, uints[i]), 0);
			return indexes;
		}
		/// <param name="index">Palette indexes (one byte per pixel)</param>
		/// <param name="palette">256 rgba8888 color values</param>
		/// <returns>rgba8888 texture (one int per pixel)</returns>
		public static uint[] Index2UIntArray(this byte[] index, uint[] palette)
		{
			uint[] uints = new uint[index.Length];
			for (int i = 0; i < index.Length; i++)
				uints[i] = palette[index[i]];
			return uints;
		}
		/// <param name="ints">rgba8888 color values (one int per pixel)</param>
		/// <returns>rgba8888 texture (four bytes per pixel)</returns>
		public static byte[] UInt2ByteArray(this uint[] uints)
		{
			byte[] bytes = new byte[uints.Length << 2];
			for (int i = 0, j = 0; i < uints.Length; i++)
			{
				bytes[j++] = (byte)(uints[i] >> 24);
				bytes[j++] = (byte)(uints[i] >> 16);
				bytes[j++] = (byte)(uints[i] >> 8);
				bytes[j++] = (byte)uints[i];
			}
			return bytes;
		}
		/// <param name="bytes">rgba8888 color values (four bytes per pixel)</param>
		/// <returns>rgba8888 texture (one int per pixel)</returns>
		public static uint[] Byte2UIntArray(this byte[] bytes)
		{
			uint[] uints = new uint[bytes.Length >> 2];
			for (int i = 0, j = 0; i < bytes.Length; i += 4)
				uints[j++] = (uint)(bytes[i] << 24
					| bytes[i + 1] << 16
					| bytes[i + 2] << 8
					| bytes[i + 3]);
			return uints;
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
			using (StreamReader streamReader = new StreamReader(stream))
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
}
