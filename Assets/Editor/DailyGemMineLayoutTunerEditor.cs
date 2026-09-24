#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Custom Inspector Editor cho DailyGemMineLayoutTuner:
/// Cung cấp giao diện trực quan với thanh trượt để điều chỉnh vị trí, kích thước, font size
/// và xem trước ngay lập tức trong Scene View & Game View (đồng nhất giữa Edit Mode và Play Mode).
/// </summary>
[CustomEditor(typeof(DailyGemMineLayoutTuner))]
public class DailyGemMineLayoutTunerEditor : Editor
{
    private DailyGemMineLayoutTuner tuner;

    private static bool foldContent = true;
    private static bool foldBanner = true;
    private static bool foldPanel = true;
    private static bool foldTexts = true;
    private static bool foldCards = true;

    private void OnEnable()
    {
        tuner = (DailyGemMineLayoutTuner)target;
        if (tuner != null)
        {
            tuner.AutoFindReferences();
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (tuner == null) return;

        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };

        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12,
            normal = { textColor = new Color(0.2f, 0.85f, 1f) }
        };

        EditorGUILayout.Space(6);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("💎 DAILY GEM MINE - BỘ ĐIỀU CHỈNH VỊ TRÍ & KÍCH CỠ", titleStyle);
        EditorGUILayout.LabelField("Điều chỉnh trực tiếp thông số vị trí, kích thước, tỉ lệ hoạt động đồng bộ cả Edit Mode & Play Mode.", EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        // Nút Quick Actions
        EditorGUILayout.BeginHorizontal();
        bool isModalActive = tuner.gameObject.activeSelf;
        GUI.backgroundColor = isModalActive ? new Color(0.9f, 0.4f, 0.4f) : new Color(0.4f, 0.9f, 0.4f);
        if (GUILayout.Button(isModalActive ? "👁️ Ẩn Modal" : "👁️ Bật Modal Xem Trước", GUILayout.Height(28)))
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
        foldContent = EditorGUILayout.BeginFoldoutHeaderGroup(foldContent, "1. Content Root (Khung bao ngoài)");
        if (foldContent)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            tuner.contentAnchoredPosition = EditorGUILayout.Vector2Field("Vị trí (X, Y)", tuner.contentAnchoredPosition);
            tuner.contentSizeDelta = EditorGUILayout.Vector2Field("Kích thước (Rộng, Cao)", tuner.contentSizeDelta);
            tuner.contentScale = EditorGUILayout.Vector3Field("Tỉ lệ Scale", tuner.contentScale);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(4);

        // 2. Banner & Price Button
        foldBanner = EditorGUILayout.BeginFoldoutHeaderGroup(foldBanner, "2. Monthly Premium Banner & Nút 90k");
        if (foldBanner)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Monthly Premium Banner", headerStyle);
            tuner.bannerAnchoredPosition = EditorGUILayout.Vector2Field("Vị trí Banner (X, Y)", tuner.bannerAnchoredPosition);
            tuner.bannerSizeDelta = EditorGUILayout.Vector2Field("Kích thước Banner", tuner.bannerSizeDelta);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Hitbox Nút Giá 90.000 đ", headerStyle);
            tuner.priceButtonAnchoredPosition = EditorGUILayout.Vector2Field("Vị trí Hitbox (X, Y)", tuner.priceButtonAnchoredPosition);
            tuner.priceButtonSizeDelta = EditorGUILayout.Vector2Field("Kích thước Hitbox", tuner.priceButtonSizeDelta);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(4);

        // 3. Daily Gem Mine Main Panel
        foldPanel = EditorGUILayout.BeginFoldoutHeaderGroup(foldPanel, "3. Daily Gem Mine Main Panel");
        if (foldPanel)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            tuner.panelAnchoredPosition = EditorGUILayout.Vector2Field("Vị trí Panel (X, Y)", tuner.panelAnchoredPosition);
            tuner.panelSizeDelta = EditorGUILayout.Vector2Field("Kích thước Panel", tuner.panelSizeDelta);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(4);

        // 4. Status Texts
        foldTexts = EditorGUILayout.BeginFoldoutHeaderGroup(foldTexts, "4. Chữ Reset Timer & Entrance Count");
        if (foldTexts)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Dòng chữ: Reset in: ...", headerStyle);
            tuner.resetTimerPosition = EditorGUILayout.Vector2Field("Vị trí Reset Timer", tuner.resetTimerPosition);
            tuner.resetTimerSize = EditorGUILayout.Vector2Field("Kích thước khung", tuner.resetTimerSize);
            tuner.resetTimerFontSize = EditorGUILayout.Slider("Cỡ chữ Timer", tuner.resetTimerFontSize, 18f, 60f);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Dòng chữ: Entrance: ...", headerStyle);
            tuner.entrancePositionFull = EditorGUILayout.Vector2Field("Vị trí khi đủ 5 lượt (giữa)", tuner.entrancePositionFull);
            tuner.entrancePositionActive = EditorGUILayout.Vector2Field("Vị trí khi < 5 lượt (dưới)", tuner.entrancePositionActive);
            tuner.entranceSize = EditorGUILayout.Vector2Field("Kích thước khung", tuner.entranceSize);
            tuner.entranceFontSize = EditorGUILayout.Slider("Cỡ chữ Entrance", tuner.entranceFontSize, 18f, 60f);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(4);

        // 5. Levels ScrollView & Cards
        foldCards = EditorGUILayout.BeginFoldoutHeaderGroup(foldCards, "5. Khung cuộn 5 Level & Nút Start");
        if (foldCards)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Levels ScrollView Container", headerStyle);
            tuner.scrollViewAnchoredPosition = EditorGUILayout.Vector2Field("Vị trí ScrollView (X, Y)", tuner.scrollViewAnchoredPosition);
            tuner.scrollViewSizeDelta = EditorGUILayout.Vector2Field("Kích thước ScrollView", tuner.scrollViewSizeDelta);
            tuner.cardSpacing = EditorGUILayout.Slider("Khoảng cách giữa các Card", tuner.cardSpacing, 0f, 60f);
            tuner.cardSizeDelta = EditorGUILayout.Vector2Field("Kích thước mỗi Card (W, H)", tuner.cardSizeDelta);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Nút Start (Hồng) trên các Card", headerStyle);
            tuner.startButtonAnchoredPosition = EditorGUILayout.Vector2Field("Vị trí nút Start", tuner.startButtonAnchoredPosition);
            tuner.startButtonSizeDelta = EditorGUILayout.Vector2Field("Kích thước nút Start", tuner.startButtonSizeDelta);
            tuner.startButtonFontSize = EditorGUILayout.Slider("Cỡ chữ nút Start", tuner.startButtonFontSize, 18f, 60f);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Header & Phần Thưởng trên Card", headerStyle);
            tuner.headerHeight = EditorGUILayout.Slider("Chiều cao Header", tuner.headerHeight, 40f, 100f);
            tuner.titleFontSize = EditorGUILayout.Slider("Cỡ chữ Tiêu đề", tuner.titleFontSize, 18f, 60f);
            tuner.titleAnchoredPosition = EditorGUILayout.Vector2Field("Vị trí Tiêu đề (X, Y)", tuner.titleAnchoredPosition);
            tuner.titleSizeDelta = EditorGUILayout.Vector2Field("Kích thước Tiêu đề", tuner.titleSizeDelta);

            EditorGUILayout.Space(2);
            tuner.gemIconAnchoredPosition = EditorGUILayout.Vector2Field("Vị trí Icon Gem", tuner.gemIconAnchoredPosition);
            tuner.gemIconSizeDelta = EditorGUILayout.Vector2Field("Kích thước Icon Gem", tuner.gemIconSizeDelta);

            EditorGUILayout.Space(2);
            tuner.rewardFontSize = EditorGUILayout.Slider("Cỡ chữ Thưởng", tuner.rewardFontSize, 18f, 60f);
            tuner.rewardAnchoredPosition = EditorGUILayout.Vector2Field("Vị trí Text Thưởng", tuner.rewardAnchoredPosition);
            tuner.rewardSizeDelta = EditorGUILayout.Vector2Field("Kích thước Text Thưởng", tuner.rewardSizeDelta);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(6);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(tuner, "Modify DailyGemMine Layout");
            tuner.ApplyLayout();
            EditorUtility.SetDirty(tuner);
            SceneView.RepaintAll();
        }

        // Lưu Scene
        if (!Application.isPlaying)
        {
            EditorGUILayout.Space(4);
            if (GUILayout.Button("💾 Lưu Thay Đổi Vào Scene (Save Scene)", GUILayout.Height(32)))
            {
                tuner.ApplyLayout();
                EditorUtility.SetDirty(tuner);
                EditorSceneManager.MarkSceneDirty(tuner.gameObject.scene);
                EditorSceneManager.SaveScene(tuner.gameObject.scene);
                Debug.Log("[DailyGemMineLayoutTuner] Đã lưu thành công layout vào Scene!");
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
