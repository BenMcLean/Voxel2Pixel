using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using BenVoxel.Interfaces;
using BenVoxel.Structs;

namespace BenVoxel.Models;

/// <summary>
/// SVO stands for "Sparse Voxel Octree"
/// </summary>
[JsonConverter(typeof(SvoModelConverter))]
public class SvoModel() : IEditableBrickModel, IBinaryWritable
{
	#region Nested classes
	public abstract class Node : IBinaryWritable
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
		public byte OctantY => (byte)(Octant >> 1 & 1);
		public byte OctantZ => (byte)(Octant >> 2 & 1);
		public Node Parent { get; set; } = null;
		public virtual bool IsLeaf => (Header & 0b10000000) > 0;
		public virtual void Position(out ushort x, out ushort y, out ushort z)
		{
			Stack<Node> stack = new();
			Node current = this;
			while (current is not null)
			{
				stack.Push(current);
				current = current.Parent;
			}
			ushort count = (ushort)(17 - stack.Count);
			x = 0; y = 0; z = 0;
			while (stack.Count > 0 && stack.Pop() is Node node)
			{
				x = (ushort)(x << 1 | node.OctantX);
				y = (ushort)(y << 1 | node.OctantY);
				z = (ushort)(z << 1 | node.OctantZ);
			}
			x <<= count; y <<= count; z <<= count;
		}
		public virtual byte Depth
		{
			get
			{
				byte depth = 0;
				for (Node current = this; current is not null; current = current.Parent, depth++) { }
				return depth;
			}
		}
		public virtual ushort Size => (ushort)((1 << 17 - Depth) - 1);
		public virtual void Edge(out ushort x, out ushort y, out ushort z)
		{
			Position(out x, out y, out z);
			ushort size = Size;
			x += size; y += size; z += size;
		}
		public virtual void Edge(byte octant, out ushort x, out ushort y, out ushort z) => Edge(octant: octant, x: out x, y: out y, z: out z, depth: out _);
		public virtual void Edge(byte octant, out ushort x, out ushort y, out ushort z, out byte depth)
		{
			Position(out x, out y, out z);
			depth = Depth;
			ushort outer = (ushort)(1 << 17 - depth),
				inner = (ushort)(1 << 16 - depth);
			x += (octant & 1) > 0 ? outer : inner;
			y += (octant & 2) > 0 ? outer : inner;
			z += (octant & 4) > 0 ? outer : inner;
		}
		public virtual void EdgeNegativeZ(byte octant, out ushort x, out ushort y, out int z) => EdgeNegativeZ(octant: octant, x: out x, y: out y, z: out z, depth: out _);
		public virtual void EdgeNegativeZ(byte octant, out ushort x, out ushort y, out int z, out byte depth)
		{
			Position(out x, out y, z: out ushort @ushort);
			depth = Depth;
			ushort outer = (ushort)(1 << 17 - depth),
				inner = (ushort)(1 << 16 - depth);
			x += (octant & 1) > 0 ? outer : inner;
			y += (octant & 2) > 0 ? outer : inner;
			z = @ushort + ((octant & 4) > 0 ? inner : 0) - 1;
		}
		public abstract void Clear();
		#region IBinaryWritable
		public abstract void Write(Stream stream);
		public abstract void Write(BinaryWriter writer);
		#endregion IBinaryWritable
	}
	public class Branch : Node, IBinaryWritable, IEnumerable<Node>, IEnumerable
	{
		public override byte Header => (byte)((Math.Max(Children.OfType<Node>().Count() - 1, 0) & 7) << 3 | Octant & 7);
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
					&& !Children.Any(child => child is not null))
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
		public Branch() { }
		public Branch(Node parent = null, byte octant = 0) : this()
		{
			Parent = parent;
			Octant = octant;
		}
		public Branch(Branch parent, byte octant, byte color) : this(parent, octant) => ExpandCollapsed(color);
		public Branch(Stream stream, Branch parent = null) : this()
		{
			using BinaryReader reader = new(
				input: stream,
				encoding: Encoding.UTF8,
				leaveOpen: true);
			FromReader(reader, parent);
		}
		public Branch(BinaryReader reader, Branch parent = null) : this() => FromReader(reader, parent);
		private void FromReader(BinaryReader reader, Branch parent = null)
		{
			Parent = parent;
			byte header = reader.ReadByte();
			Octant = (byte)(header & 0b111);
			switch ((byte)(header >> 6 & 3))
			{//Check the node type (bits 7-6)
				case 0b00: // Regular branch
					byte children = (byte)((header >> 3 & 7) + 1);
					for (byte child = 0; child < children; child++)
					{
						header = reader.ReadByte();
						reader.BaseStream.Position--;
						this[(byte)(header & 7)] = (header & 0b10000000) > 0 ?
							new Leaf(reader, this)
							: new Branch(reader, this);
					}
					break;
				case 0b01: // Collapsed branch
					ExpandCollapsed(reader.ReadByte());
					break;
				default:
					throw new IOException("Invalid node type in branch header.");
			}
		}
		#region IBinaryWritable
		public override void Write(Stream stream)
		{
			using BinaryWriter writer = new(
				output: stream,
				encoding: Encoding.UTF8,
				leaveOpen: true);
			Write(writer);
		}
		public override void Write(BinaryWriter writer)
		{
			if (TryCollapse() is byte collapsed && collapsed != 0)
			{
				writer.Write((byte)(0b01000000 | Octant & 7));//Header
				writer.Write(collapsed);
				return;
			}
			writer.Write(Header);
			foreach (Node child in this)
				child.Write(writer);
		}
		#endregion IBinaryWritable
		#region IEnumerable<Node>
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		public IEnumerator<Node> GetEnumerator() => Children.OfType<Node>().GetEnumerator();
		#endregion IEnumerable<Node>
		#region Collapse
		public Branch ExpandCollapsed(byte color)
		{
			if (Depth == 15)
				for (byte octant = 0; octant < 8; octant++)
					Children[octant] = new Leaf(this, octant, color);
			else
				for (byte octant = 0; octant < 8; octant++)
					Children[octant] = new Branch(this, octant, color);
			return this;
		}
		protected byte TryCollapse(byte? color = null)
		{
			if (!color.HasValue)
				return TryCollapse(TryCollapseGetColor());
			if (color.Value == 0)
				return 0;
			ulong expectedData = color.Value * 0x0101010101010101UL;
			foreach (Node child in Children)
				if (child is null
					|| child is Leaf leaf && leaf.Data != expectedData
					|| child is Branch branch && branch.TryCollapse(color.Value) == 0)
					return 0;
			return color.Value;
		}
		protected byte TryCollapseGetColor() => Children[0] switch
		{
			Leaf leaf => leaf[0],
			Branch branch => branch.TryCollapseGetColor(),
			_ => 0,
		};
		#endregion Collapse
	}
	public class Leaf : Node, IBinaryWritable, IEnumerable<Voxel>, IEnumerable
	{
		public override byte Header => (byte)(0b11000000 | Octant & 7);
		internal ulong Data = 0;
		public override void Clear() => Data = 0;
		public byte this[byte octant]
		{
			get => (byte)(Data >> (octant << 3) & 0xFF);
			set
			{
				int shift = octant << 3;
				ulong mask = 0xFFUL << shift;
				Data = Data & ~mask | (ulong)value << shift;
				if (Data == 0 && Parent is Branch parent)
					parent[Octant] = null;
			}
		}
		public IEnumerable<byte> Values
		{
			get
			{
				for (byte i = 0; i < 8; i++)
					yield return (byte)(Data >> (i << 3) & 0xFF);
			}
		}
		public override byte Depth => 16;
		public override ushort Size => 1;
		public Leaf() { }
		public Leaf(Node parent, byte octant) : this()
		{
			Parent = parent;
			Octant = octant;
		}
		public Leaf(Node parent, byte octant, byte color) : this(parent, octant)
		{
			if (color == 0)
				Data = 0;//trigger removal
			else
				Data = color * 0x0101010101010101UL;
		}
		public Leaf(Stream stream, Node parent = null) : this()
		{
			using BinaryReader reader = new(
				input: stream,
				encoding: Encoding.UTF8,
				leaveOpen: true);
			FromReader(reader, parent);
		}
		public Leaf(BinaryReader reader, Node parent = null) : this() => FromReader(reader, parent);
		private void FromReader(BinaryReader reader, Node parent = null)
		{
			Parent = parent;
			byte header = reader.ReadByte();
			Octant = (byte)(header & 7);
			switch ((byte)(header >> 6 & 3))
			{//Check the node type (bits 7-6)
				case 0b10://2-byte payload leaf
					byte where = (byte)(header >> 3 & 7),
						foreground = reader.ReadByte(),
						background = reader.ReadByte();
					if (foreground == 0 && background == 0)
						Data = 0;//trigger removal
					else
					{
						// Fill with background, then overwrite one byte with foreground
						Data = background * 0x0101010101010101UL;
						int shift = where << 3;
						ulong mask = 0xFFUL << shift;
						Data = Data & ~mask | (ulong)foreground << shift;
					}
					break;
				case 0b11://8-byte payload leaf
					Data = reader.ReadUInt64();
					break;
				default:
					throw new IOException("Invalid node type in leaf header.");
			}
		}
		#region IBinaryWritable
		public override void Write(Stream stream)
		{
			using BinaryWriter writer = new(
				output: stream,
				encoding: Encoding.UTF8,
				leaveOpen: true);
			Write(writer);
		}
		public override void Write(BinaryWriter writer)
		{
			// Fast uniform check
			byte first = (byte)(Data & 0xFF);
			if (Data == first * 0x0101010101010101UL)
			{//Single color - use 2-byte payload leaf with same color for both bytes
				writer.Write((byte)(0b10000000 | Octant & 7));//Header
				writer.Write(first); // Foreground color
				writer.Write(first); // Background color (same)
				return;
			}
			// Not uniform - count unique colors
			Dictionary<byte, byte> counts = [];
			for (byte i = 0; i < 8; i++)
			{
				byte val = (byte)(Data >> (i << 3) & 0xFF);
				if (counts.TryGetValue(val, out byte count))
					counts[val] = (byte)(count + 1);
				else
					counts[val] = 1;
			}
			// Check for 2-color compression (one unique voxel)
			if (counts.Count == 2)
			{
				byte foreground = 0, background = 0;
				foreach (KeyValuePair<byte, byte> kvp in counts)
					if (kvp.Value == 1)
						foreground = kvp.Key;
					else if (kvp.Value == 7)
						background = kvp.Key;
				if (background != 0 || foreground != 0)
				{//Two colors with one unique - use 2-byte payload leaf
				 // Find position using XOR trick
					ulong backgroundPattern = background * 0x0101010101010101UL,
						diff = Data ^ backgroundPattern;
					// Find first non-zero byte position
					byte uniquePos = 0;
					for (byte i = 0; i < 8; i++)
						if ((diff >> (i << 3) & 0xFF) != 0)
						{
							uniquePos = i;
							break;
						}
					writer.Write((byte)(0b10000000 | (uniquePos & 7) << 3 | Octant & 7));//Header
					writer.Write(foreground);//Foreground color (the unique one)
					writer.Write(background);//Background color (the repeated one)
					return;
				}
			}
			//Multiple colors - use 8-byte payload leaf
			writer.Write(Header);
			writer.Write(Data); // BinaryWriter.Write(ulong) writes little-endian
		}
		#endregion IBinaryWritable
		public Voxel Voxel(byte octant)
		{
			Position(out ushort x, out ushort y, out ushort z);
			return new Voxel(
				X: (ushort)(x | octant & 1),
				Y: (ushort)(y | octant >> 1 & 1),
				Z: (ushort)(z | octant >> 2 & 1),
				Material: this[octant]);
		}
		#region IEnumerable<Voxel>
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		public IEnumerator<Voxel> GetEnumerator()
		{
			if (Data == 0)
				yield break;
			Position(out ushort x, out ushort y, out ushort z);
			for (byte octant = 0; octant < 8; octant++)
				if (this[octant] is byte index && index != 0)
					yield return new Voxel(
						X: (ushort)(x | octant & 1),
						Y: (ushort)(y | octant >> 1 & 1),
						Z: (ushort)(z | octant >> 2 & 1),
						Material: index);
		}
		#endregion IEnumerable<Voxel>
	}
	#endregion Nested classes
	#region SvoModel
	public Branch Root = new();
	public string Z85
	{
		get
		{
			using MemoryStream compressed = new();
			using (DeflateStream deflateStream = new(
				stream: compressed,
				mode: CompressionMode.Compress,
				leaveOpen: true))
			{
				Write(stream: deflateStream, includeSizes: false);
				deflateStream.Flush();
			}
			if (compressed.Length % 4 is long remainder && remainder > 0)
				compressed.Write(new byte[4 - remainder], 0, (int)(4 - remainder));
			compressed.Position = 0;
			return Cromulent.Encoding.Z85.ToZ85String(compressed.ToArray());
		}
		set
		{
			using MemoryStream decompressed = new();
			using (MemoryStream compressed = new(Cromulent.Encoding.Z85.FromZ85String(value)))
			using (DeflateStream deflateStream = new(
				stream: compressed,
				mode: CompressionMode.Decompress,
				leaveOpen: true))
			{
				deflateStream.CopyTo(decompressed);
			}
			decompressed.Position = 0;
			Root = new(decompressed);
		}
	}
	public void Clear() => Root.Clear();
	public SvoModel(ushort sizeX = ushort.MaxValue, ushort sizeY = ushort.MaxValue, ushort sizeZ = ushort.MaxValue) : this()
	{
		SizeX = sizeX;
		SizeY = sizeY;
		SizeZ = sizeZ;
	}
	public SvoModel(IModel model) : this(voxels: model, sizeX: model.SizeX, sizeY: model.SizeY, sizeZ: model.SizeZ) { }
	public SvoModel(IEnumerable<Voxel> voxels, ushort sizeX = ushort.MaxValue, ushort sizeY = ushort.MaxValue, ushort sizeZ = ushort.MaxValue) : this(sizeX, sizeY, sizeZ)
	{
		foreach (Voxel voxel in voxels)
			this.Set(voxel);
	}
	public SvoModel(Stream stream, ushort sizeX, ushort sizeY, ushort sizeZ) : this(sizeX, sizeY, sizeZ)
	{
		using BinaryReader reader = new(
			input: stream,
			encoding: Encoding.UTF8,
			leaveOpen: true);
		Root = new(reader);
	}
	public SvoModel(Stream stream) : this()
	{
		using BinaryReader reader = new(
			input: stream,
			encoding: Encoding.UTF8,
			leaveOpen: true);
		FromReader(reader);
	}
	private void FromReader(BinaryReader reader)
	{
		SizeX = reader.ReadUInt16();
		SizeY = reader.ReadUInt16();
		SizeZ = reader.ReadUInt16();
		Root = new(reader);
	}
	public SvoModel(BinaryReader reader, ushort sizeX, ushort sizeY, ushort sizeZ) : this(sizeX, sizeY, sizeZ) => Root = new(reader);
	public SvoModel(BinaryReader reader) : this() => FromReader(reader);
	public SvoModel(byte[] bytes, ushort sizeX, ushort sizeY, ushort sizeZ) : this(sizeX, sizeY, sizeZ)
	{
		using MemoryStream stream = new(bytes);
		using BinaryReader reader = new(input: stream, encoding: Encoding.UTF8);
		Root = new(reader);
	}
	public SvoModel(byte[] bytes) : this()
	{
		using MemoryStream stream = new(bytes);
		using BinaryReader reader = new(input: stream, encoding: Encoding.UTF8);
		FromReader(reader);
	}
	public SvoModel(string z85, ushort sizeX = ushort.MaxValue, ushort sizeY = ushort.MaxValue, ushort sizeZ = ushort.MaxValue) : this(sizeX, sizeY, sizeZ) => Z85 = z85;
	#endregion SvoModel
	#region IBinaryWritable
	public void Write(Stream stream) => Write(stream, true);
	public void Write(Stream stream, bool includeSizes)
	{
		using BinaryWriter writer = new(
			output: stream,
			encoding: Encoding.UTF8,
			leaveOpen: true);
		Write(writer: writer, includeSizes: includeSizes);
	}
	public void Write(BinaryWriter writer) => Write(writer, true);
	public void Write(BinaryWriter writer, bool includeSizes)
	{
		if (includeSizes)
		{
			writer.Write(SizeX);
			writer.Write(SizeY);
			writer.Write(SizeZ);
		}
		if (Root is not null && Root.Count() > 0)
			Root.Write(writer);
		else
		{//Empty model
			writer.Write(new byte[15]);//Branch headers
			writer.Write((byte)0b10000000);//2-byte payload Leaf header
			writer.Write(new byte[2]);//Empty 2x2x2 voxel cube
		}
	}
	#endregion IBinaryWritable
	#region Utilities
	public byte[] Bytes(bool includeSizes = true)
	{
		using MemoryStream ms = new();
		Write(stream: ms, includeSizes: includeSizes);
		return ms.ToArray();
	}
	#endregion Utilities
	#region IEditableModel
	public ushort SizeX { get; set; } = ushort.MaxValue;
	public ushort SizeY { get; set; } = ushort.MaxValue;
	public ushort SizeZ { get; set; } = ushort.MaxValue;
	public ushort[] Size
	{
		get => [SizeX, SizeY, SizeZ];
		set
		{
			if (value is null || value.Length != 3)
				throw new ArgumentException("Size must be an array of exactly 3 ushorts.");
			SizeX = value[0];
			SizeY = value[1];
			SizeZ = value[2];
		}
	}
	public byte this[ushort x, ushort y, ushort z]
	{
		get => FindVoxel(
			x: x,
			y: y,
			z: z,
			node: out _,
			octant: out _);
		set
		{
			if (this.IsOutside(x, y, z))
				throw new IndexOutOfRangeException("[" + string.Join(", ", x, y, z) + "] is not within size [" + string.Join(", ", SizeX, SizeY, SizeZ) + "]!");
			Branch branch = Root;
			byte octant;
			for (byte level = 15; level > 1; level--)
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
			if (branch[octant] is not Leaf leaf)
			{
				if (value == 0)
					return;
				leaf = (Leaf)(branch[octant] = new Leaf(branch, octant));
			}
			leaf[(byte)((z & 1) << 2 | (y & 1) << 1 | x & 1)] = value;
		}
	}
	public byte FindVoxel(ushort x, ushort y, ushort z, out Node node, out byte octant)
	{
		if (this.IsOutside(x, y, z))
			throw new IndexOutOfRangeException("[" + string.Join(", ", x, y, z) + "] is not within size [" + string.Join(", ", SizeX, SizeY, SizeZ) + "]!");
		Branch branch = Root;
		for (byte level = 15; level > 1; level--)
		{
			octant = (byte)((z >> level & 1) << 2 | (y >> level & 1) << 1 | x >> level & 1);
			if (branch[octant] is Branch child)
				branch = child;
			else
			{
				node = branch;
				return 0;
			}
		}
		octant = (byte)((z >> 1 & 1) << 2 | (y >> 1 & 1) << 1 | x >> 1 & 1);
		if (branch[octant] is Leaf leaf)
		{
			octant = (byte)((z & 1) << 2 | (y & 1) << 1 | x & 1);
			node = leaf;
			return leaf[octant];
		}
		node = branch;
		return 0;
	}
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	public IEnumerator<Voxel> GetEnumerator()
	{
		Stack<Branch> stack = new();
		void push(Branch branch)
		{
			while (branch is not null)
			{
				stack.Push(branch);
				branch = branch.First() as Branch;
			}
		}
		push(Root);
		while (stack.Count > 0 && stack.Pop() is Branch branch)
		{
			if (stack.Count == 14)
				foreach (Leaf leaf in branch.OfType<Leaf>())
					foreach (Voxel voxel in leaf)
						yield return voxel;
			if (branch.Parent is Branch parent
				&& parent.Next(branch.Octant) is Branch child)
				push(child);
		}
	}
	#endregion IEditableModel
	#region IBrickModel
	/// <summary>
	/// Gets a 2x2x2 brick at the specified coordinates.
	/// IBrickModel convention: Only return non-zero bricks (this is a SPARSE voxel octree).
	/// The tree structure automatically removes leaves that become all zeros, so if a leaf
	/// exists in the tree, it should always have at least one non-zero voxel.
	/// </summary>
	public ulong GetBrick(ushort x, ushort y, ushort z)
	{
		ushort brickX = (ushort)(x & ~1),
			brickY = (ushort)(y & ~1),
			brickZ = (ushort)(z & ~1);
		if (this.IsOutside(brickX, brickY, brickZ))
			return 0;
		Branch branch = Root;
		for (byte level = 15; level > 1; level--)
		{
			byte octant = (byte)((brickZ >> level & 1) << 2 |
				(brickY >> level & 1) << 1 |
				brickX >> level & 1);
			if (branch[octant] is Branch child)
				branch = child;
			else
				return 0; // No branch = no data (sparse)
		}
		byte leafOctant = (byte)((brickZ >> 1 & 1) << 2 |
			(brickY >> 1 & 1) << 1 |
			brickX >> 1 & 1);
		if (branch[leafOctant] is Leaf leaf)
		{
			// Defensive check: sparse tree should never contain all-zero leaves
			// (they get removed automatically by the indexer setter)
			return leaf.Data != 0 ? leaf.Data : 0;
		}
		return 0; // No leaf = no data (sparse)
	}
	/// <summary>
	/// Sets a 2x2x2 brick at the specified coordinates.
	/// Coordinates are snapped to brick origin (multiple of 2).
	/// </summary>
	public void SetBrick(ushort x, ushort y, ushort z, ulong payload)
	{
		// Snap to brick origin
		ushort brickX = (ushort)(x & ~1),
			brickY = (ushort)(y & ~1),
			brickZ = (ushort)(z & ~1);
		if (this.IsOutside(brickX, brickY, brickZ))
			throw new IndexOutOfRangeException("[" + string.Join(", ", brickX, brickY, brickZ) + "] is not within size [" + string.Join(", ", SizeX, SizeY, SizeZ) + "]!");
		Branch branch = Root;
		byte octant;
		// Navigate/create branches down to depth 15 (parent of leaves)
		for (byte level = 15; level > 1; level--)
		{
			octant = (byte)((brickZ >> level & 1) << 2 | (brickY >> level & 1) << 1 | brickX >> level & 1);
			if (branch[octant] is Branch child)
				branch = child;
			else
			{
				if (payload == 0ul)
					return; // No need to create structure for empty brick
				branch = (Branch)(branch[octant] = new Branch(branch, octant));
			}
		}
		// At depth 15, children are leaves
		octant = (byte)((brickZ >> 1 & 1) << 2 | (brickY >> 1 & 1) << 1 | brickX >> 1 & 1);
		if (payload == 0ul)
		{
			// Remove the leaf if it exists
			if (branch[octant] is Leaf)
				branch[octant] = null;
		}
		else
		{
			// Create or update the leaf
			if (branch[octant] is not Leaf leaf)
				leaf = (Leaf)(branch[octant] = new Leaf(branch, octant));
			leaf.Data = payload;
		}
	}
	IEnumerator<VoxelBrick> IEnumerable<VoxelBrick>.GetEnumerator()
	{
		Stack<(Branch branch, ushort x, ushort y, ushort z)> stack = new();
		stack.Push((Root, 0, 0, 0));
		while (stack.Count > 0)
		{
			(Branch branch, ushort baseX, ushort baseY, ushort baseZ) = stack.Pop();
			byte depth = branch.Depth;
			if (depth == 15)
			{
				// Parent of leaves - yield bricks
				for (byte octant = 0; octant < 8; octant++)
					if (branch[octant] is Leaf leaf && leaf.Data != 0)
					{
						ushort brickX = (ushort)(baseX | (octant & 1) << 1),
							brickY = (ushort)(baseY | (octant >> 1 & 1) << 1),
							brickZ = (ushort)(baseZ | (octant >> 2 & 1) << 1);
						if (brickX < SizeX && brickY < SizeY && brickZ < SizeZ)
							yield return new VoxelBrick(brickX, brickY, brickZ, leaf.Data);
					}
			}
			else
			{
				// Recurse into child branches
				int shift = 16 - depth;
				for (byte octant = 0; octant < 8; octant++)
					if (branch[octant] is Branch child)
					{
						ushort childX = (ushort)(baseX | (octant & 1) << shift),
							childY = (ushort)(baseY | (octant >> 1 & 1) << shift),
							childZ = (ushort)(baseZ | (octant >> 2 & 1) << shift);
						stack.Push((child, childX, childY, childZ));
					}
			}
		}
	}
	#endregion IBrickModel
	#region JSON
	public class SvoModelConverter : JsonConverter<SvoModel>
	{
		public override SvoModel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.StartObject)
				throw new JsonException("Expected start of object.");
			JsonObject jsonObject = JsonNode.Parse(ref reader).AsObject()
				?? throw new JsonException("Couldn't parse JSON object.");
			if (!jsonObject.TryGetPropertyValue("size", out JsonNode sizeNode) || sizeNode is null)
				throw new JsonException("Invalid model size. The size property must be an array.");
			ushort[] size = JsonSerializer.Deserialize<ushort[]>(sizeNode.ToJsonString(), options)
				?? throw new JsonException("Couldn't parse model size.");
			if (size.Length != 3)
				throw new JsonException($"Model size array must be length 3. Was: {size.Length}.");
			return new SvoModel(
				z85: jsonObject["z85"]?.GetValue<string>()
					?? throw new JsonException("z85 property missing in geometry."),
				sizeX: size[0],
				sizeY: size[1],
				sizeZ: size[2]);
		}
		public override void Write(Utf8JsonWriter writer, SvoModel value, JsonSerializerOptions options)
		{
			writer.WriteStartObject();
			writer.WritePropertyName("size");
			JsonSerializer.Serialize(writer, value.Size, options);
			writer.WritePropertyName("z85");
			JsonSerializer.Serialize(writer, value.Z85, options);
			writer.WriteEndObject();
		}
	}
	#endregion JSON
	#region Debug
#if DEBUG
	public uint NodeCount
	{
		get
		{
			uint nodes = 0;
			void Recurse(Node node)
			{
				nodes++;
				if (node is Branch branch)
					foreach (Node child in branch)
						Recurse(child);
			}
			Recurse(Root);
			return nodes;
		}
	}
	public string PrintStuff(ushort x, ushort y, ushort z)
	{
		if (this.IsOutside(x, y, z))
			throw new IndexOutOfRangeException("[" + string.Join(", ", x, y, z) + "] is not within size [" + string.Join(", ", SizeX, SizeY, SizeZ) + "]!");
		StringBuilder sb = new();
		static string print(Node node, byte @byte = 0)
		{
			node.Position(out ushort x1, out ushort y1, out ushort z1);
			node.Edge(@byte, out ushort edgeX, out ushort edgeY, out ushort edgeZ);
			return string.Join(", ",
				"x: " + x1,
				"y: " + y1,
				"z: " + z1,
				"depth: " + node.Depth,
				"size: " + node.Size,
				"octant: " + Convert.ToString(@byte, 2).PadLeft(3, '0'),
				"edgeX: " + edgeX,
				"edgeY: " + edgeY,
				"edgeZ: " + edgeZ);
		}
		byte octant;
		Branch branch = Root;
		for (int level = 15; level > 1; level--)
		{
			octant = (byte)((z >> level & 1) << 2 | (y >> level & 1) << 1 | x >> level & 1);
			sb.AppendLine(print(branch, octant));
			if (branch[octant] is Branch child)
				branch = child;
			else
				return sb.ToString();
		}
		octant = (byte)((z >> 1 & 1) << 2 | (y >> 1 & 1) << 1 | x >> 1 & 1);
		sb.AppendLine("last branch! " + print(branch, octant));
		if (branch[octant] is Leaf leaf)
		{
			octant = (byte)((z & 1) << 2 | (y & 1) << 1 | x & 1);
			sb.AppendLine("leaf! " + print(leaf, octant));
			return sb.ToString();
		}
		return sb.ToString();
	}
#endif
	#endregion Debug
}
