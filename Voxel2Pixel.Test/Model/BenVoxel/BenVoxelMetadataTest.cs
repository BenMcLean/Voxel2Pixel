using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voxel2Pixel.Model.BenVoxel;

namespace Voxel2Pixel.Test.Model.BenVoxel
{
	public class BenVoxelMetadataTest
	{
		[Fact]
		public void Test()
		{
			BenVoxelMetadata metadata = new();
			for (byte i = 1; i < 4; i++)
			{
				metadata.Properties["Property" + i] = "PropertyValue" + i;
				metadata.Points["Point" + i] = new Voxel2Pixel.Model.Point3D(i, i, i);
				metadata.Palettes["Palette" + i] = [i, i, i];
			}

		}
	}
}
