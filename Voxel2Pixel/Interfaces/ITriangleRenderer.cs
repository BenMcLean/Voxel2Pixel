using Voxel2Pixel.Model;

namespace Voxel2Pixel.Interfaces
{
	/// <summary>
	/// An ITriangleRenderer draws triangles where each triangle represents half of one of the diamonds making up one of the three visible faces of a voxel cube from an isometric perspective
	/// </summary>
	public interface ITriangleRenderer
	{
		/// <summary>
		/// Draws a triangle 3 high and 2 wide
		/// </summary>
		/// <param name="right">Points right if true, else points left</param>
		void Tri(int x, int y, bool right, uint color);
		/// <summary>
		/// Draws a triangle 3 high and 2 wide
		/// </summary>
		/// <param name="right">Points right if true, else points left</param>
		void Tri(int x, int y, bool right, byte index, VisibleFace visibleFace = VisibleFace.Front);
	}
}
