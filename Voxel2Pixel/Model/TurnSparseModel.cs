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
					ReverseRotate(out ushort x, out ushort y, out ushort z, voxel.X, voxel.Y, voxel.Z);
					yield return new Voxel
					{
						X = x,
						Y = y,
						Z = z,
						@byte = voxel.@byte,
					};
				}
			}
		}
		#endregion ISparseModel
	}
}
