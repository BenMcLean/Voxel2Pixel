using System.Threading.Tasks;
using BenProgress;
using BenVoxel.Interfaces;
using BenVoxel.Structs;

namespace Voxel2Pixel.Interfaces;

/// <summary>
/// At compile time, extension methods always have lower priority than instance methods defined in the type itself, so this interface essentially allows "overriding" the VoxelDraw.Draw extension method.
/// </summary>
public interface ISpecializedModel : IModel
{
	Task DrawAsync(IRenderer renderer, Perspective perspective, byte scaleX = 1, byte scaleY = 1, byte scaleZ = 1, double radians = 0d, ProgressContext? progressContext = null);
}
