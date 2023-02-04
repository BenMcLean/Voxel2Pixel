﻿namespace Voxel2Pixel.Color
{
	/// <summary>
	/// An IDimmer converts from the color index of a voxel to an actual color based on lighting, like a dimmer on a light switch. This converts from byte indices to RGBA8888 ints for actual colors.
	/// </summary>
	public interface IDimmer
	{
		/// <returns>Same as Dimmer(0, voxel)</returns>
		uint Dark(byte voxel);
		/// <returns>Same as Dimmer(1, voxel)</returns>
		uint Dim(byte voxel);
		/// <returns>Same as Dimmer(2, voxel)</returns>
		uint Medium(byte voxel);
		/// <returns>Same as Dimmer(3, voxel)</returns>
		uint Light(byte voxel);
		/// <returns>Same as Dimmer(4, voxel)</returns>
		uint Bright(byte voxel);
		/// <param name="brightness">0 for dark, 1 for dim, 2 for medium, 3 for light and 4 for bright</param>
		/// <param name="voxel">The color index of a voxel</param>
		/// <returns>An rgba8888 color</returns>
		uint Dimmer(int brightness, byte voxel);
	}
}
