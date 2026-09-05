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
        Assert.That(popup.IsVisible, Is.True, "Popup phải còn active trong lúc shader đang tan.");
        Assert.That(popupGo.GetComponent<UIDissolveController>().CurrentState,
            Is.EqualTo(UIDissolveController.TransitionState.Hiding));

        popup.Show();
        Assert.That(popup.IsVisible, Is.True);

        popup.Hide();
        Assert.That(popup.IsVisible, Is.True);
        Assert.That(popupGo.GetComponent<UIDissolveController>().CurrentState,
            Is.EqualTo(UIDissolveController.TransitionState.Hiding));

        Object.DestroyImmediate(popupGo);
    }

    [Test]
    public void DamageDetailsPopup_RuntimeModal_HasBackgroundCloseAndNoCloseIcon()
    {
        GameObject canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        try
        {
            DamageDetailsPopup popup = DamageDetailsPopup.CreateRuntimeModal(canvasGo.transform);
            Transform root = popup.transform;

            Assert.That(root.Find("BackgroundCloseBtn"), Is.Not.Null);
            Assert.That(root.Find("MainFrame/CloseButton"), Is.Null);

            popup.Show();
            root.Find("BackgroundCloseBtn").GetComponent<Button>().onClick.Invoke();
            Assert.That(popup.IsVisible, Is.True);
            Assert.That(root.GetComponent<UIDissolveController>().CurrentState,
                Is.EqualTo(UIDissolveController.TransitionState.Hiding));
        }
        finally
        {
            Object.DestroyImmediate(canvasGo);
        }
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
    public void PauseModalController_EnsureDamageDetailsComponents_DoesNotCreateDetailsForPause()
    {
        GameObject canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        GameObject modalRoot = new GameObject("PauseModal", typeof(RectTransform));
        modalRoot.transform.SetParent(canvasGo.transform, false);

        PauseModalController pauseCtrl = modalRoot.AddComponent<PauseModalController>();
        var serialized = new UnityEditor.SerializedObject(pauseCtrl);
        serialized.FindProperty("modalRoot").objectReferenceValue = modalRoot;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        pauseCtrl.EnsureDamageDetailsComponents();

        Assert.That(pauseCtrl.DamageDetailsButton, Is.Null, "Pause không được tự tạo nút Damage Details.");
        Assert.That(pauseCtrl.DamageDetailsPopup, Is.Null, "Pause không được tự tạo Damage Details popup.");

        Object.DestroyImmediate(canvasGo);
    }

    [Test]
    public void PauseModalController_EnsureDamageDetailsComponents_AppliesReferenceBottomBarLayout()
    {
        GameObject canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        GameObject modalRoot = new GameObject("PauseModal", typeof(RectTransform));
        modalRoot.transform.SetParent(canvasGo.transform, false);

        Button resume = CreateButton("ResumeButton", modalRoot.transform);
        Button settings = CreateButton("Setting", modalRoot.transform);
        Button home = CreateButton("HomeButton", modalRoot.transform);
        Button details = CreateButton("DamageDetailsButton", modalRoot.transform);

        PauseModalController pauseCtrl = modalRoot.AddComponent<PauseModalController>();
        pauseCtrl.SetReferencesForTesting(
            modalRoot, resume, home, null, null, null,
            null, null, null, null, null, null,
            null, null, null, null, null, null,
            dmgDetailsBtn: details, setBtn: settings);

        pauseCtrl.EnsureDamageDetailsComponents();

        Assert.That(resume.gameObject.activeSelf, Is.True, "Nút Resume phải hiện theo mẫu Pause tham chiếu.");
        Assert.That(details.gameObject.activeSelf, Is.False, "Nút Damage Details phải bị ẩn khỏi Pause.");
        Assert.That(resume.GetComponent<RectTransform>().anchoredPosition, Is.EqualTo(new Vector2(-246f, -680f)));
        Assert.That(settings.GetComponent<RectTransform>().anchoredPosition, Is.EqualTo(new Vector2(11f, -680f)));
        Assert.That(home.GetComponent<RectTransform>().anchoredPosition, Is.EqualTo(new Vector2(259f, -680f)));

        Object.DestroyImmediate(canvasGo);
    }

    private static Button CreateButton(string name, Transform parent)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        return buttonObject.GetComponent<Button>();
    }

    [Test]
    public void VictoryPanelController_EnsureDetailsUiComponents_CreatesDetailsButtonAndPopup()
    {
        GameObject canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(VictoryPanelController));
        GameObject victoryPanel = new GameObject("VictoryPanel", typeof(RectTransform));
        victoryPanel.transform.SetParent(canvasGo.transform, false);

        GameObject completePanel = new GameObject("CompletePanel", typeof(RectTransform));
        completePanel.transform.SetParent(victoryPanel.transform, false);

        GameObject dataReward = new GameObject("DataChipReward", typeof(RectTransform));
        dataReward.transform.SetParent(completePanel.transform, false);
        RectTransform dataRt = dataReward.GetComponent<RectTransform>();
        dataRt.anchoredPosition = Vector2.zero;
        dataRt.sizeDelta = new Vector2(610f, 118f);

        GameObject gemReward = new GameObject("RedGemReward", typeof(RectTransform));
        gemReward.transform.SetParent(completePanel.transform, false);
        RectTransform gemRt = gemReward.GetComponent<RectTransform>();
        gemRt.anchoredPosition = Vector2.zero;
        gemRt.sizeDelta = new Vector2(610f, 118f);

        VictoryPanelController controller = canvasGo.GetComponent<VictoryPanelController>();
        var serialized = new UnityEditor.SerializedObject(controller);
        serialized.FindProperty("victoryPanel").objectReferenceValue = victoryPanel;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        controller.EnsureDetailsUiComponents();

        Transform detailsBtn = completePanel.transform.Find("DetailsButton");
        Assert.That(detailsBtn, Is.Not.Null, "VictoryPanelController EnsureDetailsUiComponents phải tự tạo DetailsButton nếu thiếu.");
        Assert.That(dataRt.anchoredPosition.x, Is.EqualTo(-110f).Within(0.01f), "DataChipReward phải dịch sang x = -110f.");
        Assert.That(dataRt.sizeDelta.x, Is.EqualTo(340f).Within(0.01f), "DataChipReward phải thu gọn về width = 340f.");
        Assert.That(gemRt.anchoredPosition.x, Is.EqualTo(-110f).Within(0.01f), "RedGemReward phải dịch sang x = -110f.");
        Assert.That(gemRt.sizeDelta.x, Is.EqualTo(340f).Within(0.01f), "RedGemReward phải thu gọn về width = 340f.");

        DamageDetailsPopup popup = canvasGo.GetComponentInChildren<DamageDetailsPopup>(true);
        Assert.That(popup, Is.Not.Null, "VictoryPanelController EnsureDetailsUiComponents phải tự tạo DamageDetailsPopup trên Canvas.");

        // Test ToggleDetails mở và đóng popup
        controller.ToggleDetails();
        Assert.That(popup.IsVisible, Is.True, "Bấm ToggleDetails phải mở DamageDetailsPopup.");

        controller.ToggleDetails();
        Assert.That(popup.IsVisible, Is.False, "Bấm ToggleDetails lần 2 phải đóng DamageDetailsPopup.");

        Object.DestroyImmediate(canvasGo);
    }
}
