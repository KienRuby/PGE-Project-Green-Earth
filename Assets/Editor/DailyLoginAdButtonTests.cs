using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class DailyLoginAdButtonTests
{
    private GameObject rootObj;
    private DailyLoginItemUI itemUI;
    private Button claimButton;
    private Image claimButtonImage;
    private DailyLoginManager loginManager;

    [SetUp]
    public void SetUp()
    {
        AdRewardService.ForceOfflineTestMode = false;

        // Reset player prefs
        PlayerPrefs.DeleteKey(DailyLoginManager.CurrentDayKey);
        PlayerPrefs.DeleteKey(DailyLoginManager.LastLoginDateUtcKey);
        PlayerPrefs.DeleteKey(DailyLoginManager.LastClaimDateUtcKey);
        PlayerPrefs.DeleteKey(DailyLoginManager.LastAdClaimDateUtcKey);
        PlayerPrefs.DeleteKey(DailyLoginManager.ClaimedMaskKey);
        PlayerPrefs.DeleteKey(DailyLoginManager.CycleCountKey);
        PlayerPrefs.Save();

        // Create manager
        GameObject mgrObj = new GameObject("[TestDailyLoginManager]");
        loginManager = mgrObj.AddComponent<DailyLoginManager>();
        loginManager.EnsureDatabaseLoaded();

        // Create item UI
        rootObj = new GameObject("TestDailyItem", typeof(RectTransform));
        itemUI = rootObj.AddComponent<DailyLoginItemUI>();

        GameObject btnObj = new GameObject("ClaimButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(rootObj.transform, false);
        claimButton = btnObj.GetComponent<Button>();
        claimButtonImage = btnObj.GetComponent<Image>();
        claimButton.targetGraphic = claimButtonImage;

        itemUI.SetReferencesForBuilder(
            null, null, null,
            claimButton, null,
            null, null,
            null, null, null,
            null, null, null
        );
        itemUI.EnsureButtonSpritesLoaded();
    }

    [TearDown]
    public void TearDown()
    {
        AdRewardService.ForceOfflineTestMode = false;
        if (rootObj != null) UnityEngine.Object.DestroyImmediate(rootObj);
        if (loginManager != null) UnityEngine.Object.DestroyImmediate(loginManager.gameObject);
    }

    [Test]
    public void Test_InitialState_DayAvailable_DisplaysGetButton()
    {
        itemUI.UpdateState(DailyLoginState.Available);

        Assert.That(itemUI.CurrentButtonState, Is.EqualTo(DailyButtonState.Get));
        Assert.That(claimButton.gameObject.activeSelf, Is.True);
        Assert.That(claimButton.interactable, Is.True, "Nút Get phải cho phép bấm (interactable = true)");
        Assert.That(claimButtonImage.sprite, Is.EqualTo(itemUI.BtnGetSprite));
    }

    [Test]
    public void Test_ClaimGetButton_WithNetwork_TransitionsToClaimAgain()
    {
        AdRewardService.ForceOfflineTestMode = false;
        itemUI.UpdateState(DailyLoginState.Available);

        // Click nút Get
        itemUI.OnClaimButtonClicked();

        Assert.That(itemUI.CurrentButtonState, Is.EqualTo(DailyButtonState.ClaimAgain), "Sau khi bấm Get có mạng wifi thì phải hiện nút Claim again");
        Assert.That(claimButton.interactable, Is.True, "Nút Claim again phải cho phép bấm");
        Assert.That(claimButtonImage.sprite, Is.EqualTo(itemUI.BtnClaimAgainSprite));
    }

    [Test]
    public void Test_ClaimGetButton_WithoutNetwork_TransitionsToObtained_AndNotInteractable()
    {
        AdRewardService.ForceOfflineTestMode = true; // Giả lập mất mạng / không có wifi
        itemUI.UpdateState(DailyLoginState.Available);

        // Click nút Get khi không có wifi
        itemUI.OnClaimButtonClicked();

        Assert.That(itemUI.CurrentButtonState, Is.EqualTo(DailyButtonState.Obtained), "Không có mạng wifi sau khi nhận Get thì phải hiện nút Obtained");
        Assert.That(claimButton.interactable, Is.False, "Nút Obtained tuyệt đối KHÔNG cho phép bấm");
        Assert.That(claimButtonImage.sprite, Is.EqualTo(itemUI.BtnObtainedSprite));
    }

    [Test]
    public void Test_ClaimAgainButton_WithAdSuccess_GrantsRewardAndTransitionsToObtained()
    {
        AdRewardService.ForceOfflineTestMode = false;
        itemUI.UpdateState(DailyLoginState.Available);
        itemUI.OnClaimButtonClicked(); // Đổi sang Claim again

        Assert.That(itemUI.CurrentButtonState, Is.EqualTo(DailyButtonState.ClaimAgain));

        // Click nút Claim again (Xem quảng cáo)
        itemUI.OnClaimButtonClicked();

        Assert.That(loginManager.HasClaimedAdToday(), Is.True, "Đã ghi nhận nhận thưởng quảng cáo thành công hôm nay");
        Assert.That(itemUI.CurrentButtonState, Is.EqualTo(DailyButtonState.Obtained), "Sau khi xem quảng cáo nhận thưởng phải chuyển sang nút Obtained");
        Assert.That(claimButton.interactable, Is.False, "Nút Obtained không cho phép bấm");
        Assert.That(claimButtonImage.sprite, Is.EqualTo(itemUI.BtnObtainedSprite));
    }

    [Test]
    public void Test_ObtainedButton_CannotBeClicked()
    {
        itemUI.SetButtonVisual(DailyButtonState.Obtained);

        Assert.That(claimButton.interactable, Is.False, "Nút Obtained phải có interactable = false");

        // Gọi hàm click
        itemUI.OnClaimButtonClicked();

        // Trạng thái vẫn là Obtained, không có gì thay đổi
        Assert.That(itemUI.CurrentButtonState, Is.EqualTo(DailyButtonState.Obtained));
    }

    [Test]
    public void Test_ClaimAgainButton_WhenOffline_TransitionsImmediatelyToObtained()
    {
        AdRewardService.ForceOfflineTestMode = false;
        itemUI.SetButtonVisual(DailyButtonState.ClaimAgain);

        // Bị mất mạng đột ngột
        AdRewardService.ForceOfflineTestMode = true;

        itemUI.OnClaimButtonClicked();

        Assert.That(itemUI.CurrentButtonState, Is.EqualTo(DailyButtonState.Obtained), "Bị mất mạng khi bấm Claim again phải chuyển ngay sang Obtained");
        Assert.That(claimButton.interactable, Is.False, "Nút Obtained không cho phép bấm");
    }

    [Test]
    public void Test_PastDayObtained_DisplaysObtainedButton_NotInteractable()
    {
        itemUI.UpdateState(DailyLoginState.Obtained);

        Assert.That(itemUI.CurrentButtonState, Is.EqualTo(DailyButtonState.Obtained));
        Assert.That(claimButton.gameObject.activeSelf, Is.True);
        Assert.That(claimButton.interactable, Is.False, "Ngày cũ đã nhận thưởng phải hiện nút Obtained và không cho phép bấm");
        Assert.That(claimButtonImage.sprite, Is.EqualTo(itemUI.BtnObtainedSprite));
    }
}