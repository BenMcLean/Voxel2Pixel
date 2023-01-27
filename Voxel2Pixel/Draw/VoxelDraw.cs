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
								x: y,
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
								x: model.SizeY - 1 - y,
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
			for (int y = 0; y < model.SizeY; y++)
				for (int x = 0; x < model.SizeX; x++)
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
		/*
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
				if (voxel != 0)
					renderer.RectRight(
						x: voxelY * scaleX,
						y: 0,
						voxel: voxel,
						sizeX: scaleX,
						sizeY: scaleY);
				// Finish bottom row
				// Begin main bulk of model
				for (int pixelY = 1; pixelY < pixelHeight; pixelY += 2)
				{ // pixel y
					bool below = false,
						above = pixelHeight - pixelY < 2;
					int startX = (pixelY / 2) > model.SizeZ - 1 ? (pixelY / 2) - model.SizeZ + 1 : 0,
						startZ = (pixelY / 2) > model.SizeZ - 1 ? model.SizeZ - 1 : (pixelY / 2);
					for (int voxelX = startX, voxelZ = startZ;
						 voxelX <= model.SizeX && voxelZ >= -1;
						 voxelX++, voxelZ--)
					{ // vx is voxel x, vz is voxel z
						if (!above
							&& voxelZ + 1 < model.SizeZ
							&& voxelX < model.SizeX)
						{
							voxel = (byte)model.At(voxelX, voxelY, voxelZ + 1);
							if (voxel != 0)
							{
								renderer.RectRight(
									x: voxelY * scaleX,
									y: (pixelY + 1) * scaleY,
									voxel: voxel,
									sizeX: scaleX,
									sizeY: scaleY);
								above = true;
							}
						}
						if (!below && voxelX > 0 && voxelZ >= 0)
						{
							voxel = (byte)model.At(voxelX - 1, voxelY, voxelZ);
							if (voxel != 0)
							{
								renderer.RectVertical(
									x: voxelY * scaleX,
									y: pixelY * scaleY,
									voxel: voxel,
									sizeX: scaleX,
									sizeY: scaleY);
								below = true;
							}
						}
						if ((above && below)
							|| voxelX >= model.SizeX
							|| voxelZ < 0)
							break;
						voxel = (byte)model.At(voxelX, voxelY, voxelZ);
						if (voxel != 0)
						{
							if (!above)
								renderer.RectVertical(
									x: voxelY * scaleX,
									y: (pixelY + 1) * scaleY,
									voxel: voxel,
									sizeX: scaleX,
									sizeY: scaleY);
							if (!below)
								renderer.RectRight(
									x: voxelY * scaleX,
									y: pixelY * scaleY,
									voxel: voxel,
									sizeX: scaleX,
									sizeY: scaleY);
							break;
						}
					}
				}
				// Finish main bulk of model
			}
		}
		public static int IsoHeight(IModel model) => (model.SizeZ + Math.Max(model.SizeX, model.SizeY)) * 4;
		public static int IsoWidth(IModel model) => (model.SizeX + model.SizeY) * 2;
		public static void DrawIso(IModel model, ITriangleRenderer renderer)
		{
			byte voxel;
			int sizeVoxelX = model.SizeX,
				sizeVoxelY = model.SizeY,
				sizeVoxelZ = model.SizeZ,
				sizeVoxelX2 = sizeVoxelX * 2,
				sizeVoxelY2 = sizeVoxelY * 2,
				pixelWidth = IsoWidth(model);
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
						renderer.DrawRightTriangleLeftFace(pixelX + 2, bottomPixelY - 4, voxel);
						renderer.DrawLeftTriangleLeftFace(pixelX + 2, bottomPixelY - 6, voxel);
						rightEmpty = false;
					}
					voxel = (byte)model.At(sizeVoxelX - pixelX / 2 - 1, 0, 0); // Center
					if (voxel != 0)
					{
						renderer.DrawLeftTriangleLeftFace(pixelX, bottomPixelY - 4, voxel);
						if (rightEmpty)
							renderer.DrawRightTriangleRightFace(pixelX + 2, bottomPixelY - 4, voxel);
					}
				}
				else if (pixelX > sizeVoxelX2 - 2)
				{ // Right side of model
					bool leftEmpty = true;
					voxel = (byte)model.At(0, pixelX / 2 - sizeVoxelX, 0); // Front left
					if (voxel != 0)
					{
						renderer.DrawRightTriangleRightFace(pixelX, bottomPixelY - 6, voxel);
						renderer.DrawLeftTriangleRightFace(pixelX, bottomPixelY - 4, voxel);
						leftEmpty = false;
					}
					voxel = (byte)model.At(0, pixelX / 2 - sizeVoxelX + 1, 0); // Center
					if (voxel != 0)
					{
						renderer.DrawRightTriangleRightFace(pixelX + 2, bottomPixelY - 4, voxel);
						if (leftEmpty)
							renderer.DrawLeftTriangleLeftFace(pixelX, bottomPixelY - 4, voxel);
					}
				}
				else
				{ // Very bottom
					voxel = (byte)model.At(0, 0, 0);
					if (voxel != 0)
					{
						renderer.DrawLeftTriangleLeftFace(pixelX, bottomPixelY - 4, voxel);
						renderer.DrawRightTriangleRightFace(pixelX + 2, bottomPixelY - 4, voxel);
						if (sizeVoxelX % 2 == 0)
							renderer.DrawRightTriangleRightFace(pixelX, bottomPixelY - 6, voxel);
					}
					else
					{
						voxel = (byte)model.At(pixelX / 2 + 1, 0, 0);
						if (voxel != 0)
							renderer.DrawLeftTriangleRightFace(pixelX, bottomPixelY - 4, voxel);
						voxel = (byte)model.At(0, pixelX / 2 - sizeVoxelX + 2, 0);
						if (voxel != 0)
							renderer.DrawRightTriangleLeftFace(pixelX + 2, bottomPixelY - 4, voxel);
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
									renderer.DrawLeftTriangleRightFace(pixelX, py, voxel);
									topLeft = true;
								}
								if (!left)
								{
									renderer.DrawRightTriangleRightFace(pixelX, py - 2, voxel);
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
									renderer.DrawRightTriangleLeftFace(pixelX + 2, py, voxel);
									topRight = true;
								}
								if (!right)
								{
									renderer.DrawLeftTriangleLeftFace(pixelX + 2, py - 2, voxel);
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
									renderer.DrawLeftTriangleLeftFace(pixelX, py, voxel);
									topLeft = true;
								}
								if (!topRight)
								{
									renderer.DrawRightTriangleRightFace(pixelX + 2, py, voxel);
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
								renderer.DrawRightTriangleVerticalFace(pixelX, py - 2, voxel);
								left = true;
							}
						}

						// x-, y, z = Front right
						if (!right && voxelX > 0)
						{
							voxel = (byte)model.At(voxelX - 1, voxelY, voxelZ);
							if (voxel != 0)
							{
								renderer.DrawLeftTriangleVerticalFace(pixelX + 2, py - 2, voxel);
								right = true;
							}
						}

						// x, y, z  = Center
						if (left && topLeft && topRight && right) break;
						voxel = (byte)model.At(voxelX, voxelY, voxelZ);
						if (voxel != 0)
						{
							if (!topLeft)
								renderer.DrawLeftTriangleVerticalFace(pixelX, py, voxel);
							if (!left)
								renderer.DrawRightTriangleLeftFace(pixelX, py - 2, voxel);
							if (!topRight)
								renderer.DrawRightTriangleVerticalFace(pixelX + 2, py, voxel);
							if (!right)
								renderer.DrawLeftTriangleRightFace(pixelX + 2, py - 2, voxel);
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
									renderer.DrawLeftTriangleRightFace(pixelX, py, voxel);
									topLeft = true;
								}
								if (!left)
								{
									renderer.DrawRightTriangleRightFace(pixelX, py - 2, voxel);
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
									renderer.DrawRightTriangleLeftFace(pixelX + 2, py, voxel);
									topRight = true;
								}
								if (!right)
								{
									renderer.DrawLeftTriangleLeftFace(pixelX + 2, py - 2, voxel);
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
									renderer.DrawRightTriangleRightFace(pixelX + 2, py, voxel);
									topRight = true;
								}
								if (!topLeft)
								{
									renderer.DrawLeftTriangleLeftFace(pixelX, py, voxel);
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
								renderer.DrawRightTriangleVerticalFace(pixelX, py - 2, voxel);
								left = true;
							}
						}

						// x, y+ z- = Below back right
						if (!right && voxelY < sizeVoxelY - 1 && voxelZ > 0)
						{
							voxel = (byte)model.At(voxelX, voxelY + 1, voxelZ - 1);
							if (voxel != 0)
							{
								renderer.DrawLeftTriangleVerticalFace(pixelX + 2, py - 2, voxel);
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
						renderer.DrawLeftTriangleVerticalFace(pixelX + 2, topPixelY, voxel);
				}
				else if (pixelX + 2 > sizeVoxelY2)
				{ // Top right triangles
					voxel = (byte)model.At(sizeVoxelY - 1 + sizeVoxelX - pixelX / 2, sizeVoxelY - 1, sizeVoxelZ - 1);
					if (voxel != 0)
						renderer.DrawRightTriangleVerticalFace(pixelX, topPixelY, voxel);
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
						renderer.DrawRightTriangleRightFace(pixelWidth + 2, bottom - 4, voxel); // lower right corner
					for (int pixelY = bottom; pixelY < bottom + sizeVoxelZ * 4; pixelY += 4)
					{
						int voxelZ = (pixelY - bottom) / 4;
						bool aboveEmpty = true;
						if (voxelZ != sizeVoxelZ - 1)
						{
							voxel = (byte)model.At(voxelX, voxelY, voxelZ + 1);
							if (voxel != 0)
							{
								renderer.DrawRightTriangleRightFace(pixelWidth + 2, pixelY, voxel);
								aboveEmpty = false;
							}
						}
						voxel = (byte)model.At(voxelX, voxelY, voxelZ);
						if (voxel != 0)
						{
							renderer.DrawLeftTriangleRightFace(pixelWidth + 2, pixelY - 2, voxel);
							if (aboveEmpty)
								renderer.DrawRightTriangleVerticalFace(pixelWidth + 2, pixelY, voxel);
						}
					}
				}
				// Finish drawing right edge
			}
		}
		*/
	}
}
