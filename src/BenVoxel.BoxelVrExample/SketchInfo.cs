using System;
using System.Numerics;

namespace BenVoxel.BoxelVrExample;

/// <summary>
/// An object that is serialized and saved as a sketchInfo.json in a sketch folder
/// </summary>
[Serializable]
public class SketchInfo
{
	public string sketchName;
	public string dateCreated;
	public string sketchID;
	public Vector3 parentPosition;      // Position of the container where boxels live.
	public Quaternion parentRotation;   // Rotation of the container where boxels live.
	public string appVersion;
	public Color sketchBackgroundColor; // For UI previews and the editor scene.
}
