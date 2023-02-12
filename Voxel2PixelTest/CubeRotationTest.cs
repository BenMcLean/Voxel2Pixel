using System;
using System.Collections.Generic;
using System.Text;
using Voxel2Pixel.Model;
using Xunit;

namespace Voxel2PixelTest
{
	public class CubeRotationTest
	{
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
