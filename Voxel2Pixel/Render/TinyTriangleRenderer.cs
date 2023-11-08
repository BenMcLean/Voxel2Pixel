using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;

namespace Voxel2Pixel.Render
{
	public class TinyTriangleRenderer : ITriangleRenderer
	{
		public virtual IRectangleRenderer RectangleRenderer { get; set; }
		public virtual IVoxelColor VoxelColor { get; set; }
		public static int IsoWidth(IModel model) => model.SizeX + model.SizeY;
		public static int IsoHeight(IModel model) => (model.SizeX + model.SizeY) / 2 + model.SizeZ - 1;
		public static void IsoLocate(out int pixelX, out int pixelY, IModel model, int voxelX = 0, int voxelY = 0, int voxelZ = 0)
		{
			pixelX = model.SizeY + voxelX - voxelY;
			pixelY = ((model.SizeX + model.SizeY - 1) + 2 * model.SizeZ - (voxelX + voxelY)) / 2 - voxelZ;
		}
		#region ITriangleRenderer
		public virtual void Tri(ushort x, ushort y, bool right, uint color) =>
			RectangleRenderer.Rect(
				x: (ushort)(x / 2 + (right ? 0 : 1)),
				y: (ushort)(y / 4),
				color: color);
		public virtual void Tri(ushort x, ushort y, bool right, byte voxel, VisibleFace visibleFace = VisibleFace.Front) => Tri(
			x: x,
			y: y,
			right: right,
			color: VoxelColor[voxel, visibleFace]);
		#endregion ITriangleRenderer
	}
}
