using Voxel2Pixel.Model;

namespace Voxel2Pixel.Interfaces
{
	public interface ITurnable
	{
		ITurnable Turn(Turn turn);
		ITurnable CounterX();
		ITurnable CounterY();
		ITurnable CounterZ();
		ITurnable ClockX();
		ITurnable ClockY();
		ITurnable ClockZ();
		ITurnable Reset();
	}
}
