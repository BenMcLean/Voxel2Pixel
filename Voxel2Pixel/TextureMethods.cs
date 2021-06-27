using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Voxel2Pixel
{
	public static class TextureMethods
	{
		public static byte R(this uint color) => (byte)(color >> 24);
		public static byte G(this uint color) => (byte)(color >> 16);
		public static byte B(this uint color) => (byte)(color >> 8);
		public static byte A(this uint color) => (byte)color;
		public static uint Color(byte r, byte g, byte b, byte a)
			=> (uint)(r << 24 | g << 16 | b << 8 | a);

		/// <param name="index">Palette indexes (one byte per pixel)</param>
		/// <param name="palette">256 rgba8888 color values</param>
		/// <returns>rgba8888 texture (four bytes per pixel)</returns>
		public static byte[] Index2ByteArray(this byte[] index, uint[] palette)
		{
			byte[] bytes = new byte[index.Length * 4];
			for (uint i = 0; i < index.Length; i++)
			{
				bytes[i * 4] = (byte)(palette[index[i]] >> 24);
				bytes[i * 4 + 1] = (byte)(palette[index[i]] >> 16);
				bytes[i * 4 + 2] = (byte)(palette[index[i]] >> 8);
				bytes[i * 4 + 3] = (byte)palette[index[i]];
			}
			return bytes;
		}

		/// <param name="index">Palette indexes (one byte per pixel)</param>
		/// <param name="palette">256 rgba8888 color values</param>
		/// <returns>rgba8888 texture (one int per pixel)</returns>
		public static uint[] Index2IntArray(this byte[] index, uint[] palette)
		{
			uint[] ints = new uint[index.Length];
			for (uint i = 0; i < index.Length; i++)
				ints[i] = palette[index[i]];
			return ints;
		}

		public static uint[] Repeat256(this uint[] pixels256)
		{
			uint[] repeated = new uint[4096];
			for (uint x = 0; x < repeated.Length; x += 256)
				Array.Copy(pixels256, 0, repeated, x, 256);
			return repeated;
		}

		public static uint[] Tile(this uint[] squareTexture, uint tileSqrt = 64)
		{
			uint side = (uint)Math.Sqrt(squareTexture.Length);
			uint newSide = side * tileSqrt;
			uint[] tiled = new uint[squareTexture.Length * tileSqrt * tileSqrt];
			for (uint x = 0; x < newSide; x++)
				for (uint y = 0; y < newSide; y++)
					tiled[x * newSide + y] = squareTexture[x % side * side + y % side];
			return tiled;
		}

		public static byte[] Upscale(this byte[] texture, int factor, int width = 0)
		{
			if (factor == 1) return texture;
			int xSide = width == 0 ? (int)Math.Sqrt(texture.Length / 4) : width,
				ySide = width == 0 ? xSide * 4 : texture.Length / width,
				newXside = xSide * factor,
				newYside = ySide * factor;
			byte[] scaled = new byte[texture.Length * factor * factor];
			for (int x = 0; x < newXside; x += factor)
			{
				for (int y = 0; y < newYside; y += 4)
					Array.Copy(texture, x / factor * ySide + (y / factor & -4), scaled, x * newYside + y, 4); // (y / factor & -4) == y / 4 / factor * 4
				for (int z = x + 1; z < x + factor; z++)
					Array.Copy(scaled, x * newYside, scaled, z * newYside, newYside);
			}
			return scaled;
		}

		public static uint[] Upscale(this uint[] texture, int factor, int width = 0)
		{
			if (factor == 1) return texture;
			int xSide = width == 0 ? (int)Math.Sqrt(texture.Length) : width,
				ySide = width == 0 ? xSide : texture.Length / width,
				newXside = xSide * factor,
				newYside = ySide * factor;
			uint[] scaled = new uint[texture.Length * factor * factor];
			for (int x = 0; x < newXside; x += factor)
			{
				for (int y = 0; y < newYside; y++)
					scaled[x * newYside + y] = texture[x / factor * ySide + y / factor];
				for (int z = x + 1; z < x + factor; z++)
					Array.Copy(scaled, x * newYside, scaled, z * newYside, newYside);
			}
			return scaled;
		}

		/// <param name="ints">rgba8888 color values (one uint per pixel)</param>
		/// <returns>rgba8888 texture (four bytes per pixel)</returns>
		public static byte[] Int2ByteArray(this uint[] ints)
		{
			byte[] bytes = new byte[ints.Length * 4];
			for (uint i = 0; i < ints.Length; i++)
			{
				bytes[i * 4] = (byte)(ints[i] >> 24);
				bytes[i * 4 + 1] = (byte)(ints[i] >> 16);
				bytes[i * 4 + 2] = (byte)(ints[i] >> 8);
				bytes[i * 4 + 3] = (byte)ints[i];
			}
			return bytes;
		}

		/// <param name="bytes">rgba8888 color values (four bytes per pixel)</param>
		/// <returns>rgba8888 texture (one int per pixel)</returns>
		public static uint[] Byte2IntArray(this byte[] bytes)
		{
			uint[] ints = new uint[bytes.Length / 4];
			for (uint i = 0; i < bytes.Length; i += 4)
				ints[i / 4] = (uint)(bytes[i] << 24) |
					(uint)(bytes[i + 1] << 16) |
					(uint)(bytes[i + 2] << 8) |
					bytes[i + 3];
			return ints;
		}

		public static T[] ConcatArrays<T>(params T[][] list)
		{
			T[] result = new T[list.Sum(a => a.Length)];
			int offset = 0;
			for (int x = 0; x < list.Length; x++)
			{
				list[x].CopyTo(result, offset);
				offset += list[x].Length;
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
				if (!uint.TryParse(streamReader.ReadLine()?.Trim(), out uint numColors)
				 || numColors != 256)
					throw new InvalidDataException("Palette stream does not contain exactly 256 colors.");
				result = new uint[numColors];
				for (uint x = 0; x < numColors; x++)
				{
					string[] tokens = streamReader.ReadLine()?.Trim().Split(' ');
					if (tokens == null || tokens.Length != 3
						|| !byte.TryParse(tokens[0], out byte r)
						|| !byte.TryParse(tokens[1], out byte g)
						|| !byte.TryParse(tokens[2], out byte b))
						throw new InvalidDataException("Palette stream is an incorrectly formatted JASC palette.");
					result[x] = (uint)(r << 24)
						+ (uint)(g << 16)
						+ (uint)(b << 8)
						+ (uint)(x == 255 ? 0 : 255);
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
