using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Voxel2Pixel.Model.BenVoxel
{
	public class BenVoxelMetadata
	{
		[XmlElement("Property")]
		public readonly SanitizedKeyDictionary<string> Properties = [];
		[XmlElement("Point")]
		public readonly SanitizedKeyDictionary<Point3D> Points = [];
		[XmlElement("Palette")]
		public readonly SanitizedKeyDictionary<uint[]> Palettes = [];
	}
}
