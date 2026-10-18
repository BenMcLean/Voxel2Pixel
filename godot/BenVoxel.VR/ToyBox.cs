using Godot;

namespace BenVoxel.VR;

/// <summary>
/// The toy box: a treasure chest (assets/chest/chest.glb, see assets/ASSETS.md). The node is the chest's pivot, at the
/// centre of its base, in metres. The model is a child scaled to
/// about <see cref="WidthMetres"/> wide.
/// </summary>
public partial class ToyBox : Prop
{
	/// <summary>Chest width in metres.</summary>
	private const float WidthMetres = 0.14f;
	/// <summary>The model's width in its own units, and how far its base is below the model origin (both measured from the glb).</summary>
	private const float ModelWidth = 1.6446f * 1.7780f;
	private const float ModelBaseBelowOrigin = 0.4412f * 1.7780f;

	/// <summary>
	/// How far the model is turned about the vertical (counter-clockwise seen from above) so that its front faces this node's +Z,
	/// which is what <c>Root.SummonToyBox</c> relies on. The glb's own front faces a different way.
	/// </summary>
	private static readonly float FrontTurn = Mathf.DegToRad(90f);

	public override (Vector3 Centre, float Radius) LocalSphere => (new Vector3(0f, WidthMetres * 0.25f, 0f), WidthMetres * 0.7f);

	public ToyBox()
	{
		Name = "ToyBox";
		float scale = WidthMetres / ModelWidth;
		Node3D model = GD.Load<PackedScene>("res://assets/chest/chest.glb").Instantiate<Node3D>();
		model.Scale = Vector3.One * scale;
		model.Position = new Vector3(0f, ModelBaseBelowOrigin * scale, 0f);
		model.Rotation = new Vector3(0f, FrontTurn, 0f);
		AddChild(model);
		// Nothing in the scene is lit but the voxels, whose shader does its own lighting, so show the pixel-art texture as is
		foreach (Node child in model.FindChildren("*", nameof(MeshInstance3D), true, false))
		{
			MeshInstance3D mesh = (MeshInstance3D)child;
			if (mesh.GetActiveMaterial(0) is BaseMaterial3D source)
			{
				BaseMaterial3D material = (BaseMaterial3D)source.Duplicate();
				material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
				material.TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest;
				mesh.MaterialOverride = material;
			}
		}
	}
}
