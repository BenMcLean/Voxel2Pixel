using System.Numerics;
using Xunit;
using Xunit.Abstractions;

namespace Voxel2PixelTest
{
	public class NumericTest
	{
		private readonly ITestOutputHelper output;
		public NumericTest(ITestOutputHelper output) => this.output = output;
		public static readonly Matrix4x4 rotX = new Matrix4x4(
				1, 0, 0, 0,
				0, 0, -1, 0,
				0, 1, 0, 0,
				0, 0, 0, 1),
			rotY = new Matrix4x4(
				0, 0, -1, 0,
				0, 1, 0, 0,
				1, 0, 0, 0,
				0, 0, 0, 1),
			rotZ = new Matrix4x4(
				0, -1, 0, 0,
				1, 0, 0, 0,
				0, 0, 1, 0,
				0, 0, 0, 1),
			inverseX, inverseY, inverseZ;
		static NumericTest()
		{
			Matrix4x4.Invert(rotX, out inverseX);
			Matrix4x4.Invert(rotY, out inverseY);
			Matrix4x4.Invert(rotZ, out inverseZ);
		}
		[Fact]
		public void Test()
		{
			Matrix4x4 coords = new Matrix4x4(
				1, 0, 0, 0,
				2, 0, 0, 0,
				3, 0, 0, 0,
				0, 0, 0, 0),
				result = rotZ * coords;
			output.WriteLine(string.Join(", ", result.M11, result.M21, result.M31));
		}
	}
}
