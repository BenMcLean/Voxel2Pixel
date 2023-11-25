﻿namespace Voxel2Pixel.Pack
{
	/// <summary>
	/// Sprite, except that triangles are drawn at 2x horizontal scale.
	/// </summary>
	public class Sprite2x : Sprite
	{
		public override void Tri(ushort x, ushort y, bool right, uint color)
		{
			if (right)
			{
				Rect(
					x: (ushort)(x << 1),
					y: y,
					color: color,
					sizeX: 2);
				Rect(
					x: (ushort)(x << 1),
					y: (ushort)(y + 1),
					color: color,
					sizeX: 4);
				Rect(
					x: (ushort)(x << 1),
					y: (ushort)(y + 2),
					color: color,
					sizeX: 2);
			}
			else
			{
				Rect(
					x: (ushort)((x + 1) << 1),
					y: y,
					color: color,
					sizeX: 2);
				Rect(
					x: (ushort)(x << 1),
					y: (ushort)(y + 1),
					color: color,
					sizeX: 4);
				Rect(
					x: (ushort)((x + 1) << 1),
					y: (ushort)(y + 2),
					color: color,
					sizeX: 2);
			}
		}
	}
}
