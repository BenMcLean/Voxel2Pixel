using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BenVoxel.Interfaces;
using BenVoxel.Structs;

namespace BenVoxel.Models;

public class DictionaryModel : IEditableBrickModel
{
	#region DictionaryModel
	private readonly Dictionary<ulong, ulong> Dictionary = [];
	public void Clear() => Dictionary.Clear();
	public static ulong Encode(ushort x, ushort y, ushort z) => (ulong)z << 32 | (uint)y << 16 | x;
	public static ulong EncodeBrick(ushort x, ushort y, ushort z) => Encode((ushort)(x & ~1), (ushort)(y & ~1), (ushort)(z & ~1));
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
	#region IModel
	public ushort SizeX { get; set; } = 0;
	public ushort SizeY { get; set; } = 0;
	public ushort SizeZ { get; set; } = 0;
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	IEnumerator<Voxel> IEnumerable<Voxel>.GetEnumerator()
	{
		foreach (KeyValuePair<ulong, ulong> brick in Dictionary)
		{
			ushort bx = (ushort)brick.Key,
				by = (ushort)(brick.Key >> 16),
				bz = (ushort)(brick.Key >> 32);
			ulong payload = brick.Value;
			for (int lz = 0; lz < 2; lz++)
				for (int ly = 0; ly < 2; ly++)
					for (int lx = 0; lx < 2; lx++)
					{
						byte material = VoxelBrick.GetVoxel(payload, lx, ly, lz);
						if (material != 0)
							yield return new Voxel(
								X: (ushort)(bx + lx),
								Y: (ushort)(by + ly),
								Z: (ushort)(bz + lz),
								Material: material);
					}
		}
	}
	#endregion IModel
	#region IBrickModel
	public ulong GetBrick(ushort x, ushort y, ushort z)
	{
		ulong key = EncodeBrick(x, y, z);
		return Dictionary.TryGetValue(key, out ulong payload) ? payload : 0UL;
	}
	public IEnumerator<VoxelBrick> GetEnumerator() => Dictionary
		.Select(brick => new VoxelBrick(
			X: (ushort)brick.Key,
			Y: (ushort)(brick.Key >> 16),
			Z: (ushort)(brick.Key >> 32),
			Payload: brick.Value))
		.GetEnumerator();
	#endregion IBrickModel
	#region IEditableModel
	public byte this[ushort x, ushort y, ushort z]
	{
		get => VoxelBrick.GetVoxel(GetBrick(x, y, z), x & 1, y & 1, z & 1);
		set
		{
			if (this.IsOutside(x, y, z))
				throw new IndexOutOfRangeException("[" + string.Join(", ", x, y, z) + "] is outside [" + string.Join(", ", SizeX, SizeY, SizeZ) + "]");
			ulong key = EncodeBrick(x, y, z),
				brick = Dictionary.TryGetValue(key, out ulong b) ? b : 0UL;
			// Update the specific voxel within the brick
			int localX = x & 1, localY = y & 1, localZ = z & 1,
				shift = ((localZ << 2) | (localY << 1) | localX) << 3;
			ulong mask = 0xFFUL << shift,
				newBrick = (brick & ~mask) | ((ulong)value << shift);
			// Store or remove the brick
			if (newBrick == 0)
				Dictionary.Remove(key);
			else
				Dictionary[key] = newBrick;
		}
	}
	#endregion IEditableModel
	#region IEditableBrickModel
	public void SetBrick(ushort x, ushort y, ushort z, ulong payload)
	{
		ulong key = EncodeBrick(x, y, z);
		if (payload == 0)
			Dictionary.Remove(key);
		else
			Dictionary[key] = payload;
	}
	#endregion IEditableBrickModel
}
