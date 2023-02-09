using Voxel2Pixel.Color;

namespace Voxel2Pixel.Render
{
	/// <summary>
	/// Provides a default implementation of ITriangleRenderer by calling IRectangleRenderer, which is left abstract.
	/// </summary>
	public abstract class TriangleRenderer : IRectangleRenderer, ITriangleRenderer
	{
		public IVoxelColor IVoxelColor { get; set; }
		#region ITriangleRenderer
		public void Triangle(int x, int y, bool right, uint color)
		{
			if (right)
			{
				Rect(
					x: x,
					y: y,
					color: color);
				Rect(
					x: x,
					y: y + 1,
					color: color,
					sizeX: 2);
				Rect(
					x: x,
					y: y + 2,
					color: color);
			}
			else
			{
				Rect(
					x: x + 1,
					y: y,
					color: color);
				Rect(
					x: x,
					y: y + 1,
					color: color,
					sizeX: 2);
				Rect(
					x: x + 1,
					y: y + 2,
					color: color);
			}
		}
		public void TriangleVerticalFace(int x, int y, bool right, byte voxel) => Triangle(x, y, right, IVoxelColor.VerticalFace(voxel));
		public void TriangleLeftFace(int x, int y, bool right, byte voxel) => Triangle(x, y, right, IVoxelColor.LeftFace(voxel));
		public void TriangleRightFace(int x, int y, bool right, byte voxel) => Triangle(x, y, right, IVoxelColor.RightFace(voxel));
		#endregion ITriangleRenderer
		#region IRectangleRenderer
		public abstract void Rect(int x, int y, uint color, int sizeX = 1, int sizeY = 1);
		public abstract void RectVertical(int x, int y, byte voxel, int sizeX = 1, int sizeY = 1);
		public abstract void RectLeft(int x, int y, byte voxel, int sizeX = 1, int sizeY = 1);
		public abstract void RectRight(int x, int y, byte voxel, int sizeX = 1, int sizeY = 1);
		#endregion IRectangleRenderer
	}
}
