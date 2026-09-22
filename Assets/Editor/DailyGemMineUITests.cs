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

        ctrl.SetRemainingTime(9, 26);
        ctrl.SetEntrances(5, 5);

        StringAssert.Contains("09", timerTxt.text);
        StringAssert.Contains("26", timerTxt.text);
        StringAssert.Contains("Hour", timerTxt.text);
        StringAssert.Contains("Min Left", timerTxt.text);

        StringAssert.Contains("Entrance:", entranceTxt.text);
        StringAssert.Contains("5", entranceTxt.text);
        StringAssert.Contains("Left", entranceTxt.text);

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
}
#endif
