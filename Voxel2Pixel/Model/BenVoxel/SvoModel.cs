﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Interfaces;

namespace Voxel2Pixel.Model.BenVoxel
{
	/// <summary>
	/// SVO stands for "Sparse Voxel Octree"
	/// </summary>
	[XmlRoot("Geometry")]
	public class SvoModel : IEditableModel, ISpecializedModel, IBinaryWritable, IXmlSerializable
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
			public Branch(Node parent = null, byte octant = 0)
			{
				Parent = parent;
				Octant = octant;
			}
			public Branch(Stream stream, Node parent = null) : this(new BinaryReader(input: stream, encoding: Encoding.UTF8, leaveOpen: true), parent) { }
			public Branch(BinaryReader reader, Node parent = null)
			{
				Parent = parent;
				byte header = reader.ReadByte(),
					children = (byte)((header >> 3 & 7) + 1);
				Octant = (byte)(header & 0b111);
				for (byte child = 0; child < children; child++)
				{
					header = reader.ReadByte();
					reader.BaseStream.Position--;
					this[(byte)(header & 7)] = (header & 0b11000000) > 0 ?
						new Leaf(reader, this)
						: new Branch(reader, this);
				}
			}
			#region IBinaryWritable
			public override void Write(Stream stream) => Write(new BinaryWriter(output: stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true));
			public override void Write(BinaryWriter writer)
			{
				writer.Write(Header);
				foreach (Node child in this)
					child.Write(writer);
			}
			#endregion IBinaryWritable
			#region IEnumerable<Node>
			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
			public IEnumerator<Node> GetEnumerator() => Children.OfType<Node>().GetEnumerator();
			#endregion IEnumerable<Node>
		}
		public class Leaf : Node, IBinaryWritable, IEnumerable<Voxel>, IEnumerable
		{
			public override byte Header => (byte)(0b11000000 | Octant & 7);
			protected byte[] Data = new byte[8];
			public override void Clear() => Data = new byte[8];
			public byte this[byte octant]
			{
				get => Data[octant];
				set
				{
					Data[octant] = value;
					if (!Data.Any(@byte => @byte != 0) && Parent is Branch parent)
						parent[Octant] = null;
				}
			}
			public override byte Depth => 16;
			public override ushort Size => 1;
			public Leaf(Node parent, byte octant)
			{
				Parent = parent;
				Octant = octant;
			}
			public Leaf(Stream stream, Node parent = null) : this(new BinaryReader(input: stream, encoding: Encoding.UTF8, leaveOpen: true), parent) { }
			public Leaf(BinaryReader reader, Node parent = null)
			{
				Parent = parent;
				byte header = reader.ReadByte();
				Octant = (byte)(header & 7);
				switch ((byte)(header >> 6 & 3))
				{
					case 0b01:
						Data = [.. Enumerable.Repeat(reader.ReadByte(), 8)];
						break;
					case 0b10:
						byte where = (byte)(header >> 3 & 7),
							foreground = reader.ReadByte(),
							background = reader.ReadByte();
						Data = [.. Enumerable.Range(0, 8).Select(i => i == where ? foreground : background)];
						break;
					default: // 0b11
						Data = reader.ReadBytes(8);
						break;
				}
			}
			#region IBinaryWritable
			public override void Write(Stream stream) => Write(new BinaryWriter(output: stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true));
			public override void Write(BinaryWriter writer)
			{
				(byte, byte)[] occurrences = [.. Data
					.GroupBy(b => b)
					.Select(g => (g.Key, (byte)g.Count()))
					.OrderBy(t => t.Item2)];
				if (occurrences.Length == 1)
				{
					writer.Write((byte)(0b01000000 | Octant & 7)); // Header
					writer.Write(Data[0]);
				}
				else if (occurrences.Length == 2 && occurrences[0].Item2 == 1)
				{
					writer.Write((byte)(0b10000000 | (Array.IndexOf(Data, occurrences[0].Item1) & 7) << 3 | Octant & 7)); // Header
					writer.Write(occurrences[0].Item1); // Foreground
					writer.Write(occurrences[1].Item1); // Background
				}
				else
				{
					writer.Write(Header);
					writer.Write(Data);
				}
			}
			#endregion IBinaryWritable
			public Voxel Voxel(byte octant)
			{
				Position(out ushort x, out ushort y, out ushort z);
				return new Voxel(
					X: (ushort)(x | octant & 1),
					Y: (ushort)(y | octant >> 1 & 1),
					Z: (ushort)(z | octant >> 2 & 1),
					Index: this[octant]);
			}
			#region IEnumerable<Voxel>
			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
			public IEnumerator<Voxel> GetEnumerator()
			{
				if (!Data.Any(@byte => @byte != 0))
					yield break;
				Position(out ushort x, out ushort y, out ushort z);
				for (byte octant = 0; octant < 8; octant++)
					if (this[octant] is byte index && index != 0)
						yield return new Voxel(
							X: (ushort)(x | octant & 1),
							Y: (ushort)(y | octant >> 1 & 1),
							Z: (ushort)(z | octant >> 2 & 1),
							Index: index);
			}
			#endregion IEnumerable<Voxel>
		}
		#endregion Nested classes
		#region SvoModel
		public readonly Branch Root = new();
		public void Clear() => Root.Clear();
		public SvoModel() { }
		public SvoModel(IModel model) : this(
			voxels: model,
			sizeX: model.SizeX,
			sizeY: model.SizeY,
			sizeZ: model.SizeZ)
		{ }
		public SvoModel(IEnumerable<Voxel> voxels, ushort sizeX = ushort.MaxValue, ushort sizeY = ushort.MaxValue, ushort sizeZ = ushort.MaxValue) : this(sizeX, sizeY, sizeZ)
		{
			foreach (Voxel voxel in voxels)
				this.Set(voxel);
		}
		public SvoModel(Stream stream, ushort sizeX, ushort sizeY, ushort sizeZ) : this(new BinaryReader(input: stream, encoding: Encoding.UTF8, leaveOpen: true), sizeX, sizeY, sizeZ) { }
		public SvoModel(Stream stream) : this(new BinaryReader(input: stream, encoding: Encoding.UTF8, leaveOpen: true)) { }
		public SvoModel(BinaryReader reader, ushort sizeX, ushort sizeY, ushort sizeZ) : this(sizeX, sizeY, sizeZ) => Root = new Branch(reader);
		public SvoModel(BinaryReader reader) : this(reader, reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16()) { }
		public SvoModel(byte[] bytes, ushort sizeX, ushort sizeY, ushort sizeZ) : this(new MemoryStream(bytes), sizeX, sizeY, sizeZ) { }
		public SvoModel(byte[] bytes) : this(new MemoryStream(bytes)) { }
		public SvoModel(string z85, ushort sizeX = ushort.MaxValue, ushort sizeY = ushort.MaxValue, ushort sizeZ = ushort.MaxValue) : this(Cromulent.Encoding.Z85.FromZ85String(z85), sizeX, sizeY, sizeZ) { }
		public SvoModel(ushort sizeX = ushort.MaxValue, ushort sizeY = ushort.MaxValue, ushort sizeZ = ushort.MaxValue) : this()
		{
			SizeX = sizeX;
			SizeY = sizeY;
			SizeZ = sizeZ;
		}
		#endregion SvoModel
		#region IBinaryWritable
		public void Write(Stream stream) => Write(stream, true);
		public void Write(Stream stream, bool includeSizes) => Write(writer: new BinaryWriter(output: stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true), includeSizes: includeSizes);
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
				writer.Write((byte)0x40);//Leaf header
				writer.Write((byte)0);//Empty 2x2x2 voxel cube
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
		public string Z85(bool includeSizes = false)
		{
			using MemoryStream ms = new();
			Write(stream: ms, includeSizes: includeSizes);
			if (ms.Position % 4 is long four && four > 0)
				using (BinaryWriter writer = new(
					output: ms,
					encoding: Encoding.UTF8,
					leaveOpen: true))
					for (byte @byte = 0; @byte < 4 - four; @byte++)
						writer.Write((byte)0);
			return Cromulent.Encoding.Z85.ToZ85String(ms.ToArray());
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
						foreach (Node child in branch)
							Recurse(child);
				}
				Recurse(Root);
				return nodes;
			}
		}
		#endregion Utilities
		#region IEditableModel
		public ushort SizeX { get; set; } = ushort.MaxValue;
		public ushort SizeY { get; set; } = ushort.MaxValue;
		public ushort SizeZ { get; set; } = ushort.MaxValue;
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
		#region ISpecializedModel
		public void Draw(IRenderer renderer, Perspective perspective, byte peakScaleX = 6, byte peakScaleY = 6, double radians = 0d)
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
					VoxelDraw.Draw(this, renderer, perspective, peakScaleX, peakScaleY, radians);
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
		#region IXmlSerializable
		public XmlSchema GetSchema() => null;
		public void ReadXml(XmlReader reader)
		{
			XElement root = XElement.Load(reader);
			SvoModel read = new(
				z85: root.Value,
				sizeX: ushort.TryParse(root.Attribute("Width")?.Value, out ushort sizeX) ? sizeX : ushort.MaxValue,
				sizeY: ushort.TryParse(root.Attribute("Depth")?.Value, out ushort sizeY) ? sizeY : ushort.MaxValue,
				sizeZ: ushort.TryParse(root.Attribute("Height")?.Value, out ushort sizeZ) ? sizeZ : ushort.MaxValue);
			Clear();
			SizeX = read.SizeX;
			SizeY = read.SizeY;
			SizeZ = read.SizeZ;
			foreach (Voxel voxel in read)
				this[voxel.X, voxel.Y, voxel.Z] = voxel.Index;
		}
		public void WriteXml(XmlWriter writer) => new XElement(XName.Get("Geometry"),
				new XAttribute(XName.Get("Width"), SizeX),
				new XAttribute(XName.Get("Depth"), SizeY),
				new XAttribute(XName.Get("Height"), SizeZ),
				Z85(includeSizes: false))
			.WriteTo(writer);
		#endregion IXmlSerializable
		#region Debug
		/*
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
		*/
		#endregion Debug
	}
}
