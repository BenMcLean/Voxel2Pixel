using System;

namespace Voxel2Pixel.Model
{
	public class TextureModel : IModel
	{
		public TextureModel(byte[] texture, int width = 0)
		{
			SizeX = width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width;
			Palette = texture.PaletteFromTexture();
			Indexes = texture.Byte2IndexArray(Palette);
		}
		public int[] Palette { get; set; }
		public byte[] Indexes { get; set; }
		#region IModel
		public int SizeX { get; set; }
		public int SizeY => Indexes.Length / SizeX;
		public int SizeZ { get; set; } = 1;
		public byte? At(int x, int y, int z) => IsInside(x, y, z) ? Indexes[y * SizeX + x] : (byte?)null;
		public bool IsInside(int x, int y, int z) => !IsOutside(x, y, z);
		public bool IsOutside(int x, int y, int z) => x < 0 || y < 0 || x >= SizeX || y >= SizeY;
		#endregion IModel
	}
}
