using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace Voxel2Pixel.Model.BenVoxel
{
	[DataContract]
	[Serializable]
	[XmlRoot("Metadata")]
	public class BenVoxelMetadata
	{
		#region BenVoxelMetadata
		[DataMember]
		protected readonly Dictionary<string, string> Properties = [];
		[DataMember]
		protected readonly Dictionary<string, Point3D> Points = [];
		[DataMember]
		protected readonly Dictionary<string, uint[]> Palettes = [];
		public BenVoxelMetadata() { }
		public BenVoxelMetadata(BenVoxelMetadata other)
		{
			foreach (KeyValuePair<string, string> property in other.GetProperties())
				SetProperty(property.Key, property.Value);
			foreach (KeyValuePair<string, Point3D> point in other.GetPoints())
				SetPoint(point.Key, point.Value);
			foreach (KeyValuePair<string, uint[]> palette in other.GetPalettes())
				SetPalette(palette.Key, palette.Value);
		}
		public IEnumerable<KeyValuePair<string, string>> GetProperties() => Properties;
		public IEnumerable<KeyValuePair<string, Point3D>> GetPoints() => Points;
		public IEnumerable<KeyValuePair<string, uint[]>> GetPalettes() => Palettes;
		public string Property(string key) => Properties[key];
		public Point3D Point(string key) => Points[key];
		public uint[] Palette(string key) => Palettes[key];
		public bool ContainsProperty(string key) => Properties.ContainsKey(key);
		public bool ContainsPoint(string key) => Points.ContainsKey(key);
		public bool ContainsPalette(string key) => Palettes.ContainsKey(key);
		public BenVoxelMetadata ClearProperties()
		{
			Properties.Clear();
			return this;
		}
		public BenVoxelMetadata ClearPoints()
		{
			Points.Clear();
			return this;
		}
		public BenVoxelMetadata ClearPalettes()
		{
			Palettes.Clear();
			return this;
		}
		public BenVoxelMetadata Clear() => ClearProperties().ClearPoints().ClearPalettes();
		public BenVoxelMetadata SetProperty(string key, string value)
		{
			if (key.Length > byte.MaxValue)
				throw new ArgumentException("Key too long! Max 255 characters.");
			Properties[key] = value;
			return this;
		}
		public BenVoxelMetadata SetPoint(string key, Point3D value)
		{
			if (key.Length > byte.MaxValue)
				throw new ArgumentException("Key too long! Max 255 characters.");
			Points[key] = value;
			return this;
		}
		public BenVoxelMetadata SetPalette(string key, uint[] value)
		{
			if (key.Length > byte.MaxValue)
				throw new ArgumentException("Key too long! Max 255 characters.");
			if (value is null || value.Length < 1)
				throw new ArgumentNullException(nameof(value));
			if (value.Length > 256)
				throw new ArgumentOutOfRangeException(nameof(value));
			Palettes[key] = value;
			return this;
		}
		#endregion BenVoxelMetadata
	}
}
