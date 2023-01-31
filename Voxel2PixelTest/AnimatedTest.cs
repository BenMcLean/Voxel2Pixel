using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;
using System;
using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Model;
using Voxel2Pixel.Render;
using Xunit;

namespace Voxel2PixelTest
{
	public class AnimatedTest
	{
		public const int FrameDelay = 25;
		//[Fact]
		public void GifTest()
		{
			ArrayModel model = new ArrayModel(
				sizeX: 7,
				sizeY: 4,
				sizeZ: 7);
			int xScale = 32,
				yScale = 32,
				width = VoxelDraw.AboveWidth(model),
				height = VoxelDraw.AboveHeight(model);
			Image<Rgba32> gif = new Image<Rgba32>(width * xScale, height * xScale);
			GifMetadata gifMetaData = gif.Metadata.GetGifMetadata();
			gifMetaData.RepeatCount = 0;
			gifMetaData.ColorTableMode = GifColorTableMode.Local;
			Random random = new System.Random();
			for (int x = 0; x < model.SizeX; x++)
				for (int y = model.SizeY - 1; y >= 0; y--)
					for (int z = 0; z < model.SizeZ; z++)
					{
						model.Voxels[x][y][z] = 1;
						int randomX = random.Next(0, model.SizeX),
							randomY = random.Next(0, model.SizeY),
							randomZ = random.Next(0, model.SizeZ);
						model.Voxels[randomX][randomY][randomZ] = (byte)(random.Next(0, ArrayModelTest.Rainbow.Count) + 1);
						ArrayRenderer arrayRenderer = new ArrayRenderer
						{
							Image = new byte[width * 4 * height],
							Width = width,
							IVoxelColor = new NaiveDimmer(ArrayModelTest.RainbowPalette),
						};
						VoxelDraw.Above(model, arrayRenderer);
						Image<Rgba32> frame = Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(
								data: arrayRenderer.Image.Upscale(xScale, yScale, arrayRenderer.Width),
								width: arrayRenderer.Width * xScale,
								height: arrayRenderer.Height * yScale);
						GifFrameMetadata metadata = frame.Frames.RootFrame.Metadata.GetGifMetadata();
						metadata.FrameDelay = FrameDelay;
						metadata.DisposalMethod = GifDisposalMethod.RestoreToBackground;
						gif.Frames.AddFrame(frame.Frames.RootFrame);
						model.Voxels[randomX][randomY][randomZ] = 0;
						model.Voxels[x][y][z] = 2;
					}
			gif.SaveAsGif("output.gif");
		}
	}
}
