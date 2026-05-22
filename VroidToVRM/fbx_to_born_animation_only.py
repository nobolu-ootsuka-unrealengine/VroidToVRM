"""
Blender background script: FBX -> FBX that keeps bone animation and blend shape animation.
Usage: blender -b --python fbx_to_born_animation_only.py -- <input.fbx> <output.fbx>
"""
import os
import sys

import bpy


def parse_args():
    argv = sys.argv
    if "--" not in argv:
        print("Usage: blender -b --python fbx_to_born_animation_only.py -- <input.fbx> <output.fbx>")
        sys.exit(1)
    args = argv[argv.index("--") + 1:]
    if len(args) < 2:
        print("Missing arguments: <input.fbx> <output.fbx>")
        sys.exit(1)
    return args[0], args[1]


def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def import_fbx(path):
    if not os.path.isfile(path):
        sys.exit(f"[ERROR] Input FBX not found: {path}")
    bpy.ops.import_scene.fbx(filepath=path)


def find_armatures():
    return [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]


def has_shapekey_animation(obj):
    shape_keys = getattr(obj.data, "shape_keys", None)
    if not shape_keys or not shape_keys.animation_data:
        return False
    anim_data = shape_keys.animation_data
    if anim_data.action:
        return True
    for track in anim_data.nla_tracks:
        for strip in track.strips:
            if strip.action:
                return True
    return False


def find_blendshape_meshes():
    return [obj for obj in bpy.context.scene.objects if obj.type == "MESH" and has_shapekey_animation(obj)]


def base_object_name(name):
    if len(name) > 4 and name[-4] == "." and name[-3:].isdigit():
        return name[:-4]
    return name


def find_same_name_helper_objects(meshes):
    mesh_bases = {base_object_name(mesh.name) for mesh in meshes}
    helpers = []
    for obj in bpy.context.scene.objects:
        if obj.type not in {"EMPTY", "ARMATURE"}:
            continue
        if base_object_name(obj.name) in mesh_bases:
            helpers.append(obj)
    return helpers

def find_same_name_helper_object(mesh):
    mesh_base = base_object_name(mesh.name)
    for obj in bpy.context.scene.objects:
        if obj == mesh or obj.type != "EMPTY":
            continue
        if base_object_name(obj.name) == mesh_base:
            return obj
    return None


def parent_blendshape_meshes_to_helpers(meshes):
    parented = 0
    for mesh in meshes:
        helper = find_same_name_helper_object(mesh)
        if helper is None or mesh.parent == helper:
            continue
        world_matrix = mesh.matrix_world.copy()
        mesh.parent = helper
        mesh.matrix_world = world_matrix
        parented += 1
    return parented

def iter_action_fcurve_collections(action):
    if action is None:
        return

    # Blender 4.x legacy action API.
    if hasattr(action, "fcurves"):
        yield action.fcurves
        return

    # Blender 5.x layered action API.
    for layer in getattr(action, "layers", []):
        for strip in getattr(layer, "strips", []):
            for channelbag in getattr(strip, "channelbags", []):
                yield channelbag.fcurves


def clean_action_curves(action, keep_curve):
    kept = 0
    removed = 0
    for fcurves in iter_action_fcurve_collections(action) or []:
        for fcurve in list(fcurves):
            if keep_curve(fcurve):
                kept += 1
            else:
                fcurves.remove(fcurve)
                removed += 1
    return kept, removed


def clean_actions_from_anim_data(anim_data, keep_curve, seen_actions):
    total_kept = 0
    total_removed = 0
    if not anim_data:
        return total_kept, total_removed

    actions = []
    if anim_data.action:
        actions.append(anim_data.action)
    for track in anim_data.nla_tracks:
        for strip in track.strips:
            if strip.action:
                actions.append(strip.action)

    for action in actions:
        action_id = action.as_pointer()
        if action_id in seen_actions:
            continue
        kept, removed = clean_action_curves(action, keep_curve)
        total_kept += kept
        total_removed += removed
        seen_actions.add(action_id)

    return total_kept, total_removed


def clean_armature_animation(armatures):
    seen_actions = set()
    total_kept = 0
    total_removed = 0
    for armature in armatures:
        kept, removed = clean_actions_from_anim_data(
            armature.animation_data,
            lambda fcurve: fcurve.data_path.startswith('pose.bones['),
            seen_actions,
        )
        total_kept += kept
        total_removed += removed
    return total_kept, total_removed


def clean_blendshape_animation(meshes):
    seen_actions = set()
    total_kept = 0
    total_removed = 0
    for mesh in meshes:
        shape_keys = mesh.data.shape_keys
        kept, removed = clean_actions_from_anim_data(
            shape_keys.animation_data,
            lambda fcurve: fcurve.data_path.startswith('key_blocks[') and fcurve.data_path.endswith('.value'),
            seen_actions,
        )
        total_kept += kept
        total_removed += removed
    return total_kept, total_removed


def delete_unneeded_objects(armatures, blendshape_meshes):
    keep = set(armatures) | set(blendshape_meshes)
    for obj in list(bpy.context.scene.objects):
        if obj not in keep:
            bpy.data.objects.remove(obj, do_unlink=True)


def remove_armature_modifiers_from_blendshape_meshes(meshes):
    removed = 0
    for mesh in meshes:
        for modifier in list(mesh.modifiers):
            if modifier.type == "ARMATURE":
                mesh.modifiers.remove(modifier)
                removed += 1
    return removed

def collect_parent_objects(objects, stop_objects):
    parents = []
    seen = set(obj.as_pointer() for obj in objects + stop_objects)
    for obj in objects:
        parent = obj.parent
        while parent is not None:
            parent_id = parent.as_pointer()
            if parent_id in seen:
                break
            parents.append(parent)
            seen.add(parent_id)
            parent = parent.parent
    return parents


def select_export_objects(armatures, blendshape_meshes):
    bpy.ops.object.select_all(action="DESELECT")
    helper_objects = find_same_name_helper_objects(blendshape_meshes)
    parent_objects = collect_parent_objects(blendshape_meshes + helper_objects, armatures)
    export_objects = armatures + parent_objects + helper_objects + blendshape_meshes
    for obj in export_objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = armatures[0]

def strip_blender_duplicate_suffix(name):
    if len(name) > 4 and name[-4] == "." and name[-3:].isdigit():
        return name[:-4]
    return name


def patch_fbx_export_names():
    from io_scene_fbx import export_fbx_bin

    original_fbx_name_class = export_fbx_bin.fbx_name_class

    def fbx_name_class_without_duplicate_suffix(name, cls):
        if isinstance(name, bytes):
            text_name = name.decode("utf-8", errors="ignore")
            stripped_name = strip_blender_duplicate_suffix(text_name).encode("utf-8")
            return original_fbx_name_class(stripped_name, cls)
        return original_fbx_name_class(name, cls)

    export_fbx_bin.fbx_name_class = fbx_name_class_without_duplicate_suffix
    return export_fbx_bin, original_fbx_name_class

def export_fbx(path, armatures, blendshape_meshes):
    os.makedirs(os.path.dirname(os.path.abspath(path)), exist_ok=True)
    select_export_objects(armatures, blendshape_meshes)
    export_fbx_bin, original_fbx_name_class = patch_fbx_export_names()
    try:
        bpy.ops.export_scene.fbx(
            filepath=path,
            use_selection=True,
            apply_unit_scale=False,
            apply_scale_options="FBX_SCALE_NONE",
            bake_space_transform=False,
            object_types={"ARMATURE", "MESH", "EMPTY"},
            use_mesh_modifiers=False,
            add_leaf_bones=False,
            primary_bone_axis="Y",
            secondary_bone_axis="X",
            armature_nodetype="NULL",
            bake_anim=True,
            bake_anim_use_all_bones=True,
            bake_anim_use_nla_strips=False,
            bake_anim_use_all_actions=False,
            bake_anim_force_startend_keying=True,
            bake_anim_step=1.0,
            bake_anim_simplify_factor=0.0,
            axis_forward="-Z",
            axis_up="Y",
            global_scale=1.0,
        )
    finally:
        export_fbx_bin.fbx_name_class = original_fbx_name_class

def main():
    input_path, output_path = parse_args()
    clear_scene()
    import_fbx(input_path)

    armatures = find_armatures()
    if not armatures:
        sys.exit(f"[ERROR] Armature not found: {input_path}")

    blendshape_meshes = find_blendshape_meshes()
    bone_kept, bone_removed = clean_armature_animation(armatures)
    shape_kept, shape_removed = clean_blendshape_animation(blendshape_meshes)
    reparented_shapes = parent_blendshape_meshes_to_helpers(blendshape_meshes)
    removed_shape_armatures = remove_armature_modifiers_from_blendshape_meshes(blendshape_meshes)

    if bone_kept == 0:
        print("[WARN] No pose bone animation curves were found.")
    if not blendshape_meshes:
        print("[INFO] No blend shape animation meshes were found.")

    # Keep the imported hierarchy alive until export. Deleting FBX helper objects before export can change skinned mesh axes.
    export_fbx(output_path, armatures, blendshape_meshes)

    print(f"[OK] {os.path.basename(input_path)} -> {os.path.basename(output_path)}")
    print(f"[INFO] Armatures: {len(armatures)}, blend shape meshes: {len(blendshape_meshes)}, same-name helpers: {len(find_same_name_helper_objects(blendshape_meshes))}, reparented shapes: {reparented_shapes}, removed shape armatures: {removed_shape_armatures}")
    print(f"[INFO] Kept bone curves: {bone_kept}, removed armature non-bone curves: {bone_removed}")
    print(f"[INFO] Kept blend shape curves: {shape_kept}, removed shape-key non-value curves: {shape_removed}")


if __name__ == "__main__":
    main()






