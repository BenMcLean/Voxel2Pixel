using System;

namespace Voxel2Pixel.Model
{
	/// <summary>
	/// Rotates models at 90 degree angles, including their sizes
	/// </summary>
	public class TurnModel : IModel, ITurnable
	{
		public IModel Model { get; set; }
		public CubeRotation CubeRotation { get; set; } = CubeRotation.SOUTH0;
		public int RotatedSize(int axis) => ModelSize(CubeRotation.Affected(axis));
		/// <summary>
		/// Allows treating the sizes of the underlying model as if they were in an array. Allows negative values for index, unlike an array, and will correctly treat the negative index that corresponds to a reversed axis as if it was the the corresponding non-reversed axis. This means -1 will be the same as 0, -2 the same as 1, and -3 the same as 2.
		/// </summary>
		/// <param name="index">index 0 or -1 for x, 1 or -2 for y, or 2 or -3 for z; negative index values have the same size, but different starts and directions</param>
		/// <returns>the size of the specified dimension</returns>
		public int ModelSize(int index)
		{
			switch (index)
			{
				case 0:
				case -1:
					return Model.SizeX;
				case 1:
				case -2:
					return Model.SizeY;
				case 2:
				case -3:
					return Model.SizeZ;
				default:
					throw new ArgumentException("Invalid index: \"" + index + "\"");
			}
		}
		public int Start(int axis) =>
			CubeRotation.Step(axis) < 1 ?
				RotatedSize(axis) - 1// when rot is negative, we need to go from the end of the axis, not the start
				: 0;
		public int StartX => Start(0);
		public int StartY => Start(1);
		public int StartZ => Start(2);
		#region IModel
		public int SizeX => RotatedSize(0);
		public int SizeY => RotatedSize(1);
		public int SizeZ => RotatedSize(2);
		public bool IsInside(int x, int y, int z) => !IsOutside(x, y, z);
		public bool IsOutside(int x, int y, int z) => x < 0 || y < 0 || z < 0 || x >= SizeX || y >= SizeY || z >= SizeZ;
		public int Rotate(int axis, params int[] coordinates) =>
			CubeRotation.Rotate(axis, coordinates) +
			(CubeRotation.Step(axis) < 1 ?
				ModelSize(axis) - 1
				: 0);
		public void Rotate(out int x, out int y, out int z, params int[] coordinates)
		{
			x = Rotate(0, coordinates);
			y = Rotate(1, coordinates);
			z = Rotate(2, coordinates);
		}
		//{
		//	int affected = CubeRotation.Affected(axis),
		//		step = CubeRotation.Step(axis);
		//	return step < 1 ?
		//		ModelSize(affected) - 1 - coordinates[affected]
		//		: coordinates[affected];
		//}
		public static int MessWith(int @int) => @int / 2 + (@int % 2);
		public byte? At(out int x, out int y, out int z, params int[] coordinates)
		{
			//for (int i = 0; i < 3; i++)
			//	coordinates[i] -= MessWith(ModelSize(i));
			Rotate(out x, out y, out z, coordinates);
			//int sizeX = SizeX, sizeY = SizeY, sizeZ = SizeZ;
			return Model.At(x, y, z);
		}
		public byte? At(int x, int y, int z) => At(out _, out _, out _, x, y, z);
		#endregion IModel
		#region ITurnable
		public ITurnable CounterX()
		{
			CubeRotation = (CubeRotation)CubeRotation.CounterX();
			return this;
		}
		public ITurnable CounterY()
		{
			CubeRotation = (CubeRotation)CubeRotation.CounterY();
			return this;
		}
		public ITurnable CounterZ()
		{
			CubeRotation = (CubeRotation)CubeRotation.CounterZ();
			return this;
		}
		public ITurnable ClockX()
		{
			CubeRotation = (CubeRotation)CubeRotation.ClockX();
			return this;
		}
		public ITurnable ClockY()
		{
			CubeRotation = (CubeRotation)CubeRotation.ClockY();
			return this;
		}
		public ITurnable ClockZ()
		{
			CubeRotation = (CubeRotation)CubeRotation.ClockZ();
			return this;
		}
		public ITurnable Reset()
		{
			CubeRotation = CubeRotation.SOUTH0;
			return this;
		}
		#endregion ITurnable
	}
}
