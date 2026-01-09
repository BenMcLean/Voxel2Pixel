using System.IO;

namespace BenVoxel.Interfaces;

public interface IBinaryWritable
{
	void Write(Stream stream);
	void Write(BinaryWriter writer);
}
