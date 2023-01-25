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
		public static void Draw45(IModel model, IRectangleRenderer renderer)
		{
			int pixelWidth = model.SizeX + model.SizeY;
			for (int pixelY = 0; pixelY < model.SizeZ; pixelY++)
				for (int pixelX = 0; pixelX <= pixelWidth; pixelX += 2)
				{
					bool leftDone = false,
						rightDone = pixelWidth - pixelX < 2;
					int startX = pixelX > model.SizeX - 1 ? 0 : model.SizeX - pixelX - 1,
						startY = pixelX - model.SizeX + 1 < 0 ? 0 : pixelX - model.SizeX + 1;
					for (int voxelX = startX, voxelY = startY;
						 voxelX <= model.SizeX && voxelY <= model.SizeY;
						 voxelX++, voxelY++)
					{
						if (!leftDone
							&& voxelY != 0
							&& model.At(voxelX, voxelY - 1, pixelY) is byte voxelLeft
							&& voxelLeft != 0)
						{
							renderer.RectRight(
								x: pixelX,
								y: pixelY,
								voxel: voxelLeft);
							leftDone = true;
						}
						if (!rightDone
							&& voxelX > 0
							&& model.At(voxelX - 1, voxelY, pixelY) is byte voxelRight
							&& voxelRight != 0)
						{
							renderer.RectLeft(
								x: pixelX + 1,
								y: pixelY,
								voxel: voxelRight);
							rightDone = true;
						}
						if (leftDone && rightDone) break;
						if (model.At(voxelX, voxelY, pixelY) is byte voxel
							&& voxel != 0)
						{
							if (!leftDone)
								renderer.RectLeft(
									x: pixelX,
									y: pixelY,
									voxel: voxel);
							if (!rightDone)
								renderer.RectRight(
									x: pixelX + 1,
									y: pixelY,
									voxel: voxel);
							break;
						}
					}
				}
		}
		public static void Draw45Peek(IModel model, IRectangleRenderer renderer, int scaleX = 6, int scaleY = 6)
		{
			int pixelWidth = model.SizeX + model.SizeY;
			for (int pixelY = 0; pixelY < model.SizeZ; pixelY++)
				for (int pixelX = 0; pixelX <= pixelWidth; pixelX += 2)
				{
					bool leftDone = false,
						rightDone = pixelWidth - pixelX < 2;
					int startX = pixelX >= model.SizeX ? 0 : model.SizeX - pixelX - 1,
						startY = pixelX < model.SizeX ? 0 : pixelX - model.SizeX + 1;
					for (int voxelX = startX, voxelY = startY;
						 voxelX <= model.SizeX && voxelY <= model.SizeY;
						 voxelX++, voxelY++)
					{
						if (!leftDone
							&& voxelY > 0
							&& voxelX < model.SizeX
							&& model.At(voxelX, voxelY - 1, pixelY) is byte voxelLeft
							&& voxelLeft != 0)
						{
							renderer.RectRight(
								x: pixelX * scaleX,
								y: pixelY * scaleY + 1,
								voxel: voxelLeft,
								sizeX: scaleX,
								sizeY: scaleY - 1);
							if (pixelY >= model.SizeZ - 1
								|| model.At(voxelX, voxelY - 1, pixelY + 1) == 0)
								renderer.RectVertical(
									x: pixelX * scaleX,
									y: (pixelY + 1) * scaleY - 1,
									voxel: voxelLeft,
									sizeX: scaleX,
									sizeY: 1);
							leftDone = true;
						}
						if (!rightDone
							&& voxelX > 0
							&& voxelY < model.SizeY
							&& model.At(voxelX - 1, voxelY, pixelY) is byte voxelRight
							&& voxelRight != 0)
						{
							renderer.RectLeft(
								x: (pixelX + 1) * scaleX,
								y: pixelY * scaleY + 1,
								voxel: voxelRight,
								sizeX: scaleX,
								sizeY: scaleY - 1);
							if (pixelY >= model.SizeZ - 1
								|| model.At(voxelX - 1, voxelY, pixelY + 1) == 0)
								renderer.RectVertical(
									x: (pixelX + 1) * scaleX,
									y: (pixelY + 1) * scaleY - 1,
									voxel: voxelRight,
									sizeX: scaleX,
									sizeY: 1);
							rightDone = true;
						}
						if ((leftDone && rightDone)
							|| voxelX >= model.SizeX
							|| voxelY >= model.SizeY)
							break;
						if (model.At(voxelX, voxelY, pixelY) is byte voxel
							&& voxel != 0)
						{
							bool peek = pixelY >= model.SizeZ - 1 || model.At(voxelX, voxelY, pixelY + 1) == 0;
							if (!leftDone)
							{
								renderer.RectLeft(
									x: pixelX * scaleX,
									y: pixelY * scaleY + 1,
									voxel: voxel,
									sizeX: scaleX,
									sizeY: scaleY - 1);
								if (peek)
									renderer.RectVertical(
										x: pixelX * scaleX,
										y: (pixelY + 1) * scaleY - 1,
										voxel: voxel,
										sizeX: scaleX,
										sizeY: 1);
							}
							if (!rightDone)
							{
								renderer.RectRight(
									x: (pixelX + 1) * scaleX,
									y: pixelY * scaleY + 1,
									voxel: voxel,
									sizeX: scaleX,
									sizeY: scaleY - 1);
								if (peek)
									renderer.RectVertical(
										x: (pixelX + 1) * scaleX,
										y: (pixelY + 1) * scaleY - 1,
										voxel: voxel,
										sizeX: scaleX,
										sizeY: 1);
							}
							break;
						}
					}
				}
		}
		public static void DrawAbove(IModel model, IRectangleRenderer renderer, int scaleX = 6, int scaleY = 2)
		{
			int pixelHeight = (model.SizeX + model.SizeZ) * 2;
			for (int voxelY = 0; voxelY < model.SizeY; voxelY++)
			{ // voxel y is pixel x
			  // Begin bottom row
				byte voxel = (byte)model.At(0, voxelY, 0);
				if (voxel != 0) renderer.RectRight(voxelY * scaleX, 0, voxel, scaleX, scaleY);
				// Finish bottom row
				// Begin main bulk of model
				for (int pixelY = 1; pixelY < pixelHeight; pixelY += 2)
				{ // pixel y
					bool below = false, above = pixelHeight - pixelY < 2;
					int startX = (pixelY / 2) > model.SizeZ - 1 ? (pixelY / 2) - model.SizeZ + 1 : 0,
						startZ = (pixelY / 2) > model.SizeZ - 1 ? model.SizeZ - 1 : (pixelY / 2);
					for (int voxelX = startX, voxelZ = startZ;
						 voxelX <= model.SizeX && voxelZ >= -1;
						 voxelX++, voxelZ--)
					{ // vx is voxel x, vz is voxel z
						if (!above && voxelZ + 1 < model.SizeZ && voxelX < model.SizeX)
						{
							voxel = (byte)model.At(voxelX, voxelY, voxelZ + 1);
							if (voxel != 0)
							{
								renderer.RectRight(voxelY * scaleX, (pixelY + 1) * scaleY, voxel, scaleX, scaleY);
								above = true;
							}
						}
						if (!below && voxelX > 0 && voxelZ >= 0)
						{
							voxel = (byte)model.At(voxelX - 1, voxelY, voxelZ);
							if (voxel != 0)
							{
								renderer.RectVertical(voxelY * scaleX, pixelY * scaleY, voxel, scaleX, scaleY);
								below = true;
							}
						}
						if ((above && below) || voxelX >= model.SizeX || voxelZ < 0) break;
						voxel = (byte)model.At(voxelX, voxelY, voxelZ);
						if (voxel != 0)
						{
							if (!above) renderer.RectVertical(voxelY * scaleX, (pixelY + 1) * scaleY, voxel, scaleX, scaleY);
							if (!below) renderer.RectRight(voxelY * scaleX, pixelY * scaleY, voxel, scaleX, scaleY);
							break;
						}
					}
				}
				// Finish main bulk of model
			}
		}
	}
}
