using System.Collections;
using System.Collections.Generic;

namespace BenVoxel;

/// <summary>
/// The Voxel enumeration is sparse: it includes only all the non-zero voxels in an undefined order.
/// </summary>
public interface IModel : IEnumerable<Voxel>, IEnumerable
{
	byte this[ushort x, ushort y, ushort z] { get; }
	ushort SizeX { get; }
	ushort SizeY { get; }
	ushort SizeZ { get; }
}
