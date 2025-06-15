using ImageMagick;
using SixLabors.ImageSharp;
using SkiaSharp;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Render;
using static Voxel2Pixel.Draw.PixelDraw;

namespace Voxel2Pixel.Test.TestData;

/// <summary>
/// ImageMaker glues Voxel2Pixel to ImageSharp, SkiaSharp and Magick.NT.
/// It isn't included in the main project so that the library won't be subject to the ImageSharp license and could output to anything else instead.
/// </summary>
public static class ImageMaker
{
	public const int DefaultFrameDelay = 100;
	#region ImageSharp
	public static Image<SixLabors.ImageSharp.PixelFormats.Rgba32> Png(ushort width = 0, params byte[] bytes)
	{
		if (width < 1)
			width = (ushort)Math.Sqrt(bytes.Length >> 2);
		return Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(
			data: bytes,
			width: width,
			height: (bytes.Length >> 2) / width);
	}
	public static Image<SixLabors.ImageSharp.PixelFormats.Rgba32> Png(this ISprite sprite) => Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(
		data: sprite.Texture,
		width: sprite.Width,
		height: sprite.Height);
	public static Image<SixLabors.ImageSharp.PixelFormats.Rgba32> AnimatedGif(int frameDelay = DefaultFrameDelay, ushort repeatCount = 0, params ISprite[] sprites) => sprites.AsEnumerable().AnimatedGif(frameDelay, repeatCount);
	public static Image<SixLabors.ImageSharp.PixelFormats.Rgba32> AnimatedGif(this IEnumerable<ISprite> sprites, int frameDelay = DefaultFrameDelay, ushort repeatCount = 0)
	{
		Sprite[] resized = [.. sprites.SameSize()];
		Image<SixLabors.ImageSharp.PixelFormats.Rgba32> gif = new(resized[0].Width, resized[0].Height);
		SixLabors.ImageSharp.Formats.Gif.GifMetadata gifMetaData = gif.Metadata.GetGifMetadata();
		gifMetaData.RepeatCount = repeatCount;
		gifMetaData.ColorTableMode = SixLabors.ImageSharp.Formats.Gif.GifColorTableMode.Local;
		foreach (Sprite sprite in resized)
		{
			Image<SixLabors.ImageSharp.PixelFormats.Rgba32> image = sprite.Png();
			SixLabors.ImageSharp.Formats.Gif.GifFrameMetadata metadata = image.Frames.RootFrame.Metadata.GetGifMetadata();
			metadata.FrameDelay = frameDelay;
			metadata.DisposalMethod = SixLabors.ImageSharp.Formats.Gif.GifDisposalMethod.RestoreToBackground;
			gif.Frames.AddFrame(image.Frames.RootFrame);
		}
		gif.Frames.RemoveFrame(0);//I don't know why ImageSharp has me doing this but if I don't then I get an extra transparent frame at the start.
		return gif;
	}
	public static Image<SixLabors.ImageSharp.PixelFormats.Rgba32> AnimatedGif(ushort width = 0, int frameDelay = DefaultFrameDelay, ushort repeatCount = 0, params byte[][] frames)
	{
		if (width < 1)
			width = (ushort)Math.Sqrt(frames[0].Length >> 2);
		int height = (frames[0].Length >> 2) / width;
		Image<SixLabors.ImageSharp.PixelFormats.Rgba32> gif = new(width, height);
		SixLabors.ImageSharp.Formats.Gif.GifMetadata gifMetaData = gif.Metadata.GetGifMetadata();
		gifMetaData.RepeatCount = repeatCount;
		gifMetaData.ColorTableMode = SixLabors.ImageSharp.Formats.Gif.GifColorTableMode.Local;
		foreach (byte[] frame in frames)
		{
			Image<SixLabors.ImageSharp.PixelFormats.Rgba32> image = Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(
				data: frame,
				width: width,
				height: height);
			SixLabors.ImageSharp.Formats.Gif.GifFrameMetadata metadata = image.Frames.RootFrame.Metadata.GetGifMetadata();
			metadata.FrameDelay = frameDelay;
			metadata.DisposalMethod = SixLabors.ImageSharp.Formats.Gif.GifDisposalMethod.RestoreToBackground;
			gif.Frames.AddFrame(image.Frames.RootFrame);
		}
		gif.Frames.RemoveFrame(0);//I don't know why ImageSharp has me doing this but if I don't then I get an extra transparent frame at the start.
		return gif;
	}
	public static Image<SixLabors.ImageSharp.PixelFormats.Rgba32> AnimatedGif(byte scaleX = 1, byte scaleY = 1, ushort width = 0, int frameDelay = DefaultFrameDelay, ushort repeatCount = 0, params byte[][] frames) => AnimatedGif(
		width: (ushort)(width * scaleX),
		frameDelay: frameDelay,
		repeatCount: repeatCount,
		frames: scaleX == 1 && scaleY == 1 ? frames
			: [.. frames.Parallelize(frame => frame.Upscale(
			scaleX: scaleX,
			scaleY: scaleY,
			width: width))]);
	#endregion ImageSharp
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
		pixels.PngStream(width).CopyTo(fileStream);
	}
	public static string PngBase64(this ISprite sprite) => sprite.Texture.PngBase64(sprite.Width);
	/// <summary>
	/// Based on https://github.com/SixLabors/ImageSharp/blob/ede2f2d2d1e567dea74a44a482099302af9ed14d/src/ImageSharp/ImageExtensions.cs#L173-L183
	/// </summary>
	public static string PngBase64(this byte[] pixels, ushort width = 0)
	{
		using MemoryStream memoryStream = new();
		pixels.PngStream(width).CopyTo(memoryStream);
		memoryStream.TryGetBuffer(out ArraySegment<byte> buffer);
		return "data:image/png;base64," + Convert.ToBase64String(buffer.Array ?? [], 0, (int)memoryStream.Length);
	}
	#endregion SkiaSharp
	#region Magick.NET
	/// <summary>
	/// https://github.com/dlemstra/Magick.NET/blob/main/docs/ReadingImages.md
	/// </summary>
	public static MagickImage MagickImage(this ISprite sprite) => sprite.Texture.MagickImage(width: sprite.Width);
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
