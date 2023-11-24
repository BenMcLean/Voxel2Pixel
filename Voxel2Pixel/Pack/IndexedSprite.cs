using System.Buffers.Binary;
using static System.MemoryExtensions;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using Voxel2Pixel.Draw;

namespace Voxel2Pixel.Pack
{
	/// <summary>
	/// If a voxel model is limited to only using color indices 1-63 inclusive or fewer then its pixel sprite renders can be stored as 256 color limited palette indexed sprites.
	/// </summary>
	public class IndexedSprite : ISprite, IRectangleRenderer, ITriangleRenderer, IVoxelColor
	{
		#region ISprite
		public virtual byte[] Texture => GetTexture();
		public virtual ushort Width => (ushort)Pixels.GetLength(0);
		public virtual ushort Height => (ushort)Pixels.GetLength(1);
		public virtual ushort OriginX { get; set; }
		public virtual ushort OriginY { get; set; }
		#endregion ISprite
		#region IndexedSprite
		/// <summary>
		/// Each pixel is an index corresponding to a color in Palette.
		/// x+ is right, y+ is down.
		/// </summary>
		public virtual byte[,] Pixels { get; set; }
		/// <summary>
		/// Expected to be length 256 of Big Endian RGBA8888 32-bit colors.
		/// 0 is the transparent color.
		/// 1-63 are for Front face colors.
		/// 65-127 are for Top face colors.
		/// 129-191 are for Left face colors.
		/// 193-255 are for Right face colors.
		/// 64, 128 and 192 are unused.
		/// </summary>
		public virtual uint[] Palette { get; set; }
		public virtual byte[] GetTexture(bool transparent0 = true) => GetTexture(Pixels, Palette, transparent0);
		public virtual byte[] GetTexture(uint[] palette, bool transparent0 = true) => GetTexture(Pixels, palette ?? Palette, transparent0);
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
		public virtual void Rect(ushort x, ushort y, uint color, ushort sizeX = 1, ushort sizeY = 1)
		{
			if (Array.IndexOf(Palette, color) is int index && index >= 0)
				Rect(x, y, (byte)index, sizeX, sizeY);
			else
				throw new KeyNotFoundException(color.ToString("X"));
		}
		public virtual void Rect(ushort x, ushort y, byte index, VisibleFace visibleFace, ushort sizeX = 1, ushort sizeY = 1) => Rect(x, y, Index(index, visibleFace), sizeX, sizeY);
		public virtual void Rect(ushort x, ushort y, byte index, ushort sizeX = 1, ushort sizeY = 1)
		{
			sizeX = Math.Min((ushort)(x + sizeX), Width);
			sizeY = Math.Min((ushort)(y + sizeY), Height);
			for (; x < sizeX; x++)
				for (; y < sizeY; y++)
					Pixels[x, y] = index;
		}
		#endregion IRectangleRenderer
		#region ITriangleRenderer
		public virtual void Tri(ushort x, ushort y, bool right, uint color)
		{
			if (Array.IndexOf(Palette, color) is int index && index >= 0)
				Tri(x, y, right, (byte)index);
			else
				throw new KeyNotFoundException(color.ToString("X"));
		}
		public virtual void Tri(ushort x, ushort y, bool right, byte index, VisibleFace visibleFace) => Tri(x, y, right, Index(index, visibleFace));
		public virtual void Tri(ushort x, ushort y, bool right, byte index)
		{
			Rect(
				x: (ushort)(x + (right ? 0 : 1)),
				y: y,
				index: index,
				sizeX: 1,
				sizeY: 3);
			Rect(
				x: (ushort)(x + (right ? 1 : 0)),
				y: (ushort)(y + 1),
				index: index);
		}
		#endregion ITriangleRenderer
		#region IVoxelColor
		public static byte Index(byte voxel, VisibleFace visibleFace = VisibleFace.Front) => (byte)(voxel < 64 ? (byte)visibleFace + voxel : voxel);
		public virtual uint this[byte index, VisibleFace visibleFace = VisibleFace.Front] => Palette[Index(index, visibleFace)];
		#endregion IVoxelColor
		public static byte[,] DrawInsert(byte[,] bytes, byte[,] insert, ushort x = 0, ushort y = 0, bool skip0 = true)
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
		public static IEnumerable<IndexedSprite> SameSize(ushort addWidth, ushort addHeight, IEnumerable<IndexedSprite> sprites) => SameSize(addWidth, addHeight, sprites.ToArray());
		public static IEnumerable<IndexedSprite> SameSize(IEnumerable<IndexedSprite> sprites) => SameSize(sprites.ToArray());
		public static IEnumerable<IndexedSprite> SameSize(params IndexedSprite[] sprites) => SameSize(0, 0, sprites);
		public static IEnumerable<IndexedSprite> SameSize(ushort addWidth, ushort addHeight, params IndexedSprite[] sprites)
		{
			ushort originX = sprites.Select(sprite => sprite.OriginX).Max(),
				originY = sprites.Select(sprite => sprite.OriginY).Max(),
				width = (ushort)(sprites.Select(sprite => sprite.Width + originX - sprite.OriginX).Max() + addWidth),
				height = (ushort)(sprites.Select(sprite => sprite.Height + originY - sprite.OriginY).Max() + addHeight);
			foreach (IndexedSprite sprite in sprites)
				yield return new IndexedSprite
				{
					Pixels = DrawInsert(
						bytes: new byte[width, height],
						insert: sprite.Pixels,
						x: (ushort)(originX - sprite.OriginX),
						y: (ushort)(originY - sprite.OriginY)),
					OriginX = originX,
					OriginY = originY,
				};
		}
	}
}
