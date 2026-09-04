using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

[TestFixture]
public class PauseModalTests
{
    [Test]
    public void PauseModalController_SelectMainTab_TogglesOnOffButtonsCorrectly()
    {
        GameObject root = new GameObject("PauseModalRoot");
        GameObject statsOn = new GameObject("StatsOn");
        GameObject statsOff = new GameObject("StatsOff");
        GameObject chipsetOn = new GameObject("ChipsetOn");
        GameObject chipsetOff = new GameObject("ChipsetOff");
        GameObject artifactOn = new GameObject("ArtifactOn");
        GameObject artifactOff = new GameObject("ArtifactOff");

        PauseModalController pauseCtrl = root.AddComponent<PauseModalController>();
        pauseCtrl.SetTabButtonsForTesting(statsOn, statsOff, chipsetOn, chipsetOff, artifactOn, artifactOff);

        // 1. Select Stats Tab (0)
        pauseCtrl.SelectMainTab(0);
        Assert.That(pauseCtrl.CurrentMainTab, Is.EqualTo(0));
        Assert.That(statsOn.activeSelf, Is.True, "StatsOn must be active when Stats tab is selected");
        Assert.That(statsOff.activeSelf, Is.False, "StatsOff must be inactive when Stats tab is selected");
        Assert.That(chipsetOn.activeSelf, Is.False, "ChipsetOn must be inactive when Stats tab is selected");
        Assert.That(chipsetOff.activeSelf, Is.True, "ChipsetOff must be active when Stats tab is selected");
        Assert.That(artifactOn.activeSelf, Is.False, "ArtifactOn must be inactive when Stats tab is selected");
        Assert.That(artifactOff.activeSelf, Is.True, "ArtifactOff must be active when Stats tab is selected");

        // 2. Select Chipset Tab (1)
        pauseCtrl.SelectMainTab(1);
        Assert.That(pauseCtrl.CurrentMainTab, Is.EqualTo(1));
        Assert.That(statsOn.activeSelf, Is.False, "StatsOn must be inactive when Chipset tab is selected");
        Assert.That(statsOff.activeSelf, Is.True, "StatsOff must be active when Chipset tab is selected");
        Assert.That(chipsetOn.activeSelf, Is.True, "ChipsetOn must be active when Chipset tab is selected");
        Assert.That(chipsetOff.activeSelf, Is.False, "ChipsetOff must be inactive when Chipset tab is selected");
        Assert.That(artifactOn.activeSelf, Is.False, "ArtifactOn must be inactive when Chipset tab is selected");
        Assert.That(artifactOff.activeSelf, Is.True, "ArtifactOff must be active when Chipset tab is selected");

        // 3. Select Artifact Tab (2)
        pauseCtrl.SelectMainTab(2);
        Assert.That(pauseCtrl.CurrentMainTab, Is.EqualTo(2));
        Assert.That(statsOn.activeSelf, Is.False, "StatsOn must be inactive when Artifact tab is selected");
        Assert.That(statsOff.activeSelf, Is.True, "StatsOff must be active when Artifact tab is selected");
        Assert.That(chipsetOn.activeSelf, Is.False, "ChipsetOn must be inactive when Artifact tab is selected");
        Assert.That(chipsetOff.activeSelf, Is.True, "ChipsetOff must be active when Artifact tab is selected");
        Assert.That(artifactOn.activeSelf, Is.True, "ArtifactOn must be active when Artifact tab is selected");
        Assert.That(artifactOff.activeSelf, Is.False, "ArtifactOff must be inactive when Artifact tab is selected");

        Object.DestroyImmediate(root);
        Object.DestroyImmediate(statsOn);
        Object.DestroyImmediate(statsOff);
        Object.DestroyImmediate(chipsetOn);
        Object.DestroyImmediate(chipsetOff);
        Object.DestroyImmediate(artifactOn);
        Object.DestroyImmediate(artifactOff);
    }

    [Test]
    public void PauseModalController_AutoWireTabButtonsAndSettings_WiresFromHierarchy()
    {
        GameObject root = new GameObject("PauseModalRoot", typeof(RectTransform));
        GameObject sOn = new GameObject("StatsOn", typeof(RectTransform), typeof(Image));
        sOn.transform.SetParent(root.transform);
        GameObject sOff = new GameObject("StatsOff", typeof(RectTransform), typeof(Image));
        sOff.transform.SetParent(root.transform);

        GameObject cOn = new GameObject("ChipsetOn", typeof(RectTransform), typeof(Image));
        cOn.transform.SetParent(root.transform);
        GameObject cOff = new GameObject("ChipsetOff", typeof(RectTransform), typeof(Image));
        cOff.transform.SetParent(root.transform);

        GameObject aOn = new GameObject("ArtifactOn", typeof(RectTransform), typeof(Image));
        aOn.transform.SetParent(root.transform);
        GameObject aOff = new GameObject("ArtifactOff", typeof(RectTransform), typeof(Image));
        aOff.transform.SetParent(root.transform);

        GameObject setBtnObj = new GameObject("Settin", typeof(RectTransform), typeof(Image));
        setBtnObj.transform.SetParent(root.transform);

        PauseModalController pauseCtrl = root.AddComponent<PauseModalController>();
        pauseCtrl.AutoWireTabButtonsAndSettings();

        Assert.That(pauseCtrl.StatsOn, Is.EqualTo(sOn));
        Assert.That(pauseCtrl.StatsOff, Is.EqualTo(sOff));
        Assert.That(pauseCtrl.ChipsetOn, Is.EqualTo(cOn));
        Assert.That(pauseCtrl.ChipsetOff, Is.EqualTo(cOff));
        Assert.That(pauseCtrl.ArtifactOn, Is.EqualTo(aOn));
        Assert.That(pauseCtrl.ArtifactOff, Is.EqualTo(aOff));
        Assert.That(pauseCtrl.SettingButton, Is.Not.Null);
        Assert.That(pauseCtrl.SettingButton.gameObject, Is.EqualTo(setBtnObj));

        Object.DestroyImmediate(root);
    }

    [Test]
    public void PauseModalController_AlignTabPosition_AlignsOffToOnCoordinates()
    {
        GameObject root = new GameObject("Container", typeof(RectTransform));
        GameObject onObj = new GameObject("TabOn", typeof(RectTransform));
        onObj.transform.SetParent(root.transform);
        RectTransform onRt = onObj.GetComponent<RectTransform>();
        onRt.anchoredPosition = new Vector2(100f, 250f);

        GameObject offObj = new GameObject("TabOff", typeof(RectTransform));
        offObj.transform.SetParent(root.transform);
        RectTransform offRt = offObj.GetComponent<RectTransform>();
        offRt.anchoredPosition = new Vector2(100f, 150f);

        PauseModalController pauseCtrl = root.AddComponent<PauseModalController>();
        pauseCtrl.AlignTabPosition(onObj, offObj);

        Assert.That(offRt.anchoredPosition.y, Is.EqualTo(250f).Within(0.01f));
        Assert.That(offRt.anchoredPosition.x, Is.EqualTo(100f).Within(0.01f));

        Object.DestroyImmediate(root);
    }

    [Test]
    public void PauseModalController_SettingButton_OpensSettingsPanel()
    {
        GameObject canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        GameObject settingsGo = new GameObject("SettingsPanel", typeof(RectTransform));
        settingsGo.transform.SetParent(canvasGo.transform);
        SettingsPanelController settingsCtrl = settingsGo.AddComponent<SettingsPanelController>();
        settingsGo.SetActive(false);

        GameObject pauseGo = new GameObject("PauseModal", typeof(RectTransform));
        pauseGo.transform.SetParent(canvasGo.transform);
        PauseModalController pauseCtrl = pauseGo.AddComponent<PauseModalController>();

        Assert.That(settingsCtrl.IsOpen, Is.False);

        pauseCtrl.OnSettingButtonClicked();

        Assert.That(settingsCtrl.IsOpen, Is.True, "SettingsPanel should be open after clicking Setting button");

        Object.DestroyImmediate(canvasGo);
    }

    [Test]
    public void PauseModalController_SelectMainTab_SwapsSpritesOnMainTabButtonsAndKeepsAllActive()
    {
        GameObject root = new GameObject("PauseModalRoot", typeof(RectTransform));

        GameObject statsBtnGo = new GameObject("StatsTabButton", typeof(RectTransform), typeof(Image), typeof(Button));
        statsBtnGo.transform.SetParent(root.transform);
        Button statsBtn = statsBtnGo.GetComponent<Button>();
        Image statsImg = statsBtnGo.GetComponent<Image>();

        GameObject chipBtnGo = new GameObject("ChipsetTabButton", typeof(RectTransform), typeof(Image), typeof(Button));
        chipBtnGo.transform.SetParent(root.transform);
        Button chipBtn = chipBtnGo.GetComponent<Button>();
        Image chipImg = chipBtnGo.GetComponent<Image>();

        GameObject artBtnGo = new GameObject("ArtifactTabButton", typeof(RectTransform), typeof(Image), typeof(Button));
        artBtnGo.transform.SetParent(root.transform);
        Button artBtn = artBtnGo.GetComponent<Button>();
        Image artImg = artBtnGo.GetComponent<Image>();

        Sprite sOn = Sprite.Create(new Texture2D(2, 2), new Rect(0, 0, 2, 2), Vector2.zero);
        sOn.name = "StatsOn";
        Sprite sOff = Sprite.Create(new Texture2D(2, 2), new Rect(0, 0, 2, 2), Vector2.zero);
        sOff.name = "StatsOff";

        Sprite cOn = Sprite.Create(new Texture2D(2, 2), new Rect(0, 0, 2, 2), Vector2.zero);
        cOn.name = "ChipsetOn";
        Sprite cOff = Sprite.Create(new Texture2D(2, 2), new Rect(0, 0, 2, 2), Vector2.zero);
        cOff.name = "ChipsetOff";

        Sprite aOn = Sprite.Create(new Texture2D(2, 2), new Rect(0, 0, 2, 2), Vector2.zero);
        aOn.name = "ArtifactOn";
        Sprite aOff = Sprite.Create(new Texture2D(2, 2), new Rect(0, 0, 2, 2), Vector2.zero);
        aOff.name = "ArtifactOff";

        PauseModalController pauseCtrl = root.AddComponent<PauseModalController>();
        pauseCtrl.StatsOnSprite = sOn;
        pauseCtrl.StatsOffSprite = sOff;
        pauseCtrl.ChipsetOnSprite = cOn;
        pauseCtrl.ChipsetOffSprite = cOff;
        pauseCtrl.ArtifactOnSprite = aOn;
        pauseCtrl.ArtifactOffSprite = aOff;

        pauseCtrl.SetReferencesForTesting(
            root, null, null,
            statsBtn, chipBtn, artBtn,
            null, null, null,
            null, null, null,
            null, null, null,
            null, null, null
        );

        // When Tab 0 (Stats) is selected
        pauseCtrl.SelectMainTab(0);
        Assert.That(statsBtnGo.activeSelf, Is.True, "Stats button must remain active");
        Assert.That(chipBtnGo.activeSelf, Is.True, "Chipset button must remain active");
        Assert.That(artBtnGo.activeSelf, Is.True, "Artifact button must remain active");

        Assert.That(statsImg.sprite, Is.EqualTo(sOn), "Stats should show On sprite");
        Assert.That(chipImg.sprite, Is.EqualTo(cOff), "Chipset should show Off sprite");
        Assert.That(artImg.sprite, Is.EqualTo(aOff), "Artifact should show Off sprite");

        // When Tab 1 (Chipset) is selected
        pauseCtrl.SelectMainTab(1);
        Assert.That(statsBtnGo.activeSelf, Is.True);
        Assert.That(chipBtnGo.activeSelf, Is.True);
        Assert.That(artBtnGo.activeSelf, Is.True);

        Assert.That(statsImg.sprite, Is.EqualTo(sOff), "Stats should show Off sprite");
        Assert.That(chipImg.sprite, Is.EqualTo(cOn), "Chipset should show On sprite");
        Assert.That(artImg.sprite, Is.EqualTo(aOff), "Artifact should show Off sprite");

        // When Tab 2 (Artifact) is selected
        pauseCtrl.SelectMainTab(2);
        Assert.That(statsBtnGo.activeSelf, Is.True);
        Assert.That(chipBtnGo.activeSelf, Is.True);
        Assert.That(artBtnGo.activeSelf, Is.True);

        Assert.That(statsImg.sprite, Is.EqualTo(sOff), "Stats should show Off sprite");
        Assert.That(chipImg.sprite, Is.EqualTo(cOff), "Chipset should show Off sprite");
        Assert.That(artImg.sprite, Is.EqualTo(aOn), "Artifact should show On sprite");

        Object.DestroyImmediate(root);
    }

    [Test]
    public void PauseModalController_EquippedChipsets_InitiallyContainsStandardGun()
    {
        GameObject root = new GameObject("PauseModalRoot", typeof(RectTransform));
        GameObject chipPanel = new GameObject("ChipsetPanel", typeof(RectTransform));
        chipPanel.transform.SetParent(root.transform);

        GameObject cardTemplate = new GameObject("EquippedChipCard", typeof(RectTransform), typeof(Image));
        cardTemplate.transform.SetParent(chipPanel.transform);

        GameObject iconFrame = new GameObject("IconFrame", typeof(RectTransform), typeof(Image));
        iconFrame.transform.SetParent(cardTemplate.transform);

        GameObject gunIcon = new GameObject("GunIcon", typeof(RectTransform), typeof(Image));
        gunIcon.transform.SetParent(cardTemplate.transform);

        GameObject lvlBadge = new GameObject("LvlBadge", typeof(RectTransform), typeof(Image));
        lvlBadge.transform.SetParent(cardTemplate.transform);
        GameObject labelObj = new GameObject("Label", typeof(RectTransform));
        labelObj.transform.SetParent(lvlBadge.transform);
        TMPro.TMP_Text label = labelObj.AddComponent<TMPro.TextMeshProUGUI>();

        PauseModalController pauseCtrl = root.AddComponent<PauseModalController>();
        pauseCtrl.ResetRuntimeEquippedChipsForTesting();
        pauseCtrl.SetReferencesForTesting(
            root, null, null, null, null, null,
            null, chipPanel, null,
            null, null, null,
            null, null, null,
            null, null, null
        );
        pauseCtrl.SetChipsetCardTemplateForTesting(cardTemplate);

        pauseCtrl.SelectMainTab(1);

        Assert.That(pauseCtrl.RuntimeEquippedChips.Count, Is.GreaterThanOrEqualTo(1));
        Assert.That(pauseCtrl.RuntimeEquippedChips[0].id, Is.EqualTo(1));
        Assert.That(pauseCtrl.RuntimeEquippedChips[0].level, Is.EqualTo(1));
        Assert.That(label.text, Is.EqualTo("LV.01"));

        Object.DestroyImmediate(root);
    }

    [Test]
    public void PauseModalController_SelectChipsetInLevelUp_AddsCardWithIconAndFrame()
    {
        GameObject root = new GameObject("PauseModalRoot", typeof(RectTransform));
        GameObject chipPanel = new GameObject("ChipsetPanel", typeof(RectTransform));
        chipPanel.transform.SetParent(root.transform);

        GameObject cardTemplate = new GameObject("EquippedChipCard", typeof(RectTransform), typeof(Image));
        cardTemplate.transform.SetParent(chipPanel.transform);

        GameObject iconFrame = new GameObject("IconFrame", typeof(RectTransform), typeof(Image));
        iconFrame.transform.SetParent(cardTemplate.transform);

        GameObject gunIcon = new GameObject("GunIcon", typeof(RectTransform), typeof(Image));
        gunIcon.transform.SetParent(cardTemplate.transform);

        GameObject lvlBadge = new GameObject("LvlBadge", typeof(RectTransform), typeof(Image));
        lvlBadge.transform.SetParent(cardTemplate.transform);
        GameObject labelObj = new GameObject("Label", typeof(RectTransform));
        labelObj.transform.SetParent(lvlBadge.transform);
        labelObj.AddComponent<TMPro.TextMeshProUGUI>();

        PauseModalController pauseCtrl = root.AddComponent<PauseModalController>();
        pauseCtrl.ResetRuntimeEquippedChipsForTesting();
        pauseCtrl.SetReferencesForTesting(
            root, null, null, null, null, null,
            null, chipPanel, null,
            null, null, null,
            null, null, null,
            null, null, null
        );
        pauseCtrl.SetChipsetCardTemplateForTesting(cardTemplate);

        // Player picks Rifle at Level Up
        pauseCtrl.RegisterOrUpdateRuntimeChip(2, "Rifle", "rifle", 1, ChipTier.Magic);
        pauseCtrl.SelectMainTab(1);

        Assert.That(pauseCtrl.RuntimeEquippedChips.Count, Is.EqualTo(2));
        Assert.That(pauseCtrl.RuntimeEquippedChips[1].name, Is.EqualTo("Rifle"));
        Assert.That(pauseCtrl.SpawnedChipCards.Count, Is.GreaterThanOrEqualTo(1));

        GameObject rifleCard = pauseCtrl.SpawnedChipCards[0];
        Assert.That(rifleCard.activeSelf, Is.True);

        TMPro.TMP_Text badgeText = rifleCard.transform.Find("LvlBadge")?.GetComponentInChildren<TMPro.TMP_Text>();
        Assert.That(badgeText, Is.Not.Null);
        Assert.That(badgeText.text, Is.EqualTo("LV.01"));

        // Player picks Rifle again to upgrade to LV.02
        pauseCtrl.RegisterOrUpdateRuntimeChip(2, "Rifle", "rifle", 2, ChipTier.Magic);
        pauseCtrl.RefreshEquippedChips();

        Assert.That(badgeText.text, Is.EqualTo("LV.02"));

        Object.DestroyImmediate(root);
    }

    [Test]
    public void PauseModalController_MultipleChipsets_ArrangesHorizontallyAcrossRow()
    {
        GameObject root = new GameObject("PauseModalRoot", typeof(RectTransform));
        GameObject chipPanel = new GameObject("ChipsetPanel", typeof(RectTransform));
        chipPanel.transform.SetParent(root.transform);

        GameObject cardTemplate = new GameObject("EquippedChipCard", typeof(RectTransform), typeof(Image));
        cardTemplate.transform.SetParent(chipPanel.transform);

        GameObject lvlBadge = new GameObject("LvlBadge", typeof(RectTransform), typeof(Image));
        lvlBadge.transform.SetParent(cardTemplate.transform);
        GameObject labelObj = new GameObject("Label", typeof(RectTransform));
        labelObj.transform.SetParent(lvlBadge.transform);
        labelObj.AddComponent<TMPro.TextMeshProUGUI>();

        PauseModalController pauseCtrl = root.AddComponent<PauseModalController>();
        pauseCtrl.ResetRuntimeEquippedChipsForTesting();
        pauseCtrl.SetReferencesForTesting(
            root, null, null, null, null, null,
            null, chipPanel, null,
            null, null, null,
            null, null, null,
            null, null, null
        );
        pauseCtrl.SetChipsetCardTemplateForTesting(cardTemplate);

        // Add 4 chips: Standard Gun (1), Rifle (2), Shotgun (8), Gun Turret (6)
        pauseCtrl.RegisterOrUpdateRuntimeChip(1, "Standard Gun", "standard-gun", 1, ChipTier.Magic);
        pauseCtrl.RegisterOrUpdateRuntimeChip(2, "Rifle", "rifle", 1, ChipTier.Magic);
        pauseCtrl.RegisterOrUpdateRuntimeChip(8, "Shotgun", "shotgun", 1, ChipTier.Magic);
        pauseCtrl.RegisterOrUpdateRuntimeChip(6, "Gun Turret", "gun-turret", 1, ChipTier.Magic);

        pauseCtrl.SelectMainTab(1);

        RectTransform card0Rt = cardTemplate.GetComponent<RectTransform>();
        Assert.That(card0Rt.anchoredPosition.x, Is.EqualTo(-320f).Within(0.1f));

        Assert.That(pauseCtrl.SpawnedChipCards.Count, Is.GreaterThanOrEqualTo(3));
        RectTransform card1Rt = pauseCtrl.SpawnedChipCards[0].GetComponent<RectTransform>();
        RectTransform card2Rt = pauseCtrl.SpawnedChipCards[1].GetComponent<RectTransform>();
        RectTransform card3Rt = pauseCtrl.SpawnedChipCards[2].GetComponent<RectTransform>();

        Assert.That(card1Rt.anchoredPosition.x, Is.EqualTo(-160f).Within(0.1f));
        Assert.That(card2Rt.anchoredPosition.x, Is.EqualTo(0f).Within(0.1f));
        Assert.That(card3Rt.anchoredPosition.x, Is.EqualTo(160f).Within(0.1f));

        Object.DestroyImmediate(root);
    }
}

