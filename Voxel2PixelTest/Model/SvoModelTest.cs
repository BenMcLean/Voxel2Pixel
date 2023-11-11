using System.Linq;
using Voxel2Pixel.Model;
using Xunit;
using static Voxel2Pixel.Model.SvoModel;

namespace Voxel2PixelTest.Model
{
	public class SvoModelTest
	{
		//private readonly Xunit.Abstractions.ITestOutputHelper output;
		//public SvoModelTest(Xunit.Abstractions.ITestOutputHelper output) => this.output = output;
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
		public void TrimTest()
		{
			SvoModel svoModel = new SvoModel()
			{
				SizeX = ushort.MaxValue,
				SizeY = ushort.MaxValue,
				SizeZ = ushort.MaxValue,
			};
			Assert.Equal(
				expected: 0,
				actual: svoModel[ushort.MaxValue - 1, ushort.MaxValue - 1, ushort.MaxValue - 1]);
			Assert.Equal(
				expected: 1,
				actual: svoModel.NodeCount);
			svoModel[ushort.MaxValue - 1, ushort.MaxValue - 1, ushort.MaxValue - 1] = 1;
			Assert.Equal(
				expected: 1,
				actual: svoModel[ushort.MaxValue - 1, ushort.MaxValue - 1, ushort.MaxValue - 1]);
			Assert.Equal(
				expected: 16,
				actual: svoModel.NodeCount);
			svoModel[ushort.MaxValue - 1, ushort.MaxValue - 1, ushort.MaxValue - 1] = 0;
			Assert.Equal(
				expected: 0,
				actual: svoModel[ushort.MaxValue - 1, ushort.MaxValue - 1, ushort.MaxValue - 1]);
			Assert.Equal(
				expected: 1,
				actual: svoModel.NodeCount);
		}
	}
}
