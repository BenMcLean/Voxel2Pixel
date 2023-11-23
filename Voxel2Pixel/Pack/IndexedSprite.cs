using Voxel2Pixel.Draw;
using Voxel2Pixel.Interfaces;

namespace Voxel2Pixel.Pack
{
	public class IndexedSprite : ISprite
	{
		#region ISprite
		public byte[] Texture => GetTexture();
		public ushort Width => (ushort)Pixels.GetLength(0);
		public ushort Height => (ushort)Pixels.GetLength(1);
		public ushort OriginX { get; set; }
		public ushort OriginY { get; set; }
		#endregion ISprite
		#region IndexedSprite
		public byte[,] Pixels { get; set; }
		public uint[] Palette { get; set; }
		public byte[] GetTexture(bool transparent0 = true) => GetTexture(Pixels, Palette, transparent0);
		public byte[] GetTexture(uint[] palette, bool transparent0 = true) => GetTexture(Pixels, palette, transparent0);
		public static byte[] GetTexture(byte[,] pixels, uint[] palette, bool transparent0 = true)
		{
			ushort width = (ushort)pixels.GetLength(0),
				height = (ushort)pixels.GetLength(1);
			byte[] texture = new byte[(width * height) << 2];
			for (int y = height - 1, index = 0; y >= 0; y--)
				for (ushort x = 0; x < width; x++, index += 4)
					if (pixels[x, y] is byte pixel
						&& (!transparent0 || pixel != 0))
						texture.Write(palette[pixel], index);
			return texture;
		}
		#endregion IndexedSprite
	}
}
