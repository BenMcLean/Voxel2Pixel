using Voxel2Pixel.Draw;

namespace Voxel2Pixel.Render
{
	public class ArrayRenderer : TriangleRenderer
	{
		public byte[] Image { get; set; }
		public int Width { get; set; }
		public int Height => (Image.Length / Width) >> 2;
		#region IRectangleRenderer
		public override void Rect(ushort x, ushort y, uint color, ushort sizeX = 1, ushort sizeY = 1) =>
			Image.DrawRectangle(
				x: x,
				y: y,
				color: color,
				rectWidth: sizeX,
				rectHeight: sizeY,
				width: Width);
		#endregion IRectangleRenderer
	}
}
