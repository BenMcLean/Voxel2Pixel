using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;

namespace Voxel2Pixel.Render;

/// <summary>
/// Provides a default implementation of ITriangleRenderer by calling IRectangleRenderer, while leaving the Rect methods abstract.
/// </summary>
public abstract class Renderer : IRenderer
{
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
	public virtual void Tri(ushort x, ushort y, bool right, byte index, VisibleFace visibleFace = VisibleFace.Front)
	{
		if (right)
		{
			Rect(
				x: x,
				y: y,
				index: index,
				visibleFace: visibleFace);
			Rect(
				x: x,
				y: (ushort)(y + 1),
				index: index,
				visibleFace: visibleFace,
				sizeX: 2);
			Rect(
				x: x,
				y: (ushort)(y + 2),
				index: index,
				visibleFace: visibleFace);
		}
		else
		{
			Rect(
				x: (ushort)(x + 1),
				y: y,
				index: index,
				visibleFace: visibleFace);
			Rect(
				x: x,
				y: (ushort)(y + 1),
				index: index,
				visibleFace: visibleFace,
				sizeX: 2);
			Rect(
				x: (ushort)(x + 1),
				y: (ushort)(y + 2),
				index: index,
				visibleFace: visibleFace);
		}
	}
	public virtual void Diamond(ushort x, ushort y, uint color)
	{
		Rect(
			x: (ushort)(x + 1),
			y: y,
			color: color,
			sizeX: 2);
		Rect(
			x: x,
			y: (ushort)(y + 1),
			color: color,
			sizeX: 4);
		Rect(
			x: (ushort)(x + 1),
			y: (ushort)(y + 2),
			color: color,
			sizeX: 2);
	}
	public virtual void Diamond(ushort x, ushort y, byte index, VisibleFace visibleFace = VisibleFace.Front)
	{
		Rect(
			x: (ushort)(x + 1),
			y: y,
			index: index,
			visibleFace: visibleFace,
			sizeX: 2);
		Rect(
			x: x,
			y: (ushort)(y + 1),
			index: index,
			visibleFace: visibleFace,
			sizeX: 4);
		Rect(
			x: (ushort)(x + 1),
			y: (ushort)(y + 2),
			index: index,
			visibleFace: visibleFace,
			sizeX: 2);
	}
	#endregion ITriangleRenderer
	#region IRectangleRenderer
	public abstract void Rect(ushort x, ushort y, uint color, ushort sizeX = 1, ushort sizeY = 1);
	public abstract void Rect(ushort x, ushort y, byte index, VisibleFace visibleFace = VisibleFace.Front, ushort sizeX = 1, ushort sizeY = 1);
	#endregion IRectangleRenderer
}
