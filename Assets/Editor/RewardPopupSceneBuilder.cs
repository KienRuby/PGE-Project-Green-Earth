#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Editor Scene Builder cho hệ thống Daily Login Reward + Achievements Popup:
/// - Tạo ScriptableObject databases chuẩn nếu chưa có (DailyLoginDatabase.asset, AchievementDatabase.asset)
/// - Xây dựng hoàn chỉnh Popup UI bên trong Canvas của MainMenu.unity
/// - Bố trí 2 Tab: Daily Login Reward & Achievements theo đúng visual reference trong 2 ảnh mẫu
/// - Gắn và kết nối chính xác 100% tất cả các component và serialized field trong Inspector
/// 
/// Menu: PGE > UI > Build Daily Login & Achievement Popup
/// </summary>
public static class RewardPopupSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string DailyLoginDbPath = "Assets/Data/DailyLogin/DailyLoginDatabase.asset";
    private const string AchievementDbPath = "Assets/Data/Achievements/AchievementDatabase.asset";
    private const string ResourceIconPath = "Assets/Sprites/UI/icon tài nguyên.png";
    private const string BookSettingIconPath = "Assets/Sprites/UI/icon book, setting, dấu thông báo.png";
    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    // Visual Palette matching Reference Images
    private static readonly Color DimColor = new Color32(4, 10, 16, 215);
    private static readonly Color WindowBorderColor = new Color32(94, 213, 205, 255);
    private static readonly Color WindowBgColor = new Color32(11, 35, 55, 250);
    private static readonly Color CardBorderColor = new Color32(64, 180, 195, 255);
    private static readonly Color CardBgColor = new Color32(14, 48, 68, 255);

    private static readonly Color ActiveTabBg = new Color32(64, 218, 210, 255);
    private static readonly Color InactiveTabBg = new Color32(20, 70, 85, 255);
    private static readonly Color ActiveTabText = new Color32(255, 255, 255, 255);
    private static readonly Color InactiveTabText = new Color32(140, 200, 205, 255);

    private static readonly Color GetBtnColor = new Color32(56, 189, 248, 255);
    private static readonly Color NotAchievedBtnColor = new Color32(65, 80, 95, 255);
    private static readonly Color ObtainedBtnColor = new Color32(35, 50, 65, 255);
    private static readonly Color ProgressFillColor = new Color32(40, 180, 245, 255);
    private static readonly Color ProgressBgColor = new Color32(12, 32, 45, 255);

    private static readonly Color TextWhite = new Color32(245, 255, 255, 255);
    private static readonly Color TextYellow = new Color32(255, 190, 72, 255);
    private static readonly Color TextGray = new Color32(160, 180, 195, 255);
    private static readonly Color NavyOutline = new Color32(8, 30, 42, 255);

    private static TMP_FontAsset font;
    private static Sprite energySprite;
    private static Sprite redGemSprite;
    private static Sprite dataChipSprite;

    [MenuItem("PGE/UI/Build Daily Login & Achievement Popup")]
    public static void BuildFromMenu()
    {
        BuildRewardPopupScene();
    }

    public static void BuildRewardPopupScene()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += BuildRewardPopupScene;
            return;
        }

        // 1. Tạo thư mục & ScriptableObjects
        EnsureDatabasesCreated();

        // 2. Nạp Assets
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        LoadResourceSprites();

        // 3. Mở MainMenu scene
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError($"[RewardPopupSceneBuilder] Không thể mở scene tại {ScenePath}");
            return;
        }

        Canvas canvas = GameObject.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[RewardPopupSceneBuilder] Không tìm thấy Canvas trong MainMenu scene.");
            return;
        }

        // 4. Xây dựng Popup
        GameObject popupObj = BuildRewardPopup(canvas.transform as RectTransform);

        // 5. Kết nối với TopBar MailButton & Notification Badge
        TopBarCurrencyController topBar = GameObject.FindObjectOfType<TopBarCurrencyController>();
        if (topBar != null)
        {
            Transform mailBtnTr = topBar.transform.Find("MailButton");
            if (mailBtnTr != null)
            {
                Button mailBtn = mailBtnTr.GetComponent<Button>();
                Transform dotTr = mailBtnTr.Find("NotificationDot") ?? mailBtnTr.Find("Badge");
                SerializedObject topBarSO = new SerializedObject(topBar);
                if (mailBtn != null)
                {
                    topBarSO.FindProperty("questBookButton").objectReferenceValue = mailBtn;
                }
                if (dotTr != null)
                {
                    topBarSO.FindProperty("questNotificationBadge").objectReferenceValue = dotTr.gameObject;
                }
                topBarSO.ApplyModifiedProperties();
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[RewardPopupSceneBuilder] ✅ Đã xây dựng hoàn tất Reward Popup trong MainMenu scene!");
    }

    public static void EnsureDatabasesCreated()
    {
        if (!Directory.Exists("Assets/Data/DailyLogin"))
        {
            Directory.CreateDirectory("Assets/Data/DailyLogin");
        }
        if (!Directory.Exists("Assets/Data/Achievements"))
        {
            Directory.CreateDirectory("Assets/Data/Achievements");
        }

        DailyLoginDatabase dailyDb = AssetDatabase.LoadAssetAtPath<DailyLoginDatabase>(DailyLoginDbPath);
        if (dailyDb == null)
        {
            dailyDb = ScriptableObject.CreateInstance<DailyLoginDatabase>();
            dailyDb.PopulateDefault7Days();
            AssetDatabase.CreateAsset(dailyDb, DailyLoginDbPath);
            Debug.Log($"[RewardPopupSceneBuilder] Đã tạo ScriptableObject: {DailyLoginDbPath}");
        }

        AchievementDatabase achDb = AssetDatabase.LoadAssetAtPath<AchievementDatabase>(AchievementDbPath);
        if (achDb == null)
        {
            achDb = ScriptableObject.CreateInstance<AchievementDatabase>();
            achDb.PopulateDefaultAchievements();
            AssetDatabase.CreateAsset(achDb, AchievementDbPath);
            Debug.Log($"[RewardPopupSceneBuilder] Đã tạo ScriptableObject: {AchievementDbPath}");
        }

        AssetDatabase.SaveAssets();
    }

    private static void LoadResourceSprites()
    {
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(ResourceIconPath)
            .OfType<Sprite>()
            .ToArray();

        if (sprites != null && sprites.Length > 0)
        {
            energySprite = Array.Find(sprites, s => s.name == "engry") ?? sprites[0];
            redGemSprite = Array.Find(sprites, s => s.name == "red") ?? sprites[0];
            dataChipSprite = Array.Find(sprites, s => s.name == "data") ?? sprites[0];
        }
    }

    public static GameObject BuildRewardPopup(RectTransform canvasParent)
    {
        if (canvasParent == null) throw new ArgumentNullException(nameof(canvasParent));

        Transform existingPopup = canvasParent.Find("RewardPopup");
        GameObject popupObj;
        if (existingPopup != null)
        {
            popupObj = existingPopup.gameObject;
            for (int i = popupObj.transform.childCount - 1; i >= 0; i--)
            {
                GameObject.DestroyImmediate(popupObj.transform.GetChild(i).gameObject);
            }
        }
        else
        {
            popupObj = new GameObject("RewardPopup", typeof(RectTransform));
            popupObj.transform.SetParent(canvasParent, false);
        }

        RectTransform popupRect = popupObj.GetComponent<RectTransform>();
        Stretch(popupRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // A. Dim Background Button (Click ra ngoài để đóng popup)
        GameObject dimObj = CreateRect("DimBackground", popupObj.transform).gameObject;
        RectTransform dimRect = dimObj.GetComponent<RectTransform>();
        Stretch(dimRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image dimImg = dimObj.AddComponent<Image>();
        dimImg.color = DimColor;
        dimImg.raycastTarget = true;
        Button dimBtn = dimObj.AddComponent<Button>();

        // B. Window Container
        GameObject windowObj = CreateFrame("Window", popupObj.transform, WindowBgColor, WindowBorderColor, out _);
        RectTransform windowRect = windowObj.GetComponent<RectTransform>();
        windowRect.anchorMin = new Vector2(0.5f, 0.5f);
        windowRect.anchorMax = new Vector2(0.5f, 0.5f);
        windowRect.pivot = new Vector2(0.5f, 0.5f);
        windowRect.anchoredPosition = new Vector2(0f, -40f);
        windowRect.sizeDelta = new Vector2(1000f, 1400f);

        // D. Tabs Header Container
        RectTransform tabsHeader = CreateRect("Tabs", windowObj.transform);
        tabsHeader.anchorMin = new Vector2(0f, 1f);
        tabsHeader.anchorMax = new Vector2(1f, 1f);
        tabsHeader.pivot = new Vector2(0.5f, 0f);
        tabsHeader.anchoredPosition = new Vector2(0f, -2f);
        tabsHeader.sizeDelta = new Vector2(-30f, 85f);

        // Tab 1: Daily Login Tab Button (Left half)
        GameObject dailyTabObj = CreateTabButton("DailyLoginTab", tabsHeader, new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(-6f, 0f), "Daily Login Reward", 32f, ActiveTabBg, WindowBorderColor, out Image dailyTabBg, out TMP_Text dailyTabTxt, out GameObject dailyTabDot);
        Button dailyTabBtn = dailyTabObj.GetComponent<Button>();

        // Tab 2: Achievements Tab Button (Right half)
        GameObject achTabObj = CreateTabButton("AchievementTab", tabsHeader, new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(6f, 0f), new Vector2(0f, 0f), "Achievements", 32f, InactiveTabBg, WindowBorderColor, out Image achTabBg, out TMP_Text achTabTxt, out GameObject achTabDot);
        Button achTabBtn = achTabObj.GetComponent<Button>();

        // E. Daily Login Panel
        GameObject dailyPanelObj = BuildDailyLoginPanel(windowObj.transform, out DailyLoginPanelUI dailyPanelUI);

        // F. Achievements Panel
        GameObject achPanelObj = BuildAchievementPanel(windowObj.transform, out AchievementPanelUI achPanelUI);

        // G. Gắn Controller chính RewardPopupController
        RewardPopupController popupCtrl = popupObj.GetComponent<RewardPopupController>() ?? popupObj.AddComponent<RewardPopupController>();
        popupCtrl.SetReferencesForBuilder(
            popupObj,
            dimBtn,
            null,
            dailyTabBtn,
            dailyTabBg,
            dailyTabTxt,
            dailyTabDot,
            achTabBtn,
            achTabBg,
            achTabTxt,
            achTabDot,
            dailyPanelObj,
            achPanelObj,
            dailyPanelUI,
            achPanelUI
        );

        // Mặc định ban đầu hiển thị Tab Daily Login
        popupCtrl.SwitchTab(0);
        popupObj.SetActive(false); // Ẩn mặc định khi vào game

        return popupObj;
    }

    private static GameObject BuildDailyLoginPanel(Transform parent, out DailyLoginPanelUI panelUI)
    {
        GameObject panelObj = CreateRect("DailyLoginPanel", parent).gameObject;
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        Stretch(panelRect, Vector2.zero, Vector2.one, new Vector2(16f, 20f), new Vector2(-16f, -30f));

        // ScrollRect
        ScrollRect scroll = panelObj.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.elasticity = 0.1f;
        scroll.scrollSensitivity = 25f;

        // Viewport
        RectTransform viewport = CreateRect("Viewport", panelObj.transform);
        Stretch(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        viewport.gameObject.AddComponent<RectMask2D>();
        scroll.viewport = viewport;

        // Content
        RectTransform content = CreateRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 1200f);
        scroll.content = content;

        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 16f;
        layout.padding = new RectOffset(10, 10, 15, 15);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Tạo sẵn 7 DailyLoginItemUI từ Day 01 đến Day 07
        DailyLoginItemUI[] items = new DailyLoginItemUI[7];
        for (int i = 0; i < 7; i++)
        {
            items[i] = CreateDailyLoginItem(content, i + 1);
        }

        panelUI = panelObj.AddComponent<DailyLoginPanelUI>();
        panelUI.SetReferencesForBuilder(scroll, content, items, energySprite, redGemSprite, dataChipSprite);

        return panelObj;
    }

    private static DailyLoginItemUI CreateDailyLoginItem(Transform parent, int dayIndex)
    {
        GameObject itemObj = CreateFrame($"Day{dayIndex:00}", parent, CardBgColor, CardBorderColor, out Image bg);
        RectTransform rect = itemObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(920f, 150f);
        LayoutElement le = itemObj.AddComponent<LayoutElement>();
        le.preferredHeight = 150f;
        le.minHeight = 150f;

        CanvasGroup cg = itemObj.AddComponent<CanvasGroup>();
        Image border = itemObj.GetComponent<Image>();

        // 1. Day Header Container (Bên trái)
        RectTransform dayHeader = CreateRect("DayHeader", itemObj.transform);
        dayHeader.anchorMin = new Vector2(0f, 0.5f);
        dayHeader.anchorMax = new Vector2(0f, 0.5f);
        dayHeader.pivot = new Vector2(0.5f, 0.5f);
        dayHeader.anchoredPosition = new Vector2(80f, 0f);
        dayHeader.sizeDelta = new Vector2(120f, 120f);

        TMP_Text dayLabel = CreateText("DayLabel", dayHeader, "DAY", 26f, TextGray, TextAlignmentOptions.Center);
        dayLabel.rectTransform.anchoredPosition = new Vector2(0f, 25f);
        dayLabel.rectTransform.sizeDelta = new Vector2(100f, 35f);

        TMP_Text dayNumber = CreateText("DayNumber", dayHeader, $"{dayIndex:00}", 48f, TextYellow, TextAlignmentOptions.Center);
        dayNumber.rectTransform.anchoredPosition = new Vector2(0f, -20f);
        dayNumber.rectTransform.sizeDelta = new Vector2(100f, 55f);

        // 2. Rewards Container (Ở giữa)
        RectTransform rewardsTr = CreateRect("RewardsContainer", itemObj.transform);
        rewardsTr.anchorMin = new Vector2(0f, 0.5f);
        rewardsTr.anchorMax = new Vector2(1f, 0.5f);
        rewardsTr.pivot = new Vector2(0f, 0.5f);
        rewardsTr.anchoredPosition = new Vector2(160f, 0f);
        rewardsTr.sizeDelta = new Vector2(-460f, 120f);

        HorizontalLayoutGroup rLayout = rewardsTr.gameObject.AddComponent<HorizontalLayoutGroup>();
        rLayout.spacing = 14f;
        rLayout.childAlignment = TextAnchor.MiddleLeft;
        rLayout.childControlWidth = false;
        rLayout.childControlHeight = false;

        // 3. Action / State Container (Bên phải)
        RectTransform stateRight = CreateRect("StateRight", itemObj.transform);
        stateRight.anchorMin = new Vector2(1f, 0.5f);
        stateRight.anchorMax = new Vector2(1f, 0.5f);
        stateRight.pivot = new Vector2(1f, 0.5f);
        stateRight.anchoredPosition = new Vector2(-25f, 0f);
        stateRight.sizeDelta = new Vector2(280f, 120f);

        // Button Get
        GameObject getBtnObj = CreateButton("ClaimButton", stateRight, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-120f, -42.5f), new Vector2(120f, 42.5f), "Get", 36f, GetBtnColor, WindowBorderColor, out _);
        Button claimBtn = getBtnObj.GetComponent<Button>();
        TMP_Text claimBtnTxt = getBtnObj.transform.Find("Label")?.GetComponent<TMP_Text>();

        // Obtained Tag
        GameObject obtainedRoot = CreateFrame("ObtainedRoot", stateRight, ObtainedBtnColor, new Color32(45, 65, 80, 255), out _);
        RectTransform obtRect = obtainedRoot.GetComponent<RectTransform>();
        obtRect.anchoredPosition = Vector2.zero;
        obtRect.sizeDelta = new Vector2(240f, 85f);
        TMP_Text obtainedTxt = CreateText("ObtainedLabel", obtainedRoot.transform, "Obtained", 34f, TextGray, TextAlignmentOptions.Center);
        Stretch(obtainedTxt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        obtainedRoot.SetActive(false);

        // Countdown Root (Time Remaining 15:26:01)
        GameObject countRoot = CreateRect("CountdownRoot", stateRight).gameObject;
        RectTransform countRect = countRoot.GetComponent<RectTransform>();
        countRect.anchoredPosition = Vector2.zero;
        countRect.sizeDelta = new Vector2(260f, 100f);

        TMP_Text countLbl = CreateText("CountdownLabel", countRoot.transform, "Time Remaining", 26f, TextWhite, TextAlignmentOptions.Center);
        countLbl.rectTransform.anchoredPosition = new Vector2(0f, 22f);
        countLbl.rectTransform.sizeDelta = new Vector2(260f, 40f);

        TMP_Text countTxt = CreateText("CountdownTime", countRoot.transform, "15:26:01", 34f, TextWhite, TextAlignmentOptions.Center);
        countTxt.rectTransform.anchoredPosition = new Vector2(0f, -22f);
        countTxt.rectTransform.sizeDelta = new Vector2(260f, 45f);
        countRoot.SetActive(false);

        DailyLoginItemUI itemUI = itemObj.AddComponent<DailyLoginItemUI>();
        itemUI.SetReferencesForBuilder(
            dayLabel,
            dayNumber,
            rewardsTr,
            claimBtn,
            claimBtnTxt,
            obtainedRoot,
            obtainedTxt,
            countRoot,
            countLbl,
            countTxt,
            bg,
            border,
            cg
        );

        return itemUI;
    }

    private static GameObject BuildAchievementPanel(Transform parent, out AchievementPanelUI panelUI)
    {
        GameObject panelObj = CreateRect("AchievementPanel", parent).gameObject;
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        Stretch(panelRect, Vector2.zero, Vector2.one, new Vector2(16f, 20f), new Vector2(-16f, -30f));

        ScrollRect scroll = panelObj.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.elasticity = 0.1f;
        scroll.scrollSensitivity = 25f;

        RectTransform viewport = CreateRect("Viewport", panelObj.transform);
        Stretch(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        viewport.gameObject.AddComponent<RectMask2D>();
        scroll.viewport = viewport;

        RectTransform content = CreateRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 1200f);
        scroll.content = content;

        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 16f;
        layout.padding = new RectOffset(10, 10, 15, 15);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Tạo 5 Achievement Item mẫu
        List<AchievementItemUI> items = new List<AchievementItemUI>();
        for (int i = 0; i < 5; i++)
        {
            items.Add(CreateAchievementItem(content, i));
        }

        panelUI = panelObj.AddComponent<AchievementPanelUI>();
        panelUI.SetReferencesForBuilder(scroll, content, items, energySprite, redGemSprite, dataChipSprite);

        return panelObj;
    }

    private static AchievementItemUI CreateAchievementItem(Transform parent, int index)
    {
        GameObject itemObj = CreateFrame($"AchievementItem_{index}", parent, CardBgColor, CardBorderColor, out Image bg);
        RectTransform rect = itemObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(920f, 210f);
        LayoutElement le = itemObj.AddComponent<LayoutElement>();
        le.preferredHeight = 210f;
        le.minHeight = 210f;

        Image border = itemObj.GetComponent<Image>();

        // 1. Title Text
        TMP_Text title = CreateText("TitleText", itemObj.transform, "Kill 2500 enemies", 36f, TextWhite, TextAlignmentOptions.Left);
        title.rectTransform.anchorMin = new Vector2(0f, 1f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.pivot = new Vector2(0f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(30f, -18f);
        title.rectTransform.sizeDelta = new Vector2(-360f, 45f);

        // 2. Progress Bar
        GameObject barBgObj = CreateFrame("ProgressBarBg", itemObj.transform, ProgressBgColor, WindowBorderColor, out Image barBg);
        RectTransform barRect = barBgObj.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0f, 1f);
        barRect.anchorMax = new Vector2(0f, 1f);
        barRect.pivot = new Vector2(0f, 1f);
        barRect.anchoredPosition = new Vector2(30f, -70f);
        barRect.sizeDelta = new Vector2(560f, 32f);

        // Progress Fill
        GameObject fillObj = CreateImage("ProgressFill", barBgObj.transform, ProgressFillColor, false).gameObject;
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        Image fillImg = fillObj.GetComponent<Image>();
        Stretch(fillRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // Progress Text (2025/2500)
        TMP_Text progressTxt = CreateText("ProgressText", itemObj.transform, "2025/2500", 26f, TextWhite, TextAlignmentOptions.Center);
        progressTxt.rectTransform.anchorMin = new Vector2(0f, 1f);
        progressTxt.rectTransform.anchorMax = new Vector2(0f, 1f);
        progressTxt.rectTransform.pivot = new Vector2(0.5f, 1f);
        progressTxt.rectTransform.anchoredPosition = new Vector2(310f, -104f);
        progressTxt.rectTransform.sizeDelta = new Vector2(560f, 30f);

        // 3. Rewards Container (Dưới thanh progress bar)
        RectTransform rewardsTr = CreateRect("RewardsContainer", itemObj.transform);
        rewardsTr.anchorMin = new Vector2(0f, 0f);
        rewardsTr.anchorMax = new Vector2(0f, 0f);
        rewardsTr.pivot = new Vector2(0f, 0f);
        rewardsTr.anchoredPosition = new Vector2(30f, 15f);
        rewardsTr.sizeDelta = new Vector2(560f, 65f);

        HorizontalLayoutGroup rLayout = rewardsTr.gameObject.AddComponent<HorizontalLayoutGroup>();
        rLayout.spacing = 14f;
        rLayout.childAlignment = TextAnchor.MiddleLeft;
        rLayout.childControlWidth = false;
        rLayout.childControlHeight = false;

        // 4. Action Button (Bên phải)
        GameObject btnObj = CreateButton("ActionButton", itemObj.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-280f, -47.5f), new Vector2(-30f, 47.5f), "Get", 38f, GetBtnColor, WindowBorderColor, out Image btnImg);
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(1f, 0.5f);
        btnRect.anchorMax = new Vector2(1f, 0.5f);
        btnRect.pivot = new Vector2(1f, 0.5f);
        btnRect.anchoredPosition = new Vector2(-30f, 0f);

        Button actBtn = btnObj.GetComponent<Button>();
        TMP_Text btnTxt = btnObj.transform.Find("Label")?.GetComponent<TMP_Text>();

        // Dot notification trên button
        GameObject dot = CreateImage("NotificationDot", btnObj.transform, new Color32(235, 60, 60, 255), false).gameObject;
        RectTransform dotRect = dot.GetComponent<RectTransform>();
        dotRect.anchorMin = new Vector2(1f, 1f);
        dotRect.anchorMax = new Vector2(1f, 1f);
        dotRect.pivot = new Vector2(0.5f, 0.5f);
        dotRect.anchoredPosition = new Vector2(-10f, -10f);
        dotRect.sizeDelta = new Vector2(22f, 22f);
        dot.SetActive(false);

        AchievementItemUI itemUI = itemObj.AddComponent<AchievementItemUI>();
        itemUI.SetReferencesForBuilder(
            title,
            progressTxt,
            fillImg,
            barBg,
            rewardsTr,
            actBtn,
            btnImg,
            btnTxt,
            dot,
            border,
            bg
        );

        return itemUI;
    }

    // =========================================================================
    // UI BUILDER HELPERS
    // =========================================================================

    private static GameObject CreateTabButton(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax,
        string label,
        float fontSize,
        Color fillColor,
        Color borderColor,
        out Image bg,
        out TMP_Text text,
        out GameObject dot)
    {
        GameObject btnObj = CreateFrame(name, parent, fillColor, borderColor, out bg);
        bg.raycastTarget = true;
        Image border = btnObj.GetComponent<Image>();
        if (border != null) border.raycastTarget = true;

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = bg;

        text = CreateText("Label", btnObj.transform, label, fontSize, ActiveTabText, TextAlignmentOptions.Center);
        text.raycastTarget = false;
        Stretch(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // Notification Dot
        dot = CreateImage("Badge", btnObj.transform, new Color32(235, 60, 60, 255), false).gameObject;
        RectTransform dotRect = dot.GetComponent<RectTransform>();
        dotRect.anchorMin = new Vector2(1f, 1f);
        dotRect.anchorMax = new Vector2(1f, 1f);
        dotRect.pivot = new Vector2(0.5f, 0.5f);
        dotRect.anchoredPosition = new Vector2(-15f, -15f);
        dotRect.sizeDelta = new Vector2(24f, 24f);
        dot.SetActive(false);

        return btnObj;
    }

    private static GameObject CreateButton(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax,
        string label,
        float fontSize,
        Color fillColor,
        Color borderColor,
        out Image background)
    {
        GameObject btnObj = CreateFrame(name, parent, fillColor, borderColor, out background);
        background.raycastTarget = true;
        Image border = btnObj.GetComponent<Image>();
        if (border != null) border.raycastTarget = true;

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = background;

        TMP_Text txt = CreateText("Label", btnObj.transform, label, fontSize, TextWhite, TextAlignmentOptions.Center);
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

        Image topHighlight = CreateImage("TopHighlight", root, new Color32(151, 240, 226, 75), false);
        Stretch(topHighlight.rectTransform, new Vector2(0.04f, 0.92f), new Vector2(0.96f, 0.96f), Vector2.zero, Vector2.zero);

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
        if (font != null) text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        text.outlineColor = NavyOutline;
        text.outlineWidth = 0.16f;
        return text;
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
