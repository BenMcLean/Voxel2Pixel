using System;
using Voxel2Pixel.Draw;

namespace Voxel2Pixel.Model
{
	public class VoxModel : ArrayModel
	{
		public VoxModel(VoxReader.Interfaces.IVoxFile voxFile, int modelNumber = 0)
		{
			VoxFile = voxFile;
			Model = voxFile.Models[modelNumber];
		}
		public VoxModel(string filePath, int modelNumber = 0) : this(VoxReader.VoxReader.Read(filePath), modelNumber) { }
		public VoxReader.Interfaces.IVoxFile VoxFile
		{
			get => voxFile;
			set
			{
				voxFile = value;
				Palette = new uint[Math.Min(VoxFile.Palette.Colors.Length + 1, 256)];
				for (int i = 0; i < Palette.Length - 1; i++)
					Palette[i + 1] = Color(VoxFile.Palette.Colors[i]);
			}
		}
		private VoxReader.Interfaces.IVoxFile voxFile;
		public uint[] Palette;
		public VoxReader.Interfaces.IModel Model
		{
			get => model;
			set
			{
				model = value;
				Voxels = Bytes3D.Initialize(model.Size.X, model.Size.Y, model.Size.Z);
				foreach (VoxReader.Voxel voxel in Model.Voxels)
					if (Array.IndexOf(Palette, Color(voxel.Color)) is int index
						&& index > 0
						&& index <= byte.MaxValue)
						Voxels[voxel.Position.X][voxel.Position.Y][voxel.Position.Z] = (byte)index;
			}
		}
		private VoxReader.Interfaces.IModel model;
		public static uint Color(VoxReader.Color color) => PixelDraw.Color(
			r: color.R,
			g: color.G,
			b: color.B,
			a: color.A);
	}
}
