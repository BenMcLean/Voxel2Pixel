using System;
using System.Collections;
using System.Collections.Generic;

namespace BenVoxel;

/// <summary>
/// A flattened, shader-ready SVO data structure that implements IModel.
/// Use GpuSvoData.FromModel(source) to create instances.
/// </summary>
public class GpuSvoModel : IModel
{
	#region static
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
	#endregion static
	#region instance data
	// GPU Buffer 1: [ChildBaseIndex (24 bits) | ValidMask (8 bits)]
	// Passed to GPU as R32_UINT
	public uint[] Nodes { get; }
	// GPU Buffer 2: Material indices for leaf nodes
	// Passed to GPU as R8_UINT
	public byte[] Payloads { get; }
	// IModel Metadata
	public ushort SizeX { get; }
	public ushort SizeY { get; }
	public ushort SizeZ { get; }
	#endregion instance data
	public GpuSvoModel(IModel model)
	{
		BuildNode root = new();
		// 1. Build the temporary pointer-based tree
		foreach (Voxel voxel in model)
		{
			if (voxel.Index == 0) continue;
			InsertVoxel(
				node: root,
				x: voxel.X,
				y: voxel.Y,
				z: voxel.Z,
				payload: voxel.Index);
		}
		// 2. Flatten into BFS layout
		List<uint> finalNodes = [];
		List<byte> finalPayloads = [];
		// Queue: (Node, FlatIndex, Depth)
		Queue<(BuildNode node, int flatIndex, int depth)> processingQueue = new();
		// Initialize Root
		finalNodes.Add(0);
		processingQueue.Enqueue((root, 0, 0));
		while (processingQueue.Count > 0)
		{
			(BuildNode currNode, int flatIndex, int depth) = processingQueue.Dequeue();
			byte mask = 0;
			int childCount = 0;
			// A. Construct Mask
			for (int i = 0; i < 8; i++)
				if (currNode.Children[i] != null)
				{
					mask |= (byte)(1 << i);
					childCount++;
				}
			// If empty internal node (shouldn't happen often with sparse build, but safe to handle)
			if (childCount == 0 && depth < 16)
			{
				finalNodes[flatIndex] = 0;
				continue;
			}
			// B. Determine where children go
			uint childBaseIndex;
			if (depth < 15)
			{
				// Children are internal nodes -> Index into finalNodes
				childBaseIndex = (uint)finalNodes.Count;
				// Reserve space
				for (int i = 0; i < childCount; i++)
					finalNodes.Add(0);
				// Enqueue children
				int currentOffset = 0;
				for (int i = 0; i < 8; i++)
					if (currNode.Children[i] != null)
					{
						processingQueue.Enqueue((currNode.Children[i], (int)(childBaseIndex + currentOffset), depth + 1));
						currentOffset++;
					}
			}
			else
			{
				// Children are payloads -> Index into finalPayloads
				childBaseIndex = (uint)finalPayloads.Count;
				for (int i = 0; i < 8; i++)
				{
					if (currNode.Children[i] != null)
						finalPayloads.Add(currNode.Children[i].Payload);
				}
			}
			// Safety check for the 24-bit pointer limit
			if (childBaseIndex > 0xFFFFFF)
				throw new InvalidOperationException("Model too complex: Node index exceeded 24 bits.");
			// C. Pack data: [ChildBaseIndex (24 bits) | ValidMask (8 bits)]
			finalNodes[flatIndex] = (childBaseIndex << 8) | mask;
		}
		Nodes = [.. finalNodes];
		Payloads = [.. finalPayloads];
		SizeX = model.SizeX;
		SizeY = model.SizeY;
		SizeZ = model.SizeZ;
	}
	private static void InsertVoxel(BuildNode node, ushort x, ushort y, ushort z, byte payload)
	{
		for (int depth = 0; depth < 16; depth++)
		{
			int shift = 15 - depth,
				bitX = (x >> shift) & 1,
				bitY = (y >> shift) & 1,
				bitZ = (z >> shift) & 1,
				octant = (bitZ << 2) | (bitY << 1) | bitX;
			if (node.Children[octant] == null)
				node.Children[octant] = new BuildNode();
			node = node.Children[octant];
		}
		node.Payload = payload;
	}
	// Internal temporary structure
	private class BuildNode
	{
		public BuildNode[] Children = new BuildNode[8]; // null if empty
		public byte Payload = 0; // Only used at leaf level
	}
	#region IModel
	public byte this[ushort x, ushort y, ushort z]
	{
		get
		{
			if (x >= SizeX || y >= SizeY || z >= SizeZ) return 0;
			uint nodeIndex = 0;
			for (int depth = 0; depth < 16; depth++)
			{
				uint nodeData = Nodes[nodeIndex];
				byte mask = (byte)(nodeData & 0xFF);
				int shift = 15 - depth,
					octant = (((z >> shift) & 1) << 2) | (((y >> shift) & 1) << 1) | ((x >> shift) & 1);
				if ((mask & (1 << octant)) == 0)
					return 0;
				int maskLower = (1 << octant) - 1,
					childOffset = PopCount8[mask & maskLower];
				uint childBaseIndex = nodeData >> 8,
					nextIndex = childBaseIndex + (uint)childOffset;
				if (depth == 15)
					return Payloads[nextIndex];
				nodeIndex = nextIndex;
			}
			return 0;
		}
	}
	public IEnumerator<Voxel> GetEnumerator()
	{
		Stack<(uint index, int depth, ushort x, ushort y, ushort z)> stack = new();
		stack.Push((0, 0, 0, 0, 0));
		while (stack.Count > 0)
		{
			(uint currIndex, int depth, ushort x, ushort y, ushort z) = stack.Pop();
			uint nodeData = Nodes[currIndex];
			byte mask = (byte)(nodeData & 0xFF);
			uint childBaseIndex = nodeData >> 8;
			for (int i = 0; i < 8; i++)
			{
				if ((mask & (1 << i)) != 0)
				{
					int shift = 15 - depth;
					ushort nextX = (ushort)(x | ((i & 1) << shift)),
						nextY = (ushort)(y | (((i >> 1) & 1) << shift)),
						nextZ = (ushort)(z | (((i >> 2) & 1) << shift));
					int maskLower = (1 << i) - 1,
						offset = PopCount8[mask & maskLower];
					uint nextIndex = childBaseIndex + (uint)offset;
					if (depth == 15)
					{
						byte payload = Payloads[nextIndex];
						if (nextX < SizeX && nextY < SizeY && nextZ < SizeZ)
							yield return new Voxel(nextX, nextY, nextZ, payload);
					}
					else
						stack.Push((nextIndex, depth + 1, nextX, nextY, nextZ));
				}
			}
		}
	}
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	#endregion IModel
}
