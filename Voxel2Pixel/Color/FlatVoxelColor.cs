namespace Voxel2Pixel.Color
{
	public class FlatVoxelColor : IVoxelColor
	{
		public uint[] Palette { get; set; }
		public uint LeftFace(byte voxel) => Palette[voxel];
		public uint RightFace(byte voxel) => Palette[voxel];
		public uint VerticalFace(byte voxel) => Palette[voxel];
	}
}
