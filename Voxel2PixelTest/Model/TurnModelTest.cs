using Voxel2Pixel.Model;
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
				for (ushort x = 0; x < turnModel.Model.SizeX; x++)
					for (ushort y = 0; y < turnModel.Model.SizeY; y++)
						for (ushort z = 0; z < turnModel.Model.SizeZ; z++)
						{
							turnModel.Rotate(out ushort x1, out ushort y1, out ushort z1, x, y, z);
							Assert.True(x1 < turnModel.SizeX);
							Assert.True(y1 < turnModel.SizeY);
							Assert.True(z1 < turnModel.SizeZ);
							turnModel.ReverseRotate(out ushort x2, out ushort y2, out ushort z2, x, y, z);
							turnModel.ReverseRotate(out ushort x3, out ushort y3, out ushort z3, x1, y1, z1);
							Assert.Equal(x, x3);
							Assert.Equal(y, y3);
							Assert.Equal(z, z3);
							turnModel.Rotate(out ushort x4, out ushort y4, out ushort z4, x2, y2, z2);
							Assert.Equal(x, x4);
							Assert.Equal(y, y4);
							Assert.Equal(z, z4);
						}
			}
		}
	}
}
