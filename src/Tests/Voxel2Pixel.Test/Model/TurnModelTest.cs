using BenVoxel.Models;
using BenVoxel.Structs;
using Voxel2Pixel.Model;

namespace Voxel2Pixel.Test.Model;

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
						Point3D p1 = turnModel.Rotate(new(x, y, z));
						Assert.True(p1.X < turnModel.SizeX);
						Assert.True(p1.Y < turnModel.SizeY);
						Assert.True(p1.Z < turnModel.SizeZ);
						Point3D p2 = turnModel.ReverseRotate(p1);
						Assert.Equal(x, p2.X);
						Assert.Equal(y, p2.Y);
						Assert.Equal(z, p2.Z);
					}
		}
	}
}
