using Voxel2Pixel.Model;

namespace Voxel2Pixel.Interfaces
{
	public interface ITurnable
	{
		ITurnable Turn(params Turn[] turns);
	}
}
