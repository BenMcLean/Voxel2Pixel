using Voxel2Pixel.Model;

namespace Voxel2Pixel.Interfaces
{
	public interface IEditableModel : IModel
	{
		new byte this[ushort x, ushort y, ushort z] { get; set; }
		byte Set(Voxel voxel);
	}
}
