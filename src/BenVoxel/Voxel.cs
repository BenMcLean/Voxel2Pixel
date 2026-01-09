namespace BenVoxel;

public readonly record struct Voxel(ushort X, ushort Y, ushort Z, byte Material)
{
	public static implicit operator Point3D(Voxel voxel) => new()
	{
		X = voxel.X,
		Y = voxel.Y,
		Z = voxel.Z,
	};
}
