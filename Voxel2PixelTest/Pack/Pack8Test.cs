using SixLabors.ImageSharp;
using System.Linq;
using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Model;
using Voxel2Pixel.Pack;
using Voxel2PixelTest.Model;
using Xunit;

namespace Voxel2PixelTest.Pack
{
	public class Pack8Test
	{
		#region Tests
		[Fact]
		public void IsoSpritesTest()
		{
			VoxModel model = new VoxModel(@"..\..\..\Sora.vox");
			Iso8Gif(
				model: model,
				voxelColor: new NaiveDimmer(model.Palette),
				path: "IsoSpritesTest.gif");
		}
		[Fact]
		public void PyramidTest() => Iso8Gif(
			model: new ArrayModel(Pyramid(17)),
			voxelColor: new NaiveDimmer(ArrayModelTest.RainbowPalette),
			path: "PyramidTest.gif",
			originX: 0,
			originY: 0,
			originZ: 0);
		[Fact]
		public void Pyramid2Test() => Iso8Gif(
			model: new ArrayModel(Pyramid2(16, 4)),
			voxelColor: new NaiveDimmer(ArrayModelTest.RainbowPalette),
			path: "Pyramid2Test.gif",
			originX: 0,
			originY: 0,
			originZ: 0);
		[Fact]
		public void NumberCubeTest()
		{
			VoxModel model = new VoxModel(@"..\..\..\NumberCube.vox");
			Iso8Gif(
				model: model,
				voxelColor: new NaiveDimmer(model.Palette),
				path: "NumberCubeTest.gif",
				originX: 0,
				originY: 0,
				originZ: 0);
		}
		#endregion Tests
		#region Model creation
		public static byte[][][] Pyramid(int width, params byte[] colors)
		{
			if (colors is null || colors.Length < 1)
				colors = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
			int halfWidth = width >> 1;
			byte[][][] voxels = Bytes3D.Initialize<byte>(width, width, halfWidth + 1);
			voxels[0][0][0] = colors[1];
			voxels[width - 1][0][0] = colors[2];
			voxels[0][width - 1][0] = colors[3];
			voxels[width - 1][width - 1][0] = colors[4];
			for (int i = 0; i <= halfWidth; i++)
			{
				voxels[i][i][i] = colors[1];
				voxels[width - 1 - i][i][i] = colors[2];
				voxels[i][width - 1 - i][i] = colors[3];
				voxels[width - 1 - i][width - 1 - i][i] = colors[4];
				voxels[halfWidth][halfWidth][i] = colors[5];
			}
			return voxels;
		}
		public static byte[][][] Pyramid2(int width, params byte[] colors) => Pyramid2(width, width, colors);
		public static byte[][][] Pyramid2(int width, int depth, params byte[] colors)
		{
			if (colors is null || colors.Length < 1)
				colors = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
			int halfWidth = width >> 1;
			byte[][][] voxels = Bytes3D.Initialize<byte>(width, depth, halfWidth + 1);
			voxels[width - 1][0][0] = colors[2];
			voxels[0][depth - 1][0] = colors[3];
			voxels[width - 1][depth - 1][0] = colors[4];
			for (int i = 0; i <= halfWidth; i++)
				voxels[0][0][i] = colors[1];
			return voxels;
		}
		#endregion Model creation
		private static void Iso8Gif(IModel model, IVoxelColor voxelColor, string path, int originX = -1, int originY = -1, int originZ = -1, int frameDelay = 150)
		{
			byte[][] sprites = IsoPacker.Iso8Outlined(
				model: new MarkerModel
				{
					Model = model,
					Voxel = 1,
					X = originX,
					Y = originY,
					Z = originZ,
				},
				voxelColor: voxelColor,
				widths: out int[] widths,
				pixelOrigins: out int[][] pixelOrigins,
				originX,
				originY,
				originZ);
			pixelOrigins = pixelOrigins.Iso8SouthWestPixelOrigins();
			int pixelOriginX = pixelOrigins.Select(origin => origin[0]).Max(),
				pixelOriginY = pixelOrigins.Select(origin => origin[1]).Max() + 2,
				width = Enumerable.Range(0, sprites.Length)
					.Select(i => widths[i]
						+ pixelOriginX - pixelOrigins[i][0]
						).Max(),
				height = Enumerable.Range(0, sprites.Length)
					.Select(i => PixelDraw.Height(sprites[i].Length, widths[i])
						+ pixelOriginY - pixelOrigins[i][1]
						).Max() + 1;
			ImageMaker.AnimatedGif(
				scaleX: 4,
				scaleY: 4,
				width: width,
				frames: Enumerable.Range(0, sprites.Length)
					.Select(frame => new byte[width * 4 * height]
						.DrawInsert(
							x: pixelOriginX - pixelOrigins[frame][0],
							y: pixelOriginY - pixelOrigins[frame][1],
							insert: sprites[frame],
							insertWidth: widths[frame],
							width: width))
					.ToArray()
					.AddFrameNumbers(width),
				frameDelay: frameDelay)
			.SaveAsGif(path);
		}
	}
}
