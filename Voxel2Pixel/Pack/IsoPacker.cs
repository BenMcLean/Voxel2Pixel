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
		public static byte[] Pack8(IModel model, IVoxelColor voxelColor, out ushort width, out RectpackSharp.PackingRectangle[] packingRectangles)
		{
			byte[][] sprites = Iso8(
				model: model,
				voxelColor: voxelColor,
				widths: out ushort[] widths,
				pixelOrigins: out ushort[][] origins);
			packingRectangles = Enumerable.Range(0, sprites.Length)
				.Select(i => new PackingRectangle(
					x: 0,
					y: 0,
					width: (ushort)(widths[i] + 2),
					height: (ushort)(PixelDraw.Height(sprites[i].Length, widths[i]) + 2),
					id: i))
				.ToArray();
			RectanglePacker.Pack(packingRectangles, out PackingRectangle bounds, PackingHints.TryByBiggerSide);
			width = (ushort)PixelDraw.NextPowerOf2(bounds.BiggerSide);
			byte[] texture = new byte[width * 4 * width];
			foreach (PackingRectangle packingRectangle in packingRectangles)
				texture.DrawInsert(
					x: (ushort)(packingRectangle.X + 1),
					y: (ushort)(packingRectangle.Y + 1),
					insert: sprites[packingRectangle.Id],
					insertWidth: widths[packingRectangle.Id],
					width: width);
			return texture;
		}
		#region Above4
		public static byte[][] Above4Raw(IModel model, IVoxelColor voxelColor, out ushort[] widths, out ushort[][] pixelOrigins, params ushort[] voxelOrigin)
		{
			if (voxelOrigin is null || voxelOrigin.Length < 3)
				voxelOrigin = new ushort[3];
			byte[][] sprites = new byte[4][];
			widths = new ushort[sprites.Length];
			pixelOrigins = new ushort[sprites.Length][];
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
				VoxelDraw.AboveLocate(
					pixelX: out int locateX,
					pixelY: out int locateY,
					model: turnModel,
					voxelX: turnedX,
					voxelY: turnedY,
					voxelZ: turnedZ);
				pixelOrigins[i] = new ushort[2] { (ushort)locateX, (ushort)locateY };
				turnModel.ClockZ();
			}
			return sprites;
		}
		public static byte[][] Above4(IModel model, IVoxelColor voxelColor, out ushort[] widths, out ushort[][] pixelOrigins, params ushort[] voxelOrigin)
		{
			byte[][] rawSprites = Above4Raw(
					model: model,
					voxelColor: voxelColor,
					widths: out ushort[] rawWidths,
					pixelOrigins: out ushort[][] rawPixelOrigins,
					voxelOrigin: voxelOrigin),
				upscaledSprites = rawSprites.UpscaleSprites(
					widths: rawWidths,
					pixelOrigins: rawPixelOrigins,
					factorX: 5,
					factorY: 4,
					newWidths: out ushort[] upscaledWidths,
					newPixelOrigins: out ushort[][] upscaledPixelOrigins);
			return upscaledSprites.CropSprites(
				widths: upscaledWidths,
				pixelOrigins: upscaledPixelOrigins.Select(e => new ushort[] { (ushort)(e[0] + 2), (ushort)(e[1] + 3) }).ToArray(),
				newWidths: out widths,
				newPixelOrigins: out pixelOrigins);
		}
		public static byte[][] Above4Outlined(IModel model, IVoxelColor voxelColor, out ushort[] widths, out ushort[][] pixelOrigins, params ushort[] voxelOrigin)
		{
			byte[][] rawSprites = Above4Raw(
					model: model,
					voxelColor: voxelColor,
					widths: out ushort[] rawWidths,
					pixelOrigins: out ushort[][] rawPixelOrigins,
					voxelOrigin: voxelOrigin),
				upscaledSprites = rawSprites.UpscaleSprites(
					widths: rawWidths,
					pixelOrigins: rawPixelOrigins,
					factorX: 5,
					factorY: 4,
					newWidths: out ushort[] upscaledWidths,
					newPixelOrigins: out ushort[][] upscaledPixelOrigins);
			return upscaledSprites.CropOutlineSprites(
				widths: upscaledWidths,
				pixelOrigins: upscaledPixelOrigins.Select(e => new ushort[] { (ushort)(e[0] + 2), (ushort)(e[1] + 3) }).ToArray(),
				newWidths: out widths,
				newPixelOrigins: out pixelOrigins);
		}
		#endregion Above4
		#region Iso4
		public static byte[][] Iso4Raw(IModel model, IVoxelColor voxelColor, out ushort[] widths, out ushort[][] pixelOrigins, params ushort[] voxelOrigin)
		{
			if (voxelOrigin is null || voxelOrigin.Length < 3)
				voxelOrigin = new ushort[3];
			byte[][] sprites = new byte[4][];
			widths = new ushort[sprites.Length];
			pixelOrigins = new ushort[sprites.Length][];
			TurnModel turnModel = new TurnModel
			{
				Model = model,
			};
			for (int i = 0; i < sprites.Length; i++)
			{
				ushort width = (ushort)(VoxelDraw.IsoWidth(turnModel) << 1);//*2
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
				pixelOrigins[i] = new ushort[2] { (ushort)(locateX << 1), (ushort)locateY };
				turnModel.ClockZ();
			}
			return sprites;
		}
		public static byte[][] Iso4(IModel model, IVoxelColor voxelColor, out ushort[] widths, out ushort[][] pixelOrigins, params ushort[] voxelOrigin)
		{
			byte[][] rawSprites = Iso4Raw(
				model: model,
				voxelColor: voxelColor,
				widths: out ushort[] rawWidths,
				pixelOrigins: out ushort[][] rawPixelOrigins,
				voxelOrigin: voxelOrigin);
			return rawSprites.CropSprites(
				widths: rawWidths,
				pixelOrigins: rawPixelOrigins,
				newWidths: out widths,
				newPixelOrigins: out pixelOrigins);
		}
		public static byte[][] Iso4Outlined(IModel model, IVoxelColor voxelColor, out ushort[] widths, out ushort[][] pixelOrigins, params ushort[] voxelOrigin)
		{
			byte[][] rawSprites = Iso4Raw(
				model: model,
				voxelColor: voxelColor,
				widths: out ushort[] rawWidths,
				pixelOrigins: out ushort[][] rawPixelOrigins,
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
		public static byte[][] Iso8(this IModel[] models, IVoxelColor voxelColor, out ushort[] widths, out ushort[][] pixelOrigins, params ushort[][] voxelOrigins) => Iso8(
			models: models,
			voxelColors: Enumerable.Range(0, models.Length).Select(e => voxelColor).ToArray(),
			widths: out widths,
			pixelOrigins: out pixelOrigins,
			voxelOrigins: voxelOrigins);
		public static byte[][] Iso8(this IModel[] models, IVoxelColor[] voxelColors, out ushort[] widths, out ushort[][] pixelOrigins, params ushort[][] voxelOrigins)
		{
			if (voxelOrigins is null)
				voxelOrigins = new ushort[models.Length][];
			byte[][][] sprites = new byte[models.Length][][];
			ushort[][] widths2 = new ushort[sprites.Length][];
			ushort[][][] pixelOrigins2 = new ushort[sprites.Length][][];
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
		public static byte[][] Iso8(this IModel model, IVoxelColor voxelColor, out ushort[] widths, out ushort[][] pixelOrigins, params ushort[] voxelOrigin)
		{
			byte[][] aboveSprites = Above4(
					model: model,
					voxelColor: voxelColor,
					widths: out ushort[] aboveWidths,
					pixelOrigins: out ushort[][] abovePixelOrigins,
					voxelOrigin: voxelOrigin),
				isoSprites = Iso4(
					model: model,
					voxelColor: voxelColor,
					widths: out ushort[] isoWidths,
					pixelOrigins: out ushort[][] isoPixelOrigins,
					voxelOrigin: voxelOrigin);
			widths = CombineArrays(aboveWidths, isoWidths);
			pixelOrigins = CombineArrays(abovePixelOrigins, isoPixelOrigins);
			return CombineArrays(aboveSprites, isoSprites);
		}
		public static byte[][] Iso8Outlined(this IModel[] models, IVoxelColor[] voxelColors, out ushort[] widths, out ushort[][] pixelOrigins, params ushort[][] voxelOrigins)
		{
			if (voxelOrigins is null)
				voxelOrigins = new ushort[models.Length][];
			byte[][][] sprites = new byte[models.Length][][];
			ushort[][] widths2 = new ushort[sprites.Length][];
			ushort[][][] pixelOrigins2 = new ushort[sprites.Length][][];
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
		public static byte[][] Iso8Outlined(this IModel model, IVoxelColor voxelColor, out ushort[] widths, out ushort[][] pixelOrigins, params ushort[] voxelOrigin)
		{
			byte[][] aboveSprites = Above4Outlined(
					model: model,
					voxelColor: voxelColor,
					widths: out ushort[] aboveWidths,
					pixelOrigins: out ushort[][] abovePixelOrigins,
					voxelOrigin: voxelOrigin),
				isoSprites = Iso4Outlined(
					model: model,
					voxelColor: voxelColor,
					widths: out ushort[] isoWidths,
					pixelOrigins: out ushort[][] isoPixelOrigins,
					voxelOrigin: voxelOrigin);
			widths = CombineArrays(aboveWidths, isoWidths);
			pixelOrigins = CombineArrays(abovePixelOrigins, isoPixelOrigins);
			return CombineArrays(aboveSprites, isoSprites);
		}
		public static ushort[][] Iso8SouthWestPixelOrigins(this ushort[][] pixelOrigins) =>
		new ushort[][] {
			new ushort[] { (ushort)(pixelOrigins[0][0] - 2), pixelOrigins[0][1] },
			new ushort[] { pixelOrigins[1][0], pixelOrigins[1][1] },
			new ushort[] { (ushort)(pixelOrigins[2][0] + 2), pixelOrigins[2][1] },
			new ushort[] { (ushort)(pixelOrigins[3][0] + 3), (ushort)(pixelOrigins[3][1] - 1) },
			new ushort[] { (ushort)(pixelOrigins[4][0] + 2), (ushort)(pixelOrigins[4][1] - 3) },
			new ushort[] { (ushort)(pixelOrigins[5][0] - 1), (ushort)(pixelOrigins[5][1] - 2) },
			new ushort[] { (ushort)(pixelOrigins[6][0] - 2), (ushort)(pixelOrigins[6][1] - 3) },
			new ushort[] { (ushort)(pixelOrigins[7][0] - 4), (ushort)(pixelOrigins[7][1] - 1) }
		};
		#endregion Iso8
	}
}
