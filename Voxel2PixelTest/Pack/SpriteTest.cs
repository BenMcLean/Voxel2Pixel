﻿using SixLabors.ImageSharp;
using Voxel2Pixel.Color;
using Voxel2Pixel.Model;
using Voxel2Pixel.Pack;
using static Voxel2Pixel.ExtensionMethods;
using Xunit;
using System.Linq;

namespace Voxel2PixelTest.Pack
{
	public class SpriteTest
	{
		[Fact]
		public void SoraGif()
		{
			VoxFileModel model = new VoxFileModel(@"..\..\..\Sora.vox");
			Sprite.Above4(
					model: model,
					voxelColor: new NaiveDimmer(model.Palette),
					(ushort)(model.SizeX >> 1), (ushort)(model.SizeY >> 1), 0)
				.SameSize()
				.AddFrameNumbers()
				.Select(sprite => sprite.Upscale(8, 8))
				.AnimatedGif(frameDelay: 50)
				.SaveAsGif("Sora.gif");
		}
	}
}
