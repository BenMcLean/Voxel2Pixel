using BenVoxel;
using System;
using System.Collections.Generic;
using System.Linq;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;
using Voxel2Pixel.Render;

namespace Voxel2Pixel.Draw;

/// <summary>
/// All methods in this static class are actually stateless functions, meaning that they do not reference any modifiable variables besides their parameters. This makes them as thread-safe as their parameters.
/// I have been forced into a situation where X and Y mean something different in 2D space from what they mean in 3D space. Not only do the coordinates not match, but 3D is upside down when compared to 2D. I hate this. I hate it so much. But I'm stuck with it if I want my software to be interoperable with other existing software.
/// In 2D space for pixels, X+ means east/right, Y+ means south/down. 0, 0 is the upper-left / northwestern corner of the screen. This is dictated by how 2D raster graphics are typically stored in computer memory.
/// In 3D space for voxels, I'm following the MagicaVoxel convention, which is Z+up, right-handed, so X+ means right/east, Y+ means forwards/north and Z+ means up. 0, 0, 0 is the bottom left nearest / southwestern corner of the model.
/// </summary>
public static class VoxelDraw
{
	#region Perspectives
	public static void Draw(this IModel model, IRenderer renderer, Perspective perspective, byte scaleX = 1, byte scaleY = 1, byte scaleZ = 1, double radians = 0d, bool peak = false, ushort offsetX = 0, ushort offsetY = 0)
	{
		if (offsetX > 0 || offsetY > 0
			|| (!perspective.IsInternallyScaled(peak) && (scaleX != 1 || scaleY != 1)))
			renderer = new OffsetRenderer
			{
				RectangleRenderer = renderer,
				OffsetX = offsetX,
				OffsetY = offsetY,
				ScaleX = scaleX,
				ScaleY = scaleY,
			};
		switch (perspective)
		{
			default:
			case Perspective.Front:
				if (peak)
					FrontPeak(model, renderer, scaleX, scaleY);
				else
					Front(model, renderer);
				break;
			case Perspective.Overhead:
				Overhead(model, renderer);
				break;
			case Perspective.Underneath:
				Underneath(model, renderer);
				break;
			case Perspective.Diagonal:
				if (peak)
					DiagonalPeak(model, renderer, scaleX, scaleY);
				else
					Diagonal(model, renderer);
				break;
			case Perspective.Above:
				Above(model, renderer);
				break;
			case Perspective.Iso:
				Iso(model, renderer);
				break;
			case Perspective.IsoUnderneath:
				IsoUnderneath(model, renderer);
				break;
			case Perspective.Stacked:
				Stacked(model, renderer, radians, scaleX, scaleY, scaleZ, peak);
				break;
			case Perspective.ZSlices:
				ZSlices(model, renderer, peak);
				break;
		}
	}
	public static Point Size(this IModel model, Perspective perspective, byte scaleX = 1, byte scaleY = 1, byte scaleZ = 1, double radians = 0d, bool peak = false)
	{
		Point size = perspective switch
		{
			Perspective.Overhead => OverheadSize(model),
			Perspective.Underneath => UnderneathSize(model),
			Perspective.Diagonal => peak ? DiagonalPeakSize(model, scaleX, scaleY) : DiagonalSize(model),
			Perspective.Above => AboveSize(model),
			Perspective.Iso => IsoSize(model),
			Perspective.IsoUnderneath => IsoUnderneathSize(model),
			Perspective.Stacked => StackedSize(model, radians, scaleX, scaleY, scaleZ),
			Perspective.ZSlices => ZSlicesSize(model),
			_ => peak ? FrontPeakSize(model, scaleX, scaleY) : FrontSize(model),
		};
		return perspective.IsInternallyScaled(peak) ? size : new Point(X: size.X * scaleX, Y: size.Y * scaleY);
	}
	public static Point Locate(this IModel model, Perspective perspective, Point3D point, byte scaleX = 1, byte scaleY = 1, byte scaleZ = 1, double radians = 0d, bool peak = false)
	{
		Point result;
		switch (perspective)
		{
			default:
			case Perspective.Front:
				result = FrontLocate(model, point);
				break;
			case Perspective.Overhead:
			case Perspective.Underneath:
				return new Point(point.X, point.Y);
			case Perspective.Diagonal:
				result = DiagonalLocate(model, point);
				break;
			case Perspective.Above:
				return AboveLocate(model, point);
			case Perspective.Iso:
				return IsoLocate(model, point);
			case Perspective.IsoUnderneath:
				return IsoUnderneathLocate(model, point);
			case Perspective.Stacked:
				return StackedLocate(model, point, radians, scaleX, scaleY, scaleZ);
			case Perspective.ZSlices:
				return ZSlicesLocate(model, point);
		}
		return perspective.IsInternallyScaled(peak) ?
			result
			: new()
			{
				X = result.X * scaleX,
				Y = result.Y * scaleY,
			};
	}
	#endregion Perspectives
	#region Records
	private readonly record struct VoxelY(ushort Y, byte Index)
	{
		public VoxelY(Voxel voxel) : this(Y: voxel.Y, Index: voxel.Index) { }
	}
	private readonly record struct VoxelZ(ushort Z, byte Index)
	{
		public VoxelZ(Voxel voxel) : this(Z: voxel.Z, Index: voxel.Index) { }
	}
	private readonly record struct DistantShape(uint Distance, byte Index, VisibleFace VisibleFace);
	private readonly record struct VoxelFace(Voxel Voxel, VisibleFace VisibleFace)
	{
		public readonly uint Distance => (uint)Voxel.X + Voxel.Y;
	}
	#endregion Records
	#region Straight
	public static Point FrontSize(IModel model) => new(model.SizeX, model.SizeZ);
	public static Point FrontLocate(IModel model, Point3D point) => FrontLocate(model.SizeZ, point.X, point.Z);
	public static Point FrontLocate(ushort sizeZ, int voxelX = 0, int voxelZ = 0) => new()
	{
		X = voxelX,
		Y = sizeZ - 1 - voxelZ,
	};
	public static void Front(IModel model, IRectangleRenderer renderer, VisibleFace visibleFace = VisibleFace.Front)
	{
		ushort width = model.SizeX,
			height = model.SizeZ;
		VoxelY?[] grid = new VoxelY?[width * height];
		foreach (Voxel voxel in model
			.Where(voxel => voxel.Index != 0))
			if (width * (height - voxel.Z - 1) + voxel.X is int i
				&& (grid[i] is not VoxelY old
					|| old.Index == 0
					|| old.Y > voxel.Y))
				grid[i] = new VoxelY(voxel);
		uint index = 0;
		for (ushort y = 0; y < height; y++)
			for (ushort x = 0; x < width; x++)
				if (grid[index++] is VoxelY voxelY && voxelY.Index != 0)
					renderer.Rect(
						x: x,
						y: y,
						index: voxelY.Index,
						visibleFace: visibleFace);
	}
	public static Point FrontPeakSize(IModel model, byte scaleX = 6, byte scaleY = 6) => new(model.SizeX * scaleX, model.SizeZ * scaleY);
	public static void FrontPeak(IModel model, IRectangleRenderer renderer, byte scaleX = 6, byte scaleY = 6)
	{
		if (scaleX < 1) throw new ArgumentOutOfRangeException(nameof(scaleX));
		if (scaleY < 1) throw new ArgumentOutOfRangeException(nameof(scaleY));
		ushort voxelWidth = model.SizeX,
			voxelHeight = model.SizeZ;
		Voxel?[] grid = new Voxel?[voxelWidth * voxelHeight];
		foreach (Voxel voxel in model
			.Where(voxel => voxel.Index != 0))
			if (voxelWidth * (voxelHeight - voxel.Z - 1) + voxel.X is int i
				&& (grid[i] is not Voxel old
					|| old.Index == 0
					|| old.Y > voxel.Y))
				grid[i] = voxel;
		ushort pixelWidth = (ushort)(voxelWidth * scaleX),
			pixelHeight = (ushort)(voxelHeight * scaleY);
		uint index = 0;
		for (ushort y = 0; y < pixelHeight; y += scaleY)
			for (ushort x = 0; x < pixelWidth; x += scaleX)
				if (grid[index++] is Voxel voxel && voxel.Index != 0)
					if (voxel.Z >= voxelHeight - 1
					|| model[voxel.X, voxel.Y, (ushort)(voxel.Z + 1)] == 0)
					{
						renderer.Rect(
							x: x,
							y: y,
							index: voxel.Index,
							visibleFace: VisibleFace.Top,
							sizeX: scaleX,
							sizeY: 1);
						renderer.Rect(
							x: x,
							y: (ushort)(y + 1),
							index: voxel.Index,
							visibleFace: VisibleFace.Front,
							sizeX: scaleX,
							sizeY: (ushort)(scaleY - 1));
					}
					else
						renderer.Rect(
							x: x,
							y: y,
							index: voxel.Index,
							visibleFace: VisibleFace.Front,
							sizeX: scaleX,
							sizeY: scaleY);
	}
	public static Point OverheadSize(IModel model) => new(model.SizeX, model.SizeY);
	public static void Overhead(IModel model, IRectangleRenderer renderer, VisibleFace visibleFace = VisibleFace.Top)
	{
		ushort width = model.SizeX,
			height = model.SizeY;
		VoxelZ?[] grid = new VoxelZ?[width * height];
		foreach (Voxel voxel in model
			.Where(voxel => voxel.Index != 0))
			if (width * (height - voxel.Y - 1) + voxel.X is int i
				&& (grid[i] is not VoxelZ old
					|| old.Index == 0
					|| old.Z < voxel.Z))
				grid[i] = new VoxelZ(voxel);
		uint index = 0;
		for (ushort y = 0; y < height; y++)
			for (ushort x = 0; x < width; x++)
				if (grid[index++] is VoxelZ voxelZ && voxelZ.Index != 0)
					renderer.Rect(
						x: x,
						y: y,
						index: voxelZ.Index,
						visibleFace: visibleFace);
	}
	public static Point UnderneathSize(IModel model) => new(model.SizeX, model.SizeY);
	public static void Underneath(IModel model, IRectangleRenderer renderer, VisibleFace visibleFace = VisibleFace.Top)
	{
		ushort width = model.SizeX,
			height = model.SizeY;
		VoxelZ?[] grid = new VoxelZ?[width * height];
		foreach (Voxel voxel in model
			.Where(voxel => voxel.Index != 0))
			if (width * (height - voxel.Y - 1) + voxel.X is int i
				&& (grid[i] is not VoxelZ old
					|| old.Index == 0
					|| old.Z > voxel.Z))
				grid[i] = new VoxelZ(voxel);
		uint index = 0;
		for (ushort y = 0; y < height; y++)
			for (ushort x = 0; x < width; x++)
				if (grid[index++] is VoxelZ voxelZ && voxelZ.Index != 0)
					renderer.Rect(
						x: x,
						y: y,
						index: voxelZ.Index,
						visibleFace: visibleFace);
	}
	#endregion Straight
	#region Diagonal
	public static Point DiagonalSize(IModel model) => new(
		X: model.SizeX + model.SizeY,
		Y: model.SizeZ);
	public static Point DiagonalLocate(IModel model, Point3D point) => DiagonalLocate(model.SizeZ, point);
	public static Point DiagonalLocate(ushort sizeZ, Point3D point) => new(
		X: point.X + point.Y,
		Y: sizeZ - 1 - point.Z);
	public static void Diagonal(IModel model, IRectangleRenderer renderer)
	{
		ushort voxelWidth = model.SizeX,
			voxelDepth = model.SizeY,
			voxelHeight = model.SizeZ,
			pixelWidth = (ushort)(voxelWidth + voxelDepth);
		uint index;
		DistantShape?[] grid = new DistantShape?[pixelWidth * voxelHeight];
		foreach (Voxel voxel in model
			.Where(voxel => voxel.Index != 0))
		{
			index = (uint)(pixelWidth * (voxelHeight - voxel.Z - 1) + voxelDepth - voxel.Y - 1 + voxel.X);
			uint distance = (uint)voxel.X + voxel.Y;
			if (grid[index] is not DistantShape left
				|| left.Index == 0
				|| left.Distance > distance)
				grid[index] = new DistantShape
				{
					Distance = distance,
					Index = voxel.Index,
					VisibleFace = VisibleFace.Left,
				};
			if (grid[++index] is not DistantShape right
				|| right.Index == 0
				|| right.Distance > distance)
				grid[index] = new DistantShape
				{
					Distance = distance,
					Index = voxel.Index,
					VisibleFace = VisibleFace.Right,
				};
		}
		index = 0;
		for (ushort y = 0; y < voxelHeight; y++)
			for (ushort x = 0; x < pixelWidth; x++)
				if (grid[index++] is DistantShape rect && rect.Index != 0)
					renderer.Rect(
						x: x,
						y: y,
						index: rect.Index,
						visibleFace: rect.VisibleFace);
	}
	public static Point DiagonalPeakSize(IModel model, byte scaleX = 6, byte scaleY = 6) => new(
		X: (model.SizeX + model.SizeY) * scaleX,
		Y: model.SizeZ * scaleY);
	public static void DiagonalPeak(IModel model, IRectangleRenderer renderer, byte scaleX = 6, byte scaleY = 6)
	{
		if (scaleX < 1) throw new ArgumentOutOfRangeException(nameof(scaleX));
		if (scaleY < 1) throw new ArgumentOutOfRangeException(nameof(scaleY));
		ushort voxelWidth = model.SizeX,
			voxelDepth = model.SizeY,
			voxelHeight = model.SizeZ,
			pixelWidth = (ushort)(voxelWidth + voxelDepth);
		uint index;
		VoxelFace?[] grid = new VoxelFace?[pixelWidth * voxelHeight];
		foreach (Voxel voxel in model
			.Where(voxel => voxel.Index != 0))
		{
			index = (uint)(pixelWidth * (voxelHeight - voxel.Z - 1) + voxelDepth - voxel.Y - 1 + voxel.X);
			uint distance = (uint)voxel.X + voxel.Y;
			if (grid[index] is not VoxelFace left
				|| left.Voxel.Index == 0
				|| left.Distance > distance)
				grid[index] = new VoxelFace
				{
					Voxel = voxel,
					VisibleFace = VisibleFace.Left,
				};
			if (grid[++index] is not VoxelFace right
				|| right.Voxel.Index == 0
				|| right.Distance > distance)
				grid[index] = new VoxelFace
				{
					Voxel = voxel,
					VisibleFace = VisibleFace.Right,
				};
		}
		index = 0;
		pixelWidth *= scaleX;
		ushort pixelHeight = (ushort)(voxelHeight * scaleY);
		for (ushort y = 0; y < pixelHeight; y += scaleY)
			for (ushort x = 0; x < pixelWidth; x += scaleX)
				if (grid[index++] is VoxelFace face && face.Voxel.Index != 0)
					if (face.Voxel.Z >= voxelHeight - 1
						|| model[face.Voxel.X, face.Voxel.Y, (ushort)(face.Voxel.Z + 1)] == 0)
					{
						renderer.Rect(
							x: x,
							y: y,
							index: face.Voxel.Index,
							visibleFace: VisibleFace.Top,
							sizeX: scaleX,
							sizeY: 1);
						renderer.Rect(
							x: x,
							y: (ushort)(y + 1),
							index: face.Voxel.Index,
							visibleFace: face.VisibleFace,
							sizeX: scaleX,
							sizeY: (ushort)(scaleY - 1));
					}
					else
						renderer.Rect(
							x: x,
							y: y,
							index: face.Voxel.Index,
							visibleFace: face.VisibleFace,
							sizeX: scaleX,
							sizeY: scaleY);
	}
	public static Point AboveSize(IModel model) => new(
		X: model.SizeX,
		Y: model.SizeY + model.SizeZ);
	public static Point AboveLocate(IModel model, Point3D point) => new(
		X: point.X,
		Y: model.SizeY + model.SizeZ - 1 - point.Y - point.Z);
	/// <summary>
	/// Renders from a 3/4 perspective
	/// </summary>
	public static void Above(IModel model, IRectangleRenderer renderer)
	{
		ushort width = model.SizeX,
			depth = model.SizeY,
			height = model.SizeZ;
		uint pixelHeight = (uint)(depth + height), index;
		DistantShape?[] grid = new DistantShape?[width * pixelHeight];
		foreach (Voxel voxel in model
			.Where(voxel => voxel.Index != 0))
		{
			index = width * (pixelHeight - 2 - voxel.Y - voxel.Z) + voxel.X;
			uint distance = (uint)(height + voxel.Y - voxel.Z - 1);
			if (grid[index] is not DistantShape top
				|| top.Index == 0
				|| top.Distance > distance)
				grid[index] = new DistantShape
				{
					Distance = distance,
					Index = voxel.Index,
					VisibleFace = VisibleFace.Top,
				};
			index += width;
			if (grid[index] is not DistantShape front
				|| front.Index == 0
				|| front.Distance > distance)
				grid[index] = new DistantShape
				{
					Distance = distance,
					Index = voxel.Index,
					VisibleFace = VisibleFace.Front,
				};
		}
		index = 0;
		for (ushort y = 0; y < pixelHeight; y++)
			for (ushort x = 0; x < width; x++)
				if (grid[index++] is DistantShape rect && rect.Index != 0)
					renderer.Rect(
						x: x,
						y: y,
						index: rect.Index,
						visibleFace: rect.VisibleFace);
	}
	#endregion Diagonal
	#region Isometric
	public static Point IsoSize(IModel model) => new(
		X: 2 * (model.SizeX + model.SizeY),
		Y: 2 * (model.SizeX + model.SizeY) + 4 * model.SizeZ - 1);
	public static Point IsoLocate(IModel model, Point3D point) => new(
		// To move one x+ in voxels is x + 2, y - 2 in pixels.
		// To move one x- in voxels is x - 2, y + 2 in pixels.
		// To move one y+ in voxels is x - 2, y - 2 in pixels.
		// To move one y- in voxels is x + 2, y + 2 in pixels.
		// To move one z+ in voxels is y - 4 in pixels.
		// To move one z- in voxels is y + 4 in pixels.
		X: 2 * (model.SizeY + point.X - point.Y),
		Y: IsoSize(model).Y - 2 * (point.X + point.Y) - 4 * point.Z - 1);
	public static void Iso(IModel model, ITriangleRenderer renderer)
	{
		ushort voxelWidth = model.SizeX,
			voxelDepth = model.SizeY,
			voxelHeight = model.SizeZ,
			pixelHeight = (ushort)(2 * (voxelWidth + voxelDepth) + 4 * voxelHeight - 1);
		Dictionary<uint, DistantShape> dictionary = [];
		void Tri(ushort pixelX, ushort pixelY, uint distance, byte index, VisibleFace visibleFace = VisibleFace.Front)
		{
			if ((uint)((pixelY << 16) | pixelX) is uint key
				&& (!dictionary.TryGetValue(key, out DistantShape old)
					|| old.Distance > distance))
				dictionary[key] = new DistantShape
				{
					Distance = distance,
					Index = index,
					VisibleFace = visibleFace,
				};
		}
		foreach (Voxel voxel in model
			.Where(voxel => voxel.Index != 0))
		{
			uint distance = (uint)voxel.X + voxel.Y + voxelHeight - voxel.Z - 1;
			ushort pixelX = (ushort)(2 * (voxelDepth + voxel.X - voxel.Y)),
				pixelY = (ushort)(pixelHeight - 2 * (voxel.X + voxel.Y) - 4 * voxel.Z - 1);
			// 01
			//0011
			//2014
			//2244
			//2354
			//3355
			// 35
			Tri(//0
				pixelX: (ushort)(pixelX - 2),
				pixelY: (ushort)(pixelY - 6),
				distance: distance,
				index: voxel.Index,
				visibleFace: VisibleFace.Top);
			Tri(//1
				pixelX: pixelX,
				pixelY: (ushort)(pixelY - 6),
				distance: distance,
				index: voxel.Index,
				visibleFace: VisibleFace.Top);
			Tri(//2
				pixelX: (ushort)(pixelX - 2),
				pixelY: (ushort)(pixelY - 4),
				distance: distance,
				index: voxel.Index,
				visibleFace: VisibleFace.Left);
			Tri(//3
				pixelX: (ushort)(pixelX - 2),
				pixelY: (ushort)(pixelY - 2),
				distance: distance,
				index: voxel.Index,
				visibleFace: VisibleFace.Left);
			Tri(//4
				pixelX: pixelX,
				pixelY: (ushort)(pixelY - 4),
				distance: distance,
				index: voxel.Index,
				visibleFace: VisibleFace.Right);
			Tri(//5
				pixelX: pixelX,
				pixelY: (ushort)(pixelY - 2),
				distance: distance,
				index: voxel.Index,
				visibleFace: VisibleFace.Right);
		}
		byte oddWidth = (byte)(voxelWidth & 1);
		foreach (KeyValuePair<uint, DistantShape> triangle in dictionary)
			renderer.Tri(
				x: (ushort)triangle.Key,
				y: (ushort)(triangle.Key >> 16),
				right: (((triangle.Key >> 1) ^ (triangle.Key >> 17)) & 1u) == oddWidth,
				index: triangle.Value.Index,
				visibleFace: triangle.Value.VisibleFace);
	}
	public static Point IsoUnderneathSize(IModel model) => new(
		X: 2 * (model.SizeX + model.SizeY),
		Y: 2 * (model.SizeX + model.SizeY) - 1);
	public static Point IsoUnderneathLocate(IModel model, Point3D point) => new(
		// To move one x+ in voxels is x + 2, y - 2 in pixels.
		// To move one x- in voxels is x - 2, y + 2 in pixels.
		// To move one y+ in voxels is x - 2, y - 2 in pixels.
		// To move one y- in voxels is x + 2, y + 2 in pixels.
		X: 2 * (model.SizeY + point.X - point.Y),
		Y: 2 * (point.X + point.Y) - 1);
	public static void IsoUnderneath(IModel model, ITriangleRenderer renderer, VisibleFace visibleFace = VisibleFace.Top)
	{
		ushort width = model.SizeX,
			height = model.SizeY;
		VoxelZ?[] grid = new VoxelZ?[width * height];
		foreach (Voxel voxel in model)
			if (width * (height - voxel.Y - 1) + voxel.X is int i
				&& (grid[i] is not VoxelZ old
					|| old.Index == 0
					|| old.Z > voxel.Z))
				grid[i] = new VoxelZ(voxel);
		uint index = 0;
		for (ushort y = 0; y < height; y++)
			for (ushort x = 0; x < width; x++)
				if (grid[index++] is VoxelZ voxelZ && voxelZ.Index != 0)
					renderer.Diamond(
						x: (ushort)((x + y) << 1),
						y: (ushort)((width - x + y - 1) << 1),
						index: voxelZ.Index,
						visibleFace: visibleFace);
	}
	public static ushort IsoSlantWidth(int textureLength, ushort width = 0) => (ushort)((width < 1 ? (ushort)Math.Sqrt(textureLength >> 2) : width) << 1);
	public static ushort IsoSlantHeight(int textureLength, ushort width = 0)
	{
		if (width < 1) width = (ushort)Math.Sqrt(textureLength >> 2);
		return (ushort)((((textureLength >> 2) / width) << 2) + (width << 1) - 1);
	}
	public static void IsoSlantDown(ITriangleRenderer renderer, byte[] texture, ushort width = 0, byte threshold = 128)
	{
		if (width < 1) width = (ushort)Math.Sqrt(texture.Length >> 2);
		ushort height4 = (ushort)(((texture.Length >> 2) / width) << 2),
			width2 = (ushort)(width << 1);
		int index = 0;
		for (ushort y = 0; y < height4; y += 4)
			for (ushort x = 0; x < width2; x += 2, index += 4)
				if (texture[index + 3] is byte alpha && alpha >= threshold)
				{
					uint color = (uint)texture[index] << 24 | (uint)texture[index + 1] << 16 | (uint)texture[index + 2] << 8 | alpha;
					renderer.Tri(
						x: x,
						y: (ushort)(y + x),
						right: true,
						color: color);
					renderer.Tri(
						x: x,
						y: (ushort)(y + x + 2),
						right: false,
						color: color);
				}
	}
	public static void IsoSlantUp(ITriangleRenderer renderer, byte[] texture, ushort width = 0, byte threshold = 128)
	{
		if (width < 1) width = (ushort)Math.Sqrt(texture.Length >> 2);
		ushort height4 = (ushort)(((texture.Length >> 2) / width) << 2),
			width2 = (ushort)(width << 1);
		int index = 0;
		for (ushort y = 0; y < height4; y += 4)
			for (ushort x = 0; x < width2; x += 2, index += 4)
				if (texture[index + 3] is byte alpha && alpha >= threshold)
				{
					uint color = (uint)texture[index] << 24 | (uint)texture[index + 1] << 16 | (uint)texture[index + 2] << 8 | alpha;
					renderer.Tri(
						x: x,
						y: (ushort)(width2 + y - x - 2),
						right: false,
						color: color);
					renderer.Tri(
						x: x,
						y: (ushort)(width2 + y - x),
						right: true,
						color: color);
				}
	}
	public static ushort IsoTileWidth(int textureLength, ushort width = 0)
	{
		if (width < 1) width = (ushort)Math.Sqrt(textureLength >> 2);
		ushort height = (ushort)((textureLength >> 2) / width);
		return (ushort)((width + height) << 1);
	}
	public static ushort IsoTileHeight(int textureLength, ushort width = 0) => (ushort)(IsoTileWidth(textureLength, width) - 1);
	public static void IsoTile(ITriangleRenderer renderer, byte[] texture, ushort width = 0, byte threshold = 128)
	{
		if (width < 1) width = (ushort)Math.Sqrt(texture.Length >> 2);
		ushort width2 = (ushort)(width << 1),
			height2 = (ushort)(((texture.Length >> 2) / width) << 1);
		int index = 0;
		for (ushort xStart = 0, yStart = (ushort)(width2 - 2);
			yStart < width2 + height2 - 2;
			xStart += 2, yStart += 2)
			for (ushort x = xStart, y = yStart;
				x < xStart + width2;
				x += 2, y -= 2, index += 4)
				if (texture[index + 3] is byte alpha && alpha >= threshold)
					renderer.Diamond(
						x: x,
						y: y,
						color: (uint)texture[index] << 24 | (uint)texture[index + 1] << 16 | (uint)texture[index + 2] << 8 | alpha);
	}
	#endregion Isometric
	#region Stacked
	public static Point ZSliceSize(IModel model, double radians = 0d, byte scaleX = 1, byte scaleY = 1)
	{
		PixelDraw.RotatedSize(
			width: model.SizeX,
			height: model.SizeY,
			out ushort rotatedWidth,
			out ushort rotatedHeight,
			radians: radians,
			scaleX: scaleX,
			scaleY: scaleY);
		return new(X: rotatedWidth, Y: rotatedHeight);
	}
	public static Point ZSliceLocate(IModel model, Point point, double radians = 0d, byte scaleX = 1, byte scaleY = 1)
	{
		PixelDraw.RotatedLocate(
			width: model.SizeX,
			height: model.SizeY,
			x: point.X,
			y: model.SizeY - 1 - point.Y,
			out int rotatedX,
			out int rotatedY,
			radians: radians,
			scaleX: scaleX,
			scaleY: scaleY);
		return new(X: rotatedX, Y: rotatedY);
	}
	public static void ZSlice(IModel model, IRectangleRenderer renderer, ushort z = 0, bool peak = false, VisibleFace visibleFace = VisibleFace.Front)
	{
		for (ushort y = 0; y < model.SizeY; y++)
			for (ushort x = 0; x < model.SizeX; x++)
				if (model[x, (ushort)(model.SizeY - 1 - y), z] is byte index && index != 0)
					renderer.Rect(
						x: x,
						y: y,
						index: index,
						visibleFace: peak && (z == model.SizeZ - 1
							|| model[x, (ushort)(model.SizeY - 1 - y), (ushort)(z + 1)] == 0) ?
							VisibleFace.Top
							: visibleFace);
	}
	public static void ZSlice(IModel model, IRectangleRenderer renderer, double radians, ushort z = 0, byte scaleX = 1, byte scaleY = 1, bool peak = false, VisibleFace visibleFace = VisibleFace.Front)
	{
		if (z >= model.SizeZ) throw new ArgumentOutOfRangeException(nameof(z));
		if (scaleX < 1) throw new ArgumentOutOfRangeException(nameof(scaleX));
		if (scaleY < 1) throw new ArgumentOutOfRangeException(nameof(scaleY));
		if (model.SizeX > ushort.MaxValue / scaleX)
			throw new OverflowException("Scaled width exceeds maximum allowed size.");
		if (model.SizeY > ushort.MaxValue / scaleY)
			throw new OverflowException("Scaled height exceeds maximum allowed size.");
		ushort scaledWidth = (ushort)(model.SizeX * scaleX),
			scaledHeight = (ushort)(model.SizeY * scaleY),
			halfScaledWidth = (ushort)(scaledWidth >> 1),
			halfScaledHeight = (ushort)(scaledHeight >> 1);
		radians %= PixelDraw.Tau;
		double cos = Math.Cos(radians),
			sin = Math.Sin(radians),
			absCos = Math.Abs(cos),
			absSin = Math.Abs(sin);
		uint rWidth = (uint)(scaledWidth * absCos + scaledHeight * absSin),
			rHeight = (uint)(scaledWidth * absSin + scaledHeight * absCos);
		if (rWidth > ushort.MaxValue)
			throw new OverflowException("Rotated width exceeds maximum allowed size.");
		if (rHeight > ushort.MaxValue)
			throw new OverflowException("Rotated height exceeds maximum allowed size.");
		ushort rotatedWidth = (ushort)rWidth,
			rotatedHeight = (ushort)rHeight,
			halfRotatedWidth = (ushort)(rotatedWidth >> 1),
			halfRotatedHeight = (ushort)(rotatedHeight >> 1);
		double offsetX = halfScaledWidth - cos * halfRotatedWidth - sin * halfRotatedHeight,
			offsetY = halfScaledHeight - cos * halfRotatedHeight + sin * halfRotatedWidth;
		bool isNearZero = absCos < 1e-10 || absSin < 1e-10;
		double[] cornerX = isNearZero ? null : [-halfScaledWidth, halfScaledWidth, halfScaledWidth, -halfScaledWidth],
			cornerY = isNearZero ? null : [-halfScaledHeight, -halfScaledHeight, halfScaledHeight, halfScaledHeight];
		if (!isNearZero)
			for (byte cornerIndex = 0; cornerIndex < 4; cornerIndex++)
			{
				double unrotatedX = cornerX[cornerIndex];
				cornerX[cornerIndex] = unrotatedX * cos - cornerY[cornerIndex] * sin + halfRotatedWidth;
				cornerY[cornerIndex] = unrotatedX * sin + cornerY[cornerIndex] * cos + halfRotatedHeight;
			}
		for (ushort y = 0; y < rotatedHeight; y++)
		{
			ushort startX = 0, endX = rotatedWidth;
			if (!isNearZero)
			{
				double? minX = null, maxX = null;
				for (byte cornerIndex = 0; cornerIndex < 4; cornerIndex++)
				{
					if (Math.Abs(cornerY[cornerIndex] - y) <= 0.5d)
					{
						minX = minX.HasValue ? Math.Min(minX.Value, cornerX[cornerIndex]) : cornerX[cornerIndex];
						maxX = maxX.HasValue ? Math.Max(maxX.Value, cornerX[cornerIndex]) : cornerX[cornerIndex];
					}
					byte nextCornerIndex = (byte)((cornerIndex + 1) % 4);
					double currentY = cornerY[cornerIndex],
						nextY = cornerY[nextCornerIndex];
					if ((currentY <= y && nextY >= y) || (currentY >= y && nextY <= y))
					{
						double intersectX = cornerX[cornerIndex]
							+ (y - currentY) / (nextY - currentY)
							* (cornerX[nextCornerIndex] - cornerX[cornerIndex]);
						minX = minX.HasValue ? Math.Min(minX.Value, intersectX) : intersectX;
						maxX = maxX.HasValue ? Math.Max(maxX.Value, intersectX) : intersectX;
					}
				}
				if (minX.HasValue) startX = (ushort)Math.Floor(minX.Value);
				if (maxX.HasValue) endX = (ushort)Math.Ceiling(maxX.Value);
			}
			for (ushort x = startX; x < endX; x++)
			{
				double sourceX = (x * cos + y * sin + offsetX) / scaleX,
					sourceY = (y * cos - x * sin + offsetY) / scaleY;
				if (sourceX >= 0d && sourceX < model.SizeX && sourceY >= 0d && sourceY < model.SizeY)
				{
					ushort voxelX = (ushort)Math.Floor(sourceX),
						voxelY = (ushort)(model.SizeY - 1 - Math.Floor(sourceY)),
						voxelZ = z;
					if (model[voxelX, voxelY, voxelZ] is byte index && index != 0)
						renderer.Rect(
							x: x,
							y: y,
							index: index,
							visibleFace: peak && (voxelZ == model.SizeZ - 1 || model[voxelX, voxelY, (ushort)(voxelZ + 1)] == 0) ?
								VisibleFace.Top
								: visibleFace);
				}
			}
		}
	}
	public static Point StackedSize(this IModel model, double radians = 0d, byte scaleX = 1, byte scaleY = 1, byte scaleZ = 1)
	{
		Point zSliceSize = ZSliceSize(
			model: model,
			radians: radians,
			scaleX: scaleX,
			scaleY: scaleY);
		return new Point(
			X: zSliceSize.X,
			Y: zSliceSize.Y + (model.SizeZ * scaleZ) - 1);
	}
	public static Point StackedLocate(IModel model, Point3D point, double radians = 0d, byte scaleX = 1, byte scaleY = 1, byte scaleZ = 1)
	{
		if (scaleZ < 1) throw new ArgumentOutOfRangeException(nameof(scaleZ));
		Point sliceLocation = ZSliceLocate(
			model: model,
			point: new(point),
			radians: radians,
			scaleX: scaleX,
			scaleY: scaleY);
		return new(
			X: sliceLocation.X,
			Y: sliceLocation.Y + ((model.SizeZ - 1 - point.Z) * scaleZ));
	}
	public static void Stacked(IModel model, IRectangleRenderer renderer, double radians = 0d, byte scaleX = 1, byte scaleY = 1, byte scaleZ = 1, bool peak = false, VisibleFace visibleFace = VisibleFace.Front)
	{
		if (scaleX < 1) throw new ArgumentOutOfRangeException(nameof(scaleX));
		if (scaleY < 1) throw new ArgumentOutOfRangeException(nameof(scaleY));
		if (scaleZ < 1) throw new ArgumentOutOfRangeException(nameof(scaleZ));
		OffsetRenderer offsetRenderer = new()
		{
			RectangleRenderer = renderer,
		};
		if (scaleZ == 1)
		{
			offsetRenderer.OffsetY = model.SizeZ - 1;
			for (ushort z = 0; z < model.SizeZ; z++, offsetRenderer.OffsetY--)
				ZSlice(
					model: model,
					renderer: offsetRenderer,
					radians: radians,
					z: z,
					scaleX: scaleX,
					scaleY: scaleY,
					peak: peak,
					visibleFace: visibleFace);
			return;
		}
		(MemoryRenderer, MemoryRenderer)[] memories = [.. Enumerable.Range(0, model.SizeZ)
			.Parallelize(z => {
				MemoryRenderer memoryRenderer = [];
				ZSlice(
					model: model,
					renderer: memoryRenderer,
					radians: radians,
					z: (ushort)z,
					scaleX: scaleX,
					scaleY: scaleY,
					visibleFace: visibleFace);
				if (!peak)
					return (memoryRenderer, memoryRenderer);
				MemoryRenderer peakRenderer = [];
				ZSlice(
					model: model,
					renderer: peakRenderer,
					radians: radians,
					z: (ushort)z,
					scaleX: scaleX,
					scaleY: scaleY,
					peak: true,
					visibleFace: visibleFace);
				return (memoryRenderer, peakRenderer);
			})];
		offsetRenderer.OffsetY = (ushort)((model.SizeZ * scaleZ) - 1);
		for (ushort z = 0; z < model.SizeZ; z++)
			for (ushort offsetY = 0; offsetY < scaleZ; offsetY++, offsetRenderer.OffsetY--)
				if (offsetY == scaleZ - 1)
				{
					memories[z].Item2.Rect(offsetRenderer);
					if (z < model.SizeZ - 1)
						memories[z + 1].Item1.Rect(offsetRenderer);
				}
				else
					memories[z].Item1.Rect(offsetRenderer);
	}
	public static Point ZSlicesSize(IModel model) => new(
		X: model.SizeX * model.SizeZ,
		Y: model.SizeY);
	public static Point ZSlicesLocate(IModel model, Point3D point) => new(
		X: model.SizeX * point.Z + point.X,
		Y: point.Y);
	public static void ZSlices(IModel model, IRectangleRenderer renderer, bool peak = false, VisibleFace visibleFace = VisibleFace.Front)
	{
		OffsetRenderer offsetRenderer = new()
		{
			RectangleRenderer = renderer,
		};
		for (ushort z = 0; z < model.SizeZ; z++, offsetRenderer.OffsetX += model.SizeX)
			ZSlice(
				model: model,
				renderer: offsetRenderer,
				z: z,
				visibleFace: visibleFace,
				peak: peak);
	}
	#endregion Stacked
}
