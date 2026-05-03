using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Diagnostics;

public class VRMAnimationSetup : EditorWindow
{
    private Object animFBX;
    private Object animBVH;
    private GameObject vrmTarget;
    private string blenderPath = "";

    [MenuItem("Tools/VRM - Animation Setup")]
    public static void ShowWindow()
    {
        GetWindow<VRMAnimationSetup>("VRM Animation Setup");
    }

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(blenderPath))
            blenderPath = FindBlender() ?? "";
    }

    private void OnGUI()
    {
        vrmTarget = (GameObject)EditorGUILayout.ObjectField("VRM GameObject (Hierarchy)", vrmTarget, typeof(GameObject), true);

        EditorGUILayout.Space(8);

        // --- FBX セクション ---
        EditorGUILayout.LabelField("▼ FBX アニメーション", EditorStyles.boldLabel);
        animFBX = EditorGUILayout.ObjectField("Animation FBX", animFBX, typeof(Object), false);

        using (new EditorGUI.DisabledGroupScope(animFBX == null || vrmTarget == null))
        {
            if (GUILayout.Button("FBX セットアップ実行  ( Humanoid → Controller → Animator )", GUILayout.Height(34)))
                ExecuteFBX();
        }

        EditorGUILayout.Space(12);

        // --- BVH セクション ---
        EditorGUILayout.LabelField("▼ BVH アニメーション  ( Blender で FBX 変換 → セットアップ )", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        blenderPath = EditorGUILayout.TextField("Blender", blenderPath);
        if (GUILayout.Button("Auto", GUILayout.Width(48)))
        {
            string found = FindBlender();
            if (found != null) blenderPath = found;
            else UnityEngine.Debug.LogWarning("[VRM Setup] Blender が見つかりませんでした。パスを手動で入力してください。");
        }
        EditorGUILayout.EndHorizontal();

        animBVH = EditorGUILayout.ObjectField("Animation BVH", animBVH, typeof(Object), false);

        bool blenderOk = !string.IsNullOrEmpty(blenderPath) && File.Exists(blenderPath);
        if (!blenderOk)
            EditorGUILayout.HelpBox("Blender のパスが見つかりません。", MessageType.Warning);

        using (new EditorGUI.DisabledGroupScope(animBVH == null || vrmTarget == null || !blenderOk))
        {
            if (GUILayout.Button("BVH セットアップ実行  ( BVH→FBX 変換 → Controller → Animator )", GUILayout.Height(34)))
                ExecuteBVH();
        }
    }

    // ------------------------------------------------------------------ FBX

    private void ExecuteFBX()
    {
        string path = AssetDatabase.GetAssetPath(animFBX);
        if (!ValidateExtension(path, ".fbx", "FBX")) return;

        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null) { UnityEngine.Debug.LogError($"[VRM Setup] ModelImporter 取得失敗: {path}"); return; }

        if (importer.animationType != ModelImporterAnimationType.Human)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            importer.SaveAndReimport();
        }

        AnimationClip clip = FindClipInAsset(path);
        if (clip == null) { UnityEngine.Debug.LogError($"[VRM Setup] AnimationClip が見つかりません: {path}"); return; }

        ApplySetup(path, clip);
    }

    // ------------------------------------------------------------------ BVH

    private void ExecuteBVH()
    {
        string bvhAssetPath = AssetDatabase.GetAssetPath(animBVH);
        if (!ValidateExtension(bvhAssetPath, ".bvh", "BVH")) return;

        // Assets 相対パス → フルパス
        string bvhFullPath = AssetToFullPath(bvhAssetPath);
        string fbxFullPath = Path.ChangeExtension(bvhFullPath, ".fbx");
        string fbxAssetPath = Path.ChangeExtension(bvhAssetPath, ".fbx");

        // bvh_to_fbx.py の場所: プロジェクトルート(Assets の 2つ上) に置いてある
        string scriptPath = Path.GetFullPath(
            Path.Combine(Application.dataPath, "..", "..", "bvh_to_fbx.py"));

        if (!File.Exists(scriptPath))
        {
            UnityEngine.Debug.LogError(
                $"[VRM Setup] bvh_to_fbx.py が見つかりません: {scriptPath}\n" +
                $"D:\\Sandbox\\VroidToVRM\\ に bvh_to_fbx.py を置いてください。");
            return;
        }

        // Blender でバックグラウンド変換
        UnityEngine.Debug.Log($"[VRM Setup] BVH 変換開始: {Path.GetFileName(bvhFullPath)}");
        EditorUtility.DisplayProgressBar("BVH → FBX", Path.GetFileName(bvhFullPath), 0.3f);

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = blenderPath,
                Arguments = $"-b --python \"{scriptPath}\" -- \"{bvhFullPath}\" \"{fbxFullPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            using (var proc = Process.Start(psi))
            {
                string stdout = proc.StandardOutput.ReadToEnd();
                string stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                if (proc.ExitCode != 0 || !File.Exists(fbxFullPath))
                {
                    UnityEngine.Debug.LogError($"[VRM Setup] Blender 変換失敗 (ExitCode={proc.ExitCode})\n{stderr}");
                    return;
                }

                if (!string.IsNullOrEmpty(stdout))
                    UnityEngine.Debug.Log($"[Blender] {stdout.Trim()}");
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        // Unity に FBX をインポートさせる
        EditorUtility.DisplayProgressBar("BVH → FBX", "Importing FBX...", 0.8f);
        AssetDatabase.ImportAsset(fbxAssetPath, ImportAssetOptions.ForceUpdate);
        EditorUtility.ClearProgressBar();

        // Humanoid 設定
        ModelImporter importer = AssetImporter.GetAtPath(fbxAssetPath) as ModelImporter;
        if (importer != null && importer.animationType != ModelImporterAnimationType.Human)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            importer.SaveAndReimport();
        }

        AnimationClip clip = FindClipInAsset(fbxAssetPath);
        if (clip == null)
        {
            UnityEngine.Debug.LogError($"[VRM Setup] 変換後 FBX に AnimationClip が見つかりません: {fbxAssetPath}");
            return;
        }

        ApplySetup(fbxAssetPath, clip);
    }

    // ------------------------------------------------------------------ 共通

    private void ApplySetup(string sourcePath, AnimationClip clip)
    {
        string dir = Path.GetDirectoryName(sourcePath).Replace("\\", "/");
        string baseName = Path.GetFileNameWithoutExtension(sourcePath);
        string controllerPath = $"{dir}/{baseName}.controller";

        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
            AssetDatabase.DeleteAsset(controllerPath);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        AnimatorState state = controller.layers[0].stateMachine.AddState(baseName);
        state.motion = clip;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        Animator animator = vrmTarget.GetComponent<Animator>();
        if (animator == null)
            animator = Undo.AddComponent<Animator>(vrmTarget);
        else
            Undo.RecordObject(animator, "VRM Animation Setup");

        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = true;
        animator.updateMode = AnimatorUpdateMode.Fixed;

        EditorUtility.SetDirty(vrmTarget);

        UnityEngine.Debug.Log($"[VRM Setup] 完了  clip={clip.name}  target={vrmTarget.name}");
        EditorUtility.DisplayDialog("セットアップ完了",
            $"Controller: {baseName}.controller\nClip: {clip.name}\nVRM: {vrmTarget.name}", "OK");
    }

    // ------------------------------------------------------------------ ユーティリティ

    private static AnimationClip FindClipInAsset(string assetPath)
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
        {
            if (asset is AnimationClip c && !c.name.StartsWith("__preview__"))
                return c;
        }
        return null;
    }

    private static bool ValidateExtension(string path, string ext, string label)
    {
        if (string.IsNullOrEmpty(path) || !path.EndsWith(ext, System.StringComparison.OrdinalIgnoreCase))
        {
            UnityEngine.Debug.LogError($"[VRM Setup] {label} ファイルを選択してください（{ext}）。");
            return false;
        }
        return true;
    }

    private static string AssetToFullPath(string assetPath)
    {
        return Path.GetFullPath(
            Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length)));
    }

    private static string FindBlender()
    {
        string[] versions = { "5.1", "5.0", "4.4", "4.3", "4.2", "4.1", "4.0", "3.6" };
        foreach (string v in versions)
        {
            string path = $@"C:\Program Files\Blender Foundation\Blender {v}\blender.exe";
            if (File.Exists(path)) return path;
        }
        return null;
    }
}
