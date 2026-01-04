using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace BenVoxel;

/// <summary>
/// Packs a GpuSvoModel into RGBA8888 texture data suitable for GPU traversal.
///
/// Texture Layout (in uint32/pixel units):
///   [0]: NodesCount
///   [1]: PayloadsCount
///   [2]: SizeX | (SizeY << 16)  -- packed model dimensions
///   [3]: SizeZ | (MaxDepth << 16)
///   [4 .. 4+NodesCount-1]: Nodes array
///   [4+NodesCount .. end]: Payloads array (2 pixels per uint64, little-endian)
///
/// Texture dimensions are power-of-2, calculated to be as square as possible.
///
/// Shader can read the header to determine all offsets and model properties,
/// requiring only texture_width as an external uniform.
/// </summary>
public class GpuSvoModelTexture
{
	public const byte HeaderSize = 16,
		HeaderSizeInPixels = 4;
	/// <summary>
	/// Raw RGBA8888 texture data, little-endian uint32 per pixel.
	/// </summary>
	public byte[] Data { get; }
	/// <summary>
	/// Texture width in pixels (always power of 2).
	/// </summary>
	public ushort Width { get; }
	/// <summary>
	/// Number of uint32 node entries.
	/// </summary>
	public int NodesCount { get; }
	/// <summary>
	/// Number of uint64 payload entries.
	/// </summary>
	public int PayloadsCount { get; }
	/// <summary>
	/// Model dimensions from the source GpuSvoModel.
	/// </summary>
	public ushort SizeX { get; }
	public ushort SizeY { get; }
	public ushort SizeZ { get; }
	public byte MaxDepth { get; }
	public GpuSvoModelTexture(GpuSvoModel model)
	{
		if (model is null)
			throw new ArgumentNullException(nameof(model));
		NodesCount = model.Nodes.Length;
		PayloadsCount = model.Payloads.Length;
		SizeX = model.SizeX;
		SizeY = model.SizeY;
		SizeZ = model.SizeZ;
		MaxDepth = model.MaxDepth;
		// Calculate total pixels needed
		// Header: 4 pixels
		// Nodes: 1 pixel each (uint32)
		// Payloads: 2 pixels each (uint64 split into two uint32s)
		int totalPixels = HeaderSizeInPixels + NodesCount + (PayloadsCount << 1);
		// Calculate power-of-2 dimensions, preferring square-ish textures
		Width = NextPowerOf2((ushort)Math.Ceiling(Math.Sqrt(totalPixels)));
		// Allocate RGBA8888 data (4 bytes per pixel)
		Data = new byte[Width * Width << 2];
		// Write header
		BinaryPrimitives.WriteUInt32LittleEndian(Data.AsSpan(0, 4), (uint)NodesCount);
		BinaryPrimitives.WriteUInt32LittleEndian(Data.AsSpan(4, 4), (uint)PayloadsCount);
		BinaryPrimitives.WriteUInt32LittleEndian(Data.AsSpan(8, 4), SizeX | ((uint)SizeY << 16));
		BinaryPrimitives.WriteUInt32LittleEndian(Data.AsSpan(12, 4), SizeZ | ((uint)MaxDepth << 16));
		// Bulk copy nodes
		MemoryMarshal.AsBytes(model.Nodes.AsSpan())
			.CopyTo(Data.AsSpan(HeaderSize, model.Nodes.Length << 2));
		// Bulk copy payloads
		MemoryMarshal.AsBytes(model.Payloads.AsSpan())
			.CopyTo(Data.AsSpan(HeaderSize + (NodesCount << 2), model.Payloads.Length << 3));
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
