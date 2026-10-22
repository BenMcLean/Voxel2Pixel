shader_type spatial;
// cull_front: only back faces rasterise, so the box footprint is covered whether the eye is outside or inside it.
// The ray for each pixel is rebuilt from the eye through SCREEN_UV (not from the interpolated vertex position),
// so the six box faces never produce seams. DEPTH is written at the hit voxel, so no depth_draw_* mode is needed.
render_mode unshaded, cull_front;

// =============================================================================
// OVERVIEW
// =============================================================================
// Bounding-box mesh + DDA raymarcher for sparse, segmented voxel models.
//
// HOW IT WORKS:
//   1. A BoxMesh sized to the model's bounds (see Root.cs) is the rasterisation footprint. Its only job
//      is to make the GPU run the fragment shader for every pixel the model could cover.
//   2. The fragment shader ignores the box geometry. For each pixel it casts a ray from the eye
//      through that pixel, intersects it analytically with the bounding box (ray-AABB slab test) and
//      walks the voxel grid from the entry point with a DDA (Digital Differential Analyser) until it
//      finds a non-empty voxel or leaves the box.
//   3. On a hit it outputs the palette colour (with simple per-face lighting) and writes DEPTH at the
//      hit point. On a miss the pixel is discarded.
//
// WHY THE RAY IS NOT TAKEN FROM THE BOX SURFACE:
//   An earlier version started the ray at the interpolated box-surface position. A voxel face lying
//   exactly on the box surface (the model's top face always does) then sat on a floor() boundary, and
//   per-pixel float error decided which voxel the DDA started in. That produced fuzzy faces, wrong
//   first-hit lighting, and (for faces at coordinate 0) vanishing faces. Intersecting the ray with the
//   box analytically and clamping the origin just inside it removes the whole class of problem.
//
// COORDINATE SPACES:
//   World space       - Godot Y-up right-handed.
//   Mesh-local space  - world transformed by inv_model_matrix; origin = box centre.
//   Voxel space       - mesh-local + box_center. Godot Y-up, one unit per voxel, box corner at
//                       box_center - box_size / 2. The DDA runs here.
//   Stored voxel data is Z-up (MagicaVoxel convention), so fetch_voxel() swaps Y and Z:
//                       (x, y, z) Y-up voxel space  ->  (x, z, y) Z-up storage.
//
// VOXEL STORAGE (see VoxelBridge.cs and voxel_storage.gdshaderinc, which holds the fetch and DDA code
// shared with voxel_ortho_sprite.gdshader):
//   The model is split into 128^3-voxel segments. Each segment is a 64^3 3D texture of "bricks"; one
//   brick is 2x2x2 voxels packed into 8 bytes (one byte of palette index per voxel). A small directory
//   texture lists which segments exist and which texture-array slot each one lives in.
//
// VR (OpenXR multiview):
//   VIEW_MATRIX / INV_VIEW_MATRIX / PROJECTION_MATRIX are the MIDPOINT (cyclopean) matrices and are the
//   same for both eyes. EYE_OFFSET is the per-eye position in that midpoint view space (+-IPD/2 on X;
//   zero when not rendering in VR). The per-eye ray origin and direction are derived from it in
//   fragment(), which is what produces correct stereo parallax. The shader never reads the vertex
//   position, so there is no vertex() function and nothing to go wrong across multiview.

#include "res://voxel_storage.gdshaderinc"

// =============================================================================
// UNIFORMS
// =============================================================================
// The voxel storage uniforms (segments, bricks, palette, box_center, box_size)
// are declared in voxel_storage.gdshaderinc.

// Inverse of the MeshInstance's GlobalTransform, precomputed on the CPU every frame (Root._Process).
// Godot provides MODEL_MATRIX in the fragment stage but not its inverse, and inverting a matrix per
// fragment would be wasteful. Because this includes the full node transform, the box can be moved,
// rotated or scaled (for example under a VR controller) and the rays follow automatically.
uniform mat4 inv_model_matrix;

// Position of the point light in voxel space (Root.cs converts the light sphere's world position every
// frame). Only the position matters; a point light has no orientation.
uniform vec3 light_position = vec3(50.0, 50.0, 25.0);

// =============================================================================
// FRAGMENT SHADER
// =============================================================================

void fragment() {
    // ---- Per-eye ray ----
    // In VR multiview, INV_VIEW_MATRIX is the midpoint (cyclopean) view and EYE_OFFSET is the
    // per-eye offset in that view space (zero when not in VR). Treating EYE_OFFSET as a point in
    // midpoint view space and transforming it to world space gives this eye's position; using
    // vec4(0,0,0,1) instead would give the midpoint, identical for both eyes, and no stereo parallax.
    vec3 camera_world = (INV_VIEW_MATRIX * vec4(EYE_OFFSET, 1.0)).xyz;

    // Unproject this pixel through the (midpoint) projection into view space, then aim the ray from
    // the per-eye position through that point. Using EYE_OFFSET for the direction as well as the
    // origin keeps the two consistent. w = 0 in INV_VIEW_MATRIX applies rotation only.
    vec4 view_frag = INV_PROJECTION_MATRIX * vec4(SCREEN_UV * 2.0 - 1.0, 0.0, 1.0);
    view_frag /= view_frag.w;
    vec3 ray_dir_view = normalize(view_frag.xyz - EYE_OFFSET);
    vec3 ray_world_dir = normalize((INV_VIEW_MATRIX * vec4(ray_dir_view, 0.0)).xyz);

    // ---- World space -> voxel space ----
    // The point transform gives mesh-local coordinates (origin at the box centre); adding box_center
    // gives voxel coordinates. The direction uses w = 0 (no translation) and is renormalised because
    // the inverse of a scaled matrix changes vector length.
    vec3 camera_voxel = (inv_model_matrix * vec4(camera_world, 1.0)).xyz + box_center;
    vec3 ray_dir = normalize((inv_model_matrix * vec4(ray_world_dir, 0.0)).xyz);
    vec3 box_min = box_center - box_size * 0.5;
    vec3 box_max = box_center + box_size * 0.5;

    // ---- March ----
    // See march_box() in voxel_storage.gdshaderinc for the box intersection and the DDA.
    vec3 normal;
    vec3 hit_pos;
    uint material = march_box(camera_voxel, ray_dir, box_min, box_max, normal, hit_pos);
    if (material == 0u) discard; // Miss: leave the pixel untouched (and keep the material opaque)

    // Point light at light_position (voxel space) plus 30% ambient, with no distance falloff. Faces
    // turned away from the light are not darker than ambient. Wrapped in max() so back-facing
    // normals clamp to 0.
    vec3 to_light = normalize(light_position - hit_pos);
    float lighting = max(dot(normal, to_light), 0.0) * 0.7 + 0.3;
    ALBEDO = get_material_color(material) * lighting;

    // ---- Depth at the hit voxel surface ----
    // Without this the depth buffer would hold the depth of the box's back face. That would make other
    // geometry intersect the model as if it were a solid box, and in VR the OpenXR compositor uses
    // depth for reprojection (ATW/ASW), so a flat box would be warped as a plane and cancel the per-eye
    // parallax the DDA computed.
    // Chain: voxel space -> mesh-local (subtract box_center) -> world (MODEL_MATRIX) -> clip space.
    vec4 hit_clip = PROJECTION_MATRIX * VIEW_MATRIX * MODEL_MATRIX * vec4(hit_pos - box_center, 1.0);
    DEPTH = hit_clip.z / hit_clip.w;
}
