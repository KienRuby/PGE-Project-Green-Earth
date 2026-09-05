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

        // When InProgress -> Must change to "Not achieved" and be disabled
        itemUI.UpdateState(AchievementState.InProgress);
        Assert.That(itemUI.ActionButtonText, Is.Not.Null);
        Assert.That(itemUI.ActionButtonText.text, Is.EqualTo("Not achieved"));
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
}

