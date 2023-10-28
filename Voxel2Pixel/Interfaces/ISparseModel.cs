using System.Collections.Generic;
using Voxel2Pixel.Model;

namespace Voxel2Pixel.Interfaces
{
	public interface ISparseModel
	{
		IEnumerable<Voxel> Voxels { get; }
		int SizeX { get; }
		int SizeY { get; }
		int SizeZ { get; }
	}
}
