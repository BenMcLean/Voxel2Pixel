﻿using System.Collections.Generic;
using System.Linq;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;

namespace Voxel2Pixel.Draw
{
	/// <summary>
	/// I have been forced into a situation where X and Y mean something different in 2D space from what they mean in 3D space. Not only do the coordinates not match, but 3D is upside down when compared to 2D. I hate this. I hate it so much. But I'm stuck with it if I want my software to be interoperable with other existing software.
	/// In 2D space for pixels, X+ means east/right, Y+ means down. This is dictated by how 2D raster graphics are typically stored.
	/// In 3D space for voxels, I'm following the MagicaVoxel convention, which is Z+up, right-handed, so X+ means east/right, Y+ means forwards/north and Z+ means up.
	/// </summary>
	public static class SparseVoxelDraw
	{
		#region Straight
		public static int FrontWidth(IModel model) => model.SizeX;
		public static int FrontHeight(IModel model) => model.SizeZ;
		private struct VoxelY
		{
			public readonly ushort Y;
			public readonly byte @byte;
			public VoxelY(Voxel voxel)
			{
				Y = voxel.Y;
				@byte = voxel.@byte;
			}
		}
		public static void Front(IModel model, IRectangleRenderer renderer, VisibleFace visibleFace = VisibleFace.Front)
		{
			ushort width = model.SizeX,
				height = model.SizeZ;
			uint index;
			VoxelY[] grid = new VoxelY[width * height];
			foreach (Voxel voxel in model
				.Where(voxel => voxel.@byte != 0))
			{
				index = (uint)(width * (height - voxel.Z - 1) + voxel.X);
				if (!(grid[index] is VoxelY old)
						|| old.@byte == 0
						|| old.Y > voxel.Y)
					grid[index] = new VoxelY(voxel);
			}
			index = 0;
			for (ushort y = 0; y < height; y++)
				for (ushort x = 0; x < width; x++)
					if (grid[index++] is VoxelY voxelY && voxelY.@byte != 0)
						renderer.Rect(
							x: x,
							y: y,
							voxel: voxelY.@byte,
							visibleFace: visibleFace);
		}
		public static void FrontPeek(IModel model, IRectangleRenderer renderer, byte scaleX = 6, byte scaleY = 6)
		{
			ushort height = model.SizeZ;
			Dictionary<uint, Voxel> dictionary = new Dictionary<uint, Voxel>();
			uint Encode(Voxel voxel) => (uint)(voxel.Z << 16) | voxel.X;
			foreach (Voxel voxel in model
				.Where(voxel => voxel.@byte != 0
					&& (!dictionary.TryGetValue(Encode(voxel), out Voxel old)
						|| old.Y > voxel.Y)))
				dictionary[Encode(voxel)] = voxel;
			foreach (Voxel voxel in dictionary.Values)
				if (voxel.Z >= height - 1
					|| model[voxel.X, voxel.Y, (ushort)(voxel.Z + 1)] == 0)
				{
					renderer.Rect(
						x: voxel.X * scaleX,
						y: (height - 1 - voxel.Z) * scaleY,
						voxel: voxel.@byte,
						visibleFace: VisibleFace.Top,
						sizeX: scaleX,
						sizeY: 1);
					renderer.Rect(
						x: voxel.X * scaleX,
						y: (height - 1 - voxel.Z) * scaleY + 1,
						voxel: voxel.@byte,
						visibleFace: VisibleFace.Front,
						sizeX: scaleX,
						sizeY: scaleY - 1);
				}
				else
					renderer.Rect(
						x: voxel.X * scaleX,
						y: (height - 1 - voxel.Z) * scaleY,
						voxel: voxel.@byte,
						visibleFace: VisibleFace.Front,
						sizeX: scaleX,
						sizeY: scaleY);
		}
		#endregion Straight
		#region Diagonal
		private struct VoxelD
		{
			public uint Distance;
			public byte @byte;
			public VisibleFace VisibleFace;
		}
		public static int DiagonalWidth(IModel model) => model.SizeX + model.SizeY;
		public static int DiagonalHeight(IModel model) => model.SizeZ;
		public static void Diagonal(IModel model, IRectangleRenderer renderer)
		{
			ushort width = model.SizeX,
				depth = model.SizeY,
				height = model.SizeZ;
			uint pixelWidth = (uint)(width + depth), index;
			VoxelD[] grid = new VoxelD[pixelWidth * height];
			foreach (Voxel voxel in model
				.Where(voxel => voxel.@byte != 0))
			{
				index = (uint)(pixelWidth * (height - voxel.Z - 1) + depth - voxel.Y - 1 + voxel.X);
				uint distance = (uint)voxel.X + voxel.Y;
				if (!(grid[index] is VoxelD left)
					|| left.@byte == 0
					|| left.Distance > distance)
					grid[index] = new VoxelD
					{
						Distance = distance,
						@byte = voxel.@byte,
						VisibleFace = VisibleFace.Left,
					};
				if (!(grid[++index] is VoxelD right)
					|| right.@byte == 0
					|| right.Distance > distance)
					grid[index] = new VoxelD
					{
						Distance = distance,
						@byte = voxel.@byte,
						VisibleFace = VisibleFace.Right,
					};
			}
			index = 0;
			for (ushort y = 0; y < height; y++)
				for (ushort x = 0; x < pixelWidth; x++)
					if (grid[index++] is VoxelD voxelD && voxelD.@byte != 0)
						renderer.Rect(
							x: x,
							y: y,
							voxel: voxelD.@byte,
							visibleFace: voxelD.VisibleFace);
		}
		public static int DiagonalPeekWidth(IModel model, byte scaleX = 4) => (model.SizeX + model.SizeY) * scaleX;
		public static int DiagonalPeekHeight(IModel model, byte scaleY = 6) => model.SizeZ * scaleY;
		public struct VoxelF
		{
			public Voxel Voxel;
			public VisibleFace VisibleFace;
			public uint Distance => (uint)Voxel.X + Voxel.Y;
		}
		public static void DiagonalPeek(IModel model, IRectangleRenderer renderer, byte scaleX = 6, byte scaleY = 6)
		{
			ushort width = model.SizeX,
				depth = model.SizeY,
				height = model.SizeZ;
			uint pixelWidth = (uint)(width + depth), index;
			VoxelF[] grid = new VoxelF[pixelWidth * height];
			foreach (Voxel voxel in model
				.Where(voxel => voxel.@byte != 0))
			{
				index = (uint)(pixelWidth * (height - voxel.Z - 1) + depth - voxel.Y - 1 + voxel.X);
				uint distance = (uint)voxel.X + voxel.Y;
				if (!(grid[index] is VoxelF left)
					|| left.Voxel.@byte == 0
					|| left.Distance > distance)
					grid[index] = new VoxelF
					{
						Voxel = voxel,
						VisibleFace = VisibleFace.Left,
					};
				if (!(grid[++index] is VoxelF right)
					|| right.Voxel.@byte == 0
					|| right.Distance > distance)
					grid[index] = new VoxelF
					{
						Voxel = voxel,
						VisibleFace = VisibleFace.Right,
					};
			}
			index = 0;
			for (ushort y = 0; y < height; y++)
				for (ushort x = 0; x < pixelWidth; x++)
					if (grid[index++] is VoxelF f && f.Voxel.@byte != 0)
						if (f.Voxel.Z >= height - 1
							|| model[f.Voxel.X, f.Voxel.Y, (ushort)(f.Voxel.Z + 1)] == 0)
						{
							renderer.Rect(
								x: x * scaleX,
								y: y * scaleY,
								voxel: f.Voxel.@byte,
								visibleFace: VisibleFace.Top,
								sizeX: scaleX,
								sizeY: 1);
							renderer.Rect(
								x: x * scaleX,
								y: y * scaleY + 1,
								voxel: f.Voxel.@byte,
								visibleFace: f.VisibleFace,
								sizeX: scaleX,
								sizeY: scaleY - 1);
						}
						else
							renderer.Rect(
								x: x * scaleX,
								y: y * scaleY,
								voxel: f.Voxel.@byte,
								visibleFace: f.VisibleFace,
								sizeX: scaleX,
								sizeY: scaleY);
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
			ushort width = model.SizeX,
				depth = model.SizeY,
				height = model.SizeZ;
			uint pixelHeight = (uint)(depth + height), index;
			VoxelD[] grid = new VoxelD[width * pixelHeight];
			foreach (Voxel voxel in model
				.Where(voxel => voxel.@byte != 0))
			{
				index = width * (pixelHeight - 2 - voxel.Y - voxel.Z) + voxel.X;
				uint distance = (uint)(height + voxel.Y - voxel.Z - 1);
				if (!(grid[index] is VoxelD top)
					|| top.@byte == 0
					|| top.Distance > distance)
					grid[index] = new VoxelD
					{
						Distance = distance,
						@byte = voxel.@byte,
						VisibleFace = VisibleFace.Top,
					};
				index += width;
				if (!(grid[index] is VoxelD front)
					|| front.@byte == 0
					|| front.Distance > distance)
					grid[index] = new VoxelD
					{
						Distance = distance,
						@byte = voxel.@byte,
						VisibleFace = VisibleFace.Front,
					};
			}
			index = 0;
			for (ushort y = 0; y < pixelHeight; y++)
				for (ushort x = 0; x < width; x++)
					if (grid[index++] is VoxelD voxelD && voxelD.@byte != 0)
						renderer.Rect(
							x: x,
							y: y,
							voxel: voxelD.@byte,
							visibleFace: voxelD.VisibleFace);
		}
		#endregion Diagonal
		#region Isometric
		public static uint IsoWidth(IModel model) => (uint)(2u * (model.SizeX + model.SizeY));
		public static uint IsoHeight(IModel model) => (uint)(2u * (model.SizeX + model.SizeY) + 4u * model.SizeZ - 1u);
		public static void IsoLocate(out int pixelX, out int pixelY, IModel model, int voxelX = 0, int voxelY = 0, int voxelZ = 0)
		{
			// To move one x+ in voxels is x + 2, y - 2 in pixels.
			// To move one x- in voxels is x - 2, y + 2 in pixels.
			// To move one y+ in voxels is x - 2, y - 2 in pixels.
			// To move one y- in voxels is x + 2, y + 2 in pixels.
			// To move one z+ in voxels is y - 4 in pixels.
			// To move one z- in voxels is y + 4 in pixels.
			pixelX = 2 * (model.SizeY + voxelX - voxelY);
			pixelY = (int)IsoHeight(model) - 2 * (voxelX + voxelY) - 4 * voxelZ - 1;
		}
		public static void Iso(IModel model, ITriangleRenderer renderer)
		{
			ushort height = model.SizeZ;
			Dictionary<uint, VoxelD> dictionary = new Dictionary<uint, VoxelD>();
			void Tri(ushort x, ushort y, uint distance, byte @byte, VisibleFace visibleFace = VisibleFace.Front)
			{
				uint index = (uint)(y << 16) | x;
				if (!dictionary.TryGetValue(index, out VoxelD old)
						|| old.Distance > distance)
					dictionary[index] = new VoxelD
					{
						Distance = distance,
						@byte = @byte,
						VisibleFace = visibleFace,
					};
			}
			foreach (Voxel voxel in model
				.Where(voxel => voxel.@byte != 0))
			{
				uint distance = (uint)voxel.X + voxel.Y + height - voxel.Z - 1;
				IsoLocate(
					pixelX: out int pixelX,
					pixelY: out int pixelY,
					model: model,
					voxelX: voxel.X,
					voxelY: voxel.Y,
					voxelZ: voxel.Z);
				// 01
				//0011
				//2014
				//2244
				//2354
				//3355
				// 35
				Tri(//0
					x: (ushort)(pixelX - 2),
					y: (ushort)(pixelY - 6),
					distance: distance,
					@byte: voxel.@byte,
					visibleFace: VisibleFace.Top);
				Tri(//1
					x: (ushort)pixelX,
					y: (ushort)(pixelY - 6),
					distance: distance,
					@byte: voxel.@byte,
					visibleFace: VisibleFace.Top);
				Tri(//2
					x: (ushort)(pixelX - 2),
					y: (ushort)(pixelY - 4),
					distance: distance,
					@byte: voxel.@byte,
					visibleFace: VisibleFace.Left);
				Tri(//3
					x: (ushort)(pixelX - 2),
					y: (ushort)(pixelY - 2),
					distance: distance,
					@byte: voxel.@byte,
					visibleFace: VisibleFace.Left);
				Tri(//4
					x: (ushort)pixelX,
					y: (ushort)(pixelY - 4),
					distance: distance,
					@byte: voxel.@byte,
					visibleFace: VisibleFace.Right);
				Tri(//5
					x: (ushort)pixelX,
					y: (ushort)(pixelY - 2),
					distance: distance,
					@byte: voxel.@byte,
					visibleFace: VisibleFace.Right);
			}
			bool Right(ushort x, ushort y) => (x >> 1) % 2 == (y >> 1) % 2;
			foreach (KeyValuePair<uint, VoxelD> triangle in dictionary)
				renderer.Tri(
					x: (ushort)triangle.Key,
					y: (ushort)(triangle.Key >> 16),
					right: Right((ushort)triangle.Key, (ushort)(triangle.Key >> 16)),
					voxel: triangle.Value.@byte,
					visibleFace: triangle.Value.VisibleFace);
		}
		#endregion Isometric
	}
}
