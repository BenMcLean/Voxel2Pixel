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
		public const int
			xPlus = 0,
			yPlus = 1,
			zPlus = 2,
			xMinus = -1,
			yMinus = -2,
			zMinus = -3;
		public static readonly CubeRotation
			SOUTH0 = new CubeRotation(0, "SOUTH0", xPlus, yPlus, zPlus, 0, 1, 2),
			SOUTH1 = new CubeRotation(1, "SOUTH1", zMinus, yPlus, xPlus, 2, 1, 0),
			SOUTH2 = new CubeRotation(2, "SOUTH2", xMinus, yPlus, zMinus, 0, 1, 2),
			SOUTH3 = new CubeRotation(3, "SOUTH3", zPlus, yPlus, xMinus, 2, 1, 0),
			WEST0 = new CubeRotation(4, "WEST0", yPlus, xMinus, zPlus, 1, 0, 2),
			WEST1 = new CubeRotation(5, "WEST1", yPlus, zPlus, xPlus, 2, 0, 1),
			WEST2 = new CubeRotation(6, "WEST2", yPlus, xPlus, zMinus, 1, 0, 2),
			WEST3 = new CubeRotation(7, "WEST3", yPlus, zMinus, xMinus, 2, 0, 1),
			NORTH0 = new CubeRotation(8, "NORTH0", xMinus, yMinus, zPlus, 0, 1, 2),
			NORTH1 = new CubeRotation(9, "NORTH1", zPlus, yMinus, xPlus, 2, 1, 0),
			NORTH2 = new CubeRotation(10, "NORTH2", xPlus, yMinus, zMinus, 0, 1, 2),
			NORTH3 = new CubeRotation(11, "NORTH3", zMinus, yMinus, xMinus, 2, 1, 0),
			EAST0 = new CubeRotation(12, "EAST0", yMinus, xPlus, zPlus, 1, 0, 2),
			EAST1 = new CubeRotation(13, "EAST1", yMinus, zMinus, xPlus, 2, 0, 1),
			EAST2 = new CubeRotation(14, "EAST2", yMinus, xMinus, zMinus, 1, 0, 2),
			EAST3 = new CubeRotation(15, "EAST3", yMinus, zPlus, xMinus, 2, 0, 1),
			UP0 = new CubeRotation(16, "UP0", xPlus, zPlus, yMinus, 0, 2, 1),
			UP1 = new CubeRotation(17, "UP1", zMinus, xPlus, yMinus, 1, 2, 0),
			UP2 = new CubeRotation(18, "UP2", xMinus, zMinus, yMinus, 0, 2, 1),
			UP3 = new CubeRotation(19, "UP3", zPlus, xMinus, yMinus, 1, 2, 0),
			DOWN0 = new CubeRotation(20, "DOWN0", xPlus, zMinus, yPlus, 0, 2, 1),
			DOWN1 = new CubeRotation(21, "DOWN1", zMinus, xMinus, yPlus, 1, 2, 0),
			DOWN2 = new CubeRotation(22, "DOWN2", xMinus, zPlus, yPlus, 0, 2, 1),
			DOWN3 = new CubeRotation(23, "DOWN3", zPlus, xPlus, yPlus, 1, 2, 0);
		public static readonly ReadOnlyCollection<CubeRotation> Values = Array.AsReadOnly(new CubeRotation[] { SOUTH0, SOUTH1, SOUTH2, SOUTH3, WEST0, WEST1, WEST2, WEST3, NORTH0, NORTH1, NORTH2, NORTH3, EAST0, EAST1, EAST2, EAST3, UP0, UP1, UP2, UP3, DOWN0, DOWN1, DOWN2, DOWN3 });
		#endregion Instances
		#region Data members
		public readonly byte Value;
		public readonly string Name;
		public readonly ReadOnlyCollection<int> Rotation;
		public readonly ReadOnlyCollection<byte> Size;
		#endregion Data members
		#region CubeRotation
		private CubeRotation(byte value, string name, int rot0, int rot1, int rot2, params byte[] size)
		{
			Value = value;
			Name = name;
			Rotation = Array.AsReadOnly(new int[] { rot0, rot1, rot2 });
			Size = Array.AsReadOnly(size);
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
		public static readonly ReadOnlyCollection<ReadOnlyCollection<byte>> Clock = Array.AsReadOnly(new ReadOnlyCollection<byte>[] {
				Array.AsReadOnly(new byte[] { 16, 5, 22, 15, 19, 9, 23, 3, 18, 13, 20, 7, 17, 1, 21, 11, 10, 6, 2, 14, 0, 4, 8, 12 }),//x axis
				Array.AsReadOnly(new byte[] { 1, 2, 3, 0, 5, 6, 7, 4, 9, 10, 11, 8, 13, 14, 15, 12, 17, 18, 19, 16, 21, 22, 23, 20 }),//y axis
				Array.AsReadOnly(new byte[] { 4, 21, 14, 19, 8, 22, 2, 18, 12, 23, 6, 17, 0, 20, 10, 16, 5, 1, 13, 9, 7, 11, 15, 3 })}),//z axis
			Counter = Array.AsReadOnly(Clock.Select(a => Array.AsReadOnly(a.Select(e => (byte)a.IndexOf(e)).ToArray())).ToArray());
		public ITurnable CounterX() => Values[Counter[0][Value]];
		public ITurnable CounterY() => Values[Counter[1][Value]];
		public ITurnable CounterZ() => Values[Counter[2][Value]];
		public ITurnable ClockX() => Values[Clock[0][Value]];
		public ITurnable ClockY() => Values[Clock[1][Value]];
		public ITurnable ClockZ() => Values[Clock[2][Value]];
		public ITurnable Reset() => SOUTH0;
		#endregion ITurnable
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
		public int AffectedX => Affected(0);
		public int AffectedY => Affected(1);
		public int AffectedZ => Affected(2);
		public static CubeRotation Get(params int[] rotation) =>
			Values.FirstOrDefault(value =>
				value.Rotation[0] == rotation[0]
				&& value.Rotation[1] == rotation[1]
				&& value.Rotation[2] == rotation[2]);
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
