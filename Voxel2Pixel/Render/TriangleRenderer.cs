using Voxel2Pixel.Color;

namespace Voxel2Pixel.Render
{
	/// <summary>
	/// Provides a default implementation of ITriangleRenderer by calling IRectangleRenderer, while leaving the Rect method abstract.
	/// </summary>
	public abstract class TriangleRenderer : IRectangleRenderer, ITriangleRenderer
	{
		public virtual IVoxelColor VoxelColor { get; set; }
		#region ITriangleRenderer
		public virtual void Tri(int x, int y, bool right, uint color)
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
		public virtual void TriVertical(int x, int y, bool right, byte voxel) => Tri(x, y, right, VoxelColor.TopFace(voxel));
		public virtual void TriLeft(int x, int y, bool right, byte voxel) => Tri(x, y, right, VoxelColor.LeftFace(voxel));
		public virtual void TriRight(int x, int y, bool right, byte voxel) => Tri(x, y, right, VoxelColor.RightFace(voxel));
		#endregion ITriangleRenderer
		#region IRectangleRenderer
		public abstract void Rect(int x, int y, uint color, int sizeX = 1, int sizeY = 1);
		public virtual void RectTop(int x, int y, byte voxel, int sizeX = 1, int sizeY = 1) =>
			Rect(
				x: x,
				y: y,
				color: VoxelColor.TopFace(voxel),
				sizeX: sizeX,
				sizeY: sizeY);
		public virtual void RectRight(int x, int y, byte voxel, int sizeX = 1, int sizeY = 1) =>
			Rect(
				x: x,
				y: y,
				color: VoxelColor.RightFace(voxel),
				sizeX: sizeX,
				sizeY: sizeY);
		public virtual void RectFront(int x, int y, byte voxel, int sizeX = 1, int sizeY = 1) =>
			Rect(
				x: x,
				y: y,
				color: VoxelColor.FrontFace(voxel),
				sizeX: sizeX,
				sizeY: sizeY);
		public virtual void RectLeft(int x, int y, byte voxel, int sizeX = 1, int sizeY = 1) =>
			Rect(
				x: x,
				y: y,
				color: VoxelColor.LeftFace(voxel),
				sizeX: sizeX,
				sizeY: sizeY);
		#endregion IRectangleRenderer
	}
}
