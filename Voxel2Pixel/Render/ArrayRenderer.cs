using System.Drawing;
using Voxel2Pixel.Color;

namespace Voxel2Pixel.Render
{
	public class ArrayRenderer : IRectangleRenderer
	{
		public byte Transparency { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
		public byte[] Image { get; set; }
		public int Width { get; set; }
		IVoxelColor IVoxelColor { get; set; }
		public void Rect(int x, int y, int sizeX, int sizeY, int color) =>
			Image.DrawRectangle(
				color: color,
				x: x,
				y: y,
				rectWidth: sizeX,
				rectHeight: sizeY,
				width: Width);
		public void RectLeft(int x, int y, int sizeX, int sizeY, byte voxel) =>
			Rect(
				color: IVoxelColor.LeftFace(voxel),
				x: x,
				y: y,
				sizeX: sizeX,
				sizeY: sizeY);
		public void RectRight(int x, int y, int sizeX, int sizeY, byte voxel) =>
			Rect(
				color: IVoxelColor.RightFace(voxel),
				x: x,
				y: y,
				sizeX: sizeX,
				sizeY: sizeY);
		public void RectVertical(int x, int y, int sizeX, int sizeY, byte voxel) =>
			Rect(
				color: IVoxelColor.VerticalFace(voxel),
				x: x,
				y: y,
				sizeX: sizeX,
				sizeY: sizeY);
	}
}
