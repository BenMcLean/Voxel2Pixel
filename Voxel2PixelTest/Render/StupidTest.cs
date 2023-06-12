using System.Collections.Generic;
using Voxel2Pixel.Model;
using Voxel2PixelTest.Pack;
using Xunit;

namespace Voxel2PixelTest.Render
{
    public class StupidTest
    {
        [Fact]
        public void Fact()
        {
            byte[][][] bytes = Pack8Test.Pyramid(17);
            List<ArrayModel> models = new List<ArrayModel>();
            models.Add(new ArrayModel(bytes));

        }
    }
}
