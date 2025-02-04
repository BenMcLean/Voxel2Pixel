using System;

namespace BenVoxel.BoxelVrExample;

/// <summary>
/// An object that is serialized and saved as a sketchData.json in a sketch folder.
/// </summary>
[Serializable]
public class BoxelsData
{
	public BoxelData[] data = [new BoxelData()];
}
