using System;
using System.Collections.Generic;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Interfaces;

namespace Voxel2Pixel.Model
{
	public class TextureModel : ISparseModel
	{
		public TextureModel(byte[] texture, ushort width = 0)
		{
			Palette = texture.PaletteFromTexture();
			Indexes = texture.Byte2IndexArray(Palette);
			SizeX = width < 1 ? (ushort)Math.Sqrt(Indexes.Length) : width;
			SizeY = (ushort)(Indexes.Length / SizeX);
		}
		public uint[] Palette { get; set; }
		public byte[] Indexes { get; }
		#region IFetch
		public byte this[ushort x, ushort y, ushort z] => IsInside(x, y, z) ? Indexes[y * SizeX + x] : (byte)0;
		#endregion IFetch
		#region IModel
		public ushort SizeX { get; }
		public ushort SizeY { get; }
		public ushort SizeZ { get; set; } = 1;
		public bool IsInside(ushort x, ushort y, ushort z) => !IsOutside(x, y, z);
		public bool IsOutside(ushort x, ushort y, ushort z) => x >= SizeX || y >= SizeY || z >= SizeZ;
		#endregion IModel
		#region ISparseModel
		public IEnumerable<Voxel> Voxels
		{
			get
			{
				for (ushort x = 0; x < SizeX; x++)
					for (int y = 0, rowStart = 0; y < SizeY; y++, rowStart += SizeX)
						for (ushort z = 0; z < SizeZ; z++)
							if (Indexes[rowStart + x] is byte @byte && @byte != 0)
								yield return new Voxel(x, (ushort)y, z, @byte);
			}
		}
		#endregion ISparseModel
	}
}
