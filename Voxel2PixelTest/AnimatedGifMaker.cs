using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System;
using SixLabors.ImageSharp.Formats.Gif;

namespace Voxel2PixelTest
{
	public static class AnimatedGifMaker
	{
		public static Image<Rgba32> Gif(params byte[][] frames) => Gif(
			width: 0,
			frames: frames);
		public static Image<Rgba32> Gif(int width, params byte[][] frames) => Gif(
			width: width,
			frameDelay: 25,
			frames: frames);
		public static Image<Rgba32> Gif(int width, int frameDelay, params byte[][] frames) => Gif(
			width: width,
			frameDelay: frameDelay,
			repeatCount: 0,
			frames: frames);
		public static Image<Rgba32> Gif(int width, int frameDelay, ushort repeatCount, params byte[][] frames)
		{
			if (width < 1)
				width = (int)Math.Sqrt(frames[0].Length >> 2);
			int height = frames[0].Length / width >> 2;
			Image<Rgba32> gif = new Image<Rgba32>(width, height);
			GifMetadata gifMetaData = gif.Metadata.GetGifMetadata();
			gifMetaData.RepeatCount = repeatCount;
			gifMetaData.ColorTableMode = GifColorTableMode.Local;
			foreach (byte[] frame in frames)
			{
				Image<Rgba32> image = Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(
					data: frame,
					width: width,
					height: height);
				GifFrameMetadata metadata = image.Frames.RootFrame.Metadata.GetGifMetadata();
				metadata.FrameDelay = frameDelay;
				metadata.DisposalMethod = GifDisposalMethod.RestoreToBackground;
				gif.Frames.AddFrame(image.Frames.RootFrame);
			}
			return gif;
		}
	}
}
