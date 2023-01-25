using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace Voxel2PixelTest
{
	public class VoxModelTest
	{
		const string path = @"..\..\..\Sora.vox";
		[Fact]
		public void ArrayRendererTest()
		{
			if (!File.Exists(path))
				throw new FileNotFoundException(Path.GetFullPath(path));
		}
	}
}
