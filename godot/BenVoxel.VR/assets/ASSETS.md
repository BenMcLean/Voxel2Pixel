# Third-party assets

Every asset in this project that was not made for it is listed here with where it came from, so its origin and license can always be checked. Add an entry whenever an asset is added, and do not add an asset whose license is not recorded here.

Only the files the game uses are kept here. The original downloads are not stored in the repo; the download URLs below fetch them again if they are ever needed.

## Pencil (pencil tool icon)

- **Page:** https://opengameart.org/content/pencil-low-poly-3d-model
- **Download:** https://opengameart.org/sites/default/files/Pencil.zip
- **Author:** Code Inferno Games ([profile](https://opengameart.org/users/codeinfernogames), codeinferno.com)
- **License:** [CC0 1.0](http://creativecommons.org/publicdomain/zero/1.0/) (public domain; no credit required)
- **Retrieved:** 2026-10-17
- **Contents:** low-poly mesh (25 vertices) with a 1024x1024 diffuse texture
- **In the project:** `pencil/pencil.glb` (converted from the original FBX), `pencil/readme.txt` (the author's own license note).
- **Changes:** converted to glTF binary with Blender 5.1.2. The FBX has UVs but no material and no texture reference, so a material using the supplied `diffuse.png` as its base colour was added and the texture embedded in the `.glb`. The mesh was not edited.

## Treasure chest (toy box)

- **Page:** https://opengameart.org/content/low-poly-pixel-art-chest
- **Download:** https://opengameart.org/sites/default/files/pixel%20chest.zip
- **Author:** kev93
- **License:** [CC0 1.0](http://creativecommons.org/publicdomain/zero/1.0/) (public domain)
- **Retrieved:** 2026-10-17
- **Contents:** Blender 2.73 file, low-poly mesh (18 vertices) with a 32x32 pixel-art texture. No glTF or FBX is supplied.
- **In the project:** `chest/chest.glb`, converted from the original.
- **Changes:** converted to glTF binary with Blender 5.1.2. The `.blend` uses 2.73-era textures that Blender cannot convert to modern materials, so the texture was reapplied by hand (an Image Texture node with Closest, i.e. nearest-neighbour, filtering feeding Base Color; roughness 1) and embedded in the `.glb`. The mesh geometry was not edited, but the UVs of the eight U-shaped end faces were: the original snapped the arch corners to whole texture pixels, shearing the pixel grid there, so those faces were remapped with one exact linear mapping (6.74 texture pixels per unit; the face is 11 px wide for 1.644 units), giving perfectly square pixels. The remapped faces reach slightly past the painted artwork and showed the white background, so every white texel in the texture (the unused background, 313 of 1024) was replaced with the artwork's light grey rim colour (189,181,181); the result is the texture embedded in the `.glb`. The artwork itself is otherwise unchanged. The other faces keep the original UVs (their pixels are slightly uneven). It is about 3.5 units wide at its Blender scale (the node is scaled 1.78), so scale it down in Godot.
