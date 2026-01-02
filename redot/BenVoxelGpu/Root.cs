using System;
using System.Runtime.InteropServices;
using BenVoxel;
using Godot;
using Voxel2Pixel.Model.FileFormats;

namespace BenVoxelGpu;

public partial class Root : Node3D
{
	// Updated Shader with uint8 extension and flexible model size
	public const string ShaderCode = """
#version 450

// Required for uint8_t support to match the byte[] Payloads buffer
#extension GL_EXT_shader_explicit_arithmetic_types_int8 : require

layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

// Binding 0: The output texture we'll display on the Quad
layout(rgba8, set = 0, binding = 0) uniform image2D out_tex;

// Binding 1.0: The SVO nodes [ChildBaseIndex (24 bits) | ValidMask (8 bits)]
layout(set = 1, binding = 0, std430) buffer Nodes { uint data[]; } nodes;
// Binding 1.1: The material IDs (1-255)
layout(set = 1, binding = 1, std430) buffer Payloads { uint8_t data[]; } payloads;

layout(push_constant) uniform Constants {
	uvec3 model_size;
} params;

void main() {
	ivec2 uv = ivec2(gl_GlobalInvocationID.xy);
	ivec2 tex_size = imageSize(out_tex);

	// Safety check for dispatch size
	if (uv.x >= tex_size.x || uv.y >= tex_size.y) return;

	// Background color (very dark blue-grey)
	vec4 final_color = vec4(0.03, 0.03, 0.04, 1.0);

	// --- RENDER SETTINGS ---
	uint scale = 4; // Zoom level: 1 voxel = 4x4 pixels
	ivec2 screen_center = tex_size / 2;
	// --- --- --- --- --- ---

	// Sprite Stacking works by iterating from bottom to top (Z)
	// We check every Z layer. If we hit a voxel, that's the pixel color.
	// Because it's "stacked," we iterate from top-down (Z = size to 0) 
	// to find the first visible voxel.
	for(int z = int(params.model_size.z) - 1; z >= 0; z--) {

		// Sprite Stacking Projection:
		// We offset the X and Y lookup based on the current Z height.
		// This creates the "3D" look from 2D layers.
		int x_offset = z; 
		int y_offset = z;

		// Calculate which voxel coordinate (vx, vy) corresponds to this pixel
		// 1. Center the coordinates
		// 2. Apply scale
		// 3. Apply the stack offset
		int vx = ((uv.x - screen_center.x) / int(scale)) + (int(params.model_size.x) / 2) - x_offset;
		int vy = ((uv.y - screen_center.y) / int(scale)) + (int(params.model_size.y) / 2) - y_offset;

		// Bounds check: if this pixel's ray is outside the model's footprint, skip Z layer
		if (vx < 0 || vx >= int(params.model_size.x) || vy < 0 || vy >= int(params.model_size.y)) {
			continue;
		}

		// --- SVO TRAVERSAL ---
		uint node_idx = 0;
		uint ux = uint(vx);
		uint uy = uint(vy);
		uint uz = uint(z);
		bool hit = false;

		for(int depth = 0; depth < 16; depth++) {
			uint node_data = nodes.data[node_idx];
			uint mask = node_data & 0xFFu;

			// SVOs are powers of 2. Level 15 is 65536. 
			// We find the octant by checking the bit at the current depth.
			int shift = 15 - depth;
			uint octant = (((uz >> shift) & 1u) << 2) | 
						  (((uy >> shift) & 1u) << 1) | 
						   ((ux >> shift) & 1u);

			// If the bit in the ValidMask is 0, this branch is empty
			if (((mask >> octant) & 1u) == 0u) {
				break; 
			}

			// Calculate child index using PopCount (number of set bits before our octant)
			uint child_base = node_data >> 8;
			uint offset = uint(bitCount(mask & ((1u << octant) - 1u)));
			uint next_idx = child_base + offset;

			// Leaf node reached
			if (depth == 15) {
				uint material = uint(payloads.data[next_idx]);
				if (material > 0u) {
					// Simple lighting: slightly darken lower layers to add depth
					float depth_shading = 0.6 + (float(z) / float(params.model_size.z)) * 0.4;
					vec3 base_color = vec3(float(material) / 255.0);

					final_color = vec4(base_color * depth_shading, 1.0);
					hit = true;
				}
				break;
			}
			node_idx = next_idx;
		}

		if (hit) break; // Found the top-most voxel for this pixel, stop searching Z
	}

	imageStore(out_tex, uv, final_color);
}
""";
	private Camera3D _camera;
	private MeshInstance3D _screenQuad;
	private RenderingDevice _rd;
	private Rid _shader;
	private Rid _pipeline;
	private Rid _texture;
	private Rid _textureSet;
	private Rid _nodesBuffer;
	private Rid _payloadsBuffer;
	private Rid _dataUniformSet;
	private Vector3I _modelSize;

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
		var shaderSource = new RDShaderSource { Language = RenderingDevice.ShaderLanguage.Glsl, SourceCompute = ShaderCode };
		var spirv = _rd.ShaderCompileSpirVFromSource(shaderSource);
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

		_camera = new Camera3D
		{
			Projection = Camera3D.ProjectionType.Orthogonal,
			Size = 2.0f, // Match the QuadMesh size (2x2)
			Position = new Vector3(0, 0, 1), // Step back 1 unit
		};
		_camera.LookAt(Vector3.Zero); // Ensure it's looking at the origin
		AddChild(_camera);

		_screenQuad = new MeshInstance3D
		{
			Mesh = new QuadMesh { Size = new Vector2(2, 2) },
			MaterialOverride = CreateDisplayMaterial(),
			Position = Vector3.Zero // Centered at origin
		};
		AddChild(_screenQuad);
	}

	public override void _Process(double delta)
	{
		// On the main device, we just dispatch; no Submit or Sync required
		long list = _rd.ComputeListBegin();
		_rd.ComputeListBindComputePipeline(list, _pipeline);
		_rd.ComputeListBindUniformSet(list, _textureSet, 0);
		_rd.ComputeListBindUniformSet(list, _dataUniformSet, 1);

		// Pass the model size as push constants
		byte[] pushData = new byte[16]; // 3 * uint (12 bytes) + padding
		MemoryMarshal.Write(pushData.AsSpan(0), ref _modelSize);
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
			UsageBits = RenderingDevice.TextureUsageBits.StorageBit | RenderingDevice.TextureUsageBits.SamplingBit | RenderingDevice.TextureUsageBits.CanUpdateBit
		};
		_texture = _rd.TextureCreate(fmt, new RDTextureView());
	}

	private ShaderMaterial CreateDisplayMaterial()
	{
		var mat = new ShaderMaterial
		{
			Shader = new Shader
			{
				Code = "shader_type spatial; render_mode unshaded; uniform sampler2D t; void fragment() { ALBEDO = texture(t, SCREEN_UV).rgb; }"
			}
		};
		var tex = new Texture2Drd { TextureRdRid = _texture };
		mat.SetShaderParameter("t", tex);
		return mat;
	}
}

public static class Extensions
{
	public static byte[] ToByteArray<T>(this T[] source) where T : struct
	{
		if (source == null) return [];
		// Returns a byte view of the existing memory, then copies to a new array
		return MemoryMarshal.AsBytes(source.AsSpan()).ToArray();
	}
}
