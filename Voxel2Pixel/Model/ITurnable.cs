namespace Voxel2Pixel.Model
{
	public interface ITurnable
	{
		ITurnable CounterX();
		ITurnable CounterY();
		ITurnable CounterZ();
		ITurnable ClockX();
		ITurnable ClockY();
		ITurnable ClockZ();
		ITurnable Reset();
	}
}
