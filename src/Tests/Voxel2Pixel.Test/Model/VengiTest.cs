using BenVoxel.Interfaces;
using BenVoxel.Models;
using SixLabors.ImageSharp;
using Voxel2Pixel.Color;
using Voxel2Pixel.ImageSharp;
using Voxel2Pixel.Model;
using Voxel2Pixel.Model.FileFormats;
using Voxel2Pixel.Render;

namespace Voxel2Pixel.Test.Model;

public class VengiTest
{
	[Fact]
	public void KingKong()
	{
		VengiFile.Node root = VengiFile.Read(new FileStream(@"..\..\..\TestData\Models\kingkong255colors.vengi", FileMode.Open));
		static VengiFile.Node? Model(VengiFile.Node parent)
		{
			if (parent.Header.Type.Equals("Model"))
				return parent;
			foreach (VengiFile.Node child in parent.Children)
				if (Model(child) is VengiFile.Node model)
					return model;
			return null;
		}
		VengiFile.Node node = Model(root) ?? throw new InvalidDataException();
		IModel model = new DictionaryModel(node.Data?.GetVoxels(), node.Data?.Size ?? throw new InvalidDataException());
		new SpriteMaker()
		{
			VoxelColor = new NaiveDimmer(node.Palette?.Palette),
			Model = model,
			CuboidOrientation = CuboidOrientation.NORTH0,
		}.Stacks()
			.Parallelize(spriteMaker => spriteMaker
				.Make()
				.Upscale(8, 8))
			.AnimatedGif(frameDelay: 10)
			.SaveAsGif("Vengi.gif");
	}
}
