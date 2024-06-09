using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voxel2Pixel.Model.FileFormats;

namespace Voxel2Pixel.Test.Model
{
	public class VengiModelTest
	{
		[Fact]
		public void KingKong()
		{
			new VengiModel(new FileStream(@"..\..\..\kingkong.vengi", FileMode.Open));
		}
	}
}
