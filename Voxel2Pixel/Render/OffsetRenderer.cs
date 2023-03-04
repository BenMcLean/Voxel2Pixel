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
		public override void Rect(int x, int y, uint color, int sizeX = 1, int sizeY = 1) =>
			RectangleRenderer.Rect(
				x: x + OffsetX,
				y: y + OffsetY,
				color: color,
				sizeX: sizeX * ScaleX,
				sizeY: sizeY * ScaleY);
		#endregion IRectangleRenderer
	}
}
