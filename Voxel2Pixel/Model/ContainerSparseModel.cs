using System.Collections.Generic;
using Voxel2Pixel.Interfaces;

namespace Voxel2Pixel.Model
{
	public abstract class ContainerSparseModel : ContainerModel, ISparseModel
	{
		#region ContainerModel
		public override IModel Model => SparseModel;
		public virtual ISparseModel SparseModel { get; set; }
		#endregion ContainerModel
		#region ISparseModel
		public virtual IEnumerable<Voxel> Voxels => SparseModel.Voxels;
		#endregion ISparseModel
	}
}
