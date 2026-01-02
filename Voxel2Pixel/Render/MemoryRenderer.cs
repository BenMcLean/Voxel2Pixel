using System.Collections;
using System.Collections.Generic;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;

namespace Voxel2Pixel.Render;

/// <summary>
/// Stores a series of Rect calls that can be replayed back onto another IRectangleRenderer.
/// </summary>
public class MemoryRenderer : Renderer, IList<MemoryRenderer.Rectangle>
{
	#region MemoryRenderer
	public void Rect(IRectangleRenderer renderer) => Rectangles.ForEach(rect => rect.Rect(renderer));
	public readonly record struct Rectangle(ushort X, ushort Y, ushort SizeX = 1, ushort SizeY = 1, byte Index = 0, VisibleFace VisibleFace = VisibleFace.Front, uint Color = 0u)
	{
		public void Rect(IRectangleRenderer renderer)
		{
			if (Index == 0)
				renderer.Rect(
					x: X,
					y: Y,
					color: Color,
					sizeX: SizeX,
					sizeY: SizeY);
			else
				renderer.Rect(
					x: X,
					y: Y,
					index: Index,
					visibleFace: VisibleFace,
					sizeX: SizeX,
					sizeY: SizeY);
		}
	}
	protected List<Rectangle> Rectangles = [];
	#endregion MemoryRenderer
	#region IRectangleRenderer
	public override void Rect(ushort x, ushort y, uint color, ushort sizeX = 1, ushort sizeY = 1) => Rectangles.Add(new(X: x, Y: y, SizeX: sizeX, SizeY: sizeY, Color: color));
	public override void Rect(ushort x, ushort y, byte index, VisibleFace visibleFace = VisibleFace.Front, ushort sizeX = 1, ushort sizeY = 1) => Rectangles.Add(new(X: x, Y: y, SizeX: sizeX, SizeY: sizeY, Index: index, VisibleFace: visibleFace));
	#endregion IRectangleRenderer
	#region IList
	public int IndexOf(Rectangle item) => ((IList<Rectangle>)Rectangles1).IndexOf(item);
	public void Insert(int index, Rectangle item) => ((IList<Rectangle>)Rectangles1).Insert(index, item);
	public void RemoveAt(int index) => ((IList<Rectangle>)Rectangles1).RemoveAt(index);
	public void Add(Rectangle item) => ((ICollection<Rectangle>)Rectangles1).Add(item);
	public void Clear() => ((ICollection<Rectangle>)Rectangles1).Clear();
	public bool Contains(Rectangle item) => ((ICollection<Rectangle>)Rectangles1).Contains(item);
	public void CopyTo(Rectangle[] array, int arrayIndex) => ((ICollection<Rectangle>)Rectangles1).CopyTo(array, arrayIndex);
	public bool Remove(Rectangle item) => ((ICollection<Rectangle>)Rectangles1).Remove(item);
	public IEnumerator<Rectangle> GetEnumerator() => ((IEnumerable<Rectangle>)Rectangles1).GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Rectangles1).GetEnumerator();
	public int Count => ((ICollection<Rectangle>)Rectangles1).Count;
	public bool IsReadOnly => ((ICollection<Rectangle>)Rectangles1).IsReadOnly;
	public List<Rectangle> Rectangles1 { get => Rectangles; set => Rectangles = value; }
	public Rectangle this[int index] { get => ((IList<Rectangle>)Rectangles1)[index]; set => ((IList<Rectangle>)Rectangles1)[index] = value; }
	#endregion IList
}
