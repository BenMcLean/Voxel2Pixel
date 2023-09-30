namespace Voxel2Pixel.Model
{
	/// <summary>
	/// Adds one voxel on top of another model to mark a coordinate
	/// </summary>
	public class MarkerModel : ContainerModel
	{
		#region Data members
		public byte Voxel { get; set; } = 1;
		public bool Overwrite { get; set; } = true;
		public int X { get; set; } = 0;
		public int Y { get; set; } = 0;
		public int Z { get; set; } = 0;
		#endregion Data members
		#region IModel
		public override byte? At(int x, int y, int z) =>
			x == X && y == Y && z == Z ?
				Overwrite ?
					Voxel
					: Model.At(x, y, z) is byte @byte && @byte != 0 ?
						@byte
						: Voxel
				: Model.At(x, y, z);
		#endregion IModel
	}
}
