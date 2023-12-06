using SixLabors.ImageSharp;
using Voxel2Pixel.Color;
using Voxel2Pixel.Model;
using Voxel2Pixel.Pack;
using static Voxel2Pixel.ExtensionMethods;
using Xunit;
using System.Linq;
using Voxel2Pixel.Draw;
using System.IO;

namespace Voxel2PixelTest.Pack
{
	public class SpriteTest
	{
		[Fact]
		public void SoraGif()
		{
			VoxFileModel model = new VoxFileModel(@"..\..\..\Sora.vox");
			Sprite.Iso8(
					model: model,
					voxelColor: new NaiveDimmer(model.Palette))
				.Select(sprite => sprite.CropOutline())
				.SameSize()
				.AddFrameNumbers()
				.Select(sprite => sprite.Upscale(8, 8))
				.AnimatedGif(frameDelay: 100)
				.SaveAsGif("Sora.gif");
		}
		[Fact]
		public void BenSprite()
		{
			VoxFileModel model = new VoxFileModel(@"..\..\..\Sora.vox");
			IndexedSprite2x sprite = new IndexedSprite2x(
				width: (ushort)(VoxelDraw.IsoWidth(model) << 1),
				height: VoxelDraw.IsoHeight(model))
			{
				Palette = new NaiveDimmer(model.Palette).CreatePalette(),
			};
			VoxelDraw.Iso(model, sprite);
			string file = "Sora.BenSprite";
			if (File.Exists(file))
				File.Delete(file);
			using (FileStream fs = new FileStream(
				path: file,
				mode: FileMode.CreateNew))
				sprite.Write(fs);
			using (FileStream fs = new FileStream(
				path: file,
				mode: FileMode.Open))
				new IndexedSprite(fs)
					.Png()
					.SaveAsPng("BenSprite.png");
		}
	}
}
