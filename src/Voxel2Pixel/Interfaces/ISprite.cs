using System.Collections.Generic;
using BenVoxel.Structs;

namespace Voxel2Pixel.Interfaces;

public interface ISprite : IDictionary<string, Point>
{
	byte[] Texture { get; }
	ushort Width { get; }
	ushort Height { get; }
}
