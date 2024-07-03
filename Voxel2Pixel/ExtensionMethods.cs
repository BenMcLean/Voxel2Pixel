using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;
using Voxel2Pixel.Render;

namespace Voxel2Pixel
{
	public static class ExtensionMethods
	{
		/// <summary>
		/// Checking for being out of bounds can involve fewer comparisons than checking for being in bounds.
		/// </summary>
		/// <param name="coordinates">3D coordinates</param>
		/// <returns>true if coordinates are outside the bounds of the model</returns>
		public static bool IsOutside(this IModel model, params ushort[] coordinates) => coordinates[0] >= model.SizeX || coordinates[1] >= model.SizeY || coordinates[2] >= model.SizeZ;
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
		public static bool IsPeak(this Perspective perspective) => perspective == Perspective.FrontPeak || perspective == Perspective.DiagonalPeak;
		public static bool HasShadow(this Perspective perspective) => perspective == Perspective.Above || perspective == Perspective.Iso || perspective == Perspective.Stacked;
		public static byte Set(this IEditableModel model, Voxel voxel) => model[voxel.X, voxel.Y, voxel.Z] = voxel.Index;
		public static Point3D Center(this IModel model) => new(model.SizeX >> 1, model.SizeY >> 1, model.SizeZ >> 1);
		public static Point3D BottomCenter(this IModel model) => new(model.SizeX >> 1, model.SizeY >> 1, 0);
		#region PLINQ
		/// <summary>
		/// Parallelizes the execution of a Select query while preserving the order of the source sequence.
		/// </summary>
		public static List<TResult> Parallelize<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector) => source
			.Select((element, index) => (element, index))
			.AsParallel()
			.Select(sourceTuple => (result: selector(sourceTuple.element), sourceTuple.index))
			.OrderBy(resultTuple => resultTuple.index)
			.AsEnumerable()
			.Select(resultTuple => resultTuple.result)
			.ToList();
		#endregion PLINQ
		#region XML
		public static string Utf8Xml<T>(T o, bool indent = true)
		{
			Utf8StringWriter stringWriter = new();
			new XmlSerializer(typeof(T)).Serialize(
				xmlWriter: XmlWriter.Create(
					output: stringWriter,
					settings: new()
					{
						Encoding = Encoding.UTF8,
						Indent = indent,
						IndentChars = "\t",
					}),
				o: o,
				namespaces: new([XmlQualifiedName.Empty]));
			return stringWriter.ToString();
		}
		public class Utf8StringWriter : StringWriter
		{
			public override Encoding Encoding => Encoding.UTF8;
		}
		#endregion XML
		#region Sprite
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
		public readonly record struct HorizontalLine(int X, int Y, int Width);
		public static IEnumerable<HorizontalLine> TriangleRows(params Point[] points)
		{
			if (points is null)
				throw new ArgumentNullException("points");
			if (points.Length < 3)
				throw new ArgumentException("points must contain at least 3 elements.");
			points = [.. points
				.Take(3)
				.OrderBy(point => point.Y)
				.ThenBy(point => point.X)];
			Point[][] edges = [
				[.. new Point[] { points[0], points[1] }.OrderBy(point => point.X)],
				[.. new Point[] { points[0], points[2] }.OrderBy(point => point.X)],
				[.. new Point[] { points[1], points[2] }.OrderBy(point => point.X)],
			];
			static double calculateSlope(Point a, Point b) => (double)(b.Y - a.Y) / (b.X - a.X);
			bool[] hasInfiniteSlope = edges
				.Select(edge => edge[0].X == edge[1].X || edge[0].Y == edge[0].Y)
				.ToArray();
			double[] slopes = Enumerable.Range(0, 3)
				.Select(edge => hasInfiniteSlope[edge] ? 0 : calculateSlope(edges[edge][0], edges[edge][1]))
				.ToArray();
			double[] yIntercepts = [
				-slopes[0] * points[0].X,
				-slopes[1] * points[0].X,
				-slopes[2] * points[1].X,
			];
			int solveForX(int y, byte edge) => hasInfiniteSlope[edge] ?
				points[edge == 2 ? 1 : 0].X
				: Convert.ToInt32((y - yIntercepts[edge]) / slopes[edge]);
			for (int y = points[0].Y; y <= points[2].Y; y++)
			{
				bool isUp = y < points[1].Y;
				int a = solveForX(y: y, edge: (byte)(0 + (isUp ? 1 : 0))),
					b = solveForX(y: y, edge: (byte)(1 + (isUp ? 1 : 0))),
					x = Math.Min(a, b);
				yield return new HorizontalLine(
					X: x,
					Y: y,
					Width: Math.Max(a, b) - x);
			}
		}
		public static void DrawTriangle(this IRectangleRenderer renderer, uint color, params Point[] points)
		{
			foreach (HorizontalLine line in TriangleRows(points)
				.Where(point => point.Y >= 0
					&& point.X + point.Width >= 0
					&& point.X + point.Width < ushort.MaxValue))
				renderer.Rect(
					x: (ushort)Math.Max(0, line.X),
					y: (ushort)line.Y,
					color: color,
					sizeX: (ushort)(line.Width + (line.X < 0 ? line.X : 0)));
		}
		#endregion Geometry
	}
}
