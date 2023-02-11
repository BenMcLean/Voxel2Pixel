﻿using System.Collections.ObjectModel;
using System;
using System.Linq;

namespace Voxel2Pixel.Model
{
	/// <summary>
	/// There are only 24 possible orientations achievable by 90 degree rotations around coordinate axis, which form the rotation group of a cube, also known as the chiral octahedral symmetry group.
	/// http://www.ams.org/samplings/feature-column/fcarc-cubes7
	/// https://en.wikipedia.org/wiki/Octahedral_symmetry#Chiral_octahedral_symmetry
	/// </summary>
	public class CubeRotation : ITurnable
	{
		#region Instances
		public static readonly CubeRotation
			SOUTH0 = new CubeRotation(0, "SOUTH0", -1, 1, 2),
			SOUTH1 = new CubeRotation(1, "SOUTH1", -1, 2, -2),
			SOUTH2 = new CubeRotation(2, "SOUTH2", -1, -2, -3),
			SOUTH3 = new CubeRotation(3, "SOUTH3", -1, -3, 1),
			WEST0 = new CubeRotation(4, "WEST0", -2, -1, 2),
			WEST1 = new CubeRotation(5, "WEST1", -2, 2, 0),
			WEST2 = new CubeRotation(6, "WEST2", -2, 0, -3),
			WEST3 = new CubeRotation(7, "WEST3", -2, -3, -1),
			NORTH0 = new CubeRotation(8, "NORTH0", 0, -2, 2),
			NORTH1 = new CubeRotation(9, "NORTH1", 0, 2, 1),
			NORTH2 = new CubeRotation(10, "NORTH2", 0, 1, -3),
			NORTH3 = new CubeRotation(11, "NORTH3", 0, -3, -2),
			EAST0 = new CubeRotation(12, "EAST0", 1, 0, 2),
			EAST1 = new CubeRotation(13, "EAST1", 1, 2, -1),
			EAST2 = new CubeRotation(14, "EAST2", 1, -1, -3),
			EAST3 = new CubeRotation(15, "EAST3", 1, -3, 0),
			UP0 = new CubeRotation(16, "UP0", -3, -2, 0),
			UP1 = new CubeRotation(17, "UP1", -3, 0, 1),
			UP2 = new CubeRotation(18, "UP2", -3, 1, -1),
			UP3 = new CubeRotation(19, "UP3", -3, -1, -2),
			DOWN0 = new CubeRotation(20, "DOWN0", 2, -2, -1),
			DOWN1 = new CubeRotation(21, "DOWN1", 2, -1, 1),
			DOWN2 = new CubeRotation(22, "DOWN2", 2, 1, 0),
			DOWN3 = new CubeRotation(23, "DOWN3", 2, 0, -2);
		public static readonly ReadOnlyCollection<CubeRotation> Values = Array.AsReadOnly(new CubeRotation[] { SOUTH0, SOUTH1, SOUTH2, SOUTH3, WEST0, WEST1, WEST2, WEST3, NORTH0, NORTH1, NORTH2, NORTH3, EAST0, EAST1, EAST2, EAST3, UP0, UP1, UP2, UP3, DOWN0, DOWN1, DOWN2, DOWN3 });
		#endregion Instances
		#region Data members
		public readonly int Value;
		public readonly string Name;
		public readonly ReadOnlyCollection<int> Rotation;
		#endregion Data members
		private CubeRotation(int value, string name, params int[] rotation)
		{
			Value = value;
			Name = name;
			Rotation = Array.AsReadOnly(rotation);
		}
		public override string ToString() => Name;
		public bool South() => South(this);
		public static bool South(CubeRotation turner) => turner == SOUTH0 || turner == SOUTH1 || turner == SOUTH2 || turner == SOUTH3;
		public bool West() => West(this);
		public static bool West(CubeRotation turner) => turner == WEST0 || turner == WEST1 || turner == WEST2 || turner == WEST3;
		public bool North() => North(this);
		public static bool North(CubeRotation turner) => turner == NORTH0 || turner == NORTH1 || turner == NORTH2 || turner == NORTH3;
		public bool East() => East(this);
		public static bool East(CubeRotation turner) => turner == EAST0 || turner == EAST1 || turner == EAST2 || turner == EAST3;
		public bool Up() => Up(this);
		public static bool Up(CubeRotation turner) => turner == UP0 || turner == UP1 || turner == UP2 || turner == UP3;
		public bool Down() => Down(this);
		public static bool Down(CubeRotation turner) => turner == DOWN0 || turner == DOWN1 || turner == DOWN2 || turner == DOWN3;
		#region ITurnable
		public ITurnable CounterX() => CounterX(Value);
		public static CubeRotation CounterX(int value)
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
		public ITurnable CounterY() => CounterY(Value);
		public static CubeRotation CounterY(int value)
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
		public ITurnable CounterZ() => CounterZ(Value);
		public static CubeRotation CounterZ(int value)
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
		public static CubeRotation ClockX(int value)
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
		public ITurnable ClockY() => ClockY(Value);
		public static CubeRotation ClockY(int value)
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
		public ITurnable ClockZ() => ClockZ(Value);
		public static CubeRotation ClockZ(int value)
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
		/// <param name="index">index 0 for x, 1 for y, 2 for z</param>
		/// <returns>if selected rotation is negative, return -1, otherwise return 1</returns>
		public int Step(int index) => Rotation[index] >> 31 | 1;
		public int StepX => Step(0);
		public int StepY => Step(1);
		public int StepZ => Step(2);
		/// <summary>
		/// Flips the bits in rot if rot is negative, leaves it alone if positive. Where x, y, and z correspond to elements 0, 1, and 2 in a rotation array, if those axes are reversed we use 0, or -1, for x, and likewise -2 and -3 for y and z when reversed.
		/// </summary>
		public static int FlipBits(int rot) => rot ^ rot >> 31; // (rot ^ rot >> 31) is roughly equal to (rot < 0 ? -1 - rot : rot)
		public int Affected(int axis) => FlipBits(Rotation[axis]);
		public static CubeRotation Get(params int[] rotation) => Values
			.Where(value => value.Rotation[0] == rotation[0]
				&& value.Rotation[1] == rotation[1]
				&& value.Rotation[2] == rotation[2])
			.FirstOrDefault()
			?? throw new ArgumentException("Rotation array " + string.Join(", ", rotation) + " does not correspond to a rotation.");
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
		public int Rotate(int axis, params int[] coordinates)
		{
			int index = ReverseLookup(axis);
			return coordinates[index] * Step(index);
		}
		public void Rotate(out int x, out int y, out int z, params int[] coordinates)
		{
			x = Rotate(0, coordinates);
			y = Rotate(1, coordinates);
			z = Rotate(2, coordinates);
		}
	}
}