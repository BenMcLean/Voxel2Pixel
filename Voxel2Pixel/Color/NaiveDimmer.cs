using Voxel2Pixel.Draw;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;

namespace Voxel2Pixel.Color;

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
			Palette[0][color] = Palette[2][color].LerpColor(0x000000FF, 0.3f);
			Palette[1][color] = Palette[2][color].LerpColor(0x000000FF, 0.15f);
			Palette[3][color] = Palette[2][color].LerpColor(0xFFFFFFFF, 0.15f);
			Palette[4][color] = Palette[2][color].LerpColor(0xFFFFFFFF, 0.3f);
		}
	}
	#region Data members
	private uint[][] Palette { get; set; }
	#endregion Data members
	#region IDimmer
	public uint Dark(byte voxel) => Dimmer(0, voxel);
	public uint Dim(byte voxel) => Dimmer(1, voxel);
	public uint Medium(byte voxel) => Dimmer(2, voxel);
	public uint Light(byte voxel) => Dimmer(3, voxel);
	public uint Bright(byte voxel) => Dimmer(4, voxel);
	public uint Dimmer(int brightness, byte voxel) => Palette[brightness][voxel];
	#endregion IDimmer
	#region IVoxelColor
	public virtual uint this[byte voxel, VisibleFace visibleFace = VisibleFace.Front]
	{
		get
		{
			switch (visibleFace)
			{
				case VisibleFace.Top:
					return Bright(voxel);
				case VisibleFace.Right:
					return Light(voxel);
				case VisibleFace.Front:
					return Medium(voxel);
				case VisibleFace.Left:
					return Dim(voxel);
			}
			throw new System.IO.InvalidDataException();
		}
	}
	#endregion IVoxelColor
}
