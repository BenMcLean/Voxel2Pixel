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
#extension GL_EXT_shader_explicit_arithmetic_types_int8 : require

layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(rgba8, set = 0, binding = 0) uniform image2D out_tex;
layout(set = 1, binding = 0, std430) buffer Nodes { uint data[]; } nodes;
layout(set = 1, binding = 1, std430) buffer Payloads { uint8_t data[]; } payloads;

layout(push_constant) uniform Constants {
	uvec3 model_size;
	float scale;
	mat4 rotation;
} params;

uint sample_svo(uvec3 pos) {
	if (any(greaterThanEqual(pos, params.model_size))) return 0u;
	uint node_idx = 0;
	for(int depth = 0; depth < 16; depth++) {
		uint node_data = nodes.data[node_idx];
		uint mask = node_data & 0xFFu;
		int shift = 15 - depth;
		uint octant = (((pos.z >> shift) & 1u) << 2) | (((pos.y >> shift) & 1u) << 1) | ((pos.x >> shift) & 1u);
		if (((mask >> octant) & 1u) == 0u) return 0u;
		uint child_base = node_data >> 8;
		uint offset = uint(bitCount(mask & ((1u << octant) - 1u)));
		uint next_idx = child_base + offset;
		if (depth == 15) return uint(payloads.data[next_idx]);
		node_idx = next_idx;
	}
	return 0u;
}

void main() {
	ivec2 uv = ivec2(gl_GlobalInvocationID.xy);
	ivec2 tex_size = imageSize(out_tex);
	if (uv.x >= tex_size.x || uv.y >= tex_size.y) return;

	// 1. Calculate ray in "Camera Space"
	// We center the pixels and apply scale
	vec2 p = (vec2(uv) - vec2(tex_size) * 0.5) / params.scale;

	// Ray starts far away at +Z and looks towards -Z
	vec3 ro_view = vec3(p.x, p.y, 500.0);
	vec3 rd_view = vec3(0.0, 0.0, -1.0);

	// 2. Transform Ray to "Model Space"
	// We use the rotation matrix to turn the camera around the model
	// We add half the model size to keep the rotation centered on the model's middle
	vec3 ro = (params.rotation * vec4(ro_view, 1.0)).xyz + (vec3(params.model_size) * 0.5);
	vec3 rd = normalize((params.rotation * vec4(rd_view, 0.0)).xyz);

	vec4 color = vec4(0.03, 0.03, 0.05, 1.0); // Dark background

	// 3. DDA Traversal
	vec3 pos = floor(ro);
	vec3 step_dir = sign(rd);
	vec3 t_delta = abs(1.0 / rd);
	vec3 t_max = (floor(ro) + max(step_dir, 0.0) - ro) / rd;
	int last_axis = 0;

	// Use a large enough step count to cross the bounding box from any angle
	for (int i = 0; i < 1000; i++) {
		// Only sample if we are inside the actual grid
		if (all(greaterThanEqual(pos, vec3(0.0))) && all(lessThan(pos, vec3(params.model_size)))) {
			uint mat = sample_svo(uvec3(pos));
			if (mat > 0u) {
				// --- DIRECTIONAL LIGHTING ---
				vec3 normal = vec3(0.0);
				if (last_axis == 0) normal.x = -step_dir.x;
				else if (last_axis == 1) normal.y = -step_dir.y;
				else if (last_axis == 2) normal.z = -step_dir.z;

				// Light from top-right-front
				vec3 light_dir = normalize(vec3(0.5, 1.0, 0.7));
				float diff = max(dot(normal, light_dir), 0.0);

				vec3 base_color = vec3(float(mat) / 255.0);
				color = vec4(base_color * (diff + 0.2), 1.0);
				break;
			}
		}

		// Advance DDA
		if (t_max.x < t_max.y) {
			if (t_max.x < t_max.z) { t_max.x += t_delta.x; pos.x += step_dir.x; last_axis = 0; }
			else { t_max.z += t_delta.z; pos.z += step_dir.z; last_axis = 2; }
		} else {
			if (t_max.y < t_max.z) { t_max.y += t_delta.y; pos.y += step_dir.y; last_axis = 1; }
			else { t_max.z += t_delta.z; pos.z += step_dir.z; last_axis = 2; }
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
	private float _rotationAngle = 0f;

	public override void _Ready()
	{
		// Load model and store size metadata
		GpuSvoModel model = new(new VoxFileModel(@"..\..\src\Tests\Voxel2Pixel.Test\TestData\Models\Sora.vox"));
		_modelSize = new Vector3I(model.SizeX, model.SizeY, model.SizeZ);

		_rd = RenderingServer.GetRenderingDevice();

		SetupTexture();

		// Create buffers using the model data
		_nodesBuffer = _rd.StorageBufferCreate((uint)(model.Nodes.Length * 4), model.Nodes.ToByteArray());
		_payloadsBuffer = _rd.StorageBufferCreate((uint)model.Payloads.Length, model.Payloads);

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

	[StructLayout(LayoutKind.Explicit, Size = 80)] // 12 (uvec3) + 4 (float) + 64 (mat4)
	public struct ShaderConstants
	{
		[FieldOffset(0)] public Vector3I ModelSize;
		[FieldOffset(12)] public float Scale;
		[FieldOffset(16)] public System.Numerics.Matrix4x4 Rotation;
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

		ShaderConstants constants = new()
		{
			ModelSize = _modelSize,
			Scale = 4f,
			Rotation = System.Numerics.Matrix4x4.Transpose(rotation), // Transpose for GLSL column-major
		};

		byte[] pushData = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref constants, 1)).ToArray();
		_rd.ComputeListSetPushConstant(list, pushData, (uint)pushData.Length); //

		_rd.ComputeListDispatch(list, 512 / 8, 512 / 8, 1); //
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
