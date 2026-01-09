using System;
using System.Collections;
using System.Collections.Generic;
using BenVoxel;
using BenVoxel.Interfaces;
using BenVoxel.Structs;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Interfaces;

namespace Voxel2Pixel.Models;

public class TextureModel : IModel
{
	public TextureModel(byte[] texture, ushort width = 0)
	{
		Palette = texture.PaletteFromTexture();
		Indices = texture.Byte2IndexArray(Palette);
		SizeX = width < 1 ? (ushort)Math.Sqrt(Indices.Length) : width;
	}
	public TextureModel(ISprite sprite) : this(texture: sprite.Texture, width: sprite.Width) { }
	public uint[] Palette { get; set; }
	public byte[] Indices { get; }
	#region IModel
	public byte this[ushort x, ushort y, ushort z] => !this.IsOutside(x, y, z) ? Indices[y * SizeX + x] : (byte)0;
	public ushort SizeX { get; set; }
	public ushort SizeY => (ushort)(Indices.Length / SizeX);
	public ushort SizeZ { get; set; } = 1;
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	public virtual IEnumerator<Voxel> GetEnumerator()
	{
		for (ushort x = 0; x < SizeX; x++)
			for (uint y = 0, rowStart = 0; y < SizeY; y++, rowStart += SizeX)
				for (ushort z = 0; z < SizeZ; z++)
					if (Indices[rowStart + x] is byte @byte && @byte != 0)
						yield return new Voxel(x, (ushort)y, z, @byte);
	}
	#endregion IModel
}
