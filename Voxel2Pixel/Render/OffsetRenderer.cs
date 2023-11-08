using Voxel2Pixel.Interfaces;

namespace Voxel2Pixel.Render
{
	public class OffsetRenderer : TriangleRenderer
	{
		public IRectangleRenderer RectangleRenderer { get; set; }
		public int OffsetX { get; set; } = 0;
		public int OffsetY { get; set; } = 0;
		public int ScaleX { get; set; } = 1;
		public int ScaleY { get; set; } = 1;
		#region IRectangleRenderer
		public override void Rect(ushort x, ushort y, uint color, ushort sizeX = 1, ushort sizeY = 1) =>
			RectangleRenderer.Rect(
				x: (ushort)(x * ScaleX + OffsetX),
				y: (ushort)(y * ScaleY + OffsetY),
				color: color,
				sizeX: (ushort)(sizeX * ScaleX),
				sizeY: (ushort)(sizeY * ScaleY));
		#endregion IRectangleRenderer
	}
}
