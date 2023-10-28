namespace Voxel2Pixel.Interfaces
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
