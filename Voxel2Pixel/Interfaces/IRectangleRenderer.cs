using Voxel2Pixel.Model;

namespace Voxel2Pixel.Interfaces;

/// <summary>
/// An IRectangleRenderer draws rectangles representing the three visible faces of a voxel cube
/// </summary>
public interface IRectangleRenderer
{
	void Rect(ushort x, ushort y, uint color, ushort sizeX = 1, ushort sizeY = 1);
	void Rect(ushort x, ushort y, byte index, VisibleFace visibleFace = VisibleFace.Front, ushort sizeX = 1, ushort sizeY = 1);
}
