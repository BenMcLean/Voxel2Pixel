using System.Collections.ObjectModel;
using System;
using System.Linq;

namespace Voxel2Pixel.Model
{
	/// <summary>
	/// There are only 24 possible orientations achievable by 90 degree rotations around coordinate axis, which form the rotation group of a cube, also known as the chiral octahedral symmetry group.
	/// http://www.ams.org/samplings/feature-column/fcarc-cubes7
	/// https://en.wikipedia.org/wiki/Octahedral_symmetry#Chiral_octahedral_symmetry
	/// In 3D space for voxels, I'm following the MagicaVoxel convention, which is Z+up, right-handed, so X+ means east/right, Y+ means forwards/north and Z+ means up.
	/// </summary>
	public sealed class CubeRotation : ITurnable
	{
		#region Instances
		private const int
			xPlus = 0,
			yPlus = 1,
			zPlus = 2,
			xMinus = -1,
			yMinus = -2,
			zMinus = -3;
		public static readonly CubeRotation
			SOUTH0 = new CubeRotation(0, "SOUTH0", xPlus, yPlus, zPlus),
			SOUTH1 = new CubeRotation(1, "SOUTH1", zMinus, yPlus, xPlus),
			SOUTH2 = new CubeRotation(2, "SOUTH2", xMinus, yPlus, zMinus),
			SOUTH3 = new CubeRotation(3, "SOUTH3", zPlus, yPlus, xMinus),
			WEST0 = new CubeRotation(4, "WEST0", yPlus, xMinus, zPlus),
			WEST1 = new CubeRotation(5, "WEST1", zMinus, xMinus, yPlus),
			WEST2 = new CubeRotation(6, "WEST2", yMinus, xMinus, zMinus),
			WEST3 = new CubeRotation(7, "WEST3", zPlus, xMinus, yMinus),
			NORTH0 = new CubeRotation(8, "NORTH0", xMinus, yMinus, zPlus),
			NORTH1 = new CubeRotation(9, "NORTH1", zMinus, yMinus, xMinus),
			NORTH2 = new CubeRotation(10, "NORTH2", xPlus, yMinus, zMinus),
			NORTH3 = new CubeRotation(11, "NORTH3", zPlus, yMinus, xPlus),
			EAST0 = new CubeRotation(12, "EAST0", yMinus, xPlus, zPlus),
			EAST1 = new CubeRotation(13, "EAST1", zMinus, xPlus, yMinus),
			EAST2 = new CubeRotation(14, "EAST2", yPlus, xPlus, zMinus),
			EAST3 = new CubeRotation(15, "EAST3", zPlus, xPlus, yPlus),
			UP0 = new CubeRotation(16, "UP0", xMinus, zMinus, yMinus),
			UP1 = new CubeRotation(17, "UP1", yPlus, zMinus, xMinus),
			UP2 = new CubeRotation(18, "UP2", xPlus, zMinus, yPlus),
			UP3 = new CubeRotation(19, "UP3", yMinus, zMinus, xPlus),
			DOWN0 = new CubeRotation(20, "DOWN0", xPlus, zMinus, yPlus),
			DOWN1 = new CubeRotation(21, "DOWN1", zMinus, xMinus, yPlus),
			DOWN2 = new CubeRotation(22, "DOWN2", xMinus, zPlus, yPlus),
			DOWN3 = new CubeRotation(23, "DOWN3", zPlus, xPlus, yPlus);
		public static readonly ReadOnlyCollection<CubeRotation> Values = Array.AsReadOnly(new CubeRotation[] { SOUTH0, SOUTH1, SOUTH2, SOUTH3, WEST0, WEST1, WEST2, WEST3, NORTH0, NORTH1, NORTH2, NORTH3, EAST0, EAST1, EAST2, EAST3, UP0, UP1, UP2, UP3, DOWN0, DOWN1, DOWN2, DOWN3 });
		#endregion Instances
		#region Data members
		public readonly byte Value;
		public readonly string Name;
		public readonly ReadOnlyCollection<int> Rotation;
		#endregion Data members
		#region CubeRotation
		private CubeRotation(byte value, string name, params int[] rotation)
		{
			Value = value;
			Name = name;
			Rotation = Array.AsReadOnly(rotation);
		}
		public override string ToString() => Name;
		public static bool operator ==(CubeRotation obj1, CubeRotation obj2) => obj1.Equals(obj2);
		public static bool operator !=(CubeRotation obj1, CubeRotation obj2) => !(obj1 == obj2);
		public override bool Equals(object other) => other is CubeRotation cubeRotation && Value == cubeRotation.Value;
		public override int GetHashCode() => Value;
		#endregion CubeRotation
		#region Sides
		public bool South => Value < 4;
		public bool West => Value > 3 && Value < 8;
		public bool North => Value > 7 && Value < 12;
		public bool East => Value > 11 && Value < 16;
		public bool Up => Value > 15 && Value < 20;
		public bool Down => Value > 19;
		#endregion Sides
		#region ITurnable
		public ITurnable CounterX() => CounterX(Value);
		public static CubeRotation CounterX(byte value)
		{
			switch (value)
			{
				default:
				case 0://SOUTH0:
					return DOWN2;
				case 1://SOUTH1:
					return WEST1;
				case 2://SOUTH2:
					return UP0;
				case 3://SOUTH3:
					return EAST3;
				case 4://WEST0:
					return DOWN1;
				case 5://WEST1:
					return NORTH1;
				case 6://WEST2:
					return UP1;
				case 7://WEST3:
					return SOUTH3;
				case 8://NORTH0:
					return DOWN0;
				case 9://NORTH1:
					return EAST1;
				case 10://NORTH2:
					return UP2;
				case 11://NORTH3:
					return WEST3;
				case 12://EAST0:
					return DOWN3;
				case 13://EAST1:
					return SOUTH1;
				case 14://EAST2:
					return UP3;
				case 15://EAST3:
					return NORTH3;
				case 16://UP0:
					return NORTH0;
				case 17://UP1:
					return EAST0;
				case 18://UP2:
					return SOUTH0;
				case 19://UP3:
					return WEST0;
				case 20://DOWN0:
					return SOUTH2;
				case 21://DOWN1:
					return EAST2;
				case 22://DOWN2:
					return NORTH2;
				case 23://DOWN3:
					return WEST2;
			}
		}
		public ITurnable CounterY() => CounterY(Value);
		public static CubeRotation CounterY(byte value)
		{
			switch (value)
			{
				default:
				case 0://SOUTH0:
					return SOUTH3;
				case 1://SOUTH1:
					return SOUTH0;
				case 2://SOUTH2:
					return SOUTH1;
				case 3://SOUTH3:
					return SOUTH2;
				case 4://WEST0:
					return WEST3;
				case 5://WEST1:
					return WEST0;
				case 6://WEST2:
					return WEST1;
				case 7://WEST3:
					return WEST2;
				case 8://NORTH0:
					return NORTH3;
				case 9://NORTH1:
					return NORTH0;
				case 10://NORTH2:
					return NORTH1;
				case 11://NORTH3:
					return NORTH2;
				case 12://EAST0:
					return EAST3;
				case 13://EAST1:
					return EAST0;
				case 14://EAST2:
					return EAST1;
				case 15://EAST3:
					return EAST2;
				case 16://UP0:
					return UP3;
				case 17://UP1:
					return UP0;
				case 18://UP2:
					return UP1;
				case 19://UP3:
					return UP2;
				case 20://DOWN0:
					return DOWN3;
				case 21://DOWN1:
					return DOWN0;
				case 22://DOWN2:
					return DOWN1;
				case 23://DOWN3:
					return DOWN2;
			}
		}
		public ITurnable CounterZ() => CounterZ(Value);
		public static CubeRotation CounterZ(byte value)
		{
			switch (value)
			{
				default:
				case 0://SOUTH0:
					return WEST0;
				case 1://SOUTH1:
					return UP3;
				case 2://SOUTH2:
					return EAST2;
				case 3://SOUTH3:
					return DOWN1;
				case 4://WEST0:
					return NORTH0;
				case 5://WEST1:
					return UP0;
				case 6://WEST2:
					return SOUTH2;
				case 7://WEST3:
					return DOWN0;
				case 8://NORTH0:
					return EAST0;
				case 9://NORTH1:
					return UP1;
				case 10://NORTH2:
					return WEST2;
				case 11://NORTH3:
					return DOWN3;
				case 12://EAST0:
					return SOUTH0;
				case 13://EAST1:
					return UP2;
				case 14://EAST2:
					return NORTH2;
				case 15://EAST3:
					return DOWN2;
				case 16://UP0:
					return EAST3;
				case 17://UP1:
					return SOUTH3;
				case 18://UP2:
					return WEST3;
				case 19://UP3:
					return NORTH3;
				case 20://DOWN0:
					return EAST1;
				case 21://DOWN1:
					return NORTH1;
				case 22://DOWN2:
					return WEST1;
				case 23://DOWN3:
					return SOUTH1;
			}
		}
		public ITurnable ClockX() => ClockX(Value);
		public static CubeRotation ClockX(byte value)
		{
			switch (value)
			{
				default:
				case 0://SOUTH0:
					return UP2;
				case 1://SOUTH1:
					return EAST1;
				case 2://SOUTH2:
					return DOWN0;
				case 3://SOUTH3:
					return WEST3;
				case 4://WEST0:
					return UP3;
				case 5://WEST1:
					return SOUTH1;
				case 6://WEST2:
					return DOWN3;
				case 7://WEST3:
					return NORTH3;
				case 8://NORTH0:
					return UP0;
				case 9://NORTH1:
					return WEST1;
				case 10://NORTH2:
					return DOWN2;
				case 11://NORTH3:
					return EAST3;
				case 12://EAST0:
					return UP1;
				case 13://EAST1:
					return NORTH1;
				case 14://EAST2:
					return DOWN1;
				case 15://EAST3:
					return SOUTH3;
				case 16://UP0:
					return SOUTH2;
				case 17://UP1:
					return WEST2;
				case 18://UP2:
					return NORTH2;
				case 19://UP3:
					return EAST2;
				case 20://DOWN0:
					return NORTH0;
				case 21://DOWN1:
					return WEST0;
				case 22://DOWN2:
					return SOUTH0;
				case 23://DOWN3:
					return EAST0;
			}
		}
		public ITurnable ClockY() => ClockY(Value);
		public static CubeRotation ClockY(byte value)
		{
			switch (value)
			{
				default:
				case 0://SOUTH0:
					return SOUTH1;
				case 1://SOUTH1:
					return SOUTH2;
				case 2://SOUTH2:
					return SOUTH3;
				case 3://SOUTH3:
					return SOUTH0;
				case 4://WEST0:
					return WEST1;
				case 5://WEST1:
					return WEST2;
				case 6://WEST2:
					return WEST3;
				case 7://WEST3:
					return WEST0;
				case 8://NORTH0:
					return NORTH1;
				case 9://NORTH1:
					return NORTH2;
				case 10://NORTH2:
					return NORTH3;
				case 11://NORTH3:
					return NORTH0;
				case 12://EAST0:
					return EAST1;
				case 13://EAST1:
					return EAST2;
				case 14://EAST2:
					return EAST3;
				case 15://EAST3:
					return EAST0;
				case 16://UP0:
					return UP1;
				case 17://UP1:
					return UP2;
				case 18://UP2:
					return UP3;
				case 19://UP3:
					return UP0;
				case 20://DOWN0:
					return DOWN1;
				case 21://DOWN1:
					return DOWN2;
				case 22://DOWN2:
					return DOWN3;
				case 23://DOWN3:
					return DOWN0;
			}
		}
		public ITurnable ClockZ() => ClockZ(Value);
		public static CubeRotation ClockZ(byte value)
		{
			switch (value)
			{
				default:
				case 0://SOUTH0:
					return EAST0;
				case 1://SOUTH1:
					return DOWN3;
				case 2://SOUTH2:
					return WEST2;
				case 3://SOUTH3:
					return UP1;
				case 4://WEST0:
					return SOUTH0;
				case 5://WEST1:
					return DOWN2;
				case 6://WEST2:
					return NORTH2;
				case 7://WEST3:
					return UP2;
				case 8://NORTH0:
					return WEST0;
				case 9://NORTH1:
					return DOWN1;
				case 10://NORTH2:
					return EAST2;
				case 11://NORTH3:
					return UP3;
				case 12://EAST0:
					return NORTH0;
				case 13://EAST1:
					return DOWN0;
				case 14://EAST2:
					return SOUTH2;
				case 15://EAST3:
					return UP0;
				case 16://UP0:
					return WEST1;
				case 17://UP1:
					return NORTH1;
				case 18://UP2:
					return EAST1;
				case 19://UP3:
					return SOUTH1;
				case 20://DOWN0:
					return WEST3;
				case 21://DOWN1:
					return SOUTH3;
				case 22://DOWN2:
					return EAST3;
				case 23://DOWN3:
					return NORTH3;
			}
		}
		public ITurnable Reset() => SOUTH0;
		#endregion ITurntable
		#region Rotate
		/// <param name="index">index 0 for x, 1 for y, 2 for z</param>
		/// <returns>if selected rotation is negative, return -1, otherwise return 1</returns>
		public int Step(int index) => Rotation[FlipBits(index)] >> 31 | 1;
		public int StepX => Step(0);
		public int StepY => Step(1);
		public int StepZ => Step(2);
		/// <summary>
		/// Flips the bits in rot if rot is negative, leaves it alone if positive. Where x, y, and z correspond to elements 0, 1, and 2 in a rotation array, if those axes are reversed we use 0, or -1, for x, and likewise -2 and -3 for y and z when reversed.
		/// </summary>
		public static int FlipBits(int rot) => rot ^ rot >> 31; // (rot ^ rot >> 31) is roughly equal to (rot < 0 ? -1 - rot : rot)
		public int Affected(int axis) => FlipBits(Rotation[axis]);
		public static CubeRotation Get(params int[] rotation) =>
			Values.FirstOrDefault(value =>
				value.Rotation[0] == rotation[0]
				&& value.Rotation[1] == rotation[1]
				&& value.Rotation[2] == rotation[2]);
		/// <summary>
		/// Does a reverse lookup on the rotation array for the axis affected by the rotation
		/// </summary>
		/// <param name="axis">axis 0 or -1 for x, 1 or -2 for y, 2 or -3 for z</param>
		/// <returns>Which axis the specified axis was before the rotation. 0 for x, 1 for y, 2 for z.</returns>
		public int ReverseLookup(int axis)
		{
			int index = FlipBits(axis);
			for (int rot = 0; rot < 3; rot++)
				if (index == Affected(rot))
					return rot;
			throw new ArgumentException("Invalid axis: \"" + axis + "\".");
		}
		public int Rotate(int axis, params int[] coordinates) => coordinates[Affected(axis)] * Step(axis);
		public void Rotate(out int x, out int y, out int z, params int[] coordinates)
		{
			x = Rotate(0, coordinates);
			y = Rotate(1, coordinates);
			z = Rotate(2, coordinates);
		}
		#endregion Rotate
	}
}
