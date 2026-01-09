using System.Collections;
using System.Collections.Generic;
using BenVoxel.Interfaces;
using BenVoxel.Structs;

namespace BenVoxel.Models;

public class ArrayModel(byte[][][] voxels) : IEditableModel, ITurnable
{
	public ArrayModel(ArrayModel other) : this(other.Array.DeepCopy()) { }
	public ArrayModel(ushort sizeX = 1, ushort sizeY = 1, ushort sizeZ = 1) : this(Array3D.Initialize<byte>(sizeX, sizeY, sizeZ)) { }
	public ArrayModel(IModel model) : this(model.SizeX, model.SizeY, model.SizeZ)
	{
		foreach (Voxel voxel in model)
			Array[voxel.X][voxel.Y][voxel.Z] = voxel.Material;
	}
	public byte[][][] Array { get; set; } = voxels;
	#region IEditableModel
	public ushort SizeX => (ushort)Array.Length;
	public ushort SizeY => (ushort)Array[0].Length;
	public ushort SizeZ => (ushort)Array[0][0].Length;
	public byte this[ushort x, ushort y, ushort z]
	{
		get => !this.IsOutside(x, y, z) ? Array[x][y][z] : (byte)0;
		set => Array[x][y][z] = value;
	}
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	public IEnumerator<Voxel> GetEnumerator()
	{
		for (ushort x = 0; x < SizeX; x++)
			for (ushort y = 0; y < SizeY; y++)
				for (ushort z = 0; z < SizeZ; z++)
					if (Array[x][y][z] is byte index && index != 0)
						yield return new Voxel(x, y, z, index);
	}
	#endregion IEditableModel
	#region ITurnable
	public ITurnable Turn(params Turn[] turns)
	{
		Array = Array.Turn(turns);
		return this;
	}
	#endregion ITurnable
}
