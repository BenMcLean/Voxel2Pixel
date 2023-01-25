using Voxel2Pixel.Color;

namespace Voxel2Pixel.Render
{
	public class ArrayRenderer : IRectangleRenderer
	{
		public byte Transparency { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
		public byte[] Image { get; set; }
		public int Width { get; set; }
		IVoxelColor IVoxelColor { get; set; }
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
	}
}
