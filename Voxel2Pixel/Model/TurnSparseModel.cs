using System.Collections.Generic;
using Voxel2Pixel.Interfaces;

namespace Voxel2Pixel.Model
{
	public class TurnSparseModel : TurnModel, ISparseModel
	{
		#region ContainerModel
		public override IModel Model => SparseModel;
		public virtual ISparseModel SparseModel { get; set; }
		#endregion ContainerModel
		#region ISparseModel
		public virtual IEnumerable<Voxel> Voxels
		{
			get
			{
				foreach (Voxel voxel in SparseModel.Voxels)
				{
					Rotate(out int x, out int y, out int z, voxel.X, voxel.Y, voxel.Z);
					yield return new Voxel
					{
						X = (ushort)x,
						Y = (ushort)y,
						Z = (ushort)z,
						@byte = voxel.@byte,
					};
				}
			}
		}
		#endregion ISparseModel
	}
}
