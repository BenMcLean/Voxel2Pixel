using BenProgress;
using BenVoxel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model.FileFormats;
using static BenVoxel.ExtensionMethods;

namespace Voxel2Pixel.Model;

public class SvoModel : BenVoxel.SvoModel, ISpecializedModel
{
	public static BenVoxelFile FromMagicaVoxel(Stream stream)
	{
		VoxFileModel[] models = VoxFileModel.Models(stream, out uint[] palette);
		BenVoxelFile.Metadata global = new();
		global[""] = palette;
		BenVoxelFile file = new()
		{
			Global = global,
		};
		for (int i = 0; i < models.Length; i++)
			file.Models[i == 0 ? "" : i.ToString()] = new BenVoxelFile.Model { Geometry = new(models[i]) };
		return file;
	}
	#region SvoModel
	public SvoModel() : base() { }
	public SvoModel(IModel model) : base(model) { }
	public SvoModel(IEnumerable<Voxel> voxels, ushort sizeX = ushort.MaxValue, ushort sizeY = ushort.MaxValue, ushort sizeZ = ushort.MaxValue) : base(voxels, sizeX, sizeY, sizeZ) { }
	public SvoModel(Stream stream, ushort sizeX, ushort sizeY, ushort sizeZ) : base(stream, sizeX, sizeY, sizeZ) { }
	public SvoModel(Stream stream) : base(stream) { }
	public SvoModel(BinaryReader reader, ushort sizeX, ushort sizeY, ushort sizeZ) : base(reader, sizeX, sizeY, sizeZ) { }
	public SvoModel(BinaryReader reader) : base(reader) { }
	public SvoModel(byte[] bytes, ushort sizeX, ushort sizeY, ushort sizeZ) : base(bytes, sizeX, sizeY, sizeZ) { }
	public SvoModel(byte[] bytes) : base(bytes) { }
	public SvoModel(string z85, ushort sizeX = ushort.MaxValue, ushort sizeY = ushort.MaxValue, ushort sizeZ = ushort.MaxValue) : base(z85, sizeX, sizeY, sizeZ) { }
	public SvoModel(ushort sizeX = ushort.MaxValue, ushort sizeY = ushort.MaxValue, ushort sizeZ = ushort.MaxValue) : base(sizeX, sizeY, sizeZ) { }
	#endregion SvoModel
	#region ISpecializedModel
	public async Task DrawAsync(IRenderer renderer, Perspective perspective, byte scaleX = 1, byte scaleY = 1, byte scaleZ = 1, double radians = 0d, ProgressContext? progressContext = null)
	{
		switch (perspective)
		{
			case Perspective.Front:
				Front(renderer);
				break;
			case Perspective.Diagonal:
				Diagonal(renderer);
				break;
			case Perspective.Above:
				Above(renderer);
				break;
			default:
				await VoxelDraw.DrawAsync(this, renderer, perspective, scaleX, scaleY, scaleZ, radians, progressContext: progressContext);
				break;
		}
	}
	public void Front(IRectangleRenderer renderer, VisibleFace visibleFace = VisibleFace.Front)
	{
		for (ushort x = 0; x < SizeX; x++)
			for (ushort z = 0; z < SizeZ; z++)
				if (LowestY(x, z) is byte index && index != 0)
					renderer.Rect(
						x: x,
						y: (ushort)(SizeZ - 1 - z),
						index: index,
						visibleFace: visibleFace);
	}
	public byte LowestY(ushort x, ushort z)
	{
		if (this.IsOutside(x, 0, z))
			throw new IndexOutOfRangeException("[" + string.Join(", ", x, 0, z) + "] is not within size [" + string.Join(", ", SizeX, SizeY, SizeZ) + "]!");
		Stack<Branch> stack = new();
		byte left(byte count) => (byte)((z >> 16 - count & 1) << 2 | x >> 16 - count & 1);
		void push(Branch branch)
		{
			while (branch is not null)
			{
				stack.Push(branch);
				byte octant = left((byte)stack.Count);
				branch = branch[octant] as Branch
					?? branch[(byte)(octant | 2)] as Branch;
			}
		}
		push(Root);
		while (stack.Count > 0 && stack.Pop() is Branch branch)
		{
			if (stack.Count == 14)
			{
				byte octant = left(15),
					final = left(16);
				if (branch[octant] is Leaf leaf)
				{
					if (leaf[final] is byte index1 && index1 != 0)
						return index1;
					else if (leaf[(byte)(final | 2)] is byte index2 && index2 != 0)
						return index2;
				}
				else if (branch[(byte)(octant | 2)] is Leaf leaf2)
				{
					if (leaf2[final] is byte index1 && index1 != 0)
						return index1;
					else if (leaf2[(byte)(final | 2)] is byte index2 && index2 != 0)
						return index2;
				}
			}
			if ((branch.Octant & 2) == 0
				&& branch.Parent is Branch parent
				&& parent[(byte)(branch.Octant | 2)] is Branch child)
				push(child);
		}
		return 0;
	}
	public void Diagonal(IRectangleRenderer renderer)
	{
		ushort pixelWidth = (ushort)(SizeX + SizeY);
		for (ushort voxelZ = 0, pixelY = (ushort)(SizeZ - 1);
			voxelZ < SizeZ;
			voxelZ++, pixelY = (ushort)(SizeZ - 1 - voxelZ))
			for (ushort pixelX = 0; pixelX < pixelWidth; pixelX++)
			{
				bool yFirst = pixelX < SizeY;
				ushort voxelXStart = (ushort)Math.Max(0, pixelX - SizeY),
					voxelYStart = (ushort)Math.Max(0, SizeY - 1 - pixelX),
					voxelX = voxelXStart,
					voxelY = voxelYStart;
				while (voxelX < SizeX && voxelY < SizeY)
				{
					if (FindVoxel(
						x: voxelX,
						y: voxelY,
						z: voxelZ,
						node: out Node node,
						octant: out byte octant) is byte index && index != 0)
					{
						renderer.Rect(
							x: pixelX,
							y: pixelY,
							index: index,
							visibleFace: yFirst && voxelX - voxelXStart >= voxelY - voxelYStart
								|| !yFirst && voxelX - voxelXStart > voxelY - voxelYStart ?
								VisibleFace.Left
								: VisibleFace.Right);
						break;
					}
					else
					{
						node.Edge(
							octant: octant,
							x: out ushort edgeX,
							y: out ushort edgeY,
							z: out _,
							depth: out byte depth);
						if (depth < 2)
							break;
						if (node is Leaf)
							if (yFirst && voxelX - voxelXStart >= voxelY - voxelYStart
								|| !yFirst && voxelX - voxelXStart > voxelY - voxelYStart)
								voxelY++;
							else
								voxelX++;
						else
						{
							if (yFirst && edgeX - voxelXStart < edgeY - voxelYStart
								|| !yFirst && edgeX - voxelXStart <= edgeY - voxelYStart)
							{
								voxelY = (ushort)(voxelYStart + edgeX - voxelXStart - (yFirst || voxelXStart == edgeX ? 0 : 1));
								voxelX = edgeX;
							}
							else
							{
								voxelX = (ushort)(voxelXStart + edgeY - voxelYStart - (yFirst && voxelXStart != edgeY ? 1 : 0));
								voxelY = edgeY;
							}
						}
					}
				}
			}
	}
	public void Above(IRectangleRenderer renderer)
	{
		ushort pixelHeight = (ushort)(SizeY + SizeZ);
		for (ushort x = 0; x < SizeX; x++)
			for (ushort pixelY = 0; pixelY < pixelHeight; pixelY++)
			{
				bool zFirst = pixelY < SizeY;
				ushort voxelYStart = (ushort)Math.Max(0, SizeY - 1 - pixelY),
					voxelZStart = (ushort)Math.Min(SizeZ - 1, pixelHeight - 1 - pixelY),
					voxelY = voxelYStart;
				int voxelZ = voxelZStart;
				while (voxelY < SizeY && voxelZ >= 0)
				{
					if (FindVoxel(
						x: x,
						y: voxelY,
						z: (ushort)voxelZ,
						node: out Node node,
						octant: out byte octant) is byte index && index != 0)
					{
						renderer.Rect(
							x: x,
							y: pixelY,
							index: index,
							visibleFace: zFirst && voxelY - voxelYStart > voxelZStart - voxelZ
								|| !zFirst && voxelY - voxelYStart >= voxelZStart - voxelZ ?
								VisibleFace.Front
								: VisibleFace.Top);
						break;
					}
					else
					{
						node.EdgeNegativeZ(
							octant: octant,
							x: out _,
							y: out ushort edgeY,
							z: out int edgeZ,
							depth: out byte depth);
						if (depth < 2)
							break;
						if (node is Leaf)
							if (zFirst && voxelY - voxelYStart > voxelZStart - voxelZ
								|| !zFirst && voxelY - voxelYStart >= voxelZStart - voxelZ)
								voxelZ--;
							else
								voxelY++;
						else
						{
							if (zFirst && edgeY - voxelYStart <= voxelZStart - edgeZ
								|| !zFirst && edgeY - voxelYStart < voxelZStart - edgeZ)
							{
								voxelZ = voxelZStart - (edgeY - voxelYStart) + (zFirst && voxelYStart != edgeY ? 1 : 0);
								voxelY = edgeY;
							}
							else
							{
								voxelY = (ushort)(voxelYStart + voxelZStart - edgeZ - (!zFirst && voxelZStart != edgeZ ? 1 : 0));
								voxelZ = edgeZ;
							}
						}
					}
				}
			}
	}
	#endregion ISpecializedModel
}
