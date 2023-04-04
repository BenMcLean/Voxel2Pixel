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
			//ArrayModel sourceModel = new ArrayModel(ArrayModelTest.RainbowBox(5, 6, 7));
			//IVoxelColor voxelColor = new NaiveDimmer(ArrayModelTest.RainbowPalette);
			VoxModel sourceModel = (VoxModel)new VoxModel(@"..\..\..\Sora.vox").DrawBox(1);
			IVoxelColor voxelColor = new NaiveDimmer(sourceModel.Palette);
			//int testTextureWidth = 10, testTextureHeight = 32;
			//byte[] testTexture = TextureModelTest.TestTexture(testTextureWidth, testTextureHeight);
			//TextureModel sourceModel = new TextureModel(testTexture, testTextureWidth)
			//{
			//	SizeZ = 1,
			//};
			//IVoxelColor voxelColor = new NaiveDimmer(sourceModel.Palette);
			TurnModel turnModel = new TurnModel
			{
				Model = sourceModel,
			};
			BoxModel model = new BoxModel
			{
				Model = turnModel,
				Voxel = 4,
				Overwrite = false,
			};
			int width = Math.Max(VoxelDraw.IsoWidth(model), VoxelDraw.IsoHeight(model)),
				height = width + 4;
			List<byte[]> frames = new List<byte[]>();
			foreach (CubeRotation cubeRotation in CubeRotation.Values)
			{
				turnModel.CubeRotation = cubeRotation;
				ArrayRenderer arrayRenderer = new ArrayRenderer
				{
					Image = new byte[width * 4 * height],
					Width = width,
					VoxelColor = voxelColor,
				};
				VoxelDraw.Iso(model, arrayRenderer);
				arrayRenderer.Image.Draw3x4(
					@string: cubeRotation.Name + ": " + string.Join(", ", model.SizeX, model.SizeY, model.SizeZ),
					width: width,
					x: 0,
					y: height - 4);
				frames.Add(arrayRenderer.Image);
			}
			ImageMaker.AnimatedGif(
				scaleX: 16,
				scaleY: 16,
				width: width,
				frames: frames.ToArray(),
				frameDelay: 150)
			.SaveAsGif("TurnModelTest.gif");
		}
		[Fact]
		public void OffsetModelTest()
		{
			VoxModel sourceModel = (VoxModel)new VoxModel(@"..\..\..\Sora.vox").DrawBox(1);
			IVoxelColor voxelColor = new NaiveDimmer(sourceModel.Palette);
			TurnModel turnModel = new TurnModel
			{
				Model = sourceModel,
				CubeRotation = CubeRotation.WEST1,
			};
			OffsetModel offsetModel = new OffsetModel
			{
				Model = turnModel,
			};
			BoxModel model = new BoxModel
			{
				Model = offsetModel,
				Voxel = 4,
			};
			int width = Math.Max(VoxelDraw.IsoWidth(model), VoxelDraw.IsoHeight(model)),
				height = width;
			List<byte[]> frames = new List<byte[]>();
			for (offsetModel.OffsetY = 0; offsetModel.OffsetY > -10; offsetModel.OffsetY--)
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
			ImageMaker.AnimatedGif(
				scaleX: 16,
				scaleY: 16,
				width: width,
				frames: frames.ToArray(),
				frameDelay: 50)
			.SaveAsGif("OffsetModelTest.gif");
		}
	}
}
