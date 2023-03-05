using Voxel2Pixel.Color;

namespace Voxel2Pixel.Render
{
	/// <summary>
	/// Provides a default implementation of ITriangleRenderer by calling IRectangleRenderer, while leaving the Rect method abstract.
	/// </summary>
	public abstract class TriangleRenderer : IRectangleRenderer, ITriangleRenderer
	{
		public virtual IVoxelColor IVoxelColor { get; set; }
		#region ITriangleRenderer
		public void Tri(int x, int y, bool right, uint color)
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
		public void TriVertical(int x, int y, bool right, byte voxel) => Tri(x, y, right, IVoxelColor.VerticalFace(voxel));
		public void TriLeft(int x, int y, bool right, byte voxel) => Tri(x, y, right, IVoxelColor.LeftFace(voxel));
		public void TriRight(int x, int y, bool right, byte voxel) => Tri(x, y, right, IVoxelColor.RightFace(voxel));
		#endregion ITriangleRenderer
		#region IRectangleRenderer
		public abstract void Rect(int x, int y, uint color, int sizeX = 1, int sizeY = 1);
		public void RectLeft(int x, int y, byte voxel, int sizeX = 1, int sizeY = 1) =>
			Rect(
				x: x,
				y: y,
				color: IVoxelColor.LeftFace(voxel),
				sizeX: sizeX,
				sizeY: sizeY);
		public void RectRight(int x, int y, byte voxel, int sizeX = 1, int sizeY = 1) =>
			Rect(
				x: x,
				y: y,
				color: IVoxelColor.RightFace(voxel),
				sizeX: sizeX,
				sizeY: sizeY);
		public void RectVertical(int x, int y, byte voxel, int sizeX = 1, int sizeY = 1) =>
			Rect(
				x: x,
				y: y,
				color: IVoxelColor.VerticalFace(voxel),
				sizeX: sizeX,
				sizeY: sizeY);
		#endregion IRectangleRenderer
	}
}
