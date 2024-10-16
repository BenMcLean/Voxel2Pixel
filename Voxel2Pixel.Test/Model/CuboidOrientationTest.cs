using BenVoxel;
using Voxel2Pixel.Model;

namespace Voxel2Pixel.Test.Model;

public class CuboidOrientationTest
{
	//private readonly Xunit.Abstractions.ITestOutputHelper output;
	//public CuboidOrientationTest(Xunit.Abstractions.ITestOutputHelper output) => this.output = output;
	[Fact]
	public void RelationshipTest()
	{
		foreach (CuboidOrientation cuboidOrientation in CuboidOrientation.Values)
		{
			Assert.Equal(cuboidOrientation, (CuboidOrientation)cuboidOrientation.Turn(Turn.CounterX, Turn.CounterX, Turn.CounterX, Turn.CounterX));
			Assert.Equal(cuboidOrientation, (CuboidOrientation)cuboidOrientation.Turn(Turn.CounterY, Turn.CounterY, Turn.CounterY, Turn.CounterY));
			Assert.Equal(cuboidOrientation, (CuboidOrientation)cuboidOrientation.Turn(Turn.CounterZ, Turn.CounterZ, Turn.CounterZ, Turn.CounterZ));
			Assert.Equal(cuboidOrientation, (CuboidOrientation)cuboidOrientation.Turn(Turn.ClockX, Turn.ClockX, Turn.ClockX, Turn.ClockX));
			Assert.Equal(cuboidOrientation, (CuboidOrientation)cuboidOrientation.Turn(Turn.ClockY, Turn.ClockY, Turn.ClockY, Turn.ClockY));
			Assert.Equal(cuboidOrientation, (CuboidOrientation)cuboidOrientation.Turn(Turn.ClockZ, Turn.ClockZ, Turn.ClockZ, Turn.ClockZ));
			Assert.NotEqual(cuboidOrientation, (CuboidOrientation)cuboidOrientation.Turn(Turn.ClockX));
			Assert.NotEqual(cuboidOrientation, (CuboidOrientation)cuboidOrientation.Turn(Turn.ClockY));
			Assert.NotEqual(cuboidOrientation, (CuboidOrientation)cuboidOrientation.Turn(Turn.ClockZ));
			Assert.NotEqual(cuboidOrientation, (CuboidOrientation)cuboidOrientation.Turn(Turn.CounterX));
			Assert.NotEqual(cuboidOrientation, (CuboidOrientation)cuboidOrientation.Turn(Turn.CounterY));
			Assert.NotEqual(cuboidOrientation, (CuboidOrientation)cuboidOrientation.Turn(Turn.CounterZ));
			Assert.Equal(cuboidOrientation, (CuboidOrientation)cuboidOrientation.Turn(Turn.ClockX, Turn.CounterX));
			Assert.Equal(cuboidOrientation, (CuboidOrientation)cuboidOrientation.Turn(Turn.ClockY, Turn.CounterY));
			Assert.Equal(cuboidOrientation, (CuboidOrientation)cuboidOrientation.Turn(Turn.ClockZ, Turn.CounterZ));
			int x = 0, y = 1, z = 2;
			Point3D p = new(x, y, z),
				p1 = cuboidOrientation.Rotate(p);
			if (cuboidOrientation == CuboidOrientation.SOUTH0)
			{
				Assert.Equal(x, p1.X);
				Assert.Equal(y, p1.Y);
				Assert.Equal(z, p1.Z);
			}
			else
				Assert.True(x != p1.X
					|| y != p1.Y
					|| z != p1.Z);
			Point3D p2 = cuboidOrientation.ReverseRotate(p1);
			Assert.Equal(x, p2.X);
			Assert.Equal(y, p2.Y);
			Assert.Equal(z, p2.Z);
		}
	}
	[Fact]
	public void FlipBitsTest()
	{
		Assert.Equal(
			expected: 0,
			actual: CuboidOrientation.FlipBits(-1));
		Assert.Equal(
			expected: 0,
			actual: CuboidOrientation.FlipBits(0));
		Assert.Equal(
			expected: 1,
			actual: CuboidOrientation.FlipBits(-2));
		Assert.Equal(
			expected: 1,
			actual: CuboidOrientation.FlipBits(1));
		Assert.Equal(
			expected: 2,
			actual: CuboidOrientation.FlipBits(-3));
		Assert.Equal(
			expected: 2,
			actual: CuboidOrientation.FlipBits(2));
	}
}
