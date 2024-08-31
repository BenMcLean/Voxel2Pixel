using System;
using System.Collections.Generic;
using System.Linq;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;
using Voxel2Pixel.Render;

namespace Voxel2Pixel.Draw
{
	/// <summary>
	/// All methods in this static class are actually stateless functions, meaning that they do not reference any modifiable variables besides their parameters. This makes them as thread-safe as their parameters.
	/// I have been forced into a situation where X and Y mean something different in 2D space from what they mean in 3D space. Not only do the coordinates not match, but 3D is upside down when compared to 2D. I hate this. I hate it so much. But I'm stuck with it if I want my software to be interoperable with other existing software.
	/// In 2D space for pixels, X+ means east/right, Y+ means down. This is dictated by how 2D raster graphics are typically stored.
	/// In 3D space for voxels, I'm following the MagicaVoxel convention, which is Z+up, right-handed, so X+ means right/east, Y+ means forwards/north and Z+ means up.
	/// </summary>
	public static class VoxelDraw
	{
		#region Perspectives
		public static void Draw(this IModel model, IRenderer renderer, Perspective perspective, byte peakScaleX = 6, byte peakScaleY = 6, double radians = 0d)
		{
			switch (perspective)
			{
				default:
				case Perspective.Front:
					Front(model, renderer);
					break;
				case Perspective.FrontPeak:
					FrontPeak(model, renderer, peakScaleX, peakScaleY);
					break;
				case Perspective.Overhead:
					Overhead(model, renderer);
					break;
				case Perspective.Underneath:
					Underneath(model, renderer);
					break;
				case Perspective.Diagonal:
					Diagonal(model, renderer);
					break;
				case Perspective.DiagonalPeak:
					DiagonalPeak(model, renderer, peakScaleX, peakScaleY);
					break;
				case Perspective.Above:
					Above(model, renderer);
					break;
				case Perspective.Iso:
					Iso(model, renderer);
					break;
				case Perspective.IsoShadow:
					IsoShadow(model, renderer);
					break;
				case Perspective.Stacked:
					Stacked(model, renderer, radians);
					break;
				case Perspective.ZSlices:
					ZSlices(model, renderer);
					break;
			}
		}
		public static Point Size(this IModel model, Perspective perspective, byte peakScaleX = 6, byte peakScaleY = 6, double radians = 0d) => perspective switch
		{
			Perspective.FrontPeak => FrontPeakSize(model, peakScaleX, peakScaleY),
			Perspective.Overhead => OverheadSize(model),
			Perspective.Underneath => UnderneathSize(model),
			Perspective.Diagonal => DiagonalSize(model),
			Perspective.DiagonalPeak => DiagonalPeakSize(model, peakScaleX, peakScaleY),
			Perspective.Above => AboveSize(model),
			Perspective.Iso => IsoSize(model),
			Perspective.IsoShadow => IsoShadowSize(model),
			Perspective.Stacked => StackedSize(model, radians),
			Perspective.ZSlices => ZSlicesSize(model),
			_ => FrontSize(model),
		};
		public static Point Locate(this IModel model, Perspective perspective, Point3D point, byte peakScaleX = 6, byte peakScaleY = 6, double radians = 0d)
		{
			Point result;
			switch (perspective)
			{
				default:
				case Perspective.Front:
				case Perspective.FrontPeak:
					result = FrontLocate(model, point);
					break;
				case Perspective.Overhead:
				case Perspective.Underneath:
					return new Point(point.X, point.Y);
				case Perspective.Diagonal:
				case Perspective.DiagonalPeak:
					result = DiagonalLocate(model, point);
					break;
				case Perspective.Above:
					return AboveLocate(model, point);
				case Perspective.Iso:
					return IsoLocate(model, point);
				case Perspective.IsoShadow:
					return IsoShadowLocate(model, point);
				case Perspective.Stacked:
					return StackedLocate(model, point, radians);
				case Perspective.ZSlices:
					return ZSlicesLocate(model, point);
			}
			return perspective.IsPeak() ? new()
			{
				X = result.X * peakScaleX,
				Y = result.Y * peakScaleY,
			} : result;
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
			VoxelY[] grid = new VoxelY[width * height];
			foreach (Voxel voxel in model
				.Where(voxel => voxel.Index != 0))
				if (width * (height - voxel.Z - 1) + voxel.X is int i
					&& (!(grid[i] is VoxelY old)
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
		public static Point FrontPeakSize(IModel model, ushort peakScaleX = 6, ushort peakScaleY = 6) => new(model.SizeX * peakScaleX, model.SizeZ * peakScaleY);
		public static void FrontPeak(IModel model, IRectangleRenderer renderer, byte scaleX = 6, byte scaleY = 6)
		{
			ushort voxelWidth = model.SizeX,
				voxelHeight = model.SizeZ;
			Voxel[] grid = new Voxel[voxelWidth * voxelHeight];
			foreach (Voxel voxel in model
				.Where(voxel => voxel.Index != 0))
				if (voxelWidth * (voxelHeight - voxel.Z - 1) + voxel.X is int i
					&& (!(grid[i] is Voxel old)
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
			VoxelZ[] grid = new VoxelZ[width * height];
			foreach (Voxel voxel in model
				.Where(voxel => voxel.Index != 0))
				if (width * (height - voxel.Y - 1) + voxel.X is int i
					&& (!(grid[i] is VoxelZ old)
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
			VoxelZ[] grid = new VoxelZ[width * height];
			foreach (Voxel voxel in model
				.Where(voxel => voxel.Index != 0))
				if (width * (height - voxel.Y - 1) + voxel.X is int i
					&& (!(grid[i] is VoxelZ old)
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
			DistantShape[] grid = new DistantShape[pixelWidth * voxelHeight];
			foreach (Voxel voxel in model
				.Where(voxel => voxel.Index != 0))
			{
				index = (uint)(pixelWidth * (voxelHeight - voxel.Z - 1) + voxelDepth - voxel.Y - 1 + voxel.X);
				uint distance = (uint)voxel.X + voxel.Y;
				if (!(grid[index] is DistantShape left)
					|| left.Index == 0
					|| left.Distance > distance)
					grid[index] = new DistantShape
					{
						Distance = distance,
						Index = voxel.Index,
						VisibleFace = VisibleFace.Left,
					};
				if (!(grid[++index] is DistantShape right)
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
		public static Point DiagonalPeakSize(IModel model, byte peakScaleX = 6, byte peakScaleY = 6) => new(
			X: (model.SizeX + model.SizeY) * peakScaleX,
			Y: model.SizeZ * peakScaleY);
		public static void DiagonalPeak(IModel model, IRectangleRenderer renderer, byte scaleX = 6, byte scaleY = 6)
		{
			ushort voxelWidth = model.SizeX,
				voxelDepth = model.SizeY,
				voxelHeight = model.SizeZ,
				pixelWidth = (ushort)(voxelWidth + voxelDepth);
			uint index;
			VoxelFace[] grid = new VoxelFace[pixelWidth * voxelHeight];
			foreach (Voxel voxel in model
				.Where(voxel => voxel.Index != 0))
			{
				index = (uint)(pixelWidth * (voxelHeight - voxel.Z - 1) + voxelDepth - voxel.Y - 1 + voxel.X);
				uint distance = (uint)voxel.X + voxel.Y;
				if (!(grid[index] is VoxelFace left)
					|| left.Voxel.Index == 0
					|| left.Distance > distance)
					grid[index] = new VoxelFace
					{
						Voxel = voxel,
						VisibleFace = VisibleFace.Left,
					};
				if (!(grid[++index] is VoxelFace right)
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
			DistantShape[] grid = new DistantShape[width * pixelHeight];
			foreach (Voxel voxel in model
				.Where(voxel => voxel.Index != 0))
			{
				index = width * (pixelHeight - 2 - voxel.Y - voxel.Z) + voxel.X;
				uint distance = (uint)(height + voxel.Y - voxel.Z - 1);
				if (!(grid[index] is DistantShape top)
					|| top.Index == 0
					|| top.Distance > distance)
					grid[index] = new DistantShape
					{
						Distance = distance,
						Index = voxel.Index,
						VisibleFace = VisibleFace.Top,
					};
				index += width;
				if (!(grid[index] is DistantShape front)
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
		public static Point IsoShadowSize(IModel model) => new(
			X: 2 * (model.SizeX + model.SizeY),
			Y: 2 * (model.SizeX + model.SizeY) - 1);
		public static Point IsoShadowLocate(IModel model, Point3D point) => new(
			// To move one x+ in voxels is x + 2, y - 2 in pixels.
			// To move one x- in voxels is x - 2, y + 2 in pixels.
			// To move one y+ in voxels is x - 2, y - 2 in pixels.
			// To move one y- in voxels is x + 2, y + 2 in pixels.
			X: 2 * (model.SizeY + point.X - point.Y),
			Y: 2 * (point.X + point.Y) - 1);
		public static void IsoShadow(IModel model, ITriangleRenderer renderer, VisibleFace visibleFace = VisibleFace.Top)
		{
			ushort width = model.SizeX,
				height = model.SizeY;
			VoxelZ[] grid = new VoxelZ[width * height];
			foreach (Voxel voxel in model
				.Where(voxel => voxel.Index != 0))
				if (width * (height - voxel.Y - 1) + voxel.X is int i
					&& (!(grid[i] is VoxelZ old)
						|| old.Index == 0
						|| old.Z > voxel.Z))
					grid[i] = new VoxelZ(voxel);
			uint index = 0;
			for (ushort y = 0; y < height; y++)
				for (ushort x = 0; x < width; x++)
					if (grid[index++] is VoxelZ voxelZ && voxelZ.Index != 0)
						renderer.Diamond(
							x: (ushort)(2 * (x + y)),
							y: (ushort)(2 * (width - x + y - 1)),
							index: voxelZ.Index,
							visibleFace: visibleFace);
		}
		public static ushort IsoSlantWidth(int textureLength, ushort width = 0) => (ushort)((width < 1 ? (ushort)Math.Sqrt(textureLength >> 2) : width) << 1);
		public static ushort IsoSlantHeight(int textureLength, ushort width = 0)
		{
			if (width < 1)
				width = (ushort)Math.Sqrt(textureLength >> 2);
			return (ushort)((((textureLength >> 2) / width) << 2) + (width << 1) - 1);
		}
		public static void IsoSlantDown(ITriangleRenderer renderer, byte[] texture, ushort width = 0, byte threshold = 128)
		{
			if (width < 1)
				width = (ushort)Math.Sqrt(texture.Length >> 2);
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
			if (width < 1)
				width = (ushort)Math.Sqrt(texture.Length >> 2);
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
			if (width < 1)
				width = (ushort)Math.Sqrt(textureLength >> 2);
			ushort height = (ushort)((textureLength >> 2) / width);
			return (ushort)((width + height) << 1);
		}
		public static ushort IsoTileHeight(int textureLength, ushort width = 0) => (ushort)(IsoTileWidth(textureLength, width) - 1);
		public static void IsoTile(ITriangleRenderer renderer, byte[] texture, ushort width = 0, byte threshold = 128)
		{
			if (width < 1)
				width = (ushort)Math.Sqrt(texture.Length >> 2);
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
		public static Point ZSliceSize(IModel model, double radians = 0d)
		{
			double cos = Math.Abs(Math.Cos(radians)),
				sin = Math.Abs(Math.Sin(radians));
			return new Point(
				X: (int)(model.SizeX * cos + model.SizeY * sin),
				Y: (int)(model.SizeX * sin + model.SizeY * cos));
		}
		public static void ZSlice(IModel model, IRectangleRenderer renderer, ushort z = 0, VisibleFace visibleFace = VisibleFace.Front)
		{
			for (ushort y = 0; y < model.SizeY; y++)
				for (ushort x = 0; x < model.SizeX; x++)
					if (model[x, (ushort)(model.SizeY - 1 - y), z] is byte index && index != 0)
						renderer.Rect(
							x: x,
							y: y,
							index: index,
							visibleFace: visibleFace);
		}
		public static void ZSlice(IModel model, IRectangleRenderer renderer, double radians, ushort z = 0, VisibleFace visibleFace = VisibleFace.Front)
		{
			Point size = ZSliceSize(model, radians);
			double cos = Math.Cos(radians),
				sin = Math.Sin(radians),
				offsetX = (model.SizeX >> 1) - cos * (size.X >> 1) - sin * (size.Y >> 1),
				offsetY = (model.SizeY >> 1) - cos * (size.Y >> 1) + sin * (size.X >> 1);
			for (ushort y = 0; y < size.Y; y++)
				for (ushort x = 0; x < size.X; x++)
					if ((int)(x * cos + y * sin + offsetX) is int oldX
						&& oldX >= 0 && oldX < model.SizeX
						&& (int)(y * cos - x * sin + offsetY) is int oldY
						&& oldY >= 0 && oldY < model.SizeY
						&& model[(ushort)oldX, (ushort)(model.SizeY - 1 - oldY), z] is byte index && index != 0)
						renderer.Rect(
							x: x,
							y: y,
							index: index,
							visibleFace: visibleFace);
		}
		public static Point StackedSize(this IModel model, double radians = 0d)
		{
			double cos = Math.Abs(Math.Cos(radians)),
				sin = Math.Abs(Math.Sin(radians));
			return new Point(
				X: (int)(model.SizeX * cos + model.SizeY * sin),
				Y: (int)(model.SizeX * sin + model.SizeY * cos) + model.SizeZ - 1);
		}
		public static Point StackedLocate(IModel model, Point3D point, double radians = 0d)
		{
			double cos = Math.Cos(radians),
				sin = Math.Sin(radians);
			ushort width = (ushort)(model.SizeX * Math.Abs(cos) + model.SizeY * Math.Abs(sin)),
				height = (ushort)(model.SizeX * Math.Abs(sin) + model.SizeY * Math.Abs(cos));
			double offsetX = (model.SizeX >> 1) - cos * (width >> 1) - sin * (height >> 1),
				offsetY = (model.SizeY >> 1) - cos * (height >> 1) + sin * (width >> 1);
			return new Point(
				X: (int)(cos * (point.X - offsetX) - sin * (point.Y - offsetY)),
				Y: (int)(sin * (point.X - offsetX) + cos * (point.Y - offsetY) + model.SizeX - 1 - point.Z));
		}
		public static void Stacked(IModel model, IRectangleRenderer renderer, double radians = 0d, VisibleFace visibleFace = VisibleFace.Front)
		{
			OffsetRenderer offsetRenderer = new()
			{
				RectangleRenderer = renderer,
				OffsetY = model.SizeZ - 1,
			};
			for (ushort z = 0; z < model.SizeZ; z++, offsetRenderer.OffsetY--)
				ZSlice(
					model: model,
					renderer: offsetRenderer,
					radians: radians,
					z: z,
					visibleFace: visibleFace);
		}
		public static Point ZSlicesSize(IModel model) => new(
			X: model.SizeX * model.SizeZ,
			Y: model.SizeY);
		public static Point ZSlicesLocate(IModel model, Point3D point) => new(
			X: model.SizeX * point.Z + point.X,
			Y: point.Y);
		public static void ZSlices(IModel model, IRectangleRenderer renderer, VisibleFace visibleFace = VisibleFace.Front)
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
					visibleFace: visibleFace);
		}
		#endregion Stacked
	}
}
