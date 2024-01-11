using SixLabors.ImageSharp;
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
			Leaf leaf = new Leaf(null, 0);
			for (byte i = 0; i < bytes.Length; i++)
			{
				leaf[i] = bytes[i];
				Assert.Equal(
					expected: bytes[i],
					actual: leaf[i]);
			}
			byte[] written;
			using (MemoryStream ms = new MemoryStream())
			{
				leaf.Write(ms);
				written = ms.ToArray();
			}
			output.WriteLine(BitConverter.ToString(written));
		}
		[Fact]
		public void ModelTest()
		{
			VoxFileModel model = new VoxFileModel(@"..\..\..\Sora.vox");
			SvoModel svo = new SvoModel(model);
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
			SvoModel svoModel = new SvoModel()
			{
				SizeX = ushort.MaxValue,
				SizeY = ushort.MaxValue,
				SizeZ = ushort.MaxValue,
			};
			Voxel voxel = new Voxel(
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
			SvoModel svoModel = new SvoModel()
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
			VoxFileModel model = new VoxFileModel(@"..\..\..\Sora.vox");
			SvoModel svo = new SvoModel(model),
				svo2 = new SvoModel(svo.Z85(), svo.SizeX, svo.SizeY, svo.SizeZ);
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
			using (MemoryStream ms = new MemoryStream())
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
			Random rng = new Random();
			ushort Next()
			{
				byte[] two = new byte[2];
				rng.NextBytes(two);
				return (ushort)(two[0] << 8 | two[1]);
			}
			ushort x = Next(), z = Next();
			byte left(byte count) => (byte)(((z >> count) & 1) << 2 | (x >> count) & 1);
			Stack<byte> octants = new Stack<byte>();
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
			VoxFileModel model = new VoxFileModel(@"..\..\..\Sora.vox");
			SvoModel svo = new SvoModel(model);
			Sprite sprite = new Sprite(svo.SizeX, svo.SizeZ)
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
			VoxFileModel model = new VoxFileModel(@"..\..\..\Sora.vox");
			IVoxelColor voxelColor = new NaiveDimmer(model.Palette);
			SvoModel svo = new SvoModel(model);
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
			VoxFileModel model = new VoxFileModel(@"..\..\..\Sora.vox");
			IVoxelColor voxelColor = new NaiveDimmer(model.Palette);
			SvoModel svo = new SvoModel(model);
			Sprite[] sprites = new Sprite[2]
			{
				new Sprite(svo.SizeX, (ushort)(svo.SizeY + svo.SizeZ))
				{
					VoxelColor = voxelColor,
				},
				new Sprite(svo.SizeX, (ushort)(svo.SizeY + svo.SizeZ))
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
		[Fact]
		public void TurtleTest()
		{
			static ushort expected(ushort startY, ushort startZ, ushort newZ, bool zFirst = false)
			{
				ushort y = startY;
				int z = startZ;
				while (z != newZ)
					if (zFirst && y - startY < startZ - z
						|| !zFirst && y - startY <= startZ - z)
						y++;
					else
						z--;
				return y;
			}
			static ushort actual(ushort startY, ushort startZ, ushort newZ, bool zFirst = false) => (ushort)(startY + startZ - newZ - (zFirst && startZ != newZ ? 1 : 0));
			for (ushort startY = 10; startY < 19; startY++)
				for (ushort startZ = 10; startZ < 19; startZ++)
					for (ushort newZ = startZ; newZ > startZ - 9; newZ--)
					{
						output.WriteLine(string.Join(", ",
							"startX: " + startY,
							"startY: " + startZ,
							"newZ: " + newZ,
							"expected newY false: " + expected(startY, startZ, newZ, false),
							"actual newY false: " + actual(startY, startZ, newZ, false),
							"expected newY true: " + expected(startY, startZ, newZ, true),
							"actual newY true: " + actual(startY, startZ, newZ, true)));
						Assert.Equal(
							expected: expected(startY, startZ, newZ, false),
							actual: actual(startY, startZ, newZ, false));
						Assert.Equal(
							expected: expected(startY, startZ, newZ, true),
							actual: actual(startY, startZ, newZ, true));
					}
		}
	}
}
