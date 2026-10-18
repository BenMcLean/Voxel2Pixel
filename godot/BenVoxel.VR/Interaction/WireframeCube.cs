using Godot;

namespace BenVoxel.VR;

/// <summary>A unit cube drawn as its twelve edges only, centred on the node. The select tool's icon.</summary>
public partial class WireframeCube : MeshInstance3D
{
	public WireframeCube()
	{
		Name = "WireframeCube";
		Vector3[] corners = new Vector3[8];
		for (int i = 0; i < 8; i++)
			corners[i] = new Vector3((i & 1) - 0.5f, ((i >> 1) & 1) - 0.5f, ((i >> 2) & 1) - 0.5f);
		// An edge joins two corners that differ in exactly one bit of their index
		System.Collections.Generic.List<int> edges = [];
		for (int i = 0; i < 8; i++)
		{
			for (int bit = 1; bit <= 4; bit <<= 1)
			{
				if ((i & bit) == 0)
				{
					edges.Add(i);
					edges.Add(i | bit);
				}
			}
		}
		Godot.Collections.Array arrays = [];
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = corners;
		arrays[(int)Mesh.ArrayType.Index] = edges.ToArray();
		ArrayMesh mesh = new();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, arrays);
		Mesh = mesh;
		MaterialOverride = new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			AlbedoColor = Colors.White,
		};
		CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
	}
}
