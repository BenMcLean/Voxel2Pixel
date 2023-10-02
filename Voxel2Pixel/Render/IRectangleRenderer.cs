namespace Voxel2Pixel.Render
{
	/// <summary>
	/// An IRectangleRenderer draws rectangles representing the three visible faces of a voxel cube
	/// </summary>
	public interface IRectangleRenderer
	{
		void Rect(int x, int y, uint color, int sizeX = 1, int sizeY = 1);
		void RectTop(int x, int y, byte voxel, int sizeX = 1, int sizeY = 1);
		void RectRight(int x, int y, byte voxel, int sizeX = 1, int sizeY = 1);
		void RectFront(int x, int y, byte voxel, int sizeX = 1, int sizeY = 1);
		void RectLeft(int x, int y, byte voxel, int sizeX = 1, int sizeY = 1);
	}
}
