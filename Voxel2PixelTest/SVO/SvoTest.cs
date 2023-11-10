﻿using System.Linq;
using Voxel2Pixel.Model;
using Xunit;
using Voxel2Pixel.SVO;
using static Voxel2Pixel.SVO.SVO;

namespace Voxel2PixelTest.SVO
{
	public class SvoTest
	{
		private readonly Xunit.Abstractions.ITestOutputHelper output;
		public SvoTest(Xunit.Abstractions.ITestOutputHelper output) => this.output = output;

		[Fact]
		public void LeafTest()
		{
			byte[] bytes = Enumerable.Range(1, 8).Select(i => (byte)i).ToArray();
			Leaf leaf = new Leaf();
			for (byte i = 0; i < bytes.Length; i++)
			{
				leaf[i] = bytes[i];
				Assert.Equal(
					expected: bytes[i],
					actual: leaf[i]);
			}
		}
		[Fact]
		public void ModelTest()
		{
			VoxFileModel model = new VoxFileModel(@"..\..\..\Sora.vox");
			Voxel2Pixel.SVO.SVO svo = new Voxel2Pixel.SVO.SVO(model);
		}
	}
}
