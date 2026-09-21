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
public static class ChapterMenuSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string BuildRequestPath = "Assets/Editor/PGE_ChapterUI_BuildRequest.txt";
    private const string IconAtlasPath = "Assets/UI/Lab/Generated/lab-icon-atlas.png";
    private const string BackgroundPath = "Assets/UI/Lab/Generated/lab-background.png";
    private const string StartButtonSpritePath = "Assets/Sprites/UI/nút start.png";
    private const string ChapterDatabasePath = "Assets/Data/Chapters/ChapterDatabase.asset";
    private const string QuestDataPath = "Assets/Data/Quests/Quest_01_LabUpgrade.asset";
    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    private const string QuestBannerSpritePath = "Assets/Sprites/UI/Chapter/btn_quest_banner.png";
    private const string GrowthFundSpritePath = "Assets/Sprites/UI/Chapter/btn_growth_fund.png";
    private const string TowerDefSpritePath = "Assets/Sprites/UI/Chapter/btn_tower_def.png";
    private const string GemMineSpritePath = "Assets/Sprites/UI/Chapter/btn_gem_mine.png";

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

    [MenuItem("PGE/UI/Build Chapter Screen")]
    public static void BuildFromMenu()
    {
        BuildChapterScreenScene();
    }

    public static void BuildChapterScreenScene()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += BuildChapterScreenScene;
            return;
        }

        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
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
        GameObject prevBtnObj = CreateButton("PrevChapterButton", chapterHeaderObj, new Vector2(-360f, -40f), new Vector2(80f, 80f), "<", 40f, MutedTeal, TealBorder);
        Button prevBtn = prevBtnObj.GetComponent<Button>();

        // Right Arrow Button
        GameObject nextBtnObj = CreateButton("NextChapterButton", chapterHeaderObj, new Vector2(360f, -40f), new Vector2(80f, 80f), ">", 40f, MutedTeal, TealBorder);
        Button nextBtn = nextBtnObj.GetComponent<Button>();

        // Subtitle Text (Chapter. 04)
        TMP_Text subtitleText = CreateText("SubtitleText", chapterHeaderObj, "Chapter. 04", 36f, Cream, TextAlignmentOptions.Center);
        subtitleText.rectTransform.anchoredPosition = new Vector2(0f, -10f);
        subtitleText.rectTransform.sizeDelta = new Vector2(600f, 50f);

        // Title Text (Dense Jungle 1)
        TMP_Text titleText = CreateText("TitleText", chapterHeaderObj, "Dense Jungle 1", 62f, Yellow, TextAlignmentOptions.Center);
        titleText.rectTransform.anchoredPosition = new Vector2(0f, -65f);
        titleText.rectTransform.sizeDelta = new Vector2(800f, 80f);

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
        waveBadgeRect.sizeDelta = new Vector2(280f, 64f);

        TMP_Text waveText = CreateText("WaveText", waveBadgeObj.transform, "WAVE: 01/10", 30f, Cream, TextAlignmentOptions.Center);
        Stretch(waveText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

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
        ctrlSO.FindProperty("gemMineButton").objectReferenceValue = gemMineBtn;
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
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        text.outlineColor = Navy;
        text.outlineWidth = 0.16f;
        return text;
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
