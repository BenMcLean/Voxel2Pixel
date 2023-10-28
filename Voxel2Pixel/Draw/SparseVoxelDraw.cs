using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;
using Voxel2Pixel.Render;

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
			public ushort y;
			public byte color;
			public VoxelY(Voxel voxel)
			{
				y = voxel.Y;
				color = voxel.@byte;
			}
		}
		public static void Front(ISparseModel model, IRectangleRenderer renderer)
		{
			VoxelY?[] grid = new VoxelY?[model.SizeX * model.SizeZ];
			foreach (Voxel voxel in model.Voxels)
				if (!(grid[voxel.Z * model.SizeZ + voxel.X] is VoxelY old)
					|| voxel.Y < old.y)
					grid[voxel.Z * model.SizeZ + voxel.X] = new VoxelY(voxel);
			uint index = 0;
			for (ushort y = 0; y < model.SizeZ; y++)
				for (ushort x = 0; x < model.SizeX; x++)
					if (grid[++index] is VoxelY voxelY)
						renderer.RectFront(
							x: x,
							y: model.SizeZ - 1 - y,
							voxel: voxelY.color);
		}
		#endregion Straight
	}
}
