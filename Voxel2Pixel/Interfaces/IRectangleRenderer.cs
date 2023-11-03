using Voxel2Pixel.Model;

namespace Voxel2Pixel.Interfaces
{
	/// <summary>
	/// An IRectangleRenderer draws rectangles representing the three visible faces of a voxel cube
	/// </summary>
	public interface IRectangleRenderer
	{
		void Rect(int x, int y, uint color, int sizeX = 1, int sizeY = 1);
		void Rect(int x, int y, byte voxel, VisibleFace visibleFace = VisibleFace.Front, int sizeX = 1, int sizeY = 1);
	}
}
