using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;

namespace Voxel2Pixel.Render
{
	/// <summary>
	/// Similar to ArrayRenderer, except that triangles are drawn at 2x horizontal scale.
	/// </summary>
	public class Array2xRenderer : IRectangleRenderer, ITriangleRenderer
	{
		public byte[] Image { get; set; }
		public int Width { get; set; }
		public int Height => (Image.Length / Width) >> 2;
		public IVoxelColor VoxelColor { get; set; }
		#region IRectangleRenderer
		public void Rect(int x, int y, uint color, int sizeX = 1, int sizeY = 1) =>
			Image.DrawRectangle(
				x: x,
				y: y,
				color: color,
				rectWidth: sizeX,
				rectHeight: sizeY,
				width: Width);
		public void RectLeft(int x, int y, byte voxel, int sizeX = 1, int sizeY = 1) =>
			Rect(
				x: x,
				y: y,
				color: VoxelColor.LeftFace(voxel),
				sizeX: sizeX,
				sizeY: sizeY);
		public void RectRight(int x, int y, byte voxel, int sizeX = 1, int sizeY = 1) =>
			Rect(
				x: x,
				y: y,
				color: VoxelColor.RightFace(voxel),
				sizeX: sizeX,
				sizeY: sizeY);
		public void RectVertical(int x, int y, byte voxel, int sizeX = 1, int sizeY = 1) =>
			Rect(
				x: x,
				y: y,
				color: VoxelColor.VerticalFace(voxel),
				sizeX: sizeX,
				sizeY: sizeY);
		#endregion IRectangleRenderer
		#region ITriangleRenderer
		public void Tri(int x, int y, bool right, uint color)
		{
			if (right)
			{
				Rect(
					x: x << 1,
					y: y,
					color: color,
					sizeX: 2);
				Rect(
					x: x << 1,
					y: y + 1,
					color: color,
					sizeX: 4);
				Rect(
					x: x << 1,
					y: y + 2,
					color: color,
					sizeX: 2);
			}
			else
			{
				Rect(
					x: (x + 1) << 1,
					y: y,
					color: color,
					sizeX: 2);
				Rect(
					x: x << 1,
					y: y + 1,
					color: color,
					sizeX: 4);
				Rect(
					x: (x + 1) << 1,
					y: y + 2,
					color: color,
					sizeX: 2);
			}
		}
		public virtual void TriVertical(int x, int y, bool right, byte voxel) => Tri(x, y, right, VoxelColor.VerticalFace(voxel));
		public virtual void TriLeft(int x, int y, bool right, byte voxel) => Tri(x, y, right, VoxelColor.LeftFace(voxel));
		public virtual void TriRight(int x, int y, bool right, byte voxel) => Tri(x, y, right, VoxelColor.RightFace(voxel));
		#endregion ITriangleRenderer
	}
}
