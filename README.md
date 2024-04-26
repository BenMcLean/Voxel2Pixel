# Voxel2Pixel
Voxel2Pixel is a [C#](https://dotnet.microsoft.com/en-us/languages/csharp) [.NET Standard 2.0](https://learn.microsoft.com/en-us/dotnet/standard/net-standard) library for rendering [voxel models](https://www.megavoxels.com/learn/what-is-a-voxel/) as [pixel art assets](https://2dwillneverdie.com/intro/) from a variety of fixed [camera perspectives](https://opengameart.org/content/chapter-3-perspectives) and [palette limitations](https://lospec.com/palette-list) intended for use in retro-styled video games.
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
Sprite sprite = new(VoxelDraw.IsoWidth(model), VoxelDraw.IsoHeight(model))
{
	VoxelColor = voxelColor,
};
VoxelDraw.Iso(model, sprite);
sprite = sprite.TransparentCrop().Upscale(2);
```
The `Sprite` class provides a `byte[] Texture` array containing [RGBA8888 format](https://en.wikipedia.org/wiki/RGBA_color_model#RGBA8888) pixels and a `ushort Width` property for the texture's size. These would be referenced to export images.

This snippet constructs a `Sprite` instance by calling methods that use the model's size to determine the texture's size and references the `model` and `voxelColor` variables from the previous section.

The `VoxelDraw.Iso` method draws the provided `model` onto the provided renderer `sprite` from an isometric camera perspective. The `VoxelDraw` class also contains similar methods for other camera perspectives such as `Above`, `Diagonal`, `Front`, `Overhead` and `Underneath`.

The `TransparentCrop` method crops out transparent pixels from the edges and the `Upscale` method stretches out the sprite to double the width, which tends to look better for isometric sprites.
### Creating a texture atlas
```csharp
IEnumerable<ISprite> sprites = Sprite.Iso8(model, voxelColor)
	.Select(sprite => sprite.CropOutline());
Dictionary<string, ISprite> dictionary = new();
byte direction = 0;
foreach (ISprite iSprite in sprites)
	dictionary.Add("Sora" + direction++, iSprite);
Sprite output = new(dictionary, out TextureAtlas textureAtlas);
```
The `VoxelDraw.Iso8` method draws a series of 8 sprites using alternating isometric and 3/4ths camera perspectives from the 8 winds of the compass. The `CropOutline` method crops each sprite to remove transparent pixels from the edges and adds a 1 pixel black outline.

The sprites are then placed into a `Dictionary<string, ISprite>` data structure to associate each sprite with a unique name.

Finally, all the sprites are combined into one texture atlas, where `output` is a `Sprite` instance containing the texture and `textureAtlas` is an object containing the metadata which is serializable to XML.
## Dependencies
|Package|License|Purpose|
|---|---|---|
|[Cromulent.Encoding.Z85](https://github.com/Trigger2991/Cromulent.Encoding.Z85)|[MIT](https://github.com/Trigger2991/Cromulent.Encoding.Z85/blob/master/LICENSE)|Experimental voxel model file format encoding|
|[FileToVoxCore](https://github.com/Zarbuz/FileToVoxCore)|Unspecified|Parses [MagicaVoxel](https://ephtracy.github.io/) files|
|[PolySharp](https://github.com/Sergio0694/PolySharp)|[MIT](https://github.com/Sergio0694/PolySharp/blob/main/LICENSE)|Polyfills newer C# language features|
|[RectPackSharp](https://github.com/ThomasMiz/RectpackSharp)|[MIT](https://github.com/ThomasMiz/RectpackSharp/blob/main/LICENSE)|Packs rectangles for texture atlases|
