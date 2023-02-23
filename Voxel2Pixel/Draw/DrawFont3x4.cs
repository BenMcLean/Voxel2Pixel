using System;
using System.Collections.ObjectModel;

namespace Voxel2Pixel.Draw
{
	public static class DrawFont3x4
	{
		/// <summary>
		/// From https://github.com/Michaelangel007/nanofont3x4
		/// This entire font glyph data is Copyleft 2015 since virtual "ownership" of bits (numbers) is retarded.
		///
		/// 3x4 Monochrome Font Glyphs within 4x4 cell
		/// Each nibble is one row, with the bits stored in reverse order: msb = left column, lsb = right column
		/// The bit layout for the 4x4 cell format is uppercase A is:
		///   abcd    Row0  _X__  0100
		///   efgh    Row1  XXX_  1110
		///   ijkl    Row2  X_X_  1010
		///   mnop    Row3  ____  0000
		/// </summary>
		public static readonly ReadOnlyCollection<ushort> Glyphs3x4 = Array.AsReadOnly(new ushort[]
		{           // Hex    [1] [2] [3] [4] Scanlines
			0x0000, // 0x20
			0x4404, // 0x21 ! 010 010 000 010
			0xAA00, // 0x22 " 101 101 000
			0xAEE0, // 0x23 # 101 111 111
			0x64C4, // 0x24 $ 011 010 110 010
			0xCE60, // 0x25 % 110 111 011
			0x4C60, // 0x26 & 010 110 011
			0x4000, // 0x27 ' 010 000 000
			0x4840, // 0x28 ( 010 100 010
			0x4240, // 0x29 ) 010 001 010
			0x6600, // 0x2A * 011 011 000
			0x4E40, // 0x2B + 010 111 010
			0x0088, // 0x2C , 000 000 100 100
			0x0E00, // 0x2D - 000 111 000
			0x0080, // 0x2E . 000 000 100
			0x2480, // 0x2F / 001 010 100

			0x4A40, // 0x30 0 010 101 010
			0x8440, // 0x31 1 100 010 010
			0xC460, // 0x32 2 110 010 011
			0xE6E0, // 0x33 3 111 011 111
			0xAE20, // 0x34 4 101 111 001
			0x64C0, // 0x35 5 011 010 110
			0x8EE0, // 0x36 6 100 111 111
			0xE220, // 0x37 7 111 001 001
			0x6EC0, // 0x38 8 011 111 110
			0xEE20, // 0x39 9 111 111 001
			0x4040, // 0x3A : 010 000 010
			0x0448, // 0x3B ; 000 010 010 100
			0x4840, // 0x3C < 010 100 010
			0xE0E0, // 0x3D = 111 000 111
			0x4240, // 0x3E > 010 001 010
			0x6240, // 0x3F ? 011 001 010

			0xCC20, // 0x40 @ 110 110 001 // 0 = 000_
			0x4EA0, // 0x41 A 010 111 101 // 2 = 001_
			0xCEE0, // 0x42 B 110 111 111 // 4 = 010_
			0x6860, // 0x43 C 011 100 011 // 6 = 011_
			0xCAC0, // 0x44 D 110 101 110 // 8 = 100_
			0xECE0, // 0x45 E 111 110 111 // A = 101_
			0xEC80, // 0x46 F 111 110 100 // C = 110_
			0xCAE0, // 0x47 G 110 101 111 // E = 111_
			0xAEA0, // 0x48 H 101 111 101
			0x4440, // 0x49 I 010 010 010
			0x22C0, // 0x4A J 001 001 110
			0xACA0, // 0x4B K 101 110 101
			0x88E0, // 0x4C L 100 100 111
			0xEEA0, // 0x4D M 111 111 101
			0xEAA0, // 0x4E N 111 101 101
			0xEAE0, // 0x4F O 111 101 111

			0xEE80, // 0x50 P 111 111 100
			0xEAC0, // 0x51 Q 111 101 110
			0xCEA0, // 0x52 R 110 111 101
			0x64C0, // 0x53 S 011 010 110
			0xE440, // 0x54 T 111 010 010
			0xAAE0, // 0x55 U 101 101 111
			0xAA40, // 0x56 V 101 101 010
			0xAEE0, // 0x57 W 101 111 111
			0xA4A0, // 0x58 X 101 010 101
			0xA440, // 0x59 Y 101 010 010
			0xE4E0, // 0x5A Z 111 010 111
			0xC8C0, // 0x5B [ 110 100 110
			0x8420, // 0x5C \ 100 010 001
			0x6260, // 0x5D ] 011 001 011
			0x4A00, // 0x5E ^ 010 101 000
			0x00E0, // 0x5F _ 000 000 111

			0x8400, // 0x60 ` 100 010 000
			0x04C0, // 0x61 a 000 010 110
			0x8CC0, // 0x62 b 100 110 110
			0x0CC0, // 0x63 c 000 110 110
			0x4CC0, // 0x64 d 010 110 110
			0x08C0, // 0x65 e 000 100 110
			0x4880, // 0x66 f 010 100 100
			0x0CCC, // 0x67 g 000 110 110 110
			0x8CC0, // 0x68 h 100 110 110
			0x0880, // 0x69 i 000 100 100
			0x0448, // 0x6A j 000 010 010 100
			0x8CA0, // 0x6B k 100 110 101
			0x8840, // 0x6C l 100 100 010
			0x0CE0, // 0x6D m 000 110 111
			0x0CA0, // 0x6E n 000 110 101
			0x0CC0, // 0x6F o 000 110 110

			0x0CC8, // 0x70 p 000 110 110 100
			0x0CC4, // 0x71 q 000 110 110 010
			0x0C80, // 0x72 r 000 110 100
			0x0480, // 0x73 s 000 010 100
			0x4C60, // 0x74 t 010 110 011
			0x0AE0, // 0x75 u 000 101 111
			0x0A40, // 0x76 v 000 101 010
			0x0E60, // 0x77 w 000 111 011
			0x0CC0, // 0x78 x 000 110 110
			0x0AE2, // 0x79 y 000 101 111 001
			0x0840, // 0x7A z 000 100 010
			0x6C60, // 0x7B { 011 110 011
			0x4444, // 0x7C | 010 010 010 010
			0xC6C0, // 0x7D } 110 011 110
			0x6C00, // 0x7E ~ 011 110 000
			0xA4A4  // 0x7F   101 010 101 010 // Alternative: Could even have a "full" 4x4 checkerboard
		});
		public static byte[] Draw3x4(this byte[] texture, char @char, int width = 0, int x = 0, int y = 0, uint color = 0xFFFFFFFF) => Draw3x4(texture, @char, width, x, y, (byte)(color >> 24), (byte)(color >> 16), (byte)(color >> 8), (byte)color);
		public static byte[] Draw3x4(this byte[] texture, char @char, int width = 0, int x = 0, int y = 0, params byte[] rgba)
		{
			if (width < 1)
				width = (int)Math.Sqrt(texture.Length >> 2);
			if (@char < 32
				|| @char > 127
				|| x < 0
				|| y < 0
				|| x + 3 > width
				|| y > (texture.Length / width >> 2))
				return texture;
			ushort glyph = Glyphs3x4[@char - 32];
			int xSide = width << 2,
				start = y * xSide + (x << 2);
			if (start + 11 > texture.Length)
				return texture;
			if ((glyph & 0b1000000000000000) != 0)
				Array.Copy(
					sourceArray: rgba,
					sourceIndex: 0,
					destinationArray: texture,
					destinationIndex: start,
					length: 4);
			if ((glyph & 0b0100000000000000) != 0)
				Array.Copy(
					sourceArray: rgba,
					sourceIndex: 0,
					destinationArray: texture,
					destinationIndex: start + 4,
					length: 4);
			if ((glyph & 0b0010000000000000) != 0)
				Array.Copy(
					sourceArray: rgba,
					sourceIndex: 0,
					destinationArray: texture,
					destinationIndex: start + 8,
					length: 4);
			//if ((glyph & 0b0001000000000000) != 0)
			//	Array.Copy(
			//		sourceArray: rgba,
			//		sourceIndex: 0,
			//		destinationArray: texture,
			//		destinationIndex: start + 12,
			//		length: 4);
			start += xSide;
			if (start + 11 > texture.Length)
				return texture;
			if ((glyph & 0b0000100000000000) != 0)
				Array.Copy(
					sourceArray: rgba,
					sourceIndex: 0,
					destinationArray: texture,
					destinationIndex: start,
					length: 4);
			if ((glyph & 0b0000010000000000) != 0)
				Array.Copy(
					sourceArray: rgba,
					sourceIndex: 0,
					destinationArray: texture,
					destinationIndex: start + 4,
					length: 4);
			if ((glyph & 0b0000001000000000) != 0)
				Array.Copy(
					sourceArray: rgba,
					sourceIndex: 0,
					destinationArray: texture,
					destinationIndex: start + 8,
					length: 4);
			//if ((glyph & 0b0000000100000000) != 0)
			//	Array.Copy(
			//		sourceArray: rgba,
			//		sourceIndex: 0,
			//		destinationArray: texture,
			//		destinationIndex: start + 12,
			//		length: 4);
			start += xSide;
			if (start + 11 > texture.Length)
				return texture;
			if ((glyph & 0b0000000010000000) != 0)
				Array.Copy(
					sourceArray: rgba,
					sourceIndex: 0,
					destinationArray: texture,
					destinationIndex: start,
					length: 4);
			if ((glyph & 0b0000000001000000) != 0)
				Array.Copy(
					sourceArray: rgba,
					sourceIndex: 0,
					destinationArray: texture,
					destinationIndex: start + 4,
					length: 4);
			if ((glyph & 0b0000000000100000) != 0)
				Array.Copy(
					sourceArray: rgba,
					sourceIndex: 0,
					destinationArray: texture,
					destinationIndex: start + 8,
					length: 4);
			//if ((glyph & 0b0000000000010000) != 0)
			//	Array.Copy(
			//		sourceArray: rgba,
			//		sourceIndex: 0,
			//		destinationArray: texture,
			//		destinationIndex: start + 12,
			//		length: 4);
			start += xSide;
			if (start + 11 > texture.Length)
				return texture;
			if ((glyph & 0b0000000000001000) != 0)
				Array.Copy(
					sourceArray: rgba,
					sourceIndex: 0,
					destinationArray: texture,
					destinationIndex: start,
					length: 4);
			if ((glyph & 0b0000000000000100) != 0)
				Array.Copy(
					sourceArray: rgba,
					sourceIndex: 0,
					destinationArray: texture,
					destinationIndex: start + 4,
					length: 4);
			if ((glyph & 0b0000000000000010) != 0)
				Array.Copy(
					sourceArray: rgba,
					sourceIndex: 0,
					destinationArray: texture,
					destinationIndex: start + 8,
					length: 4);
			//if ((glyph & 0b0000000000000001) != 0)
			//	Array.Copy(
			//		sourceArray: rgba,
			//		sourceIndex: 0,
			//		destinationArray: texture,
			//		destinationIndex: start + 12,
			//		length: 4);
			return texture;
		}
		public static byte[] Draw3x4(this byte[] texture, string @string, int width = 0, int x = 0, int y = 0, uint color = 0xFFFFFFFF) => Draw3x4(texture, @string, width, x, y, (byte)(color >> 24), (byte)(color >> 16), (byte)(color >> 8), (byte)color);
		public static byte[] Draw3x4(this byte[] texture, string @string, int width = 0, int x = 0, int y = 0, params byte[] rgba)
		{
			foreach (char @char in @string)
			{
				texture.Draw3x4(
					@char: @char,
					width: width,
					x: x,
					y: y,
					rgba: rgba);
				x += 4;
			}
			return texture;
		}
	}
}
