using System;
using System.IO;
using System.Linq;
using Voxel2Pixel;
using Voxel2Pixel.Model;
using Xunit;
using static Voxel2Pixel.Model.SvoModel;

namespace Voxel2PixelTest.Model
{
	public class SvoModelTest
	{
		private readonly Xunit.Abstractions.ITestOutputHelper output;
		public SvoModelTest(Xunit.Abstractions.ITestOutputHelper output) => this.output = output;
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
		public void CountTest()
		{
			VoxFileModel model = new VoxFileModel(@"..\..\..\Sora.vox");
			SvoModel svo = new SvoModel(model);
			Assert.Equal(
				expected: svo.ListVoxels().Count(),
				actual: svo.Count());
			Assert.Equal(
				expected: model.Count(),
				actual: svo.Count());
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
			Voxel voxel = new Voxel
			{
				X = ushort.MaxValue - 1,
				Y = ushort.MaxValue - 1,
				Z = ushort.MaxValue - 1,
				Index = 1,
			};
			svoModel.Set(voxel);
			Voxel voxel2 = svoModel.First();
			void CompareBinary(ushort a, ushort b) => output.WriteLine(
				Convert.ToString(a, 2)
				+ Environment.NewLine
				+ Convert.ToString(b, 2));
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
	}
}
