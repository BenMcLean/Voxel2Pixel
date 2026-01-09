namespace BenVoxel.Structs;

public readonly record struct VoxelBrick(ushort X, ushort Y, ushort Z, ulong Payload)
{
	/// <summary>
	/// Extracts a single voxel byte from the 2x2x2 payload.
	/// localX, localY, localZ must be 0 or 1.
	/// </summary>
	public byte GetVoxel(int localX, int localY, int localZ) => GetVoxel(Payload, localX, localY, localZ);
	public static byte GetVoxel(ulong payload, int localX, int localY, int localZ) =>
		(byte)(payload >> (((localZ & 1) << 2 | (localY & 1) << 1 | localX & 1) << 3) & 0xFF);
	/// <summary>
	/// A helper to "edit" a brick by returning a new one with one voxel changed
	/// localX, localY, localZ must be 0 or 1.
	/// </summary>
	public VoxelBrick WithVoxel(int localX, int localY, int localZ, byte material)
	{
		int shift = (localZ << 2 | localY << 1 | localX) << 3;
		ulong mask = 0xFFUL << shift,
			newPayload = Payload & ~mask | (ulong)material << shift;
		return this with { Payload = newPayload };
	}
}
