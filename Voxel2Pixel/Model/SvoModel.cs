using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Voxel2Pixel.Interfaces;

namespace Voxel2Pixel.Model
{
	/// <summary>
	/// SVO stands for "Sparse Voxel Octree"
	/// </summary>
	public class SvoModel : IEditableModel
	{
		#region Nested classes
		public abstract class Node
		{
			/// <summary>
			/// Header bit 7: 0 for Branch, 1 for Leaf.
			/// Header bit 6: Unused
			/// Header bits 5-3: For Branch only, number of children (1-8) minus one
			/// Header bit 2: Z of octant
			/// Header bit 1: Y of octant
			/// Header bit 0: X of octant
			/// </summary>
			public virtual byte Header { get; }
			public byte Octant { get; set; }
			public byte OctantX => (byte)(Octant & 1);
			public byte OctantY => (byte)((Octant >> 1) & 1);
			public byte OctantZ => (byte)((Octant >> 2) & 1);
			public Node Parent { get; set; } = null;
			public virtual bool IsLeaf => (Header & 0b10000000) > 0;
			public void Position(out ushort x, out ushort y, out ushort z)
			{
				Stack<Node> stack = new Stack<Node>();
				Node current = this;
				while (current is Node)
				{
					stack.Push(current);
					current = current.Parent;
				}
				ushort count = (ushort)(17 - stack.Count);
				x = 0; y = 0; z = 0;
				while (stack.Count > 0 && stack.Pop() is Node node)
				{
					x = (ushort)((x << 1) | node.OctantX);
					y = (ushort)((y << 1) | node.OctantY);
					z = (ushort)((z << 1) | node.OctantZ);
				}
				x <<= count; y <<= count; z <<= count;
			}
			public abstract void Clear();
			public abstract void Write(Stream stream);
		}
		public class Branch : Node, IEnumerable<Node>, IEnumerable
		{
			public override byte Header => (byte)((((Math.Max(Children.Where(child => child is Node).Count() - 1, 0)) & 0b111) << 3) | Octant & 0b111);
			protected Node[] Children = new Node[8];
			public override void Clear() => Children = new Node[8];
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
			public Node Next(byte octant)
			{
				for (byte child = (byte)(octant + 1); child < Children.Length; child++)
					if (Children[child] is Node node)
						return node;
				return null;
			}
			public Branch(Node parent, byte octant)
			{
				Parent = parent;
				Octant = octant;
			}
			public Branch(Stream stream, Node parent = null)
			{
				Parent = parent;
				using (BinaryReader reader = new BinaryReader(
					input: stream,
					encoding: System.Text.Encoding.Default,
					leaveOpen: true))
				{
					byte header = reader.ReadByte(),
						children = (byte)(((header >> 3) & 0b111) + 1);
					Octant = (byte)(header & 0b111);
					for (byte child = 0; child < children; child++)
					{
						header = reader.ReadByte();
						reader.BaseStream.Position--;
						this[(byte)(header & 0b111)] = (header & 0b10000000) > 0 ?
							(Node)new Leaf(stream, this)
							: new Branch(stream, this);
					}
				}
			}
			public override void Write(Stream stream)
			{
				using (BinaryWriter writer = new BinaryWriter(
					output: stream,
					encoding: System.Text.Encoding.Default,
					leaveOpen: true))
				{
					writer.Write(Header);
					foreach (Node child in this)
						child.Write(stream);
				}
			}
			#region IEnumerable<Node>
			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
			public IEnumerator<Node> GetEnumerator() => Children
				.Where(child => child is Node)
				.GetEnumerator();
			#endregion IEnumerable<Node>
		}
		public class Leaf : Node, IEnumerable<Voxel>, IEnumerable
		{
			public override byte Header => (byte)(0b10000000 | Octant & 0b111);
			public ulong Data = 0ul;
			public override void Clear() => Data = 0ul;
			public byte this[byte octant]
			{
				get => (byte)(Data >> (octant << 3));
				set
				{
					Data = Data & ~(0xFFul << (octant << 3)) | (ulong)value << (octant << 3);
					if (Data == 0ul && Parent is Branch parent)
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
			public Leaf(Stream stream, Node parent = null)
			{
				Parent = parent;
				using (BinaryReader reader = new BinaryReader(
					input: stream,
					encoding: System.Text.Encoding.Default,
					leaveOpen: true))
				{
					Octant = (byte)(reader.ReadByte() & 0b111);
					Data = reader.ReadUInt64();
				}
			}
			public override void Write(Stream stream)
			{
				using (BinaryWriter writer = new BinaryWriter(
					output: stream,
					encoding: System.Text.Encoding.Default,
					leaveOpen: true))
				{
					writer.Write(Header);
					writer.Write(Data);
				}
			}
			#region IEnumerable<Voxel>
			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
			public IEnumerator<Voxel> GetEnumerator()
			{
				if (Data == 0u)
					yield break;
				Position(out ushort x, out ushort y, out ushort z);
				for (byte octant = 0; octant < 8; octant++)
					if (this[octant] is byte index && index != 0)
						yield return new Voxel
						{
							X = (ushort)(x | (octant & 1)),
							Y = (ushort)(y | ((octant >> 1) & 1)),
							Z = (ushort)(z | ((octant >> 2) & 1)),
							Index = index,
						};
			}
			#endregion IEnumerable<Voxel>
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
		public SvoModel(IEnumerable<Voxel> voxels, ushort sizeX, ushort sizeY, ushort sizeZ) : this(sizeX, sizeY, sizeZ)
		{
			foreach (Voxel voxel in voxels)
				this[voxel.X, voxel.Y, voxel.Z] = voxel.Index;
		}
		public SvoModel(Stream stream, ushort sizeX, ushort sizeY, ushort sizeZ) : this(sizeX, sizeY, sizeZ) => Root = new Branch(stream);
		public SvoModel(byte[] bytes, ushort sizeX, ushort sizeY, ushort sizeZ) : this(new MemoryStream(bytes), sizeX, sizeY, sizeZ) { }
		public SvoModel(string z85, ushort sizeX, ushort sizeY, ushort sizeZ) : this(Cromulent.Encoding.Z85.FromZ85String(z85), sizeX, sizeY, sizeZ) { }
		public SvoModel(ushort sizeX, ushort sizeY, ushort sizeZ) : this()
		{
			SizeX = sizeX;
			SizeY = sizeY;
			SizeZ = sizeZ;
		}
		public void Write(Stream stream) => Root.Write(stream);
		public byte[] Bytes()
		{
			using (MemoryStream ms = new MemoryStream())
			{
				Write(ms);
				return ms.ToArray();
			}
		}
		public string Z85()
		{
			using (MemoryStream ms = new MemoryStream())
			{
				Write(ms);
				if (ms.Position % 4 is long four && four > 0)
					using (BinaryWriter writer = new BinaryWriter(
						output: ms,
						encoding: System.Text.Encoding.Default,
						leaveOpen: true))
						for (byte @byte = 0; @byte < 4 - four; @byte++)
							writer.Write((byte)0);
				return Cromulent.Encoding.Z85.ToZ85String(ms.ToArray());
			}
		}
		public uint NodeCount
		{
			get
			{
				uint nodes = 0;
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
		#region IEditableModel
		public ushort SizeX { get; set; }
		public ushort SizeY { get; set; }
		public ushort SizeZ { get; set; }
		public byte this[ushort x, ushort y, ushort z]
		{
			get
			{
				if (this.IsOutside(x, y, z))
					throw new IndexOutOfRangeException("[" + string.Join(", ", x, y, z) + "] is not within size [" + string.Join(", ", SizeX, SizeY, SizeZ) + "]!");
				Branch branch = Root;
				for (int level = 15; level > 1; level--)
					if (branch[(byte)((z >> level & 1) << 2 | (y >> level & 1) << 1 | x >> level & 1)] is Branch child)
						branch = child;
					else
						return 0;
				return branch[(byte)((z >> 1 & 1) << 2 | (y >> 1 & 1) << 1 | x >> 1 & 1)] is Leaf leaf ?
					leaf[(byte)((z & 1) << 2 | (y & 1) << 1 | x & 1)]
					: (byte)0;
			}
			set
			{
				if (this.IsOutside(x, y, z))
					throw new IndexOutOfRangeException("[" + string.Join(", ", x, y, z) + "] is not within size [" + string.Join(", ", SizeX, SizeY, SizeZ) + "]!");
				Branch branch = Root;
				byte octant;
				for (int level = 15; level > 1; level--)
				{
					octant = (byte)((z >> level & 1) << 2 | (y >> level & 1) << 1 | x >> level & 1);
					if (branch[octant] is Branch child)
						branch = child;
					else
					{
						if (value == 0)
							return;
						branch = (Branch)(branch[octant] = new Branch(branch, octant));
					}
				}
				octant = (byte)((z >> 1 & 1) << 2 | (y >> 1 & 1) << 1 | x >> 1 & 1);
				if (!(branch[octant] is Leaf leaf))
				{
					if (value == 0)
						return;
					leaf = (Leaf)(branch[octant] = new Leaf(branch, octant));
				}
				leaf[(byte)((z & 1) << 2 | (y & 1) << 1 | x & 1)] = value;
			}
		}
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		public IEnumerator<Voxel> GetEnumerator()
		{
			Stack<Branch> stack = new Stack<Branch>();
			void Push(Branch branch)
			{
				while (branch is Branch)
				{
					stack.Push(branch);
					branch = branch.First() as Branch;
				}
			}
			Push(Root);
			while (stack.Count > 0 && stack.Pop() is Branch popped)
			{
				if (stack.Count > 13)
					for (byte octant = 0; octant < 8; octant++)
						if (popped[octant] is Leaf leaf)
							foreach (Voxel voxel in leaf)
								yield return voxel;
				if (popped.Parent is Branch parent
					&& parent.Next(popped.Octant) is Branch child)
					Push(child);
			}
		}
		#endregion IEditableModel
	}
}
