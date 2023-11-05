using Voxel2Pixel.Model;

namespace Voxel2Pixel.Interfaces
{
	public interface IVoxelColor
	{
		uint this[byte voxel, VisibleFace visibleFace = VisibleFace.Front] { get; }
	}
}
