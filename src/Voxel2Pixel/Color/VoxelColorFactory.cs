/*
using System;
using System.Text.Json.Nodes;
using BenVoxel;
using Voxel2Pixel.Interfaces;

namespace Voxel2Pixel.Color;

public record VoxelColorFactory
{
	public required Type Type;
	public JsonArray? Data;
	public static IVoxelColor VoxelColor(Type type, JsonArray data, BenVoxelFile file, string? model = null) => new VoxelColorFactory
	{
		Type = type,
		Data = data,
	}.VoxelColor(file, model);
	public IVoxelColor VoxelColor(BenVoxelFile file, string? model = null)
	{
		//construct IVoxelColor and return it here.
	}
}
*/
