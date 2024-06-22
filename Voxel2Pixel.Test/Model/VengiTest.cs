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
			static VengiFile.Data? Data(VengiFile.Node parent)
			{
				if (parent.Data is VengiFile.Data data)
					return data;
				foreach (VengiFile.Node child in parent.Children)
					if (Data(child) is VengiFile.Data data2)
						return data2;
				return null;
			}
			VengiFile.Data data = Data(root) ?? throw new InvalidDataException();
			new SpriteMaker()
			{
				Model = new DictionaryModel(data.GetVoxels(), data.Size),
				VoxelColor = new OneVoxelColor(0xFF0000FFu),
			}.Make()
				.Png("Vengi.png");
		}
	}
}
