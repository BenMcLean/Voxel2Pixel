﻿using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Voxel2Pixel;
using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;
using Voxel2Pixel.Pack;
using Xunit;
using static Voxel2Pixel.Model.SvoModel;

namespace Voxel2PixelTest.Model
{
	public class SvoModelTest
	{
		private readonly Xunit.Abstractions.ITestOutputHelper output;
		public SvoModelTest(Xunit.Abstractions.ITestOutputHelper output) => this.output = output;
		private void CompareBinary(ushort a, ushort b) => output.WriteLine(
			Convert.ToString(a, 2)
			+ Environment.NewLine
			+ Convert.ToString(b, 2));
		[Fact]
		public void LeafTest()
		{
			byte[] bytes = Enumerable.Range(1, 8).Select(i => (byte)i).ToArray();
			Leaf leaf = new(null, 0);
			for (byte i = 0; i < bytes.Length; i++)
			{
				leaf[i] = bytes[i];
				Assert.Equal(
					expected: bytes[i],
					actual: leaf[i]);
			}
			byte[] written;
			using (MemoryStream ms = new())
			{
				leaf.Write(ms);
				written = ms.ToArray();
			}
			output.WriteLine(BitConverter.ToString(written));
		}
		[Fact]
		public void ModelTest()
		{
			VoxFileModel model = new(@"..\..\..\Sora.vox");
			SvoModel svo = new(model);
			foreach (Voxel voxel in model)
			{
				Assert.Equal(
					expected: voxel.Index,
					actual: svo[voxel.X, voxel.Y, voxel.Z]);
			}
			foreach (Voxel voxel in svo)
			{
				Assert.Equal(
					expected: model[voxel.X, voxel.Y, voxel.Z],
					actual: voxel.Index);
			}
			Assert.Equal(
				expected: model.Count(),
				actual: svo.Count());
			for (ushort x = 0; x < model.SizeX; x++)
				for (ushort y = 0; y < model.SizeY; y++)
					for (ushort z = 0; z < model.SizeZ; z++)
						Assert.Equal(
							expected: model[x, y, z],
							actual: svo[x, y, z]);
		}
		[Fact]
		public void OneVoxelTest()
		{
			SvoModel svoModel = new()
			{
				SizeX = ushort.MaxValue,
				SizeY = ushort.MaxValue,
				SizeZ = ushort.MaxValue,
			};
			Voxel voxel = new(
				X: ushort.MaxValue - 1,
				Y: ushort.MaxValue - 1,
				Z: ushort.MaxValue - 1,
				Index: 1);
			svoModel.Set(voxel);
			Voxel voxel2 = svoModel.First();
			output.WriteLine("X:");
			CompareBinary(voxel.X, voxel2.X);
			output.WriteLine("Y:");
			CompareBinary(voxel.Y, voxel2.Y);
			output.WriteLine("Z:");
			CompareBinary(voxel.Z, voxel2.Z);
			output.WriteLine("Index:");
			CompareBinary(voxel.Index, voxel2.Index);
			Assert.Equal(
				expected: voxel,
				actual: voxel2);
		}
		[Fact]
		public void TrimTest()
		{
			SvoModel svoModel = new()
			{
				SizeX = ushort.MaxValue,
				SizeY = ushort.MaxValue,
				SizeZ = ushort.MaxValue,
			};
			OutputNode(svoModel.Root);
			Assert.Equal(
				expected: 0,
				actual: svoModel[ushort.MaxValue - 1, ushort.MaxValue - 1, ushort.MaxValue - 1]);
			Assert.Equal(
				expected: 1u,
				actual: svoModel.NodeCount);
			svoModel[ushort.MaxValue - 1, ushort.MaxValue - 1, ushort.MaxValue - 1] = 1;
			OutputNode(svoModel.Root);
			Assert.Equal(
				expected: 1,
				actual: svoModel[ushort.MaxValue - 1, ushort.MaxValue - 1, ushort.MaxValue - 1]);
			Assert.Equal(
				expected: 16u,
				actual: svoModel.NodeCount);
			svoModel[ushort.MaxValue - 1, ushort.MaxValue - 1, ushort.MaxValue - 1] = 0;
			OutputNode(svoModel.Root);
			Assert.Equal(
				expected: 0,
				actual: svoModel[ushort.MaxValue - 1, ushort.MaxValue - 1, ushort.MaxValue - 1]);
			Assert.Equal(
				expected: 1u,
				actual: svoModel.NodeCount);
		}
		[Fact]
		public void WriteReadTest()
		{
			VoxFileModel model = new(@"..\..\..\Sora.vox");
			SvoModel svo = new(model),
				svo2 = new(svo.Z85(), svo.SizeX, svo.SizeY, svo.SizeZ);
			foreach (Voxel voxel in svo)
			{
				Assert.Equal(
					expected: voxel.Index,
					actual: svo2[voxel.X, voxel.Y, voxel.Z]);
			}
			foreach (Voxel voxel in svo2)
			{
				Assert.Equal(
					expected: svo[voxel.X, voxel.Y, voxel.Z],
					actual: voxel.Index);
			}
			Assert.Equal(
				expected: svo.Count(),
				actual: svo2.Count());
			for (ushort x = 0; x < model.SizeX; x++)
				for (ushort y = 0; y < model.SizeY; y++)
					for (ushort z = 0; z < model.SizeZ; z++)
						Assert.Equal(
							expected: svo[x, y, z],
							actual: svo2[x, y, z]);
		}
		private void OutputNode(Node node)
		{
			byte[] written;
			using (MemoryStream ms = new())
			{
				node.Write(ms);
				written = ms.ToArray();
			}
			output.WriteLine(Convert.ToString(written[0], 2).PadLeft(8, '0'));
			output.WriteLine(BitConverter.ToString(written));
		}
		[Fact]
		public void LeftTest()
		{
			Random rng = new();
			ushort Next()
			{
				byte[] two = new byte[2];
				rng.NextBytes(two);
				return (ushort)(two[0] << 8 | two[1]);
			}
			ushort x = Next(), z = Next();
			byte left(byte count) => (byte)(((z >> count) & 1) << 2 | (x >> count) & 1);
			Stack<byte> octants = new();
			while (octants.Count < 17)
				octants.Push(left((byte)octants.Count));
			ushort x2 = 0, z2 = 0;
			while (octants.Count > 0 && octants.Pop() is byte @byte)
			{
				x2 = (ushort)((x2 << 1) | @byte & 1);
				z2 = (ushort)((z2 << 1) | (@byte >> 2) & 1);
			}
			output.WriteLine("X:");
			CompareBinary(x, x2);
			output.WriteLine("Z:");
			CompareBinary(z, z2);
			Assert.Equal(
				expected: x,
				actual: x2);
			Assert.Equal(
				expected: z,
				actual: z2);
		}
		[Fact]
		public void FrontDrawTest()
		{
			VoxFileModel model = new(@"..\..\..\Sora.vox");
			SvoModel svo = new(model);
			Sprite sprite = new(svo.SizeX, svo.SizeZ)
			{
				VoxelColor = new NaiveDimmer(model.Palette),
			};
			svo.Front(sprite);
			sprite
				.Upscale(8, 8)
				.Png()
				.SaveAsPng("SvoModelFront.png");
		}
		[Fact]
		public void DiagonalDrawTest()
		{
			VoxFileModel model = new(@"..\..\..\Sora.vox");
			IVoxelColor voxelColor = new NaiveDimmer(model.Palette);
			SvoModel svo = new(model);
			Sprite[] sprites = new Sprite[2]
			{
				new Sprite((ushort)(svo.SizeX + svo.SizeY), svo.SizeZ)
				{
					VoxelColor = voxelColor,
				},
				new Sprite((ushort)(svo.SizeX + svo.SizeY), svo.SizeZ)
				{
					VoxelColor = voxelColor,
				}
			};
			svo.Diagonal(sprites[0]);
			VoxelDraw.Diagonal(svo, sprites[1]);
			sprites
				.AddFrameNumbers()
				.Select(sprite => sprite.Upscale(16, 16))
				.AnimatedGif(frameDelay: 100)
				.SaveAsGif("SvoModelDiagonal.gif");
		}
		[Fact]
		public void AboveDrawTest()
		{
			VoxFileModel model = new(@"..\..\..\Sora.vox");
			IVoxelColor voxelColor = new NaiveDimmer(model.Palette);
			SvoModel svo = new(model);
			Sprite[] sprites = new Sprite[2]
			{
				new(svo.SizeX, (ushort)(svo.SizeY + svo.SizeZ))
				{
					VoxelColor = voxelColor,
				},
				new(svo.SizeX, (ushort)(svo.SizeY + svo.SizeZ))
				{
					VoxelColor = voxelColor,
				}
			};
			svo.Above(sprites[0]);
			VoxelDraw.Above(svo, sprites[1]);
			sprites
				.AddFrameNumbers()
				.Select(sprite => sprite.Upscale(16, 16))
				.AnimatedGif(frameDelay: 100)
				.SaveAsGif("SvoModelAbove.gif");
		}
		[Fact]
		public void PrintStuff() =>
			output.WriteLine(new SvoModel(new VoxFileModel(@"..\..\..\NumberCube.vox"))
				.PrintStuff(1, 1, 1));
		/*
		[Fact]
		public void TurtleTest()
		{
			static int expected(ushort startY, ushort startZ, ushort newY, bool zFirst = false)
			{
				ushort y = startY;
				int z = startZ;
				while (y != newY)
					if (zFirst && y - startY > startZ - z
						|| !zFirst && y - startY >= startZ - z)
						z--;
					else
						y++;
				return z;
			}
			static int actual(ushort startY, ushort startZ, ushort newY, bool zFirst = false) => startZ - (newY - startY) + (zFirst && startY != newY ? 1 : 0);
			for (ushort startY = 10; startY < 19; startY++)
				for (ushort startZ = 10; startZ < 19; startZ++)
					for (ushort newY = startY; newY < startY + 9; newY++)
					{
						output.WriteLine(string.Join(", ",
							"startX: " + startY,
							"startY: " + startZ,
							"newY: " + newY,
							"expected newZ false: " + expected(startY, startZ, newY, false),
							"actual newZ false: " + actual(startY, startZ, newY, false),
							"expected newZ true: " + expected(startY, startZ, newY, true),
							"actual newZ true: " + actual(startY, startZ, newY, true)));
						Assert.Equal(
							expected: expected(startY, startZ, newY, false),
							actual: actual(startY, startZ, newY, false));
						Assert.Equal(
							expected: expected(startY, startZ, newY, true),
							actual: actual(startY, startZ, newY, true));
					}
		}
		*/
		[Fact]
		public void AnimatedTest()
		{
			ushort voxelWidth = 5,
				voxelDepth = voxelWidth,
				voxelHeight = voxelWidth,
				pixelWidth = voxelWidth,
				pixelHeight = (ushort)(voxelDepth + voxelHeight + 1);
			IVoxelColor voxelColor = new NaiveDimmer(ImageMaker.RainbowPalette);
			List<Sprite> frames = new();
			for (ushort z = 0; z < voxelHeight; z++)
				for (ushort x = 0; x < voxelWidth; x++)
					for (ushort y = 0; y < voxelDepth; y++)
					{
						SvoModel model = new(voxelWidth, voxelDepth, voxelHeight);
						model[x, y, z] = 1;
						Sprite sprite = new(pixelWidth, pixelHeight)
						{
							VoxelColor = voxelColor,
						};
						model.Above(sprite);
						frames.Add(sprite
							.Upscale(4, 4)
							.Draw3x4Bottom(string.Join(",", x, y, z))
							.Upscale(6, 6));
					}
			frames.AnimatedGif(frameDelay: 75)
				.SaveAsGif("AnimatedTest.gif");
		}
		private class CountRenderer : IRectangleRenderer
		{
			public int Count { get; set; } = 0;
			public void Rect(ushort x, ushort y, uint color, ushort sizeX = 1, ushort sizeY = 1) => Count += sizeX * sizeY;
			public void Rect(ushort x, ushort y, byte index, VisibleFace visibleFace = VisibleFace.Front, ushort sizeX = 1, ushort sizeY = 1) => Count += sizeX * sizeY;
		}
		[Fact]
		public void CountTest()
		{
			ushort voxelDepth = 5,
				voxelHeight = voxelDepth;
			List<Voxel2Pixel.Model.Point> points = new();
			CountRenderer countRenderer = new();
			for (ushort z = 0; z < voxelHeight; z++)
				for (ushort y = 0; y < voxelDepth; y++)
				{
					SvoModel model = new(1, voxelDepth, voxelHeight);
					model[0, y, z] = 1;
					countRenderer.Count = 0;
					model.Above(countRenderer);
					if (countRenderer.Count != 2)
						points.Add(new Voxel2Pixel.Model.Point
						{
							X = y,
							Y = z,
						});
				}
			output.WriteLine("Count = " + points.Count());
			output.WriteLine(string.Join(",", points.Select(p => "(" + p.X + "," + p.Y + ")")));
			Assert.Empty(points);
		}
	}
}
