namespace Voxel2Pixel.Interfaces
{
	public interface IVoxelColor
	{
		uint TopFace(byte voxel);
		uint RightFace(byte voxel);
		uint FrontFace(byte voxel);
		uint LeftFace(byte voxel);
	}
}
