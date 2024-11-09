# Voxel2Pixel
Voxel2Pixel is an [MIT-licensed](LICENSE) [C# 8.0](https://dotnet.microsoft.com/en-us/languages/csharp) [.NET Standard 2.0](https://learn.microsoft.com/en-us/dotnet/standard/net-standard) library created by Ben McLean for rendering [voxel models](https://www.megavoxels.com/learn/what-is-a-voxel/) as [pixel art assets](https://2dwillneverdie.com/intro/) from a variety of fixed [camera perspectives](https://opengameart.org/content/chapter-3-perspectives) and [palette limitations](https://lospec.com/palette-list) intended for use in retro-styled video games.

Also in this repo is the [BenVoxel project](BenVoxel/README.md) to develop an open standard for interoperable voxel model file formats.
## Usage
### Importing a model
```csharp
VoxFileModel model = new(@"..\..\..\Sora.vox");
IVoxelColor voxelColor = new NaiveDimmer(model.Palette);
```
The `VoxFileModel` class is used for importing [MagicaVoxel](https://ephtracy.github.io/) files. The `Sora.vox` file contains a test model based on the character Sora from Kingdom Hearts.

Colors are specified in [RGBA8888 format](https://en.wikipedia.org/wiki/RGBA_color_model#RGBA8888). The `IVoxelColor` interface is used to determine which colors to draw sprites with. The `NaiveDimmer` class implements `IVoxelColor` by interpolating lighter and darker versions from the provided palette, which in this snippet comes from the imported MagicaVoxel file.
### Drawing a sprite
```csharp
Sprite sprite = new SpriteMaker
{
	Model = model,
	VoxelColor = new NaiveDimmer(model.Palette),
	Perspective = Perspective.Iso,
	Outline = true,
	ScaleX = 2,
}.Make();
byte[] texture = sprite.Texture;
ushort width = sprite.Width,
	height = sprite.Height;
```
The `SpriteMaker` class uses [builder pattern](https://en.wikipedia.org/wiki/Builder_pattern) to construct `Sprite` objects.

The `Sprite` class provides a `byte[] Texture` array containing [RGBA8888 format](https://en.wikipedia.org/wiki/RGBA_color_model#RGBA8888) pixels and a `ushort Width` property for the texture's size. These would be referenced to export images.
### Creating a texture atlas
```csharp
Dictionary<string, Sprite> sprites = [];
byte direction = 0;
foreach (SpriteMaker spriteMaker in new SpriteMaker()
	{
		Model = model,
		VoxelColor = new NaiveDimmer(model.Palette),
		Outline = true,
	}.Iso8())
	sprites.Add("Sprite" + direction++, spriteMaker.Make());
Sprite output = new(sprites, out TextureAtlas textureAtlas);
```
The `SpriteMaker.Iso8` method generates makers for a series of 8 sprites using alternating isometric and 3/4ths camera perspectives from the 8 winds of the compass.

The sprites are then placed into a `Dictionary<string, Sprite>` data structure to associate each sprite with a unique name.

Finally, all the sprites are combined into one texture atlas, where `output` is a `Sprite` instance containing the texture and `textureAtlas` contains the metadata.
## Dependencies
### Voxel2Pixel
|Package|License|Included Via|Purpose|
|---|---|---|---|
|[`BenVoxel`](BenVoxel/README.md)|[MIT](LICENSE)|Project|Open standard for voxel model files|
|[`Cromulent.Encoding.Z85`](https://github.com/Trigger2991/Cromulent.Encoding.Z85)|[MIT](https://github.com/Trigger2991/Cromulent.Encoding.Z85/blob/master/LICENSE)|[NuGet](https://www.nuget.org/packages/Cromulent.Encoding.Z85)|BenVoxel JSON format encoding/decoding|
|[`FileToVoxCore`](https://github.com/Zarbuz/FileToVoxCore)|[MIT](https://github.com/Zarbuz/FileToVoxCore/blob/master/LICENSE)|[NuGet](https://www.nuget.org/packages/FileToVoxCore)|Parses [MagicaVoxel](https://ephtracy.github.io/) files|
|[`PolySharp`](https://github.com/Sergio0694/PolySharp)|[MIT](https://github.com/Sergio0694/PolySharp/blob/main/LICENSE)|[NuGet](https://www.nuget.org/packages/PolySharp)|Polyfills newer C# language features|
|[`RectPackSharp`](https://github.com/ThomasMiz/RectpackSharp)|[MIT](https://github.com/ThomasMiz/RectpackSharp/blob/main/LICENSE)|[NuGet](https://www.nuget.org/packages/RectpackSharp)|Packs rectangles for texture atlases|
### Voxel2Pixel.Test
The `Voxel2Pixel.Test` project also depends on [SixLabors.ImageSharp](https://github.com/SixLabors/ImageSharp), [Magick.NET](https://github.com/dlemstra/Magick.NET) and [SkiaSharp](https://github.com/mono/SkiaSharp) for image file output, but the actual Voxel2Pixel library does not include these.
### Voxel2Pixel.Web
|Package|License|Included Via|Purpose|
|---|---|---|---|
|`Voxel2Pixel`|[MIT](LICENSE)|Project|Voxel model rendering|
|[`MudBlazor`](https://mudblazor.com/)|[MIT](https://github.com/MudBlazor/MudBlazor/blob/dev/LICENSE)|[NuGet](https://www.nuget.org/packages/MudBlazor)|UI components|
|[`SixLabors.ImageSharp`](https://github.com/SixLabors/ImageSharp)|[Six Labors Split License](https://github.com/SixLabors/ImageSharp/blob/main/LICENSE)|[NuGet](https://www.nuget.org/packages/sixlabors.imagesharp/)|Image file output|

Note that using the other non-test projects without the web front-end does not impose the conditions of the Six Labors Split License.
