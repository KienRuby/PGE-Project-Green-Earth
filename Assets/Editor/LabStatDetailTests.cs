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

    [Test]
    public void LabUpgradeController_EnsureUIReferences_ForcesRaycastTargetTrue()
    {
        GameObject labObj = new GameObject("LabTest", typeof(RectTransform));
        CanvasGroup cg = labObj.AddComponent<CanvasGroup>();
        cg.interactable = false;
        cg.blocksRaycasts = false;

        GameObject btnObj = new GameObject("UpgradeButton", typeof(RectTransform));
        btnObj.transform.SetParent(labObj.transform, false);
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.raycastTarget = false; // Initially false as was the bug in MainMenu.unity

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImg;

        LabUpgradeController controller = labObj.AddComponent<LabUpgradeController>();
        var so = new UnityEditor.SerializedObject(controller);
        so.FindProperty("upgradeButton").objectReferenceValue = btn;
        so.FindProperty("upgradeBackground").objectReferenceValue = null; // Unassigned as was in scene
        so.ApplyModifiedPropertiesWithoutUndo();

        // Invoke private Awake/EnsureUIReferences via reflection
        System.Reflection.MethodInfo method = typeof(LabUpgradeController).GetMethod("EnsureUIReferences",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(method);
        method.Invoke(controller, null);

        // Verification: raycastTarget MUST be forced true so clicks work on Android/mobile
        Assert.IsTrue(btnImg.raycastTarget, "Button image raycastTarget must be true for touch detection!");
        Assert.IsTrue(btn.targetGraphic.raycastTarget, "Button targetGraphic raycastTarget must be true!");

        // Verification: upgradeBackground auto-wired from upgradeButton Image
        UnityEditor.SerializedObject soAfter = new UnityEditor.SerializedObject(controller);
        Image bg = soAfter.FindProperty("upgradeBackground").objectReferenceValue as Image;
        Assert.IsNotNull(bg, "upgradeBackground must be auto-wired!");
        Assert.IsTrue(bg.raycastTarget, "upgradeBackground raycastTarget must be true!");

        // Verification: CanvasGroup interactable and blocksRaycasts forced true
        Assert.IsTrue(cg.interactable, "CanvasGroup interactable must be true!");
        Assert.IsTrue(cg.blocksRaycasts, "CanvasGroup blocksRaycasts must be true!");

        Object.DestroyImmediate(labObj);
    }

    [Test]
    public void LabUpgradeController_StartRoll_WhenInsufficientChips_ShowsClearFeedback()
    {
        GameObject labObj = new GameObject("LabTest", typeof(RectTransform));
        GameObject btnObj = new GameObject("UpgradeButton", typeof(RectTransform));
        btnObj.transform.SetParent(labObj.transform, false);
        Image btnImg = btnObj.AddComponent<Image>();
        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImg;

        GameObject textObj = new GameObject("ResultText", typeof(RectTransform));
        textObj.transform.SetParent(labObj.transform, false);
        TextMeshProUGUI resultTmp = textObj.AddComponent<TextMeshProUGUI>();

        LabUpgradeController controller = labObj.AddComponent<LabUpgradeController>();
        var so = new UnityEditor.SerializedObject(controller);
        so.FindProperty("upgradeButton").objectReferenceValue = btn;
        so.FindProperty("resultText").objectReferenceValue = resultTmp;
        so.FindProperty("basePrice").intValue = 300;
        so.ApplyModifiedPropertiesWithoutUndo();

        int initialChips = PlayerDataService.DataChips;
        try
        {
            PlayerDataService.DataChips = 50; // Less than 300 required

            System.Reflection.MethodInfo startRollMethod = typeof(LabUpgradeController).GetMethod("StartRoll",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(startRollMethod);
            startRollMethod.Invoke(controller, null);

            // Should not crash and should show NOT ENOUGH DATA CHIPS
            Assert.IsTrue(resultTmp.text.Contains("NOT ENOUGH DATA CHIPS"),
                $"Expected 'NOT ENOUGH DATA CHIPS' feedback, but was '{resultTmp.text}'");
            Assert.AreEqual(50, PlayerDataService.DataChips, "Chips must not be spent when insufficient!");
        }
        finally
        {
            PlayerDataService.DataChips = initialChips;
            Object.DestroyImmediate(labObj);
        }
    }

    [Test]
    public void LabUpgradeController_RefreshMainView_PreservesDesignerUpgradeButtonColor()
    {
        GameObject labObj = new GameObject("LabUpgradeColorTest", typeof(RectTransform));
        GameObject buttonObj = new GameObject("UpgradeButton", typeof(RectTransform));
        buttonObj.transform.SetParent(labObj.transform, false);

        UnityEngine.UI.Image background = buttonObj.AddComponent<UnityEngine.UI.Image>();
        Color designerColor = new Color32(239, 247, 238, 255);
        background.color = designerColor;

        UnityEngine.UI.Button button = buttonObj.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = background;

        LabUpgradeController controller = labObj.AddComponent<LabUpgradeController>();
        var serializedController = new UnityEditor.SerializedObject(controller);
        serializedController.FindProperty("upgradeButton").objectReferenceValue = button;
        serializedController.FindProperty("upgradeBackground").objectReferenceValue = background;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        var itemsField = typeof(LabUpgradeController).GetField(
            "items",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var priceField = typeof(LabUpgradeController).GetField(
            "currentPrice",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var refreshMethod = typeof(LabUpgradeController).GetMethod(
            "RefreshMainView",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.That(itemsField, Is.Not.Null);
        Assert.That(priceField, Is.Not.Null);
        Assert.That(refreshMethod, Is.Not.Null);

        itemsField.SetValue(controller, new[]
        {
            new LabUpgradeController.ItemEntry
            {
                rarity = LabUpgradeController.ItemRarity.Common,
                dropWeight = 1f,
                level = 1
            }
        });

        int originalChips = PlayerDataService.DataChips;
        try
        {
            PlayerDataService.DataChips = 10000;
            priceField.SetValue(controller, 300);
            refreshMethod.Invoke(controller, null);
            Assert.That(background.color, Is.EqualTo(designerColor),
                "Affordable refresh must keep the color authored in the Scene.");

            PlayerDataService.DataChips = 0;
            priceField.SetValue(controller, 7350);
            refreshMethod.Invoke(controller, null);
            Assert.That(background.color, Is.EqualTo(designerColor),
                "Unaffordable refresh must also keep the color authored in the Scene.");
        }
        finally
        {
            PlayerDataService.DataChips = originalChips;
            Object.DestroyImmediate(labObj);
        }
    }

    [Test]
    public void LabUpgradeController_GetLevelTextColor_ReturnsRequestedHexColorsByRarity()
    {
        GameObject labObj = new GameObject("LabUpgradeColorTest", typeof(RectTransform));
        try
        {
            LabUpgradeController controller = labObj.AddComponent<LabUpgradeController>();

            Color commonExpected = new Color32(255, 233, 92, 255); // #FFE95C
            Color eliteExpected = new Color32(255, 240, 106, 255);  // #FFF06A
            Color epicExpected = new Color32(255, 244, 184, 255);   // #FFF4B8
            Color legendExpected = new Color32(22, 50, 79, 255);    // #16324F

            Assert.AreEqual(commonExpected, controller.GetLevelTextColor(LabUpgradeController.ItemRarity.Common),
                "Common level color should be #FFE95C");
            Assert.AreEqual(eliteExpected, controller.GetLevelTextColor(LabUpgradeController.ItemRarity.Elite),
                "Elite level color should be #FFF06A");
            Assert.AreEqual(epicExpected, controller.GetLevelTextColor(LabUpgradeController.ItemRarity.Epic),
                "Epic level color should be #FFF4B8");
            Assert.AreEqual(legendExpected, controller.GetLevelTextColor(LabUpgradeController.ItemRarity.Legend),
                "Legend level color should be #16324F");
        }
        finally
        {
            Object.DestroyImmediate(labObj);
        }
    }

    [Test]
    public void LabUpgradeController_RefreshItemView_AppliesLevelTextColorByRarity()
    {
        GameObject labObj = new GameObject("LabUpgradeItemViewColorTest", typeof(RectTransform));
        try
        {
            LabUpgradeController controller = labObj.AddComponent<LabUpgradeController>();

            var refreshMethod = typeof(LabUpgradeController).GetMethod(
                "RefreshItemView",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.That(refreshMethod, Is.Not.Null);

            LabUpgradeController.ItemRarity[] rarities = new[]
            {
                LabUpgradeController.ItemRarity.Common,
                LabUpgradeController.ItemRarity.Elite,
                LabUpgradeController.ItemRarity.Epic,
                LabUpgradeController.ItemRarity.Legend
            };

            foreach (var rarity in rarities)
            {
                GameObject textObj = new GameObject("TestLevelText", typeof(RectTransform));
                textObj.transform.SetParent(labObj.transform, false);
                TextMeshProUGUI levelTmp = textObj.AddComponent<TextMeshProUGUI>();

                var item = new LabUpgradeController.ItemEntry
                {
                    itemName = "TestStat",
                    rarity = rarity,
                    level = 1,
                    levelText = levelTmp
                };

                refreshMethod.Invoke(controller, new object[] { item });

                Color expected = controller.GetLevelTextColor(rarity);
                Assert.AreEqual(expected, levelTmp.color,
                    $"RefreshItemView must apply {expected} to levelText for rarity {rarity}");

                Object.DestroyImmediate(textObj);
            }
        }
        finally
        {
            Object.DestroyImmediate(labObj);
        }
    }
}
#endif
