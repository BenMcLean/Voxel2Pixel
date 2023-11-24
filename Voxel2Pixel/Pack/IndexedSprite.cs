using System.Buffers.Binary;
using static System.MemoryExtensions;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;
using System;
using System.Collections.Generic;

namespace Voxel2Pixel.Pack
{
	/// <summary>
	/// If a voxel model is limited to only using color indices 1-63 inclusive or fewer then its pixel sprite renders can be stored as 256 color limited palette indexed sprites.
	/// </summary>
	public class IndexedSprite : ISprite, IRectangleRenderer, ITriangleRenderer, IVoxelColor
	{
		#region ISprite
		public byte[] Texture => GetTexture();
		public ushort Width => (ushort)Pixels.GetLength(0);
		public ushort Height => (ushort)Pixels.GetLength(1);
		public ushort OriginX { get; set; }
		public ushort OriginY { get; set; }
		#endregion ISprite
		#region IndexedSprite
		/// <summary>
		/// Each pixel is an index corresponding to a color in Palette.
		/// x+ is right, y+ is down.
		/// </summary>
		public byte[,] Pixels { get; set; }
		/// <summary>
		/// Expected to be length 256 of Big Endian RGBA8888 32-bit colors.
		/// 0 is the transparent color.
		/// 1-63 are for Front face colors.
		/// 65-127 are for Top face colors.
		/// 129-191 are for Left face colors.
		/// 193-255 are for Right face colors.
		/// 64, 128 and 192 are unused.
		/// </summary>
		public uint[] Palette { get; set; }
		public byte[] GetTexture(bool transparent0 = true) => GetTexture(Pixels, Palette, transparent0);
		public byte[] GetTexture(uint[] palette, bool transparent0 = true) => GetTexture(Pixels, palette ?? Palette, transparent0);
		public static byte[] GetTexture(byte[,] pixels, uint[] palette, bool transparent0 = true)
		{
			ushort width = (ushort)pixels.GetLength(0),
				height = (ushort)pixels.GetLength(1);
			byte[] texture = new byte[(width * height) << 2];
			int index = 0;
			for (ushort y = 0; y < height; y++)
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
		public void Rect(ushort x, ushort y, byte index, VisibleFace visibleFace, ushort sizeX = 1, ushort sizeY = 1) => Rect(x, y, Index(index, visibleFace), sizeX, sizeY);
		public void Rect(ushort x, ushort y, byte index, ushort sizeX = 1, ushort sizeY = 1)
		{
			sizeX = Math.Min((ushort)(x + sizeX), Width);
			sizeY = Math.Min((ushort)(y + sizeY), Height);
			for (; x < sizeX; x++)
				for (; y < sizeY; y++)
					Pixels[x, y] = index;
		}
		#endregion IRectangleRenderer
		#region ITriangleRenderer
		public void Tri(ushort x, ushort y, bool right, uint color)
		{
			if (Array.IndexOf(Palette, color) is int index && index >= 0)
				Tri(x, y, right, (byte)index);
			else
				throw new KeyNotFoundException(color.ToString("X"));
		}
		public void Tri(ushort x, ushort y, bool right, byte index, VisibleFace visibleFace) => Tri(x, y, right, Index(index, visibleFace));
		public void Tri(ushort x, ushort y, bool right, byte index)
		{
			if (right)
			{
				Rect(
					x: x,
					y: y,
					index: index,
					sizeX: 1,
					sizeY: 3);
				Rect(
					x: (ushort)(x + 1),
					y: (ushort)(y + 1),
					index: index);
			}
			else
			{
				Rect(
					x: (ushort)(x + 1),
					y: y,
					index: index,
					sizeX: 1,
					sizeY: 3);
				Rect(
					x: x,
					y: (ushort)(y + 1),
					index: index);
			}
		}
		#endregion ITriangleRenderer
		#region IVoxelColor
		public static byte Index(byte voxel, VisibleFace visibleFace = VisibleFace.Front) => (byte)(voxel < 64 ? (byte)visibleFace + voxel : voxel);
		public uint this[byte index, VisibleFace visibleFace = VisibleFace.Front] => Palette[Index(index, visibleFace)];
		#endregion IVoxelColor
	}
}
