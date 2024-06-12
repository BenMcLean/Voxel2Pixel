using SixLabors.ImageSharp;
using SkiaSharp;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Render;

namespace Voxel2Pixel.Web
{
	/// <summary>
	/// ImageMaker glues Voxel2Pixel to ImageSharp.
	/// It isn't included in the main project so that the library won't be subject to the ImageSharp license and could output to anything else instead.
	/// </summary>
	public static class ImageMaker
	{
		public const int DefaultFrameDelay = 100;
		#region ImageSharp
		public static SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> Png(ushort width = 0, params byte[] bytes)
		{
			if (width < 1)
				width = (ushort)Math.Sqrt(bytes.Length >> 2);
			return SixLabors.ImageSharp.Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(
				data: bytes,
				width: width,
				height: bytes.Length / width >> 2);
		}
		public static SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> Png(this ISprite sprite) => SixLabors.ImageSharp.Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(
			data: sprite.Texture,
			width: sprite.Width,
			height: sprite.Height);
		public static SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> AnimatedGif(int frameDelay = DefaultFrameDelay, ushort repeatCount = 0, params ISprite[] sprites) => sprites.AsEnumerable().AnimatedGif(frameDelay, repeatCount);
		public static SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> AnimatedGif(this IEnumerable<ISprite> sprites, int frameDelay = DefaultFrameDelay, ushort repeatCount = 0)
		{
			Sprite[] resized = Voxel2Pixel.ExtensionMethods.SameSize(sprites).ToArray();
			SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> gif = new(resized[0].Width, resized[0].Height);
			SixLabors.ImageSharp.Formats.Gif.GifMetadata gifMetaData = gif.Metadata.GetGifMetadata();
			gifMetaData.RepeatCount = repeatCount;
			gifMetaData.ColorTableMode = SixLabors.ImageSharp.Formats.Gif.GifColorTableMode.Local;
			foreach (Sprite sprite in resized)
			{
				SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> image = sprite.Png();
				SixLabors.ImageSharp.Formats.Gif.GifFrameMetadata metadata = image.Frames.RootFrame.Metadata.GetGifMetadata();
				metadata.FrameDelay = frameDelay;
				metadata.DisposalMethod = SixLabors.ImageSharp.Formats.Gif.GifDisposalMethod.RestoreToBackground;
				gif.Frames.AddFrame(image.Frames.RootFrame);
			}
			gif.Frames.RemoveFrame(0);//I don't know why ImageSharp has me doing this but if I don't then I get an extra transparent frame at the start.
			return gif;
		}
		public static SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> AnimatedGif(ushort scaleX, ushort scaleY, ushort width = 0, int frameDelay = DefaultFrameDelay, ushort repeatCount = 0, params byte[][] frames) => AnimatedGif(
			width: (ushort)(width * scaleX),
			frameDelay: frameDelay,
			repeatCount: repeatCount,
			frames: scaleX == 1 && scaleY == 1 ? frames
				: frames
					.Select(f => Voxel2Pixel.Draw.PixelDraw.Upscale(f, scaleX, scaleY, width))
					.ToArray());
		public static SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> AnimatedGif(ushort width = 0, int frameDelay = DefaultFrameDelay, ushort repeatCount = 0, params byte[][] frames)
		{
			if (width < 1)
				width = (ushort)Math.Sqrt(frames[0].Length >> 2);
			int height = frames[0].Length / width >> 2;
			SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> gif = new(width, height);
			SixLabors.ImageSharp.Formats.Gif.GifMetadata gifMetaData = gif.Metadata.GetGifMetadata();
			gifMetaData.RepeatCount = repeatCount;
			gifMetaData.ColorTableMode = SixLabors.ImageSharp.Formats.Gif.GifColorTableMode.Local;
			foreach (byte[] frame in frames)
			{
				SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> image = SixLabors.ImageSharp.Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(
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
		#endregion ImageSharp
		#region SkiaSharp
		public static Stream PngStream(this ISprite sprite) => sprite.Texture.PngStream(sprite.Width);
		public static Stream PngStream(this byte[] pixels, ushort width = 0) => SKImage.FromPixelCopy(
				info: new SKImageInfo(
					width: width,
					height: (pixels.Length >> 2) / width,
					colorType: SKColorType.Rgba8888),
				pixels: pixels,
				rowBytes: width << 2)
			.Encode()
			.AsStream();
		public static string PngBase64String(this ISprite sprite) => sprite.Texture.PngBase64String(sprite.Width);
		/// <summary>
		/// Based on https://github.com/SixLabors/ImageSharp/blob/ede2f2d2d1e567dea74a44a482099302af9ed14d/src/ImageSharp/ImageExtensions.cs#L173-L183
		/// </summary>
		public static string PngBase64String(this byte[] pixels, ushort width = 0)
		{
			using MemoryStream memoryStream = new();
			PngStream(pixels, width).CopyTo(memoryStream);
			memoryStream.TryGetBuffer(out ArraySegment<byte> buffer);
			return "data:image/png;base64," + Convert.ToBase64String(buffer.Array ?? [], 0, (int)memoryStream.Length);
		}
		#endregion SkiaSharp
	}
}
