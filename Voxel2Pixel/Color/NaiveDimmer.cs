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
				{
					Palette[brightness] = new int[Palette[2].Length];
					double adjustment = (brightness + 1) / 3d;
					for (int color = 0; color < Palette[brightness].Length; color++)
						Palette[brightness][color] = Adjust(Palette[2][color], adjustment);
				}
		}
		private int[][] Palette { get; set; }
		public static int Adjust(int color, double amount) =>
			TextureMethods.Color(
				r: Adjust(TextureMethods.R(color), amount),
				g: Adjust(TextureMethods.G(color), amount),
				b: Adjust(TextureMethods.B(color), amount),
				a: byte.MaxValue);
		public static byte Adjust(byte component, double amount) =>
			amount < 1 ?
				(byte)(component / amount)
				: (byte)(component + (byte)((byte.MaxValue - component) / amount));
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
