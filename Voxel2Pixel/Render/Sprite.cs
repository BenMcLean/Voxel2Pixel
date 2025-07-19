using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenVoxel;
using RectpackSharp;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;
using static Voxel2Pixel.Render.TextureAtlas;

namespace Voxel2Pixel.Render;

public class Sprite : IDictionary<string, Point>, ISprite, IColoredRenderer
{
	#region ISprite
	public byte[] Texture { get; set; }
	public ushort Width { get; set; }
	public ushort Height => (ushort)((Texture.Length >> 2) / Width);
	#endregion ISprite
	#region Sprite
	public const string Origin = "";
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
	#region IDictionary
	private readonly SanitizedKeyDictionary<Point> _points = [];
	public Point this[string key]
	{
		get => _points[key];
		set => _points[key] = value;
	}
	public ICollection<string> Keys => _points.Keys;
	public ICollection<Point> Values => _points.Values;
	public int Count => _points.Count;
	public bool IsReadOnly => _points.IsReadOnly;
	public void Add(string key, Point value) => _points.Add(key, value);
	public void Add(KeyValuePair<string, Point> item) => _points.Add(item);
	public void Clear() => _points.Clear();
	public bool Contains(KeyValuePair<string, Point> item) => _points.Contains(item);
	public bool ContainsKey(string key) => _points.ContainsKey(key);
	public void CopyTo(KeyValuePair<string, Point>[] array, int arrayIndex) => _points.CopyTo(array, arrayIndex);
	public IEnumerator<KeyValuePair<string, Point>> GetEnumerator() => _points.GetEnumerator();
	public bool Remove(string key) => _points.Remove(key);
	public bool Remove(KeyValuePair<string, Point> item) => _points.Remove(item);
	public bool TryGetValue(string key, out Point value) => _points.TryGetValue(key, out value);
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	#endregion IDictionary
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
	public Sprite(out PackingRectangle[] packingRectangles, IEnumerable<Sprite> sprites) : this(packingRectangles: out packingRectangles, sprites: [.. sprites]) { }
	public Sprite(out PackingRectangle[] packingRectangles, params Sprite[] sprites)
	{
		packingRectangles = [.. sprites
			.Select((sprite, index) => new PackingRectangle(
				x: 0,
				y: 0,
				width: (ushort)(sprite.Width + 2),
				height: (ushort)(sprite.Height + 2),
				id: index))];
		RectanglePacker.Pack(
			rectangles: packingRectangles,
			bounds: out PackingRectangle bounds,
			packingHint: PackingHints.TryByBiggerSide);
		Width = (ushort)bounds.Width;
		Texture = new byte[bounds.Width * bounds.Height << 2];
		Parallel.Invoke([.. packingRectangles
			.Select<PackingRectangle, Action>(packingRectangle => () => Texture
				.DrawInsert(
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
					width: Width))
			]);
	}
	public Sprite(IDictionary<string, Sprite> dictionary, out TextureAtlas textureAtlas) : this(sprites: [.. dictionary], textureAtlas: out textureAtlas) { }
	public Sprite(KeyValuePair<string, Sprite>[] sprites, out TextureAtlas textureAtlas) : this(packingRectangles: out PackingRectangle[] packingRectangles, sprites: sprites.Select(pair => pair.Value)) =>
		textureAtlas = new TextureAtlas
		{
			SubTextures = [.. packingRectangles.Zip(sprites, (packingRectangle, spritePair) => new KeyValuePair<string, SubTexture>(spritePair.Key, new()
			{
				X = (ushort)(packingRectangle.X + 1),
				Y = (ushort)(packingRectangle.Y + 1),
				Width = (ushort)(packingRectangle.Width - 2),
				Height = (ushort)(packingRectangle.Height - 2),
				Points = [.. spritePair.Value
					.Select(pointPair => new KeyValuePair<string, SubTexture.Point>(pointPair.Key, new()
					{
						X = pointPair.Value.X,
						Y = pointPair.Value.Y,
					}))],
			}))],
		};
	#endregion Packing
	#region Image manipulation
	/// <returns>processed copy</returns>
	public Sprite Process(
		byte scaleX = 1,
		byte scaleY = 1,
		bool outline = false,
		uint outlineColor = PixelDraw.Black,
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
	public Sprite Outline(uint color = PixelDraw.Black, byte threshold = PixelDraw.DefaultTransparencyThreshold) => new Sprite
	{
		Texture = Texture.Outline(
			width: Width,
			color: color,
			threshold: threshold),
		Width = Width,
		VoxelColor = VoxelColor,
	}.SetRange(this);
	/// <returns>cropped and outlined copy</returns>
	public Sprite CropOutline(uint color = PixelDraw.Black, byte threshold = PixelDraw.DefaultTransparencyThreshold) => new Sprite
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
	public Sprite Upscale(byte scaleX = 1, byte scaleY = 1) => new Sprite
	{
		Texture = Texture.Upscale(
			scaleX: scaleX,
			scaleY: scaleY,
			newWidth: out ushort newWidth,
			width: Width),
		Width = newWidth,
		VoxelColor = VoxelColor,
	}.SetRange(this.Select(point => new KeyValuePair<string, Point>(
		key: point.Key,
		value: new Point(
			X: point.Value.X * scaleX,
			Y: point.Value.Y * scaleY))));
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
	public Point RotatedSize(double radians = 0d, byte scaleX = 1, byte scaleY = 1)
	{
		PixelDraw.RotatedSize(
			width: Width,
			height: Height,
			rotatedWidth: out ushort rotatedWidth,
			rotatedHeight: out ushort rotatedHeight,
			radians: radians,
			scaleX: scaleX,
			scaleY: scaleY);
		return new Point(rotatedWidth, rotatedHeight);
	}
	public Sprite Rotate(double radians = 0d, byte scaleX = 1, byte scaleY = 1)
	{
		Sprite sprite = new()
		{
			Texture = Texture.Rotate(
				rotatedWidth: out ushort rotatedWidth,
				rotatedHeight: out ushort rotatedHeight,
				radians: radians,
				scaleX: scaleX,
				scaleY: scaleY,
				width: Width),
			Width = rotatedWidth,
			VoxelColor = VoxelColor,
		};
		return sprite.SetRange(this.Select(pair =>
		{
			PixelDraw.RotatedLocate(
				width: Width,
				height: Height,
				x: pair.Value.X,
				y: pair.Value.Y,
				out int rotatedX,
				out int rotatedY,
				radians: radians,
				scaleX: scaleX,
				scaleY: scaleY);
			return new KeyValuePair<string, Point>(
				key: pair.Key,
				value: new Point(X: rotatedX, Y: rotatedY));
		}));
	}
	public Sprite DrawBoundingBox()
	{
		Texture.DrawBoundingBox(Width);
		return this;
	}
	public void Rotate(IRectangleRenderer renderer, double radians = 0d, byte scaleX = 1, byte scaleY = 1)
	{
		if (scaleX < 1) throw new ArgumentOutOfRangeException(nameof(scaleX));
		if (scaleY < 1) throw new ArgumentOutOfRangeException(nameof(scaleY));
		ushort height = Height;
		if (height > ushort.MaxValue / scaleY)
			throw new OverflowException("Scaled height exceeds maximum allowed size.");
		ushort scaledWidth = (ushort)(Width * scaleX),
			scaledHeight = (ushort)(height * scaleY),
			halfScaledWidth = (ushort)(scaledWidth >> 1),
			halfScaledHeight = (ushort)(scaledHeight >> 1);
		radians %= PixelDraw.Tau;
		double cos = Math.Cos(radians),
			sin = Math.Sin(radians),
			absCos = Math.Abs(cos),
			absSin = Math.Abs(sin);
		uint rWidth = (uint)(scaledWidth * absCos + scaledHeight * absSin),
			rHeight = (uint)(scaledWidth * absSin + scaledHeight * absCos);
		if (rWidth > ushort.MaxValue)
			throw new OverflowException("Rotated width exceeds maximum allowed size.");
		if (rHeight > ushort.MaxValue)
			throw new OverflowException("Rotated height exceeds maximum allowed size.");
		ushort rotatedWidth = (ushort)rWidth,
			rotatedHeight = (ushort)rHeight,
			halfRotatedWidth = (ushort)(rotatedWidth >> 1),
			halfRotatedHeight = (ushort)(rotatedHeight >> 1);
		double offsetX = halfScaledWidth - cos * halfRotatedWidth - sin * halfRotatedHeight,
			offsetY = halfScaledHeight - cos * halfRotatedHeight + sin * halfRotatedWidth;
		//Pre-calculate rotated corners in clockwise order:
		//0 = top-left (0,0)
		//1 = top-right (w,0)
		//2 = bottom-right (w,h)
		//3 = bottom-left (0,h)
		bool isNearZero = absCos < 1e-10 || absSin < 1e-10;
		double[] cornerX = isNearZero ? null : [-halfScaledWidth, halfScaledWidth, halfScaledWidth, -halfScaledWidth],
			cornerY = isNearZero ? null : [-halfScaledHeight, -halfScaledHeight, halfScaledHeight, halfScaledHeight];
		if (!isNearZero)
			for (byte cornerIndex = 0; cornerIndex < 4; cornerIndex++)
			{
				double unrotatedX = cornerX[cornerIndex];
				cornerX[cornerIndex] = unrotatedX * cos - cornerY[cornerIndex] * sin + halfRotatedWidth;
				cornerY[cornerIndex] = unrotatedX * sin + cornerY[cornerIndex] * cos + halfRotatedHeight;
			}
		for (ushort y = 0; y < rotatedHeight; y++)
		{
			ushort startX = 0, endX = (ushort)(rotatedWidth - 1);
			if (!isNearZero)
			{
				double? minX = null, maxX = null;
				for (byte cornerIndex = 0; cornerIndex < 4; cornerIndex++)
				{//Check each edge of the rectangle
					if (Math.Abs(cornerY[cornerIndex] - y) <= 0.5d)
					{//If this corner is on this scanline
						minX = minX.HasValue ? Math.Min(minX.Value, cornerX[cornerIndex]) : cornerX[cornerIndex];
						maxX = maxX.HasValue ? Math.Max(maxX.Value, cornerX[cornerIndex]) : cornerX[cornerIndex];
					}
					byte nextCornerIndex = (byte)((cornerIndex + 1) % 4);
					double currentY = cornerY[cornerIndex],
						nextY = cornerY[nextCornerIndex];
					if ((currentY <= y && nextY >= y) || (currentY >= y && nextY <= y))
					{//If the edge crosses this scanline
						if (Math.Abs(nextY - currentY) > 1e-10)
						{//Only calculate intersection if denominator isn't zero
							double intersectX = cornerX[cornerIndex]
								+ (y - currentY) / (nextY - currentY)
								* (cornerX[nextCornerIndex] - cornerX[cornerIndex]);
							minX = minX.HasValue ? Math.Min(minX.Value, intersectX) : intersectX;
							maxX = maxX.HasValue ? Math.Max(maxX.Value, intersectX) : intersectX;
						}
					}
				}
				if (minX.HasValue) startX = (ushort)Math.Floor(minX.Value);
				if (maxX.HasValue) endX = (ushort)Math.Ceiling(maxX.Value);
			}
			for (ushort x = startX; x <= endX; x++)
			{
				double sourceX = (x * cos + y * sin + offsetX) / scaleX,
					sourceY = (y * cos - x * sin + offsetY) / scaleY;
				if (sourceX >= 0d && sourceX < Width && sourceY >= 0d && sourceY < height)
					renderer.Rect(
						x: x,
						y: y,
						color: Pixel(
							x: (ushort)Math.Floor(sourceX),
							y: (ushort)Math.Floor(sourceY)));
			}
		}
	}
	#endregion Image manipulation
}
