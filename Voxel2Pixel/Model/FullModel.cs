namespace Voxel2Pixel.Model
{
	public class FullModel : EmptyModel
	{
		public byte Voxel { get; set; } = 1;
		public override byte? At(int x, int y, int z) => Voxel;
	}
}
