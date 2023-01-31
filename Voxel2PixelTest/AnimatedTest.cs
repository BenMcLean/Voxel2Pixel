using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Text;
using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Model;
using Voxel2Pixel.Render;
using VoxReader;
using Xunit;

namespace Voxel2PixelTest
{
	public class AnimatedTest
	{
		public const int FrameDelay = 200;
		[Fact]
		public void GifTest()
		{
			ArrayModel model = new ArrayModel(
				sizeX: 9,
				sizeY: 6,
				sizeZ: 9);
			int xScale = 32,
				yScale = 32,
				width = VoxelDraw.AboveWidth(model),
				height = VoxelDraw.AboveHeight(model);
			Image<Rgba32> gif = new Image<Rgba32>(width * xScale, height * xScale);
			GifMetadata gifMetaData = gif.Metadata.GetGifMetadata();
			gifMetaData.RepeatCount = 0;
			GifFrameMetadata metadata = gif.Frames.RootFrame.Metadata.GetGifMetadata();
			metadata.FrameDelay = FrameDelay;
			for (int x = 0; x < model.SizeX; x++)
				for (int y = model.SizeY - 1; y >= 0; y--)
					for (int z = 0; z < model.SizeZ; z++)
					{
						model.Voxels[x][y][z] = 1;
						ArrayRenderer arrayRenderer = new ArrayRenderer
						{
							Image = new byte[width * 4 * height],
							Width = width,
							IVoxelColor = new NaiveDimmer(ArrayModelTest.RainbowPalette),
						};
						VoxelDraw.Above(model, arrayRenderer);
						gif.Frames.AddFrame(
							Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(
								data: arrayRenderer.Image.Upscale(xScale, yScale, arrayRenderer.Width),
								width: arrayRenderer.Width * xScale,
								height: arrayRenderer.Height * yScale)
							.Frames.RootFrame);
						model.Voxels[x][y][z] = 0;
					}
			gif.SaveAsGif("output.gif");
		}
	}
}
