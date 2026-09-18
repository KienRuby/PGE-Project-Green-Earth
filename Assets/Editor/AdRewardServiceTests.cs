using System;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class AdRewardServiceTests
{
    private GameObject testRoot;
    private PlayerHealth playerHealth;
    private PlayerRunEndController runEndController;
    private Button adReviveButton;
    private Button gemReviveButton;
    private Button noButton;
    private TMP_Text feedbackText;
    private GameObject revivePanel;
    private GameObject gameOverPanel;
    private Button getRewardButton;
    private DailyLoginManager loginManager;

    [SetUp]
    public void SetUp()
    {
        AdRewardService.ForceOfflineTestMode = false;
        AdRewardService.IgnoreCooldownForTesting = false;
        AdRewardService.ResetAllCooldowns();

        testRoot = new GameObject("[TestRoot_AdRewardService]");

        // Setup PlayerHealth
        GameObject playerObj = new GameObject("Player", typeof(PlayerHealth));
        playerObj.transform.SetParent(testRoot.transform);
        playerHealth = playerObj.GetComponent<PlayerHealth>();

        // Setup Revive Panel and components
        revivePanel = new GameObject("RevivePanel", typeof(RectTransform));
        revivePanel.transform.SetParent(testRoot.transform);

        GameObject adBtnObj = new GameObject("AdReviveButton", typeof(RectTransform), typeof(Image), typeof(Button));
        adBtnObj.transform.SetParent(revivePanel.transform);
        adReviveButton = adBtnObj.GetComponent<Button>();

        GameObject gemBtnObj = new GameObject("GemReviveButton", typeof(RectTransform), typeof(Image), typeof(Button));
        gemBtnObj.transform.SetParent(revivePanel.transform);
        gemReviveButton = gemBtnObj.GetComponent<Button>();

        GameObject noBtnObj = new GameObject("NoButton", typeof(RectTransform), typeof(Image), typeof(Button));
        noBtnObj.transform.SetParent(revivePanel.transform);
        noButton = noBtnObj.GetComponent<Button>();

        GameObject fbObj = new GameObject("FeedbackText", typeof(RectTransform), typeof(TextMeshProUGUI));
        fbObj.transform.SetParent(revivePanel.transform);
        feedbackText = fbObj.GetComponent<TMP_Text>();

        // Setup GameOver Panel
        gameOverPanel = new GameObject("GameOverPanel", typeof(RectTransform));
        gameOverPanel.transform.SetParent(testRoot.transform);

        GameObject getRewardObj = new GameObject("GetRewardButton", typeof(RectTransform), typeof(Image), typeof(Button));
        getRewardObj.transform.SetParent(gameOverPanel.transform);
        getRewardButton = getRewardObj.GetComponent<Button>();

        // Setup PlayerRunEndController
        runEndController = testRoot.AddComponent<PlayerRunEndController>();

        SerializedObject so = new SerializedObject(runEndController);
        so.FindProperty("playerHealth").objectReferenceValue = playerHealth;
        so.FindProperty("revivePanel").objectReferenceValue = revivePanel;
        so.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanel;
        so.FindProperty("adReviveButton").objectReferenceValue = adReviveButton;
        so.FindProperty("gemReviveButton").objectReferenceValue = gemReviveButton;
        so.FindProperty("noButton").objectReferenceValue = noButton;
        so.FindProperty("getRewardButton").objectReferenceValue = getRewardButton;
        so.FindProperty("reviveFeedbackText").objectReferenceValue = feedbackText;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Setup DailyLoginManager
        PlayerPrefs.DeleteKey(DailyLoginManager.CurrentDayKey);
        PlayerPrefs.DeleteKey(DailyLoginManager.LastLoginDateUtcKey);
        PlayerPrefs.DeleteKey(DailyLoginManager.LastClaimDateUtcKey);
        PlayerPrefs.DeleteKey(DailyLoginManager.LastAdClaimDateUtcKey);
        PlayerPrefs.DeleteKey(DailyLoginManager.ClaimedMaskKey);
        PlayerPrefs.DeleteKey(DailyLoginManager.CycleCountKey);
        PlayerPrefs.Save();

        GameObject mgrObj = new GameObject("[TestDailyLoginManager]");
        mgrObj.transform.SetParent(testRoot.transform);
        loginManager = mgrObj.AddComponent<DailyLoginManager>();
        loginManager.EnsureDatabaseLoaded();
    }

    [TearDown]
    public void TearDown()
    {
        AdRewardService.ForceOfflineTestMode = false;
        AdRewardService.IgnoreCooldownForTesting = false;
        AdRewardService.ResetAllCooldowns();

        if (testRoot != null)
        {
            UnityEngine.Object.DestroyImmediate(testRoot);
        }
    }

    // =========================================================================
    // 1. REVIVE SYSTEM TESTS (1 REVIVE PER RUN)
    // =========================================================================

    [Test]
    public void Test_PlayerRunEndController_AdRevive_SuccessfullyRevivesPlayer_AndMarksRevivedThisRun()
    {
        // 1. Giả lập Player chết lần đầu trong trận
        playerHealth.TakeDamage(1000);
        Assert.That(playerHealth.IsDead, Is.True, "Player phải ở trạng thái đã chết.");

        // 2. Mở Revive Panel
        MethodInfo showPromptMethod = typeof(PlayerRunEndController).GetMethod("ShowRevivePrompt", BindingFlags.Instance | BindingFlags.NonPublic);
        showPromptMethod.Invoke(runEndController, null);

        Assert.That(revivePanel.activeSelf, Is.True, "Lần đầu chết, RevivePanel phải mở lên.");
        Assert.That(adReviveButton.interactable, Is.True, "Nút Ad Revive phải bấm được.");

        // 3. Bấm nút xem quảng cáo hồi sinh
        MethodInfo onAdReviveClicked = typeof(PlayerRunEndController).GetMethod("OnAdReviveClicked", BindingFlags.Instance | BindingFlags.NonPublic);
        onAdReviveClicked.Invoke(runEndController, null);

        // 4. Kiểm tra Player đã được hồi sinh và cờ HasRevivedThisRun được bật
        Assert.That(playerHealth.IsDead, Is.False, "Player phải được hồi sinh!");
        Assert.That(playerHealth.CurrentHealth, Is.GreaterThan(0), "Máu của Player phải được phục hồi.");
        Assert.That(revivePanel.activeSelf, Is.False, "RevivePanel phải tự động đóng lại.");
        Assert.That(runEndController.HasRevivedThisRun, Is.True, "Trận này đã được ghi nhận là đã dùng lượt hồi sinh.");
    }

    [Test]
    public void Test_PlayerRunEndController_SecondDeathInSameRun_GoesDirectlyToGameOver_NoReviveAllowed()
    {
        // 1. Lần 1: Chết và hồi sinh thành công
        playerHealth.TakeDamage(1000);
        MethodInfo showPromptMethod = typeof(PlayerRunEndController).GetMethod("ShowRevivePrompt", BindingFlags.Instance | BindingFlags.NonPublic);
        showPromptMethod.Invoke(runEndController, null);

        MethodInfo onAdReviveClicked = typeof(PlayerRunEndController).GetMethod("OnAdReviveClicked", BindingFlags.Instance | BindingFlags.NonPublic);
        onAdReviveClicked.Invoke(runEndController, null);

        Assert.That(runEndController.HasRevivedThisRun, Is.True, "Đã hồi sinh 1 lần trong trận.");
        Assert.That(playerHealth.IsDead, Is.False);

        // 2. Lần 2: Player lại chết tiếp trong cùng trận đấu
        playerHealth.TakeDamage(1000);
        Assert.That(playerHealth.IsDead, Is.True);

        // Gọi lại ShowRevivePrompt khi chết lần 2
        showPromptMethod.Invoke(runEndController, null);

        // 3. Kiểm tra: KHÔNG cho mở RevivePanel, chuyển thẳng sang GameOverPanel!
        Assert.That(revivePanel.activeSelf, Is.False, "Chết lần 2 không được mở lại bảng Revive.");
        Assert.That(gameOverPanel.activeSelf, Is.True, "Chết lần 2 phải chuyển thẳng sang Game Over Panel.");
    }

    [Test]
    public void Test_PlayerRunEndController_ResetReviveForNewRun_AllowsRevivingAgainInNextRun()
    {
        // Giả lập đã hồi sinh trong trận cũ
        playerHealth.TakeDamage(1000);
        MethodInfo showPromptMethod = typeof(PlayerRunEndController).GetMethod("ShowRevivePrompt", BindingFlags.Instance | BindingFlags.NonPublic);
        showPromptMethod.Invoke(runEndController, null);
        MethodInfo onAdReviveClicked = typeof(PlayerRunEndController).GetMethod("OnAdReviveClicked", BindingFlags.Instance | BindingFlags.NonPublic);
        onAdReviveClicked.Invoke(runEndController, null);

        Assert.That(runEndController.HasRevivedThisRun, Is.True);

        // Bắt đầu trận mới -> Reset
        runEndController.ResetReviveForNewRun();
        Assert.That(runEndController.HasRevivedThisRun, Is.False, "Trận mới phải reset lại cờ hồi sinh.");
    }

    [Test]
    public void Test_PlayerRunEndController_AdRevive_WhenOffline_DisplaysNoNetwork()
    {
        AdRewardService.ForceOfflineTestMode = true;

        playerHealth.TakeDamage(1000);
        MethodInfo showPromptMethod = typeof(PlayerRunEndController).GetMethod("ShowRevivePrompt", BindingFlags.Instance | BindingFlags.NonPublic);
        showPromptMethod.Invoke(runEndController, null);

        MethodInfo onAdReviveClicked = typeof(PlayerRunEndController).GetMethod("OnAdReviveClicked", BindingFlags.Instance | BindingFlags.NonPublic);
        onAdReviveClicked.Invoke(runEndController, null);

        Assert.That(playerHealth.IsDead, Is.True);
        Assert.That(feedbackText.text, Is.EqualTo("NO NETWORK CONNECTION"), "Phải báo không có mạng khi mất kết nối.");
    }

    // =========================================================================
    // 2. DAILY LOGIN 1 PER DAY TESTS (NO 60S COOLDOWN)
    // =========================================================================

    [Test]
    public void Test_DailyLoginItemUI_ClaimAgain_GrantsReward_AndTransitionsDirectlyToObtained_NoCooldown()
    {
        GameObject itemObj = new GameObject("DailyItem", typeof(RectTransform));
        itemObj.transform.SetParent(testRoot.transform);
        DailyLoginItemUI itemUI = itemObj.AddComponent<DailyLoginItemUI>();

        GameObject btnObj = new GameObject("ClaimButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(itemObj.transform);
        Button btn = btnObj.GetComponent<Button>();
        Image btnImg = btnObj.GetComponent<Image>();
        btn.targetGraphic = btnImg;

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(btnObj.transform);
        TMP_Text btnText = txtObj.GetComponent<TMP_Text>();

        itemUI.SetReferencesForBuilder(null, null, null, btn, btnText, null, null, null, null, null, null, null, null);
        itemUI.EnsureButtonSpritesLoaded();

        // 1. Ban đầu ở trạng thái Available -> bấm Get
        itemUI.UpdateState(DailyLoginState.Available);
        Assert.That(itemUI.CurrentButtonState, Is.EqualTo(DailyButtonState.Get));

        itemUI.OnClaimButtonClicked();
        Assert.That(itemUI.CurrentButtonState, Is.EqualTo(DailyButtonState.ClaimAgain), "Bấm Get xong chuyển sang Claim again");

        // 2. Bấm Claim again (Xem quảng cáo nhận quà x2)
        itemUI.OnClaimButtonClicked();

        // 3. Phải chuyển ngay sang nút Obtained, khóa click, KHÔNG có cooldown
        Assert.That(loginManager.HasClaimedAdToday(), Is.True, "Đã ghi nhận nhận thưởng quảng cáo hôm nay");
        Assert.That(itemUI.CurrentButtonState, Is.EqualTo(DailyButtonState.Obtained), "Chuyển vĩnh viễn sang Obtained trong ngày.");
        Assert.That(btn.interactable, Is.False, "Nút Obtained không cho phép bấm.");
        Assert.That(btnText.gameObject.activeSelf, Is.False, "Không được hiển thị đếm ngược 60s trên nút.");
    }

    // =========================================================================
    // 3. ADS ON WIN & LOSE TESTS (INTERSTITIAL / RUN END AD)
    // =========================================================================

    [Test]
    public void Test_GameOver_ReturnHome_TriggersInterstitialAd()
    {
        bool interstitialRequested = false;
        Action<Action> handler = (onComplete) =>
        {
            interstitialRequested = true;
            onComplete?.Invoke();
        };

        AdRewardService.OnInterstitialAdRequested += handler;
        try
        {
            runEndController.ReturnHome();
            Assert.That(interstitialRequested, Is.True, "Bấm về Home từ GameOver phải yêu cầu phát quảng cáo Interstitial.");
        }
        finally
        {
            AdRewardService.OnInterstitialAdRequested -= handler;
        }
    }

    [Test]
    public void Test_Victory_ReturnHome_TriggersInterstitialAd()
    {
        GameObject victoryObj = new GameObject("VictoryPanelController");
        victoryObj.transform.SetParent(testRoot.transform);
        VictoryPanelController victoryController = victoryObj.AddComponent<VictoryPanelController>();

        bool interstitialRequested = false;
        Action<Action> handler = (onComplete) =>
        {
            interstitialRequested = true;
            onComplete?.Invoke();
        };

        AdRewardService.OnInterstitialAdRequested += handler;
        try
        {
            victoryController.ReturnHome();
            Assert.That(interstitialRequested, Is.True, "Bấm về Home từ Victory phải yêu cầu phát quảng cáo Interstitial.");
        }
        finally
        {
            AdRewardService.OnInterstitialAdRequested -= handler;
        }
    }
}
