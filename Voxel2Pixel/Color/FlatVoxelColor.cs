namespace Voxel2Pixel.Color
{
	public class FlatVoxelColor : IVoxelColorIso
	{
		public FlatVoxelColor(uint[] palette) => Palette = palette;
		public uint[] Palette { get; set; }
		#region IVoxelColorIso
		public bool Iso { get; set; } = true;
		public uint LeftFace(byte voxel) => Palette[voxel];
		public uint RightFace(byte voxel) => Palette[voxel];
		public uint VerticalFace(byte voxel) => Palette[voxel];
		#endregion IVoxelColorIso
	}
}
