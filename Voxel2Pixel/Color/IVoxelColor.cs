namespace Voxel2Pixel.Color
{
	public interface IVoxelColor
	{
		uint TopFace(byte voxel);
		uint RightFace(byte voxel);
		uint FrontFace(byte voxel);
		uint LeftFace(byte voxel);
	}
}
