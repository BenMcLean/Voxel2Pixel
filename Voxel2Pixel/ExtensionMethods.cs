using System;
using System.Collections.Generic;
using System.Linq;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;
using Voxel2Pixel.Render;

namespace Voxel2Pixel;

public static class ExtensionMethods
{
	public static T Also<T>(this T obj, Action<T> action)
	{
		action(obj);
		return obj;
	}
	/// <param name="color">Using only colors 1-63</param>
	/// <returns>Big Endian RGBA8888 32-bit 256 color palette, leaving colors 0, 64, 128 and 192 as zeroes</returns>
	public static uint[] CreatePalette(this IVoxelColor color)
	{
		uint[] palette = new uint[256];
		foreach (VisibleFace face in Enum.GetValues(typeof(VisibleFace)))
			for (byte @byte = 1; @byte < 64; @byte++)
				palette[(byte)face + @byte] = color[@byte, face];
		return palette;
	}
	public static VisibleFace VisibleFace(this byte @byte) => (VisibleFace)(@byte & 192);
	public static bool IsInternallyScaled(this Perspective perspective, bool peak = false) =>
		perspective == Perspective.Stacked
		|| (peak && (perspective == Perspective.Front || perspective == Perspective.Diagonal));
	public static bool HasScaleZ(this Perspective perspective) => perspective == Perspective.Stacked;
	public static bool HasShadow(this Perspective perspective) => perspective == Perspective.Above || perspective == Perspective.Iso || perspective == Perspective.Stacked;
	#region PLINQ
	/// <summary>
	/// Parallelizes the execution of a Select query while preserving the order of the source sequence.
	/// </summary>
	public static List<TResult> Parallelize<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector) => [.. source
		.Select((element, index) => (element, index))
		.AsParallel()
		.Select(sourceTuple => (result: selector(sourceTuple.element), sourceTuple.index))
		.OrderBy(resultTuple => resultTuple.index)
		.AsEnumerable()
		.Select(resultTuple => resultTuple.result)];
	#endregion PLINQ
	#region Sprite
	public static Point Size(this ISprite sprite) => new(sprite.Width, sprite.Height);
	public static IEnumerable<Sprite> AddFrameNumbers(this IEnumerable<Sprite> frames, uint color = 0xFFFFFFFFu)
	{
		int frame = 0;
		foreach (Sprite sprite in frames)
		{
			sprite.Texture.Draw3x4(
				@string: (++frame).ToString(),
				width: sprite.Width,
				x: 0,
				y: sprite.Height - 4,
				color: color);
			yield return sprite;
		}
	}
	/// <summary>
	/// Warning: the new sprites only retain the Origin point, dropping all other points.
	/// </summary>
	public static IEnumerable<Sprite> SameSize(this IEnumerable<ISprite> sprites, ushort addWidth = 0, ushort addHeight = 0)
	{
		int originX = sprites.Select(sprite => sprite.TryGetValue(Sprite.Origin, out Point value) ? value.X : 0).Max(),
			originY = sprites.Select(sprite => sprite.TryGetValue(Sprite.Origin, out Point value) ? value.Y : 0).Max();
		ushort width = (ushort)(sprites.Select(sprite => sprite.Width + originX - (sprite.TryGetValue(Sprite.Origin, out Point value) ? value.X : 0)).Max() + addWidth),
			height = (ushort)(sprites.Select(sprite => sprite.Height + originY - (sprite.TryGetValue(Sprite.Origin, out Point value) ? value.Y : 0)).Max() + addHeight);
		foreach (ISprite sprite in sprites)
			yield return new Sprite
			{
				Texture = new byte[width * height << 2]
					.DrawInsert(
						x: originX - (sprite.TryGetValue(Sprite.Origin, out Point valueX) ? valueX.X : 0),
						y: originY - (sprite.TryGetValue(Sprite.Origin, out Point valueY) ? valueY.Y : 0),
						insert: sprite.Texture,
						insertWidth: sprite.Width,
						width: width),
				Width = width,
			}.SetRange(new KeyValuePair<string, Point>(Sprite.Origin, new Point(X: originX, Y: originY)));
	}
	#endregion Sprite
	#region SpriteMaker
	public static List<Sprite> Make(this IEnumerable<SpriteMaker> spriteMakers) => spriteMakers.Parallelize(spriteMaker => spriteMaker.Make());
	public static Dictionary<T, Sprite> Make<T>(this IDictionary<T, SpriteMaker> spriteMakers) => spriteMakers
		.AsParallel()
		.Select(spriteMakerPair => (
			key: spriteMakerPair.Key,
			sprite: spriteMakerPair.Value.Make()))
		.ToDictionary(//sequential
			keySelector: tuple => tuple.key,
			elementSelector: tuple => tuple.sprite);
	#endregion SpriteMaker
	#region Geometry
	public readonly record struct HorizontalLine(ushort X, ushort Width, ushort Y);
	public static IEnumerable<HorizontalLine> TriangleRows(Point a, Point b, Point c, Point? bounds = null)
	{
		ushort maxX = ushort.MaxValue;
		ushort maxY = ushort.MaxValue;
		if (bounds.HasValue)
		{
			if (bounds.Value.X < 1 || bounds.Value.Y < 1)
				throw new ArgumentException("Bounds must be at least 1x1", nameof(bounds));
			maxX = (ushort)(bounds.Value.X - 1);
			maxY = (ushort)(bounds.Value.Y - 1);
		}
		Point[] sortedPoints = [.. new[] { a, b, c }.OrderBy(point => point.Y)];
		double[] slopes = [
			CalculateSlope(sortedPoints[0], sortedPoints[2]),
			CalculateSlope(sortedPoints[0], sortedPoints[1]),
			CalculateSlope(sortedPoints[1], sortedPoints[2])];
		foreach (HorizontalLine line in GenerateLines(sortedPoints[0], sortedPoints[1], slopes[0], slopes[1], maxX, maxY))
			yield return line;
		foreach (HorizontalLine line in GenerateLines(sortedPoints[1], sortedPoints[2], slopes[0], slopes[2], maxX, maxY))
			yield return line;
	}
	public static double CalculateSlope(Point a, Point b) => a.X == b.X ?
		double.PositiveInfinity
		: (double)(b.Y - a.Y) / (b.X - a.X);
	private static IEnumerable<HorizontalLine> GenerateLines(Point start, Point end, double slope1, double slope2, ushort maxX, ushort maxY)
	{
		for (int y = Math.Max(0, start.Y); y <= Math.Min(end.Y, maxY); y++)
		{
			double x1 = start.X + (y - start.Y) / slope1;
			double x2 = start.X + (y - start.Y) / slope2;
			int startX = (int)Math.Ceiling(Math.Min(x1, x2));
			int endX = (int)Math.Floor(Math.Max(x1, x2));
			startX = Math.Max(0, Math.Min(startX, maxX));
			endX = Math.Max(0, Math.Min(endX, maxX));
			if (startX <= endX)
				yield return new HorizontalLine((ushort)startX, (ushort)(endX - startX + 1), (ushort)y);
		}
	}
	public static void DrawTriangle(this IRectangleRenderer renderer, uint color, Point a, Point b, Point c, Point? bounds = null)
	{
		foreach (HorizontalLine line in TriangleRows(a, b, c, bounds))
			renderer.Rect(
				x: line.X,
				y: line.Y,
				color: color,
				sizeX: (ushort)(line.Width + line.X));
	}
	#endregion Geometry
}
