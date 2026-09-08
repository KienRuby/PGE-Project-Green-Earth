using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class DailyLoginAchievementTests
{
    private int originalChips;
    private int originalGems;
    private int originalEnergy;
    private int originalStones;

    [SetUp]
    public void SetUp()
    {
        ChipManager.IsTestMode = false;
        originalChips = PlayerDataService.DataChips;
        originalGems = PlayerDataService.RedGems;
        originalEnergy = PlayerDataService.Energy;
        originalStones = PlayerDataService.AdvanceStones;

        // Clean up test player prefs
        PlayerPrefs.DeleteKey(DailyLoginManager.CurrentDayKey);
        PlayerPrefs.DeleteKey(DailyLoginManager.LastLoginDateUtcKey);
        PlayerPrefs.DeleteKey(DailyLoginManager.LastClaimDateUtcKey);
        PlayerPrefs.DeleteKey(DailyLoginManager.ClaimedMaskKey);
        PlayerPrefs.DeleteKey(DailyLoginManager.CycleCountKey);
        PlayerPrefs.Save();
    }

    [TearDown]
    public void TearDown()
    {
        PlayerDataService.DataChips = originalChips;
        PlayerDataService.RedGems = originalGems;
        PlayerDataService.Energy = originalEnergy;
        PlayerDataService.AdvanceStones = originalStones;
        PlayerPrefs.Save();
    }

    // =========================================================================
    // DAILY LOGIN REWARD TESTS
    // =========================================================================

    [Test]
    public void DailyLogin_FirstLogin_Day1Available_AndFutureDaysLocked()
    {
        GameObject go = new GameObject("DailyLoginTest");
        DailyLoginManager mgr = go.AddComponent<DailyLoginManager>();
        mgr.EnsureDatabaseLoaded();

        Assert.That(mgr.CurrentLoginDay, Is.EqualTo(1));
        Assert.That(mgr.GetDayState(1), Is.EqualTo(DailyLoginState.Available), "Day 1 phải ở trạng thái Available khi mở game lần đầu.");
        Assert.That(mgr.GetDayState(2), Is.EqualTo(DailyLoginState.Locked), "Day 2 phải ở trạng thái Locked.");
        Assert.That(mgr.GetDayState(7), Is.EqualTo(DailyLoginState.Locked), "Day 7 phải ở trạng thái Locked.");
        Assert.That(mgr.CanClaimToday(), Is.True);

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void DailyLogin_ClaimDay1_GrantsCurrencies_SetsObtained_AndSavesImmediately()
    {
        GameObject go = new GameObject("DailyLoginClaimTest");
        DailyLoginManager mgr = go.AddComponent<DailyLoginManager>();
        mgr.EnsureDatabaseLoaded();

        int initialEnergy = ChipManager.Energy;
        int initialGems = ChipManager.RedGems;
        int initialChips = ChipManager.DataChips;

        // Day 1 reward: Energy x30, RedGem x300, DataChip x1000
        bool claimed = mgr.TryClaimTodayReward();
        Assert.That(claimed, Is.True, "Claim Day 1 phải thành công.");

        Assert.That(ChipManager.Energy, Is.EqualTo(initialEnergy + 30), "Energy phải tăng đúng 30.");
        Assert.That(ChipManager.RedGems, Is.EqualTo(initialGems + 300), "Red Gems phải tăng đúng 300.");
        Assert.That(ChipManager.DataChips, Is.EqualTo(initialChips + 1000), "Data Chips phải tăng đúng 1000.");

        Assert.That(mgr.IsDayClaimed(1), Is.True);
        Assert.That(mgr.GetDayState(1), Is.EqualTo(DailyLoginState.Obtained));
        Assert.That(mgr.CanClaimToday(), Is.False, "Sau khi claim hôm nay, CanClaimToday phải trả về false.");

        // Kiểm tra lưu trữ ngay lập tức
        Assert.That(PlayerPrefs.GetString(DailyLoginManager.LastClaimDateUtcKey), Is.Not.Empty);
        Assert.That(PlayerPrefs.GetInt(DailyLoginManager.ClaimedMaskKey), Is.EqualTo(1));

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void DailyLogin_DoubleClaimSpam_IsPrevented()
    {
        GameObject go = new GameObject("DailyLoginAntiSpamTest");
        DailyLoginManager mgr = go.AddComponent<DailyLoginManager>();
        mgr.EnsureDatabaseLoaded();

        bool firstClaim = mgr.TryClaimTodayReward();
        Assert.That(firstClaim, Is.True);

        int gemsAfterFirst = ChipManager.RedGems;

        // Cố gắng claim lần 2
        bool secondClaim = mgr.TryClaimTodayReward();
        Assert.That(secondClaim, Is.False, "Claim lần 2 trong cùng một ngày phải bị từ chối.");
        Assert.That(ChipManager.RedGems, Is.EqualTo(gemsAfterFirst), "Số dư không được tăng thêm khi spam claim.");

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void DailyLogin_Countdown_FormatsTimeCorrectly()
    {
        GameObject go = new GameObject("DailyLoginCountdownTest");
        DailyLoginManager mgr = go.AddComponent<DailyLoginManager>();
        mgr.EnsureDatabaseLoaded();

        TimeSpan span = mgr.GetTimeUntilNextResetUtc();
        Assert.That(span.TotalSeconds, Is.GreaterThan(0));

        string formatted = mgr.GetRemainingTimeFormatted();
        Assert.That(formatted, Does.Match(@"^\d{2}:\d{2}:\d{2}$"), "Format countdown phải đúng định dạng HH:mm:ss");

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void DailyLogin_ContinuousStreak_AdvancesToNextDayWithoutReset()
    {
        DailyLoginDatabase db = ScriptableObject.CreateInstance<DailyLoginDatabase>();
        db.streakMode = StreakResetMode.ContinuousKeepStreak;
        db.PopulateDefault7Days();

        GameObject go = new GameObject("DailyLoginStreakTest");
        DailyLoginManager mgr = go.AddComponent<DailyLoginManager>();
        typeof(DailyLoginManager).GetField("database", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.SetValue(mgr, db);

        // Day 1 claim
        mgr.TryClaimTodayReward();
        Assert.That(mgr.CurrentLoginDay, Is.EqualTo(1));

        // Giả lập 3 ngày sau mới quay lại
        DateTime fakeToday = mgr.GetEffectiveDateUtc();
        DateTime fakeOldDate = fakeToday.AddDays(-3);
        PlayerPrefs.SetString(DailyLoginManager.LastLoginDateUtcKey, fakeOldDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        PlayerPrefs.Save();

        mgr.CheckAndUpdateLoginDay();

        // Với ContinuousKeepStreak, ngày tiếp theo phải là Day 2 (không bị reset về Day 1)
        Assert.That(mgr.CurrentLoginDay, Is.EqualTo(2), "ContinuousKeepStreak phải tiếp tục sang Day 2.");
        Assert.That(mgr.GetDayState(2), Is.EqualTo(DailyLoginState.Available));

        UnityEngine.Object.DestroyImmediate(go);
        UnityEngine.Object.DestroyImmediate(db);
    }

    [Test]
    public void DailyLogin_ResetStreakMode_ResetsToDay1OnMissedDay()
    {
        DailyLoginDatabase db = ScriptableObject.CreateInstance<DailyLoginDatabase>();
        db.streakMode = StreakResetMode.ResetToDay1OnMissedDay;
        db.PopulateDefault7Days();

        GameObject go = new GameObject("DailyLoginResetStreakTest");
        DailyLoginManager mgr = go.AddComponent<DailyLoginManager>();
        typeof(DailyLoginManager).GetField("database", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.SetValue(mgr, db);

        // Giả lập đang ở Day 3
        PlayerPrefs.SetInt(DailyLoginManager.CurrentDayKey, 3);
        PlayerPrefs.SetInt(DailyLoginManager.ClaimedMaskKey, 3); // Đã claim Day 1, Day 2

        // Giả lập bỏ lỡ 2 ngày
        DateTime fakeToday = mgr.GetEffectiveDateUtc();
        DateTime fakeOldDate = fakeToday.AddDays(-2);
        PlayerPrefs.SetString(DailyLoginManager.LastLoginDateUtcKey, fakeOldDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        PlayerPrefs.Save();

        mgr.CheckAndUpdateLoginDay();

        Assert.That(mgr.CurrentLoginDay, Is.EqualTo(1), "ResetToDay1OnMissedDay phải reset CurrentLoginDay về 1.");
        Assert.That(mgr.ClaimedMask, Is.EqualTo(0), "ClaimedMask phải được reset về 0.");

        UnityEngine.Object.DestroyImmediate(go);
        UnityEngine.Object.DestroyImmediate(db);
    }

    // =========================================================================
    // ACHIEVEMENTS TESTS
    // =========================================================================

    [Test]
    public void Achievement_ProgressClamping_NeverExceeds100PercentFill()
    {
        GameObject go = new GameObject("AchievementManagerTest");
        AchievementManager mgr = go.AddComponent<AchievementManager>();
        mgr.EnsureDatabaseLoaded();

        string achId = "drone_upgrade_3"; // Target = 3
        mgr.SetProgress(achId, 19); // 19 / 3

        Assert.That(mgr.GetProgress(achId), Is.EqualTo(19));
        float normalized = mgr.GetProgressNormalized(achId);
        Assert.That(normalized, Is.EqualTo(1.0f), "Thanh fill progress không được vượt quá 100% (1.0f).");
        Assert.That(mgr.IsCompleted(achId), Is.True);
        Assert.That(mgr.GetState(achId), Is.EqualTo(AchievementState.Completed));

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void Achievement_EventIntegration_EnemyKills_IncreasesProgress()
    {
        GameObject go = new GameObject("AchievementEnemyTest");
        AchievementManager mgr = go.AddComponent<AchievementManager>();
        mgr.EnsureDatabaseLoaded();

        string achId = "enemy_kill_2500";
        mgr.SetProgress(achId, 2024);

        Assert.That(mgr.GetState(achId), Is.EqualTo(AchievementState.InProgress));

        // Phát sự kiện 1 quái chết
        GameEvents.RaiseEnemyKilled();

        Assert.That(mgr.GetProgress(achId), Is.EqualTo(2025));

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void Achievement_EventIntegration_DroneUpgrade_IncreasesProgress()
    {
        GameObject go = new GameObject("AchievementDroneTest");
        AchievementManager mgr = go.AddComponent<AchievementManager>();
        mgr.EnsureDatabaseLoaded();

        string achId = "drone_upgrade_3";
        mgr.SetProgress(achId, 2);

        Assert.That(mgr.IsCompleted(achId), Is.False);

        GameEvents.RaiseDroneTierAdvanced();

        Assert.That(mgr.GetProgress(achId), Is.EqualTo(3));
        Assert.That(mgr.IsCompleted(achId), Is.True);
        Assert.That(mgr.GetState(achId), Is.EqualTo(AchievementState.Completed));

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void Achievement_ClaimReward_GrantsCorrectCurrencies_AndMarksClaimed()
    {
        GameObject go = new GameObject("AchievementClaimTest");
        AchievementManager mgr = go.AddComponent<AchievementManager>();
        mgr.EnsureDatabaseLoaded();

        string achId = "login_reward_2"; // Target = 2, Reward: RedGem x200
        mgr.SetProgress(achId, 2);

        int initialGems = ChipManager.RedGems;

        bool claimed = mgr.TryClaimReward(achId);
        Assert.That(claimed, Is.True);
        Assert.That(ChipManager.RedGems, Is.EqualTo(initialGems + 200), "Red Gems phải tăng đúng 200.");
        Assert.That(mgr.IsClaimed(achId), Is.True);
        Assert.That(mgr.GetState(achId), Is.EqualTo(AchievementState.Claimed));

        // Claim lại lần 2 phải bị từ chối
        bool secondClaim = mgr.TryClaimReward(achId);
        Assert.That(secondClaim, Is.False);
        Assert.That(ChipManager.RedGems, Is.EqualTo(initialGems + 200));

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void Achievement_Sorting_PrioritizesClaimableOverInProgressAndClaimed()
    {
        GameObject go = new GameObject("AchievementSortingTest");
        AchievementManager mgr = go.AddComponent<AchievementManager>();
        mgr.EnsureDatabaseLoaded();

        // 1. Claimed
        mgr.SetProgress("login_reward_2", 2);
        mgr.SetClaimed("login_reward_2", true);

        // 2. In Progress
        mgr.SetProgress("enemy_kill_2500", 500);
        mgr.SetClaimed("enemy_kill_2500", false);

        // 3. Completed (Claimable)
        mgr.SetProgress("drone_upgrade_3", 3);
        mgr.SetClaimed("drone_upgrade_3", false);

        List<AchievementDefinition> sorted = mgr.GetSortedAchievements();
        Assert.That(sorted, Is.Not.Null);
        Assert.That(sorted.Count, Is.GreaterThanOrEqualTo(3));

        // Phần tử đầu tiên phải là Completed (drone_upgrade_3)
        Assert.That(sorted[0].id, Is.EqualTo("drone_upgrade_3"), "Achievement hoàn thành chờ nhận thưởng phải lên đầu tiên.");

        // Phần tử cuối cùng phải là Claimed (login_reward_2)
        Assert.That(sorted[sorted.Count - 1].id, Is.EqualTo("login_reward_2"), "Achievement đã nhận thưởng phải nằm ở cuối cùng.");

        UnityEngine.Object.DestroyImmediate(go);
    }

    // =========================================================================
    // UI POPUP TESTS
    // =========================================================================

    [Test]
    public void Popup_TabSwitching_TogglesPanelsWithoutDestroying()
    {
        GameObject popupGo = new GameObject("RewardPopupTest");
        RewardPopupController popup = popupGo.AddComponent<RewardPopupController>();

        GameObject dailyPanel = new GameObject("DailyLoginPanel");
        dailyPanel.transform.SetParent(popupGo.transform, false);

        GameObject achPanel = new GameObject("AchievementPanel");
        achPanel.transform.SetParent(popupGo.transform, false);

        popup.SetReferencesForBuilder(
            popupGo, null, null, null, null, null, null, null, null, null, null,
            dailyPanel, achPanel, null, null
        );

        // Switch to Daily (tab 0)
        popup.SwitchTab(0);
        Assert.That(dailyPanel.activeSelf, Is.True);
        Assert.That(achPanel.activeSelf, Is.False);
        Assert.That(popup.CurrentTab, Is.EqualTo(0));

        // Switch to Achievements (tab 1)
        popup.SwitchTab(1);
        Assert.That(dailyPanel.activeSelf, Is.False);
        Assert.That(achPanel.activeSelf, Is.True);
        Assert.That(popup.CurrentTab, Is.EqualTo(1));

        UnityEngine.Object.DestroyImmediate(popupGo);
    }

    [Test]
    public void Popup_TabSwitching_SwapsActiveAndInactiveSprites()
    {
        GameObject popupGo = new GameObject("RewardPopupTest_Sprites");
        RewardPopupController popup = popupGo.AddComponent<RewardPopupController>();

        GameObject dailyBtnGo = new GameObject("DailyLoginTab");
        dailyBtnGo.transform.SetParent(popupGo.transform);
        Button dailyBtn = dailyBtnGo.AddComponent<Button>();
        Image dailyBg = dailyBtnGo.AddComponent<Image>();

        GameObject achBtnGo = new GameObject("AchievementTab");
        achBtnGo.transform.SetParent(popupGo.transform);
        Button achBtn = achBtnGo.AddComponent<Button>();
        Image achBg = achBtnGo.AddComponent<Image>();

        Sprite dailyActive = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
        dailyActive.name = "DailyActive";
        Sprite dailyInactive = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
        dailyInactive.name = "DailyInactive";
        Sprite achActive = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
        achActive.name = "AchActive";
        Sprite achInactive = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
        achInactive.name = "AchInactive";

        popup.SetReferencesForBuilder(
            popupGo, null, null,
            dailyBtn, dailyBg, null, null,
            achBtn, achBg, null, null,
            null, null, null, null,
            dailyActive, dailyInactive, achActive, achInactive
        );

        // When Daily Login is selected (tab 0): Daily is Active, Achievements is Inactive
        popup.SwitchTab(0, animated: false);
        Assert.That(dailyBg.sprite, Is.EqualTo(dailyActive));
        Assert.That(achBg.sprite, Is.EqualTo(achInactive));

        // When Achievements is selected (tab 1): Daily is Inactive, Achievements is Active
        popup.SwitchTab(1, animated: false);
        Assert.That(dailyBg.sprite, Is.EqualTo(dailyInactive));
        Assert.That(achBg.sprite, Is.EqualTo(achActive));

        // Switch back to Daily Login: Daily is Active, Achievements is Inactive
        popup.SwitchTab(0, animated: false);
        Assert.That(dailyBg.sprite, Is.EqualTo(dailyActive));
        Assert.That(achBg.sprite, Is.EqualTo(achInactive));

        UnityEngine.Object.DestroyImmediate(dailyActive);
        UnityEngine.Object.DestroyImmediate(dailyInactive);
        UnityEngine.Object.DestroyImmediate(achActive);
        UnityEngine.Object.DestroyImmediate(achInactive);
        UnityEngine.Object.DestroyImmediate(popupGo);
    }

    [Test]
    public void AchievementItemUI_EnsureReferences_SetsButtonTextToNotAchievedWhenInProgress()
    {
        GameObject itemGo = new GameObject("TestItemUI");
        AchievementItemUI itemUI = itemGo.AddComponent<AchievementItemUI>();

        GameObject btnGo = new GameObject("ActionButton");
        btnGo.transform.SetParent(itemGo.transform);
        Button btn = btnGo.AddComponent<Button>();
        Image btnImg = btnGo.AddComponent<Image>();

        GameObject txtGo = new GameObject("Text");
        txtGo.transform.SetParent(btnGo.transform);
        TextMeshProUGUI txt = txtGo.AddComponent<TextMeshProUGUI>();
        txt.text = "Get"; // Default scene placeholder

        // Before update, button text is "Get"
        Assert.That(txt.text, Is.EqualTo("Get"));

        // When InProgress -> Must be "Get" (disabled dark teal matching Image 1) or "Not achieved"
        itemUI.UpdateState(AchievementState.InProgress);
        Assert.That(itemUI.ActionButtonText, Is.Not.Null);
        Assert.That(itemUI.ActionButtonText.text, Is.EqualTo("Get").Or.EqualTo("Not achieved"));
        Assert.That(btn.interactable, Is.False);

        // When Completed -> Must change to "Get" and be interactable
        itemUI.UpdateState(AchievementState.Completed);
        Assert.That(itemUI.ActionButtonText.text, Is.EqualTo("Get"));
        Assert.That(btn.interactable, Is.True);

        UnityEngine.Object.DestroyImmediate(itemGo);
    }

    [Test]
    public void AchievementPanelUI_EnsureCapacity_PurgesNullsAndDiscoversExisting()
    {
        GameObject panelGo = new GameObject("TestPanel");
        AchievementPanelUI panel = panelGo.AddComponent<AchievementPanelUI>();

        GameObject containerGo = new GameObject("ContentContainer");
        containerGo.transform.SetParent(panelGo.transform);

        GameObject item1Go = new GameObject("Item_0");
        item1Go.transform.SetParent(containerGo.transform);
        AchievementItemUI item1 = item1Go.AddComponent<AchievementItemUI>();

        GameObject item2Go = new GameObject("Item_1");
        item2Go.transform.SetParent(containerGo.transform);
        AchievementItemUI item2 = item2Go.AddComponent<AchievementItemUI>();

        GameObject item3Go = new GameObject("Item_2");
        item3Go.transform.SetParent(containerGo.transform);
        AchievementItemUI item3 = item3Go.AddComponent<AchievementItemUI>();

        // Simulate corrupted spawnedItems: 1 valid item + 4 nulls (exactly like MainMenu.unity had!)
        List<AchievementItemUI> corruptedList = new List<AchievementItemUI>
        {
            item1,
            null,
            null,
            null,
            null
        };

        panel.SetReferencesForBuilder(null, containerGo.transform, corruptedList, null, null, null);

        // EnsureSpawnedItemsCapacity should purge nulls and discover all 3 children
        panel.EnsureSpawnedItemsCapacity(3);

        Assert.That(panel.SpawnedItems, Is.Not.Null);
        Assert.That(panel.SpawnedItems.Count, Is.EqualTo(3));
        Assert.That(panel.SpawnedItems[0], Is.EqualTo(item1));
        Assert.That(panel.SpawnedItems[1], Is.EqualTo(item2));
        Assert.That(panel.SpawnedItems[2], Is.EqualTo(item3));

        UnityEngine.Object.DestroyImmediate(panelGo);
    }

    [Test]
    public void AchievementManager_SyncExistingProgress_UpdatesDailyLoginAndChapters()
    {
        GameObject mgrGo = new GameObject("SyncTestMgr");
        AchievementManager mgr = mgrGo.AddComponent<AchievementManager>();
        mgr.EnsureDatabaseLoaded();

        // Simulate existing login claims (Day 1 and Day 2 bitmask = 3)
        PlayerPrefs.SetInt(DailyLoginManager.ClaimedMaskKey, 3);
        PlayerPrefs.SetInt(DailyLoginManager.CycleCountKey, 0);

        // Simulate chapter 3 unlocked -> 2 chapters cleared
        PlayerPrefs.SetInt(PlayerDataService.UnlockedChapterIndexKey, 2);
        PlayerPrefs.Save();

        // Clear existing achievement progress to 0
        mgr.SetProgress("login_reward_2", 0);
        mgr.SetProgress("chapter_clear_5", 0);

        mgr.SyncExistingProgress();

        Assert.That(mgr.GetProgress("login_reward_2"), Is.EqualTo(2), "Phải tự động đồng bộ 2 ngày điểm danh.");
        Assert.That(mgr.GetProgress("chapter_clear_5"), Is.EqualTo(2), "Phải tự động đồng bộ 2 chapters đã clear.");

        // Clean up
        PlayerPrefs.DeleteKey(DailyLoginManager.ClaimedMaskKey);
        PlayerPrefs.DeleteKey(PlayerDataService.UnlockedChapterIndexKey);
        PlayerPrefs.Save();
        UnityEngine.Object.DestroyImmediate(mgrGo);
    }

    [Test]
    public void Popup_TabSwitching_PreservesAuthoredPanelTransforms()
    {
        GameObject popupGo = new GameObject("RewardPopupTransformTest", typeof(RectTransform));
        RewardPopupController popup = popupGo.AddComponent<RewardPopupController>();

        GameObject dailyPanel = new GameObject("DailyLoginPanel", typeof(RectTransform));
        dailyPanel.transform.SetParent(popupGo.transform, false);
        RectTransform dailyRt = dailyPanel.GetComponent<RectTransform>();
        Vector2 customDailyPos = new Vector2(35f, -22f);
        dailyRt.anchoredPosition = customDailyPos;

        GameObject achPanel = new GameObject("AchievementPanel", typeof(RectTransform));
        achPanel.transform.SetParent(popupGo.transform, false);
        RectTransform achRt = achPanel.GetComponent<RectTransform>();
        Vector2 customAchPos = new Vector2(-18f, 40f);
        achRt.anchoredPosition = customAchPos;

        popup.SetReferencesForBuilder(
            popupGo, null, null, null, null, null, null, null, null, null, null,
            dailyPanel, achPanel, null, null
        );

        // Switch to Daily (tab 0)
        popup.SwitchTab(0, animated: false);
        Assert.That(dailyRt.anchoredPosition, Is.EqualTo(customDailyPos), "DailyPanel phải giữ nguyên anchoredPosition X/Y do người dùng chỉnh sửa trong Edit Mode.");

        // Switch to Achievements (tab 1)
        popup.SwitchTab(1, animated: false);
        Assert.That(achRt.anchoredPosition, Is.EqualTo(customAchPos), "AchievementPanel phải giữ nguyên anchoredPosition X/Y do người dùng chỉnh sửa trong Edit Mode.");

        // Switch back to Daily (tab 0)
        popup.SwitchTab(0, animated: false);
        Assert.That(dailyRt.anchoredPosition, Is.EqualTo(customDailyPos), "DailyPanel vẫn phải bảo toàn tọa độ X/Y sau khi chuyển tab qua lại.");

        UnityEngine.Object.DestroyImmediate(popupGo);
    }

    [Test]
    public void Popup_TabSwitching_PreservesAuthoredTabTransforms()
    {
        GameObject popupGo = new GameObject("RewardPopupTabTransformTest", typeof(RectTransform));
        RewardPopupController popup = popupGo.AddComponent<RewardPopupController>();

        GameObject dailyBtnGo = new GameObject("DailyLoginTab", typeof(RectTransform), typeof(Button), typeof(Image));
        dailyBtnGo.transform.SetParent(popupGo.transform, false);
        RectTransform dailyTabRt = dailyBtnGo.GetComponent<RectTransform>();
        Vector2 customDailyTabPos = new Vector2(-150f, 12f);
        dailyTabRt.anchoredPosition = customDailyTabPos;

        GameObject achBtnGo = new GameObject("AchievementTab", typeof(RectTransform), typeof(Button), typeof(Image));
        achBtnGo.transform.SetParent(popupGo.transform, false);
        RectTransform achTabRt = achBtnGo.GetComponent<RectTransform>();
        Vector2 customAchTabPos = new Vector2(150f, 12f);
        achTabRt.anchoredPosition = customAchTabPos;

        popup.SetReferencesForBuilder(
            popupGo, null, null,
            dailyBtnGo.GetComponent<Button>(), dailyBtnGo.GetComponent<Image>(), null, null,
            achBtnGo.GetComponent<Button>(), achBtnGo.GetComponent<Image>(), null, null,
            null, null, null, null
        );

        popup.SwitchTab(0, animated: false);
        Assert.That(dailyTabRt.anchoredPosition, Is.EqualTo(customDailyTabPos), "DailyLoginTab phải giữ nguyên tọa độ khi được chọn.");
        Assert.That(achTabRt.anchoredPosition, Is.EqualTo(customAchTabPos), "AchievementTab phải giữ nguyên tọa độ khi inactive.");

        popup.SwitchTab(1, animated: false);
        Assert.That(dailyTabRt.anchoredPosition, Is.EqualTo(customDailyTabPos), "DailyLoginTab phải giữ nguyên tọa độ khi inactive.");
        Assert.That(achTabRt.anchoredPosition, Is.EqualTo(customAchTabPos), "AchievementTab phải giữ nguyên tọa độ khi active.");

        UnityEngine.Object.DestroyImmediate(popupGo);
    }

    [Test]
    public void AchievementItemUI_RenderRewards_ReusesExistingBadgesAndPreservesTransforms()
    {
        GameObject itemGo = new GameObject("AchievementItemTest", typeof(RectTransform));
        AchievementItemUI itemUI = itemGo.AddComponent<AchievementItemUI>();

        GameObject rewardsContainerGo = new GameObject("RewardsContainer", typeof(RectTransform));
        rewardsContainerGo.transform.SetParent(itemGo.transform, false);

        GameObject badgeGo = new GameObject("RewardBadge_0", typeof(RectTransform), typeof(Image));
        badgeGo.transform.SetParent(rewardsContainerGo.transform, false);
        RectTransform badgeRt = badgeGo.GetComponent<RectTransform>();
        Vector2 customBadgePos = new Vector2(24f, -8f);
        badgeRt.anchoredPosition = customBadgePos;
        Vector2 customBadgeSize = new Vector2(90f, 90f);
        badgeRt.sizeDelta = customBadgeSize;

        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(badgeGo.transform, false);
        GameObject textObj = new GameObject("AmountText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(badgeGo.transform, false);

        itemUI.SetReferencesForBuilder(null, null, null, null, rewardsContainerGo.transform, null, null, null, null, null, null);

        AchievementDefinition def = new AchievementDefinition
        {
            id = "test_ach",
            title = "Test",
            targetValue = 10,
            rewards = new RewardData[]
            {
                new RewardData { type = RewardType.RedGem, amount = 100 }
            }
        };

        itemUI.Setup(def, 5, AchievementState.InProgress, null, null);

        Assert.That(rewardsContainerGo.transform.childCount, Is.EqualTo(1), "Badge phải được tái sử dụng thay vì nhân bản.");
        Assert.That(badgeRt.anchoredPosition, Is.EqualTo(customBadgePos), "Badge RectTransform anchoredPosition phải được giữ nguyên 100%.");
        Assert.That(badgeRt.sizeDelta, Is.EqualTo(customBadgeSize), "Badge RectTransform sizeDelta phải được giữ nguyên 100%.");

        UnityEngine.Object.DestroyImmediate(itemGo);
    }

    [Test]
    public void DailyLoginItemUI_RenderRewards_ReusesExistingBadgesAndPreservesTransforms()
    {
        GameObject itemGo = new GameObject("DailyItemTest", typeof(RectTransform));
        DailyLoginItemUI itemUI = itemGo.AddComponent<DailyLoginItemUI>();

        GameObject rewardsContainerGo = new GameObject("RewardsContainer", typeof(RectTransform));
        rewardsContainerGo.transform.SetParent(itemGo.transform, false);

        GameObject badgeGo = new GameObject("RewardBadge_0", typeof(RectTransform), typeof(Image));
        badgeGo.transform.SetParent(rewardsContainerGo.transform, false);
        RectTransform badgeRt = badgeGo.GetComponent<RectTransform>();
        Vector2 customBadgePos = new Vector2(18f, -12f);
        badgeRt.anchoredPosition = customBadgePos;
        Vector2 customBadgeSize = new Vector2(85f, 85f);
        badgeRt.sizeDelta = customBadgeSize;

        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(badgeGo.transform, false);
        GameObject textObj = new GameObject("AmountText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(badgeGo.transform, false);

        itemUI.SetReferencesForBuilder(null, null, rewardsContainerGo.transform, null, null, null, null, null, null, null, null, null, null);

        DailyLoginDayData dayData = new DailyLoginDayData
        {
            dayIndex = 1,
            rewards = new RewardData[]
            {
                new RewardData { type = RewardType.Energy, amount = 50 }
            }
        };

        itemUI.Setup(dayData, DailyLoginState.Available, null, null);

        Assert.That(rewardsContainerGo.transform.childCount, Is.EqualTo(1), "Badge Daily phải được tái sử dụng.");
        Assert.That(badgeRt.anchoredPosition, Is.EqualTo(customBadgePos), "Badge Daily RectTransform anchoredPosition phải được giữ nguyên.");
        Assert.That(badgeRt.sizeDelta, Is.EqualTo(customBadgeSize), "Badge Daily RectTransform sizeDelta phải được giữ nguyên.");

        UnityEngine.Object.DestroyImmediate(itemGo);
    }

    [Test]
    public void DailyLoginItemUI_ObtainedState_KeepsBlueBannerAndOverlaysGreyBanner()
    {
        GameObject itemGo = new GameObject("DailyItemTest_Overlay", typeof(RectTransform), typeof(CanvasGroup));
        DailyLoginItemUI itemUI = itemGo.AddComponent<DailyLoginItemUI>();
        CanvasGroup cg = itemGo.GetComponent<CanvasGroup>();

        GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(itemGo.transform, false);
        Image bgImg = bgObj.GetComponent<Image>();

        GameObject overlayObj = new GameObject("DarkOverlay", typeof(RectTransform), typeof(Image));
        overlayObj.transform.SetParent(bgObj.transform, false);
        Image overlayImg = overlayObj.GetComponent<Image>();

        Sprite blueBanner = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
        blueBanner.name = "Row_Banner_Blue";
        Sprite greyBanner = Sprite.Create(Texture2D.blackTexture, new Rect(0, 0, 4, 4), Vector2.zero);
        greyBanner.name = "Row_Banner_Grey";

        itemUI.SetReferencesForBuilder(
            null, null, null, null, null, null, null, null, null, null,
            bgImg, null, cg, null, null, null, overlayObj
        );

        var so = new UnityEditor.SerializedObject(itemUI);
        so.FindProperty("cardBannerBlue").objectReferenceValue = blueBanner;
        so.FindProperty("cardBannerGrey").objectReferenceValue = greyBanner;
        so.ApplyModifiedProperties();

        // 1. Trạng thái Obtained: Phải giữ sprite nút sáng (blue), overlay (grey) phải được BẬT
        itemUI.UpdateState(DailyLoginState.Obtained);

        Assert.That(bgImg.sprite, Is.EqualTo(blueBanner), "Nút sáng Row_Banner_Blue LUÔN là background chính, KHÔNG được thay thế.");
        Assert.That(overlayObj.activeSelf, Is.True, "DarkOverlay phải được bật đè lên nút sáng khi ngày đã nhận.");
        Assert.That(overlayImg.sprite, Is.EqualTo(greyBanner), "DarkOverlay phải dùng sprite Row_Banner_Grey.");
        Assert.That(cg.alpha, Is.EqualTo(1.0f), "CanvasGroup alpha phải giữ 1.0f để các chi tiết không bị xỉn.");

        // 2. Trạng thái Available: DarkOverlay phải được TẮT
        itemUI.UpdateState(DailyLoginState.Available);

        Assert.That(bgImg.sprite, Is.EqualTo(blueBanner), "Background chính vẫn là Row_Banner_Blue.");
        Assert.That(overlayObj.activeSelf, Is.False, "DarkOverlay phải bị tắt khi ngày chưa nhận (sáng).");
        Assert.That(cg.alpha, Is.EqualTo(1.0f), "CanvasGroup alpha vẫn giữ 1.0f.");

        UnityEngine.Object.DestroyImmediate(itemGo);
    }

    [Test]
    public void DailyLoginItemUI_ObtainedState_DynamicallyCreatesDarkOverlayIfMissing()
    {
        // Kiểm tra trường hợp đặc biệt quan trọng: Scene chưa có sẵn GameObject DarkOverlay
        GameObject itemGo = new GameObject("DailyItemTest_AutoCreate", typeof(RectTransform));
        DailyLoginItemUI itemUI = itemGo.AddComponent<DailyLoginItemUI>();

        GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(itemGo.transform, false);
        Image bgImg = bgObj.GetComponent<Image>();

        Sprite blueBanner = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
        blueBanner.name = "Row_Banner_Blue";
        Sprite greyBanner = Sprite.Create(Texture2D.blackTexture, new Rect(0, 0, 4, 4), Vector2.zero);
        greyBanner.name = "Row_Banner_Grey";

        itemUI.SetReferencesForBuilder(
            null, null, null, null, null, null, null, null, null, null,
            bgImg, null, null, null, null, null, null
        );

        var so = new UnityEditor.SerializedObject(itemUI);
        so.FindProperty("cardBannerBlue").objectReferenceValue = blueBanner;
        so.FindProperty("cardBannerGrey").objectReferenceValue = greyBanner;
        so.ApplyModifiedProperties();

        Assert.That(itemUI.DarkOverlay, Is.Null, "Ban đầu item chưa có DarkOverlay.");

        // Khi chuyển sang Obtained -> Phải TỰ ĐỘNG sinh ra DarkOverlay và kích hoạt
        itemUI.UpdateState(DailyLoginState.Obtained);

        Assert.That(itemUI.DarkOverlay, Is.Not.Null, "DarkOverlay phải được tự động sinh ra khi chuyển sang Obtained.");
        Assert.That(itemUI.DarkOverlay.activeSelf, Is.True, "DarkOverlay phải được SetActive(true).");
        Assert.That(itemUI.DarkOverlay.transform.parent, Is.EqualTo(bgObj.transform), "DarkOverlay phải là con của Background.");
        Assert.That(bgImg.sprite, Is.EqualTo(blueBanner), "Background chính phải luôn là Row_Banner_Blue.");

        Image createdImg = itemUI.DarkOverlay.GetComponent<Image>();
        Assert.That(createdImg, Is.Not.Null);
        Assert.That(createdImg.sprite, Is.EqualTo(greyBanner), "DarkOverlay phải mang sprite Row_Banner_Grey.");
        Assert.That(createdImg.raycastTarget, Is.False, "DarkOverlay không được chặn raycast.");

        // Khi chuyển sang Available -> DarkOverlay phải tự động tắt
        itemUI.UpdateState(DailyLoginState.Available);
        Assert.That(itemUI.DarkOverlay.activeSelf, Is.False, "DarkOverlay phải tắt khi ngày chuyển sang Available.");

        UnityEngine.Object.DestroyImmediate(itemGo);
    }

    [Test]
    public void DailyLoginItemUI_SetButtonVisual_Obtained_ControlsDarkOverlay()
    {
        GameObject itemGo = new GameObject("DailyItemTest_ButtonVisual", typeof(RectTransform));
        DailyLoginItemUI itemUI = itemGo.AddComponent<DailyLoginItemUI>();

        GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(itemGo.transform, false);
        Image bgImg = bgObj.GetComponent<Image>();

        GameObject btnGo = new GameObject("ClaimButton", typeof(RectTransform), typeof(Button), typeof(Image));
        btnGo.transform.SetParent(itemGo.transform, false);
        Button claimBtn = btnGo.GetComponent<Button>();

        Sprite blueBanner = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
        blueBanner.name = "Row_Banner_Blue";
        Sprite greyBanner = Sprite.Create(Texture2D.blackTexture, new Rect(0, 0, 4, 4), Vector2.zero);
        greyBanner.name = "Row_Banner_Grey";
        Sprite obtSprite = Sprite.Create(Texture2D.blackTexture, new Rect(0, 0, 4, 4), Vector2.zero);
        obtSprite.name = "Btn_Obtained";

        itemUI.SetReferencesForBuilder(
            null, null, null, claimBtn, null, null, null, null, null, null,
            bgImg, null, null, null, null, obtSprite, null
        );

        var so = new UnityEditor.SerializedObject(itemUI);
        so.FindProperty("cardBannerBlue").objectReferenceValue = blueBanner;
        so.FindProperty("cardBannerGrey").objectReferenceValue = greyBanner;
        so.ApplyModifiedProperties();

        // 1. SetButtonVisual(Obtained) -> DarkOverlay phải được bật
        itemUI.SetButtonVisual(DailyButtonState.Obtained);
        Assert.That(itemUI.DarkOverlay, Is.Not.Null);
        Assert.That(itemUI.DarkOverlay.activeSelf, Is.True, "Nút Obtained phải bật DarkOverlay đè lên background.");

        // 2. SetButtonVisual(Get) -> DarkOverlay phải được tắt
        itemUI.SetButtonVisual(DailyButtonState.Get);
        Assert.That(itemUI.DarkOverlay.activeSelf, Is.False, "Nút Get phải tắt DarkOverlay.");

        // 3. SetButtonVisual(ClaimAgain) -> DarkOverlay phải được tắt
        itemUI.SetButtonVisual(DailyButtonState.ClaimAgain);
        Assert.That(itemUI.DarkOverlay.activeSelf, Is.False, "Nút Claim Again phải tắt DarkOverlay.");

        UnityEngine.Object.DestroyImmediate(itemGo);
    }

    [Test]
    public void DailyLoginItemUI_CustomBackgroundSetInEditMode_IsPreservedDuringPlayModeAndStateUpdates()
    {
        GameObject itemGo = new GameObject("DailyItemTest_CustomBg", typeof(RectTransform));
        DailyLoginItemUI itemUI = itemGo.AddComponent<DailyLoginItemUI>();

        GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(itemGo.transform, false);
        Image bgImg = bgObj.GetComponent<Image>();

        // Giả lập người dùng thay thế một sprite background tùy chỉnh trong Edit Mode
        Sprite customUserBg = Sprite.Create(Texture2D.redTexture, new Rect(0, 0, 4, 4), Vector2.zero);
        customUserBg.name = "Custom_User_Background";
        bgImg.sprite = customUserBg;

        Sprite defaultBlue = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
        defaultBlue.name = "Row_Banner_Blue";
        Sprite defaultGrey = Sprite.Create(Texture2D.blackTexture, new Rect(0, 0, 4, 4), Vector2.zero);
        defaultGrey.name = "Row_Banner_Grey";

        itemUI.SetReferencesForBuilder(
            null, null, null, null, null, null, null, null, null, null,
            bgImg, null, null, null, null, null, null
        );

        var so = new UnityEditor.SerializedObject(itemUI);
        so.FindProperty("cardBannerBlue").objectReferenceValue = defaultBlue;
        so.FindProperty("cardBannerGrey").objectReferenceValue = defaultGrey;
        so.ApplyModifiedProperties();

        // 1. Khi vào Play Mode (chạy SyncVisualFromCurrentState hoặc UpdateState)
        itemUI.SyncVisualFromCurrentState();
        Assert.That(bgImg.sprite, Is.EqualTo(customUserBg), "Sprite tùy chỉnh người dùng gán ở Edit Mode phải được GIỮ NGUYÊN khi SyncVisual.");

        // 2. Khi chạy UpdateState(Available)
        itemUI.UpdateState(DailyLoginState.Available);
        Assert.That(bgImg.sprite, Is.EqualTo(customUserBg), "UpdateState(Available) KHÔNG ĐƯỢC ghi đè sprite của người dùng.");
        Assert.That(itemUI.DarkOverlay.activeSelf, Is.False, "DarkOverlay phải tắt.");

        // 3. Khi chạy UpdateState(Obtained)
        itemUI.UpdateState(DailyLoginState.Obtained);
        Assert.That(bgImg.sprite, Is.EqualTo(customUserBg), "UpdateState(Obtained) background gốc vẫn giữ nguyên sprite người dùng.");
        Assert.That(itemUI.DarkOverlay.activeSelf, Is.True, "DarkOverlay chỉ phủ đè lên trên, không thay thế background gốc.");

        UnityEngine.Object.DestroyImmediate(itemGo);
    }

    [Test]
    public void DailyLoginItemUI_RewardBadges_AlphaIsZeroForAllDaysAndIcons()
    {
        GameObject itemGo = new GameObject("DailyItemTest_BadgeAlpha", typeof(RectTransform));
        DailyLoginItemUI itemUI = itemGo.AddComponent<DailyLoginItemUI>();

        GameObject rewardsContainer = new GameObject("RewardsContainer", typeof(RectTransform));
        rewardsContainer.transform.SetParent(itemGo.transform, false);

        // Tạo 3 badge mẫu với alpha != 0
        for (int i = 0; i < 3; i++)
        {
            GameObject badge = new GameObject($"RewardBadge_{i}", typeof(RectTransform), typeof(Image));
            badge.transform.SetParent(rewardsContainer.transform, false);
            badge.GetComponent<Image>().color = new Color32(11, 45, 60, 255);
        }

        itemUI.SetReferencesForBuilder(
            null, null, rewardsContainer.transform, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null
        );

        // Gọi SyncVisualFromCurrentState -> Toàn bộ RewardBadge background phải có alpha = 0
        itemUI.SyncVisualFromCurrentState();

        for (int i = 0; i < 3; i++)
        {
            Image badgeImg = rewardsContainer.transform.GetChild(i).GetComponent<Image>();
            Assert.That(badgeImg.color.a, Is.EqualTo(0f), $"RewardBadge_{i} background Image alpha phải bằng 0.");
        }

        UnityEngine.Object.DestroyImmediate(itemGo);
    }
}


