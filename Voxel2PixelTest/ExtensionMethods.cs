using static Voxel2Pixel.TextureMethods;

namespace Voxel2PixelTest
{
	public static class ExtensionMethods
	{
		#region Bitmap
		/// <param name="source">rgba8888 pixel data</param>
		/// <param name="width">width or 0 for square images</param>
		public static System.Drawing.Bitmap Bitmap(this byte[] source, int width = 0)
		{
			byte[] bytes = new byte[source.Length];
			System.Array.Copy(source, bytes, source.Length);
			for (int x = 0; x < bytes.Length; x += 4)
			{
				bytes[x] = source[x + 2];
				bytes[x + 2] = source[x];
			}
			int height = width == 0 ? (int)System.Math.Sqrt(source.Length / 4) : source.Length / 4 / width;
			if (width == 0) width = height;
			System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			System.Drawing.Imaging.BitmapData bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, bitmap.PixelFormat);
			System.Runtime.InteropServices.Marshal.Copy(bytes, 0, bitmapData.Scan0, bytes.Length);
			bitmap.UnlockBits(bitmapData);
			return bitmap;
		}
		#endregion Bitmap
	}
}
