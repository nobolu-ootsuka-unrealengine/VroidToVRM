"""
Blender バックグラウンドスクリプト: BVH → FBX (Unity/VRM 向け)
使い方: blender -b --python bvh_to_fbx.py -- <input.bvh> <output.fbx>
"""
import bpy
import sys
import os


def main():
    bvh_path, fbx_path = parse_args()

    os.makedirs(os.path.dirname(os.path.abspath(fbx_path)), exist_ok=True)

    # シーンをまっさらにする
    bpy.ops.wm.read_factory_settings(use_empty=True)

    # BVH インポート
    if not hasattr(bpy.ops.import_anim, 'bvh'):
        sys.exit("[ERROR] BVH インポーターが使えません。Blender 4.x/5.x の Animation アドオンを確認してください。")

    bpy.ops.import_anim.bvh(
        filepath=bvh_path,
        use_fps_scale=False,
        update_scene_fps=True,
        update_scene_duration=True,
        global_scale=1.0,
        rotate_mode='NATIVE',
    )

    armatures = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE']
    if not armatures:
        sys.exit(f"[ERROR] アーマチュアが見つかりません: {bvh_path}")

    # FBX エクスポート（Unity Humanoid 向け設定）
    bpy.ops.export_scene.fbx(
        filepath=fbx_path,
        use_selection=False,
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_NONE',
        bake_space_transform=True,
        object_types={'ARMATURE'},
        use_mesh_modifiers=False,
        add_leaf_bones=False,          # Unity では OFF が必須
        primary_bone_axis='Y',
        secondary_bone_axis='X',
        armature_nodetype='NULL',
        bake_anim=True,
        bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=False,
        bake_anim_force_startend_keying=True,
        bake_anim_step=1.0,
        bake_anim_simplify_factor=1.0,
        axis_forward='-Z',             # Unity 座標系
        axis_up='Y',
        global_scale=1.0,
    )

    print(f"[OK] {os.path.basename(bvh_path)}  →  {os.path.basename(fbx_path)}")


def parse_args():
    argv = sys.argv
    if "--" not in argv:
        print("Usage: blender -b --python bvh_to_fbx.py -- <input.bvh> <output.fbx>")
        sys.exit(1)
    args = argv[argv.index("--") + 1:]
    if len(args) < 2:
        print("引数が不足しています: <input.bvh> <output.fbx>")
        sys.exit(1)
    return args[0], args[1]


if __name__ == "__main__":
    main()
