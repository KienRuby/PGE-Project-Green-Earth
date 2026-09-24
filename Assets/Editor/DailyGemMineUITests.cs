#if UNITY_EDITOR
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[TestFixture]
public class DailyGemMineUITests
{
    private const string MonthlyPremiumSpritePath = "Assets/Sprites/UI/GemMine/bg_monthly_premium.png";
    private const string PriceButtonSpritePath = "Assets/Sprites/UI/GemMine/btn_yellow_price.png";
    private const string PriceButtonPressedSpritePath = "Assets/Sprites/UI/GemMine/btn_yellow_price_pressed.png";
    private const string DailyGemMinePanelSpritePath = "Assets/Sprites/UI/GemMine/bg_daily_gem_mine_panel_clean.png";
    private const string Level1PreviewSpritePath = "Assets/Sprites/UI/GemMine/preview_level_01.png";
    private const string PinkStartSpritePath = "Assets/Sprites/UI/GemMine/btn_pink_start.png";
    private const string PinkStartPressedSpritePath = "Assets/Sprites/UI/GemMine/btn_pink_start_pressed.png";
    private const string Level2PreviewSpritePath = "Assets/Sprites/UI/GemMine/preview_level_02.png";
    private const string RedGemIconSpritePath = "Assets/Sprites/UI/GemMine/icon_red_gem.png";
    private const string RockIconSpritePath = "Assets/Sprites/UI/GemMine/icon_gem_mine_rock.png";

    [Test]
    public void GemMineSprites_AllExistInAssetDatabase()
    {
        Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>(MonthlyPremiumSpritePath), "Monthly Premium sprite must exist");
        Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>(PriceButtonSpritePath), "Price Button sprite must exist");
        Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>(PriceButtonPressedSpritePath), "Price Button pressed sprite must exist");
        Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>(DailyGemMinePanelSpritePath), "Daily Gem Mine Panel sprite must exist");
        Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>(Level1PreviewSpritePath), "Level 1 preview sprite must exist");
        Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>(PinkStartSpritePath), "Pink Start button sprite must exist");
        Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>(PinkStartPressedSpritePath), "Pink Start button pressed sprite must exist");
        Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>(Level2PreviewSpritePath), "Level 2 preview sprite must exist");
        Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>(RedGemIconSpritePath), "Red gem icon sprite must exist");
        Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>(RockIconSpritePath), "Rock icon sprite must exist");
    }

    [Test]
    public void DailyGemMineModalController_OpenAndCloseModal_TogglesCorrectly()
    {
        GameObject modalObj = new GameObject("TestModalRoot", typeof(RectTransform));
        DailyGemMineModalController ctrl = modalObj.AddComponent<DailyGemMineModalController>();

        ctrl.SetUIReferencesForTesting(modalObj, null, null, null, null, null);
        modalObj.SetActive(false);

        Assert.IsFalse(ctrl.IsOpen);

        ctrl.OpenModal();
        Assert.IsTrue(ctrl.IsOpen);

        ctrl.CloseModal();
        Assert.IsFalse(ctrl.IsOpen);

        Object.DestroyImmediate(modalObj);
    }

    [Test]
    public void DailyGemMineModalController_StatusTexts_FormatAccordingToImage2Pattern()
    {
        GameObject modalObj = new GameObject("TestModalRoot", typeof(RectTransform));
        DailyGemMineModalController ctrl = modalObj.AddComponent<DailyGemMineModalController>();

        GameObject timerObj = new GameObject("TimerText", typeof(RectTransform), typeof(TextMeshProUGUI));
        GameObject entranceObj = new GameObject("EntranceText", typeof(RectTransform), typeof(TextMeshProUGUI));
        TMP_Text timerTxt = timerObj.GetComponent<TextMeshProUGUI>();
        TMP_Text entranceTxt = entranceObj.GetComponent<TextMeshProUGUI>();

        ctrl.SetUIReferencesForTesting(modalObj, null, null, null, timerTxt, entranceTxt);

        // 1. Khi còn đủ 5 lượt: Không hiển thị Reset in time
        ctrl.SetRemainingTime(9, 26);
        ctrl.SetEntrances(5, 5);

        Assert.IsFalse(timerObj.activeSelf, "Timer must be hidden when entrances are full (5/5)");
        StringAssert.Contains("Entrance:", entranceTxt.text);
        StringAssert.Contains("5", entranceTxt.text);

        // 2. Khi đã chơi (-1 lượt -> 4 lượt): Hiển thị Reset in time
        ctrl.SetEntrances(4, 5);

        Assert.IsTrue(timerObj.activeSelf, "Timer must be visible when entrances < maxEntrances");
        StringAssert.Contains("09", timerTxt.text);
        StringAssert.Contains("26", timerTxt.text);
        StringAssert.Contains("Hour", timerTxt.text);
        StringAssert.Contains("Min Left", timerTxt.text);
        StringAssert.Contains("Entrance:", entranceTxt.text);
        StringAssert.Contains("4", entranceTxt.text);

        Object.DestroyImmediate(timerObj);
        Object.DestroyImmediate(entranceObj);
        Object.DestroyImmediate(modalObj);
    }

    [Test]
    public void DailyGemMineModalController_StartButton_DecrementsEntranceAndFiresEvent()
    {
        GameObject modalObj = new GameObject("TestModalRoot", typeof(RectTransform));
        DailyGemMineModalController ctrl = modalObj.AddComponent<DailyGemMineModalController>();

        GameObject startBtnObj = new GameObject("StartBtn", typeof(RectTransform), typeof(Button));
        Button startBtn = startBtnObj.GetComponent<Button>();

        ctrl.SetUIReferencesForTesting(modalObj, null, startBtn, null, null, null);
        ctrl.SetEntrances(5, 5);

        int levelStarted = -1;
        ctrl.OnLevelStarted += lvl => levelStarted = lvl;

        ctrl.OnStartLevel1Clicked();

        Assert.AreEqual(1, levelStarted);
        Assert.AreEqual(4, ctrl.RemainingEntrances);
        Assert.AreEqual("goalkeeper", ctrl.GemMineSceneName);

        Object.DestroyImmediate(startBtnObj);
        Object.DestroyImmediate(modalObj);
    }

    [Test]
    public void DailyGemMineModalController_MonthlyPremiumButton_FiresEvent()
    {
        GameObject modalObj = new GameObject("TestModalRoot", typeof(RectTransform));
        DailyGemMineModalController ctrl = modalObj.AddComponent<DailyGemMineModalController>();

        GameObject premiumBtnObj = new GameObject("PremiumBtn", typeof(RectTransform), typeof(Button));
        Button premiumBtn = premiumBtnObj.GetComponent<Button>();

        ctrl.SetUIReferencesForTesting(modalObj, premiumBtn, null, null, null, null);

        bool premiumPurchased = false;
        ctrl.OnMonthlyPremiumPurchased += () => premiumPurchased = true;

        ctrl.OnMonthlyPremiumClicked();

        Assert.IsTrue(premiumPurchased);

        Object.DestroyImmediate(premiumBtnObj);
        Object.DestroyImmediate(modalObj);
    }

    [Test]
    public void ChapterScreenController_GemMineButton_OpensDailyGemMineModal()
    {
        GameObject chapterObj = new GameObject("ChapterPanelTest", typeof(RectTransform), typeof(ChapterScreenController));
        ChapterScreenController chapterCtrl = chapterObj.GetComponent<ChapterScreenController>();

        GameObject gemMineBtnObj = new GameObject("GemMineBtn", typeof(RectTransform), typeof(Button));
        Button gemMineBtn = gemMineBtnObj.GetComponent<Button>();

        GameObject modalObj = new GameObject("GemMineModal", typeof(RectTransform), typeof(DailyGemMineModalController));
        DailyGemMineModalController modalCtrl = modalObj.GetComponent<DailyGemMineModalController>();
        modalCtrl.SetUIReferencesForTesting(modalObj, null, null, null, null, null);
        modalObj.SetActive(false);

        chapterCtrl.SetSideModeButtonsForTesting(null, gemMineBtn);
        chapterCtrl.SetGemMineModalForTesting(modalCtrl);

        Assert.IsFalse(modalCtrl.IsOpen);

        chapterCtrl.OnGemMineClicked();

        Assert.IsTrue(modalCtrl.IsOpen);

        Object.DestroyImmediate(modalObj);
        Object.DestroyImmediate(gemMineBtnObj);
        Object.DestroyImmediate(chapterObj);
    }

    [Test]
    public void DailyGemMineModalController_ZeroEntrances_DisablesStartButtonAndPreventsLevelStart()
    {
        GameObject modalObj = new GameObject("TestModalRoot", typeof(RectTransform));
        DailyGemMineModalController ctrl = modalObj.AddComponent<DailyGemMineModalController>();

        GameObject startBtnObj = new GameObject("StartBtn", typeof(RectTransform), typeof(Button));
        Button startBtn = startBtnObj.GetComponent<Button>();

        ctrl.SetUIReferencesForTesting(modalObj, null, startBtn, null, null, null);
        ctrl.SetEntrances(0, 5);

        Assert.IsFalse(startBtn.interactable, "Start button must be disabled when entrances == 0");

        int levelStarted = -1;
        ctrl.OnLevelStarted += lvl => levelStarted = lvl;

        ctrl.OnStartLevel1Clicked();

        Assert.AreEqual(-1, levelStarted, "Level must NOT start when entrances == 0");
        Assert.AreEqual(0, ctrl.RemainingEntrances);

        Object.DestroyImmediate(startBtnObj);
        Object.DestroyImmediate(modalObj);
    }

    [Test]
    public void DailyGemMineModalController_SanitizeMaterialsAndEffects_CleansDissolveMaterial()
    {
        GameObject modalObj = new GameObject("TestModalRoot", typeof(RectTransform), typeof(DailyGemMineModalController));
        DailyGemMineModalController ctrl = modalObj.GetComponent<DailyGemMineModalController>();

        GameObject childImageObj = new GameObject("ChildImage", typeof(RectTransform), typeof(Image));
        childImageObj.transform.SetParent(modalObj.transform);
        Image img = childImageObj.GetComponent<Image>();

        Shader dissolveShader = Shader.Find("Custom/UI/UIDissolve");
        if (dissolveShader != null)
        {
            img.material = new Material(dissolveShader);
            Assert.IsNotNull(img.material);
            Assert.IsTrue(img.material.shader.name.Contains("Dissolve"));

            ctrl.SanitizeMaterialsAndEffects();

            Assert.IsNull(img.material, "SanitizeMaterialsAndEffects must reset Dissolve material to null (default UI material)");
        }

        Object.DestroyImmediate(modalObj);
    }

    [Test]
    public void DailyGemMineLayoutTuner_ApplyLayout_SynchronizesPositionsAndSizes()
    {
        GameObject modalObj = new GameObject("TestModalRoot", typeof(RectTransform));
        DailyGemMineLayoutTuner tuner = modalObj.AddComponent<DailyGemMineLayoutTuner>();

        GameObject contentObj = new GameObject("ContentRoot", typeof(RectTransform));
        contentObj.transform.SetParent(modalObj.transform);

        GameObject bannerObj = new GameObject("MonthlyPremiumBanner", typeof(RectTransform));
        bannerObj.transform.SetParent(contentObj.transform);

        GameObject panelObj = new GameObject("DailyGemMinePanel", typeof(RectTransform));
        panelObj.transform.SetParent(contentObj.transform);

        tuner.contentAnchoredPosition = new Vector2(10f, 20f);
        tuner.contentSizeDelta = new Vector2(950f, 1500f);
        tuner.bannerAnchoredPosition = new Vector2(0f, -5f);
        tuner.bannerSizeDelta = new Vector2(950f, 280f);

        tuner.ApplyLayout();

        RectTransform contentRect = contentObj.GetComponent<RectTransform>();
        RectTransform bannerRect = bannerObj.GetComponent<RectTransform>();

        Assert.AreEqual(new Vector2(10f, 20f), contentRect.anchoredPosition);
        Assert.AreEqual(new Vector2(950f, 1500f), contentRect.sizeDelta);
        Assert.AreEqual(new Vector2(0f, -5f), bannerRect.anchoredPosition);
        Assert.AreEqual(new Vector2(950f, 280f), bannerRect.sizeDelta);

        Object.DestroyImmediate(modalObj);
    }

    [Test]
    public void DailyGemMineLayoutTuner_PlayModeSync_StatusTextsReadFromTuner()
    {
        GameObject modalObj = new GameObject("TestModalRoot", typeof(RectTransform));
        DailyGemMineModalController ctrl = modalObj.AddComponent<DailyGemMineModalController>();
        DailyGemMineLayoutTuner tuner = modalObj.AddComponent<DailyGemMineLayoutTuner>();

        GameObject timerObj = new GameObject("TimerText", typeof(RectTransform), typeof(TextMeshProUGUI));
        GameObject entranceObj = new GameObject("EntranceText", typeof(RectTransform), typeof(TextMeshProUGUI));
        TMP_Text timerTxt = timerObj.GetComponent<TextMeshProUGUI>();
        TMP_Text entranceTxt = entranceObj.GetComponent<TextMeshProUGUI>();

        tuner.resetTimerPosition = new Vector2(5f, -230f);
        tuner.entrancePositionActive = new Vector2(5f, -280f);
        tuner.resetTimerFontSize = 36f;
        tuner.entranceFontSize = 36f;

        ctrl.SetUIReferencesForTesting(modalObj, null, null, null, timerTxt, entranceTxt);
        ctrl.SetEntrances(4, 5); // < 5 lượt -> Hiển thị cả 2 text

        Assert.AreEqual(new Vector2(5f, -230f), timerTxt.rectTransform.anchoredPosition);
        Assert.AreEqual(new Vector2(5f, -280f), entranceTxt.rectTransform.anchoredPosition);
        Assert.AreEqual(36f, timerTxt.fontSize);
        Assert.AreEqual(36f, entranceTxt.fontSize);

        Object.DestroyImmediate(timerObj);
        Object.DestroyImmediate(entranceObj);
        Object.DestroyImmediate(modalObj);
    }

    [Test]
    public void DailyGemMineModalController_FiveLevelCards_AllWorkAndLockWhenZeroEntrances()
    {
        string compKey = "PGE.DailyGemMine.HighestCompletedLevel";
        int savedComp = PlayerPrefs.GetInt(compKey, 0);
        PlayerPrefs.DeleteKey(compKey);

        try
        {
            GameObject modalObj = new GameObject("TestModalRoot", typeof(RectTransform));
            DailyGemMineModalController ctrl = modalObj.AddComponent<DailyGemMineModalController>();

            DailyGemMineLevelCard[] cards = new DailyGemMineLevelCard[5];
            Button[] buttons = new Button[5];

            for (int i = 0; i < 5; i++)
            {
                GameObject cardObj = new GameObject($"Card_{i + 1}", typeof(RectTransform));
                cardObj.transform.SetParent(modalObj.transform);
                DailyGemMineLevelCard card = cardObj.AddComponent<DailyGemMineLevelCard>();

                GameObject btnObj = new GameObject("StartButton", typeof(RectTransform), typeof(Button));
                btnObj.transform.SetParent(cardObj.transform);
                Button btn = btnObj.GetComponent<Button>();

                card.SetReferencesForTesting(i + 1, btn, null, null);
                cards[i] = card;
                buttons[i] = btn;
            }

            ctrl.SetUIReferencesForTesting(modalObj, null, null, null, null, null, cards);
            ctrl.SetEntrances(5, 5);

            // 1. Mặc định chỉ mở màn 1 có nút Start; các màn 2, 3, 4, 5 bị ẩn nút Start (Locked)
            Assert.IsTrue(buttons[0].gameObject.activeSelf, "Level 1 Start button must be active initially");
            Assert.IsFalse(buttons[1].gameObject.activeSelf, "Level 2 Start button must be inactive (locked) initially");
            Assert.IsFalse(buttons[2].gameObject.activeSelf, "Level 3 Start button must be inactive (locked) initially");

            // 2. Vượt qua màn 1 -> Mở khóa màn 2
            ctrl.CompleteLevel(1);
            Assert.IsTrue(buttons[1].gameObject.activeSelf, "Level 2 Start button must become active after clearing Level 1");
            Assert.IsFalse(buttons[2].gameObject.activeSelf, "Level 3 Start button must remain inactive");

            // 3. Mở khóa toàn bộ và test bấm nút + trừ lượt
            DailyGemMineProgress.HighestCompletedLevel = 4;
            ctrl.RefreshLevelCardsState();

            for (int i = 0; i < 5; i++)
            {
                Assert.IsTrue(buttons[i].gameObject.activeSelf, $"Level {i + 1} button must be active when all unlocked");
                Assert.IsTrue(buttons[i].interactable, $"Level {i + 1} button must be interactable when entrances = 5");
            }

            int startedLevel = -1;
            ctrl.OnLevelStarted += lvl => startedLevel = lvl;

            buttons[2].onClick.Invoke();

            Assert.AreEqual(3, startedLevel, "Clicking card 3 must trigger Level 3 start");
            Assert.AreEqual(4, ctrl.RemainingEntrances, "Remaining entrances must decrement to 4");

            // 4. Khi số lượt về 0 -> Toàn bộ các nút đều bị vô hiệu hóa
            ctrl.SetEntrances(0, 5);
            for (int i = 0; i < 5; i++)
            {
                Assert.IsFalse(buttons[i].interactable, $"Level {i + 1} button must be disabled when entrances = 0");
            }

            Object.DestroyImmediate(modalObj);
        }
        finally
        {
            PlayerPrefs.SetInt(compKey, savedComp);
            PlayerPrefs.Save();
        }
    }

    [Test]
    public void DailyGemMineLevelCard_RemoveRedundantStartLabel_DestroysChildStartLabel()
    {
        GameObject cardObj = new GameObject("TestCard", typeof(RectTransform));
        DailyGemMineLevelCard card = cardObj.AddComponent<DailyGemMineLevelCard>();

        GameObject btnObj = new GameObject("StartButton", typeof(RectTransform), typeof(Button));
        btnObj.transform.SetParent(cardObj.transform);
        Button btn = btnObj.GetComponent<Button>();

        // Giả lập text Start thừa gắn trên StartButton
        GameObject startLabelObj = new GameObject("StartLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        startLabelObj.transform.SetParent(btnObj.transform);

        card.SetReferencesForTesting(1, btn, null, null);
        card.RemoveRedundantStartLabel();

        Assert.IsNull(btnObj.transform.Find("StartLabel"), "Child StartLabel must be destroyed");

        Object.DestroyImmediate(cardObj);
    }

    [Test]
    public void DailyGemMineModalController_EnsureLevelsScrollView_CreatesScrollViewWhenMissing()
    {
        GameObject modalObj = new GameObject("TestModalRoot", typeof(RectTransform));
        DailyGemMineModalController ctrl = modalObj.AddComponent<DailyGemMineModalController>();

        GameObject panelObj = new GameObject("DailyGemMinePanel", typeof(RectTransform));
        panelObj.transform.SetParent(modalObj.transform);

        ctrl.EnsureLevelsScrollView();

        Assert.IsNotNull(ctrl.LevelsScrollRect, "EnsureLevelsScrollView must create LevelsScrollRect when missing");
        Assert.IsNotNull(ctrl.LevelsScrollRect.viewport, "LevelsScrollRect must have a Viewport");
        Assert.IsNotNull(ctrl.LevelsScrollRect.content, "LevelsScrollRect must have Content");
        Assert.IsNotNull(ctrl.LevelCards, "levelCards array must be populated");
        Assert.AreEqual(5, ctrl.LevelCards.Length, "Must generate exactly 5 Level cards");

        Object.DestroyImmediate(modalObj);
    }

    [Test]
    public void DailyGemMineModalController_ScrollViewSwipeConfiguration_MatchesTowerDef()
    {
        GameObject modalObj = new GameObject("TestModalRoot", typeof(RectTransform));
        DailyGemMineModalController ctrl = modalObj.AddComponent<DailyGemMineModalController>();

        GameObject panelObj = new GameObject("DailyGemMinePanel", typeof(RectTransform));
        panelObj.transform.SetParent(modalObj.transform);

        ctrl.EnsureLevelsScrollView();
        ScrollRect sr = ctrl.LevelsScrollRect;

        Assert.IsNotNull(sr);
        Assert.IsTrue(sr.vertical, "ScrollView must allow vertical swiping/scrolling");
        Assert.IsFalse(sr.horizontal, "ScrollView must disable horizontal scrolling");
        Assert.AreEqual(ScrollRect.MovementType.Clamped, sr.movementType, "MovementType must be Clamped matching Tower Def");
        Assert.IsTrue(sr.inertia, "Inertia must be enabled for smooth glide");
        Assert.AreEqual(0.135f, sr.decelerationRate, 0.001f, "Deceleration rate must be 0.135 for smooth swipe feel");
        Assert.AreEqual(40f, sr.scrollSensitivity, 0.001f, "Scroll sensitivity must be 40f");

        Object.DestroyImmediate(modalObj);
    }

    [Test]
    public void DailyGemMineModalController_SwipeRaycastOptimization_ViewportCatchesAndCardsPassThrough()
    {
        GameObject modalObj = new GameObject("TestModalRoot", typeof(RectTransform));
        DailyGemMineModalController ctrl = modalObj.AddComponent<DailyGemMineModalController>();

        GameObject panelObj = new GameObject("DailyGemMinePanel", typeof(RectTransform));
        panelObj.transform.SetParent(modalObj.transform);

        ctrl.EnsureLevelsScrollView();

        // 1. Viewport phải có Image raycastTarget = true để bắt trọn mọi cử chỉ vuốt
        Image vpImg = ctrl.LevelsScrollRect.viewport.GetComponent<Image>();
        Assert.IsNotNull(vpImg, "Viewport must have an Image component");
        Assert.IsTrue(vpImg.raycastTarget, "Viewport Image raycastTarget must be true to receive swipe gestures");

        // 2. Thẻ previewImage phải có raycastTarget = false để truyền vuốt thẳng cho Viewport
        for (int i = 0; i < ctrl.LevelCards.Length; i++)
        {
            var card = ctrl.LevelCards[i];
            Assert.IsNotNull(card);
            if (card.PreviewImage != null)
            {
                Assert.IsFalse(card.PreviewImage.raycastTarget, $"Card {i + 1} PreviewImage raycastTarget must be false");
            }

            // Nút Start phải có raycastTarget = true để nhận click
            Assert.IsNotNull(card.StartButton, $"Card {i + 1} must have a StartButton");
            Assert.IsTrue(card.StartButton.targetGraphic.raycastTarget, $"Card {i + 1} StartButton graphic raycastTarget must be true");
        }

        Object.DestroyImmediate(modalObj);
    }

    [Test]
    public void DailyGemMineModalController_SelectLevelAndSelectedLevelProperty_SavesAndDecrementsEntrance()
    {
        string compKey = "PGE.DailyGemMine.HighestCompletedLevel";
        int savedComp = PlayerPrefs.GetInt(compKey, 0);
        DailyGemMineProgress.HighestCompletedLevel = 4;

        try
        {
            GameObject modalObj = new GameObject("TestModalRoot", typeof(RectTransform));
            DailyGemMineModalController ctrl = modalObj.AddComponent<DailyGemMineModalController>();

            GameObject panelObj = new GameObject("DailyGemMinePanel", typeof(RectTransform));
            panelObj.transform.SetParent(modalObj.transform);

            ctrl.EnsureLevelsScrollView();
            ctrl.SetEntrances(5, 5);

            // 1. Test SelectedLevel property
            ctrl.SelectedLevel = 4;
            Assert.AreEqual(4, ctrl.SelectedLevel);

            // 2. Bấm nút Level 4 -> Trừ lượt, cập nhật SelectedLevel và kích hoạt OnLevelStarted
            int startedLvl = -1;
            ctrl.OnLevelStarted += lvl => startedLvl = lvl;

            ctrl.LevelCards[3].StartButton.onClick.Invoke();

            Assert.AreEqual(4, startedLvl, "Starting Level 4 must fire OnLevelStarted(4)");
            Assert.AreEqual(4, ctrl.SelectedLevel, "SelectedLevel must equal 4");
            Assert.AreEqual(4, ctrl.RemainingEntrances, "Remaining entrances must decrement to 4");

            // 3. ScrollToLevel
            ctrl.ScrollToLevel(5);
            Assert.AreEqual(0f, ctrl.LevelsScrollRect.verticalNormalizedPosition, 0.05f, "ScrollToLevel(5) must scroll to bottom (near 0)");

            ctrl.ScrollToLevel(1);
            Assert.AreEqual(1f, ctrl.LevelsScrollRect.verticalNormalizedPosition, 0.05f, "ScrollToLevel(1) must scroll to top (near 1)");

            Object.DestroyImmediate(modalObj);
        }
        finally
        {
            PlayerPrefs.SetInt(compKey, savedComp);
            PlayerPrefs.Save();
        }
    }

    [Test]
    public void DailyGemMineModalController_Viewport_UsesRectMask2DAndUIHierarchyLayer()
    {
        GameObject modalObj = new GameObject("TestModalRoot", typeof(RectTransform));
        DailyGemMineModalController ctrl = modalObj.AddComponent<DailyGemMineModalController>();

        GameObject panelObj = new GameObject("DailyGemMinePanel", typeof(RectTransform));
        panelObj.transform.SetParent(modalObj.transform);

        ctrl.EnsureLevelsScrollView();

        // 1. Viewport phải dùng RectMask2D, không dùng Mask (tránh lỗi stencil alpha 0 làm tàng hình toàn bộ thẻ màn)
        Assert.IsNotNull(ctrl.LevelsScrollRect.viewport.GetComponent<RectMask2D>(), "Viewport must have RectMask2D");
        Assert.IsNull(ctrl.LevelsScrollRect.viewport.GetComponent<Mask>(), "Viewport must NOT have legacy Mask");

        // 2. Toàn bộ hệ thống ScrollView và Cards phải nằm ở layer UI
        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer == -1) uiLayer = 5;
        Assert.AreEqual(uiLayer, ctrl.LevelsScrollRect.gameObject.layer, "ScrollView must be on UI layer");
        Assert.AreEqual(uiLayer, ctrl.LevelsScrollRect.viewport.gameObject.layer, "Viewport must be on UI layer");
        Assert.AreEqual(uiLayer, ctrl.LevelsScrollRect.content.gameObject.layer, "Content must be on UI layer");

        for (int i = 0; i < ctrl.LevelCards.Length; i++)
        {
            Assert.AreEqual(uiLayer, ctrl.LevelCards[i].gameObject.layer, $"Card {i + 1} must be on UI layer");
        }

        Object.DestroyImmediate(modalObj);
    }

    [Test]
    public void DailyGemMineLayoutTuner_ApplyLayout_UpdatesCardHeaderAndReward()
    {
        GameObject modalObj = new GameObject("TestModalRoot", typeof(RectTransform));
        DailyGemMineModalController ctrl = modalObj.AddComponent<DailyGemMineModalController>();
        DailyGemMineLayoutTuner tuner = modalObj.AddComponent<DailyGemMineLayoutTuner>();

        GameObject panelObj = new GameObject("DailyGemMinePanel", typeof(RectTransform));
        panelObj.transform.SetParent(modalObj.transform);

        ctrl.EnsureLevelsScrollView();

        tuner.titleFontSize = 38f;
        tuner.rewardFontSize = 34f;
        tuner.headerHeight = 75f;
        tuner.ApplyLayout();

        Transform card1 = ctrl.LevelsScrollRect.content.GetChild(0);
        Transform hb = card1.Find("HeaderBanner");
        Assert.IsNotNull(hb);
        Assert.AreEqual(75f, ((RectTransform)hb).sizeDelta.y);

        TMP_Text title = hb.Find("TitleText").GetComponent<TMP_Text>();
        Assert.AreEqual(38f, title.fontSize);

        TMP_Text reward = hb.Find("RewardText").GetComponent<TMP_Text>();
        Assert.AreEqual(34f, reward.fontSize);

        Object.DestroyImmediate(modalObj);
    }
}
#endif
