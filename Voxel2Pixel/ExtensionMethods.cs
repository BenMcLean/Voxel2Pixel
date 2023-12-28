using System;
using System.Collections.Generic;
using System.Linq;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;
using Voxel2Pixel.Pack;

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
		public static byte Set(this IEditableModel model, Voxel voxel) => model[voxel.X, voxel.Y, voxel.Z] = voxel.Index;
		public static ushort[] Center(this IModel model) => new ushort[3] { (ushort)(model.SizeX >> 1), (ushort)(model.SizeY >> 1), (ushort)(model.SizeZ >> 1) };
		public static ushort[] BottomCenter(this IModel model) => new ushort[3] { (ushort)(model.SizeX >> 1), (ushort)(model.SizeY >> 1), 0 };
		#region Sprite
		public static IEnumerable<Sprite> AddFrameNumbers(this IEnumerable<Sprite> frames, uint color = 0xFFFFFFFF)
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
				}.AddRange(new KeyValuePair<string, Point>(Sprite.Origin, new Point(X: originX, Y: originY)));
		}
		#endregion Sprite
	}
}
