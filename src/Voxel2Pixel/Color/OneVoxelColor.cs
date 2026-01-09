using BenVoxel.Structs;
using Voxel2Pixel.Interfaces;

namespace Voxel2Pixel.Color;

public class OneVoxelColor(uint color = 0x7Fu) : IVoxelColor
{
	public uint Color { get; set; } = color;
	#region IVoxelColor
	public uint this[byte index, VisibleFace visibleFace = VisibleFace.Top] => Color;
	#endregion IVoxelColor
}
