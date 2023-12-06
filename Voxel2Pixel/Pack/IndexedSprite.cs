using System.Buffers.Binary;
using static System.MemoryExtensions;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using Voxel2Pixel.Draw;
using System.IO;

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
		public IndexedSprite() { }
		public IndexedSprite(ushort width, ushort height) : this() => Pixels = new byte[width, height];
		public IndexedSprite(ISprite sprite) : this()
		{
			byte[] texture = sprite.Texture;
			Palette = texture.PaletteFromTexture();
			Pixels = texture.Byte2IndexArray(Palette).OneDToTwoD(sprite.Width);
		}
		public IndexedSprite(Stream stream) : this()
		{
			using (BinaryReader reader = new BinaryReader(
				input: stream,
				encoding: System.Text.Encoding.Default,
				leaveOpen: true))
			{
				Palette = reader.ReadBytes(1024).Byte2UIntArray();
				ushort width = reader.ReadUInt16(),
					height = reader.ReadUInt16();
				Pixels = reader.ReadBytes(width * height).OneDToTwoD(width);
			}
		}
		public void Write(Stream stream)
		{
			if (Palette.Length != 256)
				throw new IndexOutOfRangeException("Palette.Length expected to be 256. Was: " + Palette.Length);
			using (BinaryWriter writer = new BinaryWriter(
				output: stream,
				encoding: System.Text.Encoding.Default,
				leaveOpen: true))
			{
				writer.Write(Palette.UInt2ByteArray());
				writer.Write(Width);
				writer.Write(Height);
				writer.Write(Pixels.TwoDToOneD());
			}
		}
		public virtual byte[] GetTexture(bool transparent0 = true) => GetTexture(Pixels, Palette, transparent0);
		public virtual byte[] GetTexture(uint[] palette, bool transparent0 = true) => GetTexture(Pixels, palette ?? Palette, transparent0);
		public static byte[] GetTexture(byte[,] pixels, uint[] palette, bool transparent0 = true)
		{
			ushort width = (ushort)pixels.GetLength(0),
				height = (ushort)pixels.GetLength(1);
			byte[] texture = new byte[width * height << 2];
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
		#region Image manipulation
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
					Pixels = new byte[width, height]
						.DrawInsert(
							insert: sprite.Pixels,
							x: (ushort)(originX - sprite.OriginX),
							y: (ushort)(originY - sprite.OriginY)),
					OriginX = originX,
					OriginY = originY,
				};
		}
		/// <returns>upscaled copy</returns>
		public IndexedSprite Upscale(ushort factorX, ushort factorY = 1) => new IndexedSprite
		{
			Pixels = Pixels.Upscale(
				factorX: factorX,
				factorY: factorY),
			OriginX = (ushort)(OriginX * factorX),
			OriginY = (ushort)(OriginY * factorY),
		};
		#endregion Image manipulation
		#region Voxel drawing
		public static IEnumerable<IndexedSprite> Above4(IModel model, uint[] palette = null, params ushort[] voxelOrigin)
		{
			if (voxelOrigin is null || voxelOrigin.Length < 3)
				voxelOrigin = model.BottomCenter();
			TurnModel turnModel = new TurnModel
			{
				Model = model,
			};
			for (byte angle = 0; angle < 4; angle++)
			{
				turnModel.ReverseRotate(
					x: out ushort turnedX,
					y: out ushort turnedY,
					z: out ushort turnedZ,
					coordinates: voxelOrigin);
				VoxelDraw.AboveLocate(
					pixelX: out int locateX,
					pixelY: out int locateY,
					model: turnModel,
					voxelX: turnedX,
					voxelY: turnedY,
					voxelZ: turnedZ);
				IndexedSprite sprite = new IndexedSprite(VoxelDraw.AboveWidth(turnModel), VoxelDraw.AboveHeight(turnModel))
				{
					Palette = palette,
					OriginX = (ushort)locateX,
					OriginY = (ushort)locateY,
				};
				VoxelDraw.Above(
					model: turnModel,
					renderer: sprite);
				yield return sprite;
				turnModel.CounterZ();
			}
		}
		public static IEnumerable<IndexedSprite> Iso4(IModel model, uint[] palette = null, params ushort[] voxelOrigin)
		{
			if (voxelOrigin is null || voxelOrigin.Length < 3)
				voxelOrigin = model.BottomCenter();
			TurnModel turnModel = new TurnModel
			{
				Model = model,
			};
			for (byte angle = 0; angle < 4; angle++)
			{
				turnModel.ReverseRotate(
					x: out ushort turnedX,
					y: out ushort turnedY,
					z: out ushort turnedZ,
					coordinates: voxelOrigin);
				VoxelDraw.IsoLocate(
					pixelX: out int locateX,
					pixelY: out int locateY,
					model: turnModel,
					voxelX: turnedX,
					voxelY: turnedY,
					voxelZ: turnedZ);
				ushort width = VoxelDraw.IsoWidth(turnModel);
				IndexedSprite sprite = new IndexedSprite(VoxelDraw.IsoWidth(turnModel), VoxelDraw.IsoHeight(turnModel))
				{
					Palette = palette,
					OriginX = (ushort)locateX,
					OriginY = (ushort)locateY,
				};
				VoxelDraw.Iso(
					model: turnModel,
					renderer: sprite);
				yield return sprite;
				turnModel.CounterZ();
			}
		}
		/*
		public static IEnumerable<IndexedSprite> Iso8(IModel model, uint[] palette = null, params ushort[] voxelOrigin)
		{
			if (voxelOrigin is null || voxelOrigin.Length < 3)
				voxelOrigin = model.BottomCenter();
			TurnModel turnModel = new TurnModel
			{
				Model = model,
			};
			for (byte angle = 0; angle < 4; angle++)
			{
				turnModel.ReverseRotate(
					x: out ushort turnedX,
					y: out ushort turnedY,
					z: out ushort turnedZ,
					coordinates: voxelOrigin);
				VoxelDraw.AboveLocate(
					pixelX: out int locateX,
					pixelY: out int locateY,
					model: turnModel,
					voxelX: turnedX,
					voxelY: turnedY,
					voxelZ: turnedZ);
				IndexedSprite sprite = new IndexedSprite(VoxelDraw.AboveWidth(turnModel), VoxelDraw.AboveHeight(turnModel))
				{
					Palette = palette,
					OriginX = (ushort)locateX,
					OriginY = (ushort)locateY,
				};
				VoxelDraw.Above(
					model: turnModel,
					renderer: sprite);
				yield return sprite.Upscale(5, 4);
				turnModel.CounterZ();
				turnModel.ReverseRotate(
					x: out turnedX,
					y: out turnedY,
					z: out turnedZ,
					coordinates: voxelOrigin);
				VoxelDraw.IsoLocate(
					pixelX: out locateX,
					pixelY: out locateY,
					model: turnModel,
					voxelX: turnedX,
					voxelY: turnedY,
					voxelZ: turnedZ);
				sprite = new IndexedSprite2x(VoxelDraw.IsoWidth(turnModel), VoxelDraw.IsoHeight(turnModel))
				{
					Palette = palette,
					OriginX = (ushort)locateX,
					OriginY = (ushort)locateY,
				};
				VoxelDraw.Iso(
					model: turnModel,
					renderer: sprite);
				yield return sprite.TransparentCrop();
			}
		}
		*/
		#endregion Voxel drawing
	}
}
