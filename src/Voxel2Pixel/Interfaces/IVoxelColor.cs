using BenVoxel.Structs;

namespace Voxel2Pixel.Interfaces;

/// <summary>
/// Interface for voxel color palettes.
/// </summary>
/// <remarks>
/// <para><strong>Palette Color Format</strong></para>
/// <para>
/// Each palette entry is a <see cref="uint"/> in <strong>0xRRGGBBAA format</strong> using <strong>big-endian byte order</strong>
/// (red, green, blue, alpha in hexadecimal notation, where each component is one byte).
/// </para>
/// <list type="bullet">
/// <item>Most significant byte (bits 24-31): Red channel (0-255)</item>
/// <item>Bits 16-23: Green channel (0-255)</item>
/// <item>Bits 8-15: Blue channel (0-255)</item>
/// <item>Least significant byte (bits 0-7): Alpha channel (0-255)</item>
/// </list>
/// <para><strong>Examples:</strong></para>
/// <list type="bullet">
/// <item><c>0xFF0000FF</c> = Red with full opacity (R=255, G=0, B=0, A=255)</item>
/// <item><c>0x00FF00FF</c> = Green with full opacity (R=0, G=255, B=0, A=255)</item>
/// <item><c>0x0000FFFF</c> = Blue with full opacity (R=0, G=0, B=255, A=255)</item>
/// <item><c>0xFFFFFF80</c> = White with 50% opacity (R=255, G=255, B=255, A=128)</item>
/// </list>
/// <para>
/// This is <strong>big-endian RGBA format</strong>. When written to a byte array using
/// <see cref="System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian"/>,
/// the bytes appear in memory as [R, G, B, A] (most significant byte first),
/// which is directly compatible with standard RGBA8 image formats.
/// </para>
/// </remarks>
public interface IVoxelColor
{
	/// <summary>
	/// Gets the color for a specific palette index and visible face.
	/// </summary>
	/// <param name="index">The palette index (0-255).</param>
	/// <param name="visibleFace">The visible face of the voxel (default: Front).</param>
	/// <returns>The color as a uint32 in 0xRRGGBBAA format.</returns>
	uint this[byte index, VisibleFace visibleFace = VisibleFace.Front] { get; }
}
