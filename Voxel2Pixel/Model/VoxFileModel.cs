using System;
using System.Collections.Generic;
using System.Linq;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Interfaces;

namespace Voxel2Pixel.Model
{
	public class VoxFileModel : DictionaryModel
	{
		#region Read
		public VoxFileModel(FileToVoxCore.Vox.VoxModel model, int frame = 0)
		{
			Palette = new uint[256];
			uint[] palette = model.Palette.Take(Palette.Length).Select(color => Color(color)).ToArray();
			System.Array.Copy(
				sourceArray: palette,
				sourceIndex: 0,
				destinationArray: Palette,
				destinationIndex: 1,
				length: Math.Min(palette.Length, Palette.Length) - 1);
			FileToVoxCore.Vox.VoxelData voxelData = model.VoxelFrames[frame];
			SizeX = (ushort)(voxelData.VoxelsWide - 1);
			SizeY = (ushort)(voxelData.VoxelsTall - 1);
			SizeZ = (ushort)(voxelData.VoxelsDeep - 1);
			for (ushort x = 0; x < SizeX; x++)
				for (ushort y = 0; y < SizeY; y++)
					for (ushort z = 0; z < SizeZ; z++)
						if (voxelData.GetSafe(x, y, z) is byte voxel && voxel != 0)
							this[x, y, z] = voxel;
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
		public static IEnumerable<FileToVoxCore.Schematics.Voxel> FileToVoxCoreVoxels(IModel model, uint[] palette) => model
			.Select(voxel => new FileToVoxCore.Schematics.Voxel(
				x: voxel.X,
				y: voxel.Y,
				z: voxel.Z,
				color: palette[voxel.@byte].Rgba2argb()));
		public static bool Write(string absolutePath, uint[] palette, IModel model) =>
			new FileToVoxCore.Vox.VoxWriter().WriteModel(
				absolutePath: absolutePath,
				palette: palette.Skip(1).Select(@uint => Color(@uint)).ToList(),
				schematic: new FileToVoxCore.Schematics.Schematic(FileToVoxCoreVoxels(model, palette).ToList()));
		#endregion Write
	}
}
