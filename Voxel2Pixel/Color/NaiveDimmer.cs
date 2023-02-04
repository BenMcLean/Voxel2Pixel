using Voxel2Pixel.Draw;

namespace Voxel2Pixel.Color
{
	public class NaiveDimmer : IVoxelColor, IDimmer
	{
		public NaiveDimmer(uint[] palette)
		{
			Palette = new uint[5][];
			Palette[2] = palette;
			for (int brightness = 0; brightness < Palette.Length; brightness++)
				if (brightness != 2)
					Palette[brightness] = new uint[Palette[2].Length];
			for (int color = 0; color < Palette[2].Length; color++)
			{
				Palette[0][color] = Palette[2][color].LerpColor(0x000000FF, 0.5f);
				Palette[1][color] = Palette[2][color].LerpColor(0x000000FF, 0.25f);
				Palette[3][color] = Palette[2][color].LerpColor(0xFFFFFFFF, 0.25f);
				Palette[4][color] = Palette[2][color].LerpColor(0xFFFFFFFF, 0.5f);
			}
		}
		private uint[][] Palette { get; set; }
		#region IDimmer
		public uint Dark(byte voxel) => Dimmer(0, voxel);
		public uint Dim(byte voxel) => Dimmer(1, voxel);
		public uint Medium(byte voxel) => Dimmer(2, voxel);
		public uint Light(byte voxel) => Dimmer(3, voxel);
		public uint Bright(byte voxel) => Dimmer(4, voxel);
		public uint Dimmer(int brightness, byte voxel) => Palette[brightness][voxel];
		#endregion IDimmer
		#region IVoxelColor
		public uint VerticalFace(byte voxel) => Light(voxel);
		public uint LeftFace(byte voxel) => Dim(voxel);
		public uint RightFace(byte voxel) => Medium(voxel);
		#endregion IVoxelColor
	}
}
