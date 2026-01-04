using System;
using System.Runtime.InteropServices;
using BenVoxel;
using Godot;
using Voxel2Pixel.Model.FileFormats;

namespace BenVoxelGpu;

public partial class Root : Node3D
{
	public const string ComputeShader = """
#version 450
#extension GL_EXT_shader_explicit_arithmetic_types_int64 : require

layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(rgba8, set = 0, binding = 0) uniform image2D out_tex;
layout(set = 1, binding = 0, std430) buffer Nodes { uint data[]; } nodes;
layout(set = 1, binding = 1, std430) buffer Payloads { uint64_t data[]; } payloads;

layout(push_constant) uniform Constants {
	uvec3 model_size;
	uint max_depth;
	vec3 ray_dir;
	float scale;
	vec3 step_dir;
	float _pad1;
	vec3 t_delta;
	float _pad2;
	mat4 rotation;
} params;

const uint FLAG_INTERNAL = 0x80000000u;
const uint FLAG_LEAF_TYPE = 0x40000000u;

uint sample_svo(uvec3 pos) {
	if (any(greaterThanEqual(pos, params.model_size))) return 0u;
	uint node_idx = 0;

	for(int depth = 0; depth < int(params.max_depth) - 1; depth++) {
		uint node_data = nodes.data[node_idx];

		// Check if this is an internal node (bit 31)
		if ((node_data & FLAG_INTERNAL) == 0u) {
			// Leaf node - check type (bit 30)
			if ((node_data & FLAG_LEAF_TYPE) == 0u) {
				// Uniform leaf - material is in lower 8 bits
				return node_data & 0xFFu;
			} else {
				// Brick leaf - get payload index and extract voxel
				uint payload_idx = node_data & ~FLAG_LEAF_TYPE;
				int octant = ((int(pos.z) & 1) << 2) | ((int(pos.y) & 1) << 1) | (int(pos.x) & 1);
				uint64_t payload = payloads.data[payload_idx];
				return uint((payload >> (octant * 8)) & 0xFFul);
			}
		}

		// Internal node - traverse to child
		uint mask = node_data & 0xFFu;
		int shift = int(params.max_depth) - 1 - depth;
		uint octant = (((pos.z >> shift) & 1u) << 2) | (((pos.y >> shift) & 1u) << 1) | ((pos.x >> shift) & 1u);

		if (((mask >> octant) & 1u) == 0u) return 0u;

		uint child_base = (node_data & ~FLAG_INTERNAL) >> 8;
		uint offset = uint(bitCount(mask & ((1u << octant) - 1u)));
		node_idx = child_base + offset;
	}

	// Final level - must be a leaf
	uint node_data = nodes.data[node_idx];
	if ((node_data & FLAG_LEAF_TYPE) == 0u) {
		return node_data & 0xFFu;
	} else {
		uint payload_idx = node_data & ~FLAG_LEAF_TYPE;
		int octant = ((int(pos.z) & 1) << 2) | ((int(pos.y) & 1) << 1) | (int(pos.x) & 1);
		uint64_t payload = payloads.data[payload_idx];
		return uint((payload >> (octant * 8)) & 0xFFul);
	}
}

void main() {
	ivec2 uv = ivec2(gl_GlobalInvocationID.xy);
	ivec2 tex_size = imageSize(out_tex);
	if (uv.x >= tex_size.x || uv.y >= tex_size.y) return;

	// 1. Calculate ray origin in "Camera Space"
	// We center the pixels and apply scale
	vec2 p = (vec2(uv) - vec2(tex_size) * 0.5) / params.scale;

	// Ray starts far away at +Z (orthographic camera)
	vec3 ro_view = vec3(p.x, p.y, 500.0);

	// 2. Transform ray origin to "Model Space"
	// We use the rotation matrix to turn the camera around the model
	// We add half the model size to keep the rotation centered on the model's middle
	vec3 ro = (params.rotation * vec4(ro_view, 1.0)).xyz + (vec3(params.model_size) * 0.5);

	// Ray direction, step_dir, and t_delta are pre-calculated and passed as uniforms!
	vec3 rd = params.ray_dir;

	vec4 color = vec4(0.03, 0.03, 0.05, 1.0); // Dark background

	// 3. DDA Traversal with pre-calculated constants
	vec3 pos = floor(ro);
	vec3 t_max = (floor(ro) + max(params.step_dir, 0.0) - ro) / rd;
	int last_axis = 0;

	// Use a large enough step count to cross the bounding box from any angle
	for (int i = 0; i < 1000; i++) {
		// Only sample if we are inside the actual grid
		if (all(greaterThanEqual(pos, vec3(0.0))) && all(lessThan(pos, vec3(params.model_size)))) {
			uint mat = sample_svo(uvec3(pos));
			if (mat > 0u) {
				// --- DIRECTIONAL LIGHTING ---
				vec3 normal = vec3(0.0);
				if (last_axis == 0) normal.x = -params.step_dir.x;
				else if (last_axis == 1) normal.y = -params.step_dir.y;
				else if (last_axis == 2) normal.z = -params.step_dir.z;

				// Light from top-right-front
				vec3 light_dir = normalize(vec3(0.5, 1.0, 0.7));
				float diff = max(dot(normal, light_dir), 0.0);

				vec3 base_color = vec3(float(mat) / 255.0);
				color = vec4(base_color * (diff + 0.2), 1.0);
				break;
			}
		}

		// Advance DDA using pre-calculated t_delta
		if (t_max.x < t_max.y) {
			if (t_max.x < t_max.z) { t_max.x += params.t_delta.x; pos.x += params.step_dir.x; last_axis = 0; }
			else { t_max.z += params.t_delta.z; pos.z += params.step_dir.z; last_axis = 2; }
		} else {
			if (t_max.y < t_max.z) { t_max.y += params.t_delta.y; pos.y += params.step_dir.y; last_axis = 1; }
			else { t_max.z += params.t_delta.z; pos.z += params.step_dir.z; last_axis = 2; }
		}

		// Safety break if we leave the "danger zone" around the model
		if (any(greaterThan(abs(pos - vec3(params.model_size)*0.5), vec3(600.0)))) break;
	}

	imageStore(out_tex, uv, color);
}
""";
	private Camera3D _camera;
	private MeshInstance3D _screenQuad;
	private RenderingDevice _rd;
	private Rid _shader,
		_pipeline,
		_texture,
		_textureSet,
		_nodesBuffer,
		_payloadsBuffer,
		_dataUniformSet;
	private Vector3I _modelSize;
	private int _maxDepth;
	private float _rotationAngle = 0f;

	public override void _Ready()
	{
		// Load model and store size metadata
		GpuSvoModel model = new(new VoxFileModel(@"..\..\src\Tests\Voxel2Pixel.Test\TestData\Models\Sora.vox"));
		_modelSize = new Vector3I(model.SizeX, model.SizeY, model.SizeZ);
		_maxDepth = model.MaxDepth;

		_rd = RenderingServer.GetRenderingDevice();

		SetupTexture();

		// Create buffers using the model data
		_nodesBuffer = _rd.StorageBufferCreate((uint)(model.Nodes.Length * 4), model.Nodes.ToByteArray());
		_payloadsBuffer = _rd.StorageBufferCreate((uint)(model.Payloads.Length * 8), model.Payloads.ToByteArray());

		// Compile and check for errors
		RDShaderSource shaderSource = new RDShaderSource { Language = RenderingDevice.ShaderLanguage.Glsl, SourceCompute = ComputeShader };
		RDShaderSpirV spirv = _rd.ShaderCompileSpirVFromSource(shaderSource);
		if (spirv.GetStageCompileError(RenderingDevice.ShaderStage.Compute) != "")
			GD.PrintErr(spirv.GetStageCompileError(RenderingDevice.ShaderStage.Compute));

		_shader = _rd.ShaderCreateFromSpirV(spirv);
		_pipeline = _rd.ComputePipelineCreate(_shader);

		// Uniforms for Texture and Model Data
		RDUniform texU = new() { Binding = 0, UniformType = RenderingDevice.UniformType.Image };
		texU.AddId(_texture);
		_textureSet = _rd.UniformSetCreate([texU], _shader, 0);

		RDUniform nodeU = new() { Binding = 0, UniformType = RenderingDevice.UniformType.StorageBuffer };
		nodeU.AddId(_nodesBuffer);
		RDUniform payU = new() { Binding = 1, UniformType = RenderingDevice.UniformType.StorageBuffer };
		payU.AddId(_payloadsBuffer);
		_dataUniformSet = _rd.UniformSetCreate([nodeU, payU], _shader, 1);

		AddChild(_camera = new Camera3D
		{
			Projection = Camera3D.ProjectionType.Orthogonal,
			Size = 2.0f, // Match the QuadMesh size (2x2)
			Position = new Vector3(0, 0, 1), // Step back 1 unit
		});
		_camera.LookAt(Vector3.Zero); // Ensure it's looking at the origin

		AddChild(_screenQuad = new MeshInstance3D
		{
			Mesh = new QuadMesh { Size = new Vector2(2, 2) },
			MaterialOverride = CreateDisplayMaterial(),
			Position = Vector3.Zero, // Centered at origin
		});
	}

	[StructLayout(LayoutKind.Explicit, Size = 128)]
	public struct ShaderConstants
	{
		[FieldOffset(0)] public Vector3I ModelSize;
		[FieldOffset(12)] public uint MaxDepth;
		[FieldOffset(16)] public Vector3 RayDir;
		[FieldOffset(28)] public float Scale;
		[FieldOffset(32)] public Vector3 StepDir;
		[FieldOffset(44)] public float _pad1;
		[FieldOffset(48)] public Vector3 TDelta;
		[FieldOffset(60)] public float _pad2;
		[FieldOffset(64)] public System.Numerics.Matrix4x4 Rotation;
	}

	public override void _Process(double delta)
	{
		// Accumulate rotation over time
		_rotationAngle += (float)delta * 1.0f; // Speed of 1 radian per second

		long list = _rd.ComputeListBegin();
		_rd.ComputeListBindComputePipeline(list, _pipeline);
		_rd.ComputeListBindUniformSet(list, _textureSet, 0);
		_rd.ComputeListBindUniformSet(list, _dataUniformSet, 1);

		// Create a spinning rotation matrix
		// We combine a constant X tilt (for that 3D look) with a changing Y rotation
		System.Numerics.Matrix4x4 rotation =
			System.Numerics.Matrix4x4.CreateRotationY(_rotationAngle) * System.Numerics.Matrix4x4.CreateRotationX(Mathf.DegToRad(30));

		// Transpose once for GLSL column-major format
		System.Numerics.Matrix4x4 rotationTransposed = System.Numerics.Matrix4x4.Transpose(rotation);

		// Pre-calculate DDA constants for orthographic camera
		// Ray direction in view space is always (0, 0, -1) for orthographic
		System.Numerics.Vector3 rdView = new(0, 0, -1);
		// Transform using the transposed matrix to match what the shader does
		System.Numerics.Vector4 rdView4 = new(rdView.X, rdView.Y, rdView.Z, 0);
		System.Numerics.Vector4 rdModel4 = System.Numerics.Vector4.Transform(rdView4, rotationTransposed);
		System.Numerics.Vector3 rdModel = new(rdModel4.X, rdModel4.Y, rdModel4.Z);
		rdModel = System.Numerics.Vector3.Normalize(rdModel);

		// Calculate step_dir and t_delta once for all rays
		System.Numerics.Vector3 stepDir = new(
			Math.Sign(rdModel.X),
			Math.Sign(rdModel.Y),
			Math.Sign(rdModel.Z)
		);

		System.Numerics.Vector3 tDelta = new(
			Math.Abs(1.0f / rdModel.X),
			Math.Abs(1.0f / rdModel.Y),
			Math.Abs(1.0f / rdModel.Z)
		);

		ShaderConstants constants = new()
		{
			ModelSize = _modelSize,
			MaxDepth = (uint)_maxDepth,
			RayDir = new Vector3(rdModel.X, rdModel.Y, rdModel.Z),
			Scale = 4f,
			StepDir = new Vector3(stepDir.X, stepDir.Y, stepDir.Z),
			TDelta = new Vector3(tDelta.X, tDelta.Y, tDelta.Z),
			Rotation = rotationTransposed,
		};

		byte[] pushData = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref constants, 1)).ToArray();
		_rd.ComputeListSetPushConstant(list, pushData, (uint)pushData.Length);

		_rd.ComputeListDispatch(list, 512 / 8, 512 / 8, 1);
		_rd.ComputeListEnd();
	}

	private void SetupTexture()
	{
		RDTextureFormat fmt = new()
		{
			Width = 512,
			Height = 512,
			Format = RenderingDevice.DataFormat.R8G8B8A8Unorm,
			UsageBits = RenderingDevice.TextureUsageBits.StorageBit | RenderingDevice.TextureUsageBits.SamplingBit | RenderingDevice.TextureUsageBits.CanUpdateBit,
		};
		_texture = _rd.TextureCreate(fmt, new RDTextureView());
	}

	private ShaderMaterial CreateDisplayMaterial()
	{
		ShaderMaterial mat = new()
		{
			Shader = new Shader
			{
				Code = "shader_type spatial; render_mode unshaded; uniform sampler2D t; void fragment() { ALBEDO = texture(t, SCREEN_UV).rgb; }",
			}
		};
		Texture2Drd tex = new() { TextureRdRid = _texture, };
		mat.SetShaderParameter("t", tex);
		return mat;
	}
}

public static class Extensions
{
	// Returns a byte view of the existing memory, then copies to a new array
	public static byte[] ToByteArray<T>(this T[] source) where T : struct =>
		source is null ?[] : MemoryMarshal.AsBytes(source.AsSpan()).ToArray();
}
