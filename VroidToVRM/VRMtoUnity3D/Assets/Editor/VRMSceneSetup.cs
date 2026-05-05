using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class VRMSceneSetup : EditorWindow
{
    // --- セットアップ ---
    private GameObject vrmPrefab;

    // --- カメラ基本設定 ---
    private Vector3 camPos = new Vector3(0f, 2.4f, 2.2f);
    private Vector3 camRot = new Vector3(33.6f, 180f, 0f);
    private float camFOV = 50f;

    // --- 軌道カメラワーク ---
    private float orbitAngleY = 0f;               // 水平角 0-360
    private float orbitHeight = 0f;               // 高さ (注目点からのオフセット Y)
    private float orbitDist  = 2.35f;             // 距離
    private Vector3 orbitTarget = new Vector3(0f, 0.8f, 0f);

    // --- シーン内オブジェクト参照 ---
    private Camera   cam;
    private bool     sceneReady;

    // ビューポートプレビュー用スクロール
    private Vector2 scroll;

    [MenuItem("Tools/VRM - Scene Setup")]
    public static void ShowWindow() =>
        GetWindow<VRMSceneSetup>("VRM Scene Setup");

    private void OnGUI()
    {
        // 参照が切れていたら再取得
        if (cam == null)
        {
            cam = FindFirstObjectByType<Camera>();
            sceneReady = cam != null;
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);

        DrawSetupSection();
        EditorGUILayout.Space(10);
        DrawCameraSection();
        EditorGUILayout.Space(10);
        DrawOrbitSection();

        EditorGUILayout.EndScrollView();
    }

    // ================================================================
    //  セットアップ
    // ================================================================
    private void DrawSetupSection()
    {
        EditorGUILayout.LabelField("▼ シーンセットアップ", EditorStyles.boldLabel);
        vrmPrefab = (GameObject)EditorGUILayout.ObjectField(
            "VRM Prefab", vrmPrefab, typeof(GameObject), false);

        if (GUILayout.Button("新規シーンを作成してセットアップ", GUILayout.Height(36)))
            CreateScene();
    }

    private void CreateScene()
    {
        var scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // VRM を 0,0,0 に配置
        if (vrmPrefab != null)
        {
            var vrm = (GameObject)PrefabUtility.InstantiatePrefab(vrmPrefab);
            vrm.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            vrm.name = vrmPrefab.name;
        }

        // カメラ
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        cam = camGO.AddComponent<Camera>();
        camGO.AddComponent<AudioListener>();
        ApplyCameraTransform();
        cam.fieldOfView = camFOV;
        cam.nearClipPlane = 0.01f;

        // 床: Cube を (0.5, -0.5, 0.5) スケール (10, 1, 10)
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.SetPositionAndRotation(
            new Vector3(0.5f, -0.5f, 0.5f), Quaternion.identity);
        floor.transform.localScale = new Vector3(10f, 1f, 10f);

        // ディレクショナルライト
        var lightGO = new GameObject("Directional Light");
        var dl = lightGO.AddComponent<Light>();
        dl.type = LightType.Directional;
        dl.intensity = 1f;
        lightGO.transform.eulerAngles = new Vector3(50f, -30f, 0f);

        sceneReady = true;
        EditorSceneManager.MarkSceneDirty(scene);
        UnityEngine.Debug.Log("[VRM Scene Setup] シーン作成完了");
    }

    // ================================================================
    //  カメラ設定（直接操作）
    // ================================================================
    private void DrawCameraSection()
    {
        EditorGUILayout.LabelField("▼ カメラ設定", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledGroupScope(!sceneReady))
        {
            EditorGUI.BeginChangeCheck();

            camPos = EditorGUILayout.Vector3Field("Position", camPos);
            camRot = EditorGUILayout.Vector3Field("Rotation", camRot);
            camFOV = EditorGUILayout.Slider("Field of View", camFOV, 5f, 120f);

            if (EditorGUI.EndChangeCheck())
                ApplyCameraTransform();

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("正面 (0°)"))   SetCameraAngle(0f);
            if (GUILayout.Button("左 (90°)"))    SetCameraAngle(90f);
            if (GUILayout.Button("背面 (180°)")) SetCameraAngle(180f);
            if (GUILayout.Button("右 (270°)"))   SetCameraAngle(270f);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("VRM を正面に向ける（リセット）"))
                ResetCamera();
        }
    }

    // ================================================================
    //  軌道カメラワーク
    // ================================================================
    private void DrawOrbitSection()
    {
        EditorGUILayout.LabelField("▼ カメラワーク（オービット）", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledGroupScope(!sceneReady))
        {
            orbitTarget = EditorGUILayout.Vector3Field("注目点", orbitTarget);

            EditorGUI.BeginChangeCheck();
            orbitAngleY = EditorGUILayout.Slider("水平角 (Y)", orbitAngleY, 0f, 360f);
            orbitHeight = EditorGUILayout.Slider("高さ", orbitHeight, -1f, 3f);
            orbitDist   = EditorGUILayout.Slider("距離", orbitDist, 0.5f, 10f);
            if (EditorGUI.EndChangeCheck())
                ApplyOrbit();

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("← 15°")) { orbitAngleY = (orbitAngleY - 15f + 360f) % 360f; ApplyOrbit(); }
            if (GUILayout.Button("→ 15°")) { orbitAngleY = (orbitAngleY + 15f) % 360f;         ApplyOrbit(); }
            EditorGUILayout.EndHorizontal();
        }
    }

    // ================================================================
    //  内部ヘルパー
    // ================================================================
    private void ApplyCameraTransform()
    {
        if (cam == null) return;
        cam.transform.position   = camPos;
        cam.transform.eulerAngles = camRot;
        cam.fieldOfView = camFOV;
    }

    private void ApplyOrbit()
    {
        if (cam == null) return;

        float rad = orbitAngleY * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            Mathf.Sin(rad) * orbitDist,
            orbitHeight,
            Mathf.Cos(rad) * orbitDist);

        camPos = orbitTarget + offset;
        camRot = Quaternion.LookRotation(orbitTarget - camPos, Vector3.up).eulerAngles;

        cam.transform.position    = camPos;
        cam.transform.eulerAngles = camRot;
        Repaint();
    }

    private void SetCameraAngle(float angleY)
    {
        orbitAngleY = angleY;
        ApplyOrbit();
    }

    private void ResetCamera()
    {
        orbitAngleY = 0f;
        orbitHeight = 0f;
        orbitDist   = 2.35f;
        orbitTarget = new Vector3(0f, 0.8f, 0f);
        camPos      = new Vector3(0f, 0.8f, 2.35f);
        camRot      = new Vector3(0f, 180f, 0f);
        camFOV      = 50f;

        if (cam != null)
        {
            cam.transform.position    = camPos;
            cam.transform.eulerAngles = camRot;
            cam.fieldOfView           = camFOV;
        }
        Repaint();
    }
}
