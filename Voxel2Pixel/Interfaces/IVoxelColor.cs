using Voxel2Pixel.Model;

namespace Voxel2Pixel.Interfaces
{
	public interface IVoxelColor
	{
		uint Color(byte voxel, VisibleFace visibleFace);
	}
}
