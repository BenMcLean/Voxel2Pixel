﻿using SixLabors.ImageSharp;
using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Model;
using Voxel2Pixel.Render;
using Xunit;
using static Voxel2Pixel.Draw.PixelDraw;

namespace Voxel2PixelTest.Model
{
	public class TextureModelTest
	{
		[Fact]
		public void ArrayRendererTest()
		{
			ushort testTextureWidth = 10, testTextureHeight = 32;
			byte[] testTexture = TestTexture(testTextureWidth, testTextureHeight);
			ImageMaker.Png(
				width: testTextureWidth,
				bytes: testTexture)
				.SaveAsPng("TextureModelTestTexture.png");
			TextureModel model = new TextureModel(testTexture, testTextureWidth)
			{
				SizeZ = 5,
			};
			ushort width = VoxelDraw.IsoWidth(model),
				height = VoxelDraw.IsoHeight(model);
			ArrayRenderer arrayRenderer = new ArrayRenderer
			{
				Image = new byte[width * 4 * height],
				Width = width,
				VoxelColor = new NaiveDimmer(model.Palette),
			};
			VoxelDraw.Iso(model, arrayRenderer);
			ImageMaker.Png(
				scaleX: 1,
				scaleY: 1,
				width: width,
				bytes: arrayRenderer.Image)
				.SaveAsPng("TextureModel.png");
		}
		public static byte[] TestTexture(ushort width, ushort height) =>
			new byte[16] {
				255,0,0,255,
				0,255,0,255,
				0,0,255,255,
				128,128,128,255}
			.Upscale((ushort)(width / 2), (ushort)(height / 2), 2)
			.DrawRectangle(0xFFu, width / 4, height / 4, width / 2, height / 2, width);
	}
}
