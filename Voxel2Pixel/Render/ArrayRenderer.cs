using Voxel2Pixel.Color;

namespace Voxel2Pixel.Render
{
	public class ArrayRenderer : IRectangleRenderer, ITriangleRenderer
	{
		public byte[] Image { get; set; }
		public int Width { get; set; }
		public int Height => (Image.Length / Width) >> 2;
		public IVoxelColor IVoxelColor { get; set; }
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
		public void DrawLeftTriangle(int x, int y, int color) =>
			Image.DrawPixel(
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
		public void DrawLeftTriangleLeftFace(int x, int y, byte voxel) =>
			DrawLeftTriangle(
				x: x,
				y: y,
				color: IVoxelColor.LeftFace(voxel));
		public void DrawLeftTriangleRightFace(int x, int y, byte voxel) =>
			DrawLeftTriangle(
				x: x,
				y: y,
				color: IVoxelColor.RightFace(voxel));
		public void DrawLeftTriangleVerticalFace(int x, int y, byte voxel) =>
			DrawLeftTriangle(
				x: x,
				y: y,
				color: IVoxelColor.VerticalFace(voxel));
		public void DrawRightTriangle(int x, int y, int color) =>
			Image.DrawPixel(
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
		public void DrawRightTriangleLeftFace(int x, int y, byte voxel) =>
			DrawRightTriangle(
				x: x,
				y: y,
				color: IVoxelColor.LeftFace(voxel));
		public void DrawRightTriangleRightFace(int x, int y, byte voxel) =>
			DrawRightTriangle(
				x: x,
				y: y,
				color: IVoxelColor.RightFace(voxel));
		public void DrawRightTriangleVerticalFace(int x, int y, byte voxel) =>
			DrawRightTriangle(
				x: x,
				y: y,
				color: IVoxelColor.VerticalFace(voxel));
	}
}
