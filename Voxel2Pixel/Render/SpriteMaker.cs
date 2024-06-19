using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;

namespace Voxel2Pixel.Render
{
	public class SpriteMaker
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
		public bool Crop { get; set; } = true;
		#endregion Data
		#region Constructors
		public SpriteMaker() { }
		public SpriteMaker(SpriteMaker maker)
		{
			Model = maker.Model;
			VoxelColor = maker.VoxelColor;
			Points = maker.Points;
			Perspective = maker.Perspective;
			PeakScaleX = maker.PeakScaleX;
			PeakScaleY = maker.PeakScaleY;
			FlipX = maker.FlipX;
			FlipY = maker.FlipY;
			FlipZ = maker.FlipZ;
			CuboidOrientation = maker.CuboidOrientation;
			ScaleX = maker.ScaleX;
			ScaleY = maker.ScaleY;
			Shadow = maker.Shadow;
			ShadowColor = maker.ShadowColor;
			Outline = maker.Outline;
			OutlineColor = maker.OutlineColor;
			Radians = maker.Radians;
			Threshold = maker.Threshold;
			Crop = maker.Crop;
		}
		#endregion Constructors
		#region Setters
		public SpriteMaker Set(IModel model) { Model = model; return this; }
		public SpriteMaker Set(IVoxelColor voxelColor) { VoxelColor = voxelColor; return this; }
		public SpriteMaker Set(Perspective perspective) { Perspective = perspective; return this; }
		public SpriteMaker SetPeakScaleX(byte peakScaleX) { PeakScaleX = peakScaleX; return this; }
		public SpriteMaker SetPeakScaleY(byte peakScaleY) { PeakScaleY = peakScaleY; return this; }
		public SpriteMaker SetFlipX(bool flipX) { FlipX = flipX; return this; }
		public SpriteMaker SetFlipY(bool flipY) { FlipY = flipY; return this; }
		public SpriteMaker SetFlipZ(bool flipZ) { FlipZ = flipZ; return this; }
		public SpriteMaker Set(CuboidOrientation cuboidOrientation) { CuboidOrientation = cuboidOrientation; return this; }
		public SpriteMaker SetScaleX(ushort scaleX) { ScaleX = scaleX; return this; }
		public SpriteMaker SetScaleY(ushort scaleY) { ScaleY = scaleY; return this; }
		public SpriteMaker SetShadow(bool shadow) { Shadow = shadow; return this; }
		public SpriteMaker ToggleShadow() => SetShadow(!Shadow);
		public SpriteMaker SetShadowColor(IVoxelColor shadowColor) { ShadowColor = shadowColor; return this; }
		public SpriteMaker SetShadowColor(uint rgba)
		{
			ShadowColor = new OneVoxelColor(rgba);
			return this;
		}
		public SpriteMaker SetOutline(bool outline) { Outline = outline; return this; }
		public SpriteMaker ToggleOutline() => SetOutline(!Outline);
		public SpriteMaker SetOutlineColor(uint rgba) { OutlineColor = rgba; return this; }
		public SpriteMaker SetRadians(double radians) { Radians = radians; return this; }
		public SpriteMaker SetThreshold(byte threshold) { Threshold = threshold; return this; }
		public SpriteMaker SetCrop(bool crop) { Crop = crop; return this; }
		public SpriteMaker ToggleCrop() => SetCrop(!Crop);
		public SpriteMaker SetPoints(Dictionary<string, Point3D> points) { Points = points; return this; }
		public SpriteMaker Set(Point3D origin)
		{
			Points ??= [];
			Points[Sprite.Origin] = origin;
			return this;
		}
		public SpriteMaker SetAll(params KeyValuePair<string, Point3D>[] points) => SetAll(points.AsEnumerable());
		public SpriteMaker SetAll(IEnumerable<KeyValuePair<string, Point3D>> points) => SetPoints([]).SetRange(points);
		public SpriteMaker SetRange(params KeyValuePair<string, Point3D>[] points) => SetRange(points.AsEnumerable());
		public SpriteMaker SetRange(IEnumerable<KeyValuePair<string, Point3D>> points)
		{
			Points ??= [];
			foreach (KeyValuePair<string, Point3D> point in points)
				Points[point.Key] = point.Value;
			return this;
		}
		#endregion Setters
		#region Modifiers
		protected bool NeedsReorientation => FlipX || FlipY || FlipZ || CuboidOrientation != CuboidOrientation.SOUTH0;
		protected SpriteMaker Flipped()
		{
			SpriteMaker maker = new(this)
			{
				FlipX = false,
				FlipY = false,
				FlipZ = false,
			};
			if (FlipX || FlipY || FlipZ)
			{
				maker.Model = new FlipModel
				{
					Model = maker.Model,
					FlipX = FlipX,
					FlipY = FlipY,
					FlipZ = FlipZ,
				};
				maker.SetAll(Points.Select(point => new KeyValuePair<string, Point3D>(point.Key, new Point3D(
					X: FlipX ? maker.Model.SizeX - point.Value.X - 1 : point.Value.X,
					Y: FlipY ? maker.Model.SizeY - point.Value.Y - 1 : point.Value.Y,
					Z: FlipZ ? maker.Model.SizeZ - point.Value.Z - 1 : point.Value.Z))));
			}
			return maker;
		}
		protected SpriteMaker Reoriented()
		{
			SpriteMaker maker = Flipped();
			if (maker.CuboidOrientation != CuboidOrientation.SOUTH0)
			{
				TurnModel turnModel = new()
				{
					Model = maker.Model,
					CuboidOrientation = maker.CuboidOrientation ?? CuboidOrientation.SOUTH0,
				};
				maker.Model = turnModel;
				Dictionary<string, Point3D> points = maker.Points;
				maker.SetAll(points.Select(point => new KeyValuePair<string, Point3D>(point.Key, turnModel.ReverseRotate(point.Value))));
			}
			maker.CuboidOrientation = CuboidOrientation.SOUTH0;
			return maker;
		}
		#endregion Modifiers
		#region Makers
		public Sprite Make() => Make(this);
		public static Sprite Make(SpriteMaker maker)
		{
			if (maker.NeedsReorientation)
				maker = maker.Reoriented();
			Point size = VoxelDraw.Size(
				perspective: maker.Perspective,
				model: maker.Model,
				peakScaleX: maker.PeakScaleX,
				peakScaleY: maker.PeakScaleY,
				radians: maker.Radians);
			Sprite sprite = new(
				width: (ushort)(size.X * maker.ScaleX + (maker.Outline ? 2 : 0)),
				height: (ushort)(size.Y * maker.ScaleX + (maker.Outline ? 2 : 0)))
			{
				VoxelColor = maker.VoxelColor,
			};
			VoxelDraw.Draw(
				perspective: maker.Perspective,
				model: maker.Model,
				renderer: new OffsetRenderer
				{
					RectangleRenderer = sprite,
					OffsetX = maker.Outline ? 1 : 0,
					OffsetY = maker.Outline ? 1 : 0,
					ScaleX = maker.ScaleX,
					ScaleY = maker.ScaleY,
				},
				peakScaleX: maker.PeakScaleX,
				peakScaleY: maker.PeakScaleY,
				radians: maker.Radians);
			if (maker.Outline)
				sprite = sprite.Outline(
					color: maker.OutlineColor,
					threshold: maker.Threshold);
			if (maker.Shadow && maker.Perspective.HasShadow())
			{
				Sprite insert = new(sprite);
				sprite.Texture = new byte[sprite.Texture.Length];
				//sprite.Rect(0, 0, 0xFF0000FFu, sprite.Width, sprite.Height);
				Perspective shadowPerspective = maker.Perspective == Perspective.Iso ? Perspective.IsoShadow : Perspective.Underneath;
				if (maker.Perspective == Perspective.Stacked)
				{
					Sprite shadow = new SpriteMaker(maker)
						.Set(shadowPerspective)
						.Set(maker.ShadowColor)
						.SetOutline(false)
						.SetCrop(false)
						.SetScaleX(1)
						.SetScaleY(1)
						.Make();
					Point shadowSize = shadow.RotatedSize(maker.Radians);
					shadow.Rotate(
						radians: maker.Radians,
						renderer: new OffsetRenderer
						{
							RectangleRenderer = sprite,
							OffsetX = maker.Outline ? 1 : 0,
							OffsetY = maker.Model.SizeZ - 1 + (maker.Outline ? 1 : 0),
							ScaleX = maker.ScaleX,
							ScaleY = maker.ScaleY,
						});
				}
				else
				{
					sprite.VoxelColor = maker.ShadowColor;
					Point shadowSize = VoxelDraw.Size(
						perspective: shadowPerspective,
						model: maker.Model);
					VoxelDraw.Draw(
						perspective: shadowPerspective,
						model: maker.Model,
						renderer: new OffsetRenderer
						{
							RectangleRenderer = sprite,
							OffsetX = (size.X - shadowSize.X) * maker.ScaleX + (maker.Outline ? 1 : 0),
							OffsetY = (size.Y - shadowSize.Y) * maker.ScaleY + (maker.Outline ? 1 : 0),
							ScaleX = maker.ScaleX,
							ScaleY = maker.ScaleY,
						});
					sprite.VoxelColor = maker.VoxelColor;
				}
				sprite.DrawTransparentInsert(
					x: 0,
					y: 0,
					insert: insert,
					threshold: maker.Threshold);
			}
			Dictionary<string, Point3D> points = maker.Points ?? new() { { Sprite.Origin, maker.Model.BottomCenter() }, };
			Point Point(Point3D point3D)
			{
				Point point = VoxelDraw.Locate(
					perspective: maker.Perspective,
					model: maker.Model,
					point: point3D,
					peakScaleX: maker.PeakScaleX,
					peakScaleY: maker.PeakScaleY,
					radians: maker.Radians);
				return new Point(
					X: point.X * maker.ScaleX + (maker.Outline ? 1 : 0),
					Y: point.Y * maker.ScaleY + (maker.Outline ? 1 : 0));
			}
			sprite.SetRange(points.Select(point => new KeyValuePair<string, Point>(point.Key, Point(point.Value))));
			return maker.Crop ?
				sprite.Crop2Content(maker.Threshold)
				: sprite;
		}
		public IEnumerable<SpriteMaker> Z4(Turn turn = Turn.CounterZ)
		{
			CuboidOrientation cuboidOrientation = CuboidOrientation;
			SpriteMaker maker = Flipped().Set(CuboidOrientation.SOUTH0);
			if (Points is null || !Points.ContainsKey(Sprite.Origin))
				maker.Set(maker.Model.BottomCenter());
			for (byte angle = 0; angle < 4; angle++)
			{
				yield return new SpriteMaker(maker)
					.Set(cuboidOrientation);
				cuboidOrientation = (CuboidOrientation)cuboidOrientation.Turn(turn);
			}
		}
		public IEnumerable<SpriteMaker> Iso8()
		{
			CuboidOrientation cuboidOrientation = CuboidOrientation;
			SpriteMaker maker = Flipped().Set(CuboidOrientation.SOUTH0);
			if (Points is null || !Points.ContainsKey(Sprite.Origin))
				maker.Set(maker.Model.BottomCenter());
			for (byte angle = 0; angle < 4; angle++)
			{
				yield return new SpriteMaker(maker)
					.Set(cuboidOrientation)
					.Set(Perspective.Above)
					.SetScaleX((ushort)(5 * ScaleX))
					.SetScaleY((ushort)(ScaleY << 2));
				cuboidOrientation = (CuboidOrientation)cuboidOrientation.Turn(Turn.CounterZ);
				yield return new SpriteMaker(maker)
					.Set(cuboidOrientation)
					.Set(Perspective.Iso)
					.SetScaleX((ushort)(ScaleX << 1))
					.SetScaleY(ScaleY);
			}
		}
		public IEnumerable<SpriteMaker> Iso8Shadows()
		{
			CuboidOrientation cuboidOrientation = CuboidOrientation;
			SpriteMaker maker = Flipped()
				.Set(CuboidOrientation.SOUTH0)
				.Set(ShadowColor)
				.SetOutline(false)
				.SetShadow(false);
			if (Points is null || !Points.ContainsKey(Sprite.Origin))
				maker.Set(maker.Model.BottomCenter());
			for (byte angle = 0; angle < 4; angle++)
			{
				yield return new SpriteMaker(maker)
					.Set(cuboidOrientation)
					.Set(Perspective.Underneath)
					.SetScaleX((ushort)(5 * ScaleX))
					.SetScaleY((ushort)(ScaleY << 2));
				cuboidOrientation = (CuboidOrientation)cuboidOrientation.Turn(Turn.CounterZ);
				yield return new SpriteMaker(maker)
					.Set(cuboidOrientation)
					.Set(Perspective.IsoShadow)
					.SetScaleX((ushort)(ScaleX << 1))
					.SetScaleY(ScaleY);
			}
		}
		public Sprite Iso8TextureAtlas(out TextureAtlas textureAtlas, string name = "Sprite") => new(dictionary: Iso8TextureAtlas(name).Make(), textureAtlas: out textureAtlas);
		public Dictionary<string, SpriteMaker> Iso8TextureAtlas(string name = "Sprite")
		{
			Dictionary<string, SpriteMaker> dictionary = [];
			byte direction = 0;
			foreach (SpriteMaker maker in new SpriteMaker(this)
				.SetShadow(false)
				.Iso8())
				dictionary.Add(name + direction++, maker);
			direction = 0;
			if (Shadow)
				foreach (SpriteMaker maker in new SpriteMaker(this)
					.SetOutline(false)
					.Iso8Shadows())
					dictionary.Add(name + "Shadow" + direction++, maker);
			return dictionary;
		}
		public const double Tau = 2d * Math.PI;
		public IEnumerable<SpriteMaker> Stacks(ushort quantity = 24)
		{
			SpriteMaker maker = Reoriented()
				.Set(Perspective.Stacked);
			for (ushort i = 0; i < quantity; i++)
				yield return new SpriteMaker(maker)
					.SetRadians(Radians + Tau * ((double)i / quantity));
		}
		public Sprite StackedShadow() => Reoriented()
			.Set(Perspective.Underneath)
			.Set(ShadowColor)
			.SetOutline(false)
			.SetCrop(false)
			.Make()
			.Rotate(Radians);
		public IEnumerable<Sprite> StacksShadows(ushort quantity = 24)
		{
			Task<Sprite>[] tasks = StacksShadowsTasks(quantity).ToArray();
			foreach (Task<Sprite> task in tasks)
				task.Start();
			Task.WaitAll(tasks);
			return tasks.Select(task => task.Result);
		}
		public IEnumerable<Task<Sprite>> StacksShadowsTasks(ushort quantity = 24)
		{
			Sprite shadow = Reoriented()
				.Set(Perspective.Underneath)
				.Set(ShadowColor)
				.SetCrop(true)
				.Make();
			return Enumerable.Range(0, quantity)
				.Select(i => new Task<Sprite>(() =>
				shadow.Rotate(Radians + Tau * ((double)i / quantity))));
		}
		public Sprite StacksTextureAtlas(out TextureAtlas textureAtlas, string name = "SpriteStack", ushort quantity = 24) => new(dictionary: StacksTextureAtlas(name, quantity), textureAtlas: out textureAtlas);
		public Dictionary<string, Sprite> StacksTextureAtlas(string name = "SpriteStack", ushort quantity = 24)
		{
			Dictionary<string, Sprite> dictionary = [];
			byte direction = 0;
			foreach (Sprite sprite in new SpriteMaker(this)
				.SetShadow(false)
				.Stacks(quantity)
				.Make())
				dictionary.Add(name + direction++, sprite);
			direction = 0;
			if (Shadow)
				foreach (Sprite sprite in new SpriteMaker(this)
					.SetOutline(false)
					.StacksShadows(quantity))
					dictionary.Add(name + "Shadow" + direction++, sprite);
			return dictionary;
		}
		#endregion Makers
	}
}
