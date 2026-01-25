using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using BenProgress;
using BenVoxel.Interfaces;
using BenVoxel.Models;
using Godot;

namespace BenVoxelGpu;

/// <summary>
/// Bridge class that manages GPU-bound SVO textures and palettes.
/// Multiple VolumetricOrthoImpostors can share the same bridge to render different models
/// from the same texture atlas.
/// </summary>
public class GpuSvoModelTextureBridge
{
	private readonly ImageTexture _svoTexture;
	private readonly ImageTexture[] _paletteTextures;
	private readonly IReadOnlyList<SvoModelDescriptor> _descriptors;
	private readonly int _textureWidth;
	private readonly int _totalNodesCount;

	/// <summary>
	/// Number of models available in this texture bridge.
	/// </summary>
	public int ModelCount => _descriptors.Count;

	/// <summary>
	/// Gets the descriptor for a specific model.
	/// </summary>
	public SvoModelDescriptor GetDescriptor(int modelIndex) => _descriptors[modelIndex];

	/// <summary>
	/// Creates a texture bridge from an array of GpuSvoModels and their palettes.
	/// </summary>
	/// <param name="models">Array of GPU SVO models to pack into a texture</param>
	/// <param name="palettes">Array of 256-color palettes. If one palette is provided, it is used for all models.
	/// Otherwise, the number of palettes must match the number of models.</param>
	public GpuSvoModelTextureBridge(IModel[] models, params uint[][] palettes)
	{
		if (palettes.Length != 1 && models.Length != palettes.Length)
			throw new ArgumentException("Palettes array must contain either one palette (shared by all models) or one palette per model");

		GpuSvoModelTexture modelTexture = new(models);
		_descriptors = modelTexture.Descriptors;
		_totalNodesCount = modelTexture.TotalNodesCount;
		_textureWidth = modelTexture.Width;

		// Create ImageTexture from the model texture data
		Image image = Image.CreateFromData(_textureWidth, _textureWidth, false, Image.Format.Rgba8, modelTexture.Data);
		_svoTexture = ImageTexture.CreateFromImage(image);

		// Create palette textures
		_paletteTextures = new ImageTexture[palettes.Length];
		for (int m = 0; m < palettes.Length; m++)
		{
			byte[] paletteBytes = new byte[256 << 2];
			for (int i = 0; i < 256; i++)
				BinaryPrimitives.WriteUInt32BigEndian(paletteBytes.AsSpan(i << 2, 4), palettes[m][i]);
			Image paletteImage = Image.CreateFromData(256, 1, false, Image.Format.Rgba8, paletteBytes);
			_paletteTextures[m] = ImageTexture.CreateFromImage(paletteImage);
		}
	}

	/// <summary>
	/// Binds the shared SVO texture and global parameters to a shader material.
	/// Call this once when creating a material.
	/// </summary>
	public void BindToMaterial(ShaderMaterial material)
	{
		material.SetShaderParameter("svo_texture", _svoTexture);
		material.SetShaderParameter("texture_width", _textureWidth);
		material.SetShaderParameter("svo_nodes_count", (uint)_totalNodesCount);
	}

	/// <summary>
	/// Binds the model-specific parameters (palette, offsets, size) to a shader material.
	/// Call this when switching which model an impostor displays.
	/// </summary>
	public void BindModelToMaterial(ShaderMaterial material, int modelIndex)
	{
		if (modelIndex < 0 || modelIndex >= _descriptors.Count)
			throw new ArgumentOutOfRangeException(nameof(modelIndex));

		SvoModelDescriptor descriptor = _descriptors[modelIndex];
		// Use single shared palette if only one was provided, otherwise use per-model palette
		int paletteIndex = _paletteTextures.Length == 1 ? 0 : modelIndex;
		material.SetShaderParameter("palette_texture", _paletteTextures[paletteIndex]);
		material.SetShaderParameter("svo_model_size", new Vector3I(descriptor.SizeX, descriptor.SizeY, descriptor.SizeZ));
		material.SetShaderParameter("svo_max_depth", (uint)descriptor.MaxDepth);
		material.SetShaderParameter("node_offset", descriptor.NodeOffset);
		material.SetShaderParameter("payload_offset", descriptor.PayloadOffset);
	}

	/// <summary>
	/// Gets the model size for a specific model index.
	/// </summary>
	public Vector3I GetModelSize(int modelIndex)
	{
		SvoModelDescriptor descriptor = _descriptors[modelIndex];
		return new Vector3I(descriptor.SizeX, descriptor.SizeY, descriptor.SizeZ);
	}
}
