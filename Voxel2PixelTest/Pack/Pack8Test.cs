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
		public void HazmatTest()
		{
			VoxModel hazmat = new VoxModel(@"..\..\..\Hazmat.vox"),
				hazmat2 = new VoxModel(@"..\..\..\Hazmat2.vox");
			IVoxelColor hazmatColor = new NaiveDimmer(hazmat.Palette),
				hazmat2Color = new NaiveDimmer(hazmat2.Palette);
			int[] hazmatVoxelOrigin = new int[] { 7, 4, 0 },
				hazmat2VoxelOrigin = new int[] { 7, 11, 0 };
			Iso8Gif(
				models: new IModel[]
					{
						hazmat,
						hazmat2,
						hazmat,
						new FlipModel
						{
							Model = hazmat2,
							FlipX = true,
						}
					},
				voxelColors: new IVoxelColor[] { hazmatColor, hazmat2Color, hazmatColor, hazmat2Color },
				path: "Hazmat.gif",
				voxelOrigins: new int[][] { hazmatVoxelOrigin, hazmat2VoxelOrigin, hazmatVoxelOrigin, hazmat2VoxelOrigin },
				frameDelay: 100);
		}
		[Fact]
		public void PyramidTest() => Iso8Gif(
			model: new ArrayModel(Pyramid(17)),
			voxelColor: new NaiveDimmer(ArrayModelTest.RainbowPalette),
			path: "PyramidTest.gif");
		[Fact]
		public void Pyramid2Test() => Iso8Gif(
			model: new ArrayModel(Pyramid2(16, 4)),
			voxelColor: new NaiveDimmer(ArrayModelTest.RainbowPalette),
			path: "Pyramid2Test.gif",
			voxelOrigin: new int[] { 5, 0, 0 });
		[Fact]
		public void NumberCubeTest()
		{
			VoxModel model = new VoxModel(@"..\..\..\NumberCube.vox");
			Iso8Gif(
				model: model,
				voxelColor: new NaiveDimmer(model.Palette),
				path: "NumberCubeTest.gif",
				voxelOrigin: new int[] { 0, 0, 0 });
		}
		#endregion Tests
		#region Model creation
		public static byte[][][] Pyramid(int width, params byte[] colors)
		{
			if (colors is null || colors.Length < 1)
				colors = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
			int halfWidth = width >> 1;
			byte[][][] voxels = Array3D.Initialize<byte>(width, width, halfWidth + 1);
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
			byte[][][] voxels = Array3D.Initialize<byte>(width, depth, halfWidth + 1);
			voxels[width - 1][0][0] = colors[2];
			voxels[0][depth - 1][0] = colors[3];
			voxels[width - 1][depth - 1][0] = colors[4];
			for (int i = 0; i <= halfWidth; i++)
				voxels[0][0][i] = colors[1];
			return voxels;
		}
		#endregion Model creation
		#region GIF
		private static void Iso8Gif(IModel model, IVoxelColor voxelColor, string path, int[] voxelOrigin = null, int frameDelay = 150) => Iso8Gif(
			models: new IModel[] { model },
			voxelColors: new IVoxelColor[] { voxelColor },
			path: path,
			voxelOrigins: voxelOrigin is null ? null : new int[][] { voxelOrigin },
			frameDelay: frameDelay);
		private static void Iso8Gif(IModel[] models, IVoxelColor[] voxelColors, string path, int[][] voxelOrigins = null, int frameDelay = 150)
		{
			byte[][] sprites = IsoPacker.Iso8Outlined(
				models: models,
				voxelColors: voxelColors,
				widths: out int[] widths,
				pixelOrigins: out int[][] pixelOrigins,
				voxelOrigins: voxelOrigins);
			//pixelOrigins = pixelOrigins.Iso8SouthWestPixelOrigins();
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
							width: width)
						.DrawPixel(
							color: 0xFF00FFFF,
							x: pixelOriginX,
							y: pixelOriginY,
							width: width)
						)
					.ToArray()
					.AddFrameNumbers(width),
				frameDelay: frameDelay)
			.SaveAsGif(path);
		}
		#endregion GIF
	}
}
