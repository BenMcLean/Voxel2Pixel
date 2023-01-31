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
		public static int DiagonalWidth(IModel model) => model.SizeX + model.SizeY;
		public static int DiagonalHeight(IModel model) => model.SizeZ;
		public static void Diagonal(IModel model, IRectangleRenderer renderer)
		{
			int pixelWidth = model.SizeX + model.SizeY;
			for (int pixelY = 0; pixelY < model.SizeZ; pixelY++)
				for (int pixelX = 0; pixelX <= pixelWidth; pixelX += 2)
				{
					bool leftDone = false,
						rightDone = false;
					int startX = Math.Max(pixelX - model.SizeY + 1, 0),
						startY = Math.Max(model.SizeY - 1 - pixelX, 0),
						voxelZ = model.SizeZ - 1 - pixelY;
					if (pixelX + 2 >= pixelWidth + 1
						&& model.At(model.SizeX - 1, 0, voxelZ) is byte rightEdge
						&& rightEdge != 0)
					{
						renderer.RectRight(
							x: pixelX,
							y: pixelY,
							voxel: rightEdge);
						continue;
					}
					for (int voxelX = startX, voxelY = startY;
						 voxelX <= model.SizeX && voxelY <= model.SizeY;
						 voxelX++, voxelY++)
					{
						if (!leftDone
							&& voxelX > 0
							&& model.At(voxelX - 1, voxelY, voxelZ) is byte voxelRight
							&& voxelRight != 0)
						{
							renderer.RectRight(
								x: pixelX,
								y: pixelY,
								voxel: voxelRight);
							leftDone = true;
						}
						if (!rightDone
							&& voxelY > 0
							&& model.At(voxelX, voxelY - 1, voxelZ) is byte voxelLeft
							&& voxelLeft != 0)
						{
							renderer.RectLeft(
								x: pixelX + 1,
								y: pixelY,
								voxel: voxelLeft);
							rightDone = true;
						}
						if (leftDone && rightDone) break;
						if (model.At(voxelX, voxelY, voxelZ) is byte voxel
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
		public static int DiagonalPeekWidth(IModel model, int scaleX = 4) => (model.SizeX + model.SizeY) * scaleX;
		public static int DiagonalPeekHeight(IModel model, int scaleY = 6) => model.SizeZ * scaleY;
		public static void DiagonalPeek(IModel model, IRectangleRenderer renderer, int scaleX = 4, int scaleY = 6)
		{
			void DrawRight(int x, int y, byte voxel, bool peek = false)
			{
				if (peek)
				{
					renderer.RectVertical(
						x: x * scaleX,
						y: y * scaleY,
						voxel: voxel,
						sizeX: scaleX,
						sizeY: 1);
					renderer.RectRight(
						x: x * scaleX,
						y: y * scaleY + 1,
						voxel: voxel,
						sizeX: scaleX,
						sizeY: scaleY - 1);
				}
				else
					renderer.RectRight(
						x: x * scaleX,
						y: y * scaleY,
						voxel: voxel,
						sizeX: scaleX,
						sizeY: scaleY);
			}
			void DrawLeft(int x, int y, byte voxel, bool peek = false)
			{
				if (peek)
				{
					renderer.RectVertical(
						x: x * scaleX,
						y: y * scaleY,
						voxel: voxel,
						sizeX: scaleX,
						sizeY: 1);
					renderer.RectLeft(
						x: x * scaleX,
						y: y * scaleY + 1,
						voxel: voxel,
						sizeX: scaleX,
						sizeY: scaleY - 1);
				}
				else
					renderer.RectLeft(
						x: x * scaleX,
						y: y * scaleY,
						voxel: voxel,
						sizeX: scaleX,
						sizeY: scaleY);
			}
			int pixelWidth = model.SizeX + model.SizeY;
			for (int pixelY = 0; pixelY < model.SizeZ; pixelY++)
				for (int pixelX = 0; pixelX <= pixelWidth; pixelX += 2)
				{
					bool leftDone = false,
						rightDone = false;
					int startX = Math.Max(pixelX - model.SizeY + 1, 0),
						startY = Math.Max(model.SizeY - 1 - pixelX, 0),
						voxelZ = model.SizeZ - 1 - pixelY;
					if (pixelX + 2 >= pixelWidth + 1
						&& model.At(model.SizeX - 1, 0, voxelZ) is byte rightEdge
						&& rightEdge != 0)
					{
						DrawRight(
							x: pixelX,
							y: pixelY,
							voxel: rightEdge,
							peek: voxelZ >= model.SizeZ
								|| (model.At(model.SizeX - 1, 0, voxelZ + 1) is byte voxelAbove
								&& voxelAbove != 0));
						continue;
					}
					for (int voxelX = startX, voxelY = startY;
						 voxelX <= model.SizeX && voxelY <= model.SizeY;
						 voxelX++, voxelY++)
					{
						if (!leftDone
							&& voxelX > 0
							&& model.At(voxelX - 1, voxelY, voxelZ) is byte voxelRight
							&& voxelRight != 0)
						{
							DrawRight(
								x: pixelX,
								y: pixelY,
								voxel: voxelRight,
								peek: voxelZ == model.SizeZ - 1
									|| (model.At(voxelX - 1, voxelY, voxelZ + 1) is byte voxelAbove
									&& voxelAbove == 0));
							leftDone = true;
						}
						if (!rightDone
							&& voxelY > 0
							&& model.At(voxelX, voxelY - 1, voxelZ) is byte voxelLeft
							&& voxelLeft != 0)
						{
							DrawLeft(
								x: pixelX + 1,
								y: pixelY,
								voxel: voxelLeft,
								peek: voxelZ == model.SizeZ - 1
									|| (model.At(voxelX, voxelY - 1, voxelZ + 1) is byte voxelAbove
									&& voxelAbove == 0));
							rightDone = true;
						}
						if (leftDone && rightDone) break;
						if (model.At(voxelX, voxelY, voxelZ) is byte voxel
							&& voxel != 0)
						{
							if (!leftDone)
								DrawLeft(
									x: pixelX,
									y: pixelY,
									voxel: voxel,
									peek: voxelZ == model.SizeZ - 1
										|| (model.At(voxelX, voxelY, voxelZ + 1) is byte voxelAbove
										&& voxelAbove == 0));
							if (!rightDone)
								DrawRight(
									x: pixelX + 1,
									y: pixelY,
									voxel: voxel,
									peek: voxelZ == model.SizeZ - 1
										|| (model.At(voxelX, voxelY, voxelZ + 1) is byte voxelAbove
										&& voxelAbove == 0));
							break;
						}
					}
				}
		}
		public static int AboveWidth(IModel model) => model.SizeX;
		public static int AboveHeight(IModel model) => model.SizeY + model.SizeZ;
		public static void Above(IModel model, IRectangleRenderer renderer)
		{
			int pixelHeight = model.SizeY + model.SizeZ;
			for (int pixelX = 0; pixelX < model.SizeX; pixelX++)
				for (int pixelY = 0; pixelY <= pixelHeight; pixelY += 2)
				{
					int startY = Math.Max(model.SizeY - 1 - pixelY, 0),
						startZ = model.SizeZ - 1 - Math.Max(pixelY + 1 - model.SizeY, 0);
					bool higher = false,
						lower = false;
					if (pixelY + 2 > pixelHeight
						&& model.At(pixelX, 0, 0) is byte bottomEdge
						&& bottomEdge != 0)
					{
						renderer.RectRight(
							x: pixelX,
							y: pixelY,
							voxel: bottomEdge);
						continue;
					}
					for (int voxelY = startY, voxelZ = startZ;
						voxelY < model.SizeY && voxelZ >= 0;
						voxelY++, voxelZ--)
					{
						if (voxelZ < model.SizeZ - 1
							&& model.At(pixelX, voxelY, voxelZ + 1) is byte voxelAbove
							&& voxelAbove != 0)
						{
							renderer.RectRight(
								x: pixelX,
								y: pixelY,
								voxel: voxelAbove);
							higher = true;
						}
						if (voxelY > 0
							&& model.At(pixelX, voxelY - 1, voxelZ) is byte voxelFront
							&& voxelFront != 0)
						{
							renderer.RectVertical(
								x: pixelX,
								y: pixelY + 1,
								voxel: voxelFront);
							lower = true;
						}
						if (model.At(pixelX, voxelY, voxelZ) is byte voxel
							&& voxel != 0)
						{
							if (!higher)
								renderer.RectVertical(
									x: pixelX,
									y: pixelY,
									voxel: voxel);
							if (!lower)
								renderer.RectRight(
									x: pixelX,
									y: pixelY + 1,
									voxel: voxel);
							break;
						}
						if (!higher
							&& voxelY < model.SizeY - 1
							&& model.At(pixelX, voxelY + 1, voxelZ) is byte voxelBack
							&& voxelBack != 0)
						{
							renderer.RectRight(
								x: pixelX,
								y: pixelY,
								voxel: voxelBack);
							higher = true;
						}
						if (!lower
							&& voxelZ > 0
							&& model.At(pixelX, voxelY, voxelZ - 1) is byte voxelBelow
							&& voxelBelow != 0)
						{
							renderer.RectVertical(
								x: pixelX,
								y: pixelY + 1,
								voxel: voxelBelow);
							lower = true;
						}
						if (higher && lower)
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
							&& pixelX < pixelWidth + modelSizeY2 - pixelY - 2,
						startAtLeft = !startAtTop && pixelX < modelSizeY2;
					//screen.x = (map.x - map.y) * TILE_WIDTH_HALF;
					//screen.y = (map.x + map.y) * TILE_HEIGHT_HALF;
					//map.x = (screen.x / TILE_WIDTH_HALF + screen.y / TILE_HEIGHT_HALF) / 2;
					//map.y = (screen.y / TILE_HEIGHT_HALF - (screen.x / TILE_WIDTH_HALF)) / 2;
					int halfX = pixelX / 2,
						halfY = pixelY / 2,
						startX = startAtTop ? model.SizeX - 1 - (halfY - halfX + model.SizeX) / 2
							: startAtLeft ? 0
							: 0,
						startY = startAtTop ? model.SizeY - 1 - (halfX + halfY - model.SizeX + 1) / 2
							: startAtLeft ? model.SizeY - 1 - halfX
							: 0,
						startZ = startAtTop ? model.SizeZ - 1
							: startAtLeft ? model.SizeZ - 1 - (halfY - halfX - model.SizeX) / 2
							: 0;
					if (!startAtTop && !startAtLeft)
						renderer.Triangle(
							x: pixelX,
							y: pixelY,
							right: right,
							color: startAtLeft ? 0x00FFFFFF
							: right ? unchecked((int)0xFF0000FF) : 0x0000FFFF);
					else if (model.At(startX, startY, startZ) is byte voxel
						&& voxel != 0)
					{
						renderer.TriangleRightFace(
							x: pixelX,
							y: pixelY,
							right: right,
							voxel: voxel);
						//break;
					}
					//for (int voxelX = startX, voxelY = startY, voxelZ = startZ, distance = 0;
					//		voxelX < model.SizeX && voxelY < model.SizeY && voxelZ >= 0;
					//		voxelX++, voxelY++, voxelZ--, distance++)
					//{
					//	if (model.At(voxelX, voxelY, voxelZ) is byte voxel
					//		&& voxel != 0)
					//	{
					//		renderer.TriangleRightFace(
					//			x: pixelX,
					//			y: pixelY,
					//			right: right,
					//			voxel: voxel);
					//		break;
					//	}
					//}
				}
			}
		}
		#endregion Isometric
	}
}
