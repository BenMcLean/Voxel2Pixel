using System.IO;

namespace Voxel2Pixel.Interfaces
{
	public interface IBinaryWritable
	{
		void Write(Stream stream);
	}
}
