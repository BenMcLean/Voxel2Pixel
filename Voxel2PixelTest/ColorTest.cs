using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Text;
using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Model;
using Voxel2Pixel.Render;
using Xunit;

namespace Voxel2PixelTest
{
	public class ColorTest
	{
		[Fact]
		public void SideTest()
		{
			VoxModel model = new VoxModel(@"..\..\..\Sora.vox");
			int width = VoxelDraw.DiagonalWidth(model),
				height = VoxelDraw.DiagonalHeight(model);
			ArrayRenderer arrayRenderer = new ArrayRenderer
			{
				Image = new byte[width * 4 * height],
				Width = width,
				VoxelColor = new NaiveDimmer(model.Palette),
			};
			VoxelDraw.Diagonal(model, arrayRenderer);
			ImageMaker.Png(
				scaleX: 32,
				scaleY: 32,
				width: width,
				bytes: arrayRenderer.Image)
				.SaveAsPng("ColorTest.png");
		}
	}
}
