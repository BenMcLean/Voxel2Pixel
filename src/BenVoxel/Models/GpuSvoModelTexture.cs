using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BenVoxel.Models;

/// <summary>
/// Per-model descriptor for GPU traversal as per GpuSvoModel.md Section 15.
/// Contains all metadata needed to traverse a specific model within packed SVO data.
/// Offsets are expressed in element units, not bytes.
/// </summary>
public readonly struct SvoModelDescriptor
{
	/// <summary>
	/// Offset into the Nodes array where this model's nodes begin.
	/// </summary>
	public uint NodeOffset { get; }
	/// <summary>
	/// Offset into the Payloads array where this model's payloads begin.
	/// Note: Payloads are stored as uint64, so each payload occupies 2 uint32 slots in texture.
	/// </summary>
	public uint PayloadOffset { get; }
	/// <summary>
	/// Maximum depth of the octree for this model.
	/// </summary>
	public byte MaxDepth { get; }
	/// <summary>
	/// Voxel dimensions (X, Y, Z) of the model in Z-up coordinate space.
	/// </summary>
	public ushort SizeX { get; }
	public ushort SizeY { get; }
	public ushort SizeZ { get; }
	/// <summary>
	/// Length of one edge of the cubic root volume (2^MaxDepth).
	/// </summary>
	public uint RootSize { get; }

	public SvoModelDescriptor(uint nodeOffset, uint payloadOffset, GpuSvoModel model)
	{
		NodeOffset = nodeOffset;
		PayloadOffset = payloadOffset;
		MaxDepth = model.MaxDepth;
		SizeX = model.SizeX;
		SizeY = model.SizeY;
		SizeZ = model.SizeZ;
		RootSize = model.RootSize;
	}
}

/// <summary>
/// Packs one or more GpuSvoModel instances into RGBA8888 texture data suitable for GPU traversal.
///
/// Texture Layout (in uint32/pixel units):
///   [0 .. TotalNodesCount-1]: All Nodes arrays concatenated
///   [TotalNodesCount .. end]: All Payloads arrays concatenated (2 pixels per uint64, little-endian)
///
/// Per GpuSvoModel.md Section 14.1: No padding rows or per-model alignment gaps are permitted.
/// Texture dimensions are power-of-2, calculated to be as square as possible.
///
/// Per-model descriptors (NodeOffset, PayloadOffset, MaxDepth, VoxelDimensions) are provided
/// via the Descriptors property for shader uniform binding.
/// </summary>
public class GpuSvoModelTexture
{
	/// <summary>
	/// Raw RGBA8888 texture data, little-endian uint32 per pixel.
	/// </summary>
	public byte[] Data { get; }
	/// <summary>
	/// Texture width in pixels (always power of 2).
	/// </summary>
	public ushort Width { get; }
	/// <summary>
	/// Total number of uint32 node entries across all models.
	/// </summary>
	public int TotalNodesCount { get; }
	/// <summary>
	/// Total number of uint64 payload entries across all models.
	/// </summary>
	public int TotalPayloadsCount { get; }
	/// <summary>
	/// Per-model descriptors for GPU traversal. Index corresponds to model order.
	/// </summary>
	public IReadOnlyList<SvoModelDescriptor> Descriptors { get; }

	/// <summary>
	/// Creates a texture containing multiple models packed contiguously.
	/// </summary>
	public GpuSvoModelTexture(params GpuSvoModel[] models)
	{
		if (models is null || models.Length == 0)
			throw new ArgumentException("At least one model is required.", nameof(models));

		// Build descriptors and calculate totals
		List<SvoModelDescriptor> descriptors = new(models.Length);
		uint nodeOffset = 0;
		uint payloadOffset = 0;

		foreach (GpuSvoModel model in models)
		{
			if (model is null)
				throw new ArgumentNullException(nameof(models), "Model array contains null entry.");
			descriptors.Add(new SvoModelDescriptor(nodeOffset, payloadOffset, model));
			nodeOffset += (uint)model.Nodes.Length;
			payloadOffset += (uint)model.Payloads.Length;
		}

		Descriptors = descriptors;
		TotalNodesCount = (int)nodeOffset;
		TotalPayloadsCount = (int)payloadOffset;

		// Calculate total pixels needed
		// Nodes: 1 pixel each (uint32)
		// Payloads: 2 pixels each (uint64 split into two uint32s)
		int totalPixels = TotalNodesCount + (TotalPayloadsCount << 1);

		// Calculate power-of-2 dimensions, preferring square-ish textures
		Width = NextPowerOf2((ushort)Math.Ceiling(Math.Sqrt(totalPixels)));

		// Allocate RGBA8888 data (4 bytes per pixel)
		Data = new byte[Width * Width << 2];

		// Copy all nodes contiguously
		int nodeByteOffset = 0;
		foreach (GpuSvoModel model in models)
		{
			int nodeBytes = model.Nodes.Length << 2;
			MemoryMarshal.AsBytes(model.Nodes.AsSpan())
				.CopyTo(Data.AsSpan(nodeByteOffset, nodeBytes));
			nodeByteOffset += nodeBytes;
		}

		// Copy all payloads contiguously (starting after all nodes)
		int payloadByteOffset = TotalNodesCount << 2;
		foreach (GpuSvoModel model in models)
		{
			int payloadBytes = model.Payloads.Length << 3;
			MemoryMarshal.AsBytes(model.Payloads.AsSpan())
				.CopyTo(Data.AsSpan(payloadByteOffset, payloadBytes));
			payloadByteOffset += payloadBytes;
		}
	}

	/// <summary>
	/// Compute power of two greater than or equal to n.
	/// </summary>
	public static ushort NextPowerOf2(ushort n)
	{
		if (n == 0) return 1;
		n--;
		n |= (ushort)(n >> 1);
		n |= (ushort)(n >> 2);
		n |= (ushort)(n >> 4);
		n |= (ushort)(n >> 8);
		return (ushort)(n + 1);
	}
}
