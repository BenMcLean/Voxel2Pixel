using SixLabors.ImageSharp;
using Voxel2Pixel.Pack;
using Xunit;
using static Voxel2Pixel.ExtensionMethods;
using static Voxel2PixelBlazor.ImageMaker;

namespace Voxel2PixelTest.Draw
{
	public class ArbitraryTriangleTest
	{
		[Fact]
		public void Test()
		{
			Sprite sprite = new(width: 100, height: 100);
			sprite.DrawTriangle(
				color: 0xFF0000FFu,
				new Voxel2Pixel.Model.Point(X: 1, Y: 1),
				new Voxel2Pixel.Model.Point(X: 2, Y: 98),
				new Voxel2Pixel.Model.Point(X: 99, Y: 99));
			sprite.Png().SaveAsPng("ArbitraryTriangleTest.png");
		}
	}
}
