using System.Collections.Generic;
using BenVoxel.Structs;

namespace BenVoxel.Interfaces;

/// <summary>
/// The VoxelBrick enumeration is sparse: it includes only all the non-empty bricks in an undefined order.
/// </summary>
public interface IBrickModel : IModel, IEnumerable<VoxelBrick>
{
	// Default implementation of the IModel byte-access
	// This makes any IBrickModel automatically compliant with IModel!
	// byte IModel.this[ushort x, ushort y, ushort z] => VoxelBrick.GetVoxel(GetBrick(x, y, z), x & 1, y & 1, z & 1);
	/// <summary>
	/// Retrieves a brick payload at the given world coordinates.
	/// Implementation should snap x,y,z to the nearest brick origin (multiple of 2)
	/// internally using: x &amp; ~1, y &amp; ~1, z &amp; ~1 (or x &amp; 0xFFFE, etc.)
	/// </summary>
	ulong GetBrick(ushort x, ushort y, ushort z);
}
