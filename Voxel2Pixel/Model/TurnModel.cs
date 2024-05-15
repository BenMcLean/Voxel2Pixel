using System;
using System.Collections.Generic;
using System.Linq;
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
		public ushort[] Sizes => [SizeX, SizeY, SizeZ];
		public override byte this[ushort x, ushort y, ushort z]
		{
			get
			{
				Point3D point = Rotate(new Point3D(x, y, z));
				return Model[(ushort)point.X, (ushort)point.Y, (ushort)point.Z];
			}
		}
		public override IEnumerator<Voxel> GetEnumerator()
		{
			foreach (Voxel voxel in Model)
			{
				Point3D point = ReverseRotate(new Point3D(voxel.X, voxel.Y, voxel.Z));
				yield return new Voxel((ushort)point.X, (ushort)point.Y, (ushort)point.Z, voxel.Index);
			}
		}
		#endregion ContainerModel
		#region ITurnable
		public ITurnable Turn(Turn turn) => turn switch
		{
			Voxel2Pixel.Model.Turn.ClockX => ClockX(),
			Voxel2Pixel.Model.Turn.ClockY => ClockY(),
			Voxel2Pixel.Model.Turn.ClockZ => ClockZ(),
			Voxel2Pixel.Model.Turn.CounterX => CounterX(),
			Voxel2Pixel.Model.Turn.CounterY => CounterY(),
			Voxel2Pixel.Model.Turn.CounterZ => CounterZ(),
			_ => Reset(),
		};
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
		public int Rotate(int axis, Point3D point) => CuboidOrientation.Rotate(axis, point) + CuboidOrientation.Offset(axis, SizeX, SizeY, SizeZ);
		public Point3D Rotate(Point3D point) => new(Enumerable.Range(0, 3).Select(axis => Rotate(axis, point)).ToArray());
		#endregion Rotate
		#region ReverseRotate
		public int ReverseRotate(int axis, Point3D point) => CuboidOrientation.ReverseRotate(axis, point) + CuboidOrientation.Offset(CuboidOrientation.ReverseAffected(axis), SizeX, SizeY, SizeZ);
		public Point3D ReverseRotate(Point3D point) => new(Enumerable.Range(0, 3).Select(axis => ReverseRotate(axis, point)).ToArray());
		public byte Reverse(ushort x, ushort y, ushort z)
		{
			Point3D point = ReverseRotate(new Point3D(x, y, z));
			return Model[(ushort)point.X, (ushort)point.Y, (ushort)point.Z];
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
