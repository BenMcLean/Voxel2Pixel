namespace Voxel2Pixel.Color
{
	public class FlatVoxelColor : IVoxelColor
	{
		public int[] Palette { get; set; }
		public int LeftFace(byte voxel) => Palette[voxel];
		public int RightFace(byte voxel) => Palette[voxel];
		public int VerticalFace(byte voxel) => Palette[voxel];
	}
}
