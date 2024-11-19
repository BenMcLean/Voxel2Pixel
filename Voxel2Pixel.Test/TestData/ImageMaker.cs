using ImageMagick;
using SkiaSharp;
using Voxel2Pixel.Interfaces;

namespace Voxel2Pixel.Test
{
	/// <summary>
	/// ImageMaker glues Voxel2Pixel to SkiaSharp and Magick.NT.
	/// It isn't included in the main project so that the library won't be subject to the ImageSharp license and could output to anything else instead.
	/// There's another ImageMaker class in the Test project which connects to ImageSharp.
	/// </summary>
	public static class ImageMaker
	{
		#region SkiaSharp
		public static Stream PngStream(this ISprite sprite) => sprite.Texture.PngStream(sprite.Width);
		public static Stream PngStream(this byte[] pixels, ushort width = 0)
		{
			if (width < 1)
				width = (ushort)Math.Sqrt(pixels.Length >> 2);
			return SKImage.FromPixelCopy(
					info: new SKImageInfo(
						width: width,
						height: (pixels.Length >> 2) / width,
						colorType: SKColorType.Rgba8888),
					pixels: pixels,
					rowBytes: width << 2)
				.Encode()
				.AsStream();
		}
		public static void Png(this ISprite sprite, string path) => sprite.Texture.Png(path: path, width: sprite.Width);
		public static void Png(this byte[] pixels, string path, ushort width = 0)
		{
			using FileStream fileStream = new(
				path: path,
				mode: FileMode.Create,
				access: FileAccess.Write);
			PngStream(pixels, width).CopyTo(fileStream);
		}
		public static string PngBase64(this ISprite sprite) => sprite.Texture.PngBase64(sprite.Width);
		/// <summary>
		/// Based on https://github.com/SixLabors/ImageSharp/blob/ede2f2d2d1e567dea74a44a482099302af9ed14d/src/ImageSharp/ImageExtensions.cs#L173-L183
		/// </summary>
		public static string PngBase64(this byte[] pixels, ushort width = 0)
		{
			using MemoryStream memoryStream = new();
			PngStream(pixels, width).CopyTo(memoryStream);
			memoryStream.TryGetBuffer(out ArraySegment<byte> buffer);
			return "data:image/png;base64," + Convert.ToBase64String(buffer.Array ?? [], 0, (int)memoryStream.Length);
		}
		#endregion SkiaSharp
		#region Magick.NET
		/// <summary>
		/// https://github.com/dlemstra/Magick.NET/blob/main/docs/ReadingImages.md
		/// </summary>
		public static MagickImage MagickImage(this ISprite sprite) => MagickImage(pixels: sprite.Texture, width: sprite.Width);
		public static MagickImage MagickImage(this byte[] pixels, ushort width = 0)
		{
			if (width < 1)
				width = (ushort)Math.Sqrt(pixels.Length >> 2);
			MagickImage magickImage = new();
			magickImage.ReadPixels(
				data: pixels,
				settings: new PixelReadSettings(
					width: width,
					height: (uint)((pixels.Length >> 2) / width),
					storageType: StorageType.Int32,
					mapping: PixelMapping.RGBA));
			return magickImage;
		}
		public static string MagickImagePngBase64(this ISprite sprite)
		{
			MagickImage magickImage = sprite.MagickImage();
			magickImage.Format = MagickFormat.Png;
			return "data:image/png;base64," + magickImage.ToBase64();
		}
		#endregion Magick.NET
	}
}
