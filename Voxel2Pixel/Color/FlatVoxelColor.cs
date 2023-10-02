namespace Voxel2Pixel.Color
{
	public class FlatVoxelColor : IVoxelColor
	{
		public FlatVoxelColor(uint[] palette) => Palette = palette;
		public uint[] Palette { get; set; }
		#region IVoxelColor
		public bool Iso { get; set; } = true;
		public uint TopFace(byte voxel) => Palette[voxel];
		public uint RightFace(byte voxel) => Palette[voxel];
		public uint FrontFace(byte voxel) => Palette[voxel];
		public uint LeftFace(byte voxel) => Palette[voxel];
		#endregion IVoxelColor
	}
}
