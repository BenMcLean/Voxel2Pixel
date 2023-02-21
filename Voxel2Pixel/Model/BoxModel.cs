
namespace Voxel2Pixel.Model
{
	public class BoxModel : ContainerModel
	{
		public byte Voxel { get; set; } = 1;
		public bool Overwrite { get; set; } = true;
		public bool IsBorder(int x, int y, int z)
		{
			int borderCount = 0;
			if (x == 0 || x == SizeX - 1)
				borderCount++;
			if (y == 0 || y == SizeY - 1)
				borderCount++;
			if (z == 0 || z == SizeZ - 1)
				borderCount++;
			return borderCount >= 2;
		}
		#region IModel
		public override byte? At(int x, int y, int z) =>
			Overwrite ?
				IsBorder(x, y, z) ?
					Voxel
					: Model.At(x, y, z)
				: Model.At(x, y, z) is byte voxel && voxel != 0 ?
					voxel
					: IsBorder(x, y, z) ?
						Voxel
						: (byte)0;
		#endregion IModel
	}
}
