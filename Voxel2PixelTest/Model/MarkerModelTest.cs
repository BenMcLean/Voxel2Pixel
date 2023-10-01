using SixLabors.ImageSharp;
using System.Collections.Generic;
using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Model;
using Voxel2Pixel.Render;
using Xunit;

namespace Voxel2PixelTest.Model
{
	public class MarkerModelTest
	{
		[Fact]
		public void Above()
		{
			MarkerModel model = new MarkerModel
			{
				Model = new EmptyModel
				{
					SizeX = 25,
					SizeY = 10,
					SizeZ = 10,
				},
				Voxel = 1,
			};
			int width = VoxelDraw.AboveWidth(model),
				height = VoxelDraw.AboveHeight(model);
			IVoxelColor voxelColor = new NaiveDimmer(ArrayModelTest.RainbowPalette);
			List<byte[]> frames = new List<byte[]>();
			for (int y = 0; y < model.SizeY; y++)
				for (int z = 0; z < model.SizeZ; z++)
					for (int x = 0; x < model.SizeX; x++)
					{
						model.X = x;
						model.Y = y;
						model.Z = z;
						ArrayRenderer arrayRenderer = new ArrayRenderer
						{
							Image = new byte[width * 4 * height],
							Width = width,
							VoxelColor = voxelColor,
						};
						VoxelDraw.Above(
							model: model,
							renderer: arrayRenderer);
						VoxelDraw.AboveLocate(out int pixelX, out int pixelY, model, x, y, z);
						frames.Add(arrayRenderer.Image
							.Draw3x4(
								@string: string.Join(",", x, y, z),
								x: 0,
								y: 0,
								width: width)
							.DrawPixel(
								color: 0x0000FFFF,
								x: pixelX,
								y: pixelY,
								width: width));
					}
			ImageMaker.AnimatedGif(
				scaleX: 16,
				scaleY: 16,
				width: width,
				frames: frames.ToArray(),
				frameDelay: 1)
			.SaveAsGif("MarkerModelAboveTest.gif");
		}
	}
}
