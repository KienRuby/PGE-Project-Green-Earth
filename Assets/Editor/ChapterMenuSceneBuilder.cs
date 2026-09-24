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
/// Editor Scene Builder cho Màn hình Chapter (Chapter Screen):
/// Xây dựng UI bên trong Canvas/Content/ChapterPanel dùng chung shell và visual assets với Lab/Shop.
/// Menu: PGE > UI > Build Chapter Screen
/// </summary>
[InitializeOnLoad]
public static class ChapterMenuSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string BuildRequestPath = "Assets/Editor/PGE_ChapterUI_BuildRequest.txt";
    private const string GemMineBuildRequestPath = "Assets/Editor/PGE_DailyGemMine_BuildRequest.txt";

    static ChapterMenuSceneBuilder()
    {
        EditorApplication.delayCall += TryBuildRequestedUI;
    }

    private static void TryBuildRequestedUI()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            return;
        }

        if (File.Exists(GemMineBuildRequestPath))
        {
            try
            {
                File.Delete(GemMineBuildRequestPath);
                if (File.Exists(GemMineBuildRequestPath + ".meta"))
                    File.Delete(GemMineBuildRequestPath + ".meta");
            }
            catch { }

            AddDailyGemMineModalToScene();
        }

        if (File.Exists(BuildRequestPath))
        {
            try
            {
                File.Delete(BuildRequestPath);
                if (File.Exists(BuildRequestPath + ".meta"))
                    File.Delete(BuildRequestPath + ".meta");
            }
            catch { }

            BuildChapterScreenScene();
        }
    }
    private const string IconAtlasPath = "Assets/UI/Lab/Generated/lab-icon-atlas.png";
    private const string BackgroundPath = "Assets/UI/Lab/Generated/lab-background.png";
    private const string StartButtonSpritePath = "Assets/Sprites/UI/nút start.png";
    private const string ChapterDatabasePath = "Assets/Data/Chapters/ChapterDatabase.asset";
    private const string BossShadowSpritePath = "Assets/Sprites/Enemy/Boss/Boss1/BossShadow.png";
    private const string QuestDataPath = "Assets/Data/Quests/Quest_01_LabUpgrade.asset";
    private const string FontPath = "Assets/Fonts/Nunito/Nunito SDF.asset";
    private const string FontMaterialPath = "Assets/Fonts/Nunito/Nunito SDF - Stroke.mat";
    private const string ChapterHeadingMaterialPath = "Assets/Fonts/Nunito/Nunito SDF - Chapter Heading.mat";
    private const string WaveFontPath = "Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Roboto-Bold SDF.asset";
    private const string WaveFontMaterialPath = "Assets/Fonts/WaveText Outline.mat";
    private const string QuestBannerSpritePath = "Assets/Sprites/UI/Chapter/btn_quest_banner.png";
    private const string GrowthFundSpritePath = "Assets/Sprites/UI/Chapter/btn_growth_fund.png";
    private const string TowerDefSpritePath = "Assets/Sprites/UI/Chapter/btn_tower_def.png";
    private const string TowerDefPanelPath = "Assets/Sprites/Mini game/Sliced/UI/Panel_TowerDef_Popup.png";
    private const string TowerDefBoardPath = "Assets/Sprites/Mini game/Sliced/UI/Frame_Board_Preview.png";
    private const string TowerDefBackPath = "Assets/Sprites/Mini game/Sliced/UI/Btn_Arrow_Back_Popup.png";
    private const string TowerDefStartPath = "Assets/Sprites/Mini game/Sliced/UI/Btn_Start.png";
    private const string GemMineSpritePath = "Assets/Sprites/UI/Chapter/btn_gem_mine.png";
    private const string MonthlyPremiumSpritePath = "Assets/Sprites/UI/GemMine/bg_monthly_premium.png";
    private const string PriceButtonSpritePath = "Assets/Sprites/UI/GemMine/btn_yellow_price.png";
    private const string PriceButtonPressedSpritePath = "Assets/Sprites/UI/GemMine/btn_yellow_price_pressed.png";
    private const string DailyGemMinePanelSpritePath = "Assets/Sprites/UI/GemMine/bg_daily_gem_mine_panel_clean.png";
    private const string Level1PreviewSpritePath = "Assets/Sprites/UI/GemMine/preview_level_01.png";
    private const string PinkStartSpritePath = "Assets/Sprites/UI/GemMine/btn_pink_start.png";
    private const string PinkStartPressedSpritePath = "Assets/Sprites/UI/GemMine/btn_pink_start_pressed.png";
    private const string Level2PreviewSpritePath = "Assets/Sprites/UI/GemMine/preview_level_02.png";
    private const string RedGemIconSpritePath = "Assets/Sprites/UI/GemMine/icon_red_gem.png";

    private static readonly Color Navy = new Color32(8, 39, 69, 255);
    private static readonly Color Border = new Color32(8, 30, 42, 255);
    private static readonly Color TealBorder = new Color32(94, 213, 205, 255);
    private static readonly Color Panel = new Color32(31, 87, 94, 245);
    private static readonly Color BrightTeal = new Color32(76, 186, 178, 255);
    private static readonly Color MutedTeal = new Color32(27, 74, 82, 255);
    private static readonly Color Cream = new Color32(239, 247, 238, 255);
    private static readonly Color Yellow = new Color32(255, 190, 72, 255);
    private static readonly Color Green = new Color32(88, 174, 108, 255);

    private static TMP_FontAsset font;
    private static Material fontMaterial;

    [MenuItem("PGE/UI/Build Chapter Screen")]
    public static void BuildFromMenu()
    {
        BuildChapterScreenScene();
    }

    [MenuItem("PGE/UI/Add Daily Gem Mine Modal To Scene")]
    public static void AddDailyGemMineModalToScene()
    {
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        fontMaterial = AssetDatabase.LoadAssetAtPath<Material>(FontMaterialPath);

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid()) return;

        Canvas canvas = GameObject.FindObjectOfType<Canvas>();
        if (canvas == null) return;

        Transform contentTr = canvas.transform.Find("Content");
        if (contentTr == null) return;

        Transform chapterPanelTr = contentTr.Find("ChapterPanel");
        if (chapterPanelTr == null) return;

        ChapterScreenController chapterCtrl = chapterPanelTr.GetComponent<ChapterScreenController>();

        Transform existingModal = chapterPanelTr.Find("DailyGemMineModal");
        if (existingModal != null)
        {
            Undo.DestroyObjectImmediate(existingModal.gameObject);
        }

        GameObject modalObj = BuildDailyGemMineModal(chapterPanelTr, font);

        if (chapterCtrl != null)
        {
            SerializedObject ctrlSO = new SerializedObject(chapterCtrl);
            ctrlSO.FindProperty("gemMineModal").objectReferenceValue = modalObj.GetComponent<DailyGemMineModalController>();
            ctrlSO.ApplyModifiedProperties();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ChapterMenuSceneBuilder] DailyGemMineModal đã được cập nhật vào scene với 5 Level Cards và ScrollView mượt mà!");
    }

    public static void BuildChapterScreenScene()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += BuildChapterScreenScene;
            return;
        }

        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        fontMaterial = AssetDatabase.LoadAssetAtPath<Material>(FontMaterialPath);
        if (font == null)
        {
            Debug.LogError($"[ChapterMenuSceneBuilder] Không tìm thấy font tại {FontPath}");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError($"[ChapterMenuSceneBuilder] Không thể mở scene tại {ScenePath}");
            return;
        }

        // Tìm Canvas
        Canvas canvas = GameObject.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[ChapterMenuSceneBuilder] Không tìm thấy Canvas trong MainMenu scene.");
            return;
        }

        Transform contentTr = canvas.transform.Find("Content");
        if (contentTr == null)
        {
            Debug.LogError("[ChapterMenuSceneBuilder] Không tìm thấy Canvas/Content trong MainMenu scene. ChapterPanel bắt buộc phải nằm trong Canvas/Content.");
            return;
        }

        // Migration: Xóa ChapterPanel cũ trực tiếp dưới Canvas nếu có
        Transform legacyChapterPanel = canvas.transform.Find("ChapterPanel");
        if (legacyChapterPanel != null)
        {
            GameObject.DestroyImmediate(legacyChapterPanel.gameObject);
        }

        // Xây dựng ChapterPanel bên trong Canvas/Content
        GameObject chapterPanelObj = BuildChapterPanel(contentTr as RectTransform, font);

        // Update BottomNavigationController in Scene
        BottomNavigationController bottomNav = GameObject.FindObjectOfType<BottomNavigationController>();
        if (bottomNav != null)
        {
            SerializedObject navSO = new SerializedObject(bottomNav);
            SerializedProperty itemsProp = navSO.FindProperty("items");
            if (itemsProp != null && itemsProp.arraySize >= 3)
            {
                // Item index 2 is Chapter
                SerializedProperty chapterItem = itemsProp.GetArrayElementAtIndex(2);
                chapterItem.FindPropertyRelative("panel").objectReferenceValue = chapterPanelObj;

                SerializedProperty iconProp = chapterItem.FindPropertyRelative("icon");
                if (iconProp.objectReferenceValue != null)
                {
                    Image navIconImg = iconProp.objectReferenceValue as Image;
                    if (navIconImg != null)
                    {
                        navIconImg.sprite = LoadIcon("chapter");
                    }
                }

                navSO.FindProperty("defaultSelectedIndex").intValue = 2; // Default to Chapter tab
                navSO.ApplyModifiedProperties();
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    public static GameObject BuildChapterPanel(RectTransform contentParent, TMP_FontAsset fontAsset)
    {
        if (contentParent == null)
        {
            throw new ArgumentNullException(nameof(contentParent));
        }

        font = fontAsset;
        if (font == null)
        {
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        }

        Transform chapterPanelTr = contentParent.Find("ChapterPanel");
        GameObject chapterPanelObj;
        if (chapterPanelTr != null)
        {
            chapterPanelObj = chapterPanelTr.gameObject;
            for (int i = chapterPanelObj.transform.childCount - 1; i >= 0; i--)
            {
                GameObject.DestroyImmediate(chapterPanelObj.transform.GetChild(i).gameObject);
            }
        }
        else
        {
            chapterPanelObj = new GameObject("ChapterPanel", typeof(RectTransform));
            chapterPanelObj.transform.SetParent(contentParent, false);
            chapterPanelTr = chapterPanelObj.transform;
        }

        RectTransform panelRect = chapterPanelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panelRect.localScale = Vector3.one;

        // 1. Sub-Widgets Container (Quest & Growth Fund)
        RectTransform subWidgetsObj = CreateRect("SubWidgetsContainer", chapterPanelTr);
        subWidgetsObj.anchorMin = new Vector2(0f, 1f);
        subWidgetsObj.anchorMax = new Vector2(1f, 1f);
        subWidgetsObj.pivot = new Vector2(0.5f, 1f);
        subWidgetsObj.anchoredPosition = new Vector2(0f, -20f);
        subWidgetsObj.sizeDelta = new Vector2(-40f, 160f);

        // 1A. Quest Widget (Left)
        CreateQuestWidget(subWidgetsObj);

        // 1B. Growth Fund Widget (Right)
        CreateGrowthFundWidget(subWidgetsObj);

        // 2. Chapter Selector Header
        RectTransform chapterHeaderObj = CreateRect("ChapterSelectorHeader", chapterPanelTr);
        chapterHeaderObj.anchorMin = new Vector2(0.5f, 1f);
        chapterHeaderObj.anchorMax = new Vector2(0.5f, 1f);
        chapterHeaderObj.pivot = new Vector2(0.5f, 1f);
        chapterHeaderObj.anchoredPosition = new Vector2(0f, -195f);
        chapterHeaderObj.sizeDelta = new Vector2(960f, 150f);

        // Left Arrow Button
        GameObject prevBtnObj = CreateButton("PrevChapterButton", chapterHeaderObj, new Vector2(-420f, -25f), new Vector2(80f, 98f), "<", 40f, MutedTeal, TealBorder);
        Button prevBtn = prevBtnObj.GetComponent<Button>();

        // Right Arrow Button
        GameObject nextBtnObj = CreateButton("NextChapterButton", chapterHeaderObj, new Vector2(420f, -25f), new Vector2(80f, 98f), ">", 40f, MutedTeal, TealBorder);
        Button nextBtn = nextBtnObj.GetComponent<Button>();

        // Dòng số Chapter nằm phía trên tên riêng của khu vực.
        TMP_Text subtitleText = CreateText("SubtitleText", chapterHeaderObj, "Chapter. 01", 52f, Color.white, TextAlignmentOptions.Center);
        ApplyChapterHeadingStyle(subtitleText, 52f);
        subtitleText.rectTransform.anchoredPosition = new Vector2(0f, 30f);
        subtitleText.rectTransform.sizeDelta = new Vector2(700f, 70f);
        subtitleText.gameObject.SetActive(true);

        // Title Text (Yellow Desert 1)
        TMP_Text titleText = CreateText("TitleText", chapterHeaderObj, "Yellow Desert 1", 80f, Color.white, TextAlignmentOptions.Center);
        ApplyChapterHeadingStyle(titleText, 80f);
        titleText.rectTransform.anchoredPosition = new Vector2(0f, -25f);
        titleText.rectTransform.sizeDelta = new Vector2(900f, 105f);

        // 3. Stage Preview Window
        GameObject previewWindowObj = CreateFrame("StagePreviewWindow", chapterPanelTr, Panel, TealBorder, out _);
        RectTransform previewRect = previewWindowObj.GetComponent<RectTransform>();
        previewRect.anchorMin = new Vector2(0.5f, 1f);
        previewRect.anchorMax = new Vector2(0.5f, 1f);
        previewRect.pivot = new Vector2(0.5f, 1f);
        previewRect.anchoredPosition = new Vector2(0f, -360f);
        previewRect.sizeDelta = new Vector2(960f, 580f);

        // Viewport
        RectTransform viewportObj = CreateRect("Viewport", previewWindowObj.transform);
        Stretch(viewportObj, Vector2.zero, Vector2.one, new Vector2(8f, 8f), new Vector2(-8f, -8f));

        // Background Image
        Image stageBgImg = CreateImage("StageBackground", viewportObj, Color.white, false);
        stageBgImg.sprite = LoadBackground();
        Stretch(stageBgImg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // Wave Badge Pill (Top Center)
        GameObject waveBadgeObj = CreateFrame("WaveBadge", viewportObj, Navy, TealBorder, out _);
        RectTransform waveBadgeRect = waveBadgeObj.GetComponent<RectTransform>();
        waveBadgeRect.anchorMin = new Vector2(0.5f, 1f);
        waveBadgeRect.anchorMax = new Vector2(0.5f, 1f);
        waveBadgeRect.pivot = new Vector2(0.5f, 1f);
        waveBadgeRect.anchoredPosition = new Vector2(0f, -25f);
        waveBadgeRect.sizeDelta = new Vector2(400f, 90f);

        TMP_Text waveText = CreateText("WaveText", waveBadgeObj.transform, "WAVE 01/10", 52f, Color.white, TextAlignmentOptions.Center);
        TMP_FontAsset waveFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(WaveFontPath);
        Material waveMaterial = AssetDatabase.LoadAssetAtPath<Material>(WaveFontMaterialPath);
        if (waveFont != null)
        {
            waveText.font = waveFont;
            waveText.fontStyle = FontStyles.Normal;
            waveText.fontWeight = FontWeight.Regular;
        }
        if (waveMaterial != null)
        {
            waveText.fontSharedMaterial = waveMaterial;
        }
        Stretch(waveText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // Ground shadow behind the boss, matching the chapter preview mockup.
        Image bossShadow = CreateImage("BossShadow", viewportObj, Color.white, false);
        bossShadow.sprite = AssetDatabase.LoadAllAssetsAtPath(BossShadowSpritePath).OfType<Sprite>().FirstOrDefault();
        bossShadow.gameObject.SetActive(bossShadow.sprite != null);
        bossShadow.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        bossShadow.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        bossShadow.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        bossShadow.rectTransform.anchoredPosition = new Vector2(1f, -175f);
        bossShadow.rectTransform.sizeDelta = new Vector2(400f, 60f);

        // Boss Silhouette (Center)
        Image bossImg = CreateImage("BossSilhouette", viewportObj, Color.white, false);
        bossImg.sprite = LoadIcon("leaf");
        bossImg.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        bossImg.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        bossImg.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        bossImg.rectTransform.anchoredPosition = new Vector2(0f, -10f);
        bossImg.rectTransform.sizeDelta = new Vector2(300f, 300f);
        bossImg.preserveAspect = true;
        bossImg.gameObject.SetActive(true);

        // Lock Overlay (Centered in Viewport for locked chapters)
        GameObject lockOverlayObj = CreateRect("LockOverlay", viewportObj).gameObject;
        RectTransform lockRect = lockOverlayObj.GetComponent<RectTransform>();
        lockRect.anchorMin = new Vector2(0.5f, 0.5f);
        lockRect.anchorMax = new Vector2(0.5f, 0.5f);
        lockRect.pivot = new Vector2(0.5f, 0.5f);
        lockRect.anchoredPosition = new Vector2(0f, -10f);
        lockRect.sizeDelta = new Vector2(240f, 240f);

        Image lockIconImg = CreateIcon("LockIcon", lockOverlayObj.transform, "lock", 130f);
        lockIconImg.color = new Color32(235, 100, 90, 255);
        lockIconImg.rectTransform.anchoredPosition = new Vector2(0f, 25f);

        TMP_Text lockLabel = CreateText("LockLabel", lockOverlayObj.transform, "LOCKED", 32f, new Color32(235, 100, 90, 255), TextAlignmentOptions.Center);
        lockLabel.rectTransform.anchoredPosition = new Vector2(0f, -65f);
        lockLabel.rectTransform.sizeDelta = new Vector2(200f, 40f);
        lockOverlayObj.SetActive(false);

        // Flavor / Story Text (Bottom)
        TMP_Text flavorText = CreateText("FlavorText", viewportObj, "Going through the vines\nto look for you, mutants.", 28f, Cream, TextAlignmentOptions.Center);
        flavorText.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        flavorText.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        flavorText.rectTransform.pivot = new Vector2(0.5f, 0f);
        flavorText.rectTransform.anchoredPosition = new Vector2(0f, 20f);
        flavorText.rectTransform.sizeDelta = new Vector2(880f, 80f);

        // 4. Start Action Button
        Sprite startSprite0 = LoadStartSprite("nút start_0");
        Sprite startSprite1 = LoadStartSprite("nút start_1");

        GameObject startBtnObj;
        Button startBtn;
        Image startBtnBg;
        TMP_Text startLabel = null;
        GameObject costBoxObj = null;
        TMP_Text costText = null;
        Image costIconImg = null;

        if (startSprite0 != null && startSprite1 != null)
        {
            startBtnObj = CreateRect("StartButton", chapterPanelTr).gameObject;
            RectTransform startBtnRect = startBtnObj.GetComponent<RectTransform>();
            startBtnRect.anchorMin = new Vector2(0.5f, 0f);
            startBtnRect.anchorMax = new Vector2(0.5f, 0f);
            startBtnRect.pivot = new Vector2(0.5f, 0f);
            startBtnRect.anchoredPosition = new Vector2(0f, 35f);
            startBtnRect.sizeDelta = new Vector2(500f, 145f);

            startBtnBg = startBtnObj.AddComponent<Image>();
            startBtnBg.sprite = startSprite0;
            startBtnBg.color = Color.white;
            startBtnBg.raycastTarget = true;

            Shadow shadow = startBtnObj.AddComponent<Shadow>();
            shadow.effectColor = new Color32(0, 14, 24, 210);
            shadow.effectDistance = new Vector2(5f, -6f);
            shadow.useGraphicAlpha = true;

            startBtn = startBtnObj.AddComponent<Button>();
            startBtn.targetGraphic = startBtnBg;
            startBtn.transition = Selectable.Transition.SpriteSwap;

            SpriteState ss = startBtn.spriteState;
            ss.highlightedSprite = startSprite0;
            ss.pressedSprite = startSprite1;
            ss.selectedSprite = startSprite0;
            ss.disabledSprite = startSprite0;
            startBtn.spriteState = ss;
        }
        else
        {
            startBtnObj = CreateFrame("StartButton", chapterPanelTr, Green, TealBorder, out startBtnBg);
            startBtnBg.raycastTarget = true;
            Image startBorder = startBtnObj.GetComponent<Image>();
            if (startBorder != null)
            {
                startBorder.raycastTarget = true;
            }

            RectTransform startBtnRect = startBtnObj.GetComponent<RectTransform>();
            startBtnRect.anchorMin = new Vector2(0.5f, 0f);
            startBtnRect.anchorMax = new Vector2(0.5f, 0f);
            startBtnRect.pivot = new Vector2(0.5f, 0f);
            startBtnRect.anchoredPosition = new Vector2(0f, 35f);
            startBtnRect.sizeDelta = new Vector2(500f, 145f);

            startBtn = startBtnObj.AddComponent<Button>();
            startBtn.targetGraphic = startBtnBg;

            // Label Start
            startLabel = CreateText("StartLabel", startBtnObj.transform, "Start", 50f, Cream, TextAlignmentOptions.Center);
            startLabel.rectTransform.anchoredPosition = new Vector2(0f, 22f);
            startLabel.rectTransform.sizeDelta = new Vector2(300f, 60f);

            // Cost sub-box
            costBoxObj = CreateFrame("CostBox", startBtnObj.transform, new Color32(11, 55, 72, 220), Border, out _);
            RectTransform costBoxRect = costBoxObj.GetComponent<RectTransform>();
            costBoxRect.anchorMin = new Vector2(0.5f, 0.5f);
            costBoxRect.anchorMax = new Vector2(0.5f, 0.5f);
            costBoxRect.pivot = new Vector2(0.5f, 0.5f);
            costBoxRect.anchoredPosition = new Vector2(0f, -30f);
            costBoxRect.sizeDelta = new Vector2(220f, 48f);

            // Energy Icon
            costIconImg = CreateIcon("CostIcon", costBoxObj.transform, "energy", 40f);
            costIconImg.rectTransform.anchoredPosition = new Vector2(-45f, 0f);

            // Cost Text (X 10)
            costText = CreateText("CostText", costBoxObj.transform, "X 10", 32f, Cream, TextAlignmentOptions.Left);
            costText.rectTransform.anchoredPosition = new Vector2(15f, 0f);
            costText.rectTransform.sizeDelta = new Vector2(120f, 40f);
        }

        // 4B. Side Mode Action Buttons (Tower Def & Gem Mine)
        GameObject towerBtnObj = CreateTowerDefButton(chapterPanelTr);
        Button towerBtn = towerBtnObj != null ? towerBtnObj.GetComponent<Button>() : null;

        GameObject gemMineBtnObj = CreateGemMineButton(chapterPanelTr);
        Button gemMineBtn = gemMineBtnObj != null ? gemMineBtnObj.GetComponent<Button>() : null;

        // 4C. Daily Gem Mine Modal (Popup mở ra khi bấm nút Gem Mine)
        GameObject gemMineModalObj = BuildDailyGemMineModal(chapterPanelTr, font);
        DailyGemMineModalController gemMineModal = gemMineModalObj != null ? gemMineModalObj.GetComponent<DailyGemMineModalController>() : null;

        // 5. Attach ChapterScreenController to ChapterPanel
        ChapterScreenController chapterCtrl = chapterPanelObj.GetComponent<ChapterScreenController>();
        if (chapterCtrl == null)
        {
            chapterCtrl = chapterPanelObj.AddComponent<ChapterScreenController>();
        }

        ChapterDatabase chapterDb = AssetDatabase.LoadAssetAtPath<ChapterDatabase>(ChapterDatabasePath);
        SerializedObject ctrlSO = new SerializedObject(chapterCtrl);
        ctrlSO.FindProperty("chapterDatabase").objectReferenceValue = chapterDb;
        ctrlSO.FindProperty("defaultChapterIndex").intValue = 0;
        ctrlSO.FindProperty("prevChapterButton").objectReferenceValue = prevBtn;
        ctrlSO.FindProperty("nextChapterButton").objectReferenceValue = nextBtn;
        ctrlSO.FindProperty("chapterSubtitleText").objectReferenceValue = subtitleText;
        ctrlSO.FindProperty("chapterTitleText").objectReferenceValue = titleText;
        ctrlSO.FindProperty("previewBackgroundImage").objectReferenceValue = stageBgImg;
        ctrlSO.FindProperty("bossSilhouetteImage").objectReferenceValue = bossImg;
        ctrlSO.FindProperty("lockOverlay").objectReferenceValue = lockOverlayObj;
        ctrlSO.FindProperty("waveBadgeText").objectReferenceValue = waveText;
        ctrlSO.FindProperty("flavorText").objectReferenceValue = flavorText;
        ctrlSO.FindProperty("startButton").objectReferenceValue = startBtn;
        ctrlSO.FindProperty("normalStartSprite").objectReferenceValue = startSprite0;
        ctrlSO.FindProperty("pressedStartSprite").objectReferenceValue = startSprite1;
        ctrlSO.FindProperty("startButtonLabel").objectReferenceValue = startLabel;
        ctrlSO.FindProperty("costBox").objectReferenceValue = costBoxObj;
        ctrlSO.FindProperty("energyCostText").objectReferenceValue = costText;
        ctrlSO.FindProperty("energyCostIcon").objectReferenceValue = costIconImg;
        ctrlSO.FindProperty("towerDefButton").objectReferenceValue = towerBtn;
        ctrlSO.FindProperty("towerDefPanelSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(TowerDefPanelPath);
        ctrlSO.FindProperty("towerDefBoardSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(TowerDefBoardPath);
        ctrlSO.FindProperty("towerDefBackSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(TowerDefBackPath);
        ctrlSO.FindProperty("towerDefStartSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(TowerDefStartPath);
        ctrlSO.FindProperty("gemMineButton").objectReferenceValue = gemMineBtn;
        ctrlSO.FindProperty("gemMineModal").objectReferenceValue = gemMineModal;
        ctrlSO.ApplyModifiedProperties();

        return chapterPanelObj;
    }

    private static GameObject CreateQuestWidget(Transform parent)
    {
        Sprite questSprite = AssetDatabase.LoadAssetAtPath<Sprite>(QuestBannerSpritePath);
        GameObject questObj = CreateRect("QuestWidget", parent).gameObject;
        RectTransform rect = questObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(10f, 0f);
        rect.sizeDelta = new Vector2(310f, 160f);

        Image questImg = questObj.AddComponent<Image>();
        if (questSprite != null)
        {
            questImg.sprite = questSprite;
        }
        questImg.color = Color.white;
        questImg.raycastTarget = true;
        questImg.preserveAspect = true;

        Button questBtn = questObj.AddComponent<Button>();
        questBtn.targetGraphic = questImg;

        Shadow shadow = questObj.AddComponent<Shadow>();
        shadow.effectColor = new Color32(0, 14, 24, 180);
        shadow.effectDistance = new Vector2(3f, -4f);
        shadow.useGraphicAlpha = true;

        QuestWidgetController questCtrl = questObj.AddComponent<QuestWidgetController>();
        QuestData qData = AssetDatabase.LoadAssetAtPath<QuestData>(QuestDataPath);
        SerializedObject qSO = new SerializedObject(questCtrl);
        qSO.FindProperty("currentQuest").objectReferenceValue = qData;
        qSO.FindProperty("getButton").objectReferenceValue = questBtn;
        qSO.ApplyModifiedProperties();

        return questObj;
    }

    private static GameObject CreateGrowthFundWidget(Transform parent)
    {
        Sprite fundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GrowthFundSpritePath);
        GameObject fundObj = CreateRect("GrowthFundWidget", parent).gameObject;
        RectTransform rect = fundObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = new Vector2(-10f, 0f);
        rect.sizeDelta = new Vector2(135f, 120f);

        Image fundImg = fundObj.AddComponent<Image>();
        if (fundSprite != null)
        {
            fundImg.sprite = fundSprite;
        }
        fundImg.color = Color.white;
        fundImg.raycastTarget = true;
        fundImg.preserveAspect = true;

        Button fundBtn = fundObj.AddComponent<Button>();
        fundBtn.targetGraphic = fundImg;

        Shadow shadow = fundObj.AddComponent<Shadow>();
        shadow.effectColor = new Color32(0, 14, 24, 180);
        shadow.effectDistance = new Vector2(3f, -4f);
        shadow.useGraphicAlpha = true;

        GrowthFundWidgetController fundCtrl = fundObj.AddComponent<GrowthFundWidgetController>();
        SerializedObject fundSO = new SerializedObject(fundCtrl);
        fundSO.FindProperty("fundButton").objectReferenceValue = fundBtn;
        fundSO.ApplyModifiedProperties();

        return fundObj;
    }

    private static GameObject CreateTowerDefButton(Transform parent)
    {
        Sprite towerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TowerDefSpritePath);
        GameObject towerObj = CreateRect("TowerDefButton", parent).gameObject;
        RectTransform rect = towerObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(-365f, 291f);
        rect.sizeDelta = new Vector2(180f, 160f);

        Image towerImg = towerObj.AddComponent<Image>();
        if (towerSprite != null)
        {
            towerImg.sprite = towerSprite;
        }
        towerImg.color = Color.white;
        towerImg.raycastTarget = true;
        towerImg.preserveAspect = true;

        Button towerBtn = towerObj.AddComponent<Button>();
        towerBtn.targetGraphic = towerImg;

        Shadow shadow = towerObj.AddComponent<Shadow>();
        shadow.effectColor = new Color32(0, 14, 24, 180);
        shadow.effectDistance = new Vector2(4f, -5f);
        shadow.useGraphicAlpha = true;

        return towerObj;
    }

    private static GameObject CreateGemMineButton(Transform parent)
    {
        Sprite mineSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GemMineSpritePath);
        GameObject mineObj = CreateRect("GemMineButton", parent).gameObject;
        RectTransform rect = mineObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(365f, 291f);
        rect.sizeDelta = new Vector2(178f, 160f);

        Image mineImg = mineObj.AddComponent<Image>();
        if (mineSprite != null)
        {
            mineImg.sprite = mineSprite;
        }
        mineImg.color = Color.white;
        mineImg.raycastTarget = true;
        mineImg.preserveAspect = true;

        Button mineBtn = mineObj.AddComponent<Button>();
        mineBtn.targetGraphic = mineImg;

        Shadow shadow = mineObj.AddComponent<Shadow>();
        shadow.effectColor = new Color32(0, 14, 24, 180);
        shadow.effectDistance = new Vector2(4f, -5f);
        shadow.useGraphicAlpha = true;

        return mineObj;
    }

    public static GameObject BuildDailyGemMineModal(Transform parent, TMP_FontAsset fontAsset)
    {
        Sprite bannerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MonthlyPremiumSpritePath);
        Sprite priceBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PriceButtonSpritePath);
        Sprite priceBtnPressedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PriceButtonPressedSpritePath);
        Sprite panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DailyGemMinePanelSpritePath);
        Sprite lvl1Sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Level1PreviewSpritePath);
        Sprite pinkBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PinkStartSpritePath);
        Sprite pinkBtnPressedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PinkStartPressedSpritePath);
        Sprite lvl2Sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Level2PreviewSpritePath);

        // 1. Root Modal Object
        GameObject modalObj = CreateRect("DailyGemMineModal", parent).gameObject;
        RectTransform modalRect = modalObj.GetComponent<RectTransform>();
        Stretch(modalRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        CanvasGroup cg = modalObj.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;

        DailyGemMineModalController modalCtrl = modalObj.AddComponent<DailyGemMineModalController>();

        // 2. Backdrop Dim (Touch outside to dismiss)
        GameObject backdropObj = CreateRect("Backdrop", modalObj.transform).gameObject;
        RectTransform backdropRect = backdropObj.GetComponent<RectTransform>();
        Stretch(backdropRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image backdropImg = backdropObj.AddComponent<Image>();
        backdropImg.color = new Color32(0, 0, 0, 220);
        backdropImg.raycastTarget = true;
        Button backdropBtn = backdropObj.AddComponent<Button>();

        // 3. Content Root (Centered container for Banner + Main Panel)
        GameObject contentObj = CreateRect("ContentRoot", modalObj.transform).gameObject;
        RectTransform contentRect = contentObj.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.anchoredPosition = new Vector2(0f, 25f);
        contentRect.sizeDelta = new Vector2(980f, 1530f);

        // 4. Monthly Premium Banner (with star badge & 90.000 đ button baked into sprite)
        GameObject bannerObj = CreateRect("MonthlyPremiumBanner", contentObj.transform).gameObject;
        RectTransform bannerRect = bannerObj.GetComponent<RectTransform>();
        bannerRect.anchorMin = new Vector2(0.5f, 1f);
        bannerRect.anchorMax = new Vector2(0.5f, 1f);
        bannerRect.pivot = new Vector2(0.5f, 1f);
        bannerRect.anchoredPosition = new Vector2(0f, 0f);
        bannerRect.sizeDelta = new Vector2(980f, 290f);

        Image bannerImg = bannerObj.AddComponent<Image>();
        if (bannerSprite != null) bannerImg.sprite = bannerSprite;
        bannerImg.color = Color.white;
        bannerImg.preserveAspect = false;

        Shadow bannerShadow = bannerObj.AddComponent<Shadow>();
        bannerShadow.effectColor = new Color32(0, 14, 24, 200);
        bannerShadow.effectDistance = new Vector2(4f, -5f);

        // 4A. Hitbox Button (90.000 đ) over Monthly Premium Banner (transparent raycast target)
        GameObject priceBtnObj = CreateRect("PriceButton", bannerObj.transform).gameObject;
        RectTransform priceRect = priceBtnObj.GetComponent<RectTransform>();
        priceRect.anchorMin = new Vector2(0.5f, 0f);
        priceRect.anchorMax = new Vector2(0.5f, 0f);
        priceRect.pivot = new Vector2(0.5f, 0f);
        priceRect.anchoredPosition = new Vector2(0f, 15f);
        priceRect.sizeDelta = new Vector2(300f, 90f);

        Image priceImg = priceBtnObj.AddComponent<Image>();
        priceImg.color = Color.clear;
        priceImg.raycastTarget = true;

        Button priceBtn = priceBtnObj.AddComponent<Button>();
        priceBtn.targetGraphic = priceImg;

        // 5. Daily Gem Mine Main Panel
        GameObject panelObj = CreateRect("DailyGemMinePanel", contentObj.transform).gameObject;
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0f, -305f);
        panelRect.sizeDelta = new Vector2(766f, 1200f);

        Image panelImg = panelObj.AddComponent<Image>();
        if (panelSprite != null) panelImg.sprite = panelSprite;
        panelImg.color = Color.white;
        panelImg.preserveAspect = false;

        Shadow panelShadow = panelObj.AddComponent<Shadow>();
        panelShadow.effectColor = new Color32(0, 14, 24, 220);
        panelShadow.effectDistance = new Vector2(5f, -6f);

        // 5A. Reset Timer Text (Reset in: 09 Hour 26 Min Left)
        TMP_Text resetTxt = CreateText("ResetTimerText", panelObj.transform, "Reset in: <color=#FFEE33>09</color> Hour <color=#FFEE33>26</color> Min Left", 30f, Cream, TextAlignmentOptions.Center);
        resetTxt.fontStyle = FontStyles.Bold;
        resetTxt.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        resetTxt.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        resetTxt.rectTransform.pivot = new Vector2(0.5f, 1f);
        resetTxt.rectTransform.anchoredPosition = new Vector2(0f, -225f);
        resetTxt.rectTransform.sizeDelta = new Vector2(700f, 40f);
        resetTxt.outlineColor = Navy;
        resetTxt.outlineWidth = 0.2f;

        // 5B. Entrance Count Text (Entrance: 5 Left)
        TMP_Text entranceTxt = CreateText("EntranceCountText", panelObj.transform, "Entrance: <color=#FFEE33>5</color> Left", 32f, Cream, TextAlignmentOptions.Center);
        entranceTxt.fontStyle = FontStyles.Bold;
        entranceTxt.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        entranceTxt.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        entranceTxt.rectTransform.pivot = new Vector2(0.5f, 1f);
        entranceTxt.rectTransform.anchoredPosition = new Vector2(0f, -270f);
        entranceTxt.rectTransform.sizeDelta = new Vector2(700f, 40f);
        entranceTxt.outlineColor = Navy;
        entranceTxt.outlineWidth = 0.2f;

        // 5C. Scrollable Levels Container (ScrollRect with 5 playable Level Cards)
        GameObject scrollViewObj = CreateRect("LevelsScrollView", panelObj.transform).gameObject;
        RectTransform scrollRectTransform = scrollViewObj.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0.5f, 1f);
        scrollRectTransform.anchorMax = new Vector2(0.5f, 1f);
        scrollRectTransform.pivot = new Vector2(0.5f, 1f);
        scrollRectTransform.anchoredPosition = new Vector2(0f, -325f);
        scrollRectTransform.sizeDelta = new Vector2(720f, 850f);

        ScrollRect scrollRect = scrollViewObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.135f;
        scrollRect.scrollSensitivity = 40f;

        // Viewport with Image (raycastTarget = true) and RectMask2D (geometric 2D clipping, no alpha stencil bug)
        GameObject viewportObj = CreateRect("Viewport", scrollViewObj.transform).gameObject;
        RectTransform viewportRect = viewportObj.GetComponent<RectTransform>();
        Stretch(viewportRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image vpImg = viewportObj.AddComponent<Image>();
        vpImg.color = Color.clear;
        vpImg.raycastTarget = true;
        viewportObj.AddComponent<RectMask2D>();
        scrollRect.viewport = viewportRect;

        // Content container with VerticalLayoutGroup and ContentSizeFitter
        GameObject contentListObj = CreateRect("Content", viewportObj.transform).gameObject;
        RectTransform contentListRect = contentListObj.GetComponent<RectTransform>();
        contentListRect.anchorMin = new Vector2(0f, 1f);
        contentListRect.anchorMax = new Vector2(1f, 1f);
        contentListRect.pivot = new Vector2(0.5f, 1f);
        contentListRect.anchoredPosition = Vector2.zero;
        contentListRect.sizeDelta = new Vector2(0f, 0f);
        scrollRect.content = contentListRect;

        VerticalLayoutGroup vlg = contentListObj.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 10, 30);
        vlg.spacing = 25f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = false;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = contentListObj.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Sprite references for Cards
        Sprite redGemSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RedGemIconSpritePath);

        // Build 5 Level Cards
        DailyGemMineLevelCard[] cards = new DailyGemMineLevelCard[5];

        cards[0] = CreateLevelCard(contentListObj.transform, 1, "Gem Mine LV.01", "x60-120", lvl1Sprite, Color.white, pinkBtnSprite, pinkBtnPressedSprite, redGemSprite);
        cards[1] = CreateLevelCard(contentListObj.transform, 2, "Gem Mine LV.02", "x120-220", lvl2Sprite, Color.white, pinkBtnSprite, pinkBtnPressedSprite, redGemSprite);
        cards[2] = CreateLevelCard(contentListObj.transform, 3, "Gem Mine LV.03", "x200-350", lvl1Sprite, new Color32(215, 235, 255, 255), pinkBtnSprite, pinkBtnPressedSprite, redGemSprite);
        cards[3] = CreateLevelCard(contentListObj.transform, 4, "Gem Mine LV.04", "x300-500", lvl2Sprite, new Color32(255, 235, 205, 255), pinkBtnSprite, pinkBtnPressedSprite, redGemSprite);
        cards[4] = CreateLevelCard(contentListObj.transform, 5, "Gem Mine LV.05", "x450-700", lvl1Sprite, new Color32(235, 215, 255, 255), pinkBtnSprite, pinkBtnPressedSprite, redGemSprite);

        Button startBtn = cards[0] != null ? cards[0].StartButton : null;

        // 6. Wire up serialized properties in DailyGemMineModalController
        SerializedObject modalSO = new SerializedObject(modalCtrl);
        modalSO.FindProperty("modalRoot").objectReferenceValue = modalObj;
        modalSO.FindProperty("mainPanel").objectReferenceValue = contentRect;
        modalSO.FindProperty("canvasGroup").objectReferenceValue = cg;
        modalSO.FindProperty("backdropButton").objectReferenceValue = backdropBtn;
        modalSO.FindProperty("closeButton").objectReferenceValue = null;
        modalSO.FindProperty("monthlyPremiumButton").objectReferenceValue = priceBtn;
        modalSO.FindProperty("startLevel1Button").objectReferenceValue = startBtn;
        modalSO.FindProperty("resetTimerText").objectReferenceValue = resetTxt;
        modalSO.FindProperty("entranceCountText").objectReferenceValue = entranceTxt;
        modalSO.FindProperty("levelsScrollRect").objectReferenceValue = scrollRect;

        SerializedProperty cardsProp = modalSO.FindProperty("levelCards");
        if (cardsProp != null)
        {
            cardsProp.arraySize = cards.Length;
            for (int i = 0; i < cards.Length; i++)
            {
                cardsProp.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
            }
        }

        SerializedProperty sceneNameProp = modalSO.FindProperty("gemMineSceneName");
        if (sceneNameProp != null) sceneNameProp.stringValue = "GenMine";
        modalSO.ApplyModifiedProperties();

        // 7. Attach DailyGemMineLayoutTuner for live position & size adjustments
        DailyGemMineLayoutTuner tuner = modalObj.GetComponent<DailyGemMineLayoutTuner>() ?? modalObj.AddComponent<DailyGemMineLayoutTuner>();
        tuner.AutoFindReferences();
        tuner.ApplyLayout();

        // Modal starts hidden
        modalObj.SetActive(false);

        return modalObj;
    }

    private static DailyGemMineLevelCard CreateLevelCard(
        Transform parent,
        int level,
        string title,
        string reward,
        Sprite previewSprite,
        Color previewColor,
        Sprite pinkBtnSprite,
        Sprite pinkBtnPressedSprite,
        Sprite gemIconSprite)
    {
        GameObject cardObj = CreateRect($"Card_Level_{level:D2}", parent).gameObject;
        RectTransform cardRect = cardObj.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(700f, 350f);

        LayoutElement le = cardObj.AddComponent<LayoutElement>();
        le.preferredWidth = 700f;
        le.preferredHeight = 350f;
        le.minHeight = 350f;

        // Background Preview Image (raycastTarget = false để cử chỉ lướt truyền thẳng về ScrollRect)
        Image bgImg = cardObj.AddComponent<Image>();
        if (previewSprite != null) bgImg.sprite = previewSprite;
        bgImg.color = previewColor;
        bgImg.preserveAspect = false;
        bgImg.raycastTarget = false;

        Shadow cardShadow = cardObj.AddComponent<Shadow>();
        cardShadow.effectColor = new Color32(0, 14, 24, 200);
        cardShadow.effectDistance = new Vector2(4f, -5f);

        // Header Banner Bar
        GameObject headerObj = CreateRect("HeaderBanner", cardObj.transform).gameObject;
        RectTransform headerRect = headerObj.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = new Vector2(0f, 0f);
        headerRect.sizeDelta = new Vector2(0f, 70f);

        Image headerImg = headerObj.AddComponent<Image>();
        headerImg.color = new Color32(0, 0, 0, 180);
        headerImg.raycastTarget = false;

        // Title Text (Gem Mine LV.0X)
        TMP_Text titleTxt = CreateText("TitleText", headerObj.transform, title, 36f, Color.white, TextAlignmentOptions.Left);
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.outlineColor = Navy;
        titleTxt.outlineWidth = 0.25f;
        titleTxt.enableWordWrapping = false;
        titleTxt.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        titleTxt.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        titleTxt.rectTransform.pivot = new Vector2(0f, 0.5f);
        titleTxt.rectTransform.anchoredPosition = new Vector2(25f, 0f);
        titleTxt.rectTransform.sizeDelta = new Vector2(320f, 50f);

        // Reward Gem Icon
        GameObject gemIconObj = CreateRect("GemIcon", headerObj.transform).gameObject;
        RectTransform gemRect = gemIconObj.GetComponent<RectTransform>();
        gemRect.anchorMin = new Vector2(1f, 0.5f);
        gemRect.anchorMax = new Vector2(1f, 0.5f);
        gemRect.pivot = new Vector2(1f, 0.5f);
        gemRect.anchoredPosition = new Vector2(-175f, 0f);
        gemRect.sizeDelta = new Vector2(34f, 44f);

        Image gemImg = gemIconObj.AddComponent<Image>();
        if (gemIconSprite != null) gemImg.sprite = gemIconSprite;
        gemImg.preserveAspect = true;
        gemImg.raycastTarget = false;

        // Reward Text (x120-220)
        TMP_Text rwdTxt = CreateText("RewardText", headerObj.transform, reward, 32f, Color.white, TextAlignmentOptions.Left);
        rwdTxt.fontStyle = FontStyles.Bold;
        rwdTxt.outlineColor = Navy;
        rwdTxt.outlineWidth = 0.25f;
        rwdTxt.enableWordWrapping = false;
        rwdTxt.overflowMode = TextOverflowModes.Overflow;
        rwdTxt.rectTransform.anchorMin = new Vector2(1f, 0.5f);
        rwdTxt.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        rwdTxt.rectTransform.pivot = new Vector2(0f, 0.5f);
        rwdTxt.rectTransform.anchoredPosition = new Vector2(-165f, 0f);
        rwdTxt.rectTransform.sizeDelta = new Vector2(150f, 50f);

        // Pink Start Button (Bottom-Right)
        GameObject startBtnObj = CreateRect("StartButton", cardObj.transform).gameObject;
        RectTransform startBtnRect = startBtnObj.GetComponent<RectTransform>();
        startBtnRect.anchorMin = new Vector2(1f, 0f);
        startBtnRect.anchorMax = new Vector2(1f, 0f);
        startBtnRect.pivot = new Vector2(1f, 0f);
        startBtnRect.anchoredPosition = new Vector2(-20f, 18f);
        startBtnRect.sizeDelta = new Vector2(190f, 72f);

        Image startImg = startBtnObj.AddComponent<Image>();
        if (pinkBtnSprite != null) startImg.sprite = pinkBtnSprite;
        startImg.color = Color.white;
        startImg.preserveAspect = false;

        Button startBtn = startBtnObj.AddComponent<Button>();
        startBtn.targetGraphic = startImg;
        if (pinkBtnPressedSprite != null)
        {
            startBtn.transition = Selectable.Transition.SpriteSwap;
            SpriteState ss = startBtn.spriteState;
            ss.pressedSprite = pinkBtnPressedSprite;
            startBtn.spriteState = ss;
        }

        // Locked Badge
        GameObject lockBadgeObj = CreateRect("LockedBadge", cardObj.transform).gameObject;
        RectTransform lockRect = lockBadgeObj.GetComponent<RectTransform>();
        lockRect.anchorMin = new Vector2(1f, 0f);
        lockRect.anchorMax = new Vector2(1f, 0f);
        lockRect.pivot = new Vector2(1f, 0f);
        lockRect.anchoredPosition = new Vector2(-20f, 18f);
        lockRect.sizeDelta = new Vector2(190f, 72f);

        Image lockImg = lockBadgeObj.AddComponent<Image>();
        lockImg.color = new Color(0.12f, 0.12f, 0.16f, 0.88f);
        lockImg.raycastTarget = false;

        TMP_Text lockTxt = CreateText("LockedLabel", lockBadgeObj.transform, "LOCKED", 30f, new Color32(200, 200, 200, 255), TextAlignmentOptions.Center);
        lockTxt.fontStyle = FontStyles.Bold;
        Stretch(lockTxt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        lockBadgeObj.SetActive(false);

        // Attach DailyGemMineLevelCard
        DailyGemMineLevelCard cardCtrl = cardObj.AddComponent<DailyGemMineLevelCard>();
        SerializedObject cardSO = new SerializedObject(cardCtrl);
        cardSO.FindProperty("levelNumber").intValue = level;
        cardSO.FindProperty("levelTitleText").objectReferenceValue = titleTxt;
        cardSO.FindProperty("rewardText").objectReferenceValue = rwdTxt;
        cardSO.FindProperty("rewardGemIcon").objectReferenceValue = gemImg;
        cardSO.FindProperty("previewImage").objectReferenceValue = bgImg;
        cardSO.FindProperty("startButton").objectReferenceValue = startBtn;
        cardSO.FindProperty("lockOverlay").objectReferenceValue = lockBadgeObj;
        cardSO.FindProperty("lockLabel").objectReferenceValue = lockTxt;
        cardSO.ApplyModifiedProperties();

        return cardCtrl;
    }

    private static GameObject CreateButton(
        string name,
        Transform parent,
        Vector2 pos,
        Vector2 size,
        string label,
        float fontSize,
        Color fillColor,
        Color borderColor)
    {
        GameObject btnObj = CreateFrame(name, parent, fillColor, borderColor, out Image bg);
        bg.raycastTarget = true;
        Image border = btnObj.GetComponent<Image>();
        if (border != null)
        {
            border.raycastTarget = true;
        }

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = bg;

        TMP_Text txt = CreateText("Label", btnObj.transform, label, fontSize, Cream, TextAlignmentOptions.Center);
        txt.raycastTarget = false;
        Stretch(txt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        return btnObj;
    }

    private static GameObject CreateFrame(
        string name,
        Transform parent,
        Color fillColor,
        Color borderColor,
        out Image background)
    {
        RectTransform root = CreateRect(name, parent);
        Image borderImage = root.gameObject.AddComponent<Image>();
        borderImage.color = borderColor;
        borderImage.raycastTarget = false;

        Shadow shadow = root.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color32(0, 14, 24, 210);
        shadow.effectDistance = new Vector2(5f, -6f);
        shadow.useGraphicAlpha = true;

        background = CreateImage("Background", root, fillColor, false);
        Stretch(background.rectTransform, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f));

        return root.gameObject;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        return rect;
    }

    private static Image CreateImage(string name, Transform parent, Color color, bool raycast)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycast;
        return image;
    }

    private static Image CreateIcon(string name, Transform parent, string spriteName, float size)
    {
        Image image = CreateImage(name, parent, Color.white, false);
        image.sprite = LoadIcon(spriteName);
        image.preserveAspect = true;
        image.rectTransform.sizeDelta = new Vector2(size, size);
        return image;
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        string value,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        if (fontMaterial != null)
        {
            text.fontSharedMaterial = fontMaterial;
        }
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        text.outlineColor = Navy;
        text.outlineWidth = 0.16f;
        return text;
    }

    private static void ApplyChapterHeadingStyle(TMP_Text text, float fontSize)
    {
        Material chapterHeadingMaterial = AssetDatabase.LoadAssetAtPath<Material>(ChapterHeadingMaterialPath);
        if (chapterHeadingMaterial != null)
        {
            text.fontSharedMaterial = chapterHeadingMaterial;
        }

        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.fontWeight = FontWeight.Black;
        text.color = Color.white;
    }

    private static Sprite LoadIcon(string spriteName)
    {
        return AssetDatabase.LoadAllAssetRepresentationsAtPath(IconAtlasPath)
            .OfType<Sprite>()
            .FirstOrDefault(sprite => sprite.name == spriteName);
    }

    private static Sprite LoadBackground()
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
    }

    private static Sprite LoadStartSprite(string spriteName)
    {
        Sprite[] sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(StartButtonSpritePath)
            .OfType<Sprite>()
            .ToArray();
        if (sprites.Length == 0)
        {
            sprites = AssetDatabase.LoadAllAssetsAtPath(StartButtonSpritePath)
                .OfType<Sprite>()
                .ToArray();
        }
        return sprites.FirstOrDefault(s => s.name == spriteName);
    }

    private static void Stretch(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.localScale = Vector3.one;
    }
}
#endif
