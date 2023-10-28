using System.Collections.Generic;
using Voxel2Pixel.Model;

namespace Voxel2Pixel.Interfaces
{
	public interface ISparseModel : IModel
	{
		IEnumerable<Voxel> Voxels { get; }
	}
}
