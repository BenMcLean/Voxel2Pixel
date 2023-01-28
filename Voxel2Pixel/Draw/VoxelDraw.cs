using System;
using Voxel2Pixel.Model;
using Voxel2Pixel.Render;

namespace Voxel2Pixel.Draw
{
	/// <summary>
	/// I have been forced into a situation where X and Y mean something different in 2D space from what they mean in 3D space. Not only do the coordinates not match, but 3D is upside down when compared to 2D. I hate this. I hate it so much. But I'm stuck with it if I want my software to be interoperable with other existing software.
	/// In 2D space for pixels, X+ means east/right, Y+ means down. This is dictated by how 2D raster graphics are typically stored.
	/// In 3D space for voxels, I'm following the MagicaVoxel convention, which is Z+up, right-handed, so X+ means east/right, Y+ means forwards/north and Z+ means up.
	/// </summary>
	public static class VoxelDraw
	{
		#region Straight
		public static int FrontWidth(IModel model) => model.SizeX;
		public static int FrontHeight(IModel model) => model.SizeZ;
		public static void Front(IModel model, IRectangleRenderer renderer)
		{
			for (int z = 0; z < model.SizeZ; z++)
				for (int x = 0; x < model.SizeX; x++)
					for (int y = 0; y < model.SizeY; y++)
						if (model.At(x, y, z) is byte voxel
							&& voxel != 0)
						{
							renderer.RectRight(
								x: x,
								y: model.SizeZ - 1 - z,
								voxel: voxel);
							break;
						}
		}
		public static int FrontPeekWidth(IModel model, int scaleX = 6) => model.SizeX * scaleX;
		public static int FrontPeekHeight(IModel model, int scaleY = 6) => model.SizeZ * scaleY;
		public static void FrontPeek(IModel model, IRectangleRenderer renderer, int scaleX = 6, int scaleY = 6)
		{
			for (int z = 0; z < model.SizeZ; z++)
				for (int x = 0; x < model.SizeX; x++)
					for (int y = 0; y < model.SizeY; y++)
						if (model.At(x, y, z) is byte voxel
							&& voxel != 0)
						{
							if (z >= model.SizeZ - 1
								|| model.At(x, y, z + 1) == 0)
							{
								renderer.RectVertical(
									x: x * scaleX,
									y: (model.SizeZ - 1 - z) * scaleY,
									voxel: voxel,
									sizeX: scaleX,
									sizeY: 1);
								renderer.RectRight(
									x: x * scaleX,
									y: (model.SizeZ - 1 - z) * scaleY + 1,
									voxel: voxel,
									sizeX: scaleX,
									sizeY: scaleY - 1);
							}
							else
								renderer.RectRight(
									x: x * scaleX,
									y: (model.SizeZ - 1 - z) * scaleY,
									voxel: voxel,
									sizeX: scaleX,
									sizeY: scaleY);
							break;
						}
		}
		public static int RightWidth(IModel model) => model.SizeY;
		public static int RightHeight(IModel model) => model.SizeZ;
		public static void Right(IModel model, IRectangleRenderer renderer)
		{
			for (int z = 0; z < model.SizeZ; z++)
				for (int y = 0; y < model.SizeY; y++)
					for (int x = 0; x < model.SizeX; x++)
						if (model.At(x, y, z) is byte voxel
							&& voxel != 0)
						{
							renderer.RectRight(
								x: model.SizeY - 1 - y,
								y: model.SizeZ - 1 - z,
								voxel: voxel);
							break;
						}
		}
		public static int RightPeekWidth(IModel model, int scaleX = 6) => model.SizeY * scaleX;
		public static int RightPeekHeight(IModel model, int scaleY = 6) => model.SizeZ * scaleY;
		public static void RightPeek(IModel model, IRectangleRenderer renderer, int scaleX = 6, int scaleY = 6)
		{
			for (int z = 0; z < model.SizeZ; z++)
				for (int y = 0; y < model.SizeY; y++)
					for (int x = 0; x < model.SizeX; x++)
						if (model.At(x, y, z) is byte voxel
							&& voxel != 0)
						{
							if (z >= model.SizeZ - 1
								|| model.At(x, y, z + 1) == 0)
							{
								renderer.RectVertical(
									x: (model.SizeY - 1 - y) * scaleX,
									y: (model.SizeZ - 1 - z) * scaleY,
									voxel: voxel,
									sizeX: scaleX,
									sizeY: 1);
								renderer.RectRight(
									x: (model.SizeY - 1 - y) * scaleX,
									y: (model.SizeZ - 1 - z) * scaleY + 1,
									voxel: voxel,
									sizeX: scaleX,
									sizeY: scaleY - 1);
							}
							else
								renderer.RectRight(
									x: (model.SizeY - 1 - y) * scaleX,
									y: (model.SizeZ - 1 - z) * scaleY,
									voxel: voxel,
									sizeX: scaleX,
									sizeY: scaleY);
							break;
						}
		}
		public static int BackWidth(IModel model) => model.SizeX;
		public static int BackHeight(IModel model) => model.SizeZ;
		public static void Back(IModel model, IRectangleRenderer renderer)
		{
			for (int z = 0; z < model.SizeZ; z++)
				for (int x = 0; x < model.SizeX; x++)
					for (int y = model.SizeY - 1; y >= 0; y--)
						if (model.At(x, y, z) is byte voxel
							&& voxel != 0)
						{
							renderer.RectLeft(
								x: model.SizeX - 1 - x,
								y: model.SizeZ - 1 - z,
								voxel: voxel);
							break;
						}
		}
		public static int BackPeekWidth(IModel model, int scaleX = 6) => model.SizeX * scaleX;
		public static int BackPeekHeight(IModel model, int scaleY = 6) => model.SizeZ * scaleY;
		public static void BackPeek(IModel model, IRectangleRenderer renderer, int scaleX = 6, int scaleY = 6)
		{
			for (int z = 0; z < model.SizeZ; z++)
				for (int x = 0; x < model.SizeX; x++)
					for (int y = model.SizeY - 1; y >= 0; y--)
						if (model.At(x, y, z) is byte voxel
							&& voxel != 0)
						{
							if (z >= model.SizeZ - 1
								|| model.At(x, y, z + 1) == 0)
							{
								renderer.RectVertical(
									x: (model.SizeX - 1 - x) * scaleX,
									y: (model.SizeZ - 1 - z) * scaleY,
									voxel: voxel,
									sizeX: scaleX,
									sizeY: 1);
								renderer.RectRight(
									x: (model.SizeX - 1 - x) * scaleX,
									y: (model.SizeZ - 1 - z) * scaleY + 1,
									voxel: voxel,
									sizeX: scaleX,
									sizeY: scaleY - 1);
							}
							else
								renderer.RectRight(
									x: (model.SizeX - 1 - x) * scaleX,
									y: (model.SizeZ - 1 - z) * scaleY,
									voxel: voxel,
									sizeX: scaleX,
									sizeY: scaleY);
							break;
						}
		}
		public static int LeftWidth(IModel model) => model.SizeY;
		public static int LeftHeight(IModel model) => model.SizeZ;
		public static void Left(IModel model, IRectangleRenderer renderer)
		{
			for (int z = 0; z < model.SizeZ; z++)
				for (int y = 0; y < model.SizeY; y++)
					for (int x = model.SizeX - 1; x >= 0; x--)
						if (model.At(x, y, z) is byte voxel
							&& voxel != 0)
						{
							renderer.RectLeft(
								x: y,
								y: model.SizeZ - 1 - z,
								voxel: voxel);
							break;
						}
		}
		public static int LeftPeekWidth(IModel model, int scaleX = 6) => model.SizeY * scaleX;
		public static int LeftPeekHeight(IModel model, int scaleY = 6) => model.SizeZ * scaleY;
		public static void LeftPeek(IModel model, IRectangleRenderer renderer, int scaleX = 6, int scaleY = 6)
		{
			for (int z = 0; z < model.SizeZ; z++)
				for (int y = 0; y < model.SizeY; y++)
					for (int x = model.SizeX - 1; x >= 0; x--)
						if (model.At(x, y, z) is byte voxel
							&& voxel != 0)
						{
							if (z >= model.SizeZ - 1
								|| model.At(x, y, z + 1) == 0)
							{
								renderer.RectVertical(
									x: y * scaleX,
									y: (model.SizeZ - 1 - z) * scaleY,
									voxel: voxel,
									sizeX: scaleX,
									sizeY: 1);
								renderer.RectRight(
									x: y * scaleX,
									y: (model.SizeZ - 1 - z) * scaleY + 1,
									voxel: voxel,
									sizeX: scaleX,
									sizeY: scaleY - 1);
							}
							else
								renderer.RectRight(
									x: y * scaleX,
									y: (model.SizeZ - 1 - z) * scaleY,
									voxel: voxel,
									sizeX: scaleX,
									sizeY: scaleY);
							break;
						}
		}
		public static int TopWidth(IModel model) => model.SizeX;
		public static int TopHeight(IModel model) => model.SizeY;
		public static void Top(IModel model, IRectangleRenderer renderer)
		{
			for (int y = 0; y < model.SizeY; y++)
				for (int x = 0; x < model.SizeX; x++)
					for (int z = model.SizeZ - 1; z >= 0; z--)
						if (model.At(x, y, z) is byte voxel
							&& voxel != 0)
						{
							renderer.RectVertical(
								x: x,
								y: model.SizeY - 1 - y,
								voxel: voxel);
							break;
						}
		}
		public static int BottomWidth(IModel model) => model.SizeX;
		public static int BottomHeight(IModel model) => model.SizeY;
		public static void Bottom(IModel model, IRectangleRenderer renderer)
		{
			for (int x = 0; x < model.SizeX; x++)
				for (int y = 0; y < model.SizeY; y++)
					for (int z = 0; z < model.SizeZ; z++)
						if (model.At(x, y, z) is byte voxel
							&& voxel != 0)
						{
							renderer.RectVertical(
								x: x,
								y: y,
								voxel: voxel);
							break;
						}
		}
		#endregion Straight
		#region Diagonal
		public static int Draw45Width(IModel model) => model.SizeX + model.SizeY;
		public static int Draw45Height(IModel model) => model.SizeZ;
		public static void Draw45(IModel model, IRectangleRenderer renderer)
		{
			int pixelWidth = model.SizeX + model.SizeY;
			for (int pixelY = 0; pixelY < model.SizeZ; pixelY++)
				for (int pixelX = 0; pixelX <= pixelWidth; pixelX += 2)
				{
					bool leftDone = pixelWidth - pixelX < 2,
						rightDone = false;
					int startX = Math.Max(model.SizeX - pixelX - 1, 0),
						startY = Math.Max(pixelX - model.SizeX + 1, 0);
					for (int voxelX = startX, voxelY = startY;
						 voxelX < model.SizeX && voxelY < model.SizeY;
						 voxelX++, voxelY++)
					{
						if (!leftDone
							&& voxelX > 0
							&& model.At(voxelX - 1, voxelY, pixelY) is byte voxelRight
							&& voxelRight != 0)
						{
							renderer.RectRight(
								x: pixelWidth - 1 - pixelX,
								y: model.SizeZ - 1 - pixelY,
								voxel: voxelRight);
							leftDone = true;
						}
						if (!rightDone
							&& voxelY > 0
							&& model.At(voxelX, voxelY - 1, pixelY) is byte voxelLeft
							&& voxelLeft != 0)
						{
							renderer.RectLeft(
								x: pixelWidth - pixelX,
								y: model.SizeZ - 1 - pixelY,
								voxel: voxelLeft);
							rightDone = true;
						}
						if (leftDone && rightDone) break;
						if (model.At(voxelX, voxelY, pixelY) is byte voxel
							&& voxel != 0)
						{
							if (!leftDone)
								renderer.RectLeft(
									x: pixelWidth - 1 - pixelX,
									y: model.SizeZ - 1 - pixelY,
									voxel: voxel);
							if (!rightDone)
								renderer.RectRight(
									x: pixelWidth - pixelX,
									y: model.SizeZ - 1 - pixelY,
									voxel: voxel);
							break;
						}
					}
				}
		}
		public static int Draw45PeekWidth(IModel model, int scaleX = 4) => (model.SizeX + model.SizeY) * scaleX;
		public static int Draw45PeekHeight(IModel model, int scaleY = 6) => model.SizeZ * scaleY;
		public static void Draw45Peek(IModel model, IRectangleRenderer renderer, int scaleX = 4, int scaleY = 6)
		{
			int pixelWidth = model.SizeX + model.SizeY;
			for (int pixelY = 0; pixelY < model.SizeZ; pixelY++)
				for (int pixelX = 0; pixelX <= pixelWidth; pixelX += 2)
				{
					bool leftDone = pixelWidth - pixelX < 2,
						rightDone = false;
					int startX = Math.Max(model.SizeX - pixelX - 1, 0),
						startY = Math.Max(pixelX - model.SizeX + 1, 0);
					for (int voxelX = startX, voxelY = startY;
						 voxelX < model.SizeX && voxelY < model.SizeY;
						 voxelX++, voxelY++)
					{
						if (!leftDone
							&& voxelX > 0
							&& model.At(voxelX - 1, voxelY, pixelY) is byte voxelRight
							&& voxelRight != 0)
						{
							if (pixelY >= model.SizeZ - 1
								|| model.At(voxelX - 1, voxelY, pixelY + 1) == 0)
							{
								renderer.RectVertical(
									x: (pixelWidth - 1 - pixelX) * scaleX,
									y: (model.SizeZ - 1 - pixelY) * scaleY,
									voxel: voxelRight,
									sizeX: scaleX,
									sizeY: 1);
								renderer.RectRight(
									x: (pixelWidth - 1 - pixelX) * scaleX,
									y: (model.SizeZ - 1 - pixelY) * scaleY + 1,
									voxel: voxelRight,
									sizeX: scaleX,
									sizeY: scaleY - 1);
							}
							else
								renderer.RectRight(
									x: (pixelWidth - 1 - pixelX) * scaleX,
									y: (model.SizeZ - 1 - pixelY) * scaleY,
									voxel: voxelRight,
									sizeX: scaleX,
									sizeY: scaleY);
							leftDone = true;
						}
						if (!rightDone
							&& voxelY > 0
							&& model.At(voxelX, voxelY - 1, pixelY) is byte voxelLeft
							&& voxelLeft != 0)
						{
							if (pixelY >= model.SizeZ - 1
								|| model.At(voxelX, voxelY - 1, pixelY + 1) == 0)
							{
								renderer.RectVertical(
									x: (pixelWidth - pixelX) * scaleX,
									y: (model.SizeZ - 1 - pixelY) * scaleY,
									voxel: voxelLeft,
									sizeX: scaleX,
									sizeY: 1);
								renderer.RectLeft(
									x: (pixelWidth - pixelX) * scaleX,
									y: (model.SizeZ - 1 - pixelY) * scaleY + 1,
									voxel: voxelLeft,
									sizeX: scaleX,
									sizeY: scaleY - 1);
							}
							else
								renderer.RectLeft(
									x: (pixelWidth - pixelX) * scaleX,
									y: (model.SizeZ - 1 - pixelY) * scaleY,
									voxel: voxelLeft,
									sizeX: scaleX,
									sizeY: scaleY);
							rightDone = true;
						}
						if (leftDone && rightDone) break;
						if (model.At(voxelX, voxelY, pixelY) is byte voxel
							&& voxel != 0)
						{
							if (!leftDone)
							{
								if (pixelY >= model.SizeZ - 1
									|| model.At(voxelX, voxelY, pixelY + 1) == 0)
								{
									renderer.RectVertical(
										x: (pixelWidth - 1 - pixelX) * scaleX,
										y: (model.SizeZ - 1 - pixelY) * scaleY,
										voxel: voxel,
										sizeX: scaleX,
										sizeY: 1);
									renderer.RectLeft(
										x: (pixelWidth - 1 - pixelX) * scaleX,
										y: (model.SizeZ - 1 - pixelY) * scaleY + 1,
										voxel: voxel,
										sizeX: scaleX,
										sizeY: scaleY - 1);
								}
								else
									renderer.RectLeft(
										x: (pixelWidth - 1 - pixelX) * scaleX,
										y: (model.SizeZ - 1 - pixelY) * scaleY,
										voxel: voxel,
										sizeX: scaleX,
										sizeY: scaleY);
							}
							if (!rightDone)
							{
								if (pixelY >= model.SizeZ - 1
									|| model.At(voxelX, voxelY, pixelY + 1) == 0)
								{
									renderer.RectVertical(
										x: (pixelWidth - pixelX) * scaleX,
										y: (model.SizeZ - 1 - pixelY) * scaleY,
										voxel: voxel,
										sizeX: scaleX,
										sizeY: 1);
									renderer.RectRight(
										x: (pixelWidth - pixelX) * scaleX,
										y: (model.SizeZ - 1 - pixelY) * scaleY + 1,
										voxel: voxel,
										sizeX: scaleX,
										sizeY: scaleY - 1);
								}
								else
									renderer.RectRight(
										x: (pixelWidth - pixelX) * scaleX,
										y: (model.SizeZ - 1 - pixelY) * scaleY,
										voxel: voxel,
										sizeX: scaleX,
										sizeY: scaleY);
							}
							break;
						}
					}
				}
		}
		public static int DrawAboveWidth(IModel model) => model.SizeX;
		public static int DrawAboveHeight(IModel model) => model.SizeY + model.SizeZ;
		public static void DrawAbove(IModel model, IRectangleRenderer renderer)
		{
			int pixelHeight = model.SizeY + model.SizeZ;
			for (int pixelX = 0; pixelX < model.SizeX; pixelX++)
				for (int pixelY = 0; pixelY <= pixelHeight; pixelY += 2)
				{
					int startY = Math.Max(model.SizeY - 1 - pixelY, 0),
						startZ = model.SizeZ - 1 - Math.Max(pixelY - model.SizeY, 0);
					bool above = false,
						below = false;
					for (int voxelY = startY, voxelZ = startZ;
						voxelY < model.SizeY && voxelZ >= 0;
						voxelY++, voxelZ--)
					{
						if (model.At(pixelX, voxelY, voxelZ) is byte voxel
							&& voxel != 0)
						{
							if (!above)
								renderer.RectVertical(
									x: pixelX,
									y: pixelY,
									voxel: voxel);
							if (!below)
								renderer.RectRight(
									x: pixelX,
									y: pixelY + 1,
									voxel: voxel);
							break;
						}
						if (!above
							&& voxelY < model.SizeY - 1
							&& model.At(pixelX, voxelY + 1, voxelZ) is byte voxelAbove
							&& voxelAbove != 0)
						{
							renderer.RectRight(
								x: pixelX,
								y: pixelY,
								voxel: voxelAbove);
							above = true;
						}
						if (!below
							&& voxelZ > 0
							&& model.At(pixelX, voxelY, voxelZ - 1) is byte voxelBelow
							&& voxelBelow != 0)
						{
							renderer.RectVertical(
								x: pixelX,
								y: pixelY + 1,
								voxel: voxelBelow);
							below = true;
						}
						if (above && below)
							break;
					}
				}
		}
		#endregion Diagonal
		#region Isometric
		public static int IsoWidth(IModel model) => 2 * (model.SizeX + model.SizeY);
		public static int IsoHeight(IModel model) => 2 * (model.SizeX + model.SizeY) + 4 * model.SizeZ - 1;
		public static void Iso(IModel model, ITriangleRenderer renderer)
		{
			// To move one x+ in voxels is x + 2, y - 2 in pixels.
			// To move one x- in voxels is x - 2, y + 2 in pixels.
			// To move one y+ in voxels is x - 2, y - 2 in pixels.
			// To move one y- in voxels is x + 2, y + 2 in pixels.
			// To move one z+ in voxels is y - 4 in pixels.
			// To move one z- in voxels is y + 4 in pixels.
			int modelSizeX2 = model.SizeX * 2,
				modelSizeY2 = model.SizeY * 2,
				modelSizeZ4 = model.SizeZ * 4,
				pixelWidth = modelSizeX2 + modelSizeY2,
				pixelHeight = pixelWidth + modelSizeZ4;
			bool evenSizeX = model.SizeX % 2 == 0;
			for (int pixelY = 0; pixelY < pixelHeight - 2; pixelY += 2)
			{
				int pixelStartX = pixelY < modelSizeX2 + modelSizeZ4 ?
						Math.Max(modelSizeX2 - pixelY - 2, 0)
						: pixelY - modelSizeX2 - modelSizeZ4 + 2,
					pixelStopX = pixelY < modelSizeY2 + modelSizeZ4 ?
						Math.Min(modelSizeX2 + pixelY + 2, pixelWidth - 1)
						: pixelWidth + modelSizeY2 + modelSizeZ4 - pixelY - 2;
				for (int pixelX = pixelStartX; pixelX < pixelStopX; pixelX += 2)
				{
					bool right = ((pixelX >> 1) + (pixelY >> 1) & 1) == (evenSizeX ? 0 : 1),
						startAtTop = pixelY < modelSizeX2 + modelSizeY2
							&& pixelX > pixelY - modelSizeX2
							&& pixelX < pixelWidth + modelSizeY2 - pixelY - 2;
					renderer.Triangle(
						x: pixelX,
						y: pixelY,
						right: right,
						color: startAtTop ? 0x00FF00FF
						: right ? unchecked((int)0xFF0000FF) : 0x0000FFFF);
				}
			}
		}
		#endregion Isometric
	}
}
