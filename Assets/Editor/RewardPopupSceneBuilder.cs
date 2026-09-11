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
[InitializeOnLoad]
public static class RewardPopupSceneBuilder
{
    private const string AutoBuildFlagPath = "Temp/RunRewardPopupBuild.flag";

    static RewardPopupSceneBuilder()
    {
        EditorApplication.delayCall += CheckAutoBuildFlag;
    }

    private static void CheckAutoBuildFlag()
    {
        if (File.Exists(AutoBuildFlagPath))
        {
            try
            {
                File.Delete(AutoBuildFlagPath);
            }
            catch {}

            BuildRewardPopupScene();
        }
    }

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
    private static Sprite btnGetSprite;
    private static Sprite btnClaimAgainSprite;
    private static Sprite btnObtainedSprite;
    private static Sprite btnNotAchievedSprite;
    private static Sprite achievementBannerSprite;
    private static Sprite dailyBannerBlue;
    private static Sprite dailyBannerGrey;
    private static Sprite progressBarBgSprite;
    private static Sprite progressBarFillSprite;

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

        Sprite redGemExtracted = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Reward/Extracted/Icon_Red_Gem.png");
        if (redGemExtracted != null) redGemSprite = redGemExtracted;
        Sprite dataChipExtracted = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Reward/Extracted/Icon_Data_Chip.png");
        if (dataChipExtracted != null) dataChipSprite = dataChipExtracted;
        Sprite energyExtracted = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Reward/Extracted/Icon_Energy.png");
        if (energyExtracted != null) energySprite = energyExtracted;

        btnGetSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Reward/Extracted/Btn_Get.png");
        btnClaimAgainSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Reward/Extracted/Btn_Claim_Again.png");
        btnObtainedSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Reward/Extracted/Btn_Obtained.png");
        btnNotAchievedSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Reward/Extracted/Btn_Not_Achieved.png");
        achievementBannerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Reward/Extracted/Row_Banner_Achievement.png");
        dailyBannerBlue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Reward/Extracted/Row_Banner_Blue.png");
        dailyBannerGrey = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Reward/Extracted/Row_Banner_Grey.png");
        progressBarBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Reward/Extracted/Progress_Bar_Bg.png");
        progressBarFillSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Reward/Extracted/Progress_Bar_Fill.png");

        if (btnGetSprite == null || btnClaimAgainSprite == null || btnObtainedSprite == null)
        {
            Sprite[] loginSprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/UI/Reward/nút daily login.png")
                ?.OfType<Sprite>().ToArray();
            if (loginSprites != null)
            {
                foreach (var s in loginSprites)
                {
                    if (btnGetSprite == null && s.name == "Btn_Get") btnGetSprite = s;
                    else if (btnClaimAgainSprite == null && s.name == "Btn_Claim_Again") btnClaimAgainSprite = s;
                    else if (btnObtainedSprite == null && s.name == "Btn_Obtained") btnObtainedSprite = s;
                }
            }
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

        // Tab 1: Daily Login Tab Button (Left half) - Inactive
        GameObject dailyTabObj = CreateTabButton("DailyLoginTab", tabsHeader, new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(-6f, 0f), "Daily Login", 32f, InactiveTabBg, WindowBorderColor, out Image dailyTabBg, out TMP_Text dailyTabTxt, out GameObject dailyTabDot);
        dailyTabTxt.color = InactiveTabText;
        Button dailyTabBtn = dailyTabObj.GetComponent<Button>();

        // Tab 2: Achievements Tab Button (Right half) - Active theo đúng Image 1
        GameObject achTabObj = CreateTabButton("AchievementTab", tabsHeader, new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(6f, 0f), new Vector2(0f, 0f), "Achievements", 32f, ActiveTabBg, WindowBorderColor, out Image achTabBg, out TMP_Text achTabTxt, out GameObject achTabDot);
        achTabTxt.color = ActiveTabText;
        Button achTabBtn = achTabObj.GetComponent<Button>();

        // E. Daily Login Panel (Inactive theo Image 1)
        GameObject dailyPanelObj = BuildDailyLoginPanel(windowObj.transform, out DailyLoginPanelUI dailyPanelUI);
        dailyPanelObj.SetActive(false);

        // F. Achievements Panel (Active theo Image 1)
        GameObject achPanelObj = BuildAchievementPanel(windowObj.transform, out AchievementPanelUI achPanelUI);
        achPanelObj.SetActive(true);

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

        // Mặc định ban đầu hiển thị Tab Achievements theo Image 1
        popupCtrl.EnsureTabSpritesLoaded();
        popupCtrl.SwitchTab(1, animated: false);
        popupObj.SetActive(true); // Hiển thị trong Edit mode để xem trước giống y đúc Image 1

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
        le.preferredWidth = 948f;
        le.minWidth = 948f;
        le.preferredHeight = 150f;
        le.minHeight = 150f;

        CanvasGroup cg = itemObj.AddComponent<CanvasGroup>();
        cg.alpha = 1.0f;
        Image border = itemObj.GetComponent<Image>();
        if (border != null) border.color = Color.clear;

        // Nút sáng (dailyBannerBlue) LUÔN LUÔN là background chính cho mọi ngày
        if (dailyBannerBlue != null)
        {
            bg.sprite = dailyBannerBlue;
            bg.color = Color.white;
        }

        // Tạo DarkOverlay đè lên Background bằng dailyBannerGrey
        GameObject overlayObj = new GameObject("DarkOverlay", typeof(RectTransform), typeof(Image));
        overlayObj.transform.SetParent(bg.transform, false);
        RectTransform overlayRt = overlayObj.GetComponent<RectTransform>();
        Stretch(overlayRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image overlayImg = overlayObj.GetComponent<Image>();
        overlayImg.sprite = dailyBannerGrey;
        overlayImg.color = Color.white;
        overlayImg.raycastTarget = false;
        overlayObj.SetActive(dayIndex == 1 || dayIndex == 2);

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
        rLayout.spacing = 18f;
        rLayout.childAlignment = TextAnchor.MiddleLeft;
        rLayout.childControlWidth = false;
        rLayout.childControlHeight = false;

        // Thứ tự & số lượng chuẩn 100% theo ảnh mẫu (Pin x30, Chip x300, Gem x1000)
        (Sprite sprite, string amount)[] defaultRewards = new[]
        {
            (energySprite, "x30"),
            (dataChipSprite, "x300"),
            (redGemSprite, "x1000")
        };

        for (int r = 0; r < defaultRewards.Length; r++)
        {
            var rw = defaultRewards[r];
            GameObject badge = new GameObject($"RewardBadge_{r}", typeof(RectTransform), typeof(Image));
            badge.transform.SetParent(rewardsTr, false);
            RectTransform badgeRt = badge.GetComponent<RectTransform>();
            badgeRt.sizeDelta = new Vector2(75f, 75f);
            Image badgeBg = badge.GetComponent<Image>();
            badgeBg.color = new Color32(11, 45, 60, 0); // alpha = 0 theo yêu cầu người dùng
            badgeBg.raycastTarget = false;

            // Icon
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(badge.transform, false);
            RectTransform iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = new Vector2(0f, 8f);
            iconRt.sizeDelta = new Vector2(45f, 45f);
            Image iconImg = iconObj.GetComponent<Image>();
            iconImg.sprite = rw.sprite;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            // Amount Text
            TMP_Text amtTxt = CreateText("AmountText", badge.transform, rw.amount, 20f, TextWhite, TextAlignmentOptions.Center);
            amtTxt.rectTransform.anchorMin = new Vector2(0f, 0f);
            amtTxt.rectTransform.anchorMax = new Vector2(1f, 0f);
            amtTxt.rectTransform.pivot = new Vector2(0.5f, 0f);
            amtTxt.rectTransform.anchoredPosition = new Vector2(0f, -22f);
            amtTxt.rectTransform.sizeDelta = new Vector2(90f, 24f);
            amtTxt.fontStyle = FontStyles.Bold;
        }

        // 3. Action / State Container (Bên phải)
        RectTransform stateRight = CreateRect("StateRight", itemObj.transform);
        stateRight.anchorMin = new Vector2(1f, 0.5f);
        stateRight.anchorMax = new Vector2(1f, 0.5f);
        stateRight.pivot = new Vector2(1f, 0.5f);
        stateRight.anchoredPosition = new Vector2(-25f, 0f);
        stateRight.sizeDelta = new Vector2(280f, 120f);

        // Button Get / Claim Again / Obtained
        GameObject getBtnObj = new GameObject("ClaimButton", typeof(RectTransform), typeof(Image), typeof(Button));
        getBtnObj.transform.SetParent(stateRight, false);
        RectTransform btnRect = getBtnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = Vector2.zero;
        btnRect.sizeDelta = new Vector2(240f, 105f);

        Image btnImg = getBtnObj.GetComponent<Image>();
        btnImg.preserveAspect = true;
        btnImg.color = Color.white;

        Button claimBtn = getBtnObj.GetComponent<Button>();
        claimBtn.targetGraphic = btnImg;
        var colors = claimBtn.colors;
        colors.disabledColor = Color.white;
        claimBtn.colors = colors;

        if (dayIndex == 1 || dayIndex == 2)
        {
            btnImg.sprite = btnObtainedSprite;
            claimBtn.interactable = false;
        }
        else if (dayIndex == 3)
        {
            // Day 03 hiển thị nút Claim again theo đúng ảnh mẫu
            btnImg.sprite = btnClaimAgainSprite != null ? btnClaimAgainSprite : btnGetSprite;
            claimBtn.interactable = true;
        }
        else
        {
            // Day 04..07 hiển thị nút Get theo đúng ảnh mẫu
            btnImg.sprite = btnGetSprite;
            claimBtn.interactable = true;
        }

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
            null,
            obtainedRoot,
            obtainedTxt,
            countRoot,
            countLbl,
            countTxt,
            bg,
            border,
            cg,
            btnGetSprite,
            btnClaimAgainSprite,
            btnObtainedSprite,
            overlayObj
        );

        return itemUI;
    }

    private struct AchievementPreviewData
    {
        public string id;
        public string title;
        public int current;
        public int target;
        public string progressText;
        public (Sprite sprite, string amount)[] rewards;
        public bool isClaimed;
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

        // Định nghĩa chính xác 5 thẻ Achievement theo đúng Image 1
        AchievementPreviewData[] previewItems = new AchievementPreviewData[]
        {
            new AchievementPreviewData
            {
                id = "chapter_play_15",
                title = "Play chapter 15 time(s)",
                current = 13,
                target = 15,
                progressText = "13/15",
                rewards = new (Sprite, string)[]
                {
                    (redGemSprite, "X200"),
                    (dataChipSprite, "X800")
                },
                isClaimed = false
            },
            new AchievementPreviewData
            {
                id = "enemy_kill_2500",
                title = "Kill 2500 enemies",
                current = 1406,
                target = 2500,
                progressText = "1406/2500",
                rewards = new (Sprite, string)[]
                {
                    (redGemSprite, "X200"),
                    (dataChipSprite, "X1.200")
                },
                isClaimed = false
            },
            new AchievementPreviewData
            {
                id = "chapter_clear_5",
                title = "Clear chapter 5",
                current = 2,
                target = 5,
                progressText = "2/5",
                rewards = new (Sprite, string)[]
                {
                    (redGemSprite, "X200"),
                    (dataChipSprite, "X2.000"),
                    (energySprite, "X10")
                },
                isClaimed = false
            },
            new AchievementPreviewData
            {
                id = "login_reward_2",
                title = "Get 2 times log in reward",
                current = 5,
                target = 2,
                progressText = "5/2",
                rewards = new (Sprite, string)[]
                {
                    (redGemSprite, "X200")
                },
                isClaimed = true
            },
            new AchievementPreviewData
            {
                id = "drone_upgrade_3",
                title = "Advance Drone Tier 3 time(s)",
                current = 3,
                target = 3,
                progressText = "3/3",
                rewards = new (Sprite, string)[]
                {
                    (redGemSprite, "X200"),
                    (dataChipSprite, "X1.000")
                },
                isClaimed = true
            }
        };

        List<AchievementItemUI> items = new List<AchievementItemUI>();
        for (int i = 0; i < previewItems.Length; i++)
        {
            items.Add(CreateAchievementItem(content, i, previewItems[i]));
        }

        panelUI = panelObj.AddComponent<AchievementPanelUI>();
        panelUI.SetReferencesForBuilder(scroll, content, items, energySprite, redGemSprite, dataChipSprite);

        return panelObj;
    }

    private static AchievementItemUI CreateAchievementItem(Transform parent, int index, AchievementPreviewData preview)
    {
        GameObject itemObj = CreateFrame($"AchievementItem_{index}", parent, CardBgColor, CardBorderColor, out Image bg);
        if (achievementBannerSprite != null)
        {
            bg.sprite = achievementBannerSprite;
            bg.color = Color.white;
        }
        RectTransform rect = itemObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(920f, 210f);
        LayoutElement le = itemObj.AddComponent<LayoutElement>();
        le.preferredWidth = 948f;
        le.minWidth = 948f;
        le.preferredHeight = 210f;
        le.minHeight = 210f;

        Image border = itemObj.GetComponent<Image>();
        if (achievementBannerSprite != null && border != null)
        {
            border.color = Color.white;
        }

        // 1. Title Text
        TMP_Text title = CreateText("TitleText", itemObj.transform, preview.title, 36f, TextWhite, TextAlignmentOptions.Left);
        title.rectTransform.anchorMin = new Vector2(0f, 1f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.pivot = new Vector2(0f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(30f, -18f);
        title.rectTransform.sizeDelta = new Vector2(-360f, 45f);

        // 2. Progress Bar
        GameObject barBgObj = CreateFrame("ProgressBarBg", itemObj.transform, ProgressBgColor, WindowBorderColor, out Image barBg);
        if (progressBarBgSprite != null)
        {
            barBg.sprite = progressBarBgSprite;
            barBg.color = Color.white;
        }
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
        if (progressBarFillSprite != null)
        {
            fillImg.sprite = progressBarFillSprite;
            fillImg.color = Color.white;
        }
        float fillRatio = Mathf.Clamp01((float)preview.current / preview.target);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(fillRatio, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        // Progress Text
        TMP_Text progressTxt = CreateText("ProgressText", itemObj.transform, preview.progressText, 26f, TextWhite, TextAlignmentOptions.Center);
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
        rLayout.spacing = preview.rewards.Length == 2 ? 180f : 45f;
        rLayout.childAlignment = TextAnchor.MiddleLeft;
        rLayout.childControlWidth = false;
        rLayout.childControlHeight = false;

        for (int r = 0; r < preview.rewards.Length; r++)
        {
            var rw = preview.rewards[r];
            GameObject badge = new GameObject($"RewardBadge_{r}", typeof(RectTransform), typeof(Image));
            badge.transform.SetParent(rewardsTr, false);
            RectTransform badgeRt = badge.GetComponent<RectTransform>();
            badgeRt.sizeDelta = new Vector2(75f, 75f);
            Image badgeBg = badge.GetComponent<Image>();
            badgeBg.color = new Color32(11, 45, 60, 0); // alpha = 0 theo yêu cầu người dùng
            badgeBg.raycastTarget = false;

            // Icon
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(badge.transform, false);
            RectTransform iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = new Vector2(0f, 10f);
            iconRt.sizeDelta = new Vector2(46f, 46f);
            Image iconImg = iconObj.GetComponent<Image>();
            iconImg.sprite = rw.sprite;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            // AmountText
            GameObject textObj = new GameObject("AmountText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(badge.transform, false);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0f, 0f);
            textRt.anchorMax = new Vector2(1f, 0.38f);
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            TextMeshProUGUI txt = textObj.GetComponent<TextMeshProUGUI>();
            if (font != null) txt.font = font;
            txt.text = rw.amount;
            txt.fontSize = 20f;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;
            txt.raycastTarget = false;
        }

        // 4. Action Button (Bên phải theo Image 1)
        Color btnFill = preview.isClaimed ? new Color32(78, 140, 147, 255) : new Color32(23, 68, 88, 255);
        Color btnBorderColor = preview.isClaimed ? new Color32(38, 77, 85, 255) : new Color32(11, 35, 48, 255);
        string btnLabel = preview.isClaimed ? "Obtained" : "Get";
        Color btnTextColor = preview.isClaimed ? new Color32(35, 80, 95, 255) : new Color32(35, 95, 120, 255);

        GameObject btnObj = CreateButton("ActionButton", itemObj.transform,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-280f, -47.5f), new Vector2(-30f, 47.5f),
            btnLabel, 38f, btnFill, btnBorderColor, out Image btnImg);
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(1f, 0.5f);
        btnRect.anchorMax = new Vector2(1f, 0.5f);
        btnRect.pivot = new Vector2(1f, 0.5f);
        btnRect.anchoredPosition = new Vector2(-30f, 0f);
        btnRect.sizeDelta = new Vector2(250f, 95f);

        Button actBtn = btnObj.GetComponent<Button>();
        actBtn.interactable = false;

        TMP_Text btnTxt = btnObj.transform.Find("Label")?.GetComponent<TMP_Text>();
        Sprite initialBtnSp = preview.isClaimed ? btnObtainedSprite : (preview.current >= preview.target ? btnGetSprite : (btnNotAchievedSprite ?? btnGetSprite));
        if (initialBtnSp != null)
        {
            btnImg.sprite = initialBtnSp;
            btnImg.color = Color.white;
            btnImg.preserveAspect = true;
            if (btnTxt != null) btnTxt.gameObject.SetActive(false);
            if (btnObj.TryGetComponent<Image>(out var borderImg)) borderImg.color = Color.clear;
        }
        else if (btnTxt != null)
        {
            btnTxt.gameObject.SetActive(true);
            btnTxt.text = btnLabel;
            btnTxt.color = btnTextColor;
        }

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
        itemUI.SetSprites(
            btnGetSprite,
            btnNotAchievedSprite,
            btnObtainedSprite,
            achievementBannerSprite,
            progressBarBgSprite,
            progressBarFillSprite
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
