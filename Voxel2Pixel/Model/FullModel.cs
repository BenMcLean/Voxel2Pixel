using System.Collections.Generic;

namespace Voxel2Pixel.Model
{
	public class FullModel : EmptyModel
	{
		public byte Voxel { get; set; } = 1;
		#region IModel
		public override byte this[ushort x, ushort y, ushort z] => Voxel;
		public override IEnumerable<Voxel> Voxels
		{
			get
			{
				for (ushort x = 0; x < SizeX; x++)
					for (ushort y = 0; y < SizeY; y++)
						for (ushort z = 0; z < SizeZ; z++)
							yield return new Voxel(x, y, z, Voxel);
			}
		}
		#endregion IModel
	}
}
