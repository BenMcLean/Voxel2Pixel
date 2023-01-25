namespace Voxel2Pixel.Model
{
	public interface IModel : IFetch
	{
		/// <summary>
		/// Upper bound of x
		/// </summary>
		int Width { get; }
		/// <summary>
		/// Upper bound of y
		/// </summary>
		int Height { get; }
		/// <summary>
		/// Upper bound of z
		/// </summary>
		int Depth { get; }
		bool IsInside(int x, int y, int z);
		bool IsOutside(int x, int y, int z);
	}
}
