using System.Buffers.Binary;
using static System.MemoryExtensions;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;
using System;
using System.Collections.Generic;

namespace Voxel2Pixel.Pack
{
	public class IndexedSprite : ISprite, IRectangleRenderer
	{
		#region ISprite
		public byte[] Texture => GetTexture();
		public ushort Width => (ushort)Pixels.GetLength(0);
		public ushort Height => (ushort)Pixels.GetLength(1);
		public ushort OriginX { get; set; }
		public ushort OriginY { get; set; }
		#endregion ISprite
		#region IndexedSprite
		public byte[,] Pixels { get; set; }
		public uint[] Palette { get; set; }
		public uint Color(byte index, VisibleFace visibleFace = VisibleFace.Front) => Palette[Index(index, visibleFace)];
		public static byte Index(byte voxel, VisibleFace visibleFace = VisibleFace.Front) => (byte)((byte)visibleFace + voxel);
		public byte[] GetTexture(bool transparent0 = true) => GetTexture(Pixels, Palette, transparent0);
		public byte[] GetTexture(uint[] palette, bool transparent0 = true) => GetTexture(Pixels, palette, transparent0);
		public static byte[] GetTexture(byte[,] pixels, uint[] palette, bool transparent0 = true)
		{
			ushort width = (ushort)pixels.GetLength(0),
				height = (ushort)pixels.GetLength(1);
			byte[] texture = new byte[(width * height) << 2];
			for (int y = height - 1, index = 0; y >= 0; y--)
				for (ushort x = 0; x < width; x++, index += 4)
					if (pixels[x, y] is byte pixel
						&& (!transparent0 || pixel != 0))
						BinaryPrimitives.WriteUInt32BigEndian(
							destination: texture.AsSpan(
								start: index,
								length: 4),
							value: palette[pixel]);
			return texture;
		}
		#endregion IndexedSprite
		#region IRectangleRenderer
		public void Rect(ushort x, ushort y, uint color, ushort sizeX = 1, ushort sizeY = 1)
		{
			if (Array.IndexOf(Palette, color) is int index && index >= 0)
				Rect(x, y, (byte)index, sizeX, sizeY);
			else
				throw new KeyNotFoundException(color.ToString("X"));
		}
		public void Rect(ushort x, ushort y, byte index, VisibleFace visibleFace = VisibleFace.Front, ushort sizeX = 1, ushort sizeY = 1) => Rect(x, y, Index(index, visibleFace), sizeX, sizeY);
		public void Rect(ushort x, ushort y, byte index, ushort sizeX = 1, ushort sizeY = 1)
		{
			sizeX = Math.Min((ushort)(x + sizeX), Width);
			sizeY = Math.Min((ushort)(y + sizeY), Height);
			for (; x < sizeX; x++)
				for (; y < sizeY; y++)
					Pixels[x, y] = index;
		}
		#endregion IRectangleRenderer
	}
}
