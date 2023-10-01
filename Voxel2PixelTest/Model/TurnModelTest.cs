using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Model;
using Voxel2Pixel.Render;
using Xunit;

namespace Voxel2PixelTest.Model
{
	public class TurnModelTest
	{
		[Fact]
		public void ReverseRotateTest()
		{
			TurnModel turnModel = new TurnModel()
			{
				Model = new MarkerModel
				{
					Model = new EmptyModel
					{
						SizeX = 3,
						SizeY = 3,
						SizeZ = 3,
					},
					Voxel = 1,
					X = 0,
					Y = 0,
					Z = 0,
				},
			};
			foreach (CuboidOrientation cuboidOrientation in CuboidOrientation.Values)
			{
				turnModel.CuboidOrientation = cuboidOrientation;
				for (int x = 0; x < turnModel.Model.SizeX; x++)
					for (int y = 0; y < turnModel.Model.SizeY; y++)
						for (int z = 0; z < turnModel.Model.SizeZ; z++)
						{
							turnModel.Rotate(out int x1, out int y1, out int z1, x, y, z);
							Assert.True(x1 >= 0);
							Assert.True(y1 >= 0);
							Assert.True(z1 >= 0);
							Assert.True(x1 < turnModel.SizeX);
							Assert.True(y1 < turnModel.SizeY);
							Assert.True(z1 < turnModel.SizeZ);
							turnModel.ReverseRotate(out int x2, out int y2, out int z2, x, y, z);
							Assert.True(x2 >= 0);
							Assert.True(y2 >= 0);
							Assert.True(z2 >= 0);
							turnModel.ReverseRotate(out int x3, out int y3, out int z3, x1, y1, z1);
							Assert.Equal(x, x3);
							Assert.Equal(y, y3);
							Assert.Equal(z, z3);
							turnModel.Rotate(out int x4, out int y4, out int z4, x2, y2, z2);
							Assert.Equal(x, x4);
							Assert.Equal(y, y4);
							Assert.Equal(z, z4);
						}
			}
		}
		[Fact]
		public void ArrayRendererTest()
		{
			//ArrayModel sourceModel = new ArrayModel(ArrayModelTest.RainbowBox(5, 6, 7));
			//IVoxelColor voxelColor = new NaiveDimmer(ArrayModelTest.RainbowPalette);
			VoxModel sourceModel = new VoxModel(@"..\..\..\Sora.vox");
			sourceModel.Voxels.Box<byte>(1);
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
			foreach (CuboidOrientation cuboidOrientation in CuboidOrientation.Values)
			{
				turnModel.CuboidOrientation = cuboidOrientation;
				ArrayRenderer arrayRenderer = new ArrayRenderer
				{
					Image = new byte[width * 4 * height],
					Width = width,
					VoxelColor = voxelColor,
				};
				VoxelDraw.Iso(model, arrayRenderer);
				arrayRenderer.Image.Draw3x4(
					@string: cuboidOrientation.Value + " " + cuboidOrientation.Name + ": " + string.Join(", ", model.SizeX, model.SizeY, model.SizeZ),
					width: width,
					x: 0,
					y: height - 4);
				frames.Add(arrayRenderer.Image);
			}
			Directory.CreateDirectory(@".\NumberCube");
			for (int frame = 0; frame < frames.Count; frame++)
				ImageMaker.Png(
					scaleX: 16,
					scaleY: 16,
					width: width,
					bytes: frames[frame])
				.SaveAsPng(@".\NumberCube\NumberCube" + frame.ToString("00") + ".png");
			ImageMaker.AnimatedGif(
				scaleX: 16,
				scaleY: 16,
				width: width,
				frames: frames.ToArray(),
				frameDelay: 150)
			.SaveAsGif("TurnModelTest.gif");
		}
	}
}
