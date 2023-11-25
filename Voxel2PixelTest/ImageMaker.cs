﻿using SixLabors.ImageSharp;
using System;
using System.Linq;
using static Voxel2Pixel.Draw.PixelDraw;
using static Voxel2Pixel.Draw.DrawFont3x4;
using System.Collections.ObjectModel;
using Voxel2Pixel.Model;
using System.Collections;
using Voxel2Pixel.Pack;
using System.Collections.Generic;

namespace Voxel2PixelTest
{
	public static class ImageMaker
	{
		public static byte[][] AddFrameNumbers(this byte[][] frames, ushort width = 0, uint color = 0xFFFFFFFF)
		{
			for (int frame = 0; frame < frames.Length; frame++)
				frames[frame].Draw3x4(
					@string: frame.ToString(),
					width: width,
					x: 0,
					y: Height(frames[frame].Length, width) - 4,
					color: color);
			return frames;
		}
		public static SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> Png(int width = 0, params byte[] bytes)
		{
			if (width < 1)
				width = (int)Math.Sqrt(bytes.Length >> 2);
			return SixLabors.ImageSharp.Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(
				data: bytes,
				width: width,
				height: bytes.Length / width >> 2);
		}
		public static SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> Png(ushort scaleX, ushort scaleY, ushort width = 0, params byte[] bytes)
		{
			if (scaleX == 1 && scaleY == 1)
				return Png(width, bytes);
			if (width < 1)
				width = (ushort)Math.Sqrt(bytes.Length >> 2);
			return Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(
				data: bytes.Upscale(scaleX, scaleY, width),
				width: width * scaleX,
				height: (bytes.Length / width >> 2) * scaleY);
		}
		public static SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> AnimatedGif(int frameDelay = 25, ushort repeatCount = 0, params Sprite[] sprites) => AnimatedGif(sprites.AsEnumerable(), frameDelay, repeatCount);
		public static SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> AnimatedGif(IEnumerable<Sprite> sprites, int frameDelay = 25, ushort repeatCount = 0)
		{
			Sprite[] resized = Sprite.SameSize(sprites).ToArray();
			Sprite first = resized.First();
			SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> gif = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(first.Width, first.Height);
			SixLabors.ImageSharp.Formats.Gif.GifMetadata gifMetaData = gif.Metadata.GetGifMetadata();
			gifMetaData.RepeatCount = repeatCount;
			gifMetaData.ColorTableMode = SixLabors.ImageSharp.Formats.Gif.GifColorTableMode.Local;
			foreach (Sprite sprite in resized)
			{
				SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> image = SixLabors.ImageSharp.Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(
					data: sprite.Texture,
					width: sprite.Width,
					height: sprite.Height);
				SixLabors.ImageSharp.Formats.Gif.GifFrameMetadata metadata = image.Frames.RootFrame.Metadata.GetGifMetadata();
				metadata.FrameDelay = frameDelay;
				metadata.DisposalMethod = SixLabors.ImageSharp.Formats.Gif.GifDisposalMethod.RestoreToBackground;
				gif.Frames.AddFrame(image.Frames.RootFrame);
			}
			gif.Frames.RemoveFrame(0);//I don't know why ImageSharp has me doing this but if I don't then I get an extra transparent frame at the start.
			return gif;
		}
		public static SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> AnimatedGif(ushort scaleX, ushort scaleY, ushort width = 0, int frameDelay = 25, ushort repeatCount = 0, params byte[][] frames) => AnimatedGif(
		width: (ushort)(width * scaleX),
		frameDelay: frameDelay,
		repeatCount: repeatCount,
		frames: scaleX == 1 && scaleY == 1 ? frames
			: frames
				.Select(f => f.Upscale(scaleX, scaleY, width))
				.ToArray());
		public static SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> AnimatedGif(ushort width = 0, int frameDelay = 25, ushort repeatCount = 0, params byte[][] frames)
		{
			if (width < 1)
				width = (ushort)Math.Sqrt(frames[0].Length >> 2);
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
			gif.Frames.RemoveFrame(0);//I don't know why ImageSharp has me doing this but if I don't then I get an extra transparent frame at the start.
			return gif;
		}
		#region Test data
		public static readonly ReadOnlyCollection<uint> Rainbow = Array.AsReadOnly(new uint[7]
		{//Just a color test, not a political statement.
			0xFF0000FFu,//Red
			0xFFA500FFu,//Orange
			0xFFFF00FFu,//Yellow
			0x00FF00FFu,//Green
			0x0000FFFFu,//Blue
			0x4B0082FFu,//Indigo
			0x8F00FFFFu,//Violet
		});
		public static readonly uint[] RainbowPalette =
			Enumerable.Range(0, byte.MaxValue)
			.Select(i => i == 0 ? 0 : Rainbow[(i - 1) % Rainbow.Count])
			.ToArray();
		public static byte[][][] RainbowBox(int sizeX, int sizeY, int sizeZ)
		{
			byte[][][] model = Array3D.Initialize<byte>(sizeX, sizeY, sizeZ);
			for (int x = 0; x < sizeX; x++)
			{
				byte voxel = (byte)((sizeX - 1 - x) % Rainbow.Count + 1);
				model[x][0][0] = voxel;
				model[x][sizeY - 1][0] = voxel;
				model[x][0][sizeZ - 1] = voxel;
				model[x][sizeY - 1][sizeZ - 1] = voxel;
			}
			for (int y = 1; y < sizeY - 1; y++)
			{
				byte voxel = (byte)((sizeY - 1 - y) % Rainbow.Count + 1);
				model[0][y][0] = voxel;
				model[sizeX - 1][y][0] = voxel;
				model[0][y][sizeZ - 1] = voxel;
				model[sizeX - 1][y][sizeZ - 1] = voxel;
			}
			for (int z = 1; z < sizeZ - 1; z++)
			{
				byte voxel = (byte)((sizeZ - 1 - z) % Rainbow.Count + 1);
				model[0][0][z] = voxel;
				model[sizeX - 1][0][z] = voxel;
				model[0][sizeY - 1][z] = voxel;
				model[sizeX - 1][sizeY - 1][z] = voxel;
			}
			return model;
		}
		public static byte[][][] AltRainbowBox(int sizeX, int sizeY, int sizeZ)
		{
			byte[][][] model = Array3D.Initialize<byte>(sizeX, sizeY, sizeZ);
			model[0][3][0] = 1;
			model[3][0][0] = 1;
			for (int x = 1; x < sizeX; x++)
			{
				byte voxel = (byte)((sizeX - 1 - x) % Rainbow.Count + 1);
				model[x][1][0] = voxel;
				model[x][sizeY - 1][0] = voxel;
				model[x][1][sizeZ - 1] = voxel;
				model[x][sizeY - 1][sizeZ - 1] = voxel;
			}
			for (int y = 1; y < sizeY - 1; y++)
			{
				byte voxel = (byte)((sizeY - 1 - y) % Rainbow.Count + 1);
				model[1][y][0] = voxel;
				model[sizeX - 1][y][0] = voxel;
				model[1][y][sizeZ - 1] = voxel;
				model[sizeX - 1][y][sizeZ - 1] = voxel;
			}
			for (int z = 1; z < sizeZ - 1; z++)
			{
				byte voxel = (byte)((sizeZ - 1 - z) % Rainbow.Count + 1);
				model[1][1][z] = voxel;
				model[sizeX - 1][1][z] = voxel;
				model[1][sizeY - 1][z] = voxel;
				model[sizeX - 1][sizeY - 1][z] = voxel;
			}
			return model;
		}
		public static byte[][][] SmallerRainbowBox(int sizeX, int sizeY, int sizeZ)
		{
			byte[][][] model = Array3D.Initialize<byte>(sizeX, sizeY, sizeZ);
			for (int x = 1; x < sizeX - 1; x++)
			{
				byte voxel = (byte)(x % Rainbow.Count);
				model[x][1][1] = voxel;
				model[x][sizeY - 2][1] = voxel;
				model[x][1][sizeZ - 2] = voxel;
				model[x][sizeY - 2][sizeZ - 2] = voxel;
			}
			for (int y = 1; y < sizeY - 2; y++)
			{
				byte voxel = (byte)(y % Rainbow.Count);
				model[1][y][1] = voxel;
				model[sizeX - 2][y][1] = voxel;
				model[1][y][sizeZ - 2] = voxel;
				model[sizeX - 2][y][sizeZ - 2] = voxel;
			}
			for (int z = 1; z < sizeZ - 2; z++)
			{
				byte voxel = (byte)(z % Rainbow.Count);
				model[1][1][z] = voxel;
				model[sizeX - 2][1][z] = voxel;
				model[1][sizeY - 2][z] = voxel;
				model[sizeX - 2][sizeY - 2][z] = voxel;
			}
			return model;
		}
		public static byte[] TestTexture(ushort width, ushort height) =>
			new byte[16] {
				255,0,0,255,
				0,255,0,255,
				0,0,255,255,
				128,128,128,255}
			.Upscale(
				factorX: (ushort)(width >> 1),
				factorY: (ushort)(height >> 1),
				width: 2)
			.DrawRectangle(
				color: 0xFFu,
				x: width >> 2,
				y: height >> 2,
				rectWidth: width >> 1,
				rectHeight: height >> 1,
				width: width);
		#endregion Test data
	}
}
