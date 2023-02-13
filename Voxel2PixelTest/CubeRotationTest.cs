using Voxel2Pixel.Model;
using Xunit;

namespace Voxel2PixelTest
{
	public class CubeRotationTest
	{
		[Fact]
		public void CoordinateTest()
		{
			CubeRotation cubeRotation = CubeRotation.SOUTH0;
			cubeRotation.Rotate(out int x, out int y, out int z, 1, 1, 1);
			Assert.True(x == 1);
			Assert.True(y == 1);
			Assert.True(z == 1);
			cubeRotation = (CubeRotation)cubeRotation.ClockZ();
			cubeRotation.Rotate(out x, out y, out z, 1, 2, 3);
			Assert.True(x == -2);
			Assert.True(y == 1);
			Assert.True(z == 3);
		}
		[Fact]
		public void RelationshipTest()
		{
			foreach (CubeRotation cubeRotation in CubeRotation.Values)
			{
				Assert.Equal(cubeRotation, (CubeRotation)cubeRotation.CounterX().CounterX().CounterX().CounterX());
				Assert.Equal(cubeRotation, (CubeRotation)cubeRotation.CounterY().CounterY().CounterY().CounterY());
				Assert.Equal(cubeRotation, (CubeRotation)cubeRotation.CounterZ().CounterZ().CounterZ().CounterZ());
				Assert.Equal(cubeRotation, (CubeRotation)cubeRotation.ClockX().ClockX().ClockX().ClockX());
				Assert.Equal(cubeRotation, (CubeRotation)cubeRotation.ClockY().ClockY().ClockY().ClockY());
				Assert.Equal(cubeRotation, (CubeRotation)cubeRotation.ClockZ().ClockZ().ClockZ().ClockZ());
			}
		}
	}
}
