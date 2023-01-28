﻿using System;
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
		public static int DrawWidth(IModel model) => model.SizeX;
		public static int DrawHeight(IModel model) => model.SizeZ;
		public static void Draw(IModel model, IRectangleRenderer renderer)
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
		public static int DrawPeekWidth(IModel model, int scaleX = 6) => model.SizeX * scaleX;
		public static int DrawPeekHeight(IModel model, int scaleY = 6) => model.SizeZ * scaleY;
		public static void DrawPeek(IModel model, IRectangleRenderer renderer, int scaleX = 6, int scaleY = 6)
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
		public static int DrawRightWidth(IModel model) => model.SizeY;
		public static int DrawRightHeight(IModel model) => model.SizeZ;
		public static void DrawRight(IModel model, IRectangleRenderer renderer)
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
		public static int DrawRightPeekWidth(IModel model, int scaleX = 6) => model.SizeY * scaleX;
		public static int DrawRightPeekHeight(IModel model, int scaleY = 6) => model.SizeZ * scaleY;
		public static void DrawRightPeek(IModel model, IRectangleRenderer renderer, int scaleX = 6, int scaleY = 6)
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
		public static int DrawBackWidth(IModel model) => model.SizeX;
		public static int DrawBackHeight(IModel model) => model.SizeZ;
		public static void DrawBack(IModel model, IRectangleRenderer renderer)
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
		public static int DrawBackPeekWidth(IModel model, int scaleX = 6) => model.SizeX * scaleX;
		public static int DrawBackPeekHeight(IModel model, int scaleY = 6) => model.SizeZ * scaleY;
		public static void DrawBackPeek(IModel model, IRectangleRenderer renderer, int scaleX = 6, int scaleY = 6)
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
		public static int DrawLeftWidth(IModel model) => model.SizeY;
		public static int DrawLeftHeight(IModel model) => model.SizeZ;
		public static void DrawLeft(IModel model, IRectangleRenderer renderer)
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
		public static int DrawLeftPeekWidth(IModel model, int scaleX = 6) => model.SizeY * scaleX;
		public static int DrawLeftPeekHeight(IModel model, int scaleY = 6) => model.SizeZ * scaleY;
		public static void DrawLeftPeek(IModel model, IRectangleRenderer renderer, int scaleX = 6, int scaleY = 6)
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
		public static int DrawTopWidth(IModel model) => model.SizeX;
		public static int DrawTopHeight(IModel model) => model.SizeY;
		public static void DrawTop(IModel model, IRectangleRenderer renderer)
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
		public static int DrawBottomWidth(IModel model) => model.SizeX;
		public static int DrawBottomHeight(IModel model) => model.SizeY;
		public static void DrawBottom(IModel model, IRectangleRenderer renderer)
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
		public static int DrawIsoHeight(IModel model) => (model.SizeZ + Math.Max(model.SizeX, model.SizeY)) * 4;
		public static int DrawIsoWidth(IModel model) => (model.SizeX + model.SizeY) * 2;
		public static void DrawIso(IModel model, ITriangleRenderer renderer)
		{
			byte voxel;
			int sizeVoxelX = model.SizeX,
				sizeVoxelY = model.SizeY,
				sizeVoxelZ = model.SizeZ,
				sizeVoxelX2 = sizeVoxelX * 2,
				sizeVoxelY2 = sizeVoxelY * 2,
				pixelWidth = (model.SizeX + model.SizeY) * 2,
				pixelHeight = (model.SizeZ + Math.Max(model.SizeX, model.SizeY)) * 4;
			// To move one x+ in voxels is x + 2, y - 2 in pixels.
			// To move one x- in voxels is x - 2, y + 2 in pixels.
			// To move one y+ in voxels is x + 2, y + 2 in pixels.
			// To move one y- in voxels is x - 2, y - 2 in pixels.
			// To move one z+ in voxels is y + 4 in pixels.
			// To move one z- in voxels is y - 4 in pixels.
			for (int pixelX = 0; pixelX < pixelWidth; pixelX += 4)
			{
				int bottomPixelY = Math.Abs(sizeVoxelX2 - 2 - pixelX),
						topPixelY = sizeVoxelX2 - 2 + // black space at the bottom from the first column
							(sizeVoxelZ - 1) * 4 + // height of model
							sizeVoxelY2 - Math.Abs(sizeVoxelY2 - 2 - pixelX);

				// Begin drawing bottom row triangles
				if (pixelX < sizeVoxelX2 - 2)
				{ // Left side of model
					bool rightEmpty = true;
					voxel = (byte)model.At(sizeVoxelX - pixelX / 2 - 2, 0, 0); // Front right
					if (voxel != 0)
					{
						renderer.DrawRightTriangleLeftFace(
							x: pixelX + 2,
							y: bottomPixelY - 4,
							voxel: voxel);
						renderer.DrawLeftTriangleLeftFace(
							x: pixelX + 2,
							y: bottomPixelY - 6,
							voxel: voxel);
						rightEmpty = false;
					}
					voxel = (byte)model.At(sizeVoxelX - pixelX / 2 - 1, 0, 0); // Center
					if (voxel != 0)
					{
						renderer.DrawLeftTriangleLeftFace(
							x: pixelX,
							y: bottomPixelY - 4,
							voxel: voxel);
						if (rightEmpty)
							renderer.DrawRightTriangleRightFace(
								x: pixelX + 2,
								y: bottomPixelY - 4,
								voxel: voxel);
					}
				}
				else if (pixelX > sizeVoxelX2 - 2)
				{ // Right side of model
					bool leftEmpty = true;
					voxel = (byte)model.At(0, pixelX / 2 - sizeVoxelX, 0); // Front left
					if (voxel != 0)
					{
						renderer.DrawRightTriangleRightFace(
							x: pixelX,
							y: bottomPixelY - 6,
							voxel: voxel);
						renderer.DrawLeftTriangleRightFace(
							x: pixelX,
							y: bottomPixelY - 4,
							voxel: voxel);
						leftEmpty = false;
					}
					voxel = (byte)model.At(0, pixelX / 2 - sizeVoxelX, 0); // Center
					if (voxel != 0)
					{
						renderer.DrawRightTriangleRightFace(
							x: pixelX + 2,
							y: bottomPixelY - 4,
							voxel: voxel);
						if (leftEmpty)
							renderer.DrawLeftTriangleLeftFace(
								x: pixelX,
								y: bottomPixelY - 4,
								voxel: voxel);
					}
				}
				else
				{ // Very bottom
					voxel = (byte)model.At(0, 0, 0);
					if (voxel != 0)
					{
						renderer.DrawLeftTriangleLeftFace(
							x: pixelX,
							y: bottomPixelY - 4,
							voxel: voxel);
						renderer.DrawRightTriangleRightFace(
							x: pixelX + 2,
							y: bottomPixelY - 4,
							voxel: voxel);
						if (sizeVoxelX % 2 == 0)
							renderer.DrawRightTriangleRightFace(
								x: pixelX,
								y: bottomPixelY - 6,
								voxel: voxel);
					}
					else
					{
						voxel = (byte)model.At(pixelX / 2 + 1, 0, 0);
						if (voxel != 0)
							renderer.DrawLeftTriangleRightFace(
								x: pixelX,
								y: bottomPixelY - 4,
								voxel: voxel);
						voxel = (byte)model.At(0, pixelX / 2 - sizeVoxelX + 2, 0);
						if (voxel != 0)
							renderer.DrawRightTriangleLeftFace(
								x: pixelX + 2,
								y: bottomPixelY - 4,
								voxel: voxel);
					}
				}
				// Finish drawing bottom row triangles

				// Begin drawing main bulk of model
				for (int py = bottomPixelY - 4; py <= topPixelY; py += 4)
				{
					bool topSide = py > bottomPixelY + (sizeVoxelZ - 1) * 4, bottomSide = !topSide;
					int additive = (py - bottomPixelY) / 4 - sizeVoxelZ + 1,
						startVoxelX = (pixelX < sizeVoxelX2 ? sizeVoxelX - 1 - pixelX / 2 : 0) + (topSide ? additive : 0),
						startVoxelY = (pixelX < sizeVoxelX2 ? 0 : pixelX / 2 - sizeVoxelX + 1) + (topSide ? additive : 0),
						startVoxelZ = bottomSide ? (py - bottomPixelY) / 4 : sizeVoxelZ - 1;

					bool left = false,
						topLeft = false,
						topRight = false,
						right = false;
					for (int voxelX = startVoxelX, voxelY = startVoxelY, voxelZ = startVoxelZ;
						 voxelX < sizeVoxelX && voxelY < sizeVoxelY && voxelZ >= 0;
						 voxelX++, voxelY++, voxelZ--)
					{

						// Order to check
						// x, y-, z+ = Above front left
						// x-, y, z+ = Above front right
						// x, y, z+ = Above
						// x, y-, z = Front left
						// x-, y, z = Front right
						// x, y, z  = Center
						// x+, y, z = Back left
						// x, y+, z = Back right
						// x+, y, z- = Below back left
						// x, y+ z- = Below back right

						// OK here goes:
						// x, y-, z+ = Above front left
						if ((!left || !topLeft) && voxelX == 0 && voxelY > 0 && voxelZ < sizeVoxelZ - 1)
						{
							voxel = (byte)model.At(voxelX, voxelY - 1, voxelZ + 1);
							if (voxel != 0)
							{
								if (!topLeft)
								{
									renderer.DrawLeftTriangleRightFace(
										x: pixelX,
										y: py,
										voxel: voxel);
									topLeft = true;
								}
								if (!left)
								{
									renderer.DrawRightTriangleRightFace(
										x: pixelX,
										y: py - 2,
										voxel: voxel);
									left = true;
								}
							}
						}

						// x-, y, z+ = Above front right
						if ((!topRight || !right) && voxelX > 0 && voxelY == 0 && voxelZ < sizeVoxelZ - 1)
						{
							voxel = (byte)model.At(voxelX - 1, voxelY, voxelZ + 1);
							if (voxel != 0)
							{
								if (!topRight)
								{
									renderer.DrawRightTriangleLeftFace(
										x: pixelX + 2,
										y: py,
										voxel: voxel);
									topRight = true;
								}
								if (!right)
								{
									renderer.DrawLeftTriangleLeftFace(
										x: pixelX + 2,
										y: py - 2,
										voxel: voxel);
									right = true;
								}
							}
						}

						// x, y, z+ = Above
						if ((!topLeft || !topRight) && voxelZ < sizeVoxelZ - 1)
						{
							voxel = (byte)model.At(voxelX, voxelY, voxelZ + 1);
							if (voxel != 0)
							{
								if (!topLeft)
								{
									renderer.DrawLeftTriangleLeftFace(
										x: pixelX,
										y: py,
										voxel: voxel);
									topLeft = true;
								}
								if (!topRight)
								{
									renderer.DrawRightTriangleRightFace(
										x: pixelX + 2,
										y: py,
										voxel: voxel);
									topRight = true;
								}
							}
						}

						// x, y-, z = Front left
						if (!left && voxelY > 0)
						{
							voxel = (byte)model.At(voxelX, voxelY - 1, voxelZ);
							if (voxel != 0)
							{
								renderer.DrawRightTriangleVerticalFace(
									x: pixelX,
									y: py - 2,
									voxel: voxel);
								left = true;
							}
						}

						// x-, y, z = Front right
						if (!right && voxelX > 0)
						{
							voxel = (byte)model.At(voxelX - 1, voxelY, voxelZ);
							if (voxel != 0)
							{
								renderer.DrawLeftTriangleVerticalFace(
									x: pixelX + 2,
									y: py - 2,
									voxel: voxel);
								right = true;
							}
						}

						// x, y, z  = Center
						if (left && topLeft && topRight && right) break;
						voxel = (byte)model.At(voxelX, voxelY, voxelZ);
						if (voxel != 0)
						{
							if (!topLeft)
								renderer.DrawLeftTriangleVerticalFace(
									x: pixelX,
									y: py,
									voxel: voxel);
							if (!left)
								renderer.DrawRightTriangleLeftFace(
									x: pixelX,
									y: py - 2,
									voxel: voxel);
							if (!topRight)
								renderer.DrawRightTriangleVerticalFace(
									x: pixelX + 2,
									y: py,
									voxel: voxel);
							if (!right)
								renderer.DrawLeftTriangleRightFace(
									x: pixelX + 2,
									y: py - 2,
									voxel: voxel);
							break;
						}

						// x+, y, z = Back left
						if ((!left || !topLeft) && voxelX < sizeVoxelX - 1)
						{
							voxel = (byte)model.At(voxelX + 1, voxelY, voxelZ);
							if (voxel != 0)
							{
								if (!topLeft)
								{
									renderer.DrawLeftTriangleRightFace(
										x: pixelX,
										y: py,
										voxel: voxel);
									topLeft = true;
								}
								if (!left)
								{
									renderer.DrawRightTriangleRightFace(
										x: pixelX,
										y: py - 2,
										voxel: voxel);
									left = true;
								}
							}
						}

						// x, y+, z = Back right
						if ((!right || !topRight) && voxelY < sizeVoxelY - 1)
						{
							voxel = (byte)model.At(voxelX, voxelY + 1, voxelZ);
							if (voxel != 0)
							{
								if (!topRight)
								{
									renderer.DrawRightTriangleLeftFace(
										x: pixelX + 2,
										y: py,
										voxel: voxel);
									topRight = true;
								}
								if (!right)
								{
									renderer.DrawLeftTriangleLeftFace(
										x: pixelX + 2,
										y: py - 2,
										voxel: voxel);
									right = true;
								}
							}
						}

						// x+, y+ z = Back center
						if ((!topLeft || !topRight) && voxelX < sizeVoxelX - 1 && voxelY < sizeVoxelY - 1)
						{
							voxel = (byte)model.At(voxelX + 1, voxelY + 1, voxelZ);
							if (voxel != 0)
							{
								if (!topRight)
								{
									renderer.DrawRightTriangleRightFace(
										x: pixelX + 2,
										y: py,
										voxel: voxel);
									topRight = true;
								}
								if (!topLeft)
								{
									renderer.DrawLeftTriangleLeftFace(
										x: pixelX,
										y: py,
										voxel: voxel);
									topLeft = true;
								}
							}
						}

						// x+, y, z- = Below back left
						if (!left && voxelX < sizeVoxelX - 1 && voxelZ > 0)
						{
							voxel = (byte)model.At(voxelX + 1, voxelY, voxelZ - 1);
							if (voxel != 0)
							{
								renderer.DrawRightTriangleVerticalFace(
									x: pixelX,
									y: py - 2,
									voxel: voxel);
								left = true;
							}
						}

						// x, y+ z- = Below back right
						if (!right && voxelY < sizeVoxelY - 1 && voxelZ > 0)
						{
							voxel = (byte)model.At(voxelX, voxelY + 1, voxelZ - 1);
							if (voxel != 0)
							{
								renderer.DrawLeftTriangleVerticalFace(
									x: pixelX + 2,
									y: py - 2,
									voxel: voxel);
								right = true;
							}
						}

						// Debugging
						//                    if (startVX == 10 && startVY == 0 && startVZ == 3) {
						//                        Gdx.app.log("debug", "Coord: " + vx + ", " + vy + ", " + vz);
						////                    if (!topLeft)
						//                        renderer.drawLeftTriangle(px, py, flash());
						////                    if (!left)
						//                        renderer.drawRightTriangle(px, py - 2, flash());
						////                    if (!topRight)
						//                        renderer.drawRightTriangle(px + 2, py, flash());
						////                    if (!right)
						//                        renderer.drawLeftTriangle(px + 2, py - 2, flash());
						//                    }
						// Finish debugging
					}
				}
				// Finish drawing main bulk of model

				// Begin drawing top triangles
				if (pixelX + 2 < sizeVoxelY2)
				{ // Top left triangles
					voxel = (byte)model.At(sizeVoxelX - 1, pixelX / 2 + 1, sizeVoxelZ - 1);
					if (voxel != 0)
						renderer.DrawLeftTriangleVerticalFace(
							x: pixelX + 2,
							y: topPixelY,
							voxel: voxel);
				}
				else if (pixelX + 2 > sizeVoxelY2)
				{ // Top right triangles
					voxel = (byte)model.At(sizeVoxelY - 1 + sizeVoxelX - pixelX / 2, sizeVoxelY - 1, sizeVoxelZ - 1);
					if (voxel != 0)
						renderer.DrawRightTriangleVerticalFace(
							x: pixelX,
							y: topPixelY,
							voxel: voxel);
				}
				// Finish drawing top triangles.

				// Drawing right edge (only for when sizeVX + sizeVY is odd numbered)
				if ((sizeVoxelX + sizeVoxelY) % 2 == 1)
				{
					int voxelX = 0,
						voxelY = sizeVoxelY - 1,
						bottom = Math.Abs(sizeVoxelX2 - 2 - pixelWidth);
					voxel = (byte)model.At(voxelX, voxelY, 0);
					if (voxel != 0)
						renderer.DrawRightTriangleRightFace(
							x: pixelWidth + 2,
							y: bottom - 4,
							voxel: voxel); // lower right corner
					for (int pixelY = bottom; pixelY < bottom + sizeVoxelZ * 4; pixelY += 4)
					{
						int voxelZ = (pixelY - bottom) / 4;
						bool aboveEmpty = true;
						if (voxelZ != sizeVoxelZ - 1)
						{
							voxel = (byte)model.At(voxelX, voxelY, voxelZ + 1);
							if (voxel != 0)
							{
								renderer.DrawRightTriangleRightFace(
									x: pixelWidth + 2,
									y: pixelY,
									voxel: voxel);
								aboveEmpty = false;
							}
						}
						voxel = (byte)model.At(voxelX, voxelY, voxelZ);
						if (voxel != 0)
						{
							renderer.DrawLeftTriangleRightFace(
								x: pixelWidth + 2,
								y: pixelY - 2,
								voxel: voxel);
							if (aboveEmpty)
								renderer.DrawRightTriangleVerticalFace(
									x: pixelWidth + 2,
									y: pixelY,
									voxel: voxel);
						}
					}
				}
				// Finish drawing right edge
			}
		}
		#endregion Isometric
	}
}
