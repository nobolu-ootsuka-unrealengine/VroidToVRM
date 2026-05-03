using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public class MaterialToToonShader : EditorWindow
{
    private const string TargetShaderName = "VRM/MToon";

    [MenuItem("Tools/VRM - Shader Reapplier")]
    public static void ShowWindow()
    {
        GetWindow<MaterialToToonShader>("MToon Reapplier");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Step 1: URP を無効化", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "VRM/MToon は Built-in RP 専用です。\n" +
            "URP が有効なままだとシェーダーを当てても必ずピンクになります。\n" +
            "まずこのボタンで全品質レベルの URP を解除してください。",
            MessageType.Warning);

        if (GUILayout.Button("全品質レベルの URP を無効化 (Built-in RP へ切替)", GUILayout.Height(36)))
        {
            DisableURPAllLevels();
        }

        EditorGUILayout.Space(12);

        EditorGUILayout.LabelField("Step 2: マテリアル一括置換", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Project ウィンドウでフォルダを選択してから実行してください。\n" +
            "選択フォルダ以下の全マテリアルを " + TargetShaderName + " に強制置換します。",
            MessageType.Info);

        if (GUILayout.Button("強制置換・修復を実行", GUILayout.Height(36)))
        {
            FixAndReplaceShader();
        }
    }

    [MenuItem("Tools/VRM - Disable URP (All Quality Levels)")]
    private static void DisableURPAllLevels()
    {
        int saved = QualitySettings.GetQualityLevel();
        int count = QualitySettings.names.Length;

        for (int i = 0; i < count; i++)
        {
            QualitySettings.SetQualityLevel(i, applyExpensiveChanges: false);
            QualitySettings.renderPipeline = null;
        }

        QualitySettings.SetQualityLevel(saved, applyExpensiveChanges: false);

        // グローバル設定も念のためクリア
        GraphicsSettings.defaultRenderPipeline = null;

        UnityEngine.Debug.Log($"完了: {count} 個の品質レベルすべての Render Pipeline を None に設定しました。\nUnity を再起動してシーンを確認してください。");
        EditorUtility.DisplayDialog("URP 無効化完了",
            $"{count} 個の品質レベルの Render Pipeline を None に設定しました。\n\nUnity を再起動してください。",
            "OK");
    }

    [MenuItem("Tools/VRM - Shader Reapplier (Selected Folder)")]
    private static void FixAndReplaceShader()
    {
        Shader newShader = Shader.Find(TargetShaderName);
        if (newShader == null)
        {
            UnityEngine.Debug.LogError(
                $"Shader '{TargetShaderName}' が見つかりません。\n" +
                "Packages/com.vrmc.univrm が正しくインポートされているか確認してください。");
            return;
        }

        UnityEngine.Object[] selected = Selection.GetFiltered(
            typeof(Material),
            SelectionMode.DeepAssets | SelectionMode.Editable);

        if (selected.Length == 0)
        {
            UnityEngine.Debug.LogWarning("Project ウィンドウでフォルダまたはマテリアルを選択してから実行してください。");
            return;
        }

        int count = 0;
        foreach (UnityEngine.Object obj in selected)
        {
            Material mat = obj as Material;
            if (mat == null) continue;

            Undo.RecordObject(mat, "Fix Shader to MToon");
            mat.shader = newShader;
            EditorUtility.SetDirty(mat);
            count++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        UnityEngine.Debug.Log($"完了: {count} 個のマテリアルを {TargetShaderName} に置換しました。");
    }
}
