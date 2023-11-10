using System;
using System.Collections;
using System.Collections.Generic;
using Voxel2Pixel.Interfaces;

namespace Voxel2Pixel.Model
{
	/// <summary>
	/// SVO stands for "Sparse Voxel Octree"
	/// </summary>
	public class SvoModel : IModel
	{
		public abstract class Node
		{
			public virtual byte Header { get; }
			public virtual bool IsLeaf => (Header & 0b10000000) > 0;
		}
		public class Branch : Node
		{
			public override byte Header => (byte)(Children.Length - 1);
			protected Node[] Children = new Node[8];
			public Node this[byte index]
			{
				get => Children[index];
				set => Children[index] = value;
			}
			public byte NumberOfChildren
			{
				get => (byte)Children.Length;
				set => Children = new Node[Math.Min(value, (byte)8)];
			}
		}
		public class Leaf : Node
		{
			public override byte Header => 0b10000000;
			public ulong Data = 0;
			public byte this[byte index]
			{
				get => (byte)(Data >> (index << 3));
				set => Data = Data & ~(0xFFul << (index << 3)) | (ulong)value << (index << 3);
			}
			public byte this[bool x, bool y, bool z]
			{
				get => this[(byte)((z ? 4 : 0) + (y ? 2 : 0) + (x ? 1 : 0))];
				set => this[(byte)((z ? 4 : 0) + (y ? 2 : 0) + (x ? 1 : 0))] = value;
			}
			public byte this[byte x, byte y, byte z]
			{
				get => this[(byte)((z > 0 ? 4 : 0) + (y > 0 ? 2 : 0) + (x > 0 ? 1 : 0))];
				set => this[(byte)((z > 0 ? 4 : 0) + (y > 0 ? 2 : 0) + (x > 0 ? 1 : 0))] = value;
			}
		}
		public readonly Branch Root;
		public SvoModel(IEnumerable<Voxel> voxels)
		{
			Root = new Branch();
			foreach (Voxel voxel in voxels)
				this[voxel.X, voxel.Y, voxel.Z] = voxel.Index;
		}
		#region IModel
		public ushort SizeX { get; set; }
		public ushort SizeY { get; set; }
		public ushort SizeZ { get; set; }
		public byte this[ushort x, ushort y, ushort z]
		{
			get
			{
				Node node = Root;
				for (int level = 16; level > 1; level--)
					if (!(node is Branch branch))
						return 0;
					else
					{
						byte childNumber = (byte)((z >> level & 1) << 2 | (y >> level & 1) << 1 | x >> level & 1);
						if (branch[childNumber] is Node child)
							node = child;
						else
							return 0;
					}
				if (!(node is Branch lastBranch))
					return 0;
				byte leafNumber = (byte)((z >> 1 & 1) << 2 | (y >> 1 & 1) << 1 | x >> 1 & 1);
				return lastBranch[leafNumber] is Leaf leaf ?
					leaf[(byte)((z & 1) << 2 | (y & 1) << 1 | x & 1)]
					: (byte)0;
			}
			set
			{
				Node node = Root;
				for (int level = 16; level > 1; level--)
					if (!(node is Branch branch))
						break;
					else
					{
						byte childNumber = (byte)((z >> level & 1) << 2 | (y >> level & 1) << 1 | x >> level & 1);
						if (branch[childNumber] is Node child)
							node = child;
						else
							node = branch[childNumber] = new Branch();
					}
				Branch lastBranch = (Branch)node;
				byte leafNumber = (byte)((z >> 1 & 1) << 2 | (y >> 1 & 1) << 1 | x >> 1 & 1);
				if (!(lastBranch[leafNumber] is Leaf leaf))
					leaf = (Leaf)(lastBranch[leafNumber] = new Leaf());
				leaf[(byte)((z & 1) << 2 | (y & 1) << 1 | x & 1)] = value;
			}
		}
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		public IEnumerator<Voxel> GetEnumerator()
		{
			List<Voxel> voxels = new List<Voxel>();
			void Recurse(Node node, ushort x, ushort y, ushort z)
			{
				if (node is Branch branch)
				{
					for (byte i = 0; i < 8; i++)
						if (branch[i] is Node child)
							Recurse(
								node: child,
								x: (ushort)(x << 1 | i & 1),
								y: (ushort)(y << 1 | i << 1 & 1),
								z: (ushort)(z << 1 | i << 2 & 1));
				}
				else if (node is Leaf leaf)
					for (byte i = 0; i < 8; i++)
						if (leaf[i] is byte index && index != 0)
							voxels.Add(new Voxel
							{
								X = (ushort)(x | index & 1),
								Y = (ushort)(y | index << 1 & 1),
								Z = (ushort)(z | index << 2 & 1),
								Index = index,
							});
			}
			return voxels.GetEnumerator();
		}
		#endregion IModel
	}
}
