using System.Collections.Generic;
using System.IO;
using System.Linq;
using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;
using Voxel2Pixel.Render;

namespace Voxel2Pixel.Pack
{
	public class SpriteFactory
	{
		public readonly static OneVoxelColor DefaultShadowVoxelColor = new(0x88u);
		#region Data
		public IModel Model { get; set; }
		public IVoxelColor VoxelColor { get; set; }
		public Dictionary<string, Point3D> Points { get; set; } = null;
		public Perspective Perspective { get; set; } = Perspective.Iso;
		public byte PeakScaleX
		{
			get => peakScaleX;
			set
			{
				if (value < 1)
					throw new InvalidDataException();
				else
					peakScaleX = value;
			}
		}
		private byte peakScaleX = 6;
		public byte PeakScaleY
		{
			get => peakScaleY;
			set
			{
				if (value < 2)
					throw new InvalidDataException();
				else
					peakScaleY = value;
			}
		}
		private byte peakScaleY = 6;
		public bool FlipX { get; set; } = false;
		public bool FlipY { get; set; } = false;
		public bool FlipZ { get; set; } = false;
		public CuboidOrientation CuboidOrientation { get; set; } = CuboidOrientation.SOUTH0;
		public ushort ScaleX
		{
			get => scaleX;
			set
			{
				if (value < 1)
					throw new InvalidDataException();
				else
					scaleX = value;
			}
		}
		private ushort scaleX = 1;
		public ushort ScaleY
		{
			get => scaleY;
			set
			{
				if (value < 1)
					throw new InvalidDataException();
				else
					scaleY = value;
			}
		}
		private ushort scaleY = 1;
		public bool Shadow { get; set; } = false;
		public IVoxelColor ShadowColor { get; set; } = DefaultShadowVoxelColor;
		public bool Outline { get; set; } = false;
		public uint OutlineColor { get; set; } = PixelDraw.DefaultOutlineColor;
		public double Radians { get; set; } = 0d;
		public byte Threshold { get; set; } = PixelDraw.DefaultTransparencyThreshold;
		#endregion Data
		#region Constructors
		public SpriteFactory() { }
		public SpriteFactory(SpriteFactory factory)
		{
			Model = factory.Model;
			VoxelColor = factory.VoxelColor;
			Points = factory.Points;
			Perspective = factory.Perspective;
			PeakScaleX = factory.PeakScaleX;
			PeakScaleY = factory.PeakScaleY;
			FlipX = factory.FlipX;
			FlipY = factory.FlipY;
			FlipZ = factory.FlipZ;
			CuboidOrientation = factory.CuboidOrientation;
			ScaleX = factory.ScaleX;
			ScaleY = factory.ScaleY;
			Shadow = factory.Shadow;
			ShadowColor = factory.ShadowColor;
			Outline = factory.Outline;
			OutlineColor = factory.OutlineColor;
			Radians = factory.Radians;
			Threshold = factory.Threshold;
		}
		#endregion Constructors
		#region Setters
		public SpriteFactory Set(IModel model) { Model = model; return this; }
		public SpriteFactory Set(IVoxelColor voxelColor) { VoxelColor = voxelColor; return this; }
		public SpriteFactory Set(Perspective perspective) { Perspective = perspective; return this; }
		public SpriteFactory SetPeakScaleX(byte peakScaleX) { PeakScaleX = peakScaleX; return this; }
		public SpriteFactory SetPeakScaleY(byte peakScaleY) { PeakScaleY = peakScaleY; return this; }
		public SpriteFactory SetFlipX(bool flipX) { FlipX = flipX; return this; }
		public SpriteFactory SetFlipY(bool flipY) { FlipY = flipY; return this; }
		public SpriteFactory SetFlipZ(bool flipZ) { FlipZ = flipZ; return this; }
		public SpriteFactory Set(CuboidOrientation cuboidOrientation) { CuboidOrientation = cuboidOrientation; return this; }
		public SpriteFactory SetScaleX(ushort scaleX) { ScaleX = scaleX; return this; }
		public SpriteFactory SetScaleY(ushort scaleY) { ScaleY = scaleY; return this; }
		public SpriteFactory SetShadow(bool shadow) { Shadow = shadow; return this; }
		public SpriteFactory SetShadowColor(IVoxelColor shadowColor) { ShadowColor = shadowColor; return this; }
		public SpriteFactory SetShadowColor(uint rgba)
		{
			ShadowColor = new OneVoxelColor(rgba);
			return this;
		}
		public SpriteFactory SetOutline(bool outline) { Outline = outline; return this; }
		public SpriteFactory SetOutlineColor(uint rgba) { OutlineColor = rgba; return this; }
		public SpriteFactory SetRadians(double radians) { Radians = radians; return this; }
		public SpriteFactory SetThreshold(byte threshold) { Threshold = threshold; return this; }
		public SpriteFactory Set(Point3D origin)
		{
			Points ??= [];
			Points[Sprite.Origin] = origin;
			return this;
		}
		public SpriteFactory AddRange(params KeyValuePair<string, Point3D>[] points) => AddRange(points.AsEnumerable());
		public SpriteFactory AddRange(IEnumerable<KeyValuePair<string, Point3D>> points)
		{
			Points ??= [];
			foreach (KeyValuePair<string, Point3D> point in points)
				Points[point.Key] = point.Value;
			return this;
		}
		#endregion Setters
		#region Builders
		public Sprite Build() => Build(this);
		public static Sprite Build(SpriteFactory factory)
		{
			if (factory.NeedsReorientation)
				factory = factory.Reoriented();
			Sprite sprite = new(
				width: (ushort)(VoxelDraw.Width(factory.Perspective, factory.Model, factory.PeakScaleX) * factory.ScaleX + (factory.Outline ? 2 : 0)),
				height: (ushort)(VoxelDraw.Height(factory.Perspective, factory.Model, factory.PeakScaleX) * factory.ScaleX + (factory.Outline ? 2 : 0)))
			{
				VoxelColor = factory.VoxelColor,
			};
			VoxelDraw.Draw(
				perspective: factory.Perspective,
				model: factory.Model,
				renderer: new OffsetRenderer
				{
					RectangleRenderer = sprite,
					OffsetX = factory.Outline ? 1 : 0,
					OffsetY = factory.Outline ? 1 : 0,
					ScaleX = factory.ScaleX,
					ScaleY = factory.ScaleY,
				},
				peakScaleX: factory.PeakScaleX,
				peakScaleY: factory.PeakScaleY);
			if (factory.Outline)
				sprite = sprite.Outline(
					color: factory.OutlineColor,
					threshold: factory.Threshold);
			if (factory.Shadow && factory.Perspective.HasShadow())
			{
				Sprite insert = new(sprite);
				sprite.Texture = new byte[sprite.Texture.Length];
				Perspective shadowPerspective = factory.Perspective == Perspective.Iso ? Perspective.IsoShadow : Perspective.Underneath;
				sprite.VoxelColor = factory.ShadowColor;
				VoxelDraw.Draw(
					perspective: shadowPerspective,
					model: factory.Model,
					renderer: new OffsetRenderer
					{
						RectangleRenderer = sprite,
						OffsetX = (VoxelDraw.Width(factory.Perspective, factory.Model) - VoxelDraw.Width(shadowPerspective, factory.Model)) * factory.ScaleX + (factory.Outline ? 1 : 0),
						OffsetY = (VoxelDraw.Height(factory.Perspective, factory.Model) - VoxelDraw.Height(shadowPerspective, factory.Model)) * factory.ScaleY + (factory.Outline ? 1 : 0),
						ScaleX = factory.ScaleX,
						ScaleY = factory.ScaleY,
					});
				sprite.VoxelColor = factory.VoxelColor;
				sprite.DrawTransparentInsert(
					x: 0,
					y: 0,
					insert: insert,
					threshold: factory.Threshold);
			}
			Dictionary<string, Point3D> points = factory.Points ?? new() { { Sprite.Origin, factory.Model.BottomCenter() }, };
			Point Point(Point3D point3D)
			{
				Point point = VoxelDraw.Locate(
					perspective: factory.Perspective,
					model: factory.Model,
					point: point3D,
					peakScaleX: factory.PeakScaleX,
					peakScaleY: factory.PeakScaleY);
				return new Point(
					X: point.X * factory.ScaleX + (factory.Outline ? 1 : 0),
					Y: point.Y * factory.ScaleY + (factory.Outline ? 1 : 0));
			}
			sprite.AddRange(points.Select(point => new KeyValuePair<string, Point>(point.Key, Point(point.Value))));
			return sprite.Crop2Content(factory.Threshold);
		}
		protected bool NeedsReorientation => FlipX || FlipY || FlipZ || CuboidOrientation != CuboidOrientation.SOUTH0;
		protected SpriteFactory Flipped()
		{
			SpriteFactory factory = new(this)
			{
				FlipX = false,
				FlipY = false,
				FlipZ = false,
			};
			if (FlipX || FlipY || FlipZ)
				factory.Model = new FlipModel
				{
					Model = factory.Model,
					FlipX = FlipX,
					FlipY = FlipY,
					FlipZ = FlipZ,
				};
			return factory;
		}
		protected SpriteFactory Reoriented()
		{
			SpriteFactory factory = Flipped();
			if (factory.CuboidOrientation != CuboidOrientation.SOUTH0)
				factory.Model = new TurnModel
				{
					Model = factory.Model,
					CuboidOrientation = factory.CuboidOrientation ?? CuboidOrientation.SOUTH0,
				};
			factory.CuboidOrientation = CuboidOrientation.SOUTH0;
			return factory;
		}
		public IEnumerable<Sprite> Z4(Turn turn = Turn.CounterZ)
		{
			SpriteFactory factory = Flipped();
			TurnModel turnModel = new()
			{
				Model = factory.Model,
				CuboidOrientation = factory.CuboidOrientation,
			};
			factory.Model = turnModel;
			Dictionary<string, Point3D> points = Points ?? new() { { Sprite.Origin, factory.Model.BottomCenter() }, };
			for (byte angle = 0; angle < 4; angle++)
			{
				yield return factory
					.AddRange(points.Select(point => new KeyValuePair<string, Point3D>(point.Key, turnModel.ReverseRotate(point.Value))))
					.Build();
				turnModel.Turn(turn);
			}
		}
		public IEnumerable<Sprite> Iso8()
		{
			SpriteFactory factory = Flipped();
			TurnModel turnModel = new()
			{
				Model = factory.Model,
				CuboidOrientation = factory.CuboidOrientation,
			};
			factory.Model = turnModel;
			Dictionary<string, Point3D> points = Points ?? new() { { Sprite.Origin, factory.Model.BottomCenter() }, };
			for (byte angle = 0; angle < 4; angle++)
			{
				yield return factory
					.Set(Perspective.Above)
					.SetScaleX((ushort)(5 * ScaleX))
					.SetScaleY((ushort)(ScaleY << 2))
					.AddRange(points.Select(point => new KeyValuePair<string, Point3D>(point.Key, turnModel.ReverseRotate(point.Value))))
					.Build();
				turnModel.Turn(Turn.CounterZ);
				yield return factory
					.Set(Perspective.Iso)
					.SetScaleX((ushort)(ScaleX << 1))
					.SetScaleY(ScaleY)
					.AddRange(points.Select(point => new KeyValuePair<string, Point3D>(point.Key, turnModel.ReverseRotate(point.Value))))
					.Build();
			}
		}
		public IEnumerable<Sprite> Iso8Shadows()
		{
			SpriteFactory factory = Flipped();
			factory.VoxelColor = ShadowColor;
			TurnModel turnModel = new()
			{
				Model = factory.Model,
				CuboidOrientation = factory.CuboidOrientation,
			};
			factory.Model = turnModel;
			Dictionary<string, Point3D> points = Points ?? new() { { Sprite.Origin, factory.Model.BottomCenter() }, };
			for (byte angle = 0; angle < 4; angle++)
			{
				yield return factory
					.Set(Perspective.Underneath)
					.SetScaleX((ushort)(5 * ScaleX))
					.SetScaleY((ushort)(ScaleY << 2))
					.AddRange(points.Select(point => new KeyValuePair<string, Point3D>(point.Key, turnModel.ReverseRotate(point.Value))))
					.Build();
				turnModel.Turn(Turn.CounterZ);
				yield return factory
					.Set(Perspective.IsoShadow)
					.SetScaleX((ushort)(ScaleX << 1))
					.SetScaleY(ScaleY)
					.AddRange(points.Select(point => new KeyValuePair<string, Point3D>(point.Key, turnModel.ReverseRotate(point.Value))))
					.Build();
			}
		}
		#endregion Builders
	}
}
