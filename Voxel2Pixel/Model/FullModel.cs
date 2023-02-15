namespace Voxel2Pixel.Model
{
	public class FullModel : EmptyModel
	{
		public override byte? At(int x, int y, int z) => 1;
	}
}
