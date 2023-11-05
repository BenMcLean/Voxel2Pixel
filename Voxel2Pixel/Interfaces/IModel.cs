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
		/// <summary>
		/// Checking for being out of bounds can involve fewer comparisons than checking for being in bounds.
		/// </summary>
		/// <returns>true if coordinate is outside the bounds of the model</returns>
		bool IsOutside(ushort x, ushort y, ushort z);
	}
}
