using System;
using System.Collections.Generic;
using Voxel2Pixel.Interfaces;

namespace Voxel2Pixel.Model
{
	/// <summary>
	/// Rotates models at 90 degree angles, including their sizes
	/// </summary>
	public class TurnModel : ContainerModel, ITurnable
	{
		public CuboidOrientation CuboidOrientation { get; set; } = CuboidOrientation.SOUTH0;
		#region ContainerModel
		public override ushort SizeX => RotatedSize(0);
		public override ushort SizeY => RotatedSize(1);
		public override ushort SizeZ => RotatedSize(2);
		public ushort[] Sizes => new ushort[] { SizeX, SizeY, SizeZ };
		public override byte this[ushort x, ushort y, ushort z]
		{
			get
			{
				Rotate(out ushort x1, out ushort y1, out ushort z1, x, y, z);
				return Model[x1, y1, z1];
			}
		}
		public override IEnumerable<Voxel> GetEnumerator()
		{
			foreach (Voxel voxel in Model)
			{
				ReverseRotate(out ushort x, out ushort y, out ushort z, voxel.X, voxel.Y, voxel.Z);
				yield return new Voxel(x, y, z, voxel.@byte);
			}
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
			CuboidOrientation = (CuboidOrientation)CuboidOrientation.Reset();
			return this;
		}
		#endregion ITurnable
		#region Rotate
		public ushort Rotate(int axis, params ushort[] coordinates) => Rotate(axis, Array.ConvertAll(coordinates, @ushort => (int)@ushort));
		public ushort Rotate(int axis, params int[] coordinates) => checked((ushort)(CuboidOrientation.Rotate(axis, coordinates) + CuboidOrientation.Offset(axis, SizeX, SizeY, SizeZ)));
		public void Rotate(out ushort x, out ushort y, out ushort z, params ushort[] coordinates) => Rotate(out x, out y, out z, Array.ConvertAll(coordinates, @ushort => (int)@ushort));
		public void Rotate(out ushort x, out ushort y, out ushort z, params int[] coordinates)
		{
			x = Rotate(0, coordinates);
			y = Rotate(1, coordinates);
			z = Rotate(2, coordinates);
		}
		#endregion Rotate
		#region ReverseRotate
		public ushort ReverseRotate(int axis, params ushort[] coordinates) => ReverseRotate(axis, Array.ConvertAll(coordinates, @ushort => (int)@ushort));
		public ushort ReverseRotate(int axis, params int[] coordinates) => checked((ushort)(CuboidOrientation.ReverseRotate(axis, coordinates) + CuboidOrientation.Offset(CuboidOrientation.ReverseAffected(axis), SizeX, SizeY, SizeZ)));
		public void ReverseRotate(out ushort x, out ushort y, out ushort z, params ushort[] coordinates) => ReverseRotate(out x, out y, out z, Array.ConvertAll(coordinates, @ushort => (int)@ushort));
		public void ReverseRotate(out ushort x, out ushort y, out ushort z, params int[] coordinates)
		{
			x = ReverseRotate(0, coordinates);
			y = ReverseRotate(1, coordinates);
			z = ReverseRotate(2, coordinates);
		}
		public byte Reverse(ushort x, ushort y, ushort z)
		{
			ReverseRotate(out ushort x1, out ushort y1, out ushort z1, x, y, z);
			return Model[x1, y1, z1];
		}
		#endregion ReverseRotate
		#region Size
		public ushort RotatedSize(int axis) => ModelSize(CuboidOrientation.ReverseAffected(axis));
		/// <summary>
		/// Allows treating the sizes of the underlying model as if they were in an array. Allows negative values for index, unlike an array, and will correctly treat the negative index that corresponds to a reversed axis as if it was the the corresponding non-reversed axis. This means -1 will be the same as 0, -2 the same as 1, and -3 the same as 2.
		/// </summary>
		/// <param name="index">index 0 or -1 for x, 1 or -2 for y, or 2 or -3 for z; negative index values have the same size, but different starts and directions</param>
		/// <returns>the size of the specified dimension</returns>
		public ushort ModelSize(int index)
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
	}
}
