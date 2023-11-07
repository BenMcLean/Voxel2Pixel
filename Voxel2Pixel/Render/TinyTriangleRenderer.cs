using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;

namespace Voxel2Pixel.Render
{
	public class TinyTriangleRenderer : ITriangleRenderer
	{
		public virtual IRectangleRenderer RectangleRenderer { get; set; }
		public virtual IVoxelColor VoxelColor { get; set; }
		public static int IsoWidth(IModel model) => (2 * (model.SizeX + model.SizeY)) >> 1;
		public static int IsoHeight(IModel model) => (2 * (model.SizeX + model.SizeY) + 4 * model.SizeZ - 1) >> 2;
		public static void IsoLocate(out int pixelX, out int pixelY, IModel model, int voxelX = 0, int voxelY = 0, int voxelZ = 0)
		{
			pixelX = (2 * (model.SizeY + voxelX - voxelY)) >> 1;
			pixelY = (2 * (model.SizeX + model.SizeY) + 4 * model.SizeZ - 1 - 2 * (voxelX + voxelY) - 4 * voxelZ - 1) >> 2;
		}
		#region ITriangleRenderer
		public virtual void Tri(int x, int y, bool right, uint color) =>
			RectangleRenderer.Rect(
				x: (x >> 1)//divided by 2
					+ (right ? 0 : 1),
				y: y >> 2,//divided by 4
				color: color);
		public virtual void Tri(int x, int y, bool right, byte voxel, VisibleFace visibleFace = VisibleFace.Front) => Tri(
			x: x,
			y: y,
			right: right,
			color: VoxelColor[voxel, visibleFace]);
		#endregion ITriangleRenderer
	}
}
