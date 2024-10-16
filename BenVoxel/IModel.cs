using System.Collections;
using System.Collections.Generic;

namespace BenVoxel;

public interface IModel : IEnumerable<Voxel>, IEnumerable
{
	byte this[ushort x, ushort y, ushort z] { get; }
	ushort SizeX { get; }
	ushort SizeY { get; }
	ushort SizeZ { get; }
}
