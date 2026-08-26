#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Editor Builder tự động xây dựng giao diện:
/// 1. Nút "PityInfoButton" (icon Baohiem_0) cạnh nút UPGRADE.
/// 2. Modal "PityGuaranteePanel" với 3 hàng tiến trình ELITE, EPIC, LEGEND.
/// 3. Kết nối đầy đủ 100% SerializedProperty giữa LabUpgradeController và PityGuaranteePanel.
/// 4. Tạo Prefab Assets/Prefabs/UI/PityGuaranteePanel.prefab.
/// 
/// Menu: PGE > UI > Build Pity Guarantee UI
/// </summary>
[InitializeOnLoad]
public static class PityUIBuilder
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string BaohiemSpritePath = "Assets/Sprites/UI/Baohiem.png";
    private const string NunitoFontPath = "Assets/Fonts/Nunito/Nunito SDF.asset";
    private const string NunitoStrokeMatPath = "Assets/Fonts/Nunito/Nunito SDF - Stroke.mat";
    private const string PrefabDir = "Assets/Prefabs/UI";
    private const string PrefabPath = "Assets/Prefabs/UI/PityGuaranteePanel.prefab";
    private const string BuildRequestPath = "Assets/Editor/PGE_PityUI_BuildRequest.txt";

    static PityUIBuilder()
    {
        EditorApplication.update += TryBuildRequestedUI;
    }

    private static void TryBuildRequestedUI()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            return;
        }

        if (!File.Exists(BuildRequestPath))
        {
            return;
        }

        try
        {
            File.Delete(BuildRequestPath);
        }
        catch { }

        BuildPityUI();
    }

    // Visual Palette
    private static readonly Color DimColor = new Color32(4, 12, 18, 200);
    private static readonly Color WindowBgColor = new Color32(14, 38, 52, 255);
    private static readonly Color WindowBorderColor = new Color32(64, 218, 210, 255); // Cyan
    private static readonly Color RowBgColor = new Color32(20, 52, 70, 255);
    private static readonly Color RowBorderColor = new Color32(38, 88, 112, 255);
    private static readonly Color SliderBgColor = new Color32(8, 22, 30, 255);

    private static readonly Color EliteColor = new Color32(56, 130, 229, 255);
    private static readonly Color EpicColor = new Color32(168, 85, 247, 255);
    private static readonly Color LegendColor = new Color32(251, 191, 36, 255);

    private static TMP_FontAsset nunitoFont;
    private static Material nunitoStrokeMat;
    private static Sprite baohiemSprite;

    [MenuItem("PGE/UI/Build Pity Guarantee UI")]
    public static void BuildPityUIFromMenu()
    {
        BuildPityUI();
    }

    public static void BuildPityUI()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogWarning("[PityUIBuilder] Hãy dừng Play Mode trước khi build UI.");
            return;
        }

        // 1. Nạp Assets
        LoadAssets();

        // 2. Mở Scene MainMenu
        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.path, ScenePath, StringComparison.OrdinalIgnoreCase))
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        if (!scene.IsValid())
        {
            Debug.LogError($"[PityUIBuilder] Không thể mở scene tại {ScenePath}");
            return;
        }

        // 3. Tìm LabPanel & LabUpgradeController
        LabUpgradeController labController = UnityEngine.Object.FindFirstObjectByType<LabUpgradeController>();
        if (labController == null)
        {
            Debug.LogError("[PityUIBuilder] Không tìm thấy LabUpgradeController trong scene MainMenu!");
            return;
        }

        Transform labPanelTransform = labController.transform; // LabPanel
        Transform statsPanelTransform = labPanelTransform.Find("StatsPanel");
        if (statsPanelTransform == null)
        {
            statsPanelTransform = labPanelTransform;
        }

        // 4. Tạo hoặc cập nhật PityInfoButton
        Button pityInfoBtn = BuildPityInfoButton(statsPanelTransform);

        // 5. Tạo hoặc cập nhật PityGuaranteePanel
        PityGuaranteePanel pityPanel = BuildPityGuaranteePanel(labPanelTransform, labController);

        // 6. Liên kết với LabUpgradeController
        SerializedObject labSo = new SerializedObject(labController);
        SerializedProperty pityInfoBtnProp = labSo.FindProperty("pityInfoButton");
        SerializedProperty pityPanelProp = labSo.FindProperty("pityGuaranteePanel");

        if (pityInfoBtnProp != null) pityInfoBtnProp.objectReferenceValue = pityInfoBtn;
        if (pityPanelProp != null) pityPanelProp.objectReferenceValue = pityPanel;
        labSo.ApplyModifiedProperties();

        // 7. Đảm bảo cả Scene lẫn Prefab đều mặc định ẩn.
        pityPanel.gameObject.SetActive(false);

        // 8. Tạo Prefab cho PityGuaranteePanel sau khi đã đồng bộ trạng thái ban đầu.
        SaveAsPrefab(pityPanel.gameObject);

        // 9. Lưu Scene
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[PityUIBuilder] ✅ Đã xây dựng thành công giao diện PityInfoButton & PityGuaranteePanel trong MainMenu.unity!");
    }

    private static void LoadAssets()
    {
        nunitoFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NunitoFontPath);
        if (nunitoFont == null)
        {
            nunitoFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        }

        nunitoStrokeMat = AssetDatabase.LoadAssetAtPath<Material>(NunitoStrokeMatPath);

        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(BaohiemSpritePath).OfType<Sprite>().ToArray();
        if (sprites != null && sprites.Length > 0)
        {
            baohiemSprite = Array.Find(sprites, s => s.name == "Baohiem_0") ?? sprites[0];
        }
    }

    private static Button BuildPityInfoButton(Transform parent)
    {
        Transform existing = parent.Find("PityInfoButton");
        GameObject btnGo;
        if (existing != null)
        {
            btnGo = existing.gameObject;
        }
        else
        {
            btnGo = new GameObject("PityInfoButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);
        }

        RectTransform rect = btnGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(295f, 122f); // Cạnh bên phải của UpgradeButton (UpgradeButton x: 0, w: 470, y: 40..205)
        rect.sizeDelta = new Vector2(72f, 72f);
        rect.localScale = Vector3.one;

        Image img = btnGo.GetComponent<Image>();
        img.sprite = baohiemSprite;
        img.preserveAspect = true;
        img.color = Color.white;
        img.raycastTarget = true;

        Button btn = btnGo.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;

        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color32(230, 255, 255, 255);
        cb.pressedColor = new Color32(180, 235, 235, 255);
        cb.selectedColor = Color.white;
        cb.fadeDuration = 0.1f;
        btn.colors = cb;

        // Gắn thêm Shadow nhẹ cho nút nổi bật
        Shadow shadow = btnGo.GetComponent<Shadow>();
        if (shadow == null) shadow = btnGo.AddComponent<Shadow>();
        shadow.effectColor = new Color32(0, 10, 20, 180);
        shadow.effectDistance = new Vector2(2f, -3f);

        btnGo.SetActive(true);
        return btn;
    }

    private static PityGuaranteePanel BuildPityGuaranteePanel(Transform parent, LabUpgradeController labController)
    {
        Transform existing = parent.Find("PityGuaranteePanel");
        GameObject panelGo;
        if (existing != null)
        {
            panelGo = existing.gameObject;
        }
        else
        {
            panelGo = new GameObject("PityGuaranteePanel", typeof(RectTransform), typeof(CanvasGroup), typeof(PityGuaranteePanel));
            panelGo.transform.SetParent(parent, false);
        }

        panelGo.layer = 5; // UI Layer
        RectTransform rootRect = panelGo.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = Vector2.zero;
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.localScale = Vector3.one;

        CanvasGroup rootCg = panelGo.GetComponent<CanvasGroup>();
        rootCg.alpha = 1f;

        PityGuaranteePanel pityPanel = panelGo.GetComponent<PityGuaranteePanel>();

        // 1. DimBackground
        Transform dimTrans = panelGo.transform.Find("DimBackground");
        GameObject dimGo = dimTrans != null ? dimTrans.gameObject : new GameObject("DimBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(CanvasGroup));
        dimGo.transform.SetParent(panelGo.transform, false);
        dimGo.layer = 5;

        RectTransform dimRect = dimGo.GetComponent<RectTransform>();
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.sizeDelta = Vector2.zero;
        dimRect.anchoredPosition = Vector2.zero;
        dimRect.pivot = new Vector2(0.5f, 0.5f);

        Image dimImg = dimGo.GetComponent<Image>();
        dimImg.color = DimColor;
        dimImg.raycastTarget = true;

        Button dimBtn = dimGo.GetComponent<Button>();
        dimBtn.targetGraphic = dimImg;

        CanvasGroup dimCg = dimGo.GetComponent<CanvasGroup>();

        // 2. WindowContainer
        Transform winTrans = panelGo.transform.Find("WindowContainer");
        GameObject winGo = winTrans != null ? winTrans.gameObject : new GameObject("WindowContainer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(CanvasGroup));
        winGo.transform.SetParent(panelGo.transform, false);
        winGo.layer = 5;

        RectTransform winRect = winGo.GetComponent<RectTransform>();
        winRect.anchorMin = new Vector2(0.5f, 0.5f);
        winRect.anchorMax = new Vector2(0.5f, 0.5f);
        winRect.pivot = new Vector2(0.5f, 0.5f);
        winRect.anchoredPosition = new Vector2(0f, 0f);
        winRect.sizeDelta = new Vector2(620f, 660f);
        winRect.localScale = Vector3.one;

        Image winImg = winGo.GetComponent<Image>();
        winImg.color = WindowBgColor;
        winImg.raycastTarget = true;

        Outline winOutline = winGo.GetComponent<Outline>();
        winOutline.effectColor = WindowBorderColor;
        winOutline.effectDistance = new Vector2(3f, -3f);

        CanvasGroup winCg = winGo.GetComponent<CanvasGroup>();

        // 3. Header: Title, Subtitle, CloseButton
        TMP_Text titleTxt = CreateText(winGo.transform, "TitleText", "BẢO HIỂM LƯỢT ROLL", 30, FontStyles.Bold, new Color32(80, 240, 230, 255), TextAlignmentOptions.Center, new Vector2(0f, 275f), new Vector2(540f, 45f));
        TMP_Text subTxt = CreateText(winGo.transform, "SubtitleText", "Tiến độ tích lũy bảo hiểm chỉ số theo từng bậc", 17, FontStyles.Normal, new Color32(160, 215, 225, 255), TextAlignmentOptions.Center, new Vector2(0f, 238f), new Vector2(540f, 30f));

        Button closeBtn = CreateCloseButton(winGo.transform, new Vector2(265f, 280f));

        // 4. Header Divider
        CreateDivider(winGo.transform, "Divider", new Vector2(0f, 215f), 550f);

        // 5. 3 Pity Rows: ELITE, EPIC, LEGEND
        PityProgressRow eliteRow = CreatePityRow(winGo.transform, "EliteRow", "ELITE", EliteColor, new Vector2(0f, 135f));
        PityProgressRow epicRow = CreatePityRow(winGo.transform, "EpicRow", "EPIC", EpicColor, new Vector2(0f, 20f));
        PityProgressRow legendRow = CreatePityRow(winGo.transform, "LegendRow", "LEGEND", LegendColor, new Vector2(0f, -95f));

        // 6. Bottom Description Box
        Transform descBoxTrans = winGo.transform.Find("DescriptionBox");
        GameObject descBoxGo = descBoxTrans != null ? descBoxTrans.gameObject : new GameObject("DescriptionBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        descBoxGo.transform.SetParent(winGo.transform, false);
        descBoxGo.layer = 5;

        RectTransform descBoxRect = descBoxGo.GetComponent<RectTransform>();
        descBoxRect.anchorMin = new Vector2(0.5f, 0.5f);
        descBoxRect.anchorMax = new Vector2(0.5f, 0.5f);
        descBoxRect.pivot = new Vector2(0.5f, 0.5f);
        descBoxRect.anchoredPosition = new Vector2(0f, -235f);
        descBoxRect.sizeDelta = new Vector2(550f, 90f);

        Image descBoxImg = descBoxGo.GetComponent<Image>();
        descBoxImg.color = new Color32(10, 28, 38, 200);

        TMP_Text descTxt = CreateText(descBoxGo.transform, "DescriptionText",
            "• Quay trúng bậc nào sẽ chỉ đặt lại bộ đếm bảo hiểm của bậc đó về 0.\n• Các bậc còn lại tiếp tục tích lũy độc lập và không bị ảnh hưởng!\n• Khi đạt mốc bảo hiểm, lượt quay tiếp theo chắc chắn nhận được bậc đó!",
            16, FontStyles.Normal, new Color32(140, 195, 205, 255), TextAlignmentOptions.Center, Vector2.zero, new Vector2(530f, 80f));
        descTxt.enableWordWrapping = true;

        // 7. Wire SerializedProperties on PityGuaranteePanel
        SerializedObject panelSo = new SerializedObject(pityPanel);
        panelSo.FindProperty("labUpgradeController").objectReferenceValue = labController;
        panelSo.FindProperty("windowRect").objectReferenceValue = winRect;
        panelSo.FindProperty("windowCanvasGroup").objectReferenceValue = winCg;
        panelSo.FindProperty("dimCanvasGroup").objectReferenceValue = dimCg;
        panelSo.FindProperty("closeButton").objectReferenceValue = closeBtn;
        panelSo.FindProperty("dimBackgroundButton").objectReferenceValue = dimBtn;
        panelSo.FindProperty("eliteRow").objectReferenceValue = eliteRow;
        panelSo.FindProperty("epicRow").objectReferenceValue = epicRow;
        panelSo.FindProperty("legendRow").objectReferenceValue = legendRow;
        panelSo.FindProperty("titleText").objectReferenceValue = titleTxt;
        panelSo.FindProperty("descriptionText").objectReferenceValue = descTxt;
        panelSo.ApplyModifiedProperties();

        return pityPanel;
    }

    private static PityProgressRow CreatePityRow(Transform parent, string rowName, string tierName, Color tierColor, Vector2 anchoredPos)
    {
        Transform existing = parent.Find(rowName);
        GameObject rowGo = existing != null ? existing.gameObject : new GameObject(rowName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(PityProgressRow));
        rowGo.transform.SetParent(parent, false);
        rowGo.layer = 5;

        RectTransform rowRect = rowGo.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 0.5f);
        rowRect.anchorMax = new Vector2(0.5f, 0.5f);
        rowRect.pivot = new Vector2(0.5f, 0.5f);
        rowRect.anchoredPosition = anchoredPos;
        rowRect.sizeDelta = new Vector2(550f, 100f);
        rowRect.localScale = Vector3.one;

        Image rowImg = rowGo.GetComponent<Image>();
        rowImg.color = RowBgColor;

        Outline rowOutline = rowGo.GetComponent<Outline>();
        rowOutline.effectColor = RowBorderColor;
        rowOutline.effectDistance = new Vector2(1.5f, -1.5f);

        // 1. Badge indicator (khối màu đứng bên trái)
        Transform badgeTrans = rowGo.transform.Find("BadgeIndicator");
        GameObject badgeGo = badgeTrans != null ? badgeTrans.gameObject : new GameObject("BadgeIndicator", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        badgeGo.transform.SetParent(rowGo.transform, false);
        badgeGo.layer = 5;

        RectTransform badgeRect = badgeGo.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0f, 0.5f);
        badgeRect.anchorMax = new Vector2(0f, 0.5f);
        badgeRect.pivot = new Vector2(0f, 0.5f);
        badgeRect.anchoredPosition = new Vector2(15f, 18f);
        badgeRect.sizeDelta = new Vector2(8f, 26f);

        Image badgeImg = badgeGo.GetComponent<Image>();
        badgeImg.color = tierColor;

        // 2. Counter Text: "ELITE — 3 / 5"
        TMP_Text counterTxt = CreateText(rowGo.transform, "CounterText", $"{tierName} — 0 / 10", 21, FontStyles.Bold, Color.white, TextAlignmentOptions.Left, new Vector2(-60f, 18f), new Vector2(300f, 30f));
        RectTransform counterRect = counterTxt.GetComponent<RectTransform>();
        counterRect.anchorMin = new Vector2(0f, 0.5f);
        counterRect.anchorMax = new Vector2(0f, 0.5f);
        counterRect.pivot = new Vector2(0f, 0.5f);
        counterRect.anchoredPosition = new Vector2(32f, 18f);

        // 3. Remaining Text: "Còn 2 lượt"
        TMP_Text remainingTxt = CreateText(rowGo.transform, "RemainingText", "Còn 10 lượt", 18, FontStyles.Bold, tierColor, TextAlignmentOptions.Right, Vector2.zero, new Vector2(200f, 30f));
        RectTransform remRect = remainingTxt.GetComponent<RectTransform>();
        remRect.anchorMin = new Vector2(1f, 0.5f);
        remRect.anchorMax = new Vector2(1f, 0.5f);
        remRect.pivot = new Vector2(1f, 0.5f);
        remRect.anchoredPosition = new Vector2(-15f, 18f);

        // 4. Progress Slider
        Slider slider = CreateProgressBar(rowGo.transform, "ProgressBar", new Vector2(0f, -20f), new Vector2(518f, 20f), tierColor);
        Image fillImg = slider.fillRect != null ? slider.fillRect.GetComponent<Image>() : null;

        // Wire PityProgressRow SerializedProperties
        PityProgressRow rowComp = rowGo.GetComponent<PityProgressRow>();
        SerializedObject rowSo = new SerializedObject(rowComp);
        rowSo.FindProperty("tierNameText").objectReferenceValue = null;
        rowSo.FindProperty("counterText").objectReferenceValue = counterTxt;
        rowSo.FindProperty("remainingText").objectReferenceValue = remainingTxt;
        rowSo.FindProperty("progressBarSlider").objectReferenceValue = slider;
        rowSo.FindProperty("progressBarFillImage").objectReferenceValue = fillImg;
        rowSo.FindProperty("tierBadgeImage").objectReferenceValue = badgeImg;
        rowSo.ApplyModifiedProperties();

        return rowComp;
    }

    private static Slider CreateProgressBar(Transform parent, string name, Vector2 pos, Vector2 size, Color fillColor)
    {
        Transform existing = parent.Find(name);
        GameObject sliderGo = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Slider));
        sliderGo.transform.SetParent(parent, false);
        sliderGo.layer = 5;

        RectTransform sliderRect = sliderGo.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.anchoredPosition = pos;
        sliderRect.sizeDelta = size;

        Slider slider = sliderGo.GetComponent<Slider>();
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
        slider.direction = Slider.Direction.LeftToRight;

        // Background
        Transform bgTrans = sliderGo.transform.Find("Background");
        GameObject bgGo = bgTrans != null ? bgTrans.gameObject : new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bgGo.transform.SetParent(sliderGo.transform, false);
        bgGo.layer = 5;

        RectTransform bgRect = bgGo.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        Image bgImg = bgGo.GetComponent<Image>();
        bgImg.color = SliderBgColor;

        // Fill Area
        Transform fillAreaTrans = sliderGo.transform.Find("Fill Area");
        GameObject fillAreaGo = fillAreaTrans != null ? fillAreaTrans.gameObject : new GameObject("Fill Area", typeof(RectTransform));
        fillAreaGo.transform.SetParent(sliderGo.transform, false);
        fillAreaGo.layer = 5;

        RectTransform fillAreaRect = fillAreaGo.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;

        // Fill
        Transform fillTrans = fillAreaGo.transform.Find("Fill");
        GameObject fillGo = fillTrans != null ? fillTrans.gameObject : new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillGo.transform.SetParent(fillAreaGo.transform, false);
        fillGo.layer = 5;

        RectTransform fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        Image fillImg = fillGo.GetComponent<Image>();
        fillImg.color = fillColor;

        slider.targetGraphic = bgImg;
        slider.fillRect = fillRect;

        return slider;
    }

    private static Button CreateCloseButton(Transform parent, Vector2 pos)
    {
        Transform existing = parent.Find("CloseButton");
        GameObject btnGo = existing != null ? existing.gameObject : new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(parent, false);
        btnGo.layer = 5;

        RectTransform rect = btnGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(46f, 46f);

        Image img = btnGo.GetComponent<Image>();
        img.color = new Color32(28, 70, 90, 255);

        Button btn = btnGo.GetComponent<Button>();
        btn.targetGraphic = img;

        TMP_Text xTxt = CreateText(btnGo.transform, "XText", "✕", 24, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, Vector2.zero, new Vector2(46f, 46f));

        return btn;
    }

    private static void CreateDivider(Transform parent, string name, Vector2 pos, float width)
    {
        Transform existing = parent.Find(name);
        GameObject divGo = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        divGo.transform.SetParent(parent, false);
        divGo.layer = 5;

        RectTransform rect = divGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(width, 2f);

        Image img = divGo.GetComponent<Image>();
        img.color = new Color32(40, 95, 120, 180);
    }

    private static TMP_Text CreateText(Transform parent, string name, string text, float fontSize, FontStyles style, Color color, TextAlignmentOptions align, Vector2 pos, Vector2 size)
    {
        Transform existing = parent.Find(name);
        GameObject txtGo = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        txtGo.transform.SetParent(parent, false);
        txtGo.layer = 5;

        RectTransform rect = txtGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        TextMeshProUGUI tmp = txtGo.GetComponent<TextMeshProUGUI>();
        if (nunitoFont != null) tmp.font = nunitoFont;
        if (nunitoStrokeMat != null) tmp.fontSharedMaterial = nunitoStrokeMat;

        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = align;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;

        return tmp;
    }

    private static void SaveAsPrefab(GameObject instance)
    {
        if (!Directory.Exists(PrefabDir))
        {
            Directory.CreateDirectory(PrefabDir);
        }

        PrefabUtility.SaveAsPrefabAssetAndConnect(instance, PrefabPath, InteractionMode.AutomatedAction);
        Debug.Log($"[PityUIBuilder] Đã lưu Prefab tại {PrefabPath}");
    }
}
#endif
