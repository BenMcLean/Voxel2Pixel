using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;

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
		public virtual void Tri(int x, int y, bool right, byte voxel, VisibleFace visibleFace = VisibleFace.Front) => Tri(
			x: x,
			y: y,
			right: right,
			color: VoxelColor.Color(voxel, visibleFace));
		#endregion ITriangleRenderer
		#region IRectangleRenderer
		public abstract void Rect(int x, int y, uint color, int sizeX = 1, int sizeY = 1);
		public virtual void Rect(int x, int y, byte voxel, VisibleFace visibleFace = VisibleFace.Front, int sizeX = 1, int sizeY = 1) => Rect(
			x: x,
			y: y,
			color: VoxelColor.Color(voxel, visibleFace),
			sizeX: sizeX,
			sizeY: sizeY);
		#endregion IRectangleRenderer
	}
}
