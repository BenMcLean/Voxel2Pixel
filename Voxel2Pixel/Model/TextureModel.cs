using System;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Interfaces;

namespace Voxel2Pixel.Model
{
	public class TextureModel : IModel
	{
		public TextureModel(byte[] texture, ushort width = 0)
		{
			SizeX = width < 1 ? (ushort)Math.Sqrt(texture.Length >> 2) : width;
			Palette = texture.PaletteFromTexture();
			Indexes = texture.Byte2IndexArray(Palette);
		}
		public uint[] Palette { get; set; }
		public byte[] Indexes { get; }
		#region IModel
		public ushort SizeX { get; }
		public ushort SizeY => (ushort)(Indexes.Length / SizeX);
		public ushort SizeZ { get; set; } = 1;
		public byte? At(int x, int y, int z) => IsInside(x, y, z) ? Indexes[y * SizeX + x] : (byte?)null;
		public bool IsInside(int x, int y, int z) => !IsOutside(x, y, z);
		public bool IsOutside(int x, int y, int z) => x < 0 || y < 0 || z < 0 || x >= SizeX || y >= SizeY || z >= SizeZ;
		#endregion IModel
	}
}
