using System.Collections.Generic;
using Voxel2Pixel.Interfaces;

namespace Voxel2Pixel.Model
{
	public class DictionaryModel : IModel
	{
		#region DictionaryModel
		private Dictionary<ulong, byte> Dictionary = new Dictionary<ulong, byte>();
		public void Clear() => Dictionary.Clear();
		public static ulong Encode(ushort x, ushort y, ushort z) => ((ulong)z << 32) | ((ulong)y << 16) | x;
		public static void Decode(ulong @ulong, out ushort x, out ushort y, out ushort z)
		{
			x = (ushort)@ulong;
			y = (ushort)(@ulong >> 16);
			z = (ushort)(@ulong >> 32);
		}
		public DictionaryModel(IModel model) : this(model.Voxels, model.SizeX, model.SizeY, model.SizeZ) { }
		public DictionaryModel(IEnumerable<Voxel> voxels, params ushort[] size) : this(size)
		{
			foreach (Voxel voxel in voxels)
				this[voxel.X, voxel.Y, voxel.Z] = voxel.@byte;
		}
		public DictionaryModel(params ushort[] size)
		{
			SizeX = size[0];
			SizeY = size[1];
			SizeZ = size[2];
		}
		#endregion DictionaryModel
		#region IModel
		public IEnumerable<Voxel> Voxels
		{
			get
			{
				foreach (KeyValuePair<ulong, byte> voxel in Dictionary)
				{
					Decode(voxel.Key, out ushort x, out ushort y, out ushort z);
					yield return new Voxel
					{
						X = x,
						Y = y,
						Z = z,
						@byte = voxel.Value,
					};
				}
			}
		}
		#endregion IModel
		#region IModel
		public ushort SizeX { get; set; }
		public ushort SizeY { get; set; }
		public ushort SizeZ { get; set; }
		public byte this[ushort x, ushort y, ushort z]
		{
			get => Dictionary.TryGetValue(Encode(x, y, z), out byte @byte) ? @byte : (byte)0;
			set
			{
				if (value == 0)
					Dictionary.Remove(Encode(x, y, z));
				else if (!IsOutside(x, y, z))
					Dictionary[Encode(x, y, z)] = value;
			}
		}
		public bool IsOutside(ushort x, ushort y, ushort z) => x >= SizeX || y >= SizeY || z >= SizeZ;
		#endregion IModel
	}
}
