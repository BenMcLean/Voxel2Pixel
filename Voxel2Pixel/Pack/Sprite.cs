using RectpackSharp;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;
using static Voxel2Pixel.Pack.TextureAtlas;

namespace Voxel2Pixel.Pack
{
	public class Sprite : Dictionary<string, Point>, ISprite, ITriangleRenderer, IVoxelColor
	{
		#region ISprite
		public byte[] Texture { get; set; }
		public ushort Width { get; set; }
		public ushort Height => (ushort)((Texture.Length >> 2) / Width);
		#endregion ISprite
		#region Sprite
		public const string Origin = "Origin";
		public const uint DefaultShadowColor = 0x88u;
		public Sprite() { }
		public Sprite(ushort width, ushort height) : this()
		{
			Texture = new byte[width * height << 2];
			Width = width;
		}
		public Sprite(Point size) : this((ushort)size.X, (ushort)size.Y) { }
		public Sprite(ISprite sprite) : this()
		{
			Texture = sprite.Texture;
			Width = sprite.Width;
			if (sprite is Sprite s)
				VoxelColor = s.VoxelColor;
			SetRange(sprite);
		}
		public Sprite SetRange(params KeyValuePair<string, Point>[] points) => SetRange(points.AsEnumerable());
		public Sprite SetRange(IEnumerable<KeyValuePair<string, Point>> points)
		{
			foreach (KeyValuePair<string, Point> point in points)
				this[point.Key] = point.Value;
			return this;
		}
		#endregion Sprite
		#region IVoxelColor
		public IVoxelColor VoxelColor { get; set; }
		public uint this[byte index, VisibleFace visibleFace = VisibleFace.Front] => VoxelColor[index, visibleFace];
		#endregion IVoxelColor
		#region IRectangleRenderer
		public virtual void Rect(ushort x, ushort y, uint color, ushort sizeX = 1, ushort sizeY = 1) =>
			Texture.DrawRectangle(
				x: x,
				y: y,
				color: color,
				rectWidth: sizeX,
				rectHeight: sizeY,
				width: Width);
		public virtual void Rect(ushort x, ushort y, byte index, VisibleFace visibleFace = VisibleFace.Front, ushort sizeX = 1, ushort sizeY = 1) => Rect(
			x: x,
			y: y,
			color: this[index, visibleFace],
			sizeX: sizeX,
			sizeY: sizeY);
		#endregion IRectangleRenderer
		#region ITriangleRenderer
		public virtual void Tri(ushort x, ushort y, bool right, uint color)
		{
			if (right)
			{
				Rect(
					x: x,
					y: y,
					color: color);
				Rect(
					x: x,
					y: (ushort)(y + 1),
					color: color,
					sizeX: 2);
				Rect(
					x: x,
					y: (ushort)(y + 2),
					color: color);
			}
			else
			{
				Rect(
					x: (ushort)(x + 1),
					y: y,
					color: color);
				Rect(
					x: x,
					y: (ushort)(y + 1),
					color: color,
					sizeX: 2);
				Rect(
					x: (ushort)(x + 1),
					y: (ushort)(y + 2),
					color: color);
			}
		}
		public virtual void Tri(ushort x, ushort y, bool right, byte index, VisibleFace visibleFace = VisibleFace.Front) => Tri(
			x: x,
			y: y,
			right: right,
			color: this[index, visibleFace]);
		public virtual void Diamond(ushort x, ushort y, uint color)
		{
			Rect(
				x: (ushort)(x + 1),
				y: y,
				color: color,
				sizeX: 2);
			Rect(
				x: x,
				y: (ushort)(y + 1),
				color: color,
				sizeX: 4);
			Rect(
				x: (ushort)(x + 1),
				y: (ushort)(y + 2),
				color: color,
				sizeX: 2);
		}
		public virtual void Diamond(ushort x, ushort y, byte index, VisibleFace visibleFace = VisibleFace.Front) => Diamond(x: x, y: y, color: VoxelColor[index, visibleFace]);
		#endregion ITriangleRenderer
		#region Packing
		public Sprite(out PackingRectangle[] packingRectangles, IEnumerable<Sprite> sprites) : this(packingRectangles: out packingRectangles, sprites: sprites.ToArray()) { }
		public Sprite(out PackingRectangle[] packingRectangles, params Sprite[] sprites)
		{
			packingRectangles = Enumerable.Range(0, sprites.Length)
				.Select(i => new PackingRectangle(
					x: 0,
					y: 0,
					width: (ushort)(sprites[i].Width + 2),
					height: (ushort)(sprites[i].Height + 2),
					id: i))
				.ToArray();
			RectanglePacker.Pack(
				rectangles: packingRectangles,
				bounds: out PackingRectangle bounds,
				packingHint: PackingHints.TryByBiggerSide);
			Width = (ushort)bounds.Width;
			Texture = new byte[bounds.Width * bounds.Height << 2];
			foreach (PackingRectangle packingRectangle in packingRectangles)
				Texture.DrawInsert(
						x: (ushort)(packingRectangle.X + 1),
						y: (ushort)(packingRectangle.Y + 1),
						insert: sprites[packingRectangle.Id].Texture,
						insertWidth: sprites[packingRectangle.Id].Width,
						width: Width)
					.DrawPadding(
						x: (ushort)(packingRectangle.X + 1),
						y: (ushort)(packingRectangle.Y + 1),
						areaWidth: sprites[packingRectangle.Id].Width,
						areaHeight: sprites[packingRectangle.Id].Height,
						width: Width);
		}
		public Sprite(Dictionary<string, Sprite> dictionary, out TextureAtlas textureAtlas) : this(sprites: [.. dictionary], textureAtlas: out textureAtlas) { }
		public Sprite(KeyValuePair<string, Sprite>[] sprites, out TextureAtlas textureAtlas) : this(packingRectangles: out RectpackSharp.PackingRectangle[] packingRectangles, sprites: sprites.Select(pair => pair.Value)) =>
			textureAtlas = new TextureAtlas
			{
				SubTextures = Enumerable.Range(0, packingRectangles.Length)
					.Select(i => new SubTexture
					{
						Name = sprites[i].Key,
						X = (int)packingRectangles[i].X + 1,
						Y = (int)packingRectangles[i].Y + 1,
						Width = (int)packingRectangles[i].Width - 2,
						Height = (int)packingRectangles[i].Height - 2,
						Points = sprites[i].Value
							.Select(point => new SubTexture.Point
							{
								Name = point.Key,
								X = point.Value.X,
								Y = point.Value.Y,
							}).ToArray(),
					}).ToArray(),
			};
		#endregion Packing
		#region Image manipulation
		/// <returns>processed copy</returns>
		public Sprite Process(
			ushort scaleX = 1,
			ushort scaleY = 1,
			bool outline = false,
			uint outlineColor = PixelDraw.DefaultOutlineColor,
			byte threshold = PixelDraw.DefaultTransparencyThreshold)
		{
			if (outline)
				return (scaleX > 1 || scaleY > 1 ? Upscale(scaleX, scaleY) : this)
					.CropOutline(outlineColor, threshold);
			Sprite sprite = Crop2Content(threshold);
			return scaleX > 1 || scaleY > 1 ?
				sprite.Upscale(scaleX, scaleY)
				: sprite;
		}
		public static IEnumerable<Sprite> SameSize(ushort addWidth = 0, ushort addHeight = 0, params Sprite[] sprites) => sprites.AsEnumerable().SameSize(addWidth, addHeight);
		public static IEnumerable<Sprite> SameSize(params Sprite[] sprites) => sprites.AsEnumerable().SameSize();
		/// <returns>resized copy</returns>
		public Sprite Resize(ushort newWidth, ushort newHeight) => new Sprite
		{
			Texture = Texture.Resize(newWidth, newHeight, Width),
			Width = newWidth,
			VoxelColor = VoxelColor,
		}.SetRange(this);
		/// <summary>
		/// Some game engines and graphics hardware require textures to be square and sized by a power of 2. LibGDX gave me some trouble on Android for not doing this back in the 2010s.
		/// </summary>
		/// <returns>enlarged copy</returns>
		public Sprite EnlargeToPowerOf2()
		{
			ushort newSize = (ushort)PixelDraw.NextPowerOf2(Math.Max(Width, Height));
			return Resize(newSize, newSize);
		}
		/// <returns>lower right cropped copy</returns>
		public Sprite Crop(ushort x, ushort y) => Crop(x, y, (ushort)(Width - x), (ushort)(Height - y));
		/// <returns>cropped copy</returns>
		public Sprite Crop(int x, int y, ushort croppedWidth, ushort croppedHeight) => new Sprite
		{
			Texture = Texture.Crop(x, y, croppedWidth, croppedHeight, Width),
			Width = croppedWidth,
			VoxelColor = VoxelColor,
		}.SetRange(this.Select(point => new KeyValuePair<string, Point>(
			key: point.Key,
			value: new Point(
				X: point.Value.X - x,
				Y: point.Value.Y - y))));
		/// <returns>cropped copy</returns>
		public Sprite Crop2Content(byte threshold = PixelDraw.DefaultTransparencyThreshold) => new Sprite
		{
			Texture = Texture.Crop2Content(
				cutLeft: out ushort cutLeft,
				cutTop: out ushort cutTop,
				croppedWidth: out ushort croppedWidth,
				croppedHeight: out _,
				width: Width,
				threshold: threshold),
			Width = croppedWidth,
			VoxelColor = VoxelColor,
		}.SetRange(this.Select(point => new KeyValuePair<string, Point>(
			key: point.Key,
			value: new Point(
				X: point.Value.X - cutLeft,
				Y: point.Value.Y - cutTop))));
		/// <returns>cropped copy</returns>
		public Sprite Crop2ContentPlus1(byte threshold = PixelDraw.DefaultTransparencyThreshold) => new Sprite
		{
			Texture = Texture.Crop2ContentPlus1(
				cutLeft: out int cutLeft,
				cutTop: out int cutTop,
				croppedWidth: out ushort croppedWidth,
				croppedHeight: out _,
				width: Width,
				threshold: threshold),
			Width = croppedWidth,
			VoxelColor = VoxelColor,
		}.SetRange(this.Select(point => new KeyValuePair<string, Point>(
			key: point.Key,
			value: new Point(
				X: point.Value.X - cutLeft,
				Y: point.Value.Y - cutTop))));
		public Sprite Outline(uint color = PixelDraw.DefaultOutlineColor, byte threshold = PixelDraw.DefaultTransparencyThreshold) => new Sprite
		{
			Texture = Texture.Outline(
				width: Width,
				color: color,
				threshold: threshold),
			Width = Width,
			VoxelColor = VoxelColor,
		}.SetRange(this);
		/// <returns>cropped and outlined copy</returns>
		public Sprite CropOutline(uint color = PixelDraw.DefaultOutlineColor, byte threshold = PixelDraw.DefaultTransparencyThreshold) => new Sprite
		{
			Texture = Texture
				.Crop2ContentPlus1(
					cutLeft: out int cutLeft,
					cutTop: out int cutTop,
					croppedWidth: out ushort croppedWidth,
					croppedHeight: out _,
					width: Width,
					threshold: threshold)
				.Outline(
					width: croppedWidth,
					color: color,
					threshold: threshold),
			Width = croppedWidth,
			VoxelColor = VoxelColor,
		}.SetRange(this.Select(point => new KeyValuePair<string, Point>(
			key: point.Key,
			value: new Point(
				X: point.Value.X - cutLeft,
				Y: point.Value.Y - cutTop))));
		/// <returns>upscaled copy</returns>
		public Sprite Upscale(ushort factorX, ushort factorY = 1) => new Sprite
		{
			Texture = Texture.Upscale(
				factorX: factorX,
				factorY: factorY,
				newWidth: out ushort newWidth,
				width: Width),
			Width = newWidth,
			VoxelColor = VoxelColor,
		}.SetRange(this.Select(point => new KeyValuePair<string, Point>(
			key: point.Key,
			value: new Point(
				X: point.Value.X * factorX,
				Y: point.Value.Y * factorY))));
		public Sprite DrawInsert(int x, int y, Sprite insert) => DrawInsert(x, y, insert.Texture, insert.Width);
		public Sprite DrawInsert(int x, int y, byte[] insert, ushort insertWidth = 0)
		{
			Texture = Texture.DrawInsert(x, y, insert, insertWidth, Width);
			return this;
		}
		public Sprite DrawTransparentInsert(int x, int y, Sprite insert, byte threshold = PixelDraw.DefaultTransparencyThreshold) => DrawTransparentInsert(x, y, insert.Texture, insert.Width, threshold);
		public Sprite DrawTransparentInsert(int x, int y, byte[] insert, ushort insertWidth = 0, byte threshold = PixelDraw.DefaultTransparencyThreshold)
		{
			Texture = Texture.DrawTransparentInsert(x, y, insert, insertWidth, Width, threshold);
			return this;
		}
		public static IEnumerable<Sprite> AddFrameNumbers(uint color = 0xFFFFFFFFu, params Sprite[] sprites) => sprites.AsEnumerable().AddFrameNumbers(color);
		public Sprite Draw3x4(string @string, int x = 0, int y = 0, uint color = 0xFFFFFFFF)
		{
			Texture.Draw3x4(
				@string: @string,
				width: Width,
				x: x,
				y: y,
				color: color);
			return this;
		}
		public Sprite Draw3x4Bottom(string @string, uint color = 0xFFFFFFFFu) => Draw3x4(
			@string: @string,
			x: 0,
			y: Height - 4,
			color: color);
		public Sprite DrawPoint(string name = Origin, uint color = 0xFF00FFFFu)
		{
			Point point = this[name];
			Rect((ushort)point.X, (ushort)point.Y, color);
			return this;
		}
		public uint Pixel(ushort x, ushort y) => BinaryPrimitives.ReadUInt32BigEndian(Texture.AsSpan(
			start: y * (Width << 2) + (x << 2),
			length: 4));
		/// <summary>
		/// Based on https://iiif.io/api/annex/notes/rotation/
		/// </summary>
		public Point RotatedSize(double radians)
		{
			PixelDraw.RotatedSize(
				width: Width,
				height: Height,
				rotatedWidth: out ushort rotatedWidth,
				rotatedHeight: out ushort rotatedHeight,
				radians: radians);
			return new Point(rotatedWidth, rotatedHeight);
		}
		public Sprite Rotate(double radians)
		{
			Sprite sprite = new()
			{
				Texture = Texture.Rotate(
					rotatedWidth: out ushort rotatedWidth,
					rotatedHeight: out ushort rotatedHeight,
					radians: radians,
					width: Width),
				Width = rotatedWidth,
				VoxelColor = VoxelColor,
			};
			double cos = Math.Cos(radians),
				sin = Math.Sin(radians),
				offsetX = (Width >> 1) - cos * (rotatedWidth >> 1) - sin * (rotatedHeight >> 1),
				offsetY = (Height >> 1) - cos * (rotatedHeight >> 1) + sin * (rotatedWidth >> 1);
			return sprite.SetRange(this.Select(pair => new KeyValuePair<string, Point>(
				key: pair.Key,
				value: new Point(
					X: (int)(cos * (pair.Value.X - offsetX) - sin * (pair.Value.Y - offsetY)),
					Y: (int)(sin * (pair.Value.X - offsetX) + cos * (pair.Value.Y - offsetY))))));
		}
		/// <summary>
		/// Based on https://stackoverflow.com/a/6207833
		/// </summary>
		public void Rotate(double radians, IRectangleRenderer renderer)
		{
			Point size = RotatedSize(radians);
			ushort height = Height;
			double cos = Math.Cos(radians),
				sin = Math.Sin(radians),
				offsetX = (Width >> 1) - cos * (size.X >> 1) - sin * (size.Y >> 1),
				offsetY = (height >> 1) - cos * (size.Y >> 1) + sin * (size.X >> 1);
			for (ushort y = 0; y < size.Y; y++)
				for (ushort x = 0; x < size.X; x++)
					if ((int)(x * cos + y * sin + offsetX) is int oldX
						&& oldX >= 0 && oldX < Width
						&& (int)(y * cos - x * sin + offsetY) is int oldY
						&& oldY >= 0 && oldY < height)
						renderer.Rect(
							x: x,
							y: y,
							color: Pixel(
								x: (ushort)oldX,
								y: (ushort)oldY));
		}
		#endregion Image manipulation
	}
}
