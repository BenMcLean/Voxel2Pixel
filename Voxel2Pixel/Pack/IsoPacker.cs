using RectpackSharp;
using System.Linq;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;
using Voxel2Pixel.Render;
using static Voxel2Pixel.Draw.PixelDraw;

namespace Voxel2Pixel.Pack
{
	public static class IsoPacker
	{
		public static byte[] Pack8(IModel model, IVoxelColor voxelColor, out int width, out RectpackSharp.PackingRectangle[] packingRectangles)
		{
			byte[][] sprites = Iso8(
				model: model,
				voxelColor: voxelColor,
				widths: out int[] widths,
				pixelOrigins: out int[][] origins);
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
		#region Above4
		public static byte[][] Above4Raw(IModel model, IVoxelColor voxelColor, out int[] widths, out int[][] pixelOrigins, params int[] voxelOrigin)
		{
			if (voxelOrigin is null || voxelOrigin.Length < 3)
				voxelOrigin = new int[] { -1, -1, -1 };
			if (voxelOrigin[0] < 0)
				voxelOrigin[0] = model.SizeX >> 1;
			if (voxelOrigin[1] < 0)
				voxelOrigin[1] = model.SizeY >> 1;
			if (voxelOrigin[2] < 0)
				voxelOrigin[2] = 0;
			byte[][] sprites = new byte[4][];
			widths = new int[sprites.Length];
			pixelOrigins = new int[sprites.Length][];
			TurnModel turnModel = new TurnModel
			{
				Model = model,
			};
			for (int i = 0; i < sprites.Length; i++)
			{
				ushort width = VoxelDraw.AboveWidth(turnModel);
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
					x: out ushort turnedX,
					y: out ushort turnedY,
					z: out ushort turnedZ,
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
			return sprites;
		}
		public static byte[][] Above4(IModel model, IVoxelColor voxelColor, out int[] widths, out int[][] pixelOrigins, params int[] voxelOrigin)
		{
			byte[][] rawSprites = Above4Raw(
					model: model,
					voxelColor: voxelColor,
					widths: out int[] rawWidths,
					pixelOrigins: out int[][] rawPixelOrigins,
					voxelOrigin: voxelOrigin),
				upscaledSprites = rawSprites.UpscaleSprites(
					widths: rawWidths,
					pixelOrigins: rawPixelOrigins,
					xFactor: 5,
					yFactor: 4,
					newWidths: out int[] upscaledWidths,
					newPixelOrigins: out int[][] upscaledPixelOrigins);
			return upscaledSprites.CropSprites(
				widths: upscaledWidths,
				pixelOrigins: upscaledPixelOrigins.Select(e => new int[] { e[0] + 2, e[1] + 3 }).ToArray(),
				newWidths: out widths,
				newPixelOrigins: out pixelOrigins);
		}
		public static byte[][] Above4Outlined(IModel model, IVoxelColor voxelColor, out int[] widths, out int[][] pixelOrigins, params int[] voxelOrigin)
		{
			byte[][] rawSprites = Above4Raw(
					model: model,
					voxelColor: voxelColor,
					widths: out int[] rawWidths,
					pixelOrigins: out int[][] rawPixelOrigins,
					voxelOrigin: voxelOrigin),
				upscaledSprites = rawSprites.UpscaleSprites(
					widths: rawWidths,
					pixelOrigins: rawPixelOrigins,
					xFactor: 5,
					yFactor: 4,
					newWidths: out int[] upscaledWidths,
					newPixelOrigins: out int[][] upscaledPixelOrigins);
			return upscaledSprites.CropOutlineSprites(
				widths: upscaledWidths,
				pixelOrigins: upscaledPixelOrigins.Select(e => new int[] { e[0] + 2, e[1] + 3 }).ToArray(),
				newWidths: out widths,
				newPixelOrigins: out pixelOrigins);
		}
		#endregion Above4
		#region Iso4
		public static byte[][] Iso4Raw(IModel model, IVoxelColor voxelColor, out int[] widths, out int[][] pixelOrigins, params int[] voxelOrigin)
		{
			if (voxelOrigin is null || voxelOrigin.Length < 3)
				voxelOrigin = new int[] { -1, -1, -1 };
			if (voxelOrigin[0] < 0)
				voxelOrigin[0] = model.SizeX >> 1;
			if (voxelOrigin[1] < 0)
				voxelOrigin[1] = model.SizeY >> 1;
			if (voxelOrigin[2] < 0)
				voxelOrigin[2] = 0;
			byte[][] sprites = new byte[4][];
			widths = new int[sprites.Length];
			pixelOrigins = new int[sprites.Length][];
			TurnModel turnModel = new TurnModel
			{
				Model = model,
			};
			for (int i = 0; i < sprites.Length; i++)
			{
				int width = (int)VoxelDraw.IsoWidth(turnModel) << 1;//*2
				Array2xRenderer arrayRenderer = new Array2xRenderer
				{
					Image = new byte[width * 4 * VoxelDraw.IsoHeight(turnModel)],
					Width = (ushort)width,
					VoxelColor = voxelColor,
				};
				VoxelDraw.Iso(
					model: turnModel,
					renderer: arrayRenderer);
				sprites[i] = arrayRenderer.Image;
				widths[i] = width;
				turnModel.ReverseRotate(
					x: out ushort turnedX,
					y: out ushort turnedY,
					z: out ushort turnedZ,
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
			return sprites;
		}
		public static byte[][] Iso4(IModel model, IVoxelColor voxelColor, out int[] widths, out int[][] pixelOrigins, params int[] voxelOrigin)
		{
			byte[][] rawSprites = Iso4Raw(
				model: model,
				voxelColor: voxelColor,
				widths: out int[] rawWidths,
				pixelOrigins: out int[][] rawPixelOrigins,
				voxelOrigin: voxelOrigin);
			return rawSprites.CropSprites(
				widths: rawWidths,
				pixelOrigins: rawPixelOrigins,
				newWidths: out widths,
				newPixelOrigins: out pixelOrigins);
		}
		public static byte[][] Iso4Outlined(IModel model, IVoxelColor voxelColor, out int[] widths, out int[][] pixelOrigins, params int[] voxelOrigin)
		{
			byte[][] rawSprites = Iso4Raw(
				model: model,
				voxelColor: voxelColor,
				widths: out int[] rawWidths,
				pixelOrigins: out int[][] rawPixelOrigins,
				voxelOrigin: voxelOrigin);
			return rawSprites.CropOutlineSprites(
				widths: rawWidths,
				pixelOrigins: rawPixelOrigins,
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
		public static byte[][] Iso8(this IModel[] models, IVoxelColor voxelColor, out int[] widths, out int[][] pixelOrigins, params int[][] voxelOrigins) => Iso8(
			models: models,
			voxelColors: Enumerable.Range(0, models.Length).Select(e => voxelColor).ToArray(),
			widths: out widths,
			pixelOrigins: out pixelOrigins,
			voxelOrigins: voxelOrigins);
		public static byte[][] Iso8(this IModel[] models, IVoxelColor[] voxelColors, out int[] widths, out int[][] pixelOrigins, params int[][] voxelOrigins)
		{
			if (voxelOrigins is null)
				voxelOrigins = new int[models.Length][];
			byte[][][] sprites = new byte[models.Length][][];
			int[][] widths2 = new int[sprites.Length][];
			int[][][] pixelOrigins2 = new int[sprites.Length][][];
			for (int model = 0; model < models.Length; model++)
				sprites[model] = Iso8(
					model: models[model],
					voxelColor: voxelColors[model],
					widths: out widths2[model],
					pixelOrigins: out pixelOrigins2[model],
					voxelOrigin: voxelOrigins[model]);
			widths = CombineArrays(widths2);
			pixelOrigins = CombineArrays(pixelOrigins2);
			return CombineArrays(sprites);
		}
		public static byte[][] Iso8(this IModel model, IVoxelColor voxelColor, out int[] widths, out int[][] pixelOrigins, params int[] voxelOrigin)
		{
			byte[][] aboveSprites = Above4(
					model: model,
					voxelColor: voxelColor,
					widths: out int[] aboveWidths,
					pixelOrigins: out int[][] abovePixelOrigins,
					voxelOrigin: voxelOrigin),
				isoSprites = Iso4(
					model: model,
					voxelColor: voxelColor,
					widths: out int[] isoWidths,
					pixelOrigins: out int[][] isoPixelOrigins,
					voxelOrigin: voxelOrigin);
			widths = CombineArrays(aboveWidths, isoWidths);
			pixelOrigins = CombineArrays(abovePixelOrigins, isoPixelOrigins);
			return CombineArrays(aboveSprites, isoSprites);
		}
		public static byte[][] Iso8Outlined(this IModel[] models, IVoxelColor[] voxelColors, out int[] widths, out int[][] pixelOrigins, params int[][] voxelOrigins)
		{
			if (voxelOrigins is null)
				voxelOrigins = new int[models.Length][];
			byte[][][] sprites = new byte[models.Length][][];
			int[][] widths2 = new int[sprites.Length][];
			int[][][] pixelOrigins2 = new int[sprites.Length][][];
			for (int model = 0; model < models.Length; model++)
				sprites[model] = Iso8Outlined(
					model: models[model],
					voxelColor: voxelColors[model],
					widths: out widths2[model],
					pixelOrigins: out pixelOrigins2[model],
					voxelOrigin: voxelOrigins[model]);
			widths = CombineArrays(widths2);
			pixelOrigins = CombineArrays(pixelOrigins2);
			return CombineArrays(sprites);
		}
		public static byte[][] Iso8Outlined(this IModel model, IVoxelColor voxelColor, out int[] widths, out int[][] pixelOrigins, params int[] voxelOrigin)
		{
			byte[][] aboveSprites = Above4Outlined(
					model: model,
					voxelColor: voxelColor,
					widths: out int[] aboveWidths,
					pixelOrigins: out int[][] abovePixelOrigins,
					voxelOrigin: voxelOrigin),
				isoSprites = Iso4Outlined(
					model: model,
					voxelColor: voxelColor,
					widths: out int[] isoWidths,
					pixelOrigins: out int[][] isoPixelOrigins,
					voxelOrigin: voxelOrigin);
			widths = CombineArrays(aboveWidths, isoWidths);
			pixelOrigins = CombineArrays(abovePixelOrigins, isoPixelOrigins);
			return CombineArrays(aboveSprites, isoSprites);
		}
		public static int[][] Iso8SouthWestPixelOrigins(this int[][] pixelOrigins) =>
		new int[][] {
			new int[] { pixelOrigins[0][0] - 2, pixelOrigins[0][1] },
			new int[] { pixelOrigins[1][0], pixelOrigins[1][1] },
			new int[] { pixelOrigins[2][0] + 2, pixelOrigins[2][1] },
			new int[] { pixelOrigins[3][0] + 3, pixelOrigins[3][1] - 1 },
			new int[] { pixelOrigins[4][0] + 2, pixelOrigins[4][1] - 3 },
			new int[] { pixelOrigins[5][0] - 1, pixelOrigins[5][1] - 2 },
			new int[] { pixelOrigins[6][0] - 2, pixelOrigins[6][1] - 3 },
			new int[] { pixelOrigins[7][0] - 4, pixelOrigins[7][1] - 1 }
		};
		#endregion Iso8
	}
}
