#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Cửa sổ Editor Window chuyên dụng điều chỉnh vị trí, kích cỡ, font size của Daily Gem Mine:
/// - Mở từ menu: Tools > PGE > Daily Gem Mine Tuner Window
/// - Cho phép tinh chỉnh trực tiếp cả trong Edit Mode và Play Mode.
/// </summary>
public class DailyGemMineTunerWindow : EditorWindow
{
    private DailyGemMineLayoutTuner tuner;
    private Vector2 scrollPos;

    [MenuItem("Tools/PGE/Daily Gem Mine Tuner Window", false, 30)]
    [MenuItem("Window/PGE/Gem Mine Tuner", false, 30)]
    public static void OpenWindow()
    {
        DailyGemMineTunerWindow window = GetWindow<DailyGemMineTunerWindow>("Gem Mine Tuner");
        window.minSize = new Vector2(340, 600);
        window.Show();
    }

    private void OnEnable()
    {
        FindTunerInScene();
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (tuner == null) return;
    }

    private void FindTunerInScene()
    {
        tuner = Object.FindObjectOfType<DailyGemMineLayoutTuner>(true);

        if (tuner == null)
        {
            DailyGemMineModalController modal = Object.FindObjectOfType<DailyGemMineModalController>(true);
            if (modal != null)
            {
                tuner = modal.gameObject.GetComponent<DailyGemMineLayoutTuner>();
                if (tuner == null)
                {
                    tuner = modal.gameObject.AddComponent<DailyGemMineLayoutTuner>();
                    tuner.AutoFindReferences();
                    tuner.ApplyLayout();
                }
            }
        }
    }

    private void OnGUI()
    {
        if (tuner == null)
        {
            FindTunerInScene();
        }

        if (tuner == null)
        {
            EditorGUILayout.HelpBox("Không tìm thấy DailyGemMineModal trong Scene hiện tại.\nHãy mở Scene MainMenu.unity trước.", MessageType.Warning);
            if (GUILayout.Button("Tìm lại trong Scene", GUILayout.Height(30)))
            {
                FindTunerInScene();
            }
            return;
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleCenter
        };

        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 11,
            normal = { textColor = new Color(0.2f, 0.85f, 1f) }
        };

        EditorGUILayout.Space(6);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("💎 BỘ CHỈNH VỊ TRÍ & KÍCH CỠ GEM MINE", titleStyle);
        EditorGUILayout.LabelField($"Trạng thái: {(Application.isPlaying ? "🎮 Đang chạy (PLAY MODE)" : "✏️ Chỉnh sửa (EDIT MODE)")}", EditorStyles.boldLabel);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        // Nút toggle bật/tắt
        EditorGUILayout.BeginHorizontal();
        bool isModalActive = tuner.gameObject.activeSelf;
        GUI.backgroundColor = isModalActive ? new Color(0.9f, 0.4f, 0.4f) : new Color(0.4f, 0.9f, 0.4f);
        if (GUILayout.Button(isModalActive ? "👁️ Ẩn Modal" : "👁️ Hiện Modal", GUILayout.Height(28)))
        {
            Undo.RecordObject(tuner.gameObject, "Toggle DailyGemMine Modal");
            tuner.gameObject.SetActive(!isModalActive);
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("🔄 Chuẩn Ảnh Mẫu", GUILayout.Height(28)))
        {
            Undo.RecordObject(tuner, "Reset To Reference Preset");
            tuner.ResetToReferencePreset();
            SceneView.RepaintAll();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);

        EditorGUI.BeginChangeCheck();

        // 1. Content Root
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("1. Content Root (Toàn bộ Modal)", headerStyle);
        tuner.contentAnchoredPosition = EditorGUILayout.Vector2Field("Vị trí (X, Y)", tuner.contentAnchoredPosition);
        tuner.contentSizeDelta = EditorGUILayout.Vector2Field("Kích thước (W, H)", tuner.contentSizeDelta);
        tuner.contentScale = EditorGUILayout.Vector3Field("Scale", tuner.contentScale);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(4);

        // 2. Monthly Premium Banner
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("2. Monthly Premium Banner", headerStyle);
        tuner.bannerAnchoredPosition = EditorGUILayout.Vector2Field("Vị trí Banner", tuner.bannerAnchoredPosition);
        tuner.bannerSizeDelta = EditorGUILayout.Vector2Field("Kích thước Banner", tuner.bannerSizeDelta);
        tuner.priceButtonAnchoredPosition = EditorGUILayout.Vector2Field("Vị trí Hitbox Nút 90k", tuner.priceButtonAnchoredPosition);
        tuner.priceButtonSizeDelta = EditorGUILayout.Vector2Field("Kích thước Hitbox", tuner.priceButtonSizeDelta);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(4);

        // 3. Main Panel
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("3. Daily Gem Mine Panel", headerStyle);
        tuner.panelAnchoredPosition = EditorGUILayout.Vector2Field("Vị trí Panel", tuner.panelAnchoredPosition);
        tuner.panelSizeDelta = EditorGUILayout.Vector2Field("Kích thước Panel", tuner.panelSizeDelta);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(4);

        // 4. Texts
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("4. Status Texts", headerStyle);
        tuner.resetTimerPosition = EditorGUILayout.Vector2Field("Vị trí Reset Timer", tuner.resetTimerPosition);
        tuner.resetTimerFontSize = EditorGUILayout.Slider("Cỡ chữ Timer", tuner.resetTimerFontSize, 18f, 60f);

        tuner.entrancePositionFull = EditorGUILayout.Vector2Field("Vị trí Entrance (5 lượt)", tuner.entrancePositionFull);
        tuner.entrancePositionActive = EditorGUILayout.Vector2Field("Vị trí Entrance (< 5 lượt)", tuner.entrancePositionActive);
        tuner.entranceFontSize = EditorGUILayout.Slider("Cỡ chữ Entrance", tuner.entranceFontSize, 18f, 60f);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(4);

        // 5. Cards & Start Button
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("5. Card Level 01 & Nút Start", headerStyle);
        tuner.card1AnchoredPosition = EditorGUILayout.Vector2Field("Vị trí Card 01", tuner.card1AnchoredPosition);
        tuner.card1SizeDelta = EditorGUILayout.Vector2Field("Kích thước Card 01", tuner.card1SizeDelta);

        tuner.startButtonAnchoredPosition = EditorGUILayout.Vector2Field("Vị trí Nút Start", tuner.startButtonAnchoredPosition);
        tuner.startButtonSizeDelta = EditorGUILayout.Vector2Field("Kích thước Nút Start", tuner.startButtonSizeDelta);
        tuner.startButtonFontSize = EditorGUILayout.Slider("Cỡ chữ Start", tuner.startButtonFontSize, 18f, 60f);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Card Level 02", headerStyle);
        tuner.card2AnchoredPosition = EditorGUILayout.Vector2Field("Vị trí Card 02", tuner.card2AnchoredPosition);
        tuner.card2SizeDelta = EditorGUILayout.Vector2Field("Kích thước Card 02", tuner.card2SizeDelta);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(tuner, "Modify Gem Mine Layout");
            tuner.ApplyLayout();
            EditorUtility.SetDirty(tuner);
            SceneView.RepaintAll();
        }

        if (!Application.isPlaying)
        {
            if (GUILayout.Button("💾 Lưu Thay Đổi Vào Scene", GUILayout.Height(30)))
            {
                tuner.ApplyLayout();
                EditorUtility.SetDirty(tuner);
                EditorSceneManager.MarkSceneDirty(tuner.gameObject.scene);
                EditorSceneManager.SaveScene(tuner.gameObject.scene);
                Debug.Log("[DailyGemMineTunerWindow] Đã lưu vị trí và kích cỡ vào Scene!");
            }
        }

        EditorGUILayout.EndScrollView();
    }
}
#endif
