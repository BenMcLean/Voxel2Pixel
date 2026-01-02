using System;
using System.Linq;
using BenVoxel;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;

namespace Voxel2Pixel.Draw;

/// <summary>
/// This is some older code which manages to render by checking each position from the camera in a for loop until it finds a voxel to draw or finds the boundary of the model and then stops.
/// It works, but I think it is massively inefficient compared to the regular VoxelDraw class in most cases and is only more efficient in a few special cases.
/// See also the comments in VoxelDraw.cs.
/// </summary>
public static class DenseVoxelDraw
{
	#region Straight
	public static int FrontWidth(IModel model) => model.SizeX;
	public static int FrontHeight(IModel model) => model.SizeZ;
	public static void Front(IModel model, IRectangleRenderer renderer, VisibleFace visibleFace = VisibleFace.Front)
	{
		for (ushort z = 0; z < model.SizeZ; z++)
			for (ushort x = 0; x < model.SizeX; x++)
				for (ushort y = 0; y < model.SizeY; y++)
					if (model[x, y, z] is byte voxel
						&& voxel != 0)
					{
						renderer.Rect(
							x: x,
							y: (ushort)(model.SizeZ - 1 - z),
							index: voxel,
							visibleFace: visibleFace);
						break;
					}
	}
	public static int FrontPeekWidth(IModel model, byte scaleX = 6) => model.SizeX * scaleX;
	public static int FrontPeekHeight(IModel model, byte scaleY = 6) => model.SizeZ * scaleY;
	public static void FrontPeek(IModel model, IRectangleRenderer renderer, byte scaleX = 6, byte scaleY = 6)
	{
		for (ushort z = 0; z < model.SizeZ; z++)
			for (ushort x = 0; x < model.SizeX; x++)
				for (ushort y = 0; y < model.SizeY; y++)
					if (model[x, y, z] is byte voxel
						&& voxel != 0)
					{
						if (z >= model.SizeZ - 1
							|| model[x, y, (ushort)(z + 1)] == 0)
						{
							renderer.Rect(
								x: (ushort)(x * scaleX),
								y: (ushort)((model.SizeZ - 1 - z) * scaleY),
								index: voxel,
								visibleFace: VisibleFace.Top,
								sizeX: scaleX,
								sizeY: 1);
							renderer.Rect(
								x: (ushort)(x * scaleX),
								y: (ushort)((model.SizeZ - 1 - z) * scaleY + 1),
								index: voxel,
								visibleFace: VisibleFace.Front,
								sizeX: scaleX,
								sizeY: (ushort)(scaleY - 1));
						}
						else
							renderer.Rect(
								x: (ushort)(x * scaleX),
								y: (ushort)((model.SizeZ - 1 - z) * scaleY),
								index: voxel,
								visibleFace: VisibleFace.Front,
								sizeX: scaleX,
								sizeY: scaleY);
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
		for (ushort pixelY = 0; pixelY < model.SizeZ; pixelY++)
			for (ushort pixelX = 0; pixelX < pixelWidth; pixelX += 2)
			{
				bool leftDone = false,
					rightDone = false;
				ushort startX = (ushort)Math.Max(pixelX - model.SizeY + 1, 0),
					startY = (ushort)Math.Max(model.SizeY - 1 - pixelX, 0),
					voxelZ = (ushort)(model.SizeZ - 1 - pixelY);
				if (pixelX >= pixelWidth - 1
					&& model[(ushort)(model.SizeX - 1), 0, voxelZ] is byte rightEdge
					&& rightEdge != 0)
				{
					renderer.Rect(
						x: pixelX,
						y: pixelY,
						index: rightEdge,
						visibleFace: VisibleFace.Right);
					continue;
				}
				for (ushort voxelX = startX, voxelY = startY;
					 voxelX <= model.SizeX && voxelY <= model.SizeY && !(leftDone && rightDone);
					 voxelX++, voxelY++)
				{
					if (!leftDone
						&& voxelX > 0
						&& model[(ushort)(voxelX - 1), voxelY, voxelZ] is byte voxelRight
						&& voxelRight != 0)
					{
						renderer.Rect(
							x: pixelX,
							y: pixelY,
							index: voxelRight,
							visibleFace: VisibleFace.Right);
						leftDone = true;
					}
					if (!rightDone
						&& voxelY > 0
						&& model[voxelX, (ushort)(voxelY - 1), voxelZ] is byte voxelLeft
						&& voxelLeft != 0)
					{
						renderer.Rect(
							x: (ushort)(pixelX + 1),
							y: pixelY,
							index: voxelLeft,
							visibleFace: VisibleFace.Left);
						rightDone = true;
					}
					if (leftDone && rightDone) break;
					if (model[voxelX, voxelY, voxelZ] is byte voxel
						&& voxel != 0)
					{
						if (!leftDone)
							renderer.Rect(
								x: pixelX,
								y: pixelY,
								index: voxel,
								visibleFace: VisibleFace.Left);
						if (!rightDone)
							renderer.Rect(
								x: (ushort)(pixelX + 1),
								y: pixelY,
								index: voxel,
								visibleFace: VisibleFace.Right);
						break;
					}
				}
			}
	}
	public static int DiagonalPeekWidth(IModel model, byte scaleX = 4) => (model.SizeX + model.SizeY) * scaleX;
	public static int DiagonalPeekHeight(IModel model, byte scaleY = 6) => model.SizeZ * scaleY;
	public static void DiagonalPeek(IModel model, IRectangleRenderer renderer, byte scaleX = 4, byte scaleY = 6)
	{
		void DrawRight(int x, int y, byte voxel, bool peek = false)
		{
			if (peek)
			{
				renderer.Rect(
					x: (ushort)(x * scaleX),
					y: (ushort)(y * scaleY),
					index: voxel,
					visibleFace: VisibleFace.Top,
					sizeX: scaleX,
					sizeY: 1);
				renderer.Rect(
					x: (ushort)(x * scaleX),
					y: (ushort)(y * scaleY + 1),
					index: voxel,
					visibleFace: VisibleFace.Right,
					sizeX: scaleX,
					sizeY: (ushort)(scaleY - 1));
			}
			else
				renderer.Rect(
					x: (ushort)(x * scaleX),
					y: (ushort)(y * scaleY),
					index: voxel,
					visibleFace: VisibleFace.Right,
					sizeX: scaleX,
					sizeY: scaleY);
		}
		void DrawLeft(int x, int y, byte voxel, bool peek = false)
		{
			if (peek)
			{
				renderer.Rect(
					x: (ushort)(x * scaleX),
					y: (ushort)(y * scaleY),
					index: voxel,
					visibleFace: VisibleFace.Top,
					sizeX: scaleX,
					sizeY: 1);
				renderer.Rect(
					x: (ushort)(x * scaleX),
					y: (ushort)(y * scaleY + 1),
					index: voxel,
					visibleFace: VisibleFace.Left,
					sizeX: scaleX,
					sizeY: (ushort)(scaleY - 1));
			}
			else
				renderer.Rect(
					x: (ushort)(x * scaleX),
					y: (ushort)(y * scaleY),
					index: voxel,
					visibleFace: VisibleFace.Left,
					sizeX: scaleX,
					sizeY: scaleY);
		}
		ushort pixelWidth = (ushort)(model.SizeX + model.SizeY);
		for (ushort pixelY = 0; pixelY < model.SizeZ; pixelY++)
			for (ushort pixelX = 0; pixelX < pixelWidth; pixelX += 2)
			{
				bool leftDone = false,
					rightDone = false;
				ushort startX = (ushort)(Math.Max(pixelX - model.SizeY + 1, 0)),
					startY = (ushort)(Math.Max(model.SizeY - 1 - pixelX, 0)),
					voxelZ = (ushort)(model.SizeZ - 1 - pixelY);
				if (pixelX >= pixelWidth - 1
					&& model[(ushort)(model.SizeX - 1), 0, voxelZ] is byte rightEdge
					&& rightEdge != 0)
				{
					DrawRight(
						x: pixelX,
						y: pixelY,
						voxel: rightEdge,
						peek: voxelZ >= model.SizeZ
							|| (model[(ushort)(model.SizeX - 1), 0, (ushort)(voxelZ + 1)] is byte voxelAbove
							&& voxelAbove != 0));
					continue;
				}
				for (ushort voxelX = startX, voxelY = startY;
					 voxelX <= model.SizeX && voxelY <= model.SizeY && !(leftDone && rightDone);
					 voxelX++, voxelY++)
				{
					if (!leftDone
						&& voxelX > 0
						&& model[(ushort)(voxelX - 1), voxelY, voxelZ] is byte voxelRight
						&& voxelRight != 0)
					{
						DrawRight(
							x: pixelX,
							y: pixelY,
							voxel: voxelRight,
							peek: voxelZ == model.SizeZ - 1
								|| (model[(ushort)(voxelX - 1), voxelY, (ushort)(voxelZ + 1)] is byte voxelAbove
								&& voxelAbove == 0));
						leftDone = true;
					}
					if (!rightDone
						&& voxelY > 0
						&& model[voxelX, (ushort)(voxelY - 1), voxelZ] is byte voxelLeft
						&& voxelLeft != 0)
					{
						DrawLeft(
							x: pixelX + 1,
							y: pixelY,
							voxel: voxelLeft,
							peek: voxelZ == model.SizeZ - 1
								|| (model[voxelX, (ushort)(voxelY - 1), (ushort)(voxelZ + 1)] is byte voxelAbove
								&& voxelAbove == 0));
						rightDone = true;
					}
					if (leftDone && rightDone) break;
					if (model[voxelX, voxelY, voxelZ] is byte voxel
						&& voxel != 0)
					{
						if (!leftDone)
							DrawLeft(
								x: pixelX,
								y: pixelY,
								voxel: voxel,
								peek: voxelZ == model.SizeZ - 1
									|| (model[voxelX, voxelY, (ushort)(voxelZ + 1)] is byte voxelAbove
									&& voxelAbove == 0));
						if (!rightDone)
							DrawRight(
								x: pixelX + 1,
								y: pixelY,
								voxel: voxel,
								peek: voxelZ == model.SizeZ - 1
									|| (model[voxelX, voxelY, (ushort)(voxelZ + 1)] is byte voxelAbove
									&& voxelAbove == 0));
						break;
					}
				}
			}
	}
	public static int AboveWidth(IModel model) => model.SizeX;
	public static int AboveHeight(IModel model) => model.SizeY + model.SizeZ;
	public static void AboveLocate(out int pixelX, out int pixelY, IModel model, int voxelX = 0, int voxelY = 0, int voxelZ = 0)
	{
		pixelX = voxelX;
		pixelY = AboveHeight(model) - 1 - voxelY - voxelZ;
	}
	public static void Above(IModel model, IRectangleRenderer renderer)
	{
		ushort pixelHeight = (ushort)(model.SizeY + model.SizeZ);
		for (ushort pixelX = 0; pixelX < model.SizeX; pixelX++)
			for (ushort pixelY = 0; pixelY <= pixelHeight; pixelY += 2)
			{
				if (pixelY + 2 > pixelHeight
					&& model[pixelX, 0, 0] is byte bottomEdge
					&& bottomEdge != 0)
				{
					renderer.Rect(
						x: pixelX,
						y: pixelY,
						index: bottomEdge);
					continue;
				}
				ushort startY = (ushort)Math.Max(model.SizeY - 1 - pixelY, 0),
					startZ = (ushort)(model.SizeZ - 1 - Math.Max(pixelY + 1 - model.SizeY, 0));
				bool higher = false,
					lower = false;
				for (ushort voxelY = startY, voxelZ = startZ;
					voxelY < model.SizeY && voxelZ >= 0 && !(higher && lower);
					voxelY++, voxelZ--)
				{
					if (!higher
						&& voxelZ < model.SizeZ - 1
						&& model[pixelX, voxelY, (ushort)(voxelZ + 1)] is byte voxelAbove
						&& voxelAbove != 0)
					{
						renderer.Rect(
							x: pixelX,
							y: pixelY,
							index: voxelAbove);
						higher = true;
					}
					if (!lower
						&& voxelY > 0
						&& model[pixelX, (ushort)(voxelY - 1), voxelZ] is byte voxelFront
						&& voxelFront != 0)
					{
						renderer.Rect(
							x: pixelX,
							y: (ushort)(pixelY + 1),
							index: voxelFront,
							visibleFace: VisibleFace.Top);
						lower = true;
					}
					if (model[pixelX, voxelY, voxelZ] is byte voxel
						&& voxel != 0)
					{
						if (!higher)
							renderer.Rect(
								x: pixelX,
								y: pixelY,
								index: voxel,
								visibleFace: VisibleFace.Top);
						if (!lower)
							renderer.Rect(
								x: pixelX,
								y: (ushort)(pixelY + 1),
								index: voxel);
						break;
					}
					if (!higher
						&& voxelY < model.SizeY - 1
						&& model[pixelX, (ushort)(voxelY + 1), voxelZ] is byte voxelBack
						&& voxelBack != 0)
					{
						renderer.Rect(
							x: pixelX,
							y: pixelY,
							index: voxelBack);
						higher = true;
					}
					if (!lower
						&& voxelZ > 0
						&& model[pixelX, voxelY, (ushort)(voxelZ - 1)] is byte voxelBelow
						&& voxelBelow != 0)
					{
						renderer.Rect(
							x: pixelX,
							y: (ushort)(pixelY + 1),
							index: voxelBelow,
							visibleFace: VisibleFace.Top);
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
	public static void IsoLocate(out int pixelX, out int pixelY, IModel model, int voxelX = 0, int voxelY = 0, int voxelZ = 0)
	{
		// To move one x+ in voxels is x + 2, y - 2 in pixels.
		// To move one x- in voxels is x - 2, y + 2 in pixels.
		// To move one y+ in voxels is x - 2, y - 2 in pixels.
		// To move one y- in voxels is x + 2, y + 2 in pixels.
		// To move one z+ in voxels is y - 4 in pixels.
		// To move one z- in voxels is y + 4 in pixels.
		pixelX = 2 * (model.SizeY + voxelX - voxelY);
		pixelY = IsoHeight(model) - 2 * (voxelX + voxelY) - 4 * voxelZ - 1;
	}
	public static void Iso(IModel model, ITriangleRenderer renderer)
	{
		ushort modelSizeX2 = (ushort)(model.SizeX * 2),
			modelSizeY2 = (ushort)(model.SizeY * 2),
			modelSizeZ4 = (ushort)(model.SizeZ * 4),
			pixelWidth = (ushort)(modelSizeX2 + modelSizeY2),
			pixelHeight = (ushort)(pixelWidth + modelSizeZ4);
		bool evenSizeX = model.SizeX % 2 == 0,
			evenSizeY = model.SizeY % 2 == 0;
		bool[] tiles = new bool[4];
		#region Isometric local functions
		bool IsOutside(int x, int y, int z) => x < 0 || y < 0 || z < 0 || model.IsOutside((ushort)x, (ushort)y, (ushort)z);
		byte At(int x, int y, int z) => model[(ushort)x, (ushort)y, (ushort)z];
		void Tile(int tile, int x, int y, byte voxel, VisibleFace visibleFace = VisibleFace.Front)
		{
			// 12
			//1122
			//0123
			//0033
			//0  3
			if (!tiles[tile])
			{
				renderer.Tri(
					x: (ushort)(x + (tile < 2 ? 0 : 2)),
					y: (ushort)(y + (tile == 0 || tile == 3 ? 2 : 0)),
					right: tile % 2 == 0,
					index: voxel,
					visibleFace: visibleFace);
				tiles[tile] = true;
			}
		}
		#endregion Isometric local functions
		#region Isometric main tiled area
		for (int pixelY = 0; pixelY < pixelHeight - 2; pixelY += 4)
		{
			int pixelStartX = pixelY < modelSizeX2 + modelSizeZ4 ?
					modelSizeX2 - pixelY - 1 > 0 ?
						modelSizeX2 - pixelY - 2
						: (evenSizeX ? -2 : 0) + (pixelY % 4 < 2 ? 0 : 1)
					: pixelY - modelSizeX2 - modelSizeZ4 + 2,
				pixelStopX = pixelY < modelSizeY2 + modelSizeZ4 ?
					Math.Min(modelSizeX2 + pixelY + 2, pixelWidth - 1)
					: pixelWidth + modelSizeY2 + modelSizeZ4 - pixelY - 2;
			for (int pixelX = pixelStartX; pixelX < pixelStopX; pixelX += 4)
			{
				bool right = ((pixelX >> 1) + (pixelY >> 1) & 1) == (evenSizeX ? 0 : 1),
					startAtTop = pixelY < modelSizeX2 + modelSizeY2
						&& pixelX > pixelY - modelSizeX2
						&& pixelX < pixelWidth + modelSizeY2 - pixelY - 2,
					startAtLeft = !startAtTop && pixelX < modelSizeY2;
				int halfX = pixelX / 2,
					halfY = pixelY / 2,
					startX = startAtTop ? model.SizeX - 1 - (halfY - halfX + model.SizeX) / 2
						: startAtLeft ? 0
						: halfX - model.SizeY + 1,
					startY = startAtTop ? model.SizeY - 1 - (halfX + halfY - model.SizeX + 1) / 2
						: startAtLeft ? model.SizeY - 1 - halfX
						: 0,
					startZ = startAtTop ? model.SizeZ - 1
						: startAtLeft ? model.SizeZ - 2 - (halfY - halfX - model.SizeX) / 2
						: model.SizeY + model.SizeZ - (halfY + halfX - model.SizeX - 1) / 2 - 3;
				Array.Clear(tiles, 0, tiles.Length);
				for (int voxelX = startX, voxelY = startY, voxelZ = startZ;
						voxelX <= model.SizeX && voxelY <= model.SizeY && voxelZ >= 0 && tiles.Any(@bool => !@bool);
						voxelX++, voxelY++, voxelZ--)
				{
					if ((!tiles[0] || !tiles[1])
						&& !IsOutside(voxelX - 1, voxelY, voxelZ + 1)
						&& At(voxelX - 1, voxelY, voxelZ + 1) is byte xMinus1zPlus1
						&& xMinus1zPlus1 != 0)
					{
						Tile(tile: 0,
							x: pixelX,
							y: pixelY,
							voxel: xMinus1zPlus1,
							visibleFace: VisibleFace.Right);
						Tile(tile: 1,
							x: pixelX,
							y: pixelY,
							voxel: xMinus1zPlus1,
							visibleFace: VisibleFace.Right);
					}
					if ((!tiles[2] || !tiles[3])
						&& !IsOutside(voxelX, voxelY - 1, voxelZ + 1)
						&& At(voxelX, voxelY - 1, voxelZ + 1) is byte yMinus1zPlus1
						&& yMinus1zPlus1 != 0)
					{
						Tile(tile: 2,
							x: pixelX,
							y: pixelY,
							voxel: yMinus1zPlus1,
							visibleFace: VisibleFace.Left);
						Tile(tile: 3,
							x: pixelX,
							y: pixelY,
							voxel: yMinus1zPlus1,
							visibleFace: VisibleFace.Left);
					}
					if ((!tiles[1] || !tiles[2])
						&& !IsOutside(voxelX, voxelY, voxelZ + 1)
						&& At(voxelX, voxelY, voxelZ + 1) is byte zPlus1
						&& zPlus1 != 0)
					{
						Tile(tile: 1,
							x: pixelX,
							y: pixelY,
							voxel: zPlus1,
							visibleFace: VisibleFace.Left);
						Tile(tile: 2,
							x: pixelX,
							y: pixelY,
							voxel: zPlus1,
							visibleFace: VisibleFace.Right);
					}
					if (!tiles[0]
						&& !IsOutside(voxelX - 1, voxelY, voxelZ)
						&& At(voxelX - 1, voxelY, voxelZ) is byte xMinus1
						&& xMinus1 != 0)
						Tile(tile: 0,
							x: pixelX,
							y: pixelY,
							voxel: xMinus1,
							visibleFace: VisibleFace.Top);
					if (!tiles[3]
						&& !IsOutside(voxelX, voxelY - 1, voxelZ)
						&& At(voxelX, voxelY - 1, voxelZ) is byte yMinus1
						&& yMinus1 != 0)
						Tile(tile: 3,
							x: pixelX,
							y: pixelY,
							voxel: yMinus1,
							visibleFace: VisibleFace.Top);
					if (!IsOutside(voxelX, voxelY, voxelZ)
						&& At(voxelX, voxelY, voxelZ) is byte voxel
						&& voxel != 0)
					{
						Tile(tile: 0,
							x: pixelX,
							y: pixelY,
							voxel: voxel,
							visibleFace: VisibleFace.Left);
						Tile(tile: 1,
							x: pixelX,
							y: pixelY,
							voxel: voxel,
							visibleFace: VisibleFace.Top);
						Tile(tile: 2,
							x: pixelX,
							y: pixelY,
							voxel: voxel,
							visibleFace: VisibleFace.Top);
						Tile(tile: 3,
							x: pixelX,
							y: pixelY,
							voxel: voxel,
							visibleFace: VisibleFace.Right);
						break;
					}
					if ((!tiles[0] || !tiles[1])
						&& !IsOutside(voxelX, voxelY + 1, voxelZ)
						&& At(voxelX, voxelY + 1, voxelZ) is byte yPlus1
						&& yPlus1 != 0)
					{
						Tile(tile: 0,
							x: pixelX,
							y: pixelY,
							voxel: yPlus1,
							visibleFace: VisibleFace.Right);
						Tile(tile: 1,
							x: pixelX,
							y: pixelY,
							voxel: yPlus1,
							visibleFace: VisibleFace.Right);
					}
					if ((!tiles[2] || !tiles[3])
						&& !IsOutside(voxelX + 1, voxelY, voxelZ)
						&& At(voxelX + 1, voxelY, voxelZ) is byte xPlus1
						&& xPlus1 != 0)
					{
						Tile(tile: 2,
							x: pixelX,
							y: pixelY,
							voxel: xPlus1,
							visibleFace: VisibleFace.Left);
						Tile(tile: 3,
							x: pixelX,
							y: pixelY,
							voxel: xPlus1,
							visibleFace: VisibleFace.Left);
					}
					if ((!tiles[1] || !tiles[2])
						&& !IsOutside(voxelX + 1, voxelY + 1, voxelZ)
						&& At(voxelX + 1, voxelY + 1, voxelZ) is byte xPlus1yPlus1
						&& xPlus1yPlus1 != 0)
					{
						Tile(tile: 1,
							x: pixelX,
							y: pixelY,
							voxel: xPlus1yPlus1,
							visibleFace: VisibleFace.Left);
						Tile(tile: 2,
							x: pixelX,
							y: pixelY,
							voxel: xPlus1yPlus1,
							visibleFace: VisibleFace.Right);
					}
					if (!tiles[0]
						&& !IsOutside(voxelX, voxelY + 1, voxelZ - 1)
						&& At(voxelX, voxelY + 1, voxelZ - 1) is byte yPlus1zMinus1
						&& yPlus1zMinus1 != 0)
						Tile(tile: 0,
							x: pixelX,
							y: pixelY,
							voxel: yPlus1zMinus1,
							visibleFace: VisibleFace.Top);
					if (!tiles[3]
						&& !IsOutside(voxelX + 1, voxelY, voxelZ - 1)
						&& At(voxelX + 1, voxelY, voxelZ - 1) is byte xPlus1zMinus1
						&& xPlus1zMinus1 != 0)
						Tile(tile: 3,
							x: pixelX,
							y: pixelY,
							voxel: xPlus1zMinus1,
							visibleFace: VisibleFace.Top);
				}
			}
		}
		#endregion Isometric main tiled area
		#region Isometric top left edge
		for (ushort pixelX = (ushort)(evenSizeX ? 0 : 2), pixelY = (ushort)(modelSizeX2 - (evenSizeX ? 2 : 4));
			pixelX < modelSizeX2 && pixelY > 1;
			pixelX += 4, pixelY -= 4)
		{
			int voxelX = model.SizeX - 1 - (pixelY / 2 - pixelX / 2 + model.SizeX) / 2;
			if (!IsOutside(voxelX, model.SizeY - 1, model.SizeZ - 1)
				&& At(voxelX, model.SizeY - 1, model.SizeZ - 1) is byte voxel
				&& voxel != 0)
				renderer.Tri(
					x: pixelX,
					y: pixelY,
					right: false,
					index: voxel,
					visibleFace: VisibleFace.Top);
		}
		#endregion Isometric top left edge
		#region Isometric top right edge
		for (ushort pixelX = (ushort)(modelSizeX2 + 2), pixelY = 2;
			pixelX < pixelWidth && pixelY < modelSizeY2;
			pixelX += 4, pixelY += 4)
		{
			int voxelY = model.SizeY - 1 - (pixelX / 2 + pixelY / 2 - model.SizeX + 1) / 2;
			if (!IsOutside(model.SizeX - 1, voxelY, model.SizeZ - 1)
				&& At(model.SizeX - 1, voxelY, model.SizeZ - 1) is byte voxel
				&& voxel != 0)
				renderer.Tri(
					x: pixelX,
					y: pixelY,
					right: true,
					index: voxel,
					visibleFace: VisibleFace.Top);
		}
		#endregion Isometric top right edge
		#region Isometric bottom left edge
		for (ushort pixelX = (ushort)(evenSizeX ? 0 : -2), pixelY = (ushort)(modelSizeX2 + modelSizeZ4 - (evenSizeX ? 4 : 6));
			pixelX < modelSizeY2 && pixelY < pixelHeight;
			pixelX += 4, pixelY += 4)
		{
			int voxelY = model.SizeY - 1 - pixelX / 2;
			if (pixelX >= 2
				&& !IsOutside(0, voxelY + 1, 0)
				&& At(0, voxelY + 1, 0) is byte leftVoxel
				&& leftVoxel != 0)
			{
				renderer.Tri(
					x: (ushort)(pixelX - 2),
					y: pixelY,
					right: false,
					index: leftVoxel,
					visibleFace: VisibleFace.Left);
				renderer.Tri(
					x: pixelX,
					y: pixelY,
					right: true,
					index: leftVoxel,
					visibleFace: VisibleFace.Right);
			}
			if (!IsOutside(0, voxelY, 0)
				&& At(0, voxelY, 0) is byte voxel
				&& voxel != 0)
			{
				renderer.Tri(
					x: pixelX,
					y: pixelY,
					right: true,
					index: voxel,
					visibleFace: VisibleFace.Left);
				renderer.Tri(
					x: pixelX,
					y: (ushort)(pixelY + 2),
					right: false,
					index: voxel,
					visibleFace: VisibleFace.Left);
			}
		}
		#endregion Isometric bottom left edge
		#region Isometric bottom right edge
		for (ushort pixelX = (ushort)(modelSizeY2 + (evenSizeX != evenSizeY ? 0 : 2)), pixelY = (ushort)(modelSizeX2 + modelSizeZ4 + modelSizeY2 - (evenSizeX != evenSizeY ? 6 : 8));
			pixelX < pixelWidth;
			pixelX += 4, pixelY -= 4)
		{
			int voxelX = pixelX / 2 - model.SizeY;
			if (pixelX < pixelWidth - 2
				&& !IsOutside(voxelX + 1, 0, 0)
				&& At(voxelX + 1, 0, 0) is byte rightVoxel
				&& rightVoxel != 0)
			{
				renderer.Tri(
					x: pixelX,
					y: pixelY,
					right: false,
					index: rightVoxel,
					visibleFace: VisibleFace.Left);
				renderer.Tri(
					x: (ushort)(pixelX + 2),
					y: pixelY,
					right: true,
					index: rightVoxel,
					visibleFace: VisibleFace.Right);
			}
			if (!IsOutside(voxelX, 0, 0)
				&& At(voxelX, 0, 0) is byte voxel
				&& voxel != 0)
			{
				renderer.Tri(
					x: pixelX,
					y: pixelY,
					right: false,
					index: voxel,
					visibleFace: VisibleFace.Right);
				renderer.Tri(
					x: pixelX,
					y: (ushort)(pixelY + 2),
					right: true,
					index: voxel,
					visibleFace: VisibleFace.Right);
			}
		}
		#endregion Isometric bottom right edge
		#region Isometric origin
		if (evenSizeX == evenSizeY
			&& model[0, 0, 0] is byte origin
			&& origin != 0)
		{
			renderer.Tri(
				x: (ushort)(modelSizeY2 - 2),
				y: (ushort)(pixelHeight - 4),
				right: false,
				index: origin,
				visibleFace: VisibleFace.Left);
			renderer.Tri(
				x: modelSizeY2,
				y: (ushort)(pixelHeight - 4),
				right: true,
				index: origin,
				visibleFace: VisibleFace.Right);
		}
		#endregion Isometric origin
	}
	#endregion Isometric
}
