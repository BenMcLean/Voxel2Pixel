using RectpackSharp;
using System.Linq;
using System.Xml.Serialization;
using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Model;
using Voxel2Pixel.Render;
using static Voxel2Pixel.Draw.PixelDraw;

namespace Voxel2Pixel.Pack
{
	public static class IsoPacker
	{
		public static byte[] Pack8(IModel model, IVoxelColor voxelColor, out int width, out RectpackSharp.PackingRectangle[] packingRectangles)
		{
			IsoSprites(
				model: model,
				voxelColor: voxelColor,
				sprites: out byte[][] sprites,
				widths: out int[] widths,
				origins: out int[][] origins);
			packingRectangles = Enumerable.Range(0, sprites.Length)
				.Select(i => new PackingRectangle(
					x: 0,
					y: 0,
					width: (uint)(widths[i] + 2),
					height: (uint)(PixelDraw.Height(sprites[i].Length, widths[i]) + 2),
					id: i))
				.ToArray();
			RectanglePacker.Pack(packingRectangles, out PackingRectangle bounds, PackingHints.TryByBiggerSide);
			width = (int)PixelDraw.NextPowerOf2(bounds.BiggerSide);
			byte[] texture = new byte[width * 4 * width];
			foreach (PackingRectangle packingRectangle in packingRectangles)
				texture.DrawInsert(
					x: (int)(packingRectangle.X + 1),
					y: (int)(packingRectangle.Y + 1),
					insert: sprites[packingRectangle.Id],
					insertWidth: widths[packingRectangle.Id],
					width: width);
			return texture;
		}
		public static void IsoSprites(IModel model, IVoxelColor voxelColor, out byte[][] sprites, out int[] widths, out int[][] origins, int originX = -1, int originY = -1, int originZ = -1)
		{
			if (originX < 0)
				originX = model.SizeX >> 1;
			if (originY < 0)
				originY = model.SizeY >> 1;
			if (originZ < 0)
				originZ = 0;
			sprites = new byte[8][];
			widths = new int[sprites.Length];
			origins = new int[sprites.Length][];
			TurnModel turnModel = new TurnModel
			{
				Model = model,
			};
			for (int i = 0; i < sprites.Length; i += 2)
			{
				turnModel.Rotate(
					x: out int turnedX,
					y: out int turnedY,
					z: out int turnedZ,
					originX,
					originY,
					originZ);
				int width = VoxelDraw.AboveWidth(turnModel) * 5;
				ArrayRenderer arrayRenderer = new ArrayRenderer
				{
					Image = new byte[width * 4 * VoxelDraw.AboveHeight(turnModel) * 4],
					Width = width,
					VoxelColor = voxelColor,
				};
				OffsetRenderer offsetRenderer = new OffsetRenderer
				{
					RectangleRenderer = arrayRenderer,
					VoxelColor = voxelColor,
					ScaleX = 5,
					ScaleY = 4,
				};
				VoxelDraw.Above(
					model: turnModel,
					renderer: offsetRenderer);
				sprites[i] = arrayRenderer.Image
					.TransparentCropPlusOne(
						cutTop: out int cutTop,
						cutLeft: out int cutLeft,
						croppedWidth: out widths[i],
						croppedHeight: out _,
						width: width)
					.Outline(widths[i])
					.Draw3x4(
						@string: string.Join(",", turnedX, turnedY, turnedZ),
						width: widths[i]);
				origins[i] = new int[2];
				VoxelDraw.AboveLocate(
					pixelX: out origins[i][0],
					pixelY: out origins[i][1],
					model: turnModel,
					voxelX: turnedX,
					voxelY: turnedY,
					voxelZ: turnedZ);
				origins[i][0] *= offsetRenderer.ScaleX;
				origins[i][1] *= offsetRenderer.ScaleY;
				origins[i][0] -= cutLeft - 2;
				origins[i][1] -= cutTop;
				width = VoxelDraw.IsoWidth(turnModel) << 1;
				arrayRenderer = new Array2xRenderer
				{
					Image = new byte[width * 4 * VoxelDraw.IsoHeight(turnModel)],
					Width = width,
					VoxelColor = voxelColor,
				};
				VoxelDraw.Iso(
					model: turnModel,
					renderer: arrayRenderer);
				sprites[i + 1] = arrayRenderer.Image
					.TransparentCropPlusOne(
						cutTop: out cutTop,
						cutLeft: out cutLeft,
						croppedWidth: out widths[i + 1],
						croppedHeight: out _,
						width: width)
					.Outline(widths[i + 1])
					.Draw3x4(
						@string: string.Join(",", turnedX, turnedY, turnedZ),
						width: widths[i + 1]);
				origins[i + 1] = new int[2];
				VoxelDraw.IsoLocate(
					pixelX: out origins[i + 1][0],
					pixelY: out origins[i + 1][1],
					model: turnModel,
					voxelX: turnedX,
					voxelY: turnedY,
					voxelZ: turnedZ);
				origins[i + 1][0] <<= 1;
				origins[i + 1][0] -= cutLeft;
				origins[i + 1][1] -= cutTop - 5;
				turnModel.CounterZ();
			}
			for (int i = 0; i < sprites.Length; i++)
				sprites[i].DrawPixel(
					color: 0xFFFFFFFF,
					x: origins[i][0],
					y: origins[i][1],
					width: widths[i]);
		}
		#region Above4
		public static void Above4Raw(IModel model, IVoxelColor voxelColor, out byte[][] sprites, out int[] widths, out int[][] pixelOrigins, params int[] voxelOrigin)
		{
			if (voxelOrigin is null || voxelOrigin.Length < 3)
				voxelOrigin = new int[] { -1, -1, -1 };
			if (voxelOrigin[0] < 0)
				voxelOrigin[0] = model.SizeX >> 1;
			if (voxelOrigin[1] < 0)
				voxelOrigin[1] = model.SizeY >> 1;
			if (voxelOrigin[2] < 0)
				voxelOrigin[2] = 0;
			sprites = new byte[4][];
			widths = new int[sprites.Length];
			pixelOrigins = new int[sprites.Length][];
			TurnModel turnModel = new TurnModel
			{
				Model = model,
			};
			for (int i = 0; i < sprites.Length; i++)
			{
				int width = VoxelDraw.AboveWidth(turnModel);
				ArrayRenderer arrayRenderer = new ArrayRenderer
				{
					Image = new byte[width * 4 * VoxelDraw.AboveHeight(turnModel)],
					Width = width,
					VoxelColor = voxelColor,
				};
				VoxelDraw.Above(
					model: turnModel,
					renderer: arrayRenderer);
				sprites[i] = arrayRenderer.Image;
				widths[i] = width;
				turnModel.ReverseRotate(
					x: out int turnedX,
					y: out int turnedY,
					z: out int turnedZ,
					coordinates: voxelOrigin);
				pixelOrigins[i] = new int[2];
				VoxelDraw.AboveLocate(
					pixelX: out pixelOrigins[i][0],
					pixelY: out pixelOrigins[i][1],
					model: turnModel,
					voxelX: turnedX,
					voxelY: turnedY,
					voxelZ: turnedZ);
				turnModel.ClockZ();
			}
		}
		public static void Above4(IModel model, IVoxelColor voxelColor, out byte[][] sprites, out int[] widths, out int[][] pixelOrigins, params int[] voxelOrigin)
		{
			Above4Raw(
				model: model,
				voxelColor: voxelColor,
				sprites: out byte[][] rawSprites,
				widths: out int[] rawWidths,
				pixelOrigins: out int[][] rawPixelOrigins,
				voxelOrigin: voxelOrigin);
			byte[][] upscaledSprites = rawSprites.UpscaleSprites(
				widths: rawWidths,
				pixelOrigins: rawPixelOrigins,
				xFactor: 5,
				yFactor: 4,
				newWidths: out int[] upscaledWidths,
				newPixelOrigins: out int[][] upscaledPixelOrigins);
			upscaledSprites.CropSprites(
				widths: upscaledWidths,
				pixelOrigins: upscaledPixelOrigins,
				newSprites: out sprites,
				newWidths: out widths,
				newPixelOrigins: out pixelOrigins);
		}
		public static void Above4Outlined(IModel model, IVoxelColor voxelColor, out byte[][] sprites, out int[] widths, out int[][] pixelOrigins, params int[] voxelOrigin)
		{
			Above4Raw(
				model: model,
				voxelColor: voxelColor,
				sprites: out byte[][] rawSprites,
				widths: out int[] rawWidths,
				pixelOrigins: out int[][] rawPixelOrigins,
				voxelOrigin: voxelOrigin);
			byte[][] upscaledSprites = rawSprites.UpscaleSprites(
				widths: rawWidths,
				pixelOrigins: rawPixelOrigins,
				xFactor: 5,
				yFactor: 4,
				newWidths: out int[] upscaledWidths,
				newPixelOrigins: out int[][] upscaledPixelOrigins);
			upscaledSprites.CropOutlineSprites(
				widths: upscaledWidths,
				pixelOrigins: upscaledPixelOrigins,
				newSprites: out sprites,
				newWidths: out widths,
				newPixelOrigins: out pixelOrigins);
		}
		#endregion Above4
		#region Iso4
		public static void Iso4Raw(IModel model, IVoxelColor voxelColor, out byte[][] sprites, out int[] widths, out int[][] pixelOrigins, params int[] voxelOrigin)
		{
			if (voxelOrigin is null || voxelOrigin.Length < 3)
				voxelOrigin = new int[] { -1, -1, -1 };
			if (voxelOrigin[0] < 0)
				voxelOrigin[0] = model.SizeX >> 1;
			if (voxelOrigin[1] < 0)
				voxelOrigin[1] = model.SizeY >> 1;
			if (voxelOrigin[2] < 0)
				voxelOrigin[2] = 0;
			sprites = new byte[4][];
			widths = new int[sprites.Length];
			pixelOrigins = new int[sprites.Length][];
			TurnModel turnModel = new TurnModel
			{
				Model = model,
			};
			for (int i = 0; i < sprites.Length; i++)
			{
				int width = VoxelDraw.IsoWidth(turnModel) << 1;//*2
				Array2xRenderer arrayRenderer = new Array2xRenderer
				{
					Image = new byte[width * 4 * VoxelDraw.IsoHeight(turnModel)],
					Width = width,
					VoxelColor = voxelColor,
				};
				VoxelDraw.Iso(
					model: turnModel,
					renderer: arrayRenderer);
				sprites[i] = arrayRenderer.Image;
				widths[i] = width;
				turnModel.ReverseRotate(
					x: out int turnedX,
					y: out int turnedY,
					z: out int turnedZ,
					coordinates: voxelOrigin);
				pixelOrigins[i] = new int[2];
				VoxelDraw.IsoLocate(
					pixelX: out pixelOrigins[i][0],
					pixelY: out pixelOrigins[i][1],
					model: turnModel,
					voxelX: turnedX,
					voxelY: turnedY,
					voxelZ: turnedZ);
				pixelOrigins[i][0] <<= 1;//*=2
				turnModel.ClockZ();
			}
		}
		public static void Iso4(IModel model, IVoxelColor voxelColor, out byte[][] sprites, out int[] widths, out int[][] pixelOrigins, params int[] voxelOrigin)
		{
			Iso4Raw(
				model: model,
				voxelColor: voxelColor,
				sprites: out byte[][] rawSprites,
				widths: out int[] rawWidths,
				pixelOrigins: out int[][] rawPixelOrigins,
				voxelOrigin: voxelOrigin);
			rawSprites.CropSprites(
				widths: rawWidths,
				pixelOrigins: rawPixelOrigins,
				newSprites: out sprites,
				newWidths: out widths,
				newPixelOrigins: out pixelOrigins);
		}
		public static void Iso4Outlined(IModel model, IVoxelColor voxelColor, out byte[][] sprites, out int[] widths, out int[][] pixelOrigins, params int[] voxelOrigin)
		{
			Iso4Raw(
				model: model,
				voxelColor: voxelColor,
				sprites: out byte[][] rawSprites,
				widths: out int[] rawWidths,
				pixelOrigins: out int[][] rawPixelOrigins,
				voxelOrigin: voxelOrigin);
			rawSprites.CropOutlineSprites(
				widths: rawWidths,
				pixelOrigins: rawPixelOrigins,
				newSprites: out sprites,
				newWidths: out widths,
				newPixelOrigins: out pixelOrigins);
		}
		#endregion Iso4
		#region Iso8
		/// <summary>
		/// Combines arrays by alternating one item from each
		/// </summary>
		/// <param name="list">All arrays after list[0] must be sized equal to or greater than list[0]</param>
		/// <returns>Array of size list[0].Length * list.Length</returns>
		public static T[] CombineArrays<T>(params T[][] list)
		{
			T[] result = new T[list[0].Length * list.Length];
			for (int x = 0, z = 0; x < list[0].Length; x++, z += list.Length)
				for (int y = 0; y < list.Length; y++)
					result[z + y] = list[y][x];
			return result;
		}
		public static void Iso8(IModel model, IVoxelColor voxelColor, out byte[][] sprites, out int[] widths, out int[][] pixelOrigins, params int[] voxelOrigin)
		{
			Above4(
				model: model,
				voxelColor: voxelColor,
				sprites: out byte[][] aboveSprites,
				widths: out int[] aboveWidths,
				pixelOrigins: out int[][] abovePixelOrigins,
				voxelOrigin: voxelOrigin);
			Iso4(
				model: model,
				voxelColor: voxelColor,
				sprites: out byte[][] isoSprites,
				widths: out int[] isoWidths,
				pixelOrigins: out int[][] isoPixelOrigins,
				voxelOrigin: voxelOrigin); ;
			sprites = CombineArrays(aboveSprites, isoSprites);
			widths = CombineArrays(aboveWidths, isoWidths);
			pixelOrigins = CombineArrays(abovePixelOrigins, isoPixelOrigins);
		}
		public static void Iso8Outlined(IModel model, IVoxelColor voxelColor, out byte[][] sprites, out int[] widths, out int[][] pixelOrigins, params int[] voxelOrigin)
		{
			Above4Outlined(
				model: model,
				voxelColor: voxelColor,
				sprites: out byte[][] aboveSprites,
				widths: out int[] aboveWidths,
				pixelOrigins: out int[][] abovePixelOrigins,
				voxelOrigin: voxelOrigin);
			Iso4Outlined(
				model: model,
				voxelColor: voxelColor,
				sprites: out byte[][] isoSprites,
				widths: out int[] isoWidths,
				pixelOrigins: out int[][] isoPixelOrigins,
				voxelOrigin: voxelOrigin);
			sprites = CombineArrays(aboveSprites, isoSprites);
			widths = CombineArrays(aboveWidths, isoWidths);
			pixelOrigins = CombineArrays(abovePixelOrigins, isoPixelOrigins);
		}
		#endregion Iso8
	}
}
