using SixLabors.ImageSharp;
using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;
using Voxel2Pixel.Pack;
using Voxel2Pixel.Render;
using Xunit;
using static Voxel2Pixel.Web.ImageMaker;

namespace Voxel2Pixel.Test.Pack
{
	public class ShadowTest
	{
		[Fact]
		public void Shadow()
		{
			VoxFileModel model = new(@"..\..\..\Sora.vox");
			IVoxelColor voxelColor = new NaiveDimmer(model.Palette),
				voxelShadow = new OneVoxelColor(0x00000088u);
			Sprite2x sprite = new((ushort)(VoxelDraw.IsoWidth(model) << 1), VoxelDraw.IsoHeight(model))
			{
				VoxelColor = voxelColor,
			};
			OffsetRenderer offsetRenderer = new()
			{
				RectangleRenderer = sprite,
				VoxelColor = voxelShadow,
				OffsetY = model.SizeZ << 2,
				ScaleX = 2,
			};
			VoxelDraw.IsoShadow(model, offsetRenderer);
			VoxelDraw.Iso(model, sprite);
			sprite.TransparentCrop().Png().SaveAsPng("ShadowTest.png");
		}
		[Fact]
		public void Outlined()
		{
			VoxFileModel model = new(@"..\..\..\Sora.vox");
			IVoxelColor voxelColor = new NaiveDimmer(model.Palette);
			Sprite.IsoOutlinedWithShadow(model, voxelColor)
				.TransparentCrop()
				.Png().SaveAsPng("ShadowOutlinedTest.png");
		}
	}
}
