using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Voxel2Pixel.Interfaces;

namespace Voxel2Pixel.Model
{
	/// <summary>
	/// SVO stands for "Sparse Voxel Octree"
	/// </summary>
	public class SvoModel : IModel
	{
		#region Nested classes
		public abstract class Node
		{
			public virtual byte Header { get; }
			public byte Octant { get; set; }
			public Node Parent { get; set; }
			public virtual bool IsLeaf => (Header & 0b10000000) > 0;
			public abstract void Clear();
		}
		public class Branch : Node
		{
			public override byte Header => (byte)(Children.Length - 1);
			protected Node[] Children = new Node[8];
			public override void Clear() => NumberOfChildren = 8;
			public Node this[byte octant]
			{
				get => Children[octant];
				set
				{
					Children[octant] = value;
					if (value is null
						&& Parent is Branch parent
						&& !Children.Any(child => child is Node))
						parent[Octant] = null;
				}
			}
			public byte NumberOfChildren
			{
				get => (byte)Children.Length;
				set => Children = new Node[Math.Min(value, (byte)8)];
			}
			public Branch(Node parent, byte octant)
			{
				Parent = parent;
				Octant = octant;
			}
		}
		public class Leaf : Node
		{
			public override byte Header => 0b10000000;
			public ulong Data = 0ul;
			public override void Clear() => Data = 0ul;
			public byte this[byte octant]
			{
				get => (byte)(Data >> (octant << 3));
				set
				{
					Data = Data & ~(0xFFul << (octant << 3)) | (ulong)value << (octant << 3);
					if (Data == 0ul
						&& Parent is Branch parent)
						parent[Octant] = null;
				}
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
			public Leaf(Node parent, byte octant)
			{
				Parent = parent;
				Octant = octant;
			}
		}
		#endregion Nested classes
		#region SvoModel
		public readonly Branch Root = new Branch(null, 0);
		public void Clear() => Root.Clear();
		public SvoModel() { }
		public SvoModel(IModel model) : this(
			voxels: model,
			sizeX: model.SizeX,
			sizeY: model.SizeY,
			sizeZ: model.SizeZ)
		{ }
		public SvoModel(IEnumerable<Voxel> voxels, ushort sizeX, ushort sizeY, ushort sizeZ) : this()
		{
			SizeX = sizeX;
			SizeY = sizeY;
			SizeZ = sizeZ;
			foreach (Voxel voxel in voxels)
				this[voxel.X, voxel.Y, voxel.Z] = voxel.Index;
		}
		public int NodeCount
		{
			get
			{
				int nodes = 0;
				void Recurse(Node node)
				{
					nodes++;
					if (node is Branch branch)
						for (byte octant = 0; octant < 8; octant++)
							if (branch[octant] is Node child)
								Recurse(child);
				}
				Recurse(Root);
				return nodes;
			}
		}
		#endregion SvoModel
		#region IModel
		public ushort SizeX { get; set; }
		public ushort SizeY { get; set; }
		public ushort SizeZ { get; set; }
		public byte this[ushort x, ushort y, ushort z]
		{
			get
			{
				if (this.IsOutside(x, y, z))
					throw new IndexOutOfRangeException("[" + string.Join(", ", x, z, y) + "] is not within size [" + string.Join(", ", SizeX, SizeY, SizeZ) + "]!");
				Node node = Root;
				for (int level = 16; level > 1; level--)
					if (!(node is Branch branch))
						return 0;
					else
					{
						byte octant = (byte)((z >> level & 1) << 2 | (y >> level & 1) << 1 | x >> level & 1);
						if (branch[octant] is Node child)
							node = child;
						else
							return 0;
					}
				return node is Branch lastBranch
					&& lastBranch[(byte)((z >> 1 & 1) << 2 | (y >> 1 & 1) << 1 | x >> 1 & 1)] is Leaf leaf ?
						leaf[(byte)((z & 1) << 2 | (y & 1) << 1 | x & 1)]
						: (byte)0;
			}
			set
			{
				if (this.IsOutside(x, y, z))
					throw new IndexOutOfRangeException("[" + string.Join(", ", x, z, y) + "] is not within size [" + string.Join(", ", SizeX, SizeY, SizeZ) + "]!");
				Node node = Root;
				byte octant;
				for (int level = 16; level > 1; level--)
					if (!(node is Branch branch))
						throw new InvalidCastException("Wrong node type. Expected: \"Branch\", Actual: \"" + node.GetType().Name + "\"");
					else
					{
						octant = (byte)((z >> level & 1) << 2 | (y >> level & 1) << 1 | x >> level & 1);
						if (branch[octant] is Node child)
							node = child;
						else
						{
							if (value == 0)
								return;
							node = branch[octant] = new Branch(node, octant);
						}
					}
				Branch lastBranch = (Branch)node;
				octant = (byte)((z >> 1 & 1) << 2 | (y >> 1 & 1) << 1 | x >> 1 & 1);
				if (!(lastBranch[octant] is Leaf leaf))
				{
					if (value == 0)
						return;
					leaf = (Leaf)(lastBranch[octant] = new Leaf(lastBranch, octant));
				}
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
					for (byte octant = 0; octant < 8; octant++)
						if (branch[octant] is Node child)
							Recurse(
								node: child,
								x: (ushort)((x << 1) | (octant & 1)),
								y: (ushort)((y << 1) | ((octant >> 1) & 1)),
								z: (ushort)((z << 1) | ((octant >> 2) & 1)));
				}
				else if (node is Leaf leaf)
					for (byte octant = 0; octant < 8; octant++)
						if (leaf[octant] is byte index && index != 0)
							voxels.Add(new Voxel
							{
								X = (ushort)((x << 1) | (octant & 1)),
								Y = (ushort)((y << 1) | ((octant >> 1) & 1)),
								Z = (ushort)((z << 1) | ((octant >> 2) & 1)),
								Index = index,
							});
			}
			Recurse(Root, 0, 0, 0);
			return voxels.GetEnumerator();
		}
		#endregion IModel
	}
}
