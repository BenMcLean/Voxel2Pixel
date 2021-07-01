using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Voxel2Pixel
{
	/// <summary>
	/// x is width, y is height
	/// (i << 2 == i * 4)
	/// (i >> 2 == i / 4) when i is a positive integer
	/// </summary>
	public static class TextureMethods
	{
		//TODO: Resize
		//TODO: DrawTriangle
		//TODO: DrawEllipse
		public static byte R(this int color) => (byte)(color >> 24);
		public static byte G(this int color) => (byte)(color >> 16);
		public static byte B(this int color) => (byte)(color >> 8);
		public static byte A(this int color) => (byte)color;
		public static int Color(byte r, byte g, byte b, byte a) => r << 24 | g << 16 | b << 8 | a;
		/// <param name="index">Palette indexes (one byte per pixel)</param>
		/// <param name="palette">256 rgba8888 color values</param>
		/// <returns>rgba8888 texture (four bytes per pixel)</returns>
		public static byte[] Index2ByteArray(this byte[] index, int[] palette)
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
		/// <param name="index">Palette indexes (one byte per pixel)</param>
		/// <param name="palette">256 rgba8888 color values</param>
		/// <returns>rgba8888 texture (one int per pixel)</returns>
		public static int[] Index2IntArray(this byte[] index, int[] palette)
		{
			int[] ints = new int[index.Length];
			for (int i = 0; i < index.Length; i++)
				ints[i] = palette[index[i]];
			return ints;
		}
		public static byte[] DrawPixel(this byte[] texture, int color, int x, int y, int width = 0) => DrawPixel(texture, (byte)(color >> 24), (byte)(color >> 16), (byte)(color >> 8), (byte)color, x, y, width);
		public static byte[] DrawPixel(this byte[] texture, byte r, byte g, byte b, byte a, int x, int y, int width = 0)
		{
			if (x < 0 || y < 0) return texture;
			int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2,
				ySide = (width < 1 ? xSide : texture.Length / width) >> 2;
			x <<= 2; //x *= 4;
			if (x >= xSide || y >= ySide) return texture;
			int offset = y * xSide + x;
			texture[offset] = r;
			texture[offset + 1] = g;
			texture[offset + 2] = b;
			texture[offset + 3] = a;
			return texture;
		}
		public static byte[] DrawRectangle(this byte[] texture, int color, int x, int y, int rectHeight, int rectWidth = 0, int width = 0) => DrawRectangle(texture, (byte)(color >> 24), (byte)(color >> 16), (byte)(color >> 8), (byte)color, x, y, rectHeight, rectWidth, width);
		public static byte[] DrawRectangle(this byte[] texture, byte r, byte g, byte b, byte a, int x, int y, int rectWidth, int rectHeight = 0, int width = 0)
		{
			if (rectHeight < 1) rectHeight = rectWidth;
			int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2,
				ySide = (width < 1 ? xSide : texture.Length / width) >> 2,
				x4 = x << 2;
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
			if (rectWidth < 1 || rectHeight < 1 || x4 >= xSide || y >= ySide) return texture;
			if ((x + rectWidth) << 2 >= xSide) rectWidth = (xSide >> 2) - x;
			if (y + rectHeight >= ySide) rectHeight = (ySide >> 2) - y;
			int offset = y * xSide + x4,
				rectWidth4 = rectWidth << 2,
				yStop = offset + xSide * rectHeight;
			texture[offset] = r;
			texture[offset + 1] = g;
			texture[offset + 2] = b;
			texture[offset + 3] = a;
			for (int x2 = offset + 4; x2 < offset + rectWidth4; x2 += 4)
				Array.Copy(texture, offset, texture, x2, 4);
			for (int y2 = offset + xSide; y2 < yStop; y2 += xSide)
				Array.Copy(texture, offset, texture, y2, rectWidth4);
			return texture;
		}
		public static byte[] Crop(this byte[] texture, int x, int y, int croppedWidth, int croppedHeight, int width = 0)
		{
			if (x < 0)
			{
				croppedWidth += x;
				x = 0;
			}
			if (y < 0)
			{
				croppedHeight += y;
				y = 0;
			}
			if (croppedWidth < 0 || croppedHeight < 0) return texture;
			int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2;
			x <<= 2; // x *= 4;
			if (x > xSide) return texture;
			int ySide = (width < 1 ? xSide : texture.Length / width) >> 2;
			if (y > ySide) return texture;
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
		public static byte[] Insert(this byte[] texture, int x, int y, byte[] insert, int insertWidth = 0, int width = 0)
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
				Array.Copy(insert, y2, texture, y1, actualInsertXside);
			return texture;
		}
		public static byte[] Tile(this byte[] texture, int xFactor = 2, int yFactor = 2, int width = 0)
		{
			if (xFactor < 1 || yFactor < 1 || (xFactor < 2 && yFactor < 2)) return texture;
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
		public static byte[] Upscale(this byte[] texture, int xFactor, int yFactor, int width = 0)
		{
			if (xFactor < 1 || yFactor < 1 || (xFactor < 2 && yFactor < 2)) return texture;
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
		public static byte[] FlipX(this byte[] texture, int width = 0)
		{
			int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2;
			byte[] flipped = new byte[texture.Length];
			for (int y = 0; y < flipped.Length; y += xSide)
				Array.Copy(texture, y, flipped, flipped.Length - xSide - y, xSide);
			return flipped;
		}
		public static byte[] FlipY(this byte[] texture, int width = 0)
		{
			int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2;
			byte[] flipped = new byte[texture.Length];
			for (int y = 0; y < flipped.Length; y += xSide)
				for (int x = 0; x < xSide; x += 4)
					Array.Copy(texture, y + x, flipped, y + xSide - 4 - x, 4);
			return flipped;
		}
		/// <param name="ints">rgba8888 color values (one int per pixel)</param>
		/// <returns>rgba8888 texture (four bytes per pixel)</returns>
		public static byte[] Int2ByteArray(this int[] ints)
		{
			byte[] bytes = new byte[ints.Length << 2];
			for (int i = 0, j = 0; i < ints.Length; i++)
			{
				bytes[j++] = (byte)(ints[i] >> 24);
				bytes[j++] = (byte)(ints[i] >> 16);
				bytes[j++] = (byte)(ints[i] >> 8);
				bytes[j++] = (byte)ints[i];
			}
			return bytes;
		}
		/// <param name="bytes">rgba8888 color values (four bytes per pixel)</param>
		/// <returns>rgba8888 texture (one int per pixel)</returns>
		public static int[] Byte2IntArray(this byte[] bytes)
		{
			int[] ints = new int[bytes.Length >> 2];
			for (int i = 0, j = 0; i < bytes.Length; i += 4)
				ints[j++] = (bytes[i] << 24)
					| (bytes[i + 1] << 16)
					| (bytes[i + 2] << 8)
					| bytes[i + 3];
			return ints;
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
		public static int[] LoadPalette(string @string) => LoadPalette(new MemoryStream(Encoding.UTF8.GetBytes(@string)));
		public static int[] LoadPalette(Stream stream)
		{
			int[] result;
			using (StreamReader streamReader = new StreamReader(stream))
			{
				string line;
				while (string.IsNullOrWhiteSpace(line = streamReader.ReadLine().Trim())) { }
				if (!line.Equals("JASC-PAL") || !streamReader.ReadLine().Trim().Equals("0100"))
					throw new InvalidDataException("Palette stream is an incorrectly formatted JASC palette.");
				if (!int.TryParse(streamReader.ReadLine()?.Trim(), out int numColors)
				 || numColors != 256)
					throw new InvalidDataException("Palette stream does not contain exactly 256 colors.");
				result = new int[numColors];
				for (int i = 0; i < numColors; i++)
				{
					string[] tokens = streamReader.ReadLine()?.Trim().Split(' ');
					if (tokens == null || tokens.Length != 3
						|| !byte.TryParse(tokens[0], out byte r)
						|| !byte.TryParse(tokens[1], out byte g)
						|| !byte.TryParse(tokens[2], out byte b))
						throw new InvalidDataException("Palette stream is an incorrectly formatted JASC palette.");
					result[i] = (r << 24)
						| (g << 16)
						| (b << 8)
						| (i == 255 ? 0 : 255);
				}
			}
			return result;
		}
		/*
		public static void Print<T>(T[] texture, int width = 0)
		{
			int xSide = width < 1 ? (int)System.Math.Sqrt(texture.Length >> 2) : width,
				ySide = width < 1 ? xSide << 2 : texture.Length / width;
			for (int x = 0; x < xSide; x++)
			{
				for (int y = 0; y < ySide; y += 4)
					for (int z = 0; z < 4; z++)
						Console.Write(texture[x * ySide + y + z] + ", ");
				Console.WriteLine();
			}
		}
		*/
	}
}
