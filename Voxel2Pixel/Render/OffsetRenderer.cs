using Voxel2Pixel.Interfaces;

namespace Voxel2Pixel.Render
{
	public class OffsetRenderer : TriangleRenderer
	{
		public IRectangleRenderer RectangleRenderer
		{
			get => rectangleRenderer;
			set
			{
				rectangleRenderer = value;
				if (rectangleRenderer is IVoxelColor voxelColor)
					VoxelColor = voxelColor;
			}
		}
		private IRectangleRenderer rectangleRenderer;
		public int OffsetX { get; set; } = 0;
		public int OffsetY { get; set; } = 0;
		public int ScaleX { get; set; } = 1;
		public int ScaleY { get; set; } = 1;
		#region IRectangleRenderer
		public override void Rect(ushort x, ushort y, uint color, ushort sizeX = 1, ushort sizeY = 1)
		{
			int x2 = x * ScaleX + OffsetX,
				y2 = y * ScaleY + OffsetY,
				sizeX2 = sizeX * ScaleX,
				sizeY2 = sizeY * ScaleY;
			if (x2 < 0)
			{
				sizeX2 += x2;
				x2 = 0;
			}
			if (y2 < 0)
			{
				sizeY2 += y2;
				y2 = 0;
			}
			if (sizeX2 <= 0 || sizeY2 <= 0)
				return;
			RectangleRenderer.Rect(
				x: (ushort)x2,
				y: (ushort)y2,
				color: color,
				sizeX: (ushort)sizeX2,
				sizeY: (ushort)sizeY2);
		}
		#endregion IRectangleRenderer
	}
}
