namespace Voxel2Pixel.Render
{
	/// <summary>
	/// ArrayRenderer, except that triangles are drawn at 2x horizontal scale.
	/// </summary>
	public class Array2xRenderer : ArrayRenderer
	{
		public override void Tri(int x, int y, bool right, uint color)
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
	}
}
