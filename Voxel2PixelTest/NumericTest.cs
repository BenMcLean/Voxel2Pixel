using System.Numerics;
using Voxel2Pixel.Model;
using Xunit;
using Xunit.Abstractions;

namespace Voxel2PixelTest
{
	public class NumericTest
	{
		private readonly ITestOutputHelper output;
		public NumericTest(ITestOutputHelper output) => this.output = output;
		public static readonly Matrix4x4 clockX = new Matrix4x4(
				1, 0, 0, 0,
				0, 0, -1, 0,
				0, 1, 0, 0,
				0, 0, 0, 1),
			clockY = new Matrix4x4(
				0, 0, -1, 0,
				0, 1, 0, 0,
				1, 0, 0, 0,
				0, 0, 0, 1),
			clockZ = new Matrix4x4(
				0, -1, 0, 0,
				1, 0, 0, 0,
				0, 0, 1, 0,
				0, 0, 0, 1),
			counterX, counterY, counterZ;
		static NumericTest()
		{
			Matrix4x4.Invert(clockX, out counterX);
			Matrix4x4.Invert(clockY, out counterY);
			Matrix4x4.Invert(clockZ, out counterZ);
		}
		private void Rotate(out int outX, out int outY, out int outZ, int x, int y, int z, params Matrix4x4[] rotations)
		{
			Matrix4x4 coords = new Matrix4x4(
				x, 0, 0, 0,
				y, 0, 0, 0,
				z, 0, 0, 0,
				0, 0, 0, 0);
			foreach (Matrix4x4 rotation in rotations)
				coords = rotation * coords;
			outX = (int)coords.M11;
			outY = (int)coords.M21;
			outZ = (int)coords.M31;
			output.WriteLine(string.Join(", ", outX, outY, outZ));
		}
		private void TestRotation(int x, int y, int z, CubeRotation cubeRotation, params Matrix4x4[] rotations)
		{
			output.WriteLine("Testing " + cubeRotation.Name);
			Rotate(out int x1, out int y1, out int z1, x, y, z, rotations);
			cubeRotation.Rotate(out int x2, out int y2, out int z2, x, y, z);
			Assert.True(x1 == x2);
			Assert.True(y1 == y2);
			Assert.True(z1 == z2);
		}
		[Fact]
		public void Test()
		{
			CubeRotation c = CubeRotation.SOUTH0;
			TestRotation(1, 2, 3, c, Matrix4x4.Identity);
			TestRotation(1, 2, 3, (CubeRotation)c.ClockZ(), clockZ);
		}
	}
}
