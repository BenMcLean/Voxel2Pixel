using System;
using System.Collections.Generic;
using System.Linq;

namespace BenVoxel.BoxelVrExample;

public static class BenVoxelConverter
{
	public static BenVoxelFile Convert(BoxelsData boxelsData, SketchInfo sketchInfo = null)
	{
		BenVoxelFile benVoxelFile = new()
		{
			Global = new(),
		};
		uint[] palette = Color.GetPalette(boxelsData.data.Select(x => x.normalColor));
		if (sketchInfo is not null)
		{
			benVoxelFile.Global.Properties["BoxelVR.sketchName"] = sketchInfo.sketchName;
			benVoxelFile.Global.Properties["BoxelVR.dateCreated"] = sketchInfo.dateCreated;
			benVoxelFile.Global.Properties["BoxelVR.sketchID"] = sketchInfo.sketchID;
			benVoxelFile.Global.Properties["BoxelVR.parentPosition"] = sketchInfo.parentPosition.ToString();//should probably be serialized
			benVoxelFile.Global.Properties["BoxelVR.parentRotation"] = sketchInfo.parentRotation.ToString();//should probably be serialized
			benVoxelFile.Global.Properties["BoxelVR.appVersion"] = sketchInfo.appVersion;
			palette[0] = sketchInfo.sketchBackgroundColor.Uint();
		}
		benVoxelFile.Global[""] = palette;
		int minX = boxelsData.data.Select(e => e.intPosition.X).Min(),
			minY = boxelsData.data.Select(e => e.intPosition.Y).Min(),
			minZ = boxelsData.data.Select(e => e.intPosition.Z).Min(),
			maxX = boxelsData.data.Select(e => e.intPosition.X).Max(),
			maxY = boxelsData.data.Select(e => e.intPosition.Y).Max(),
			maxZ = boxelsData.data.Select(e => e.intPosition.Z).Max();
		benVoxelFile.Models[""] = new BenVoxelFile.Model()
		{
			Geometry = new(
				voxels: Voxels(
					palette: palette,
					boxelData: boxelsData.data,
					offsetX: -minX,
					offsetY: -minY,
					offsetZ: -minZ),
				size: new Point3D(
					x: maxX - minX,
					y: maxY - minY,
					z: maxZ - minZ)),
			Metadata = new()
			{
				Points = [new KeyValuePair<string, Point3D>("", new Point3D(-minX, -minY, -minZ))],
			},
		};
		return benVoxelFile;
	}
	public static IEnumerable<Voxel> Voxels(uint[] palette, BoxelData[] boxelData, int offsetX = 0, int offsetY = 0, int offsetZ = 0)
	{
		foreach (BoxelData boxel in boxelData)
			if (Array.IndexOf(palette, boxel.normalColor.Uint()) is int color and > 0)
				yield return new(
					X: (ushort)(boxel.intPosition.X + offsetX),
					Y: (ushort)(boxel.intPosition.Y + offsetY),
					Z: (ushort)(boxel.intPosition.Z + offsetZ),
					Index: (byte)color);
	}
	public static BoxelsData Convert(BenVoxelFile benVoxelFile, out SketchInfo sketchInfo)
	{
		Color[] palette = [.. (benVoxelFile.GetPalette() ?? throw new ArgumentException(message: "Couldn't get default palette.", paramName: nameof(benVoxelFile))).Take(256).Select(color => new Color(color.Rgba))];
		sketchInfo = new()
		{
			sketchName = benVoxelFile.GetProperty(modelName: null, propertyName: "BoxelVR.sketchName"),
			dateCreated = benVoxelFile.GetProperty(modelName: null, propertyName: "BoxelVR.dateCreated"),
			sketchID = benVoxelFile.GetProperty(modelName: null, propertyName: "BoxelVR.sketchID"),
			//should deserialize parentPosition
			//should deserialize parentRotation
			appVersion = benVoxelFile.GetProperty(modelName: null, propertyName: "BoxelVR.appVersion"),
			sketchBackgroundColor = palette[0],
		};
		if (benVoxelFile.Models[""] is not BenVoxelFile.Model model)
			throw new ArgumentException(message: "Couldn't get default model.", paramName: nameof(benVoxelFile));
		int offsetX = 0, offsetY = 0, offsetZ = 0;
		if (benVoxelFile.GetPoint() is Point3D origin)
		{
			offsetX = -origin.X;
			offsetY = -origin.Y;
			offsetZ = -origin.Z;
		}
		return new()
		{
			data = [.. model.Geometry.Select(voxel => new BoxelData
			{
				intPosition = new(
					X: voxel.X + offsetX,
					Y: voxel.Y + offsetY,
					Z: voxel.Z + offsetZ),
				normalColor = palette[voxel.Index],
			})],
		};
	}
}
