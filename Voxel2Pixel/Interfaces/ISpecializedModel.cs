using BenVoxel;
using Voxel2Pixel.Model;

namespace Voxel2Pixel.Interfaces;

/// <summary>
/// At compile time, extension methods always have lower priority than instance methods defined in the type itself, so this interface essentially allows "overriding" the VoxelDraw.Draw extension method.
/// </summary>
public interface ISpecializedModel : IModel
{
	void Draw(IRenderer renderer, Perspective perspective, byte scaleX = 1, byte scaleY = 1, byte scaleZ = 1, double radians = 0d);
}
