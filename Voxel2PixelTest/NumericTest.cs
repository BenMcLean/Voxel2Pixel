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
		[Fact]
		public void Test()
		{
			Matrix4x4 coords = new Matrix4x4(
				1, 0, 0, 0,
				2, 0, 0, 0,
				3, 0, 0, 0,
				0, 0, 0, 0),
				result = clockZ * coords;
			output.WriteLine(string.Join(", ", result.M11, result.M21, result.M31));
		}
	}
}
