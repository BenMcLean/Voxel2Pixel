using BenVoxel.VR.Props;
using Godot;

namespace BenVoxel.VR;

/// <summary>Makes miniature icons out of arbitrary 3D objects; shared by every menu that shows them.</summary>
public static class MiniIcon
{
	/// <summary>
	/// Wraps an object so that it is centred on the wrapper's origin and scaled so the diagonal of its bounds is
	/// <paramref name="diagonal"/> metres, whatever its own size and origin.
	/// </summary>
	public static Node3D Fit(Node3D icon, float diagonal)
	{
		Aabb bounds = ModelProp.MeshBounds(icon, Transform3D.Identity) ?? new Aabb(Vector3.Zero, Vector3.One);
		float scale = diagonal / Mathf.Max(bounds.Size.Length(), 0.0001f);
		Node3D fit = new() { Name = "Fit", Scale = Vector3.One * scale, Position = -bounds.GetCenter() * scale };
		fit.AddChild(icon);
		return fit;
	}

	/// <summary>A small white sphere standing for a light.</summary>
	public static MeshInstance3D LightSphere(float radius) => new()
	{
		Name = "LightIcon",
		Mesh = new SphereMesh { Radius = radius, Height = radius * 2f },
		MaterialOverride = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, AlbedoColor = Colors.White },
		CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
	};
}
