#if UNITY_EDITOR
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[TestFixture]
public class LabStatDetailTests
{
    private GameObject rootObj;
    private LabStatTooltip tooltip;
    private RectTransform panelRect;
    private RectTransform arrowPointer;
    private TextMeshProUGUI detailText;

    [SetUp]
    public void SetUp()
    {
        rootObj = new GameObject("Test_StatDetailTooltip", typeof(RectTransform));
        panelRect = rootObj.GetComponent<RectTransform>();

        GameObject arrowObj = new GameObject("ArrowPointer", typeof(RectTransform));
        arrowObj.transform.SetParent(rootObj.transform, false);
        arrowPointer = arrowObj.GetComponent<RectTransform>();

        GameObject textObj = new GameObject("DetailText", typeof(RectTransform));
        textObj.transform.SetParent(rootObj.transform, false);
        detailText = textObj.AddComponent<TextMeshProUGUI>();

        tooltip = rootObj.AddComponent<LabStatTooltip>();

        var so = new UnityEditor.SerializedObject(tooltip);
        so.FindProperty("panelRect").objectReferenceValue = panelRect;
        so.FindProperty("arrowPointer").objectReferenceValue = arrowPointer;
        so.FindProperty("detailText").objectReferenceValue = detailText;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    [TearDown]
    public void TearDown()
    {
        if (rootObj != null)
        {
            Object.DestroyImmediate(rootObj);
        }
    }

    [Test]
    public void FormatStatDetail_WhenLocked_ReturnsQuestionMarks()
    {
        string text = LabStatTooltip.FormatStatDetail("HP", 0, true, 10);
        Assert.AreEqual("???", text);

        string text2 = LabStatTooltip.FormatStatDetail("DEF", 5, true, 10);
        Assert.AreEqual("???", text2);
    }

    [Test]
    public void FormatStatDetail_HP_Level5_MatchesScreenshot()
    {
        string text = LabStatTooltip.FormatStatDetail("HP", 5, false, 10);
        Assert.IsTrue(text.Contains("HP +275"));
        Assert.IsTrue(text.Contains("LV.05>LV.06 HP +290"));
        Assert.IsTrue(text.Contains("Increases HP."));
    }

    [Test]
    public void FormatStatDetail_ChipsetSelection_Level1_MatchesScreenshot()
    {
        string text = LabStatTooltip.FormatStatDetail("Chipset Selection", 1, false, 10);
        Assert.IsTrue(text.Contains("Chipset Selection +1 Rate +3%"));
        Assert.IsTrue(text.Contains("LV.01>LV.02 Chipset Selection +1 Rate +6%"));
        Assert.IsTrue(text.Contains("Higher chance to obtain additional Chipset Selection upon leveling up."));
    }

    [Test]
    public void FormatStatDetail_MaxLevel_ShowsMaxLevel()
    {
        string text = LabStatTooltip.FormatStatDetail("ATK", 10, false, 10);
        Assert.IsTrue(text.Contains("(MAX LEVEL)"));
    }

    [Test]
    public void Show_PositionsPanelAndPointer_AccuratelyByRowAndCol()
    {
        // Slot 0 -> Row 0, Col 0: Y = -250, X = -348
        tooltip.Show(0, null, "HP", 5, false, 10);
        Assert.IsTrue(tooltip.IsShowing);
        Assert.AreEqual(0, tooltip.CurrentSlotIndex);
        Assert.AreEqual(-250f, panelRect.anchoredPosition.y);
        Assert.AreEqual(-348f, arrowPointer.anchoredPosition.x);

        // Slot 15 -> Row 3, Col 3: Y = -950, X = 348
        tooltip.Show(15, null, "Chipset Selection", 1, false, 10);
        Assert.AreEqual(15, tooltip.CurrentSlotIndex);
        Assert.AreEqual(-950f, panelRect.anchoredPosition.y);
        Assert.AreEqual(348f, arrowPointer.anchoredPosition.x);
    }

    [Test]
    public void Show_SameSlotTwice_TogglesOff()
    {
        tooltip.Show(0, null, "HP", 5, false, 10);
        Assert.IsTrue(tooltip.IsShowing);

        // Clicking same slot toggles hide
        tooltip.Show(0, null, "HP", 5, false, 10);
        Assert.IsFalse(tooltip.IsShowing);
        Assert.AreEqual(-1, tooltip.CurrentSlotIndex);
    }

    [Test]
    public void Hide_DeactivatesGameObject()
    {
        tooltip.Show(2, null, "DEF", 3, false, 10);
        Assert.IsTrue(tooltip.IsShowing);

        tooltip.Hide();
        Assert.IsFalse(tooltip.IsShowing);
        Assert.IsFalse(rootObj.activeSelf);
    }
}
#endif
