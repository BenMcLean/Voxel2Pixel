﻿using System.Linq;
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
		public static void Front(ISparseModel model, IRectangleRenderer renderer)
		{
			VoxelY[] grid = new VoxelY[model.SizeX * model.SizeZ];
			foreach (Voxel voxel in model.Voxels
				.Where(voxel => voxel.@byte != 0 && (
					!(grid[voxel.Z * model.SizeX + voxel.X] is VoxelY old)
						|| old.@byte == 0
						|| old.Y > voxel.Y)))
				grid[voxel.Z * model.SizeX + voxel.X] = new VoxelY(voxel);
			uint index = 0;
			for (ushort y = 0; y < model.SizeZ; y++)
				for (ushort x = 0; x < model.SizeX; x++)
					if (grid[index++] is VoxelY voxelY && voxelY.@byte != 0)
						renderer.Rect(
							x: x,
							y: model.SizeZ - 1 - y,
							voxel: voxelY.@byte);
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
		public static void Diagonal(ISparseModel model, IRectangleRenderer renderer)
		{
			ushort width = model.SizeX,
				depth = model.SizeY,
				height = model.SizeZ;
			VoxelD[] grid = new VoxelD[width * height];
			foreach (Voxel voxel in model.Voxels
				.Where(voxel => voxel.@byte != 0))
			{
				uint i = (uint)(width * voxel.Z + depth - voxel.Y - 1 + voxel.X),
					distance = (uint)(voxel.X + voxel.Y);
				if (!(grid[i] is VoxelD left)
					|| left.@byte == 0
					|| left.Distance < distance)
					grid[i] = new VoxelD
					{
						Distance = distance,
						@byte = voxel.@byte,
						VisibleFace = VisibleFace.Left,
					};
				if (!(grid[++i] is VoxelD right)
					|| right.@byte == 0
					|| right.Distance < distance)
					grid[i] = new VoxelD
					{
						Distance = distance,
						@byte = voxel.@byte,
						VisibleFace = VisibleFace.Right,
					};
			}
			uint index = 0;
			for (ushort y = 0; y < depth; y++)
				for (ushort x = 0; x < width; x++)
					if (grid[index++] is VoxelD voxelD && voxelD.@byte != 0)
						renderer.Rect(
							x: x,
							y: height - 1 - y,
							voxel: voxelD.@byte,
							visibleFace: voxelD.VisibleFace);
		}
		#endregion Diagonal
	}
}
