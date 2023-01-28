using Voxel2Pixel.Color;

namespace Voxel2Pixel.Render
{
	public class ArrayRenderer : IRectangleRenderer, ITriangleRenderer
	{
		public byte[] Image { get; set; }
		public int Width { get; set; }
		public int Height => (Image.Length / Width) >> 2;
		public IVoxelColor IVoxelColor { get; set; }
		#region IRectangleRenderer
		public void Rect(int x, int y, int color, int sizeX, int sizeY) =>
			Image.DrawRectangle(
				x: x,
				y: y,
				color: color,
				rectWidth: sizeX,
				rectHeight: sizeY,
				width: Width);
		public void RectLeft(int x, int y, byte voxel, int sizeX, int sizeY) =>
			Rect(
				x: x,
				y: y,
				color: IVoxelColor.LeftFace(voxel),
				sizeX: sizeX,
				sizeY: sizeY);
		public void RectRight(int x, int y, byte voxel, int sizeX, int sizeY) =>
			Rect(
				x: x,
				y: y,
				color: IVoxelColor.RightFace(voxel),
				sizeX: sizeX,
				sizeY: sizeY);
		public void RectVertical(int x, int y, byte voxel, int sizeX, int sizeY) =>
			Rect(
				x: x,
				y: y,
				color: IVoxelColor.VerticalFace(voxel),
				sizeX: sizeX,
				sizeY: sizeY);
		#endregion IRectangleRenderer
		#region ITriangleRenderer
		public void LeftTriangle(int x, int y, int color) => Image
			.DrawPixel(
				x: x + 1,
				y: y,
				color: color,
				width: Width)
			.DrawRectangle(
				x: x,
				y: y + 1,
				color: color,
				rectWidth: 2,
				rectHeight: 1,
				width: Width)
			.DrawPixel(
				x: x + 1,
				y: y + 2,
				color: color,
				width: Width);
		public void RightTriangle(int x, int y, int color) => Image
			.DrawPixel(
				x: x,
				y: y,
				color: color,
				width: Width)
			.DrawRectangle(
				x: x,
				y: y + 1,
				color: color,
				rectWidth: 2,
				rectHeight: 1,
				width: Width)
			.DrawPixel(
				x: x,
				y: y + 2,
				color: color,
				width: Width);
		public void Triangle(int x, int y, bool right, int color)
		{
			if (right)
				RightTriangle(x, y, color);
			else
				LeftTriangle(x, y, color);
		}
		public void TriangleVerticalFace(int x, int y, bool right, byte voxel) => Triangle(x, y, right, IVoxelColor.VerticalFace(voxel));
		public void TriangleLeftFace(int x, int y, bool right, byte voxel) => Triangle(x, y, right, IVoxelColor.LeftFace(voxel));
		public void TriangleRightFace(int x, int y, bool right, byte voxel) => Triangle(x, y, right, IVoxelColor.RightFace(voxel));
		#endregion ITriangleRenderer
	}
}
