using Voxel2Pixel.Color;

namespace Voxel2Pixel.Render
{
	public class TinyTriangleRenderer : ITriangleRenderer
	{
		public virtual IRectangleRenderer RectangleRenderer { get; set; }
		public virtual IVoxelColor VoxelColor { get; set; }
		#region ITriangleRenderer
		public virtual void Tri(int x, int y, bool right, uint color) =>
			RectangleRenderer.Rect(
				x: x / 2 + (right ? 1 : 0),
				y: y / 4,
				color: color);
		public virtual void TriVertical(int x, int y, bool right, byte voxel) => Tri(x, y, right, VoxelColor.VerticalFace(voxel));
		public virtual void TriLeft(int x, int y, bool right, byte voxel) => Tri(x, y, right, VoxelColor.LeftFace(voxel));
		public virtual void TriRight(int x, int y, bool right, byte voxel) => Tri(x, y, right, VoxelColor.RightFace(voxel));
		#endregion ITriangleRenderer
	}
}
