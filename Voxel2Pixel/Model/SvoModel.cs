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
	public class SvoModel : IModel
	{
		#region Nested classes
		public abstract class Node
		{
			/// <summary>
			/// Header bit 7: 0 for Branch, 1 for Leaf.
			/// Header bit 6: Reserved
			/// Header bits 5-3: For Branch only, number of children
			/// Header bits 2-0: Octant of parent
			/// </summary>
			public virtual byte Header { get; }
			public byte Octant { get; set; }
			public Node Parent { get; set; }
			public virtual bool IsLeaf => (Header & 0b10000000) > 0;
			public abstract void Clear();
			public abstract void Write(Stream stream);
		}
		public class Branch : Node
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
					foreach (Node child in Children.Where(child => child is Node))
						child.Write(stream);
				}
			}
		}
		public class Leaf : Node
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
					throw new IndexOutOfRangeException("[" + string.Join(", ", x, z, y) + "] is not within size [" + string.Join(", ", SizeX, SizeY, SizeZ) + "]!");
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
