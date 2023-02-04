namespace Voxel2Pixel.Color
{
	public class FlatVoxelColor : IVoxelColor
	{
		public FlatVoxelColor(uint[] palette) => Palette = palette;
		public uint[] Palette { get; set; }
		#region IVoxelColor
		public uint LeftFace(byte voxel) => Palette[voxel];
		public uint RightFace(byte voxel) => Palette[voxel];
		public uint VerticalFace(byte voxel) => Palette[voxel];
		#endregion IVoxelColor
	}
}
