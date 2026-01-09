using System;
using System.Collections;
using System.Collections.Generic;

namespace BenVoxel;

public class GpuSvoModel : IBrickModel
{
	#region Constants & Static
	// Internal starts with 1. Leaves start with 0.
	private const uint FLAG_INTERNAL = 0x80000000, // 1xxx...
		FLAG_LEAF_TYPE = 0x40000000; // Bit 30: 0 = Uniform, 1 = Brick
	private static readonly byte[] PopCount8;
	static GpuSvoModel()
	{
		PopCount8 = new byte[256];
		for (int i = 0; i < 256; i++)
			PopCount8[i] = CountSetBits((byte)i);
	}
	private static byte CountSetBits(byte n)
	{
		byte count = 0;
		while (n > 0)
		{
			n &= (byte)(n - 1); // This clears the least significant bit set
			count++;
		}
		return count;
	}
	#endregion
	#region Instance data
	public uint[] Nodes { get; }
	public ulong[] Payloads { get; }
	public ushort SizeX { get; }
	public ushort SizeY { get; }
	public ushort SizeZ { get; }
	public byte MaxDepth { get; }
	#endregion
	#region GpuSvoModel
	public GpuSvoModel(IModel model)
	{
		SizeX = model.SizeX;
		SizeY = model.SizeY;
		SizeZ = model.SizeZ;
		ushort maxDim = Math.Max(SizeX, Math.Max(SizeY, SizeZ)),
			fullDepth = 0;
		while ((1 << fullDepth) < maxDim) fullDepth++;
		MaxDepth = (byte)Math.Max(1, (int)fullDepth);
		BuildNode root = new();
		// Optimize: if model implements IBrickModel, use brick-based construction
		if (model is IBrickModel brickModel)
			foreach (VoxelBrick brick in (IEnumerable<VoxelBrick>)brickModel)
				InsertBrick(root, brick.X, brick.Y, brick.Z, brick.Payload, MaxDepth);
		else
			foreach (Voxel voxel in model)
				InsertVoxel(root, voxel.X, voxel.Y, voxel.Z, voxel.Material, MaxDepth);
		PruneTree(root);
		FlattenTree(root, out uint[] nodes, out ulong[] payloads);
		Nodes = nodes;
		Payloads = payloads;
	}
	private void FlattenTree(BuildNode root, out uint[] nodes, out ulong[] payloads)
	{
		List<uint> flatNodes = [];
		List<ulong> flatPayloads = [];
		Queue<(BuildNode node, int flatIdx)> queue = [];
		flatNodes.Add(0);
		queue.Enqueue((root, 0));
		while (queue.Count > 0)
		{
			(BuildNode curr, int idx) = queue.Dequeue();
			if (curr.IsLeaf)
			{
				// Fast uniform check using magic number pattern
				ulong first = curr.Payload & 0xFF;
				bool isUniform = curr.Payload == first * 0x0101010101010101UL;
				if (isUniform)
					// Schema: [ 0 | 0 (type) | ... | Material (8b) ]
					flatNodes[idx] = (uint)first;
				else
				{
					// Schema: [ 0 | 1 (type) | PayloadIdx (30b) ]
					uint pIdx = (uint)flatPayloads.Count;
					flatPayloads.Add(curr.Payload);
					flatNodes[idx] = FLAG_LEAF_TYPE | pIdx;
				}
			}
			else
			{
				byte mask = 0;
				for (int i = 0; i < 8; i++)
					if (curr.Children[i] != null) mask |= (byte)(1 << i);
				if (mask == 0) { flatNodes[idx] = 0; continue; } // Entirely empty
				uint childBase = (uint)flatNodes.Count;
				int activeCount = PopCount8[mask];
				for (int i = 0; i < activeCount; i++) flatNodes.Add(0);
				int offset = 0;
				for (int i = 0; i < 8; i++) // Morton Order
					if (curr.Children[i] != null)
					{
						queue.Enqueue((curr.Children[i], (int)childBase + offset));
						offset++;
					}
				// Schema: [ 1 | ChildBase (23b) | Mask (8b) ]
				flatNodes[idx] = FLAG_INTERNAL | (childBase << 8) | mask;
			}
		}
		nodes = [.. flatNodes];
		payloads = [.. flatPayloads];
	}
	private static void InsertBrick(BuildNode node, ushort x, ushort y, ushort z, ulong payload, int maxDepth)
	{
		// Navigate to parent of leaf level (maxDepth - 1)
		for (int depth = 0; depth < maxDepth - 1; depth++)
		{
			int shift = maxDepth - 1 - depth,
				oct = (((z >> shift) & 1) << 2) | (((y >> shift) & 1) << 1) | ((x >> shift) & 1);
			node.Children[oct] ??= new BuildNode();
			node = node.Children[oct];
		}
		// Insert entire brick at leaf level
		node.IsLeaf = true;
		node.Payload = payload;
	}
	private static void InsertVoxel(BuildNode node, ushort x, ushort y, ushort z, byte material, int maxDepth)
	{
		for (int depth = 0; depth < maxDepth - 1; depth++)
		{
			int shift = maxDepth - 1 - depth,
				oct = (((z >> shift) & 1) << 2) | (((y >> shift) & 1) << 1) | ((x >> shift) & 1);
			node.Children[oct] ??= new BuildNode();
			node = node.Children[oct];
		}
		int brickOct = ((z & 1) << 2) | ((y & 1) << 1) | (x & 1);
		node.IsLeaf = true;
		node.Payload |= (ulong)material << (brickOct * 8);
	}
	private bool PruneTree(BuildNode node)
	{
		if (node.IsLeaf)
		{
			// Fast uniform check using magic number pattern
			ulong first = node.Payload & 0xFF;
			return node.Payload == first * 0x0101010101010101UL;
		}
		bool allChildrenHomo = true;
		ulong? firstMat = null;
		int count = 0;
		for (int i = 0; i < 8; i++)
		{
			if (node.Children[i] != null)
			{
				count++;
				bool childHomo = PruneTree(node.Children[i]);
				if (!childHomo) { allChildrenHomo = false; break; }
				ulong m = node.Children[i].Payload & 0xFF;
				if (firstMat == null) firstMat = m;
				else if (firstMat != m) { allChildrenHomo = false; break; }
			}
			else { allChildrenHomo = false; break; }
		}
		if (allChildrenHomo && count == 8)
		{
			node.IsLeaf = true;
			ulong p = firstMat ?? 0;
			node.Payload = p | (p << 8) | (p << 16) | (p << 24) | (p << 32) | (p << 40) | (p << 48) | (p << 56);
			node.Children = new BuildNode[8];
			return true;
		}
		return false;
	}
	private class BuildNode { public BuildNode[] Children = new BuildNode[8]; public bool IsLeaf; public ulong Payload; }
	#endregion
	#region IModel Implementation
	public byte this[ushort x, ushort y, ushort z]
	{
		get
		{
			if (x >= SizeX || y >= SizeY || z >= SizeZ) return 0;
			uint nIdx = 0;
			for (int depth = 0; depth < MaxDepth - 1; depth++)
			{
				uint data = Nodes[nIdx];
				// Test bit 31 (Internal vs Leaf)
				if ((data & FLAG_INTERNAL) == 0)
					return GetLeafVoxel(data, x, y, z);
				byte mask = (byte)(data & 0xFF);
				int shift = MaxDepth - 1 - depth,
					oct = (((z >> shift) & 1) << 2) | (((y >> shift) & 1) << 1) | ((x >> shift) & 1);
				if ((mask & (1 << oct)) == 0) return 0;
				uint childBase = (data & ~FLAG_INTERNAL) >> 8;
				nIdx = childBase + PopCount8[mask & ((1 << oct) - 1)];
			}
			return GetLeafVoxel(Nodes[nIdx], x, y, z);
		}
	}
	private byte GetLeafVoxel(uint data, ushort x, ushort y, ushort z)
	{
		// Test bit 30 (Uniform vs Brick)
		if ((data & FLAG_LEAF_TYPE) == 0)
			return (byte)(data & 0xFF); // Uniform
		int pIdx = (int)(data & ~FLAG_LEAF_TYPE),
			oct = ((z & 1) << 2) | ((y & 1) << 1) | (x & 1);
		return (byte)((Payloads[pIdx] >> (oct * 8)) & 0xFF);
	}
	public IEnumerator<Voxel> GetEnumerator()
	{
		Stack<(uint idx, int depth, ushort x, ushort y, ushort z)> stack = new();
		stack.Push((0, 0, 0, 0, 0));
		while (stack.Count > 0)
		{
			(uint cIdx, int depth, ushort x, ushort y, ushort z) = stack.Pop();
			uint data = Nodes[cIdx];
			if ((data & FLAG_INTERNAL) == 0)
			{
				int totalSize = 1 << (MaxDepth - depth),
					unitSize = totalSize >> 1;
				ulong p;
				if ((data & FLAG_LEAF_TYPE) == 0)
				{
					ulong n = data & 0xFF;
					p = n | (n << 8) | (n << 16) | (n << 24) | (n << 32) | (n << 40) | (n << 48) | (n << 56);
				}
				else p = Payloads[data & ~FLAG_LEAF_TYPE];
				for (int i = 0; i < 8; i++)
				{
					byte mat = (byte)((p >> (i * 8)) & 0xFF);
					if (mat == 0)
						continue;
					ushort ox = (ushort)(x | ((i & 1) * unitSize)),
						oy = (ushort)(y | (((i >> 1) & 1) * unitSize)),
						oz = (ushort)(z | (((i >> 2) & 1) * unitSize));
					for (int dz = 0; dz < unitSize; dz++)
						for (int dy = 0; dy < unitSize; dy++)
							for (int dx = 0; dx < unitSize; dx++)
							{
								ushort fx = (ushort)(ox + dx),
									fy = (ushort)(oy + dy),
									fz = (ushort)(oz + dz);
								if (fx < SizeX && fy < SizeY && fz < SizeZ)
									yield return new Voxel(fx, fy, fz, mat);
							}
				}
				continue;
			}
			byte m = (byte)(data & 0xFF);
			uint baseIdx = (data & ~FLAG_INTERNAL) >> 8;
			int off = PopCount8[m] - 1;
			for (int i = 7; i >= 0; i--) if ((m & (1 << i)) != 0)
				{
					int s = MaxDepth - 1 - depth;
					stack.Push((baseIdx + (uint)off--, depth + 1, (ushort)(x | ((i & 1) << s)), (ushort)(y | (((i >> 1) & 1) << s)), (ushort)(z | (((i >> 2) & 1) << s))));
				}
		}
	}
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	#endregion
	#region IBrickModel
	/// <summary>
	/// Gets a 2x2x2 brick at the specified coordinates.
	/// IBrickModel convention: Only return non-zero bricks (this is a SPARSE voxel octree).
	/// </summary>
	public ulong GetBrick(ushort x, ushort y, ushort z)
	{
		ushort brickX = (ushort)(x & ~1);
		ushort brickY = (ushort)(y & ~1);
		ushort brickZ = (ushort)(z & ~1);
		if (brickX >= SizeX || brickY >= SizeY || brickZ >= SizeZ)
			return 0;
		uint nIdx = 0;
		for (int depth = 0; depth < MaxDepth - 1; depth++)
		{
			uint data = Nodes[nIdx];
			// Test bit 31 (Internal vs Leaf)
			if ((data & FLAG_INTERNAL) == 0)
				return GetLeafBrick(data);
			byte mask = (byte)(data & 0xFF);
			int shift = MaxDepth - 1 - depth,
				oct = (((brickZ >> shift) & 1) << 2) | (((brickY >> shift) & 1) << 1) | ((brickX >> shift) & 1);
			if ((mask & (1 << oct)) == 0)
				return 0; // No data (sparse)
			uint childBase = (data & ~FLAG_INTERNAL) >> 8;
			nIdx = childBase + PopCount8[mask & ((1 << oct) - 1)];
		}
		return GetLeafBrick(Nodes[nIdx]);
	}

	private ulong GetLeafBrick(uint data)
	{
		// Test bit 30 (Uniform vs Brick)
		if ((data & FLAG_LEAF_TYPE) == 0)
		{
			// Uniform leaf - all 8 voxels same color
			ulong mat = data & 0xFF;
			return mat | (mat << 8) | (mat << 16) | (mat << 24) |
			       (mat << 32) | (mat << 40) | (mat << 48) | (mat << 56);
		}
		// Brick leaf - return payload directly
		int pIdx = (int)(data & ~FLAG_LEAF_TYPE);
		return Payloads[pIdx];
	}
	IEnumerator<VoxelBrick> IEnumerable<VoxelBrick>.GetEnumerator()
	{
		Stack<(uint idx, int depth, ushort x, ushort y, ushort z)> stack = new();
		stack.Push((0, 0, 0, 0, 0));
		while (stack.Count > 0)
		{
			(uint cIdx, int depth, ushort x, ushort y, ushort z) = stack.Pop();
			uint data = Nodes[cIdx];
			if ((data & FLAG_INTERNAL) == 0)
			{
				// Leaf node - yield as brick
				ulong payload = GetLeafBrick(data);
				if (payload != 0 && x < SizeX && y < SizeY && z < SizeZ)
					yield return new VoxelBrick(x, y, z, payload);
				continue;
			}
			// Internal node - recurse into children
			byte mask = (byte)(data & 0xFF);
			uint baseIdx = (data & ~FLAG_INTERNAL) >> 8;
			int off = PopCount8[mask] - 1;
			for (int i = 7; i >= 0; i--)
				if ((mask & (1 << i)) != 0)
				{
					int shift = MaxDepth - 1 - depth;
					ushort childX = (ushort)(x | ((i & 1) << shift));
					ushort childY = (ushort)(y | (((i >> 1) & 1) << shift));
					ushort childZ = (ushort)(z | (((i >> 2) & 1) << shift));
					stack.Push((baseIdx + (uint)off--, depth + 1, childX, childY, childZ));
				}
		}
	}
	#endregion
}
