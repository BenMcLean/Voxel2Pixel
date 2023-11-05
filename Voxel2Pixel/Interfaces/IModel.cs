using System.Collections.Generic;
using Voxel2Pixel.Model;

namespace Voxel2Pixel.Interfaces
{
	public interface IModel
	{
		IEnumerable<Voxel> Voxels { get; }
		byte this[ushort x, ushort y, ushort z] { get; }
		ushort SizeX { get; }
		ushort SizeY { get; }
		ushort SizeZ { get; }
	}
}
