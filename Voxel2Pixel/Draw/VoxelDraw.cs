using System;
using System.Collections.Generic;
using System.Linq;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;

namespace Voxel2Pixel.Draw
{
	/// <summary>
	/// I have been forced into a situation where X and Y mean something different in 2D space from what they mean in 3D space. Not only do the coordinates not match, but 3D is upside down when compared to 2D. I hate this. I hate it so much. But I'm stuck with it if I want my software to be interoperable with other existing software.
	/// In 2D space for pixels, X+ means east/right, Y+ means down. This is dictated by how 2D raster graphics are typically stored.
	/// In 3D space for voxels, I'm following the MagicaVoxel convention, which is Z+up, right-handed, so X+ means right/east, Y+ means forwards/north and Z+ means up.
	/// </summary>
	public static class VoxelDraw
	{
		#region Perspectives
		public static void Draw(Perspective perspective, IModel model, ITriangleRenderer renderer, byte peakScaleX = 6, byte peakScaleY = 6)
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
			}
		}
		public static ushort Width(Perspective perspective, IModel model, byte peakScaleX = 6) => perspective switch
		{
			Perspective.FrontPeak => (ushort)(FrontWidth(model) * peakScaleX),
			Perspective.Overhead => OverheadWidth(model),
			Perspective.Underneath => UnderneathWidth(model),
			Perspective.Diagonal => DiagonalWidth(model),
			Perspective.DiagonalPeak => (ushort)(DiagonalWidth(model) * peakScaleX),
			Perspective.Above => AboveWidth(model),
			Perspective.Iso => IsoWidth(model),
			Perspective.IsoShadow => IsoShadowWidth(model),
			_ => FrontWidth(model),
		};
		public static ushort Height(Perspective perspective, IModel model, byte peakScaleY = 6) => perspective switch
		{
			Perspective.FrontPeak => (ushort)(FrontHeight(model) * peakScaleY),
			Perspective.Overhead => OverheadHeight(model),
			Perspective.Underneath => UnderneathHeight(model),
			Perspective.Diagonal => DiagonalHeight(model),
			Perspective.DiagonalPeak => (ushort)(DiagonalHeight(model) * peakScaleY),
			Perspective.Above => AboveHeight(model),
			Perspective.Iso => IsoHeight(model),
			Perspective.IsoShadow => IsoShadowHeight(model),
			_ => FrontHeight(model),
		};
		public static void Locate(Perspective perspective, out int pixelX, out int pixelY, IModel model, int voxelX = 0, int voxelY = 0, int voxelZ = 0, byte peakScaleX = 6, byte peakScaleY = 6)
		{
			switch (perspective)
			{
				default:
				case Perspective.Front:
				case Perspective.FrontPeak:
					FrontLocate(out pixelX, out pixelY, model, voxelX, voxelZ);
					break;
				case Perspective.Overhead:
					pixelX = voxelX;
					pixelY = voxelY;
					break;
				case Perspective.Underneath:
					pixelX = voxelX;
					pixelY = voxelY;
					break;
				case Perspective.Diagonal:
				case Perspective.DiagonalPeak:
					DiagonalLocate(out pixelX, out pixelY, model, voxelX, voxelY, voxelZ);
					break;
				case Perspective.Above:
					AboveLocate(out pixelX, out pixelY, model, voxelX, voxelY, voxelZ);
					break;
				case Perspective.Iso:
					IsoLocate(out pixelX, out pixelY, model, voxelX, voxelY, voxelZ);
					break;
				case Perspective.IsoShadow:
					IsoShadowLocate(out pixelX, out pixelY, model, voxelX, voxelY, voxelZ);
					break;
			}
			if (perspective.IsPeak())
			{
				pixelX *= peakScaleX;
				pixelY *= peakScaleY;
			}
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
		public static ushort FrontWidth(IModel model) => model.SizeX;
		public static ushort FrontHeight(IModel model) => model.SizeZ;
		public static void FrontLocate(out int pixelX, out int pixelY, IModel model, int voxelX = 0, int voxelZ = 0) => FrontLocate(out pixelX, out pixelY, model.SizeZ, voxelX, voxelZ);
		public static void FrontLocate(out int pixelX, out int pixelY, ushort sizeZ, int voxelX = 0, int voxelZ = 0)
		{
			pixelX = voxelX;
			pixelY = sizeZ - 1 - voxelZ;
		}
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
		public static ushort OverheadWidth(IModel model) => model.SizeX;
		public static ushort OverheadHeight(IModel model) => model.SizeY;
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
		public static ushort UnderneathWidth(IModel model) => model.SizeX;
		public static ushort UnderneathHeight(IModel model) => model.SizeY;
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
		public static ushort DiagonalWidth(IModel model) => (ushort)(model.SizeX + model.SizeY);
		public static ushort DiagonalHeight(IModel model) => model.SizeZ;
		public static void DiagonalLocate(out int pixelX, out int pixelY, IModel model, int voxelX = 0, int voxelY = 0, int voxelZ = 0) => DiagonalLocate(out pixelX, out pixelY, model.SizeZ, voxelX, voxelY, voxelZ);
		public static void DiagonalLocate(out int pixelX, out int pixelY, ushort sizeZ, int voxelX = 0, int voxelY = 0, int voxelZ = 0)
		{
			pixelX = voxelX + voxelY;
			pixelY = sizeZ - 1 - voxelZ;
		}
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
		public static ushort DiagonalPeakWidth(IModel model, byte scaleX = 6) => (ushort)((model.SizeX + model.SizeY) * scaleX);
		public static ushort DiagonalPeakHeight(IModel model, byte scaleY = 6) => (ushort)(model.SizeZ * scaleY);
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
		public static ushort AboveWidth(IModel model) => model.SizeX;
		public static ushort AboveHeight(IModel model) => (ushort)(model.SizeY + model.SizeZ);
		public static void AboveLocate(out int pixelX, out int pixelY, IModel model, int voxelX = 0, int voxelY = 0, int voxelZ = 0)
		{
			pixelX = voxelX;
			pixelY = AboveHeight(model) - 1 - voxelY - voxelZ;
		}
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
		public static ushort IsoWidth(IModel model) => (ushort)(2 * (model.SizeX + model.SizeY));
		public static ushort IsoHeight(IModel model) => (ushort)(2 * (model.SizeX + model.SizeY) + 4 * model.SizeZ - 1);
		public static void IsoLocate(out int pixelX, out int pixelY, IModel model, ushort[] voxelCoordinates) => IsoLocate(out pixelX, out pixelY, model, voxelCoordinates[0], voxelCoordinates[1], voxelCoordinates[2]);
		public static void IsoLocate(out int pixelX, out int pixelY, IModel model, int voxelX = 0, int voxelY = 0, int voxelZ = 0)
		{
			// To move one x+ in voxels is x + 2, y - 2 in pixels.
			// To move one x- in voxels is x - 2, y + 2 in pixels.
			// To move one y+ in voxels is x - 2, y - 2 in pixels.
			// To move one y- in voxels is x + 2, y + 2 in pixels.
			// To move one z+ in voxels is y - 4 in pixels.
			// To move one z- in voxels is y + 4 in pixels.
			pixelX = 2 * (model.SizeY + voxelX - voxelY);
			pixelY = IsoHeight(model) - 2 * (voxelX + voxelY) - 4 * voxelZ - 1;
		}
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
		public static ushort IsoShadowWidth(IModel model) => IsoWidth(model);
		public static ushort IsoShadowHeight(IModel model) => (ushort)(2 * (model.SizeX + model.SizeY) - 1);
		public static void IsoShadowLocate(out int pixelX, out int pixelY, IModel model, params int[] voxelCoordinates) => IsoLocate(out pixelX, out pixelY, model, voxelCoordinates[0], voxelCoordinates[1]);
		public static void IsoShadowLocate(out int pixelX, out int pixelY, IModel model, int voxelX = 0, int voxelY = 0)
		{
			// To move one x+ in voxels is x + 2, y - 2 in pixels.
			// To move one x- in voxels is x - 2, y + 2 in pixels.
			// To move one y+ in voxels is x - 2, y - 2 in pixels.
			// To move one y- in voxels is x + 2, y + 2 in pixels.
			pixelX = 2 * (model.SizeY + voxelX - voxelY);
			pixelY = 2 * (voxelX + voxelY) - 1;
		}
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
	}
}
