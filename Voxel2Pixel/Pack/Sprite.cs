using RectpackSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;
using Voxel2Pixel.Render;
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
		public Sprite(ISprite sprite) : this()
		{
			Texture = sprite.Texture;
			Width = sprite.Width;
			if (sprite is Sprite s)
				VoxelColor = s.VoxelColor;
			AddRange(sprite);
		}
		public Sprite AddRange(params KeyValuePair<string, Point>[] points) => AddRange(points.AsEnumerable());
		public Sprite AddRange(IEnumerable<KeyValuePair<string, Point>> points)
		{
			foreach (KeyValuePair<string, Point> point in points)
				this[point.Key] = point.Value;
			return this;
		}
		public Sprite ReplaceSelf(Sprite sprite)
		{
			Texture = sprite.Texture;
			Width = sprite.Width;
			VoxelColor = sprite.VoxelColor;
			Clear();
			AddRange(sprite);
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
		public Sprite(out PackingRectangle[] packingRectangles, IEnumerable<ISprite> sprites) : this(packingRectangles: out packingRectangles, sprites: sprites.ToArray()) { }
		public Sprite(out PackingRectangle[] packingRectangles, params ISprite[] sprites)
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
		public Sprite(Dictionary<string, ISprite> dictionary, out TextureAtlas textureAtlas) : this(sprites: [.. dictionary], textureAtlas: out textureAtlas) { }
		public Sprite(KeyValuePair<string, ISprite>[] sprites, out TextureAtlas textureAtlas) : this(packingRectangles: out RectpackSharp.PackingRectangle[] packingRectangles, sprites: sprites.Select(pair => pair.Value)) =>
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
			Sprite sprite = TransparentCrop(threshold);
			return scaleX > 1 || scaleY > 1 ?
				sprite.Upscale(scaleX, scaleY)
				: sprite;
		}
		public static IEnumerable<Sprite> SameSize(ushort addWidth = 0, ushort addHeight = 0, params ISprite[] sprites) => sprites.AsEnumerable().SameSize(addWidth, addHeight);
		public static IEnumerable<Sprite> SameSize(params ISprite[] sprites) => sprites.AsEnumerable().SameSize();
		/// <returns>resized copy</returns>
		public Sprite Resize(ushort newWidth, ushort newHeight) => new Sprite
		{
			Texture = Texture.Resize(newWidth, newHeight, Width),
			Width = newWidth,
			VoxelColor = VoxelColor,
		}.AddRange(this);
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
		}.AddRange(this.Select(point => new KeyValuePair<string, Point>(
			key: point.Key,
			value: new Point(
				X: point.Value.X - x,
				Y: point.Value.Y - y))));
		/// <returns>cropped copy</returns>
		public Sprite TransparentCrop(byte threshold = PixelDraw.DefaultTransparencyThreshold) => new Sprite
		{
			Texture = Texture.TransparentCrop(
				cutLeft: out ushort cutLeft,
				cutTop: out ushort cutTop,
				croppedWidth: out ushort croppedWidth,
				croppedHeight: out _,
				width: Width,
				threshold: threshold),
			Width = croppedWidth,
			VoxelColor = VoxelColor,
		}.AddRange(this.Select(point => new KeyValuePair<string, Point>(
			key: point.Key,
			value: new Point(
				X: point.Value.X - cutLeft,
				Y: point.Value.Y - cutTop))));
		/// <returns>cropped copy</returns>
		public Sprite TransparentCropPlusOne(byte threshold = PixelDraw.DefaultTransparencyThreshold) => new Sprite
		{
			Texture = Texture.TransparentCropPlusOne(
				cutLeft: out int cutLeft,
				cutTop: out int cutTop,
				croppedWidth: out ushort croppedWidth,
				croppedHeight: out _,
				width: Width,
				threshold: threshold),
			Width = croppedWidth,
			VoxelColor = VoxelColor,
		}.AddRange(this.Select(point => new KeyValuePair<string, Point>(
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
		}.AddRange(this);
		/// <returns>cropped and outlined copy</returns>
		public Sprite CropOutline(uint color = PixelDraw.DefaultOutlineColor, byte threshold = PixelDraw.DefaultTransparencyThreshold) => new Sprite
		{
			Texture = Texture
				.TransparentCropPlusOne(
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
		}.AddRange(this.Select(point => new KeyValuePair<string, Point>(
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
		}.AddRange(this.Select(point => new KeyValuePair<string, Point>(
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
		#endregion Image manipulation
		#region Voxel drawing
		public Sprite(IModel model, IVoxelColor voxelColor, IVoxelColor shadowColor, Point3D origin, Perspective perspective = Perspective.Iso, byte peakScaleX = 6, byte peakScaleY = 6, bool flipX = false, bool flipY = false, bool flipZ = false, CuboidOrientation cuboidOrientation = null, ushort scaleX = 1, ushort scaleY = 1, bool shadow = false, byte threshold = PixelDraw.DefaultTransparencyThreshold) : this(model: model, voxelColor: voxelColor, points: new Dictionary<string, Point3D> { { Origin, origin }, }, perspective: perspective, peakScaleX: peakScaleX, peakScaleY: peakScaleY, flipX: flipX, flipY: flipY, flipZ: flipZ, cuboidOrientation: cuboidOrientation, scaleX: scaleX, scaleY: scaleY, shadow: shadow, shadowColor: shadowColor, threshold: threshold) { }
		public Sprite(IModel model, IVoxelColor voxelColor, Point3D origin, Perspective perspective = Perspective.Iso, byte peakScaleX = 6, byte peakScaleY = 6, bool flipX = false, bool flipY = false, bool flipZ = false, CuboidOrientation cuboidOrientation = null, ushort scaleX = 1, ushort scaleY = 1, bool shadow = false, uint shadowColor = DefaultShadowColor, byte threshold = PixelDraw.DefaultTransparencyThreshold) : this(model: model, voxelColor: voxelColor, perspective: perspective, points: new Dictionary<string, Point3D> { { Origin, origin }, }, peakScaleX: peakScaleX, peakScaleY: peakScaleY, flipX: flipX, flipY: flipY, flipZ: flipZ, cuboidOrientation: cuboidOrientation, scaleX: scaleX, scaleY: scaleY, shadow: shadow, shadowColor: shadowColor, threshold: threshold) { }
		public Sprite(IModel model, IVoxelColor voxelColor, IEnumerable<KeyValuePair<string, Point3D>> points = null, Perspective perspective = Perspective.Iso, byte peakScaleX = 6, byte peakScaleY = 6, bool flipX = false, bool flipY = false, bool flipZ = false, CuboidOrientation cuboidOrientation = null, ushort scaleX = 1, ushort scaleY = 1, bool shadow = false, uint shadowColor = DefaultShadowColor, byte threshold = PixelDraw.DefaultTransparencyThreshold) : this(model: model, voxelColor: voxelColor, shadowColor: new OneVoxelColor(shadowColor), points: points, perspective: perspective, peakScaleX: peakScaleX, peakScaleY: peakScaleY, flipX: flipX, flipY: flipY, flipZ: flipZ, cuboidOrientation: cuboidOrientation, scaleX: scaleX, scaleY: scaleY, shadow: shadow, threshold: threshold) { }
		public Sprite(
			IModel model,
			IVoxelColor voxelColor,
			IVoxelColor shadowColor,
			IEnumerable<KeyValuePair<string, Point3D>> points = null,
			Perspective perspective = Perspective.Iso,
			byte peakScaleX = 6,
			byte peakScaleY = 6,
			bool flipX = false,
			bool flipY = false,
			bool flipZ = false,
			CuboidOrientation cuboidOrientation = null,
			ushort scaleX = 1,
			ushort scaleY = 1,
			bool shadow = false,
			bool outline = false,
			uint outlineColor = PixelDraw.DefaultOutlineColor,
			byte threshold = PixelDraw.DefaultTransparencyThreshold)
		{
			if (peakScaleX < 2) throw new ArgumentOutOfRangeException("peakScaleX");
			if (peakScaleY < 1) throw new ArgumentOutOfRangeException("peakScaleY");
			if (scaleX < 1) throw new ArgumentOutOfRangeException("scaleX");
			if (scaleY < 1) throw new ArgumentOutOfRangeException("scaleY");
			if (flipX || flipY || flipZ)
				model = new FlipModel
				{
					Model = model,
					FlipX = flipX,
					FlipY = flipY,
					FlipZ = flipZ,
				};
			cuboidOrientation ??= CuboidOrientation.SOUTH0;
			if (cuboidOrientation != CuboidOrientation.SOUTH0)
				model = new TurnModel
				{
					Model = model,
					CuboidOrientation = cuboidOrientation,
				};
			Width = (ushort)(VoxelDraw.Width(perspective, model, peakScaleX) * scaleX + (outline ? 2 : 0));
			ushort height = (ushort)(VoxelDraw.Height(perspective, model, peakScaleY) * scaleY + (outline ? 2 : 0));
			Texture = new byte[Width * height << 2];
			VoxelColor = voxelColor;
			VoxelDraw.Draw(
				perspective: perspective,
				model: model,
				renderer: new OffsetRenderer
				{
					RectangleRenderer = this,
					OffsetX = outline ? 1 : 0,
					OffsetY = outline ? 1 : 0,
					ScaleX = scaleX,
					ScaleY = scaleY,
				},
				peakScaleX: peakScaleX,
				peakScaleY: peakScaleY);
			if (outline)
				Texture = Texture.Outline(
					width: Width,
					color: outlineColor,
					threshold: threshold);
			if (shadow && perspective.HasShadow())
			{
				Sprite sprite = new()
				{
					Texture = Texture,
					Width = Width,
				};
				Texture = new byte[Texture.Length];
				Perspective shadowPerspective = perspective == Perspective.Iso ? Perspective.IsoShadow : Perspective.Underneath;
				VoxelColor = shadowColor;
				VoxelDraw.Draw(
					perspective: shadowPerspective,
					model: model,
					renderer: new OffsetRenderer
					{
						RectangleRenderer = this,
						OffsetX = (VoxelDraw.Width(perspective, model) - VoxelDraw.Width(shadowPerspective, model)) * scaleX + (outline ? 1 : 0),
						OffsetY = (VoxelDraw.Height(perspective, model) - VoxelDraw.Height(shadowPerspective, model)) * scaleY + (outline ? 1 : 0),
						ScaleX = scaleX,
						ScaleY = scaleY,
					});
				VoxelColor = voxelColor;
				DrawTransparentInsert(
					x: 0,
					y: 0,
					insert: sprite,
					threshold: threshold);
			}
			points ??= new Dictionary<string, Point3D> { { Origin, model.BottomCenter() }, };
			Point Point(Point3D point3D)
			{
				Point point = VoxelDraw.Locate(
					perspective: perspective,
					model: model,
					point: point3D,
					peakScaleX: peakScaleX,
					peakScaleY: peakScaleY);
				return new Point(
					X: point.X * scaleX + (outline ? 1 : 0),
					Y: point.Y * scaleY + (outline ? 1 : 0));
			}
			AddRange(points.Select(point => new KeyValuePair<string, Point>(point.Key, Point(point.Value))));
			ReplaceSelf(TransparentCrop(threshold));
		}
		public static IEnumerable<Sprite> Z4(IModel model, IVoxelColor voxelColor, IVoxelColor shadowColor, Point3D origin, Perspective perspective = Perspective.Iso, byte peakScaleX = 6, byte peakScaleY = 6, bool flipX = false, bool flipY = false, bool flipZ = false, CuboidOrientation cuboidOrientation = null, ushort scaleX = 1, ushort scaleY = 1, bool shadow = false, bool outline = false, uint outlineColor = PixelDraw.DefaultOutlineColor, byte threshold = PixelDraw.DefaultTransparencyThreshold, Turn turn = Turn.CounterZ) => Z4(model: model, voxelColor: voxelColor, shadowColor: shadowColor, perspective: perspective, points: new Dictionary<string, Point3D> { { Origin, origin }, }, peakScaleX: peakScaleX, peakScaleY: peakScaleY, flipX: flipX, flipY: flipY, flipZ: flipZ, cuboidOrientation: cuboidOrientation, scaleX: scaleX, scaleY: scaleY, shadow: shadow, outline: outline, outlineColor: outlineColor, threshold: threshold, turn: turn);
		public static IEnumerable<Sprite> Z4(IModel model, IVoxelColor voxelColor, Point3D voxelOrigin, Perspective perspective = Perspective.Iso, byte peakScaleX = 6, byte peakScaleY = 6, bool flipX = false, bool flipY = false, bool flipZ = false, CuboidOrientation cuboidOrientation = null, ushort scaleX = 1, ushort scaleY = 1, bool shadow = false, uint shadowColor = DefaultShadowColor, bool outline = false, uint outlineColor = PixelDraw.DefaultOutlineColor, byte threshold = PixelDraw.DefaultTransparencyThreshold, Turn turn = Turn.CounterZ) => Z4(model: model, voxelColor: voxelColor, shadowColor: new OneVoxelColor(shadowColor), perspective: perspective, points: new Dictionary<string, Point3D> { { Origin, voxelOrigin }, }, peakScaleX: peakScaleX, peakScaleY: peakScaleY, flipX: flipX, flipY: flipY, flipZ: flipZ, cuboidOrientation: cuboidOrientation, scaleX: scaleX, scaleY: scaleY, shadow: shadow, outline: outline, outlineColor: outlineColor, threshold: threshold, turn: turn);
		public static IEnumerable<Sprite> Z4(IModel model, IVoxelColor voxelColor, Perspective perspective = Perspective.Above, IEnumerable<KeyValuePair<string, Point3D>> points = null, byte peakScaleX = 6, byte peakScaleY = 6, bool flipX = false, bool flipY = false, bool flipZ = false, CuboidOrientation cuboidOrientation = null, ushort scaleX = 1, ushort scaleY = 1, bool shadow = false, uint shadowColor = DefaultShadowColor, bool outline = false, uint outlineColor = PixelDraw.DefaultOutlineColor, byte threshold = PixelDraw.DefaultTransparencyThreshold, Turn turn = Turn.CounterZ) => Z4(model: model, voxelColor: voxelColor, shadowColor: new OneVoxelColor(shadowColor), perspective: perspective, points: points, peakScaleX: peakScaleX, peakScaleY: peakScaleY, flipX: flipX, flipY: flipY, flipZ: flipZ, cuboidOrientation: cuboidOrientation, scaleX: scaleX, scaleY: scaleY, shadow: shadow, outline: outline, outlineColor: outlineColor, threshold: threshold, turn: turn);
		public static IEnumerable<Sprite> Z4(
			IModel model,
			IVoxelColor voxelColor,
			IVoxelColor shadowColor,
			IEnumerable<KeyValuePair<string, Point3D>> points = null,
			Perspective perspective = Perspective.Above,
			byte peakScaleX = 6,
			byte peakScaleY = 6,
			bool flipX = false,
			bool flipY = false,
			bool flipZ = false,
			CuboidOrientation cuboidOrientation = null,
			ushort scaleX = 1,
			ushort scaleY = 1,
			bool shadow = false,
			bool outline = false,
			uint outlineColor = PixelDraw.DefaultOutlineColor,
			byte threshold = PixelDraw.DefaultTransparencyThreshold,
			Turn turn = Turn.CounterZ)
		{
			points ??= new Dictionary<string, Point3D> { { Origin, model.BottomCenter() }, };
			if (flipX || flipY || flipZ)
				model = new FlipModel
				{
					Model = model,
					FlipX = flipX,
					FlipY = flipY,
					FlipZ = flipZ,
				};
			TurnModel turnModel = new()
			{
				Model = model,
				CuboidOrientation = cuboidOrientation ?? CuboidOrientation.SOUTH0,
			};
			for (byte angle = 0; angle < 4; angle++)
			{
				yield return new(
					model: turnModel,
					voxelColor: voxelColor,
					shadowColor: shadowColor,
					perspective: perspective,
					points: points.Select(point => new KeyValuePair<string, Point3D>(point.Key, turnModel.ReverseRotate(point.Value))),
					peakScaleX: peakScaleX,
					peakScaleY: peakScaleY,
					scaleX: scaleX,
					scaleY: scaleY,
					shadow: shadow,
					outline: outline,
					outlineColor: outlineColor,
					threshold: threshold);
				turnModel.Turn(turn);
			}
		}
		public static IEnumerable<Sprite> Iso8(IModel model, IVoxelColor voxelColor, Point3D origin, bool shadow = false, uint shadowColor = DefaultShadowColor, byte threshold = PixelDraw.DefaultTransparencyThreshold) => Iso8(model, voxelColor, new Dictionary<string, Point3D> { { Origin, origin }, }, shadow, shadowColor, threshold);
		public static IEnumerable<Sprite> Iso8(IModel model, IVoxelColor voxelColor, IEnumerable<KeyValuePair<string, Point3D>> points = null, bool shadow = false, uint shadowColor = DefaultShadowColor, byte threshold = PixelDraw.DefaultTransparencyThreshold)
		{
			points ??= new Dictionary<string, Point3D> { { Origin, model.BottomCenter() }, };
			TurnModel turnModel = new()
			{
				Model = model,
			};
			for (byte angle = 0; angle < 4; angle++)
			{
				yield return new Sprite(
						perspective: Perspective.Above,
						model: turnModel,
						voxelColor: voxelColor,
						points: points.Select(point => new KeyValuePair<string, Point3D>(point.Key, turnModel.ReverseRotate(point.Value))),
						shadow: shadow,
						shadowColor: shadowColor,
						threshold: threshold)
					.TransparentCrop(threshold)
					.Upscale(5, 4);
				turnModel.Turn(Turn.CounterZ);
				yield return new Sprite(
						perspective: Perspective.Iso,
						model: turnModel,
						voxelColor: voxelColor,
						points: points.Select(point => new KeyValuePair<string, Point3D>(point.Key, turnModel.ReverseRotate(point.Value))),
						shadow: shadow,
						shadowColor: shadowColor,
						threshold: threshold)
					.TransparentCrop(threshold)
					.Upscale(2);
			}
		}
		#endregion Voxel drawing
	}
}
