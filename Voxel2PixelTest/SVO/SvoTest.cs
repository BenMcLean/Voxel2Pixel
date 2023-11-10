using System.Linq;
using Xunit;
using static Voxel2Pixel.SVO.SVO;

namespace Voxel2PixelTest.SVO
{
	public class SvoTest
	{
		private readonly Xunit.Abstractions.ITestOutputHelper output;
		public SvoTest(Xunit.Abstractions.ITestOutputHelper output) => this.output = output;

		[Fact]
		public void ByteTest()
		{
			byte[] bytes = Enumerable.Range(1, 8).Select(i => (byte)i).ToArray();
			Leaf leaf = new Leaf();
			for (byte i = 0; i < bytes.Length; i++)
			{
				output.WriteLine("before: " + string.Format("0x{0:X}", leaf.Data));
				leaf[i] = bytes[i];
				output.WriteLine("after: " + string.Format("0x{0:X}", leaf.Data));
				output.WriteLine("bytes[" + i + "] = " + bytes[i] + ", leaf[" + i + "] = " + leaf[i]);
			}
			for (byte i = 0; i < bytes.Length; i++)
				Assert.Equal(
					expected: bytes[i],
					actual: leaf[i]);
		}
	}
}
