using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;

namespace Voxel2Pixel.Color
{
	public class FlatVoxelColor : IVoxelColor
	{
		public FlatVoxelColor(uint[] palette) => Palette = palette;
		public uint[] Palette { get; set; }
		#region IVoxelColor
		public bool Iso { get; set; } = true;
		public uint Color(byte voxel, VisibleFace visibleFace = VisibleFace.Front) => Palette[voxel];
		#endregion IVoxelColor
	}
}
