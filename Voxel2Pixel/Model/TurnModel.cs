using System;

namespace Voxel2Pixel.Model
{
	/// <summary>
	/// Rotates models at 90 degree angles, including their sizes
	/// </summary>
	public class TurnModel : ContainerModel, ITurnable
	{
		public CuboidOrientation CuboidOrientation { get; set; } = CuboidOrientation.SOUTH0;
		#region ContainerModel
		public override int SizeX => RotatedSize(0);
		public override int SizeY => RotatedSize(1);
		public override int SizeZ => RotatedSize(2);
		public override bool IsInside(int x, int y, int z) => !IsOutside(x, y, z);
		public override bool IsOutside(int x, int y, int z) => x < 0 || y < 0 || z < 0 || x >= SizeX || y >= SizeY || z >= SizeZ;
		public override byte? At(int x, int y, int z)
		{
			Rotate(out int x1, out int y1, out int z1, x, y, z);
			return Model.At(x1, y1, z1);
		}
		#endregion ContainerModel
		#region ITurnable
		public ITurnable CounterX()
		{
			CuboidOrientation = (CuboidOrientation)CuboidOrientation.CounterX();
			return this;
		}
		public ITurnable CounterY()
		{
			CuboidOrientation = (CuboidOrientation)CuboidOrientation.CounterY();
			return this;
		}
		public ITurnable CounterZ()
		{
			CuboidOrientation = (CuboidOrientation)CuboidOrientation.CounterZ();
			return this;
		}
		public ITurnable ClockX()
		{
			CuboidOrientation = (CuboidOrientation)CuboidOrientation.ClockX();
			return this;
		}
		public ITurnable ClockY()
		{
			CuboidOrientation = (CuboidOrientation)CuboidOrientation.ClockY();
			return this;
		}
		public ITurnable ClockZ()
		{
			CuboidOrientation = (CuboidOrientation)CuboidOrientation.ClockZ();
			return this;
		}
		public ITurnable Reset()
		{
			CuboidOrientation = CuboidOrientation.SOUTH0;
			return this;
		}
		#endregion ITurnable
		#region Rotate
		public int Rotate(int axis, params int[] coordinates) => CuboidOrientation.Rotate(axis, coordinates) + Start(axis);
		public void Rotate(out int x, out int y, out int z, params int[] coordinates)
		{
			x = Rotate(0, coordinates);
			y = Rotate(1, coordinates);
			z = Rotate(2, coordinates);
		}
		#endregion Rotate
		#region ReverseRotate
		public int ReverseRotate(int axis, params int[] coordinates) => CuboidOrientation.ReverseRotate(axis, coordinates) + Start(axis);
		public void ReverseRotate(out int x, out int y, out int z, params int[] coordinates)
		{
			x = ReverseRotate(0, coordinates);
			y = ReverseRotate(1, coordinates);
			z = ReverseRotate(2, coordinates);
		}
		public byte? ReverseAt(int x, int y, int z)
		{
			ReverseRotate(out int x1, out int y1, out int z1, x, y, z);
			return Model.At(x1, y1, z1);
		}
		#endregion ReverseRotate
		#region Size
		public int RotatedSize(int axis) => ModelSize(CuboidOrientation.ReverseAffected(axis));
		/// <summary>
		/// Allows treating the sizes of the underlying model as if they were in an array. Allows negative values for index, unlike an array, and will correctly treat the negative index that corresponds to a reversed axis as if it was the the corresponding non-reversed axis. This means -1 will be the same as 0, -2 the same as 1, and -3 the same as 2.
		/// </summary>
		/// <param name="index">index 0 or -1 for x, 1 or -2 for y, or 2 or -3 for z; negative index values have the same size, but different starts and directions</param>
		/// <returns>the size of the specified dimension</returns>
		public int ModelSize(int index)
		{
			switch (index)
			{
				case CuboidOrientation.xPlus:
				case CuboidOrientation.xMinus:
					return Model.SizeX;
				case CuboidOrientation.yPlus:
				case CuboidOrientation.yMinus:
					return Model.SizeY;
				case CuboidOrientation.zPlus:
				case CuboidOrientation.zMinus:
					return Model.SizeZ;
				default:
					throw new ArgumentException("Invalid index: \"" + index + "\"");
			}
		}
		#endregion Size
		#region Start
		public int Start(int axis) =>
			CuboidOrientation.Step(axis) < 1 ?
				ModelSize(axis) - 1
				: 0;
		public int StartX => Start(0);
		public int StartY => Start(1);
		public int StartZ => Start(2);
		#endregion Start
	}
}
