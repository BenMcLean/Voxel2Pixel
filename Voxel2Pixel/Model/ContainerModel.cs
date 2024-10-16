using BenVoxel;
using System.Collections;
using System.Collections.Generic;

namespace Voxel2Pixel.Model;

public abstract class ContainerModel : IModel
{
	public virtual IModel Model { get; set; }
	#region IModel
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	public virtual IEnumerator<Voxel> GetEnumerator() => Model.GetEnumerator();
	public virtual byte this[ushort x, ushort y, ushort z] => Model[x, y, z];
	public virtual ushort SizeX => Model.SizeX;
	public virtual ushort SizeY => Model.SizeY;
	public virtual ushort SizeZ => Model.SizeZ;
	#endregion IModel
}
