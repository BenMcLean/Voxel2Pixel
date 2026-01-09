using BenVoxel.Structs;
using Voxel2Pixel.Interfaces;

namespace Voxel2Pixel.Render;

public abstract class ColoredRenderer : Renderer, IColoredRenderer
{
	#region Renderer
	public override void Rect(ushort x, ushort y, byte index, VisibleFace visibleFace = VisibleFace.Front, ushort sizeX = 1, ushort sizeY = 1) => Rect(
		x: x,
		y: y,
		color: VoxelColor[index, visibleFace],
		sizeX: sizeX,
		sizeY: sizeY);
	#endregion Renderer
	#region IVoxelColor
	public virtual IVoxelColor VoxelColor { get; set; }
	public virtual uint this[byte index, VisibleFace visibleFace = VisibleFace.Front] => VoxelColor[index, visibleFace];
	#endregion IVoxelColor
}
