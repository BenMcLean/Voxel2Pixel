using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Voxel2Pixel
{
	/// <summary>
	/// x is width, y is height
	/// </summary>
	public static class TextureMethods
	{
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
			byte[] bytes = new byte[index.Length * 4];
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
			int ySide = width == 0 ? (int)Math.Sqrt(texture.Length / 4) : width,
				xSide = width == 0 ? ySide * 4 : texture.Length / width;
			x *= 4;
			if (x >= xSide || y >= ySide) return texture;
			int offset = y * xSide + x;
			texture[offset] = r;
			texture[offset + 1] = g;
			texture[offset + 2] = b;
			texture[offset + 3] = a;
			return texture;
		}

		public static byte[] DrawRectangle(this byte[] texture, int color, int x, int y, int rectHeight, int rectWidth = 0, int width = 0) => DrawRectangle(texture, (byte)(color >> 24), (byte)(color >> 16), (byte)(color >> 8), (byte)color, x, y, rectHeight, rectWidth, width);
		public static byte[] DrawRectangle(this byte[] texture, byte r, byte g, byte b, byte a, int x, int y, int rectHeight, int rectWidth = 0, int width = 0)
		{
			if (rectWidth < 1) rectWidth = rectHeight;
			int ySide = width == 0 ? (int)Math.Sqrt(texture.Length / 4) : width,
				xSide = width == 0 ? ySide * 4 : texture.Length / width;
			if (x < 0 || y < 0 || x >= xSide || y >= ySide) return texture;
			int offset = y * xSide + x * 4;
			if (y + rectWidth >= ySide) rectWidth = ySide - y;
			if ((x + rectHeight) * 4 >= xSide) rectHeight = xSide / 4 - x;
			int rectWidth4 = rectWidth * 4,
				yStop = offset + xSide * rectHeight;
			texture[offset] = r;
			texture[offset + 1] = g;
			texture[offset + 2] = b;
			texture[offset + 3] = a;
			for (int x2 = offset + 4; x2 < offset + rectWidth4; x2 += 4)
				Array.Copy(texture, offset, texture, x2, 4);
			for (int y2 = offset; y2 < yStop; y2 += xSide)
				Array.Copy(texture, offset, texture, y2, rectWidth4);
			return texture;
		}

		public static int[] Repeat256(this int[] pixels256)
		{
			int[] repeated = new int[4096];
			for (int x = 0; x < repeated.Length; x += 256)
				Array.Copy(pixels256, 0, repeated, x, 256);
			return repeated;
		}

		public static int[] Tile(this int[] squareTexture, int tileSqrt = 64)
		{
			int side = (int)Math.Sqrt(squareTexture.Length);
			int newSide = side * tileSqrt;
			int[] tiled = new int[squareTexture.Length * tileSqrt * tileSqrt];
			for (int x = 0; x < newSide; x++)
				for (int y = 0; y < newSide; y++)
					tiled[x * newSide + y] = squareTexture[x % side * side + y % side];
			return tiled;
		}

		public static byte[] Tile(this byte[] texture, int xFactor, int yFactor = 1, int width = 0) => TileY(TileX(texture, xFactor, width), yFactor);

		public static byte[] TileX(this byte[] texture, int factor = 2, int width = 0)
		{
			if (factor < 2) return texture;
			byte[] tiled = new byte[texture.Length * factor];
			int ySide = (width == 0 ? (int)Math.Sqrt(texture.Length / 4) : width) * 4,
				newYside = ySide * factor;
			for (int y1 = 0, y2 = 0; y1 < texture.Length; y1 += ySide, y2 += newYside)
				for (int x = 0; x < newYside; x += ySide)
					Array.Copy(texture, y1, tiled, y2 + x, ySide);
			return tiled;
		}

		public static byte[] TileY(this byte[] texture, int factor = 2)
		{
			if (factor < 2) return texture;
			byte[] tiled = new byte[texture.Length * factor];
			for (int y = 0; y < tiled.Length; y += texture.Length)
				Array.Copy(texture, 0, tiled, y, texture.Length);
			return tiled;
		}

		public static byte[] Upscale(this byte[] texture, int factor, bool x, bool y = false, int width = 0) => x && y ? Upscale(texture, factor, width) : x ? UpscaleX(texture, factor, width) : y ? UpscaleY(texture, factor, width) : texture;

		public static byte[] Upscale(this byte[] texture, int factor, int width = 0)
		{
			if (factor < 2) return texture;
			int ySide = width == 0 ? (int)Math.Sqrt(texture.Length / 4) : width,
				xSide = width == 0 ? ySide * 4 : texture.Length / width,
				newYside = ySide * factor,
				newXside = xSide * factor;
			byte[] scaled = new byte[texture.Length * factor * factor];
			for (int y = 0; y < newYside; y += factor)
			{
				for (int x = 0; x < newXside; x += 4)
					Array.Copy(texture, y / factor * xSide + (x / factor & -4), scaled, y * newXside + x, 4); // (y / factor & -4) == y / 4 / factor * 4
				for (int z = y + 1; z < y + factor; z++)
					Array.Copy(scaled, y * newXside, scaled, z * newXside, newXside);
			}
			return scaled;
		}

		public static int[] Upscale(this int[] texture, int factor, int width = 0)
		{
			if (factor < 2) return texture;
			int ySide = width == 0 ? (int)Math.Sqrt(texture.Length) : width,
				xSide = width == 0 ? ySide : texture.Length / width,
				newYside = ySide * factor,
				newXside = xSide * factor;
			int[] scaled = new int[texture.Length * factor * factor];
			for (int y = 0; y < newYside; y += factor)
			{
				for (int x = 0; x < newXside; x++)
					scaled[y * newXside + x] = texture[y / factor * xSide + x / factor];
				for (int z = y + 1; z < y + factor; z++)
					Array.Copy(scaled, y * newXside, scaled, z * newXside, newXside);
			}
			return scaled;
		}

		public static byte[] UpscaleXY(this byte[] texture, int xFactor, int yFactor, int width) => UpscaleY(UpscaleX(texture, xFactor, width), yFactor, width * xFactor);
		public static byte[] UpscaleX(this byte[] texture, int factor, int width = 0)
		{
			if (factor < 2) return texture;
			int ySide = width == 0 ? (int)Math.Sqrt(texture.Length / 4) : width,
				xSide = width == 0 ? ySide * 4 : texture.Length / width,
				newXside = xSide * factor,
				factor4 = factor * 4;
			byte[] scaled = new byte[texture.Length * factor];
			for (int y = 0; y < ySide; y++)
				for (int x = 0; x < xSide; x += 4)
					for (int z = 0; z < factor4; z += 4)
						Array.Copy(texture, y * xSide + x, scaled, y * newXside + x * factor + z, 4);
			return scaled;
		}

		public static byte[] UpscaleY(this byte[] texture, int factor, int width = 0)
		{
			if (factor < 2) return texture;
			int ySide = width == 0 ? (int)Math.Sqrt(texture.Length / 4) : width,
				xSide = width == 0 ? ySide * 4 : texture.Length / width;
			byte[] scaled = new byte[texture.Length * factor];
			for (int y = 0; y < ySide; y++)
				for (int z = 0; z < factor; z++)
					Array.Copy(texture, y * xSide, scaled, (y * factor + z) * xSide, xSide);
			return scaled;
		}

		public static byte[] FlipX(this byte[] texture, int width = 0)
		{
			int ySide = width == 0 ? (int)Math.Sqrt(texture.Length / 4) : width,
				xSide = width == 0 ? ySide * 4 : texture.Length / width;
			byte[] flipped = new byte[texture.Length];
			for (int y = 0; y < ySide; y++)
				for (int x = 0; x < xSide; x += 4)
					Array.Copy(texture, y * xSide + x, flipped, y * xSide + (xSide - 4 - x), 4);
			return flipped;
		}

		public static byte[] FlipY(this byte[] texture, int width = 0)
		{
			int ySide = width == 0 ? (int)Math.Sqrt(texture.Length / 4) : width,
				xSide = width == 0 ? ySide * 4 : texture.Length / width;
			byte[] flipped = new byte[texture.Length];
			for (int y = 0; y < ySide; y++)
				Array.Copy(texture, y * xSide, flipped, (ySide - 1 - y) * xSide, xSide);
			return flipped;
		}

		/// <param name="ints">rgba8888 color values (one int per pixel)</param>
		/// <returns>rgba8888 texture (four bytes per pixel)</returns>
		public static byte[] Int2ByteArray(this int[] ints)
		{
			byte[] bytes = new byte[ints.Length * 4];
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
			int[] ints = new int[bytes.Length / 4];
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
			int offset = 0;
			for (int i = 0; i < list.Length; i++)
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
			int xSide = width == 0 ? (int)System.Math.Sqrt(texture.Length / 4) : width,
				ySide = width == 0 ? xSide * 4 : texture.Length / width;
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
