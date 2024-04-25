using RectpackSharp;
using System.Collections.Generic;
using System.Linq;
using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;
using Voxel2Pixel.Render;

namespace Voxel2Pixel.Pack
{
	public class Sprite : Dictionary<string, Point>, ISprite, IRectangleRenderer, ITriangleRenderer, IVoxelColor
	{
		#region ISprite
		public byte[] Texture { get; set; }
		public ushort Width { get; set; }
		public ushort Height => (ushort)((Texture.Length >> 2) / Width);
		public const string Origin = "Origin";
		public Sprite AddRange(params KeyValuePair<string, Point>[] points) => AddRange(points.AsEnumerable());
		public Sprite AddRange(IEnumerable<KeyValuePair<string, Point>> points)
		{
			foreach (KeyValuePair<string, Point> point in points)
				this[point.Key] = point.Value;
			return this;
		}
		#endregion ISprite
		#region Sprite
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
			foreach (KeyValuePair<string, Point> point in sprite)
				this[point.Key] = point.Value;
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
		#region Image manipulation
		public Sprite(out RectpackSharp.PackingRectangle[] packingRectangles, IEnumerable<ISprite> sprites) : this(out packingRectangles, sprites.ToArray()) { }
		public Sprite(out RectpackSharp.PackingRectangle[] packingRectangles, params ISprite[] sprites)
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
			Width = (ushort)PixelDraw.NextPowerOf2(bounds.BiggerSide);
			Texture = new byte[Width * Width << 2];
			foreach (PackingRectangle packingRectangle in packingRectangles)
				Texture.DrawInsert(
					x: (ushort)(packingRectangle.X + 1),
					y: (ushort)(packingRectangle.Y + 1),
					insert: sprites[packingRectangle.Id].Texture,
					insertWidth: sprites[packingRectangle.Id].Width,
					width: Width);
		}
		public static IEnumerable<Sprite> SameSize(ushort addWidth = 0, ushort addHeight = 0, params ISprite[] sprites) => sprites.AsEnumerable().SameSize(addWidth, addHeight);
		/// <returns>resized copy</returns>
		public Sprite Resize(ushort croppedWidth, ushort croppedHeight) => Crop(0, 0, croppedWidth, croppedHeight);
		/// <returns>lower right cropped copy</returns>
		public Sprite Crop(ushort x, ushort y) => Crop(x, y, (ushort)(Width - x), (ushort)(Height - y));
		/// <returns>cropped copy</returns>
		public Sprite Crop(int x, int y, ushort croppedWidth, ushort croppedHeight) => new Sprite
		{
			Texture = Texture.Crop(x, y, croppedWidth, croppedHeight, Width),
			Width = croppedWidth,
		}.AddRange(this.Select(point => new KeyValuePair<string, Point>(
			key: point.Key,
			value: new Point(
				X: point.Value.X - x,
				Y: point.Value.Y - y))));
		/// <returns>cropped copy</returns>
		public Sprite TransparentCrop(byte threshold = 128) => new Sprite
		{
			Texture = Texture.TransparentCrop(
				cutLeft: out ushort cutLeft,
				cutTop: out ushort cutTop,
				croppedWidth: out ushort croppedWidth,
				croppedHeight: out _,
				width: Width,
				threshold: threshold),
			Width = croppedWidth,
		}.AddRange(this.Select(point => new KeyValuePair<string, Point>(
			key: point.Key,
			value: new Point(
				X: point.Value.X - cutLeft,
				Y: point.Value.Y - cutTop))));
		/// <returns>cropped copy</returns>
		public Sprite TransparentCropPlusOne(byte threshold = 128) => new Sprite
		{
			Texture = Texture.TransparentCropPlusOne(
				cutLeft: out int cutLeft,
				cutTop: out int cutTop,
				croppedWidth: out ushort croppedWidth,
				croppedHeight: out _,
				width: Width,
				threshold: threshold),
			Width = croppedWidth,
		}.AddRange(this.Select(point => new KeyValuePair<string, Point>(
			key: point.Key,
			value: new Point(
				X: point.Value.X - cutLeft,
				Y: point.Value.Y - cutTop))));
		public Sprite Outline(uint color = 0xFFu) => new Sprite
		{
			Texture = Texture.Outline(
				width: Width,
				color: color),
			Width = Width,
		}.AddRange(this);
		/// <returns>cropped and outlined copy</returns>
		public Sprite CropOutline(uint color = 0xFFu) => new Sprite
		{
			Texture = Texture
				.TransparentCropPlusOne(
					cutLeft: out int cutLeft,
					cutTop: out int cutTop,
					croppedWidth: out ushort croppedWidth,
					croppedHeight: out _,
					width: Width)
				.Outline(
					width: croppedWidth,
					color: color),
			Width = croppedWidth,
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
		public Sprite DrawTransparentInsert(int x, int y, Sprite insert, byte threshold = 128) => DrawTransparentInsert(x, y, insert.Texture, insert.Width, threshold);
		public Sprite DrawTransparentInsert(int x, int y, byte[] insert, ushort insertWidth = 0, byte threshold = 128)
		{
			Texture = Texture.DrawTransparentInsert(x, y, insert, insertWidth, Width, threshold);
			return this;
		}
		public static IEnumerable<Sprite> AddFrameNumbers(uint color = 0xFFFFFFFF, params Sprite[] sprites) => sprites.AsEnumerable().AddFrameNumbers(color);
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
		public Sprite Draw3x4Bottom(string @string, uint color = 0xFFFFFFFF) => Draw3x4(
			@string: @string,
			x: 0,
			y: Height - 4,
			color: color);
		#endregion Image manipulation
		#region Voxel drawing
		public static IEnumerable<Sprite> Above4(IModel model, IVoxelColor voxelColor, params ushort[] voxelOrigin)
		{
			if (voxelOrigin is null || voxelOrigin.Length < 3)
				voxelOrigin = model.BottomCenter();
			TurnModel turnModel = new()
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
				ushort width = VoxelDraw.AboveWidth(turnModel);
				Sprite sprite = new()
				{
					Texture = new byte[width * VoxelDraw.AboveHeight(turnModel) << 2],
					Width = width,
					VoxelColor = voxelColor,
				};
				sprite[Origin] = new Point(
					X: locateX,
					Y: locateY);
				VoxelDraw.Above(
					model: turnModel,
					renderer: sprite);
				yield return sprite;
				turnModel.CounterZ();
			}
		}
		public static IEnumerable<Sprite> Iso4(IModel model, IVoxelColor voxelColor, params ushort[] voxelOrigin)
		{
			if (voxelOrigin is null || voxelOrigin.Length < 3)
				voxelOrigin = model.BottomCenter();
			TurnModel turnModel = new()
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
				Sprite sprite = new()
				{
					Texture = new byte[width * VoxelDraw.IsoHeight(turnModel) << 2],
					Width = width,
					VoxelColor = voxelColor,
				};
				sprite[Origin] = new Point(
					X: locateX,
					Y: locateY);
				VoxelDraw.Iso(
					model: turnModel,
					renderer: sprite);
				yield return sprite;
				turnModel.CounterZ();
			}
		}
		public static IEnumerable<Sprite> Iso8(IModel model, IVoxelColor voxelColor, params ushort[] voxelOrigin)
		{
			if (voxelOrigin is null || voxelOrigin.Length < 3)
				voxelOrigin = model.BottomCenter();
			TurnModel turnModel = new()
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
				ushort width = VoxelDraw.AboveWidth(turnModel);
				Sprite sprite = new()
				{
					Texture = new byte[width * VoxelDraw.AboveHeight(turnModel) << 2],
					Width = width,
					VoxelColor = voxelColor,
				};
				sprite[Origin] = new Point(
					X: locateX,
					Y: locateY);
				VoxelDraw.Above(
					model: turnModel,
					renderer: sprite);
				yield return sprite
					.TransparentCrop()
					.Upscale(5, 4);
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
				width = (ushort)(VoxelDraw.IsoWidth(turnModel) << 1);
				sprite = new Sprite2x
				{
					Texture = new byte[width * VoxelDraw.IsoHeight(turnModel) << 2],
					Width = width,
					VoxelColor = voxelColor,
				};
				sprite[Origin] = new Point(
					X: locateX << 1,
					Y: locateY);
				VoxelDraw.Iso(
					model: turnModel,
					renderer: sprite);
				yield return sprite.TransparentCrop();
			}
		}
		public static IEnumerable<Sprite> Iso8Shadows(IModel model, IVoxelColor voxelColor, params ushort[] voxelOrigin)
		{
			if (voxelOrigin is null || voxelOrigin.Length < 3)
				voxelOrigin = model.BottomCenter();
			TurnModel turnModel = new()
			{
				Model = model,
			};
			for (byte angle = 0; angle < 4; angle++)
			{
				turnModel.ReverseRotate(
					x: out ushort turnedX,
					y: out ushort turnedY,
					z: out _,
					coordinates: voxelOrigin);
				int locateX = turnedX,
					locateY = turnedY;
				ushort width = VoxelDraw.UnderneathWidth(turnModel);
				Sprite sprite = new()
				{
					Texture = new byte[width * VoxelDraw.UnderneathHeight(turnModel) << 2],
					Width = width,
					VoxelColor = voxelColor,
				};
				sprite[Origin] = new Point(
					X: locateX,
					Y: locateY);
				VoxelDraw.Underneath(
					model: turnModel,
					renderer: sprite);
				yield return sprite
					.TransparentCrop()
					.Upscale(5, 4);
				turnModel.CounterZ();
				turnModel.ReverseRotate(
					x: out turnedX,
					y: out turnedY,
					z: out _,
					coordinates: voxelOrigin);
				VoxelDraw.IsoShadowLocate(
					pixelX: out locateX,
					pixelY: out locateY,
					model: turnModel,
					voxelX: turnedX,
					voxelY: turnedY);
				width = (ushort)(VoxelDraw.IsoShadowWidth(turnModel) << 1);
				sprite = new Sprite2x
				{
					Texture = new byte[width * VoxelDraw.IsoShadowHeight(turnModel) << 2],
					Width = width,
					VoxelColor = voxelColor,
				};
				sprite[Origin] = new Point(
					X: locateX << 1,
					Y: locateY);
				VoxelDraw.IsoShadow(
					model: turnModel,
					renderer: sprite);
				yield return sprite.TransparentCrop();
			}
		}
		public static Sprite AboveOutlinedWithShadow(IModel model, IVoxelColor voxelColor, uint shadow = 0x88u, uint outline = 0xFFu)
		{
			OneVoxelColor voxelShadow = new(shadow);
			Sprite sprite = new((ushort)(VoxelDraw.AboveWidth(model) * 5 + 2), (ushort)(VoxelDraw.AboveHeight(model) * 4 + 2))
			{
				VoxelColor = voxelColor,
			},
			shadowSprite = new((ushort)(VoxelDraw.AboveWidth(model) * 5 + 2), (ushort)(VoxelDraw.AboveHeight(model) * 4 + 2))
			{
				VoxelColor = voxelColor,
			};
			VoxelDraw.Overhead(model, new OffsetRenderer()
			{
				RectangleRenderer = shadowSprite,
				VoxelColor = voxelShadow,
				OffsetX = 1,
				OffsetY = (model.SizeZ << 2) + 1,
				ScaleX = 5,
				ScaleY = 4,
			});
			VoxelDraw.Above(model, new OffsetRenderer()
			{
				RectangleRenderer = sprite,
				VoxelColor = voxelColor,
				OffsetX = 1,
				OffsetY = 1,
				ScaleX = 5,
				ScaleY = 4,
			});
			return shadowSprite.DrawTransparentInsert(0, 0, sprite.Outline(outline));
		}
		public static Sprite IsoOutlinedWithShadow(IModel model, IVoxelColor voxelColor, uint shadow = 0x88u, uint outline = 0xFFu)
		{
			OneVoxelColor voxelShadow = new(shadow);
			Sprite sprite = new((ushort)((VoxelDraw.IsoWidth(model) << 1) + 2), (ushort)(VoxelDraw.IsoHeight(model) + 2))
			{
				VoxelColor = voxelColor,
			},
			shadowSprite = new((ushort)((VoxelDraw.IsoWidth(model) << 1) + 2), (ushort)(VoxelDraw.IsoHeight(model) + 2))
			{
				VoxelColor = voxelColor,
			};
			VoxelDraw.IsoShadow(model, new OffsetRenderer()
			{
				RectangleRenderer = shadowSprite,
				VoxelColor = voxelShadow,
				OffsetX = 1,
				OffsetY = (model.SizeZ << 2) + 1,
				ScaleX = 2,
			});
			VoxelDraw.Iso(model, new OffsetRenderer()
			{
				RectangleRenderer = sprite,
				VoxelColor = voxelColor,
				OffsetX = 1,
				OffsetY = 1,
				ScaleX = 2,
			});
			return shadowSprite.DrawTransparentInsert(0, 0, sprite.Outline(outline));
		}
		#endregion Voxel drawing
	}
}
