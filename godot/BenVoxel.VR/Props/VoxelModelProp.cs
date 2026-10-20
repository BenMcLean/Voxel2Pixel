using Godot;

namespace BenVoxel.VR.Props;

/// <summary>
/// The model being edited. <see cref="Root"/> adds the raymarched bounding box and the edit cursor as its children.
/// </summary>
public partial class VoxelModelProp : VoxelProp
{
	public VoxelModelProp()
	{
		Name = "ModelPivot";
	}

	/// <summary>Below other voxel props, such as the sprite preview of this model, which win where they overlap it.</summary>
	public override int Priority => -1;
}
