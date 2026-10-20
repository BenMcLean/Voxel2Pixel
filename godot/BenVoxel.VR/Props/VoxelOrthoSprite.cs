using Godot;

namespace BenVoxel.VR.Props;

/// <summary>
/// Non-editable ortho-sprite preview of the model (see voxel_ortho_sprite.gdshader). The node's local space is
/// Godot Y-up voxel space, exactly like the model pivot in <see cref="Root"/>: scale it to metres-per-voxel and
/// move it to place the preview in the world. Grabbing it moves and scales the preview.
///
/// The preview is a flat billboard on purpose. Every frame the proxy quad is turned to face the head (the
/// midpoint between the eyes), and the shader's rays and virtual pixel grid come from that quad, so both eyes
/// see the same chunky pixels on a flat card instead of a stereoscopic 3D "big pixel" volume.
/// </summary>
public partial class VoxelOrthoSprite : VoxelProp
{
	/// <summary>Material running voxel_ortho_sprite.gdshader. Root binds the shared voxel textures to it.</summary>
	public ShaderMaterial Material { get; } = new() { Shader = GD.Load<Shader>("res://voxel_ortho_sprite.gdshader") };

	/// <summary>Virtual pixels per voxel; the pixel grid is 1/Sigma of a voxel wide.</summary>
	public float Sigma { get; set; } = 4f;

	private readonly MeshInstance3D _quad;

	public VoxelOrthoSprite()
	{
		Name = "OrthoSprite";
		_quad = new MeshInstance3D
		{
			Name = "SpriteQuad",
			Mesh = new QuadMesh { Size = Vector2.One },
			MaterialOverride = Material,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
		AddChild(_quad);
	}

	/// <summary>
	/// Per-frame update: faces the quad towards the head and refreshes the view-dependent uniforms.
	/// </summary>
	/// <param name="headWorld">Viewer position in world space (midpoint between the eyes in VR).</param>
	/// <param name="lightWorld">Light position in world space.</param>
	public void UpdateView(Vector3 headWorld, Vector3 lightWorld)
	{
		Vector3 boxCenter = BoxCenter, boxSize = BoxSize;
		Vector3 head = ToLocal(headWorld); // ToLocal includes this node's scale, so the result is in voxels
		Vector3 viewDir = boxCenter - head;
		if (viewDir.LengthSquared() < 1e-6f)
			return; // head exactly at the model centre: no meaningful direction
		viewDir = viewDir.Normalized();
		// Sprite up comes from the node's own up (local Y) so the sprite stays upright relative to itself
		Vector3 right = viewDir.Cross(Vector3.Up);
		if (right.LengthSquared() < 1e-3f)
			right = viewDir.Cross(Vector3.Forward); // looking straight along local up; any perpendicular will do
		right = right.Normalized();
		Vector3 up = right.Cross(viewDir).Normalized();

		// Extent of the bounding box along the sprite axes: sum of |axis component| * box size
		float deltaPx = 1f / Sigma;
		float width = Mathf.Abs(right.X) * boxSize.X + Mathf.Abs(right.Y) * boxSize.Y + Mathf.Abs(right.Z) * boxSize.Z,
			height = Mathf.Abs(up.X) * boxSize.X + Mathf.Abs(up.Y) * boxSize.Y + Mathf.Abs(up.Z) * boxSize.Z;
		// 3 virtual pixels of margin (1.5 per side) so the outline is never clipped
		Vector2 quadSize = new(width + 3f * deltaPx, height + 3f * deltaPx);

		// right x up = -viewDir, so the quad's +Z normal faces the viewer
		_quad.Transform = new Transform3D(new Basis(right * quadSize.X, up * quadSize.Y, -viewDir), boxCenter);

		Material.SetShaderParameter("ray_dir_local", viewDir);
		Material.SetShaderParameter("sprite_up_local", up);
		Material.SetShaderParameter("sigma", Sigma);
		Material.SetShaderParameter("quad_size", quadSize);
		Vector3 toLight = ToLocal(lightWorld) - boxCenter;
		Material.SetShaderParameter("light_dir", toLight.LengthSquared() < 1e-6f ? Vector3.Up : toLight.Normalized());
	}
}
