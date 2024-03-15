using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;

namespace Voxel2Pixel.Color
{
	public class OneVoxelColor : IVoxelColor
	{
		public uint Color { get; set; } = 0x7Fu;
		#region IVoxelColor
		public uint this[byte index, VisibleFace visibleFace = VisibleFace.Top] => Color;
		#endregion IVoxelColor
	}
}
