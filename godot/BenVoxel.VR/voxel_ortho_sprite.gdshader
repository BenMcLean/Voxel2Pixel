shader_type spatial;
// "unshaded": we do our own lighting. "cull_back": the proxy quad is turned to face the viewer every
// frame (see VoxelOrthoSprite.cs), so its back face is never visible.
render_mode unshaded, cull_back;

// =============================================================================
// OVERVIEW
// =============================================================================
// Volumetric ortho-sprite preview. Adapted from redot/BenVoxelGpu/volumetric_ortho_sprite.gdshader.
//
// The model is drawn like a 2D sprite: parallel rays, a chunky virtual-pixel grid and a one-pixel black
// outline, all in a single fragment pass. It is a preview only; it reads the same live voxel textures as
// the editable model (voxel_storage.gdshaderinc), so edits appear in it immediately.
//
// HOW IT WORKS:
//   1. A camera-facing QuadMesh, sized to the model's orthographic footprint plus a margin for the
//      outline, is the rasterisation footprint (VoxelOrthoSprite.cs rebuilds it every frame).
//   2. Each fragment knows where it is on that quad (quad_uv). Scaled by quad_size this is a position in
//      the sprite plane in voxel units. It is snapped to the virtual pixel grid, which has `sigma`
//      pixels per voxel.
//   3. A ray with the shared direction ray_dir_local starts at the snapped position and is marched
//      through the voxels (march_box). A hit outputs the palette colour with per-face lighting. A miss
//      tests the four cardinal neighbour pixels; if any hits, this pixel is outline.
//
// WHY THE GRID COMES FROM THE QUAD AND NOT FROM THE SCREEN (the original shader used screen position):
//   The quad is a flat card. In VR each eye sees the same card from a slightly different place, so the
//   position on the card is the same physical point for both eyes, the pixel grid is identical in both,
//   and the sprite reads as a flat billboard with no per-eye pixel shimmer. The ray direction and the
//   quad orientation come from the HEAD position (the midpoint between the eyes), not the per-eye
//   position, so both eyes also march identical rays. The quad's own depth is the sprite's depth.
//   Because nothing depends on the projection matrix, this also works unchanged on the flat screen.
//
// COORDINATE SPACE: all vectors are in Godot Y-up voxel space (one unit per voxel), the same space as
// the DDA in voxel_storage.gdshaderinc, i.e. the local space of the VoxelOrthoSprite node.
// =============================================================================

#include "res://voxel_storage.gdshaderinc"

// Direction from the viewer's head to the model centre, unit length. Shared by every ray (orthographic).
uniform vec3 ray_dir_local;

// Sprite up direction, unit length and perpendicular to ray_dir_local. It is derived from the node's own
// up, not the camera's, so the sprite stays upright relative to its own orientation.
// Sprite right is cross(ray_dir_local, sprite_up_local).
uniform vec3 sprite_up_local;

// Virtual pixels per voxel. Higher is finer; 1 makes every voxel one chunky pixel.
uniform float sigma = 4.0;

// Size of the proxy quad in voxel units. The quad is centred on the model's bounding box centre.
uniform vec2 quad_size = vec2(1.0);

// Unit vector from the model towards the light, in voxel space.
uniform vec3 light_dir = vec3(0.57735);

// Position on the unit quad, -0.5..0.5 on each axis. Interpolated perspective-correctly, so it is the
// same point on the card whichever eye is rendering.
varying vec2 quad_uv;

void vertex() {
    quad_uv = VERTEX.xy;
}

// Marches one sprite-plane ray. `offset` is a position in the sprite plane relative to the model centre,
// in voxel units. The ray starts well in front of the model and is marched through it.
uint trace_sprite_ray(vec2 offset, vec3 sprite_right, vec3 box_min, vec3 box_max, out vec3 normal) {
    // Pull the start back by twice the largest dimension so it is certainly outside the box; march_box
    // then finds the entry point itself.
    float max_dim = max(max(box_size.x, box_size.y), box_size.z);
    vec3 origin = box_center + offset.x * sprite_right + offset.y * sprite_up_local - 2.0 * max_dim * ray_dir_local;
    vec3 hit_pos;
    return march_box(origin, ray_dir_local, box_min, box_max, normal, hit_pos);
}

void fragment() {
    vec3 sprite_right = cross(ray_dir_local, sprite_up_local);
    float delta_px = 1.0 / sigma; // width of one virtual pixel in voxel units

    vec3 box_min = box_center - box_size * 0.5;
    vec3 box_max = box_center + box_size * 0.5;

    // Snap to the nearest virtual pixel centre so each virtual pixel covers a block of screen fragments.
    vec2 offset = round(quad_uv * quad_size / delta_px) * delta_px;

    vec3 normal;
    uint material = trace_sprite_ray(offset, sprite_right, box_min, box_max, normal);

    if (material > 0u) {
        // Same lighting as voxel_raymarch.gdshader: diffuse from the light plus 30% ambient. The normal
        // is one of the six axis directions, so each visible face has one flat shade.
        float lighting = max(dot(normal, light_dir), 0.0) * 0.7 + 0.3;
        ALBEDO = get_material_color(material) * lighting;
    } else {
        // Miss: outline if any of the four cardinal neighbours (no diagonals, as in classic sprite
        // outlines) hit. Anything else is empty space.
        vec3 unused;
        if (trace_sprite_ray(offset - vec2(delta_px, 0.0), sprite_right, box_min, box_max, unused) > 0u
                || trace_sprite_ray(offset + vec2(delta_px, 0.0), sprite_right, box_min, box_max, unused) > 0u
                || trace_sprite_ray(offset + vec2(0.0, delta_px), sprite_right, box_min, box_max, unused) > 0u
                || trace_sprite_ray(offset - vec2(0.0, delta_px), sprite_right, box_min, box_max, unused) > 0u) {
            ALBEDO = vec3(0.0);
        } else {
            discard;
        }
    }
}
