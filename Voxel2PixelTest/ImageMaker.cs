﻿using SixLabors.ImageSharp;
using System;
using System.Linq;
using static Voxel2Pixel.Draw.TextureMethods;

namespace Voxel2PixelTest
{
	public static class ImageMaker
	{
		public static SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> Png(int width = 0, params byte[] bytes)
		{
			if (width < 1)
				width = (int)Math.Sqrt(bytes.Length >> 2);
			return SixLabors.ImageSharp.Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(
				data: bytes,
				width: width,
				height: bytes.Length / width >> 2);
		}
		public static SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> Png(int scaleX, int scaleY, int width = 0, params byte[] bytes)
		{
			if (width < 1)
				width = (int)Math.Sqrt(bytes.Length >> 2);
			return Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(
				data: bytes.Upscale(scaleX, scaleY, width),
				width: width * scaleX,
				height: (bytes.Length / width >> 2) * scaleY);
		}
		public static SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> AnimatedGifScaled(int scaleX, int scaleY, int width = 0, int frameDelay = 25, ushort repeatCount = 0, params byte[][] frames) => AnimatedGif(
			width: width * scaleX,
			frameDelay: frameDelay,
			repeatCount: repeatCount,
			frames: frames
				.Select(f => f.Upscale(scaleX, scaleY, width))
				.ToArray());
		public static SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> AnimatedGif(int width = 0, int frameDelay = 25, ushort repeatCount = 0, params byte[][] frames)
		{
			if (width < 1)
				width = (int)Math.Sqrt(frames[0].Length >> 2);
			int height = frames[0].Length / width >> 2;
			SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> gif = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(width, height);
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
			return gif;
		}
	}
}