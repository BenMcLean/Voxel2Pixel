using System.Numerics;
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
		}
		[Fact]
		public void Test()
		{
			Matrix4x4 coords = new Matrix4x4(
				1, 0, 0, 0,
				2, 0, 0, 0,
				3, 0, 0, 0,
				0, 0, 0, 0),
				result = clockZ * coords;
			Rotate(out int x, out int y, out int z, 1, 2, 3, clockZ);
			Assert.True(result.M11 == x);
			Assert.True(result.M21 == y);
			Assert.True(result.M31 == z);
		}
	}
}
