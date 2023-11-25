using RectpackSharp;
using System.Collections.Generic;
using System.Linq;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;

namespace Voxel2Pixel.Pack
{
	public class Sprite : ISprite, IRectangleRenderer, ITriangleRenderer, IVoxelColor
	{
		#region ISprite
		public byte[] Texture { get; set; }
		public ushort Width { get; set; }
		public ushort Height => (ushort)((Texture.Length >> 2) / Width);
		public ushort OriginX { get; set; }
		public ushort OriginY { get; set; }
		#endregion ISprite
		#region Sprite
		public Sprite() { }
		public Sprite(ushort width, ushort height) : this()
		{
			Texture = new byte[width * height << 2];
			Width = width;
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
		public static IEnumerable<Sprite> SameSize(ushort addWidth, ushort addHeight, params ISprite[] sprites) => SameSize(addWidth, addHeight, sprites.AsEnumerable());
		public static IEnumerable<Sprite> SameSize(params ISprite[] sprites) => SameSize(0, 0, sprites);
		public static IEnumerable<Sprite> SameSize(IEnumerable<ISprite> sprites) => SameSize(0, 0, sprites);
		public static IEnumerable<Sprite> SameSize(ushort addWidth, ushort addHeight, IEnumerable<ISprite> sprites)
		{
			ushort originX = sprites.Select(sprite => sprite.OriginX).Max(),
				originY = sprites.Select(sprite => sprite.OriginY).Max(),
				width = (ushort)(sprites.Select(sprite => sprite.Width + originX - sprite.OriginX).Max() + addWidth),
				height = (ushort)(sprites.Select(sprite => sprite.Height + originY - sprite.OriginY).Max() + addHeight);
			int textureLength = width * height << 2;
			foreach (ISprite sprite in sprites)
				yield return new Sprite
				{
					Texture = new byte[textureLength]
						.DrawInsert(
							x: originX - sprite.OriginX,
							y: originY - sprite.OriginY,
							insert: sprite.Texture,
							insertWidth: sprite.Width,
							width: width),
					Width = width,
					OriginX = originX,
					OriginY = originY,
				};
		}
		/// <returns>resized copy</returns>
		public Sprite Resize(ushort croppedWidth, ushort croppedHeight) => Crop(0, 0, croppedWidth, croppedHeight);
		/// <returns>lower right cropped copy</returns>
		public Sprite Crop(ushort x, ushort y) => Crop(x, y, (ushort)(Width - x), (ushort)(Height - y));
		/// <returns>cropped copy</returns>
		public Sprite Crop(int x, int y, ushort croppedWidth, ushort croppedHeight) => new Sprite
		{
			Texture = Texture.Crop(x, y, croppedWidth, croppedHeight, Width),
			Width = croppedWidth,
			OriginX = (ushort)(OriginX - x),
			OriginY = (ushort)(OriginY - y),
		};
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
			OriginX = (ushort)(OriginX - cutLeft),
			OriginY = (ushort)(OriginY - cutTop),
		};
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
			OriginX = (ushort)(OriginX - cutLeft),
			OriginY = (ushort)(OriginY - cutTop),
		};
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
			OriginX = (ushort)(OriginX - cutLeft),
			OriginY = (ushort)(OriginY - cutTop),
		};
		/// <returns>upscaled copy</returns>
		public Sprite Upscale(ushort factorX, ushort factorY = 1) => new Sprite
		{
			Texture = Texture.Upscale(
				factorX: factorX,
				factorY: factorY,
				newWidth: out ushort newWidth,
				width: Width),
			Width = newWidth,
			OriginX = (ushort)(OriginX * factorX),
			OriginY = (ushort)(OriginY * factorY),
		};
		public static IEnumerable<Sprite> AddFrameNumbers(uint color = 0xFFFFFFFF, params Sprite[] sprites) => AddFrameNumbers(sprites.AsEnumerable(), color);
		public static IEnumerable<Sprite> AddFrameNumbers(IEnumerable<Sprite> frames, uint color = 0xFFFFFFFF)
		{
			int frame = 0;
			foreach (Sprite sprite in frames)
			{
				sprite.Texture.Draw3x4(
					@string: (++frame).ToString(),
					width: sprite.Width,
					x: 0,
					y: sprite.Height - 4,
					color: color);
				yield return sprite;
			}
		}
		#endregion Image manipulation
		#region Voxel drawing
		public static IEnumerable<Sprite> Above4(IModel model, IVoxelColor voxelColor, params ushort[] voxelOrigin)
		{
			if (voxelOrigin is null || voxelOrigin.Length < 3)
				voxelOrigin = new ushort[3];
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
				ushort width = VoxelDraw.AboveWidth(turnModel);
				Sprite sprite = new Sprite
				{
					Texture = new byte[width * VoxelDraw.AboveHeight(turnModel) << 2],
					Width = width,
					VoxelColor = voxelColor,
					OriginX = (ushort)locateX,
					OriginY = (ushort)locateY,
				};
				VoxelDraw.Above(
					model: turnModel,
					renderer: sprite);
				yield return sprite;
				turnModel.ClockZ();
			}
		}
		public static IEnumerable<Sprite> Iso4(IModel model, IVoxelColor voxelColor, params ushort[] voxelOrigin)
		{
			if (voxelOrigin is null || voxelOrigin.Length < 3)
				voxelOrigin = new ushort[3];
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
				Sprite sprite = new Sprite
				{
					Texture = new byte[width * VoxelDraw.IsoHeight(turnModel) << 2],
					Width = width,
					VoxelColor = voxelColor,
					OriginX = (ushort)locateX,
					OriginY = (ushort)locateY,
				};
				VoxelDraw.Iso(
					model: turnModel,
					renderer: sprite);
				yield return sprite;
				turnModel.ClockZ();
			}
		}
		public static IEnumerable<Sprite> Iso8(IModel model, IVoxelColor voxelColor, params ushort[] voxelOrigin)
		{
			if (voxelOrigin is null || voxelOrigin.Length < 3)
				voxelOrigin = new ushort[3];
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
				ushort width = VoxelDraw.AboveWidth(turnModel);
				Sprite sprite = new Sprite
				{
					Texture = new byte[width * VoxelDraw.AboveHeight(turnModel) << 2],
					Width = width,
					VoxelColor = voxelColor,
					OriginX = (ushort)locateX,
					OriginY = (ushort)locateY,
				};
				VoxelDraw.Above(
					model: turnModel,
					renderer: sprite);
				yield return sprite;
				turnModel.ClockZ();
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
				width = VoxelDraw.IsoWidth(turnModel);
				sprite = new Sprite
				{
					Texture = new byte[width * VoxelDraw.IsoHeight(turnModel) << 2],
					Width = width,
					VoxelColor = voxelColor,
					OriginX = (ushort)locateX,
					OriginY = (ushort)locateY,
				};
				VoxelDraw.Iso(
					model: turnModel,
					renderer: sprite);
				yield return sprite;
			}
		}
		#endregion Voxel drawing
	}
}
