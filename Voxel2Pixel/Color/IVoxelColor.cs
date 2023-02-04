namespace Voxel2Pixel.Color
{
	public interface IVoxelColor
	{
		uint VerticalFace(byte voxel);
		uint LeftFace(byte voxel);
		uint RightFace(byte voxel);
	}
}
