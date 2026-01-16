using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BenProgress;
using BenVoxel;
using BenVoxel.Interfaces;
using BenVoxel.Models;
using BenVoxel.Structs;
using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;

namespace Voxel2Pixel.Render;

public class SpriteMaker
{
	public readonly static OneVoxelColor DefaultShadowVoxelColor = new(0x88u);
	#region Data
	public IModel Model { get; set; }
	public IVoxelColor VoxelColor { get; set; }
	public Dictionary<string, Point3D> Points { get; set; } = [];
	public Perspective Perspective { get; set; } = Perspective.Iso;
	public bool FlipX { get; set; } = false;
	public bool FlipY { get; set; } = false;
	public bool FlipZ { get; set; } = false;
	public bool Iso8 { get; set; } = false;
	public CuboidOrientation CuboidOrientation { get; set; } = CuboidOrientation.SOUTH0;
	public ushort NumberOfSprites
	{
		get => numberOfSprites;
		set
		{
			if (value < 1)
				throw new InvalidDataException();
			else
				numberOfSprites = value;
		}
	}
	private ushort numberOfSprites = 1;
	public byte ScaleX
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
	private byte scaleX = 1;
	public byte ScaleY
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
	private byte scaleY = 1;
	public byte ScaleZ
	{
		get => scaleZ;
		set
		{
			if (value < 1)
				throw new InvalidDataException();
			else
				scaleZ = value;
		}
	}
	private byte scaleZ = 1;
	public byte FinalScaleX
	{
		get => finalScaleX;
		set
		{
			if (value < 1)
				throw new InvalidDataException();
			else
				finalScaleX = value;
		}
	}
	private byte finalScaleX = 1;
	public byte FinalScaleY
	{
		get => finalScaleY;
		set
		{
			if (value < 1)
				throw new InvalidDataException();
			else
				finalScaleY = value;
		}
	}
	private byte finalScaleY = 1;
	public bool Shadow { get; set; } = false;
	public IVoxelColor ShadowColor { get; set; } = DefaultShadowVoxelColor;
	public bool Outline { get; set; } = false;
	public uint OutlineColor { get; set; } = PixelDraw.Black;
	public double Radians
	{
		get => radians;
		set => radians = value % PixelDraw.Tau;
	}
	private double radians = 0d;
	public bool Peak { get; set; } = true;
	public byte Threshold { get; set; } = PixelDraw.DefaultTransparencyThreshold;
	public bool Crop { get; set; } = true;
	#endregion Data
	#region Constructors
	public SpriteMaker() { }
	public SpriteMaker(SpriteMaker maker)
	{
		Model = maker.Model;
		VoxelColor = maker.VoxelColor;
		Points = new(maker.Points);
		Perspective = maker.Perspective;
		FlipX = maker.FlipX;
		FlipY = maker.FlipY;
		FlipZ = maker.FlipZ;
		Iso8 = maker.Iso8;
		CuboidOrientation = maker.CuboidOrientation;
		NumberOfSprites = maker.NumberOfSprites;
		ScaleX = maker.ScaleX;
		ScaleY = maker.ScaleY;
		ScaleZ = maker.ScaleZ;
		FinalScaleX = maker.FinalScaleX;
		FinalScaleY = maker.FinalScaleY;
		Shadow = maker.Shadow;
		ShadowColor = maker.ShadowColor;
		Outline = maker.Outline;
		OutlineColor = maker.OutlineColor;
		Radians = maker.Radians;
		Peak = maker.Peak;
		Threshold = maker.Threshold;
		Crop = maker.Crop;
	}
	#endregion Constructors
	#region Setters
	public SpriteMaker Set(IModel model) { Model = model; return this; }
	public SpriteMaker Set(IVoxelColor voxelColor) { VoxelColor = voxelColor; return this; }
	public SpriteMaker Set(Perspective perspective) { Perspective = perspective; return this; }
	public SpriteMaker SetFlipX(bool flipX) { FlipX = flipX; return this; }
	public SpriteMaker SetFlipY(bool flipY) { FlipY = flipY; return this; }
	public SpriteMaker SetFlipZ(bool flipZ) { FlipZ = flipZ; return this; }
	public SpriteMaker SetIso8(bool iso8) { Iso8 = iso8; return this; }
	public SpriteMaker Set(CuboidOrientation cuboidOrientation) { CuboidOrientation = cuboidOrientation; return this; }
	public SpriteMaker SetNumberOfSprites(ushort numberOfSprites) { NumberOfSprites = numberOfSprites; return this; }
	public SpriteMaker SetScaleX(byte scaleX) { ScaleX = scaleX; return this; }
	public SpriteMaker SetScaleY(byte scaleY) { ScaleY = scaleY; return this; }
	public SpriteMaker SetScaleZ(byte scaleZ) { ScaleZ = scaleZ; return this; }
	public SpriteMaker SetFinalScaleX(byte finalScaleX) { FinalScaleX = finalScaleX; return this; }
	public SpriteMaker SetFinalScaleY(byte finalScaleY) { FinalScaleY = finalScaleY; return this; }
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
	public SpriteMaker SetPeak(bool peak) { Peak = peak; return this; }
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
				x: FlipX ? maker.Model.SizeX - point.Value.X - 1 : point.Value.X,
				y: FlipY ? maker.Model.SizeY - point.Value.Y - 1 : point.Value.Y,
				z: FlipZ ? maker.Model.SizeZ - point.Value.Z - 1 : point.Value.Z))));
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
	public static Sprite Make(SpriteMaker maker) => MakeAsync(maker)
		.ConfigureAwait(false)
		.GetAwaiter()
		.GetResult();
	public Task<Sprite> MakeAsync(ProgressContext? progressContext = null) => MakeAsync(this, progressContext);
	public static async Task<Sprite> MakeAsync(SpriteMaker maker, ProgressContext? progressContext = null)
	{
		maker = maker.NeedsReorientation ? maker.Reoriented() : new(maker);
		if (!maker.Points.ContainsKey(Sprite.Origin))
			maker.Points[Sprite.Origin] = maker.Model.BottomCenter();
		switch (maker.Perspective)
		{//Replace compound perspectives with simple perspectives.
			case Perspective.IsoEight:
				maker.Perspective = Perspective.Iso;
				break;
			case Perspective.IsoEightUnderneath:
				maker.Perspective = Perspective.IsoUnderneath;
				break;
		}
		Point size = VoxelDraw.Size(
			perspective: maker.Perspective,
			model: maker.Model,
			scaleX: maker.ScaleX,
			scaleY: maker.ScaleY,
			scaleZ: maker.ScaleZ,
			radians: maker.Radians,
			peak: maker.Peak);
		Sprite sprite = new(
			width: (ushort)(size.X + (maker.Outline ? 2 : 0)),
			height: (ushort)(size.Y + (maker.Outline ? 2 : 0)))
		{
			VoxelColor = maker.VoxelColor,
		};
		await VoxelDraw.DrawAsync(
			perspective: maker.Perspective,
			model: maker.Model,
			renderer: sprite,
			scaleX: maker.ScaleX,
			scaleY: maker.ScaleY,
			scaleZ: maker.ScaleZ,
			radians: maker.Radians,
			peak: maker.Peak,
			offsetX: (ushort)(maker.Outline ? 1 : 0),
			offsetY: (ushort)(maker.Outline ? 1 : 0),
			progressContext: progressContext);
		if (maker.Outline)
			sprite = sprite.Outline(
				color: maker.OutlineColor,
				threshold: maker.Threshold);
		/*TODO fix shadows
		if (maker.Shadow && maker.Perspective.HasShadow())
		{
			Sprite insert = new(sprite);
			sprite.Texture = new byte[sprite.Texture.Length];
			//sprite.Rect(0, 0, 0xFF0000FFu, sprite.Width, sprite.Height);
			Perspective shadowPerspective = maker.Perspective == Perspective.Iso ? Perspective.IsoShadow : Perspective.Underneath;
			if (maker.Perspective == Perspective.Stacked || maker.Perspective == Perspective.StackedPeak)
			{
				Sprite shadow = new SpriteMaker(maker)
					.Set(shadowPerspective)
					.Set(maker.ShadowColor)
					.SetOutline(false)
					.SetCrop(false)
					.SetScaleX(1)
					.SetScaleY(1)
					.Make();
				Point shadowSize = shadow.RotatedSize(
					radians: maker.Radians,
					scaleX: maker.ScaleX,
					scaleY: maker.ScaleY);
				shadow.Rotate(
					radians: maker.Radians,
					renderer: new OffsetRenderer
					{
						RectangleRenderer = sprite,
						OffsetX = maker.Outline ? 1 : 0,
						OffsetY = maker.Model.SizeZ - 1 + (maker.Outline ? 1 : 0),
					},
					scaleX: maker.ScaleX,
					scaleY: maker.ScaleY);
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
		*/
		Dictionary<string, Point3D> points = maker.Points ?? new() { { Sprite.Origin, maker.Model.BottomCenter() }, };
		Point Point(Point3D point3D)
		{
			Point point = VoxelDraw.Locate(
				perspective: maker.Perspective,
				model: maker.Model,
				point: point3D,
				scaleX: maker.ScaleX,
				scaleY: maker.ScaleY,
				scaleZ: maker.ScaleZ,
				radians: maker.Radians);
			return new Point(
				X: point.X + (maker.Outline ? 1 : 0),
				Y: point.Y + (maker.Outline ? 1 : 0));
		}
		sprite.SetRange(points.Select(point => new KeyValuePair<string, Point>(point.Key, Point(point.Value))));
		if (maker.Crop)
			sprite = sprite.Crop2Content(maker.Threshold);
		return maker.FinalScaleX > 1 || maker.FinalScaleY > 1 ?
			sprite.Upscale(maker.FinalScaleX, maker.FinalScaleY)
			: sprite;
	}
	public IAsyncEnumerable<Sprite> MakeGroupAsync(ProgressContext? progressContext = null) => MakeGroupAsync(this, progressContext);
	public static async IAsyncEnumerable<Sprite> MakeGroupAsync(SpriteMaker maker, ProgressContext? progressContext = null)
	{
		if (maker.NumberOfSprites == 1)
		{
			yield return await maker.MakeAsync(progressContext);
			yield break;
		}
		using ProgressTaskGroup<Sprite> group = new(progressContext);
		group.Add((maker.Perspective switch
		{
			Perspective.IsoEight => maker.IsoEight(),
			Perspective.IsoEightUnderneath => maker.IsoEightUnderneath(),
			Perspective.Stacked => maker.Stacks(),
			Perspective.StackedUnderneath => throw new NotImplementedException(),//TODO
			_ => maker.Z4(),
		})
			.Take(maker.NumberOfSprites)
			.Select<SpriteMaker, Func<ProgressContext?, Task<Sprite>>>(m => m.MakeAsync));
		await foreach (Sprite sprite in group.GetResultsAsync())
			yield return sprite;
	}
	#endregion Makers
	#region Groups
	public IEnumerable<SpriteMaker> Z4(Turn turn = Turn.CounterZ) => Z4(this, turn);
	public static IEnumerable<SpriteMaker> Z4(SpriteMaker maker, Turn turn = Turn.CounterZ)
	{
		CuboidOrientation cuboidOrientation = maker.CuboidOrientation;
		maker = maker.Flipped().Set(CuboidOrientation.SOUTH0);
		if (maker.Points is null || !maker.Points.ContainsKey(Sprite.Origin))
			maker.Set(maker.Model.BottomCenter());
		while (true)
		{
			yield return new SpriteMaker(maker)
				.SetNumberOfSprites(1)
				.Set(cuboidOrientation);
			cuboidOrientation = (CuboidOrientation)cuboidOrientation.Turn(turn);
		}
	}
	public IEnumerable<SpriteMaker> IsoEight() => IsoEight(this);
	public static IEnumerable<SpriteMaker> IsoEight(SpriteMaker maker)
	{
		CuboidOrientation cuboidOrientation = maker.CuboidOrientation;
		maker = maker.Flipped().Set(CuboidOrientation.SOUTH0);
		if (maker.Points is null || !maker.Points.ContainsKey(Sprite.Origin))
			maker.Set(maker.Model.BottomCenter());
		while (true)
			for (byte x = 0; x < 4; x++)
			{
				for (byte y = 0; y < 4; y++)
				{
					for (byte z = 0; z < 4; z++)
					{
						yield return new SpriteMaker(maker)
							.SetNumberOfSprites(1)
							.Set(cuboidOrientation)
							.Set(Perspective.Above)
							.SetScaleX((byte)(5 * maker.ScaleX))
							.SetScaleY((byte)(maker.ScaleY << 2));
						cuboidOrientation = (CuboidOrientation)cuboidOrientation.Turn(Turn.CounterZ);
						yield return new SpriteMaker(maker)
							.SetNumberOfSprites(1)
							.Set(cuboidOrientation)
							.Set(Perspective.Iso)
							.SetScaleX((byte)(maker.ScaleX << 1));
					}
					cuboidOrientation = (CuboidOrientation)cuboidOrientation.Turn(Turn.CounterY);
				}
				cuboidOrientation = (CuboidOrientation)cuboidOrientation.Turn(Turn.CounterX);
			}
	}
	public IEnumerable<SpriteMaker> IsoEightUnderneath() => IsoEightUnderneath(this);
	public static IEnumerable<SpriteMaker> IsoEightUnderneath(SpriteMaker maker)
	{
		CuboidOrientation cuboidOrientation = maker.CuboidOrientation;
		maker = maker.Flipped()
			.Set(CuboidOrientation.SOUTH0)
			.SetOutline(false)
			.SetShadow(false);
		if (maker.Points is null || !maker.Points.ContainsKey(Sprite.Origin))
			maker.Set(maker.Model.BottomCenter());
		while (true)
			for (byte x = 0; x < 4; x++)
			{
				for (byte y = 0; y < 4; y++)
				{
					for (byte z = 0; z < 4; z++)
					{
						yield return new SpriteMaker(maker)
							.SetNumberOfSprites(1)
							.Set(cuboidOrientation)
							.Set(Perspective.Underneath)
							.SetScaleX((byte)(5 * maker.ScaleX))
							.SetScaleY((byte)(maker.ScaleY << 2));
						cuboidOrientation = (CuboidOrientation)cuboidOrientation.Turn(Turn.CounterZ);
						yield return new SpriteMaker(maker)
							.SetNumberOfSprites(1)
							.Set(cuboidOrientation)
							.Set(Perspective.IsoUnderneath)
							.SetScaleX((byte)(maker.ScaleX << 1));
					}
					cuboidOrientation = (CuboidOrientation)cuboidOrientation.Turn(Turn.CounterY);
				}
				cuboidOrientation = (CuboidOrientation)cuboidOrientation.Turn(Turn.CounterX);
			}
	}
	public IEnumerable<SpriteMaker> Stacks() => Stacks(this);
	public static IEnumerable<SpriteMaker> Stacks(SpriteMaker maker)
	{
		maker = maker.Reoriented();
		for (ushort i = 0; i < maker.NumberOfSprites; i++)
			yield return new SpriteMaker(maker)
				.SetRadians(maker.Radians + PixelDraw.Tau * ((double)i / maker.NumberOfSprites));
	}
	public Sprite StackedShadow() => Reoriented()
		.Set(Perspective.Underneath)
		.Set(ShadowColor)
		.SetOutline(false)
		.SetCrop(false)
		.Make()
		.Rotate(Radians);
	public Sprite StacksTextureAtlas(out TextureAtlas textureAtlas, string name = "SpriteStack") => new(dictionary: StacksTextureAtlas(name), textureAtlas: out textureAtlas);
	public Dictionary<string, Sprite> StacksTextureAtlas(string name = "SpriteStack")
	{
		if (!name.Contains("{0}")) name += "{0}";
		Dictionary<string, Sprite> dictionary = [];
		byte direction = 0;
		foreach (Sprite sprite in new SpriteMaker(this)
			.SetShadow(false)
			.Stacks()
			.Make())
			dictionary.Add(string.Format(name, direction++), sprite);
		/*TODO fix shadows
		direction = 0;
		if (Shadow)
			foreach (Sprite sprite in new SpriteMaker(this)
				.SetOutline(false)
				.StacksShadows(quantity))
				dictionary.Add(string.Format(name, "Shadow" + direction++), sprite);
		*/
		return dictionary;
	}
	#endregion Groups
}
