using System;
using System.Collections.Generic;
using System.Linq;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Interfaces;

namespace Voxel2Pixel.Model
{
    public class VoxFileModel : ArrayModel
	{
		#region Read
		public VoxFileModel(FileToVoxCore.Vox.VoxModel model, int frame = 0)
		{
			Palette = new uint[256];
			uint[] palette = model.Palette.Take(Palette.Length).Select(color => Color(color)).ToArray();
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
		public VoxFileModel(string filePath, int frame = 0) : this(new FileToVoxCore.Vox.VoxReader().LoadModel(filePath), frame) { }
		public uint[] Palette { get; set; }
		public static uint Color(FileToVoxCore.Drawing.Color color) => ((uint)color.ToArgb()).Argb2rgba();
		#endregion Read
		#region Write
		public static FileToVoxCore.Drawing.Color Color(uint color) => FileToVoxCore.Drawing.Color.FromArgb(
			alpha: color.A(),
			red: color.R(),
			green: color.G(),
			blue: color.B());
		public static IEnumerable<FileToVoxCore.Schematics.Voxel> FileToVoxCoreVoxels(IModel model, uint[] palette)
		{
			for (ushort x = 0; x < model.SizeX; x++)
				for (ushort y = 0; y < model.SizeY; y++)
					for (ushort z = 0; z < model.SizeZ; z++)
						if (model.At(x, y, z) is byte voxel && voxel != 0)
							yield return new FileToVoxCore.Schematics.Voxel(
								x: x,
								y: y,
								z: z,
								color: palette[voxel].Rgba2argb());
		}
		public static IEnumerable<FileToVoxCore.Schematics.Voxel> FileToVoxCoreVoxels2(IModel model, uint[] palette)
		{
			TurnModel turnModel = new TurnModel
			{
				Model = model,
				CuboidOrientation = CuboidOrientation.BOTTOM3,
			};
			for (ushort x = 0; x < turnModel.SizeX; x++)
				for (ushort y = 0; y < turnModel.SizeY; y++)
					for (ushort z = 0; z < turnModel.SizeZ; z++)
						if (turnModel.At(x, y, z) is byte voxel && voxel != 0)
							yield return new FileToVoxCore.Schematics.Voxel(
								x: x,
								y: y,
								z: z,
								color: palette[voxel].Rgba2argb());
		}
		public static bool Write(string absolutePath, uint[] palette, IModel model) =>
			new FileToVoxCore.Vox.VoxWriter().WriteModel(
				absolutePath: absolutePath,
				palette: palette.Skip(1).Select(@uint => Color(@uint)).ToList(),
				schematic: new FileToVoxCore.Schematics.Schematic(FileToVoxCoreVoxels(model, palette).ToList()));
		public static bool Write2(string absolutePath, uint[] palette, IModel model) =>
			new FileToVoxCore.Vox.VoxWriter().WriteModel(
				absolutePath: absolutePath,
				palette: palette.Skip(1).Select(@uint => Color(@uint)).ToList(),
				schematic: new FileToVoxCore.Schematics.Schematic(FileToVoxCoreVoxels2(model, palette).ToList()));
		#endregion Write
	}
}
