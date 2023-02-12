using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Model;
using Voxel2Pixel.Render;
using Xunit;

namespace Voxel2PixelTest
{
	public class TurnModelTest
	{
		[Fact]
		public void ArrayRendererTest()
		{
			VoxModel voxModel = new VoxModel(@"..\..\..\Sora.vox");
			IVoxelColor voxelColor = new NaiveDimmer(voxModel.Palette);
			TurnModel model = new TurnModel
			{
				Model = voxModel,
			};
			int width = Math.Max(VoxelDraw.IsoWidth(model), VoxelDraw.IsoHeight(model)),
				height = width;
			List<byte[]> frames = new List<byte[]>();
			foreach (CubeRotation cubeRotation in CubeRotation.Values)
			{
				model.CubeRotation = cubeRotation;
				ArrayRenderer arrayRenderer = new ArrayRenderer
				{
					Image = new byte[width * 4 * height],
					Width = width,
					IVoxelColor = voxelColor,
				};
				VoxelDraw.Iso(model, arrayRenderer);
				frames.Add(arrayRenderer.Image);
			}
			ImageMaker.AnimatedGifScaled(
				scaleX: 16,
				scaleY: 16,
				width: width,
				frames: frames.ToArray(),
				frameDelay: 100)
			.SaveAsGif("TurnModelTest.gif");
		}
	}
}
