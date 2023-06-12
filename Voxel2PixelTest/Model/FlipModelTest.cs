using SixLabors.ImageSharp;
using System.Collections.Generic;
using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Model;
using Voxel2Pixel.Render;
using Xunit;

namespace Voxel2PixelTest.Model
{
	public class FlipModelTest
	{
		[Fact]
		public void ArrayRendererTest()
		{
			VoxModel voxModel = new VoxModel(@"..\..\..\Sora.vox");
			IVoxelColor voxelColor = new NaiveDimmer(voxModel.Palette);
			FlipModel model = new FlipModel
			{
				Model = voxModel,
			};
			int width = VoxelDraw.IsoWidth(model),
				height = VoxelDraw.IsoHeight(model);
			List<byte[]> frames = new List<byte[]>();
			void addFrame()
			{
				ArrayRenderer arrayRenderer = new ArrayRenderer
				{
					Image = new byte[width * 4 * height],
					Width = width,
					VoxelColor = voxelColor,
				};
				VoxelDraw.Iso(model, arrayRenderer);
				frames.Add(arrayRenderer.Image);
			}
			addFrame();
			model.Set(true, false, false);
			addFrame();
			model.Set(false, true, false);
			addFrame();
			model.Set(true, true, false);
			addFrame();
			model.Set(false, false, true);
			addFrame();
			model.Set(true, false, true);
			addFrame();
			model.Set(false, true, true);
			addFrame();
			model.Set(true, true, true);
			addFrame();
			ImageMaker.AnimatedGif(
				scaleX: 16,
				scaleY: 16,
				width: width,
				frames: frames.ToArray(),
				frameDelay: 25)
			.SaveAsGif("FlipModelTest.gif");
		}
	}
}
