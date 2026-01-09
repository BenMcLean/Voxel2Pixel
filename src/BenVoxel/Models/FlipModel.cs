using System.Collections.Generic;
using System.Linq;
using BenVoxel.Structs;

namespace Voxel2Pixel.Model;

public class FlipModel : ContainerModel
{
	public bool FlipX { get; set; } = false;
	public bool FlipY { get; set; } = false;
	public bool FlipZ { get; set; } = false;
	public FlipModel Set(params bool?[] @bool)
	{
		if (@bool is not null && @bool.Length > 0)
		{
			if (@bool[0] is bool flipX)
				FlipX = flipX;
			if (@bool.Length > 1)
			{
				if (@bool[1] is bool flipY)
					FlipY = flipY;
				if (@bool.Length > 2
					&& @bool[2] is bool flipZ)
					FlipZ = flipZ;
			}
		}
		return this;
	}
	public bool[] Get => [FlipX, FlipY, FlipZ];
	public override byte this[ushort x, ushort y, ushort z] => Model[
		x: FlipX ? (ushort)(SizeX - 1 - x) : x,
		y: FlipY ? (ushort)(SizeY - 1 - y) : y,
		z: FlipZ ? (ushort)(SizeZ - 1 - z) : z];
	public override IEnumerator<Voxel> GetEnumerator() => Model
		.Select(voxel => new Voxel(
			X: FlipX ? (ushort)(SizeX - 1 - voxel.X) : voxel.X,
			Y: FlipY ? (ushort)(SizeY - 1 - voxel.Y) : voxel.Y,
			Z: FlipZ ? (ushort)(SizeZ - 1 - voxel.Z) : voxel.Z,
			Material: voxel.Material))
		.GetEnumerator();
}
