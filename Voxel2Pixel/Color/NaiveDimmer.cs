using Voxel2Pixel.Draw;

namespace Voxel2Pixel.Color
{
	public class NaiveDimmer : IVoxelColor, IDimmer
	{
		public NaiveDimmer(int[] palette)
		{
			Palette = new int[5][];
			Palette[2] = palette;
			for (int brightness = 0; brightness < Palette.Length; brightness++)
				if (brightness != 2)
					Palette[brightness] = new int[Palette[2].Length];
			for (int color = 0; color < Palette[2].Length; color++)
			{
				Palette[0][color] = Palette[2][color].LerpColor(0x000000FF, 0.5f);
				Palette[1][color] = Palette[2][color].LerpColor(0x000000FF, 0.25f);
				Palette[3][color] = Palette[2][color].LerpColor(unchecked((int)0xFFFFFFFF), 0.25f);
				Palette[4][color] = Palette[2][color].LerpColor(unchecked((int)0xFFFFFFFF), 0.5f);
			}
		}
		private int[][] Palette { get; set; }
		#region IDimmer
		public int Dark(byte voxel) => Dimmer(0, voxel);
		public int Dim(byte voxel) => Dimmer(1, voxel);
		public int Medium(byte voxel) => Dimmer(2, voxel);
		public int Light(byte voxel) => Dimmer(3, voxel);
		public int Bright(byte voxel) => Dimmer(4, voxel);
		public int Dimmer(int brightness, byte voxel) => Palette[brightness][voxel];
		#endregion IDimmer
		#region IVoxelColor
		public int VerticalFace(byte voxel) => Light(voxel);
		public int LeftFace(byte voxel) => Dim(voxel);
		public int RightFace(byte voxel) => Medium(voxel);
		#endregion IVoxelColor
	}
}
