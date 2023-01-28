using System;

namespace Voxel2Pixel.Model
{
	public class VoxModel : Voxel2Pixel.Model.IModel
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
				Palette = new int[Math.Min(VoxFile.Palette.Colors.Length + 1, 256)];
				for (int i = 0; i < Palette.Length - 1; i++)
					Palette[i + 1] = Color(VoxFile.Palette.Colors[i]);
			}
		}
		private VoxReader.Interfaces.IVoxFile voxFile;
		public int[] Palette;
		public VoxReader.Interfaces.IModel Model
		{
			get => model;
			set
			{
				model = value;
				Voxels = new byte[SizeX][][];
				for (int x = 0; x < Voxels.Length; x++)
				{
					Voxels[x] = new byte[SizeY][];
					for (int y = 0; y < Voxels[x].Length; y++)
						Voxels[x][y] = new byte[SizeZ];
				}
				foreach (VoxReader.Voxel voxel in Model.Voxels)
					if (Array.IndexOf(Palette, Color(voxel.Color)) is int index
						&& index > 0
						&& index <= byte.MaxValue)
						Voxels[voxel.Position.X][voxel.Position.Y][voxel.Position.Z] = (byte)index;
			}
		}
		private VoxReader.Interfaces.IModel model;
		public byte[][][] Voxels;
		public static int Color(VoxReader.Color color) => TextureMethods.Color(
			r: color.R,
			g: color.G,
			b: color.B,
			a: color.A);
		#region IModel
		public int SizeX => Model.Size.X;
		public int SizeY => Model.Size.Y;
		public int SizeZ => Model.Size.Z;
		public byte? At(int x, int y, int z) => IsInside(x, y, z) ? Voxels[x][y][z] : (byte?)null;
		public bool IsInside(int x, int y, int z) => !IsOutside(x, y, z);
		public bool IsOutside(int x, int y, int z) => x < 0 || y < 0 || z < 0 || x >= SizeX || y >= SizeY || z >= SizeZ;
		#endregion IModel
	}
}
