namespace Voxel2Pixel.Pack
{
	/// <summary>
	/// IndexedSprite, except that triangles are drawn at 2x horizontal scale.
	/// </summary>
	public class IndexedSprite2x : IndexedSprite
	{
		public override void Tri(ushort x, ushort y, bool right, byte index)
		{
			Rect(
				x: (ushort)((x + (right ? 0 : 1)) << 1),
				y: y,
				index: index,
				sizeX: 2,
				sizeY: 3);
			Rect(
				x: (ushort)((x + (right ? 1 : 0)) << 1),
				y: (ushort)(y + 1),
				index: index,
				sizeX: 2);
		}
	}
}
