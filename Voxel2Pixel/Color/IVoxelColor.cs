namespace Voxel2Pixel.Color
{
	public interface IVoxelColor
	{
		int VerticalFace(byte voxel);
		int LeftFace(byte voxel);
		int RightFace(byte voxel);
	}
}
