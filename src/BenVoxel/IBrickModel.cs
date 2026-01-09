using System.Collections.Generic;

namespace BenVoxel;

/// <summary>
/// The VoxelBrick enumeration is sparse: it includes only all the non-empty bricks in an undefined order.
/// </summary>
public interface IBrickModel : IModel, IEnumerable<VoxelBrick>
{
	// Default implementation of the IModel byte-access
	// This makes any IBrickModel automatically compliant with IModel!
	// byte IModel.this[ushort x, ushort y, ushort z] => VoxelBrick.GetVoxel(GetBrick(x, y, z), x & 1, y & 1, z & 1);
	/// <summary>
	/// Implementation should snap x,y,z to the nearest multiple of 2 
	/// internally using: x & -1, y & -1, z & -1
	/// </summary>
	ulong GetBrick(ushort x, ushort y, ushort z);
}
