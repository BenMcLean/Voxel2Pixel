using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Model;
using Voxel2Pixel.Render;
using VoxReader;
using Xunit;
using static Voxel2PixelTest.CubeRotationTest;

namespace Voxel2PixelTest
{
	public class TurnModelTest
	{
		[Fact]
		public void ArrayRendererTest()
		{
			//ArrayModel sourceModel = new ArrayModel(ArrayModelTest.RainbowBox(5, 6, 7));
			//IVoxelColor voxelColor = new NaiveDimmer(ArrayModelTest.RainbowPalette);
			VoxModel sourceModel = new VoxModel(@"..\..\..\Sora.vox");
			sourceModel.Voxels.Box(1);
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
			CubeRotation[] rotations = new CubeRotation[]
			{
				CubeRotation.WEST1,
				CubeRotation.WEST3,
				CubeRotation.EAST1,
				CubeRotation.EAST3,
				CubeRotation.UP1,
				CubeRotation.UP3,
				CubeRotation.DOWN1,
				CubeRotation.DOWN3,
			};
			foreach (CubeRotation cubeRotation in rotations)
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
		public static void TestTest()
		{
			byte[][][] bytes = ArrayModelTest.RainbowBox(4, 5, 6);
			Assert.NotNull(bytes);
			ArrayModel model = new ArrayModel(bytes);
			Assert.NotNull(model.Voxels);
			Assert.NotNull(new ArrayModel(model).Voxels);
			TurnModel turnModel = new TurnModel
			{
				Model = new ArrayModel(model),
				CubeRotation = CubeRotation.SOUTH0,
			};
			Assert.NotNull(((ArrayModel)turnModel.Model).Voxels);
			ArrayModel turned = (ArrayModel)MakeTurns(new ArrayModel(model), Turn.NONE);
		}
		[Fact]
		public static void Test24()
		{
			ArrayModel model = new ArrayModel(ArrayModelTest.RainbowBox(4, 5, 6));
			foreach (KeyValuePair<string, Turn[]> orientation in Orientations)
				TestOrientation(model, orientation.Value);
		}
		public static bool IsEqual(IModel a, IModel b)
		{
			if (!a.SizeX.Equals(b.SizeX)
				|| !a.SizeY.Equals(b.SizeY)
				|| !a.SizeZ.Equals(b.SizeZ))
				return false;
			for (int x = 0; x < a.SizeX; x++)
				for (int y = 0; y < a.SizeY; y++)
					for (int z = 0; z < a.SizeZ; z++)
						if (a.At(x, y, z) != b.At(x, y, z))
							return false;
			return true;
		}
		private static void TestOrientation(ArrayModel model, params Turn[] turns) =>
			Assert.True(IsEqual(
				new TurnModel
				{
					Model = new ArrayModel(model),
					CubeRotation = Cube(turns),
				},
				(ArrayModel)MakeTurns(new ArrayModel(model), turns)));
	}
}
