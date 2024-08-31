using SixLabors.ImageSharp;
using Voxel2Pixel.Render;
using static Voxel2Pixel.Web.ImageMaker;

namespace Voxel2Pixel.Test.Draw
{
	public class ArbitraryTriangleTest
	{
		[Fact]
		public void Test()
		{
			Sprite sprite = new(width: 100, height: 100);
			sprite.DrawTriangle(
				color: 0xFF0000FFu,
				a: new Voxel2Pixel.Model.Point(X: 1, Y: 1),
				b: new Voxel2Pixel.Model.Point(X: 1, Y: 99),
				c: new Voxel2Pixel.Model.Point(X: 99, Y: 99),
				bounds: sprite.Size());
			sprite.Png().SaveAsPng("ArbitraryTriangleTest.png");
		}
	}
}
