using System.IO;

namespace BenVoxel;

public interface IBinaryWritable
{
	void Write(Stream stream);
	void Write(BinaryWriter writer);
}
