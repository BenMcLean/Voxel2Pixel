using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;

namespace Voxel2Pixel.Render
{
	public class ArrayRenderer : TriangleRenderer
	{
		public byte[] Image { get; set; }
		public int Width { get; set; }
		public int Height => (Image.Length / Width) >> 2;
		#region IRectangleRenderer
		public override void Rect(int x, int y, uint color, int sizeX = 1, int sizeY = 1) =>
			Image.DrawRectangle(
				x: x,
				y: y,
				color: color,
				rectWidth: sizeX,
				rectHeight: sizeY,
				width: Width);
		public override void RectLeft(int x, int y, byte voxel, int sizeX = 1, int sizeY = 1) =>
			Rect(
				x: x,
				y: y,
				color: IVoxelColor.LeftFace(voxel),
				sizeX: sizeX,
				sizeY: sizeY);
		public override void RectRight(int x, int y, byte voxel, int sizeX = 1, int sizeY = 1) =>
			Rect(
				x: x,
				y: y,
				color: IVoxelColor.RightFace(voxel),
				sizeX: sizeX,
				sizeY: sizeY);
		public override void RectVertical(int x, int y, byte voxel, int sizeX = 1, int sizeY = 1) =>
			Rect(
				x: x,
				y: y,
				color: IVoxelColor.VerticalFace(voxel),
				sizeX: sizeX,
				sizeY: sizeY);
		#endregion IRectangleRenderer
	}
}
