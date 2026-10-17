using Godot;

namespace BenVoxel.VR;

/// <summary>
/// A prop showing an imported 3D model (see assets/ASSETS.md for where each came from). Whatever the model's
/// own size, units and orientation, it is measured when loaded, turned so its longest dimension points up, scaled to
/// <c>longestMetres</c> and centred on this node, so it is grabbed and scaled like any other prop. It can spin slowly
/// about the vertical so it can be seen from all sides.
/// </summary>
public partial class ModelProp : Prop
{
	private readonly Node3D _spinner = new() { Name = "Spinner" };
	private readonly float _spinRadiansPerSecond;
	private readonly float _radius;

	public override (Vector3 Centre, float Radius) LocalSphere => (Vector3.Zero, _radius);

	public ModelProp(string scenePath, float longestMetres, float spinDegreesPerSecond = 0f)
	{
		Name = System.IO.Path.GetFileNameWithoutExtension(scenePath);
		_spinRadiansPerSecond = Mathf.DegToRad(spinDegreesPerSecond);
		_radius = longestMetres * 0.6f;
		Node3D model = GD.Load<PackedScene>(scenePath).Instantiate<Node3D>();
		Aabb bounds = MeshBounds(model, Transform3D.Identity) ?? new Aabb(Vector3.Zero, Vector3.One);
		Vector3 size = bounds.Size;
		// Turn the longest axis to point up
		Basis upright = size.X >= size.Y && size.X >= size.Z ? new Basis(Vector3.Back, Mathf.Pi / 2f)
			: size.Z >= size.Y && size.Z >= size.X ? new Basis(Vector3.Right, -Mathf.Pi / 2f)
			: Basis.Identity;
		float scale = longestMetres / Mathf.Max(size.X, Mathf.Max(size.Y, size.Z));
		Node3D aligned = new()
		{
			Name = "Aligned",
			Basis = upright.Scaled(Vector3.One * scale),
		};
		model.Position = -bounds.GetCenter();
		aligned.AddChild(model);
		_spinner.AddChild(aligned);
		AddChild(_spinner);
		// Nothing in the scene is lit but the voxels, whose shader does its own lighting, so show textures as they are
		foreach (Node child in model.FindChildren("*", nameof(MeshInstance3D), true, false))
		{
			MeshInstance3D mesh = (MeshInstance3D)child;
			for (int i = 0; i < mesh.GetSurfaceOverrideMaterialCount(); i++)
			{
				if (mesh.GetActiveMaterial(i) is BaseMaterial3D source)
				{
					BaseMaterial3D material = (BaseMaterial3D)source.Duplicate();
					material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
					// Anisotropic, so steeply angled surfaces like the pencil's tip stay sharp instead of smearing colours across edges
					material.TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic;
					mesh.SetSurfaceOverrideMaterial(i, material);
				}
			}
		}
	}

	public override void _Process(double delta) => _spinner.RotateY(_spinRadiansPerSecond * (float)delta);

	/// <summary>The bounds of every mesh under a node, in the space of the node's parent chain given by <paramref name="toRoot"/>.</summary>
	private static Aabb? MeshBounds(Node3D node, Transform3D toRoot)
	{
		Transform3D transform = toRoot * node.Transform;
		Aabb? result = null;
		if (node is MeshInstance3D mesh && mesh.Mesh is not null)
			result = transform * mesh.GetAabb();
		foreach (Node child in node.GetChildren())
		{
			if (child is Node3D childNode && MeshBounds(childNode, transform) is Aabb childBounds)
				result = result is Aabb existing ? existing.Merge(childBounds) : childBounds;
		}
		return result;
	}
}
