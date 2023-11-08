﻿using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;

namespace Voxel2Pixel.Render
{
	/// <summary>
	/// Provides a default implementation of ITriangleRenderer by calling IRectangleRenderer, while leaving the Rect method abstract.
	/// </summary>
	public abstract class TriangleRenderer : IRectangleRenderer, ITriangleRenderer
	{
		public virtual IVoxelColor VoxelColor { get; set; }
		#region ITriangleRenderer
		public virtual void Tri(ushort x, ushort y, bool right, uint color)
		{
			if (right)
			{
				Rect(
					x: x,
					y: y,
					color: color);
				Rect(
					x: x,
					y: (ushort)(y + 1),
					color: color,
					sizeX: 2);
				Rect(
					x: x,
					y: (ushort)(y + 2),
					color: color);
			}
			else
			{
				Rect(
					x: (ushort)(x + 1),
					y: y,
					color: color);
				Rect(
					x: x,
					y: (ushort)(y + 1),
					color: color,
					sizeX: 2);
				Rect(
					x: (ushort)(x + 1),
					y: (ushort)(y + 2),
					color: color);
			}
		}
		public virtual void Tri(ushort x, ushort y, bool right, byte voxel, VisibleFace visibleFace = VisibleFace.Front) => Tri(
			x: x,
			y: y,
			right: right,
			color: VoxelColor[voxel, visibleFace]);
		#endregion ITriangleRenderer
		#region IRectangleRenderer
		public abstract void Rect(ushort x, ushort y, uint color, ushort sizeX = 1, ushort sizeY = 1);
		public virtual void Rect(ushort x, ushort y, byte voxel, VisibleFace visibleFace = VisibleFace.Front, ushort sizeX = 1, ushort sizeY = 1) => Rect(
			x: x,
			y: y,
			color: VoxelColor[voxel, visibleFace],
			sizeX: sizeX,
			sizeY: sizeY);
		#endregion IRectangleRenderer
	}
}
