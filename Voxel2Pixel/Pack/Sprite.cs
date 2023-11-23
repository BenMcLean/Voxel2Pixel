﻿using System.Collections.Generic;
using System.Linq;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Interfaces;

namespace Voxel2Pixel.Pack
{
	public class Sprite : ISprite
	{
		#region ISprite
		public byte[] Texture { get; set; }
		public ushort Width { get; set; }
		public ushort Height => (ushort)((Texture.Length >> 2) / Width);
		public ushort OriginX { get; set; }
		public ushort OriginY { get; set; }
		#endregion ISprite
		public static IEnumerable<Sprite> SameSize(ushort addWidth, ushort addHeight, IEnumerable<ISprite> sprites) => SameSize(addWidth, addHeight, sprites.ToArray());
		public static IEnumerable<Sprite> SameSize(IEnumerable<ISprite> sprites) => SameSize(sprites.ToArray());
		public static IEnumerable<Sprite> SameSize(params ISprite[] sprites) => SameSize(0, 0, sprites);
		public static IEnumerable<Sprite> SameSize(ushort addWidth, ushort addHeight, params ISprite[] sprites)
		{
			ushort originX = sprites.Select(sprite => sprite.OriginX).Max(),
				originY = sprites.Select(sprite => sprite.OriginY).Max(),
				width = (ushort)(sprites.Select(sprite => sprite.Width + originX - sprite.OriginX).Max() + addWidth),
				height = (ushort)(sprites.Select(sprite => sprite.Height + originY - sprite.OriginY).Max() + addHeight);
			int textureLength = (width * height) << 2;
			foreach (ISprite sprite in sprites)
				yield return new Sprite
				{
					Texture = new byte[textureLength]
						.DrawInsert(
							x: originX - sprite.OriginX,
							y: originY - sprite.OriginY,
							insert: sprite.Texture,
							insertWidth: sprite.Width,
							width: width),
					Width = width,
					OriginX = originX,
					OriginY = originY,
				};
		}
	}
}
