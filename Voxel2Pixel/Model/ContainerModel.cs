using System.Collections;
using System.Collections.Generic;
using Voxel2Pixel.Interfaces;

namespace Voxel2Pixel.Model
{
	public abstract class ContainerModel : IModel
	{
		public virtual IModel Model { get; set; }
		#region IModel
		IEnumerator<Voxel> IEnumerable<Voxel>.GetEnumerator() => (IEnumerator<Voxel>)GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => (IEnumerator)GetEnumerator();
		public virtual IEnumerable<Voxel> GetEnumerator() => Model;
		public virtual byte this[ushort x, ushort y, ushort z] => Model[x, y, z];
		public virtual ushort SizeX => Model.SizeX;
		public virtual ushort SizeY => Model.SizeY;
		public virtual ushort SizeZ => Model.SizeZ;
		#endregion IModel
	}
}
