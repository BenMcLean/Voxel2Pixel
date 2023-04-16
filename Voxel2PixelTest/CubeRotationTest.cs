using System;
using System.Numerics;
using Voxel2Pixel.Model;
using Xunit;

namespace Voxel2PixelTest
{
	public class CubeRotationTest
	{
		//private readonly Xunit.Abstractions.ITestOutputHelper output;
		//public CuberotationTest(Xunit.Abstractions.ITestOutputHelper output) => this.output = output;
		public enum Turn
		{
			NONE, CLOCKX, CLOCKY, CLOCKZ, COUNTERX, COUNTERY, COUNTERZ
		}
		public static ITurnable MakeTurn(ITurnable turnable, Turn turn)
		{
			switch (turn)
			{
				case Turn.CLOCKX:
					return turnable.ClockX();
				case Turn.CLOCKY:
					return turnable.ClockY();
				case Turn.CLOCKZ:
					return turnable.ClockZ();
				case Turn.COUNTERX:
					return turnable.CounterX();
				case Turn.COUNTERY:
					return turnable.CounterY();
				case Turn.COUNTERZ:
					return turnable.CounterZ();
				default:
					return turnable;
			}
		}
		public static CubeRotation Cube(params Turn[] turns)
		{
			CubeRotation cubeRotation = CubeRotation.SOUTH0;
			foreach (Turn turn in turns)
				cubeRotation = (CubeRotation)MakeTurn(cubeRotation, turn);
			return cubeRotation;
		}
		public static Matrix4x4 Matrix(Turn turn)
		{
			switch (turn)
			{
				case Turn.CLOCKX:
					return clockX;
				case Turn.CLOCKY:
					return clockY;
				case Turn.CLOCKZ:
					return clockZ;
				case Turn.COUNTERX:
					return counterX;
				case Turn.COUNTERY:
					return counterY;
				case Turn.COUNTERZ:
					return counterZ;
				default:
					return Matrix4x4.Identity;
			}
		}
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
		static CubeRotationTest()
		{
			Matrix4x4.Invert(clockX, out counterX);
			Matrix4x4.Invert(clockY, out counterY);
			Matrix4x4.Invert(clockZ, out counterZ);
		}
		[Fact]
		public void MatrixTest()
		{
			Test24(-11, -22, -33);
			Test24(11, -22, -33);
			Test24(-11, 22, -33);
			Test24(11, 22, -33);
			Test24(-11, -22, 33);
			Test24(11, -22, 33);
			Test24(-11, 22, 33);
			Test24(11, 22, 33);
		}
		private void Test24(int x = 1, int y = 2, int z = 3)
		{
			TestRotation(x, y, z, "SOUTH0", Turn.NONE);
			TestRotation(x, y, z, "SOUTH1", Turn.CLOCKY);
			TestRotation(x, y, z, "SOUTH2", Turn.CLOCKY, Turn.CLOCKY);
			TestRotation(x, y, z, "SOUTH3", Turn.COUNTERY);
			TestRotation(x, y, z, "WEST0", Turn.COUNTERZ);
			TestRotation(x, y, z, "WEST1", Turn.COUNTERZ, Turn.CLOCKY);
			TestRotation(x, y, z, "WEST2", Turn.COUNTERZ, Turn.CLOCKY, Turn.CLOCKY);
			TestRotation(x, y, z, "WEST3", Turn.COUNTERZ, Turn.COUNTERY);
			TestRotation(x, y, z, "NORTH0", Turn.CLOCKZ, Turn.CLOCKZ);
			TestRotation(x, y, z, "NORTH1", Turn.COUNTERY, Turn.CLOCKZ, Turn.CLOCKZ);
			TestRotation(x, y, z, "NORTH2", Turn.CLOCKX, Turn.CLOCKX);
			TestRotation(x, y, z, "NORTH3", Turn.CLOCKY, Turn.CLOCKZ, Turn.CLOCKZ);
			TestRotation(x, y, z, "EAST0", Turn.CLOCKZ);
			TestRotation(x, y, z, "EAST1", Turn.CLOCKZ, Turn.CLOCKY);
			TestRotation(x, y, z, "EAST2", Turn.CLOCKZ, Turn.CLOCKY, Turn.CLOCKY);
			TestRotation(x, y, z, "EAST3", Turn.CLOCKZ, Turn.COUNTERY);
			TestRotation(x, y, z, "UP0", Turn.CLOCKX, Turn.CLOCKY, Turn.CLOCKY);
			TestRotation(x, y, z, "UP1", Turn.CLOCKX, Turn.COUNTERY);
			TestRotation(x, y, z, "UP2", Turn.CLOCKX);
			TestRotation(x, y, z, "UP3", Turn.CLOCKX, Turn.CLOCKY);
			TestRotation(x, y, z, "DOWN0", Turn.CLOCKX, Turn.CLOCKZ, Turn.CLOCKZ);
			TestRotation(x, y, z, "DOWN1", Turn.COUNTERX, Turn.COUNTERY);
			TestRotation(x, y, z, "DOWN2", Turn.COUNTERX);
			TestRotation(x, y, z, "DOWN3", Turn.COUNTERX, Turn.CLOCKY);
		}
		private void TestRotation(int x, int y, int z, string name, params Turn[] turns)
		{
			CubeRotation cubeRotation = Cube(turns);
			cubeRotation.Rotate(out int x1, out int y1, out int z1, x, y, z);
			Rotate(out int x2, out int y2, out int z2, x, y, z, turns);
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
			Assert.Equal(
				expected: Math.Abs(x1),
				actual: turnModel.SizeX);
			Assert.Equal(
				expected: Math.Abs(y1),
				actual: turnModel.SizeY);
			Assert.Equal(
				expected: Math.Abs(z1),
				actual: turnModel.SizeZ);
		}
		private static void Rotate(out int outX, out int outY, out int outZ, int x, int y, int z, params Turn[] turns)
		{
			Matrix4x4 coords = new Matrix4x4(
				x, 0, 0, 0,
				y, 0, 0, 0,
				z, 0, 0, 0,
				1, 0, 0, 0);
			foreach (Turn turn in turns)
				coords = Matrix(turn) * coords;
			outX = (int)coords.M11;
			outY = (int)coords.M21;
			outZ = (int)coords.M31;
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
		[Fact]
		public void FlipBitsTest()
		{
			Assert.Equal(
				expected: 0,
				actual: CubeRotation.FlipBits(-1));
			Assert.Equal(
				expected: 0,
				actual: CubeRotation.FlipBits(0));
			Assert.Equal(
				expected: 1,
				actual: CubeRotation.FlipBits(-2));
			Assert.Equal(
				expected: 1,
				actual: CubeRotation.FlipBits(1));
			Assert.Equal(
				expected: 2,
				actual: CubeRotation.FlipBits(-3));
			Assert.Equal(
				expected: 2,
				actual: CubeRotation.FlipBits(2));
		}
	}
}
