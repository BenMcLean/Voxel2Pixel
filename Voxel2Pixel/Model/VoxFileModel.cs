using System;
using System.Collections.Generic;
using System.IO;
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
			uint[] palette = model.Palette.Take(Palette.Length).Select(Color).ToArray();
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
			foreach (KeyValuePair<int, byte> voxel in voxelData.Colors)
			{
				voxelData.Get3DPos(voxel.Key, out int x, out int y, out int z);
				this[(ushort)x, (ushort)y, (ushort)z] = voxel.Value;
			}
		}
		public VoxFileModel(string filePath, int frame = 0) : this(new FileToVoxCore.Vox.VoxReader().LoadModel(filePath), frame) { }
		public VoxFileModel(Stream stream, int frame = 0) : this(new FileToVoxCore.Vox.VoxReader().LoadModel(stream), frame) { }
		public uint[] Palette { get; set; }
		public static uint Color(FileToVoxCore.Drawing.Color color) => ((uint)color.ToArgb()).Argb2rgba();
		#endregion Read
		#region Write

		public static FileToVoxCore.Drawing.Color Color(uint color) => FileToVoxCore.Drawing.Color.FromArgb(
			alpha: (byte)color,
			red: (byte)(color >> 24),
			green: (byte)(color >> 16),
			blue: (byte)(color >> 8));
		public static IEnumerable<FileToVoxCore.Schematics.Voxel> FileToVoxCoreVoxels(IModel model, uint[] palette) => model
			.Select(voxel => new FileToVoxCore.Schematics.Voxel(
				x: voxel.X,
				y: voxel.Y,
				z: voxel.Z,
				color: palette[voxel.Index].Rgba2argb()));
		public static bool Write(string absolutePath, uint[] palette, IModel model) =>
			new FileToVoxCore.Vox.VoxWriter().WriteModel(
				absolutePath: absolutePath,
				palette: palette.Skip(1).Select(@uint => Color(@uint)).ToList(),
				schematic: new FileToVoxCore.Schematics.Schematic(FileToVoxCoreVoxels(model, palette).ToList()));
		#endregion Write
	}
}
