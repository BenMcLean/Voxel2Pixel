using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace BenVoxel;

/// <summary>
/// Packs a GpuSvoModel into RGBA8888 texture data suitable for GPU traversal.
///
/// Texture Layout (in uint32/pixel units):
///   [0 .. NodesCount-1]: Nodes array
///   [NodesCount .. end]: Payloads array (2 pixels per uint64, little-endian)
///
/// Texture dimensions are power-of-2, calculated to be as square as possible.
///
/// Model metadata (SizeX/Y/Z, MaxDepth) are exposed as properties and passed
/// to the shader as uniforms for efficiency (avoiding per-fragment texture reads).
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
		// Nodes: 1 pixel each (uint32)
		// Payloads: 2 pixels each (uint64 split into two uint32s)
		int totalPixels = NodesCount + (PayloadsCount << 1);
		// Calculate power-of-2 dimensions, preferring square-ish textures
		Width = NextPowerOf2((ushort)Math.Ceiling(Math.Sqrt(totalPixels)));
		// Allocate RGBA8888 data (4 bytes per pixel)
		Data = new byte[Width * Width << 2];
		// Bulk copy nodes (starting at pixel 0)
		MemoryMarshal.AsBytes(model.Nodes.AsSpan())
			.CopyTo(Data.AsSpan(0, NodesCount << 2));
		// Bulk copy payloads (starting after nodes)
		MemoryMarshal.AsBytes(model.Payloads.AsSpan())
			.CopyTo(Data.AsSpan(NodesCount << 2, PayloadsCount << 3));
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
