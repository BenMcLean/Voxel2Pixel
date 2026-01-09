using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenVoxel.Interfaces;
using BenVoxel.Models;
using FileToVoxCore.Schematics;
using FileToVoxCore.Vox;
using FileToVoxCoreColor = FileToVoxCore.Drawing.Color;

namespace BenVoxel.FileToVoxCore;

public class VoxFileModel : DictionaryModel
{
	#region Read
	public VoxFileModel(VoxModel model, int frame = 0, bool incluePalette = true)
	{
		if (incluePalette)
		{
			Palette = new uint[256];
			uint[] palette = [.. model.Palette.Take(Palette.Length).Select(Color)];
			Array.Copy(
				sourceArray: palette,
				sourceIndex: 0,
				destinationArray: Palette,
				destinationIndex: 1,
				length: Math.Min(palette.Length, Palette.Length) - 1);
		}
		VoxelData voxelData = model.VoxelFrames[frame];
		SizeX = (ushort)(voxelData.VoxelsWide - 1);
		SizeY = (ushort)(voxelData.VoxelsTall - 1);
		SizeZ = (ushort)(voxelData.VoxelsDeep - 1);
		foreach (KeyValuePair<int, byte> voxel in voxelData.Colors)
		{
			voxelData.Get3DPos(voxel.Key, out int x, out int y, out int z);
			this[(ushort)x, (ushort)y, (ushort)z] = voxel.Value;
		}
	}
	public VoxFileModel(string filePath, int frame = 0) : this(new VoxReader().LoadModel(filePath), frame) { }
	public VoxFileModel(Stream stream, int frame = 0) : this(new VoxReader().LoadModel(stream), frame) { }
	public static VoxFileModel[] Models(Stream stream)
	{
		VoxModel model = new VoxReader().LoadModel(stream);
		return [.. Enumerable.Range(0, model.VoxelFrames.Count()).Select(i => new VoxFileModel(model, i))];
	}
	public static VoxFileModel[] Models(Stream stream, out uint[] palette)
	{
		VoxModel model = new VoxReader().LoadModel(stream);
		palette = new uint[256];
		uint[] sourceArray = [.. model.Palette.Take(palette.Length).Select(Color)];
		Array.Copy(
			sourceArray: sourceArray,
			sourceIndex: 0,
			destinationArray: palette,
			destinationIndex: 1,
			length: Math.Min(palette.Length, sourceArray.Length) - 1);
		return [.. Enumerable.Range(0, model.VoxelFrames.Count()).Select(i => new VoxFileModel(model, i, false))];
	}
	public uint[] Palette { get; set; }
	public static uint Color(FileToVoxCoreColor color) => Argb2rgba((uint)color.ToArgb());
	/// <param name="rgba">argb8888, Big Endian</param>
	/// <returns>rgba8888, Big Endian</returns>
	public static uint Argb2rgba(uint argb) => argb << 8 | argb >> 24;
	#endregion Read
	#region Write
	public static FileToVoxCoreColor Color(uint color) => FileToVoxCoreColor.FromArgb(
		alpha: (byte)color,
		red: (byte)(color >> 24),
		green: (byte)(color >> 16),
		blue: (byte)(color >> 8));
	public static IEnumerable<Voxel> FileToVoxCoreVoxels(IModel model, uint[] palette) => model
		.Select(voxel => new Voxel(
			x: voxel.X,
			y: voxel.Y,
			z: voxel.Z,
			color: Rgba2argb(palette[voxel.Material])));
	public static bool Write(string absolutePath, uint[] palette, IModel model) =>
		new VoxWriter().WriteModel(
			absolutePath: absolutePath,
			palette: [.. palette.Skip(1).Select(Color)],
			schematic: new Schematic(FileToVoxCoreVoxels(model, palette).ToList()));
	/// <param name="rgba">rgba8888, Big Endian</param>
	/// <returns>argb8888, Big Endian</returns>
	public static uint Rgba2argb(uint rgba) => rgba << 24 | rgba >> 8;
	#endregion Write
}
