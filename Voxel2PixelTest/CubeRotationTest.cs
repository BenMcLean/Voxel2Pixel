﻿using System.Numerics;
using Voxel2Pixel.Model;
using Xunit;
using Xunit.Abstractions;

namespace Voxel2PixelTest
{
	public class CuberotationTest
	{
		private readonly ITestOutputHelper output;
		public CuberotationTest(ITestOutputHelper output) => this.output = output;
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
		static CuberotationTest()
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
		private void TestRotation(int x, int y, int z, string name, CubeRotation cubeRotation, params Matrix4x4[] rotations)
		{
			cubeRotation.Rotate(out int x1, out int y1, out int z1, x, y, z);
			Rotate(out int x2, out int y2, out int z2, x, y, z, rotations);
			output.WriteLine("Input: "
				+ string.Join(", ", x, y, z)
				+ ". " + cubeRotation.Name + ": "
				+ string.Join(", ", x1, y1, z1)
				+ ". Matrix4x4: "
				+ string.Join(", ", x2, y2, z2) + ".");
			Assert.Equal(name, cubeRotation.Name);
			Assert.True(x1 == x2);
			Assert.True(y1 == y2);
			Assert.True(z1 == z2);
		}
		[Fact]
		public void MatrixTest()
		{
			CubeRotation c = CubeRotation.SOUTH0;
			int x = 1, y = 2, z = 3;
			TestRotation(x, y, z, "SOUTH0", c, Matrix4x4.Identity);
			TestRotation(x, y, z, "SOUTH1", (CubeRotation)c.ClockY(), clockY);
			TestRotation(x, y, z, "SOUTH2", (CubeRotation)c.ClockY().ClockY(), clockY, clockY);
			TestRotation(x, y, z, "SOUTH3", (CubeRotation)c.CounterY(), counterY);
			TestRotation(x, y, z, "WEST0", (CubeRotation)c.CounterZ(), counterZ);
			//TestRotation(x, y, z, "WEST1", (CubeRotation)c.CounterZ().ClockY(), counterZ, clockY);
			TestRotation(x, y, z, "WEST2", (CubeRotation)c.CounterZ().ClockY().ClockY(), counterX, clockY, clockY);
			TestRotation(x, y, z, "WEST3", (CubeRotation)c.CounterZ().CounterY(), counterZ, counterY);
			TestRotation(x, y, z, "NORTH0", (CubeRotation)c.ClockZ().ClockZ(), clockZ, clockZ);
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
