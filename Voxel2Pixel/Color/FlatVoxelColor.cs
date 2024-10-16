using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;

namespace Voxel2Pixel.Color;

public class FlatVoxelColor(uint[] palette) : IVoxelColor
{
	public uint[] Palette { get; set; } = palette;
	#region IVoxelColor
	public uint this[byte voxel, VisibleFace visibleFace = VisibleFace.Front] => Palette[voxel];
	#endregion IVoxelColor
}
