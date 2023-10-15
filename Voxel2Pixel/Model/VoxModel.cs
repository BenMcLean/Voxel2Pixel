using System;
using System.Linq;
using Voxel2Pixel.Draw;

namespace Voxel2Pixel.Model
{
	public class VoxModel : ArrayModel
	{
		#region Read
		public VoxModel(FileToVoxCore.Vox.VoxModel model, int frame = 0)
		{
			uint[] palette = model.Palette.Take(256).Select(color => Color(color)).ToArray();
			Palette = new uint[256];
			Array.Copy(
				sourceArray: palette,
				sourceIndex: 0,
				destinationArray: Palette,
				destinationIndex: 1,
				length: Math.Min(palette.Length, Palette.Length) - 1);
			FileToVoxCore.Vox.VoxelData voxelData = model.VoxelFrames[frame];
			Voxels = Array3D.Initialize<byte>(voxelData.VoxelsWide, voxelData.VoxelsTall, voxelData.VoxelsDeep);
			for (int x = 0; x < SizeX; x++)
				for (int y = 0; y < SizeY; y++)
					for (int z = 0; z < SizeZ; z++)
						if (voxelData.GetSafe(x, y, z) is byte voxel && voxel != 0)
							Voxels[x][y][z] = voxel;
		}
		public VoxModel(string filePath, int frame = 0) : this(new FileToVoxCore.Vox.VoxReader().LoadModel(filePath), frame) { }
		public uint[] Palette;
		public static uint Color(FileToVoxCore.Drawing.Color color) => ((uint)color.ToArgb()).Argb2rgba();
		#endregion Read
	}
}
