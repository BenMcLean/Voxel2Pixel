# BenVoxel Specification
BenVoxel is an open standard using sparse voxel octrees to compress voxel model geometry for file storage (with optional metadata) developed by Ben McLean. This specification is released under the [Creative Commons Attribution 4.0 International (CC BY 4.0)](https://creativecommons.org/licenses/by/4.0/) license.

The idea is to sacrifice processing speed to get a very small memory storage size for the geometry while keeping the implementation relatively simple and also allowing for extensive metadata to be optionally included.

There will be no license requirements restricting usage, but this format is designed for small voxel models intended for video games, animations or other entertainment, artistic or aesthetic use cases, so its design might not be ideal for unrelated academic, scientific, medical, industrial or military applications. Also, be aware that none of this has been engineered to provide security features (such as anti-cheat, checksums or length constraints for overflow protection) so use at your own risk.
## Overview
The BenVoxel standard describes two inter-related file formats. One is a binary format with the extension `.ben` and the other is a JSON format (recommended extension `.ben.json`) designed to contain all of the same information as the binary format but with the metadata kept human-readable. The JSON format uses Z85 encoding for the geometry data. A game developer might keep their voxel models in the JSON format during development but automatically convert to the binary format (potentially stripping out metadata) as part of their release pipeline.
## Definitions
### Coordinates
The BenVoxel standard adopts the MagicaVoxel Z+up right-handed 3D coordinate system where: X+ is right/east (width), Y+ is forward/north (depth), and Z+ is up (height). Negative coordinates cannot contain voxels. Models are expected to be aligned so that their lowest edge is on 0 of each axis.
### Special Keys
The empty string key has special meaning in several contexts:
- In models: Indicates the default model.
- In palettes: Indicates the default palette.
- In points: Specifies the model origin.
- In properties: Specifies the voxel scale.
### Format Conversion
When converting between JSON and binary formats:
1. Keys
   - JSON keys map directly to `KeyString` fields in binary format.
   - Empty string keys retain their special meanings across formats.
2. Geometry Data
   - Binary: Direct octree bytes.
   - JSON: DEFLATE (RFC 1951) compressed, Z85-encoded octree bytes.
### Implementation Requirements
   - Missing optional fields should be omitted rather than included as `null`.
   - Empty string keys must retain their special meaning.
   - Size constraints are mandatory for compatibility.
   - Out-of-bounds voxels are invalid and may be discarded without warning during reading as an implementation detail.
   - Files containing out-of-bounds voxels should still be readable if otherwise valid.
   - Key strings longer than 255 characters are invalid. Key strings that start or end with whitespace are invalid. However, it is recommended as an implementation detail to first trim whitespace, then truncate the keys with last-in-wins dictionary behavior.
   - Implementations are required to tolerate any unused 0-padding at the end of decompressed geometry data.
## JSON Format
### Schema
A JSON schema for documentation purposes only (not providing validation for security) is included in the file `benvoxel.schema.json`. All the objects and properties in the JSON format correspond directly to chunks or fields in the binary format.
### Structure
The JSON objects map to binary chunks as follows:

|JSON object|Binary chunk|
|---|---|
|Root|`BENV`|
|`metadata`|`DATA`|
|`models`|`MODL`|
|`properties`|`PROP`|
|`points`|`PT3D`|
|`palettes`|`PALC`|
|`geometry`|`SVOG`|
### Versioning
Both the binary and JSON formats include version information. In the binary format, this is a field in the `BENV` chunk. In the JSON format, this is in a root key called `version`. The version should be compared alphanumerically as a string, with higher values indicating newer versions.

Implementations should rely on the `version` property/field within the file for determining BenVoxel format feature support, not the schema version.
## Binary Format
### Type Definitions
All types are little-endian.
#### Strings
Three string types are used:
- `FourCC`: 4 byte ASCII chunk identifiers.
- `KeyString`: Starts with one unsigned byte for length, followed by a UTF-8 string of that length. Empty string is valid, but `KeyString`s that begin or end with whitespace are invalid. Duplicate (identical / non-unique) keys within the same sequence are invalid. It is recommended that implementations handle duplicate keys with last-in-wins dictionary behavior.
- `ValueString`: Starts with an unsigned 32-bit integer for length, followed by a UTF-8 string of that length.
### Chunks
All chunks have:
- 4 bytes: an ASCII identifier for this chunk (examples are "`FMT `" and "`DATA`"; note the space in "`FMT `").
- 4 bytes: an unsigned 32-bit integer with the length of this chunk (except this field itself and the chunk identifier).
- variable-sized field: the chunk data itself, of the size given in the previous field.

This applies to ***all*** chunks, so this information won't be repeated in the individual chunk type descriptions.
#### `BENV` chunk (Root)
BenVoxel binary files start with a `BENV` chunk which contains the entire file and corresponds to the root object in the JSON format. It contains:
- `Version`: One `KeyString` for version information. Higher alphanumeric comparison indicates higher version.
- The remaining data is compressed using raw DEFLATE (RFC 1951) and contains:
  - `Global`: One `DATA` chunk for global metadata. (optional)
  - `Count`: One unsigned 16-bit integer for the number of models.
  - For each model:
    - `Key`: One `KeyString` for the model key. Empty string key indicates the default model.
    - `Model`: One `MODL` chunk.

The size of the compressed data can be determined by subtracting the size of the Version field (the content of its unsigned 1-byte length field plus 1) from the `BENV` chunk size.
#### `DATA` chunk (Metadata)
Corresponds to the `metadata` key in the JSON format. It contains:
- `Properties`: One `PROP` chunk. (optional)
- `Points`: One `PT3D` chunk. (optional)
- `Palettes`: One `PALC` chunk. (optional)
#### `MODL` chunk (Model)
Corresponds to one of the `models` objects in the JSON format. It contains:
- `Metadata`: One `DATA` chunk. (optional)
- `Geometry`: One `SVOG` chunk.
#### `PROP` chunk (Properties)
Key-value pairs for arbitrary metadata.

Corresponds to one or more of the `properties` objects in the JSON format. It contains:
- `Count`: One unsigned 16-bit integer for the number of properties.
- For each property:
  - `Key`: One `KeyString` for the property key. Empty string key, if present, specifies the scale in meters of each voxel. This can be either a single decimal number applied to all dimensions (e.g. `1` for Minecraft-style 1m^3 voxels) or three comma-separated decimal numbers for width, depth, and height respectively (e.g. `2.4384,2.4384,2.92608` for Wolfenstein 3-D walls which are 8ft x 8ft x 9.6ft). Scale values must be positive decimal numbers in C# decimal format. If no empty string key is present then the scale is unspecified.
  - `Value`: One `ValueString` for the property value.
#### `PT3D` chunk (Points)
Named 3D points in space as [x, y, z] arrays. Uses 32-bit signed integers to allow points to be placed outside model bounds (including negative coordinates) for purposes like specifying offsets.

Corresponds to one or more of the `points` objects in the JSON format. It contains:
- `Count`: One unsigned 16-bit integer for the number of points.
- For each point:
  - `Key`: One `KeyString` for the point key. Empty string key specifies the origin of the model. If empty string key is specified in neither the model nor global metadata then the origin should be assumed to be at the bottom center of the model calculated as `[width >> 1, depth >> 1, 0]`.
  - `Coordinates`: Three signed 32-bit integers for the X, Y and Z coordinates.
#### `PALC` chunk (Palettes)
Named color palettes.

Corresponds to one or more `palettes` objects in the JSON format. It contains:
- `Count`: One unsigned 16-bit integer for the number of palettes.
- For each palette:
  - `Key`: One `KeyString` for the palette key. Empty string key indicates the default palette.
  - `Length`: One unsigned byte representing the number of colors minus one, with a range of 0-255 representing 1-256 colors. A value of `0` indicates 1 color, and a value of `255` indicates 256 colors. This range always includes the background color at index zero while the rest of the indices correspond to the voxel payload bytes which is the reason for the 256 color limit.
  - `Colors`: A series of `Length` 32-bit unsigned integers representing colors in RGBA format.
  - `HasDescriptions`: One unsigned byte with value `0` to indicate no descriptions xor any other value to include descriptions.
  - `Descriptions`: A series of `Length` `ValueString`s describing the colors. Only included if `HasDescriptions` is not `0`. A description should stay associated with the color it describes even when the colors or their order changes. The first line should be a short, human-readable message suitable for display as a tooltip in an editor. Additional lines can contain extra data such as material settings, which editors should preserve even if they don't use it.
#### `SVOG` chunk (Geometry)
Stands for "**S**parse **V**oxel **O**ctree **G**eometry". Corresponds to a `geometry` object in the JSON format. It contains:
- `Size`: Three 16-bit unsigned integers defining model extents on X, Y, and Z axes. Valid voxel coordinates range from 0 to size-1 for each axis. Any geometry data present at coordinates equal to or greater than the corresponding size value is invalid and may be retained or safely discarded without warning as an implementation detail. For example, in a model of size `[5,5,5]`, coordinates `[4,4,4]` are valid while coordinates `[5,4,4]` are out of bounds. Selectively discarding out-of-bounds voxels when deserializing is recommended but not required. However, it is also strongly recommended that files containing such out-of-bounds voxels which are otherwise valid should still be readable.
- `Geometry`: A variable length series of bytes which encodes the voxels according to the "Geometry" section of this document.
## Geometry
Both the JSON and binary formats use the same sparse voxel octree data format, except that only for the JSON format, serializing the geometry data requires the following additional processing steps:
1. The sparse voxel octree data is first compressed using raw DEFLATE. (RFC 1951)
2. The compressed data is then encoded using Z85 (ZeroMQ Base-85) to ensure valid JSON characters. This includes automatically padding the end of the data with zeroes to make the length a multiple of 4 as a requirement of Z85 encoding. All implementations are required to tolerate having these extra zeroes optionally present at the end of the geometry data even though there is no need to add this padding in the binary format.
3. Finally, the compressed and encoded string is stored in the `z85` property.

Deserializing from the JSON format reverses the process:
1. First, the string value of the `z85` property is decoded back into compressed data.
1. The compressed data is then decompressed using raw DEFLATE. (RFC 1951)
1. Finally, the decompressed data is read to deserialize the sparse voxel octree.

This ensures that the non-human-readable section of the JSON files is compressed to a minimum length while only using JSON-safe characters. Due to typical DEFLATE implementations being pseudo-non-deterministic, the compressed and encoded string is not guaranteed to match between two runs with the same uncompressed and unencoded input data. However, the decoded and decompressed output data will be binary identical to the (padded) input data because DEFLATE data compression is lossless.

The binary format uses the raw octree data directly without any additional compression or encoding applied to it. The binary format prefixes the model size to the geometry data in the `SVOG` chunk while the JSON format includes the model size in a separate `size` property instead, omitting the sizes from the compressed and encoded `z85` property.
### Voxels
An individual voxel is defined as having four data elements.
#### Coordinates
The first three data elements of a voxel are 16-bit unsigned integers for the X, Y and Z coordinates. Negative coordinates and coordinates larger than 65,534 are unsupported.
#### Payload
The fourth data element of a voxel is the payload of one byte for the index to reference a color or material, where 0 is reserved for an empty or absent voxel, leaving 255 usable colors or materials.
### Models
Models (sets of voxels) are limited by 16-bit unsigned integer bounds, so valid geometry can range from coordinates of 0 to 65,534. Model contents are expected to be following the MagicaVoxel convention, which is Z+up, right-handed, so X+ means right/east in width, Y+ means forwards/north in depth and Z+ means up in height. Models are expected to be aligned so that their lowest edge occupies coordinate 0 on all three axes.
### Octree
To serialize a model, geometry is structured as a sparse voxel octree for compression, so that the coordinates of the voxels are implied from their positions in the octree and empty spaces are not stored.

The octree has a fixed depth of 16 levels, corresponding to the 16 bits of addressable space in the unsigned 16-bit integer spatial coordinates. The first 15 levels consist of only Branch nodes, while the 16th and final level contains only Leaf nodes.
#### Nodes
There are four node types, which include one type of branch and three types of leaves.
##### Node Headers
All nodes start with a 1-byte header, composed of bits from left to right:
- Header bits 7-6: Node type indicator
  - `00`: Branch
  - `01`: 1-byte payload Leaf
  - `10`: 2-byte payload Leaf
  - `11`: 8-byte payload Leaf
- Header bits 5-3:
  - For Branch nodes: number of children (1-8) minus one.
  - For 2-byte Leaf nodes: ZYX octant of the foreground voxel.
  - For other node types: unused.
- Header bits 2-0: ZYX octant indicator
  - These bits encode the node's position relative to its parent in ascending Z, Y, X order.
  - `0` represents the negative direction and `1` represents the positive direction for each axis.
  - Examples:
    - `000`: (-Z, -Y, -X) octant
    - `111`: (+Z, +Y, +X) octant
    - `001`: (-Z, -Y, +X) octant
##### Branch nodes
In Branch nodes, the header byte is followed by the child nodes. On the 16th (last) level of the octree, all of the children will be Leaf nodes, but the children will all be Branch nodes on every other level.
##### Leaf nodes
Each leaf node represents the contents of a 2x2x2 voxel cube. There are three types of leaf nodes.
###### 1-byte payload Leaf nodes
These represent cubes of all the same color or material. The header byte is followed by one payload byte, which should fill in all eight voxels.
###### 2-byte payload Leaf nodes
These represent cubes in which all the voxels are the same except one, called the foreground voxel. The header byte is followed by two payload bytes: the first for the foreground voxel and then the second for the background voxels.

The octant/position of the foreground voxel is indicated by coordinates in header bits 5-3, using the same ZYX octant encoding scheme as described in the Node Headers section above.

The background voxel value should be repeated in all octants/positions except for the foreground voxel.
###### 8-byte payload Leaf nodes
These represent cubes of any arbitrary values. In 8-byte Leaf nodes, the header byte is followed by the eight payload bytes of a 2x2x2 voxel cube in ascending Z, Y, X order from left to right, with 0 representing empty voxels.
#### Empty models
An empty model would be represented by the hexadecimal string `0000000000000000000000000000004000` which is structured as follows:
- 15 bytes for branch node headers which are all `0`.
- 1 byte for a leaf node header with hexadecimal value `0x40`
- 1 byte for the payload of the leaf node

The 15 branch node headers correspond to the 15 levels of the octree needed to address a space with 16-bit integer coordinates (2^16 = 65,536, except remember to subtract one for zero-based indexing). The 16th level corresponds to the leaf node.
# BenVoxel Reference Implementation
An MIT-licensed reference implementation library is provided as a C# 8.0 .NET Standard 2.0 library via PolySharp.
## Dependencies
|Package|Liscense|Included Via|
|---|---|---|
|[`Cromulent.Encoding.Z85`](https://github.com/Trigger2991/Cromulent.Encoding.Z85)|[MIT](https://github.com/Trigger2991/Cromulent.Encoding.Z85/blob/master/LICENSE)|[NuGet](https://www.nuget.org/packages/Cromulent.Encoding.Z85)|
|[`PolySharp`](https://github.com/Sergio0694/PolySharp)|[MIT](https://github.com/Sergio0694/PolySharp/blob/main/LICENSE)|[NuGet](https://www.nuget.org/packages/PolySharp/)|
## Usage
### Read binary
```csharp
BenVoxelFile benVoxelFile;
using (FileStream binaryInputStream = new(
	path: "filename.ben",
	mode: FileMode.Open,
	access: FileAccess.Read))
{
	benVoxelFile = new(binaryInputStream);
}
```
### Write binary
```csharp
using (FileStream binaryOutputStream = new(
	path: "filename.ben",
	mode: FileMode.OpenOrCreate,
	access: FileAccess.Write))
{
	benVoxelFile.Write(binaryOutputStream);
}
```
### Read JSON
```csharp
BenVoxelFile benVoxelFile;
using (FileStream jsonInputStream = new(
	path: "filename.ben.json",
	mode: FileMode.Open,
	access: FileAccess.Read))
{
	benVoxelFile = new(JsonSerializer.Deserialize<JsonObject>(jsonInputStream)
		?? throw new NullReferenceException());
}
```
### Write JSON
```csharp
File.WriteAllText(path: "filename.ben.json", contents: benVoxelFile.ToJson().Tabs());
```
### Iterate voxels
```csharp
foreach (Voxel voxel in benVoxelFile.Models[""].Geometry)
```
