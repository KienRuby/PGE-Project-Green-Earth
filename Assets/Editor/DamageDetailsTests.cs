using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DamageDetailsTests
{
    [SetUp]
    public void Setup()
    {
        ChipsetBattleStats.Reset();
    }

    [TearDown]
    public void TearDown()
    {
        ChipsetBattleStats.Reset();
    }

    [Test]
    public void ChipsetBattleStats_DefaultStandardGun_IsRegisteredOnReset()
    {
        var entry = ChipsetBattleStats.GetEntry(1);
        Assert.That(entry, Is.Not.Null, "Standard Gun (ID 1) phải được đăng ký mặc định khi reset.");
        Assert.That(entry.ChipsetName, Is.EqualTo("Standard Gun"));
        Assert.That(entry.RuntimeLevel, Is.EqualTo(1));
    }

    [Test]
    public void ChipsetBattleStats_DamageAndDPS_CalculateCorrectly()
    {
        ChipsetBattleStats.RegisterChipset(1, 1, 20);
        ChipsetBattleStats.RegisterChipset(7, 1, 35); // Spiky Discus
        ChipsetBattleStats.RegisterChipset(2, 1, 40); // Rifle

        ChipsetBattleStats.RecordDamage(1, 5108);
        ChipsetBattleStats.RecordDamage(7, 2789);
        ChipsetBattleStats.RecordDamage(2, 649);

        long grandTotal = ChipsetBattleStats.GrandTotalDamage;
        Assert.That(grandTotal, Is.EqualTo(5108 + 2789 + 649));

        var standardGun = ChipsetBattleStats.GetEntry(1);
        var discus = ChipsetBattleStats.GetEntry(7);
        var rifle = ChipsetBattleStats.GetEntry(2);

        Assert.That(standardGun.TotalDamage, Is.EqualTo(5108));
        Assert.That(discus.TotalDamage, Is.EqualTo(2789));
        Assert.That(rifle.TotalDamage, Is.EqualTo(649));

        float standardPercent = standardGun.GetDamagePercent(grandTotal);
        float discusPercent = discus.GetDamagePercent(grandTotal);
        float riflePercent = rifle.GetDamagePercent(grandTotal);

        Assert.That(standardPercent, Is.InRange(59.0f, 61.0f));
        Assert.That(discusPercent, Is.InRange(32.0f, 34.0f));
        Assert.That(riflePercent, Is.InRange(7.0f, 8.5f));

        Assert.That(standardGun.DPS, Is.GreaterThan(0));
        Assert.That(discus.DPS, Is.GreaterThan(0));
        Assert.That(rifle.DPS, Is.GreaterThan(0));

        Assert.That(standardGun.FormattedTime, Does.Match(@"^\d{2}:\d{2}$"));
    }

    [Test]
    public void ChipsetBattleStats_SortedEntries_OrdersByDamageDescending()
    {
        ChipsetBattleStats.RegisterChipset(1, 1, 20);
        ChipsetBattleStats.RegisterChipset(2, 1, 40);
        ChipsetBattleStats.RegisterChipset(3, 1, 50); // Rocket Punch

        ChipsetBattleStats.RecordDamage(2, 100);
        ChipsetBattleStats.RecordDamage(3, 500);
        ChipsetBattleStats.RecordDamage(1, 1000);

        List<ChipsetBattleStats.Entry> sorted = ChipsetBattleStats.GetSortedEntries();
        Assert.That(sorted.Count, Is.GreaterThanOrEqualTo(3));
        Assert.That(sorted[0].ChipsetId, Is.EqualTo(1));
        Assert.That(sorted[1].ChipsetId, Is.EqualTo(3));
        Assert.That(sorted[2].ChipsetId, Is.EqualTo(2));
    }

    [Test]
    public void DamageDetailRowUI_UpdateProgressBar_SetsAnchorMaxCorrectly()
    {
        GameObject rowObj = new GameObject("TestRow", typeof(RectTransform), typeof(DamageDetailRowUI));
        GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObj.transform.SetParent(rowObj.transform, false);

        RectTransform fillRt = fillObj.GetComponent<RectTransform>();
        DamageDetailRowUI rowUI = rowObj.GetComponent<DamageDetailRowUI>();

        var serialized = new UnityEditor.SerializedObject(rowUI);
        serialized.FindProperty("progressFillRect").objectReferenceValue = fillRt;
        serialized.FindProperty("progressFillImage").objectReferenceValue = fillObj.GetComponent<Image>();
        serialized.ApplyModifiedPropertiesWithoutUndo();

        rowUI.UpdateProgressBar(59.8f);
        Assert.That(fillRt.anchorMax.x, Is.EqualTo(0.598f).Within(0.001f));

        rowUI.UpdateProgressBar(100f);
        Assert.That(fillRt.anchorMax.x, Is.EqualTo(1.0f).Within(0.001f));

        rowUI.UpdateProgressBar(0f);
        Assert.That(fillRt.anchorMax.x, Is.EqualTo(0.0f).Within(0.001f));

        Object.DestroyImmediate(rowObj);
    }

    [Test]
    public void DamageDetailsPopup_ShowAndHide_TogglesActiveState()
    {
        GameObject popupGo = new GameObject("TestPopup", typeof(RectTransform), typeof(DamageDetailsPopup));
        DamageDetailsPopup popup = popupGo.GetComponent<DamageDetailsPopup>();

        popup.Hide();
        Assert.That(popup.IsVisible, Is.False);

        popup.Show();
        Assert.That(popup.IsVisible, Is.True);

        popup.Hide();
        Assert.That(popup.IsVisible, Is.False);

        Object.DestroyImmediate(popupGo);
    }

    [Test]
    public void PlayerRunEndController_EnsureDetailsUiComponents_CreatesDetailsButtonAndPopup()
    {
        GameObject canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(PlayerRunEndController));
        GameObject gameOverPanel = new GameObject("GameOverPanel", typeof(RectTransform));
        gameOverPanel.transform.SetParent(canvasGo.transform, false);

        GameObject gameOverContent = new GameObject("GameOverContent", typeof(RectTransform));
        gameOverContent.transform.SetParent(gameOverPanel.transform, false);

        GameObject dataReward = new GameObject("DataChipReward", typeof(RectTransform));
        dataReward.transform.SetParent(gameOverContent.transform, false);

        GameObject gemReward = new GameObject("RedGemReward", typeof(RectTransform));
        gemReward.transform.SetParent(gameOverContent.transform, false);

        PlayerRunEndController controller = canvasGo.GetComponent<PlayerRunEndController>();
        var serialized = new UnityEditor.SerializedObject(controller);
        serialized.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanel;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        controller.EnsureDetailsUiComponents();

        Transform detailsBtn = gameOverContent.transform.Find("DetailsButton");
        Assert.That(detailsBtn, Is.Not.Null, "EnsureDetailsUiComponents phải tự tạo DetailsButton nếu thiếu.");

        DamageDetailsPopup popup = canvasGo.GetComponentInChildren<DamageDetailsPopup>(true);
        Object.DestroyImmediate(canvasGo);
    }

    [Test]
    public void PauseModalController_EnsureDamageDetailsComponents_CreatesButtonAndPopup()
    {
        GameObject canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        GameObject modalRoot = new GameObject("PauseModal", typeof(RectTransform));
        modalRoot.transform.SetParent(canvasGo.transform, false);

        PauseModalController pauseCtrl = modalRoot.AddComponent<PauseModalController>();
        var serialized = new UnityEditor.SerializedObject(pauseCtrl);
        serialized.FindProperty("modalRoot").objectReferenceValue = modalRoot;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        pauseCtrl.EnsureDamageDetailsComponents();

        Assert.That(pauseCtrl.DamageDetailsButton, Is.Not.Null, "EnsureDamageDetailsComponents phải tự động tìm hoặc tạo DamageDetailsButton.");
        Assert.That(pauseCtrl.DamageDetailsPopup, Is.Not.Null, "EnsureDamageDetailsComponents phải tự động tìm hoặc tạo DamageDetailsPopup trên Canvas.");

        Object.DestroyImmediate(canvasGo);
    }

    [Test]
    public void PauseModalController_DamageDetailsButton_OpensPopup()
    {
        GameObject canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        GameObject modalRoot = new GameObject("PauseModal", typeof(RectTransform));
        modalRoot.transform.SetParent(canvasGo.transform, false);

        PauseModalController pauseCtrl = modalRoot.AddComponent<PauseModalController>();
        var serialized = new UnityEditor.SerializedObject(pauseCtrl);
        serialized.FindProperty("modalRoot").objectReferenceValue = modalRoot;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        pauseCtrl.EnsureDamageDetailsComponents();
        Assert.That(pauseCtrl.DamageDetailsPopup.IsVisible, Is.False);

        pauseCtrl.OnDamageDetailsButtonClicked();
        Assert.That(pauseCtrl.DamageDetailsPopup.IsVisible, Is.True, "Bấm DamageDetailsButton phải mở hiển thị DamageDetailsPopup.");

        pauseCtrl.ResumeGame();
        Assert.That(pauseCtrl.DamageDetailsPopup.IsVisible, Is.False, "ResumeGame phải tự động ẩn DamageDetailsPopup.");

        Object.DestroyImmediate(canvasGo);
    }
}
