using SixLabors.ImageSharp;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Render;
using static Voxel2Pixel.Draw.PixelDraw;

namespace Voxel2Pixel.Web;

/// <summary>
/// ImageMaker glues Voxel2Pixel to ImageSharp.
/// It isn't included in the main project so that the library won't be subject to the ImageSharp license and could output to anything else instead.
/// There's another ImageMaker class in the Test project which connects to SkiaSharp and ImageSharp.
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
			height: (bytes.Length >> 2) / width);
	}
	public static SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> Png(this ISprite sprite) => SixLabors.ImageSharp.Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(
		data: sprite.Texture,
		width: sprite.Width,
		height: sprite.Height);
	public static SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> AnimatedGif(int frameDelay = DefaultFrameDelay, ushort repeatCount = 0, params ISprite[] sprites) => sprites.AsEnumerable().AnimatedGif(frameDelay, repeatCount);
	public static SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> AnimatedGif(this IEnumerable<ISprite> sprites, int frameDelay = DefaultFrameDelay, ushort repeatCount = 0)
	{
		Sprite[] resized = [.. Voxel2Pixel.ExtensionMethods.SameSize(sprites)];
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
	public static SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> AnimatedGif(ushort width = 0, int frameDelay = DefaultFrameDelay, ushort repeatCount = 0, params byte[][] frames)
	{
		if (width < 1)
			width = (ushort)Math.Sqrt(frames[0].Length >> 2);
		int height = (frames[0].Length >> 2) / width;
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
	public static SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> AnimatedGif(byte scaleX = 1, byte scaleY = 1, ushort width = 0, int frameDelay = DefaultFrameDelay, ushort repeatCount = 0, params byte[][] frames) => AnimatedGif(
		width: (ushort)(width * scaleX),
		frameDelay: frameDelay,
		repeatCount: repeatCount,
		frames: scaleX == 1 && scaleY == 1 ? frames
			: [.. frames.Parallelize(frame => frame.Upscale(
				scaleX: scaleX,
				scaleY: scaleY,
				width: width))]);
	#endregion ImageSharp
}
