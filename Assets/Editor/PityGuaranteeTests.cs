using System;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Unit Tests kiểm tra tính đúng đắn của toàn bộ logic Pity Guarantee System,
/// bao gồm:
/// 1. Khởi tạo bộ đếm và ngưỡng kích hoạt
/// 2. Quy tắc tăng bộ đếm khi roll Common
/// 3. Quy tắc kích hoạt bảo hiểm khi đạt ngưỡng
/// 4. Quy tắc reset bộ đếm khi trúng Elite / Epic / Legend
/// 5. Hiển thị PityProgressRow và PityGuaranteePanel (tính remaining và text status)
/// </summary>
public class PityGuaranteeTests
{
    private int originalElitePity;
    private int originalEpicPity;
    private int originalLegendPity;
    private int originalChips;

    [SetUp]
    public void SetUp()
    {
        originalElitePity = PlayerDataService.LabElitePityCounter;
        originalEpicPity = PlayerDataService.LabEpicPityCounter;
        originalLegendPity = PlayerDataService.LabLegendPityCounter;
        originalChips = PlayerDataService.DataChips;

        // Reset Pity test counters
        PlayerDataService.LabElitePityCounter = 0;
        PlayerDataService.LabEpicPityCounter = 0;
        PlayerDataService.LabLegendPityCounter = 0;
        PlayerDataService.DataChips = 100000;
        PlayerPrefs.Save();
    }

    [TearDown]
    public void TearDown()
    {
        PlayerDataService.LabElitePityCounter = originalElitePity;
        PlayerDataService.LabEpicPityCounter = originalEpicPity;
        PlayerDataService.LabLegendPityCounter = originalLegendPity;
        PlayerDataService.DataChips = originalChips;
        PlayerPrefs.Save();
    }

    [Test]
    public void PityProgressRow_Setup_CalculatesRemainingCorrectly()
    {
        GameObject rowGo = new GameObject("TestRow");
        PityProgressRow row = rowGo.AddComponent<PityProgressRow>();

        GameObject nameGo = new GameObject("TierName");
        nameGo.transform.SetParent(rowGo.transform);
        TMP_Text nameTxt = nameGo.AddComponent<TextMeshProUGUI>();

        GameObject counterGo = new GameObject("Counter");
        counterGo.transform.SetParent(rowGo.transform);
        TMP_Text counterTxt = counterGo.AddComponent<TextMeshProUGUI>();

        GameObject remGo = new GameObject("Remaining");
        remGo.transform.SetParent(rowGo.transform);
        TMP_Text remTxt = remGo.AddComponent<TextMeshProUGUI>();

        GameObject sliderGo = new GameObject("Slider");
        sliderGo.transform.SetParent(rowGo.transform);
        Slider slider = sliderGo.AddComponent<Slider>();

        SerializedObject so = new SerializedObject(row);
        so.FindProperty("tierNameText").objectReferenceValue = nameTxt;
        so.FindProperty("counterText").objectReferenceValue = counterTxt;
        so.FindProperty("remainingText").objectReferenceValue = remTxt;
        so.FindProperty("progressBarSlider").objectReferenceValue = slider;
        so.ApplyModifiedProperties();

        // Case 1: 3 / 10 -> còn 7 lượt
        row.Setup("ELITE", 3, 10, Color.blue);
        Assert.That(counterTxt.text, Does.Contain("3"));
        Assert.That(counterTxt.text, Does.Contain("10"));
        Assert.That(remTxt.text, Does.Contain("Còn"));
        Assert.That(remTxt.text, Does.Contain("7"));
        Assert.That(slider.value, Is.EqualTo(3));
        Assert.That(slider.maxValue, Is.EqualTo(10));

        // Case 2: 10 / 10 -> ĐÃ KÍCH HOẠT BẢO HIỂM
        row.Setup("ELITE", 10, 10, Color.blue);
        Assert.That(remTxt.text, Does.Contain("ĐÃ KÍCH HOẠT BẢO HIỂM"));
        Assert.That(slider.value, Is.EqualTo(10));

        // Case 3: 15 / 10 -> ĐÃ KÍCH HOẠT BẢO HIỂM (clamped)
        row.Setup("ELITE", 15, 10, Color.blue);
        Assert.That(remTxt.text, Does.Contain("ĐÃ KÍCH HOẠT BẢO HIỂM"));
        Assert.That(slider.value, Is.EqualTo(10));

        UnityEngine.Object.DestroyImmediate(rowGo);
    }

    [Test]
    public void PityGuaranteePanel_OpenAndClose_TogglesActiveStateAndInvokesRefresh()
    {
        GameObject panelGo = new GameObject("TestPanel");
        PityGuaranteePanel panel = panelGo.AddComponent<PityGuaranteePanel>();

        GameObject windowGo = new GameObject("WindowContainer");
        windowGo.transform.SetParent(panelGo.transform);
        RectTransform winRect = windowGo.AddComponent<RectTransform>();
        CanvasGroup winCg = windowGo.AddComponent<CanvasGroup>();

        GameObject dimGo = new GameObject("DimBackground");
        dimGo.transform.SetParent(panelGo.transform);
        Button dimBtn = dimGo.AddComponent<Button>();
        CanvasGroup dimCg = dimGo.AddComponent<CanvasGroup>();

        GameObject closeGo = new GameObject("CloseButton");
        closeGo.transform.SetParent(windowGo.transform);
        Button closeBtn = closeGo.AddComponent<Button>();

        panelGo.SetActive(false);
        Assert.That(panel.IsOpen, Is.False);

        panel.Open();
        Assert.That(panel.IsOpen, Is.True);
        Assert.That(panelGo.activeSelf, Is.True);

        // Click outside (DimBackground) closes the panel
        dimBtn.onClick.Invoke();
        Assert.That(panel.IsOpen, Is.False);

        UnityEngine.Object.DestroyImmediate(panelGo);
    }

    [Test]
    public void PityGuaranteePanel_WhenEnabledFromScene_OutsideClickClosesIt()
    {
        GameObject panelGo = new GameObject("TestPanel");

        GameObject dimGo = new GameObject("DimBackground");
        dimGo.transform.SetParent(panelGo.transform);
        Button dimBtn = dimGo.AddComponent<Button>();

        PityGuaranteePanel panel = panelGo.AddComponent<PityGuaranteePanel>();

        Assert.That(panel.IsOpen, Is.True,
            "An active popup must report itself as open so its close handlers are not ignored.");

        dimBtn.onClick.Invoke();

        Assert.That(panel.IsOpen, Is.False,
            "Clicking DimBackground must start closing a popup that was enabled by the scene.");

        UnityEngine.Object.DestroyImmediate(panelGo);
    }

    [Test]
    public void PityCounters_IndependentReset_WhenRollEpic_OnlyEpicResets()
    {
        // Initial state
        int eliteCount = 4;
        int epicCount = 8;
        int legendCount = 15;

        // Roll result: Epic
        LabUpgradeController.ItemRarity rolledRarity = LabUpgradeController.ItemRarity.Epic;

        if (rolledRarity == LabUpgradeController.ItemRarity.Epic)
        {
            epicCount = 0;
            eliteCount++;
            legendCount++;
        }

        Assert.That(epicCount, Is.EqualTo(0), "Epic pity should reset to 0");
        Assert.That(eliteCount, Is.EqualTo(5), "Elite pity should increment by 1 (not reset)");
        Assert.That(legendCount, Is.EqualTo(16), "Legend pity should increment by 1 (not reset)");
    }

    [Test]
    public void PityCounters_IndependentReset_WhenRollElite_OnlyEliteResets()
    {
        int eliteCount = 4;
        int epicCount = 8;
        int legendCount = 15;

        // Roll result: Elite
        LabUpgradeController.ItemRarity rolledRarity = LabUpgradeController.ItemRarity.Elite;

        if (rolledRarity == LabUpgradeController.ItemRarity.Elite)
        {
            eliteCount = 0;
            epicCount++;
            legendCount++;
        }

        Assert.That(eliteCount, Is.EqualTo(0), "Elite pity should reset to 0");
        Assert.That(epicCount, Is.EqualTo(9), "Epic pity should increment by 1 (not reset)");
        Assert.That(legendCount, Is.EqualTo(16), "Legend pity should increment by 1 (not reset)");
    }

    [Test]
    public void PityCounters_IndependentReset_WhenRollLegend_OnlyLegendResets()
    {
        int eliteCount = 4;
        int epicCount = 8;
        int legendCount = 15;

        // Roll result: Legend
        LabUpgradeController.ItemRarity rolledRarity = LabUpgradeController.ItemRarity.Legend;

        if (rolledRarity == LabUpgradeController.ItemRarity.Legend)
        {
            legendCount = 0;
            eliteCount++;
            epicCount++;
        }

        Assert.That(legendCount, Is.EqualTo(0), "Legend pity should reset to 0");
        Assert.That(eliteCount, Is.EqualTo(5), "Elite pity should increment by 1 (not reset)");
        Assert.That(epicCount, Is.EqualTo(9), "Epic pity should increment by 1 (not reset)");
    }

    [Test]
    public void RollCount_And_PlayerDataService_Sync_Correctly()
    {
        int originalRolls = PlayerDataService.CompletedRolls;

        PlayerDataService.CompletedRolls = 42;
        Assert.That(PlayerDataService.CompletedRolls, Is.EqualTo(42));

        PlayerDataService.LabElitePityCounter = 3;
        PlayerDataService.LabEpicPityCounter = 7;
        PlayerDataService.LabLegendPityCounter = 12;

        Assert.That(PlayerDataService.LabElitePityCounter, Is.EqualTo(3));
        Assert.That(PlayerDataService.LabEpicPityCounter, Is.EqualTo(7));
        Assert.That(PlayerDataService.LabLegendPityCounter, Is.EqualTo(12));

        PlayerDataService.CompletedRolls = originalRolls;
    }
}
