﻿using System.Collections.Generic;
using System.Linq;
using Voxel2Pixel.Interfaces;

namespace Voxel2Pixel.Model
{
	public class DictionaryModel : IModel
	{
		#region DictionaryModel
		private readonly Dictionary<ulong, byte> Dictionary = new Dictionary<ulong, byte>();
		public void Clear() => Dictionary.Clear();
		public static ulong Encode(ushort x, ushort y, ushort z) => ((ulong)z << 32) | ((uint)y << 16) | x;
		public static void Decode(ulong @ulong, out ushort x, out ushort y, out ushort z)
		{
			x = (ushort)@ulong;
			y = (ushort)(@ulong >> 16);
			z = (ushort)(@ulong >> 32);
		}
		public DictionaryModel() { }
		public DictionaryModel(IModel model) : this(model.Voxels, model.SizeX, model.SizeY, model.SizeZ) { }
		public DictionaryModel(IEnumerable<Voxel> voxels, params ushort[] size)
		{
			foreach (Voxel voxel in voxels)
				this[voxel.X, voxel.Y, voxel.Z] = voxel.@byte;
			if (!(size is null) && size.Length > 0)
			{
				SizeX = size[0];
				if (size.Length > 1)
				{
					SizeY = size[1];
					if (size.Length > 2)
						SizeZ = size[2];
				}
			}
		}
		#endregion DictionaryModel
		#region IModel
		public IEnumerable<Voxel> Voxels => Dictionary
			.Select(voxel => new Voxel
			{
				X = (ushort)voxel.Key,
				Y = (ushort)(voxel.Key >> 16),
				Z = (ushort)(voxel.Key >> 32),
				@byte = voxel.Value,
			});
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
				else
				{
					Dictionary[Encode(x, y, z)] = value;
					if (x > SizeX)
						SizeX = x;
					if (y > SizeY)
						SizeY = y;
					if (z > SizeZ)
						SizeZ = z;
				}
			}
		}
		#endregion IModel
	}
}
