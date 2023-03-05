using RectpackSharp;
using System.Linq;
using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Model;
using Voxel2Pixel.Render;

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
				widths: out int[] widths);
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
		public static void IsoSprites(IModel model, IVoxelColor voxelColor, out byte[][] sprites, out int[] widths)
		{
			sprites = new byte[8][];
			widths = new int[sprites.Length];
			TurnModel turnModel = new TurnModel
			{
				Model = model,
			};
			for (int i = 0; i < sprites.Length; i += 2)
			{
				int width = VoxelDraw.AboveWidth(turnModel);
				ArrayRenderer arrayRenderer = new ArrayRenderer
				{
					Image = new byte[width * 4 * VoxelDraw.AboveHeight(turnModel)],
					Width = width,
					IVoxelColor = voxelColor,
				};
				VoxelDraw.Above(
					model: turnModel,
					renderer: arrayRenderer);
				sprites[i] = arrayRenderer.Image
					.TransparentCropPlusOne(
						cutTop: out _,
						cutLeft: out _,
						croppedWidth: out widths[i],
						croppedHeight: out _,
						width: width)
					.Outline(
						width: widths[i],
						color: 0x000000FF);
				width = VoxelDraw.IsoWidth(turnModel);
				arrayRenderer = new ArrayRenderer
				{
					Image = new byte[width * 4 * VoxelDraw.IsoHeight(turnModel)],
					Width = width,
					IVoxelColor = voxelColor,
				};
				VoxelDraw.Iso(
					model: turnModel,
					renderer: arrayRenderer);
				sprites[i + 1] = arrayRenderer.Image
					.TransparentCropPlusOne(
						cutTop: out _,
						cutLeft: out _,
						croppedWidth: out widths[i + 1],
						croppedHeight: out _,
						width: width)
					.Outline(
						width: widths[i + 1],
						color: 0x000000FF);
				turnModel.CounterZ();
			}
		}
	}
}
