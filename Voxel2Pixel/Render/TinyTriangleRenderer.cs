using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;

namespace Voxel2Pixel.Render
{
	public class TinyTriangleRenderer : ITriangleRenderer
	{
		public virtual IRectangleRenderer RectangleRenderer { get; set; }
		public virtual IVoxelColor VoxelColor { get; set; }
		#region ITriangleRenderer
		public virtual void Tri(int x, int y, bool right, uint color) =>
			RectangleRenderer.Rect(
				x: x / 2 + (right ? 0 : 1),
				y: y / 4,
				color: color);
		public virtual void Tri(int x, int y, bool right, byte voxel, VisibleFace visibleFace = VisibleFace.Front) => Tri(
			x: x,
			y: y,
			right: right,
			color: VoxelColor.Color(voxel, visibleFace));
		#endregion ITriangleRenderer
	}
}
