using System.Collections.Generic;
using Voxel2Pixel.Model;

namespace Voxel2Pixel.Interfaces
{
	public interface ISparseModel
	{
		IEnumerable<Voxel> Voxels { get; }
		ushort SizeX { get; }
		ushort SizeY { get; }
		ushort SizeZ { get; }
	}
}
