using Voxel2Pixel.Model;
using Voxel2Pixel.Render;

namespace Voxel2Pixel.Draw
{
	public static class VoxelDraw
	{
		public static void Draw(IModel model, IRectangleRenderer renderer) => DrawRight(model, renderer);
		public static void DrawRight(IModel model, IRectangleRenderer renderer)
		{
			for (int z = 0; z < model.SizeZ; z++)
				for (int y = 0; y < model.SizeY; y++)
					for (int x = 0; x < model.SizeX; x++)
						if (model.At(x, y, z) is byte voxel
							&& voxel != 0)
						{
							renderer.RectRight(
								x: y,
								y: z,
								voxel: voxel);
							break;
						}
		}
		public static void DrawRightPeek(IModel model, IRectangleRenderer renderer) =>
			DrawRightPeek(
				model: model,
				renderer: renderer,
				scaleX: 6,
				scaleY: 6);
		public static void DrawRightPeek(IModel model, IRectangleRenderer renderer, int scaleX, int scaleY)
		{
			for (int z = 0; z < model.SizeZ; z++)
				for (int y = 0; y < model.SizeY; y++)
					for (int x = 0; x < model.SizeX; x++)
						if (model.At(x, y, z) is byte voxel
							&& voxel != 0)
						{
							renderer.RectRight(
								x: y * scaleX + 1,
								y: z * scaleY,
								voxel: voxel,
								sizeX: scaleX,
								sizeY: scaleY - 1);
							if (z >= model.SizeZ - 1 || model.At(x, y, z + 1) == 0)
								renderer.RectVertical(
									x: y * scaleX,
									y: (z + 1) * scaleY - 1,
									voxel: voxel,
									sizeX: scaleX,
									sizeY: 1);
							break;
						}
		}
		public static void DrawLeft(IModel model, IRectangleRenderer renderer)
		{
			for (int z = 0; z < model.SizeZ; z++)
				for (int y = 0; y < model.SizeY; y++)
					for (int x = model.SizeX - 1; x >= 0; x++)
						if (model.At(x, y, z) is byte voxel
							&& voxel != 0)
						{
							renderer.RectLeft(
								x: y,
								y: z,
								voxel: voxel);
							break;
						}
		}
		public static void DrawLeftPeek(IModel model, IRectangleRenderer renderer, int scaleX, int scaleY)
		{
			for (int z = 0; z < model.SizeZ; z++)
				for (int y = 0; y < model.SizeY; y++)
					for (int x = model.SizeX - 1; x >= 0; x++)
						if (model.At(x, y, z) is byte voxel
							&& voxel != 0)
						{
							renderer.RectRight(
								x: y * scaleX + 1,
								y: z * scaleY,
								voxel: voxel,
								sizeX: scaleX,
								sizeY: scaleY - 1);
							if (z >= model.SizeZ - 1 || model.At(x, y, z + 1) == 0)
								renderer.RectVertical(
									x: y * scaleX,
									y: (z + 1) * scaleY - 1,
									voxel: voxel,
									sizeX: scaleX,
									sizeY: 1);
							break;
						}
		}
		public static void DrawTop(IModel model, IRectangleRenderer renderer)
		{
			for (int y = 0; y < model.SizeY; y++)
				for (int x = 0; x < model.SizeX; x++)
					for (int z = model.SizeZ - 1; z >= 0; z--)
						if (model.At(x, y, z) is byte voxel
							&& voxel != 0)
						{
							renderer.RectVertical(
								x: y,
								y: z,
								voxel: voxel);
							break;
						}
		}
		public static void DrawBottom(IModel model, IRectangleRenderer renderer)
		{
			for (int y = 0; y < model.SizeY; y++)
				for (int x = 0; x < model.SizeX; x++)
					for (int z = 0; z < model.SizeZ; z--)
						if (model.At(x, y, z) is byte voxel
							&& voxel != 0)
						{
							renderer.RectVertical(
								x: y,
								y: z,
								voxel: voxel);
							break;
						}
		}
	}
}
