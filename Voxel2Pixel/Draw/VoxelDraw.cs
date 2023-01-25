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
		public static void DrawRightPeek(IModel model, IRectangleRenderer renderer, int scaleX = 6, int scaleY = 6)
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
							if (z >= model.SizeZ - 1
								|| model.At(x, y, z + 1) == 0)
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
		public static void DrawLeftPeek(IModel model, IRectangleRenderer renderer, int scaleX = 6, int scaleY = 6)
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
							if (z >= model.SizeZ - 1
								|| model.At(x, y, z + 1) == 0)
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
		public static void Draw45Peek(IModel model, IRectangleRenderer renderer, int scaleX = 6, int scaleY = 6)
		{
			int pixelWidth = model.SizeX + model.SizeY;
			for (int py = 0; py < model.SizeZ; py++)
				for (int px = 0; px <= pixelWidth; px += 2)
				{
					bool leftDone = false,
						rightDone = pixelWidth - px < 2;
					int startX = px >= model.SizeX ? 0 : model.SizeX - px - 1,
						startY = px < model.SizeX ? 0 : px - model.SizeX + 1;
					for (int vx = startX, vy = startY;
						 vx <= model.SizeX && vy <= model.SizeY;
						 vx++, vy++)
					{ // vx is voxel x, vy is voxel y
						if (!leftDone
							&& vy > 0
							&& vx < model.SizeX
							&& model.At(vx, vy - 1, py) is byte voxel
							&& voxel != 0)
						{
							renderer.RectRight(
								x: px * scaleX,
								y: py * scaleY,
								voxel: voxel,
								sizeX: scaleX,
								sizeY: scaleY);
							if (py >= model.SizeZ - 1
								|| model.At(vx, vy - 1, py + 1) == 0)
								renderer.RectVertical(
									x: px * scaleX,
									y: (py + 1) * scaleY - 1,
									voxel: voxel,
									sizeX: scaleX,
									sizeY: 1);
							leftDone = true;
						}
						if (!rightDone && vx > 0 && vy < model.SizeY)
						{
							if (model.At(vx - 1, vy, py) is byte voxel2
								&& voxel2 != 0)
							{
								renderer.RectLeft(
									x: (px + 1) * scaleX,
									y: py * scaleY,
									voxel: voxel2,
									sizeX: scaleX,
									sizeY: scaleY);
								if (py >= model.SizeZ - 1 || model.At(vx - 1, vy, py + 1) == 0)
									renderer.RectVertical(
										x: (px + 1) * scaleX,
										y: (py + 1) * scaleY - 1,
										voxel: voxel2,
										sizeX: scaleX,
										sizeY: 1);
								rightDone = true;
							}
						}
						if ((leftDone && rightDone) || vx >= model.SizeX || vy >= model.SizeY) break;
						if (model.At(vx, vy, py) is byte voxel3
							&& voxel3 != 0)
						{
							bool peek = py >= model.SizeZ - 1 || model.At(vx, vy, py + 1) == 0;
							if (!leftDone)
							{
								renderer.RectLeft(
									x: px * scaleX,
									y: py * scaleY,
									voxel: voxel3,
									sizeX: scaleX,
									sizeY: scaleY);
								if (peek)
									renderer.RectVertical(
										x: px * scaleX,
										y: (py + 1) * scaleY - 1,
										voxel: voxel3,
										sizeX: scaleX,
										sizeY: 1);
							}
							if (!rightDone)
							{
								renderer.RectRight(
									x: (px + 1) * scaleX,
									y: py * scaleY,
									voxel: voxel3,
									sizeX: scaleX,
									sizeY: scaleY);
								if (peek)
									renderer.RectVertical(
										x: (px + 1) * scaleX,
										y: (py + 1) * scaleY - 1,
										voxel: voxel3,
										sizeX: scaleX,
										sizeY: 1);
							}
							break;
						}
					}
				}
		}
	}
}
