using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BenVoxel;

namespace Voxel2Pixel.Model;

public class EmptyModel : IModel
{
	#region IModel
	public virtual ushort SizeX { get; set; }
	public virtual ushort SizeY { get; set; }
	public virtual ushort SizeZ { get; set; }
	public virtual byte this[ushort x, ushort y, ushort z] => 0;
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	public virtual IEnumerator<Voxel> GetEnumerator() => Enumerable.Empty<Voxel>().GetEnumerator();
	#endregion IModel
}
