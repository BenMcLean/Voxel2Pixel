namespace Voxel2Pixel.Interfaces
{
	public interface ISprite
	{
		byte[] Texture { get; }
		ushort Width { get; }
		ushort Height { get; }
		ushort OriginX { get; }
		ushort OriginY { get; }
	}
}
