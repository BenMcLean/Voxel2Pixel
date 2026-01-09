using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BenVoxel;

public class DictionaryModel : IEditableModel
{
	#region DictionaryModel
	private readonly Dictionary<ulong, byte> Dictionary = [];
	public void Clear() => Dictionary.Clear();
	public static ulong Encode(ushort x, ushort y, ushort z) => (ulong)z << 32 | (uint)y << 16 | x;
	public static void Decode(ulong @ulong, out ushort x, out ushort y, out ushort z)
	{
		x = (ushort)@ulong;
		y = (ushort)(@ulong >> 16);
		z = (ushort)(@ulong >> 32);
	}
	public DictionaryModel() { }
	public DictionaryModel(IModel model) : this(model, model.SizeX, model.SizeY, model.SizeZ) { }
	public DictionaryModel(IEnumerable<Voxel> voxels, Point3D size) : this(voxels, (ushort)size.X, (ushort)size.Y, (ushort)size.Z) { }
	public DictionaryModel(IEnumerable<Voxel> voxels, params ushort[] size)
	{
		if (size is not null && size.Length > 0)
		{
			SizeX = size[0];
			if (size.Length > 1)
			{
				SizeY = size[1];
				if (size.Length > 2)
					SizeZ = size[2];
			}
		}
		foreach (Voxel voxel in voxels)
			this[voxel.X, voxel.Y, voxel.Z] = voxel.Material;
	}
	#endregion DictionaryModel
	#region IEditableModel
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	public IEnumerator<Voxel> GetEnumerator() => Dictionary
		.Select(voxel => new Voxel(
			X: (ushort)voxel.Key,
			Y: (ushort)(voxel.Key >> 16),
			Z: (ushort)(voxel.Key >> 32),
			Material: voxel.Value))
		.GetEnumerator();
	public ushort SizeX { get; set; } = 0;
	public ushort SizeY { get; set; } = 0;
	public ushort SizeZ { get; set; } = 0;
	public byte this[ushort x, ushort y, ushort z]
	{
		get => Dictionary.TryGetValue(Encode(x, y, z), out byte @byte) ? @byte : (byte)0;
		set
		{
			if (value == 0)
				Dictionary.Remove(Encode(x, y, z));
			else if (this.IsOutside(x, y, z))
				throw new IndexOutOfRangeException("[" + string.Join(", ", x, y, z) + "] is outside [" + string.Join(", ", SizeX, SizeY, SizeZ) + "]");
			else
				Dictionary[Encode(x, y, z)] = value;
		}
	}
	#endregion IEditableModel
}
