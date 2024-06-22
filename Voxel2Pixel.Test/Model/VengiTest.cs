using Voxel2Pixel.Color;
using Voxel2Pixel.Model;
using Voxel2Pixel.Model.FileFormats;
using Voxel2Pixel.Render;

namespace Voxel2Pixel.Test.Model
{
	public class VengiTest
	{
		[Fact]
		public void KingKong()
		{
			VengiFile.Node root = VengiFile.Read(new FileStream(@"..\..\..\kingkong.vengi", FileMode.Open));
			static VengiFile.Node? Model(VengiFile.Node parent)
			{
				if (parent.Header.Type.Equals("Model"))
					return parent;
				foreach (VengiFile.Node child in parent.Children)
					if (Model(child) is VengiFile.Node model)
						return model;
				return null;
			}
			VengiFile.Node model = Model(root) ?? throw new InvalidDataException();
			new SpriteMaker()
			{
				Model = new DictionaryModel(model.Data?.GetVoxels(), model.Data?.Size ?? throw new InvalidDataException()),
				VoxelColor = new NaiveDimmer(model.Palette?.Palette),
			}.Make()
				.Png("Vengi.png");
		}
	}
}
