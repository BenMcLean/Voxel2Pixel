using System;

namespace VoxModel.SVO
{
	public class SVO
	{
		public abstract class Node
		{
			public virtual byte Header { get; }
			public virtual bool IsLeaf => (Header & 0b10000000) > 0;
		}
		public class Branch : Node
		{
			public override byte Header => (byte)(Children.Length - 1);
			public Node[] Children;
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
	}
}
