using System;
using System.Collections;
using System.Collections.Generic;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;

namespace Voxel2Pixel.SVO
{
	public class SVO : IModel
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
				set => Data = (Data & ~(0xFFul << (index << 3))) | ((ulong)value << (index << 3));
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
		Branch Root;
		public SVO(IEnumerable<Voxel> voxels)
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
			get => throw new NotImplementedException();
			set
			{
				Node node = Root;
				for (int level = 16; level > 1; level--)
					if (!(node is Branch branch))
						break;
					else
					{
						byte childNumber = (byte)((((z >> level) & 1) << 2) | (((y >> level) & 1) << 1) | ((x >> level) & 1));
						if (branch[childNumber] is Node child)
							node = child;
						else
							node = branch[childNumber] = new Branch();
					}
				Branch lastBranch = (Branch)node;
				byte leafNumber = (byte)((((z >> 1) & 1) << 2) | (((y >> 1) & 1) << 1) | ((x >> 1) & 1));
				if (!(lastBranch[leafNumber] is Leaf leaf))
					leaf = (Leaf)(lastBranch[leafNumber] = new Leaf());
				leaf[(byte)(((z & 1) << 2) | ((y & 1) << 1) | (x >> 1) & 1)] = value;
			}
		}
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		public IEnumerator<Voxel> GetEnumerator() => throw new NotImplementedException();
		#endregion IModel
	}
}
