using BenVoxel.Structs;

namespace BenVoxel.Interfaces;

public interface ITurnable
{
	ITurnable Turn(params Turn[] turns);
}
