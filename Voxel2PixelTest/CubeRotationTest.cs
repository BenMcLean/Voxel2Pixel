using System;
using System.Numerics;
using Voxel2Pixel.Model;
using Xunit;
using Xunit.Abstractions;

namespace Voxel2PixelTest
{
	public class CuberotationTest
	{
		//private readonly ITestOutputHelper output;
		//public CuberotationTest(ITestOutputHelper output) => this.output = output;
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
			{
				coords = rotation * coords;
				coords.M11 = (int)coords.M11;
				coords.M21 = (int)coords.M21;
				coords.M31 = (int)coords.M31;
			}
			outX = (int)coords.M11;
			outY = (int)coords.M21;
			outZ = (int)coords.M31;
		}
		[Fact]
		public void MatrixTest()
		{
			Test24(-1, -2, -3);
			Test24(1, -2, -3);
			Test24(-1, 2, -3);
			Test24(1, 2, -3);
			Test24(-1, -2, 3);
			Test24(1, -2, 3);
			Test24(-1, 2, 3);
			Test24(1, 2, 3);
		}
		private void TestRotation(int x, int y, int z, string name, CubeRotation cubeRotation, params Matrix4x4[] rotations)
		{
			cubeRotation.Rotate(out int x1, out int y1, out int z1, x, y, z);
			Rotate(out int x2, out int y2, out int z2, x, y, z, rotations);
			/*
			output.WriteLine("Input: "
				+ string.Join(", ", x, y, z)
				+ ". " + cubeRotation.Name + ": "
				+ string.Join(", ", x1, y1, z1)
				+ ". Matrix4x4: "
				+ string.Join(", ", x2, y2, z2) + ".");
			*/
			Assert.Equal(name, cubeRotation.Name);
			Assert.Equal(x1, x2);
			Assert.Equal(y1, y2);
			Assert.Equal(z1, z2);
			TurnModel turnModel = new TurnModel
			{
				Model = new EmptyModel
				{
					SizeX = Math.Abs(x),
					SizeY = Math.Abs(y),
					SizeZ = Math.Abs(z),
				},
				CubeRotation = cubeRotation,
			};
			Assert.Equal(turnModel.SizeX, Math.Abs(x1));
			Assert.Equal(turnModel.SizeY, Math.Abs(y1));
			Assert.Equal(turnModel.SizeZ, Math.Abs(z1));
		}
		private void Test24(int x = 1, int y = 2, int z = 3)
		{
			CubeRotation c = CubeRotation.SOUTH0;
			TestRotation(x, y, z, "SOUTH0", c, Matrix4x4.Identity);
			TestRotation(x, y, z, "SOUTH1", (CubeRotation)c.ClockY(), clockY);
			TestRotation(x, y, z, "SOUTH2", (CubeRotation)c.ClockY().ClockY(), clockY, clockY);
			TestRotation(x, y, z, "SOUTH3", (CubeRotation)c.CounterY(), counterY);
			TestRotation(x, y, z, "WEST0", (CubeRotation)c.CounterZ(), counterZ);
			TestRotation(x, y, z, "WEST1", (CubeRotation)c.CounterZ().ClockY(), counterZ, clockY);
			TestRotation(x, y, z, "WEST2", (CubeRotation)c.CounterZ().ClockY().ClockY(), counterZ, clockY, clockY);
			TestRotation(x, y, z, "WEST3", (CubeRotation)c.CounterZ().CounterY(), counterZ, counterY);
			TestRotation(x, y, z, "NORTH0", (CubeRotation)c.ClockZ().ClockZ(), clockZ, clockZ);
			TestRotation(x, y, z, "NORTH1", (CubeRotation)c.CounterY().ClockZ().ClockZ(), counterY, clockZ, clockZ);
			TestRotation(x, y, z, "NORTH2", (CubeRotation)c.ClockX().ClockX(), clockX, clockX);
			TestRotation(x, y, z, "NORTH3", (CubeRotation)c.ClockY().ClockZ().ClockZ(), clockY, clockZ, clockZ);
			TestRotation(x, y, z, "EAST0", (CubeRotation)c.ClockZ(), clockZ);
			TestRotation(x, y, z, "EAST1", (CubeRotation)c.ClockZ().ClockY(), clockZ, clockY);
			TestRotation(x, y, z, "EAST2", (CubeRotation)c.ClockZ().ClockY().ClockY(), clockZ, clockY, clockY);
			TestRotation(x, y, z, "EAST3", (CubeRotation)c.ClockZ().CounterY(), clockZ, counterY);
			TestRotation(x, y, z, "UP0", (CubeRotation)c.ClockX().ClockY().ClockY(), clockX, clockY, clockY);
			TestRotation(x, y, z, "UP1", (CubeRotation)c.ClockX().CounterY(), clockX, counterY);
			TestRotation(x, y, z, "UP2", (CubeRotation)c.ClockX(), clockX);
			TestRotation(x, y, z, "UP3", (CubeRotation)c.ClockX().ClockY(), clockX, clockY);
			TestRotation(x, y, z, "DOWN0", (CubeRotation)c.ClockX().ClockZ().ClockZ(), clockX, clockZ, clockZ);
			TestRotation(x, y, z, "DOWN1", (CubeRotation)c.CounterX().CounterY(), counterX, counterY);
			TestRotation(x, y, z, "DOWN2", (CubeRotation)c.CounterX(), counterX);
			TestRotation(x, y, z, "DOWN3", (CubeRotation)c.CounterX().ClockY(), counterX, clockY);
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
