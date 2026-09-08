using System.Linq;
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
        Transform migratedFrame = cardTemplate.transform.Find("IconFrameAssetSlot");
        Assert.That(migratedFrame, Is.Not.Null);
        Assert.That(migratedFrame.Find("ChipIcon"), Is.Not.Null);
        Assert.That(lvlBadge.activeSelf, Is.False, "Badge chữ cũ phải ẩn khi dùng level pips của khung chipset.");

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

        Transform rifleFrame = rifleCard.transform.Find("IconFrameAssetSlot");
        Assert.That(rifleFrame, Is.Not.Null);
        Assert.That(rifleFrame.Find("ChipIcon"), Is.Not.Null);
        Assert.That(rifleFrame.Find("RuntimeLevelPip_1"), Is.Not.Null);

        // Player picks Rifle again to upgrade to LV.02
        pauseCtrl.RegisterOrUpdateRuntimeChip(2, "Rifle", "rifle", 2, ChipTier.Magic);
        pauseCtrl.RefreshEquippedChips();

        Assert.That(pauseCtrl.RuntimeEquippedChips[1].level, Is.EqualTo(2));

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

    [Test]
    public void PauseModalController_EquippedChipsets_RetainsDistinctIconsAndTierFramesFromLevelUp()
    {
        GameObject root = new GameObject("PauseModalRoot", typeof(RectTransform));
        GameObject chipPanel = new GameObject("ChipsetPanel", typeof(RectTransform));
        chipPanel.transform.SetParent(root.transform);

        GameObject cardTemplate = new GameObject("EquippedChipCard", typeof(RectTransform), typeof(Image));
        cardTemplate.transform.SetParent(chipPanel.transform);

        GameObject iconFrame = new GameObject("IconFrame", typeof(RectTransform), typeof(Image));
        iconFrame.transform.SetParent(cardTemplate.transform);
        Image frameImg = iconFrame.GetComponent<Image>();

        GameObject gunIcon = new GameObject("GunIcon", typeof(RectTransform), typeof(Image));
        gunIcon.transform.SetParent(cardTemplate.transform);
        Image iconImg = gunIcon.GetComponent<Image>();

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

        // Create distinct dummy sprites
        Sprite gunIconSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
        gunIconSprite.name = "StandardGunIcon";
        Sprite rifleIconSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
        rifleIconSprite.name = "RifleIcon";
        Sprite shotgunIconSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
        shotgunIconSprite.name = "ShotgunIcon";

        Sprite magicFrameSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
        magicFrameSprite.name = "MagicFrame";
        Sprite rareFrameSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
        rareFrameSprite.name = "RareFrame";
        Sprite uniqueFrameSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
        uniqueFrameSprite.name = "UniqueFrame";

        // Register 3 chips with custom icons and frames (representing choice from Level Up)
        pauseCtrl.RegisterOrUpdateRuntimeChip(1, "Standard Gun", "standard-gun", 1, ChipTier.Magic, gunIconSprite, magicFrameSprite);
        pauseCtrl.RegisterOrUpdateRuntimeChip(2, "Rifle", "rifle", 2, ChipTier.Rare, rifleIconSprite, rareFrameSprite);
        pauseCtrl.RegisterOrUpdateRuntimeChip(8, "Shotgun", "shotgun", 1, ChipTier.Unique, shotgunIconSprite, uniqueFrameSprite);

        pauseCtrl.SelectMainTab(1);

        // Slot 0 (Standard Gun)
        Assert.That(pauseCtrl.RuntimeEquippedChips[0].cachedIconSprite, Is.EqualTo(gunIconSprite));
        Assert.That(pauseCtrl.RuntimeEquippedChips[0].cachedFrameSprite, Is.EqualTo(magicFrameSprite));
        Assert.That(frameImg.sprite, Is.EqualTo(magicFrameSprite));
        Assert.That(iconImg.sprite, Is.EqualTo(gunIconSprite));

        // Slot 1 (Rifle)
        Assert.That(pauseCtrl.SpawnedChipCards.Count, Is.GreaterThanOrEqualTo(2));
        GameObject card1 = pauseCtrl.SpawnedChipCards[0];
        Image card1Frame = card1.transform.Find("IconFrameAssetSlot")?.GetComponent<Image>();
        Image card1Icon = card1.transform.Find("IconFrameAssetSlot/ChipIcon")?.GetComponent<Image>();
        Assert.That(card1Frame, Is.Not.Null);
        Assert.That(card1Icon, Is.Not.Null);
        Assert.That(card1Frame.sprite, Is.EqualTo(rareFrameSprite));
        Assert.That(card1Icon.sprite, Is.EqualTo(rifleIconSprite));

        // Slot 2 (Shotgun)
        GameObject card2 = pauseCtrl.SpawnedChipCards[1];
        Image card2Frame = card2.transform.Find("IconFrameAssetSlot")?.GetComponent<Image>();
        Image card2Icon = card2.transform.Find("IconFrameAssetSlot/ChipIcon")?.GetComponent<Image>();
        Assert.That(card2Frame, Is.Not.Null);
        Assert.That(card2Icon, Is.Not.Null);
        Assert.That(card2Frame.sprite, Is.EqualTo(uniqueFrameSprite));
        Assert.That(card2Icon.sprite, Is.EqualTo(shotgunIconSprite));

        Assert.That(card2Icon.rectTransform.rect.width, Is.LessThan(card2Frame.rectTransform.rect.width));
        Assert.That(card2Icon.rectTransform.rect.height, Is.LessThan(card2Frame.rectTransform.rect.height));

        Object.DestroyImmediate(root);
        Object.DestroyImmediate(gunIconSprite);
        Object.DestroyImmediate(rifleIconSprite);
        Object.DestroyImmediate(shotgunIconSprite);
        Object.DestroyImmediate(magicFrameSprite);
        Object.DestroyImmediate(rareFrameSprite);
        Object.DestroyImmediate(uniqueFrameSprite);
    }

    [Test]
    public void PauseModalController_ChipIcon_MatchesTargetTransform()
    {
        GameObject root = new GameObject("PauseModalRoot");
        PauseModalController pauseCtrl = root.AddComponent<PauseModalController>();

        GameObject chipPanel = new GameObject("ChipsetPanel");
        chipPanel.transform.SetParent(root.transform);

        GameObject cardTemplate = new GameObject("EquippedChipCard", typeof(RectTransform), typeof(Image));
        cardTemplate.transform.SetParent(chipPanel.transform);

        pauseCtrl.SetChipsetCardTemplateForTesting(cardTemplate);

        pauseCtrl.RegisterOrUpdateRuntimeChip(1, "Standard Gun", "standard-gun", 1, ChipTier.Magic, null, null);
        pauseCtrl.SelectMainTab(1);

        Transform iconTr = cardTemplate.transform.Find("IconFrameAssetSlot/ChipIcon");
        Assert.That(iconTr, Is.Not.Null);
        RectTransform iconRt = iconTr.GetComponent<RectTransform>();
        Assert.That(iconRt, Is.Not.Null);

        Assert.That(iconRt.anchoredPosition.x, Is.EqualTo(-0.5f).Within(0.01f));
        Assert.That(iconRt.anchoredPosition.y, Is.EqualTo(20.1f).Within(0.01f));
        Assert.That(iconRt.sizeDelta.x, Is.EqualTo(93.6f).Within(0.01f));
        Assert.That(iconRt.sizeDelta.y, Is.EqualTo(72f).Within(0.01f));
        Assert.That(iconRt.anchorMin, Is.EqualTo(new Vector2(0.5f, 0.5f)));
        Assert.That(iconRt.anchorMax, Is.EqualTo(new Vector2(0.5f, 0.5f)));
        Assert.That(iconRt.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
        Assert.That(iconRt.localScale, Is.EqualTo(Vector3.one));

        Object.DestroyImmediate(root);
    }

    [Test]
    public void PauseModalController_ArtifactIconSlots_PreservesPositionAndSize_WhenRefreshing()
    {
        GameObject root = new GameObject("PauseModalRoot");
        PauseModalController pauseCtrl = root.AddComponent<PauseModalController>();

        GameObject artPanel = new GameObject("ArtifactPanel", typeof(RectTransform));
        artPanel.transform.SetParent(root.transform);

        GameObject slot1 = new GameObject("ArtifactIcon_1", typeof(RectTransform), typeof(Image));
        slot1.transform.SetParent(artPanel.transform);
        RectTransform rt1 = slot1.GetComponent<RectTransform>();
        rt1.anchoredPosition = new Vector2(123.4f, -456.7f);
        rt1.sizeDelta = new Vector2(85.5f, 95.5f);
        rt1.localScale = new Vector3(1.2f, 1.2f, 1.2f);

        GameObject slot2 = new GameObject("ArtifactIcon_2", typeof(RectTransform), typeof(Image));
        slot2.transform.SetParent(artPanel.transform);
        RectTransform rt2 = slot2.GetComponent<RectTransform>();
        rt2.anchoredPosition = new Vector2(200f, 100f);
        rt2.sizeDelta = new Vector2(64f, 64f);

        pauseCtrl.SetArtifactIconSlotsForTesting(slot1, slot2);

        // Call RefreshEquippedArtifacts
        pauseCtrl.RefreshEquippedArtifacts();

        // 1. Check that RectTransform properties are 100% PRESERVED
        Assert.That(rt1.anchoredPosition.x, Is.EqualTo(123.4f).Within(0.01f));
        Assert.That(rt1.anchoredPosition.y, Is.EqualTo(-456.7f).Within(0.01f));
        Assert.That(rt1.sizeDelta.x, Is.EqualTo(85.5f).Within(0.01f));
        Assert.That(rt1.sizeDelta.y, Is.EqualTo(95.5f).Within(0.01f));
        Assert.That(rt1.localScale.x, Is.EqualTo(1.2f).Within(0.01f));

        Assert.That(rt2.anchoredPosition.x, Is.EqualTo(200f).Within(0.01f));
        Assert.That(rt2.anchoredPosition.y, Is.EqualTo(100f).Within(0.01f));
        Assert.That(rt2.sizeDelta.x, Is.EqualTo(64f).Within(0.01f));
        Assert.That(rt2.sizeDelta.y, Is.EqualTo(64f).Within(0.01f));

        Object.DestroyImmediate(root);
    }

    [Test]
    public void PauseModalController_ArtifactIconSlots_AutoWiresChildrenFromArtifactPanel()
    {
        GameObject root = new GameObject("PauseModalRoot");
        PauseModalController pauseCtrl = root.AddComponent<PauseModalController>();

        GameObject artPanel = new GameObject("ArtifactPanel", typeof(RectTransform));
        artPanel.transform.SetParent(root.transform);

        GameObject msgObj = new GameObject("ArtifactMessage", typeof(RectTransform));
        msgObj.transform.SetParent(artPanel.transform);

        GameObject slot1 = new GameObject("ArtifactIcon_1", typeof(RectTransform), typeof(Image));
        slot1.transform.SetParent(artPanel.transform);

        GameObject slot2 = new GameObject("ArtifactIcon_2", typeof(RectTransform), typeof(Image));
        slot2.transform.SetParent(artPanel.transform);

        GameObject slot3 = new GameObject("ArtifactIcon_3", typeof(RectTransform), typeof(Image));
        slot3.transform.SetParent(artPanel.transform);

        // AutoWire
        pauseCtrl.AutoWireArtifactIconSlots();

        Assert.That(pauseCtrl.ArtifactIconSlots.Count, Is.EqualTo(3));
        Assert.That(pauseCtrl.ArtifactIconSlots[0], Is.EqualTo(slot1));
        Assert.That(pauseCtrl.ArtifactIconSlots[1], Is.EqualTo(slot2));
        Assert.That(pauseCtrl.ArtifactIconSlots[2], Is.EqualTo(slot3));
        Assert.That(pauseCtrl.ArtifactIconSlots.Contains(msgObj), Is.False, "ArtifactMessage should never be treated as an icon slot");

        Object.DestroyImmediate(root);
    }

    [Test]
    public void PauseModalController_ArrangeArtifactSlotsGrid_Arranges4PerRow()
    {
        GameObject root = new GameObject("PauseModalRoot");
        PauseModalController pauseCtrl = root.AddComponent<PauseModalController>();

        GameObject artPanel = new GameObject("ArtifactPanel", typeof(RectTransform));
        artPanel.transform.SetParent(root.transform);

        System.Collections.Generic.List<GameObject> slots = new System.Collections.Generic.List<GameObject>();
        for (int i = 0; i < 8; i++)
        {
            GameObject slot = new GameObject($"ArtifactIcon_{i + 1}", typeof(RectTransform), typeof(Image));
            slot.transform.SetParent(artPanel.transform);
            slots.Add(slot);
        }

        pauseCtrl.SetArtifactIconSlotsForTesting(slots.ToArray());
        pauseCtrl.ArrangeArtifactSlotsGrid(4);

        RectTransform rt0 = slots[0].GetComponent<RectTransform>();
        RectTransform rt1 = slots[1].GetComponent<RectTransform>();
        RectTransform rt2 = slots[2].GetComponent<RectTransform>();
        RectTransform rt3 = slots[3].GetComponent<RectTransform>();
        RectTransform rt4 = slots[4].GetComponent<RectTransform>();
        RectTransform rt7 = slots[7].GetComponent<RectTransform>();

        // Check Row 0 Y positions are identical
        Assert.That(rt0.anchoredPosition.y, Is.EqualTo(rt1.anchoredPosition.y).Within(0.001f));
        Assert.That(rt1.anchoredPosition.y, Is.EqualTo(rt2.anchoredPosition.y).Within(0.001f));
        Assert.That(rt2.anchoredPosition.y, Is.EqualTo(rt3.anchoredPosition.y).Within(0.001f));

        // Check Row 1 Y positions are identical and below Row 0
        Assert.That(rt4.anchoredPosition.y, Is.EqualTo(rt7.anchoredPosition.y).Within(0.001f));
        Assert.That(rt4.anchoredPosition.y, Is.LessThan(rt0.anchoredPosition.y));

        // Check Column 0 X positions (Slot 0 and Slot 4) are identical
        Assert.That(rt0.anchoredPosition.x, Is.EqualTo(rt4.anchoredPosition.x).Within(0.001f));

        // Check Column 3 X positions (Slot 3 and Slot 7) are identical
        Assert.That(rt3.anchoredPosition.x, Is.EqualTo(rt7.anchoredPosition.x).Within(0.001f));

        // Check spacing between columns is uniform
        float step01 = rt1.anchoredPosition.x - rt0.anchoredPosition.x;
        float step12 = rt2.anchoredPosition.x - rt1.anchoredPosition.x;
        float step23 = rt3.anchoredPosition.x - rt2.anchoredPosition.x;
        Assert.That(step01, Is.EqualTo(step12).Within(0.001f));
        Assert.That(step12, Is.EqualTo(step23).Within(0.001f));

        Object.DestroyImmediate(root);
    }

    [Test]
    public void PauseModalController_ShowArtifactDetail_PopulatesFieldsAndActivates()
    {
        GameObject root = new GameObject("PauseModalRoot");
        PauseModalController pauseCtrl = root.AddComponent<PauseModalController>();

        GameObject detailPanel = new GameObject("ArtifactDetailDialog", typeof(RectTransform));
        detailPanel.transform.SetParent(root.transform);
        detailPanel.SetActive(false);

        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(detailPanel.transform);
        Image iconImg = iconObj.GetComponent<Image>();

        GameObject nameObj = new GameObject("NameText", typeof(RectTransform));
        nameObj.transform.SetParent(detailPanel.transform);
        TMPro.TMP_Text nameText = nameObj.AddComponent<TMPro.TextMeshProUGUI>();

        GameObject loreObj = new GameObject("LoreText", typeof(RectTransform));
        loreObj.transform.SetParent(detailPanel.transform);
        TMPro.TMP_Text loreText = loreObj.AddComponent<TMPro.TextMeshProUGUI>();

        GameObject statObj = new GameObject("StatText", typeof(RectTransform));
        statObj.transform.SetParent(detailPanel.transform);
        TMPro.TMP_Text statText = statObj.AddComponent<TMPro.TextMeshProUGUI>();

        GameObject okBtnObj = new GameObject("OkButton", typeof(RectTransform), typeof(Button));
        okBtnObj.transform.SetParent(detailPanel.transform);
        Button okBtn = okBtnObj.GetComponent<Button>();

        pauseCtrl.SetArtifactDetailDialogForTesting(detailPanel, iconImg, nameText, loreText, statText, okBtn);

        Sprite testSprite = Sprite.Create(new Texture2D(2, 2), new Rect(0, 0, 2, 2), Vector2.zero);
        ArtifactData testData = ScriptableObject.CreateInstance<ArtifactData>();
        testData.artifactName = "Titanium Fabric";
        testData.statType = ArtifactStatType.DamageReduction;
        testData.statValue = 10f;
        testData.loreDescription = "Sturdy titanium. Covers the body.";
        testData.icon = testSprite;

        // Show detail
        pauseCtrl.ShowArtifactDetail(testData);

        Assert.That(detailPanel.activeSelf, Is.True, "Detail panel should be active after ShowArtifactDetail");
        Assert.That(nameText.text, Is.EqualTo("Titanium Fabric"));
        Assert.That(statText.text, Is.EqualTo("DEF +10"));
        Assert.That(loreText.text, Is.EqualTo("Sturdy titanium. Covers the body."));
        Assert.That(iconImg.sprite, Is.EqualTo(testSprite));

        // Hide detail
        pauseCtrl.HideArtifactDetail();
        Assert.That(detailPanel.activeSelf, Is.False, "Detail panel should be inactive after HideArtifactDetail");

        Object.DestroyImmediate(testData);
        Object.DestroyImmediate(root);
    }

    [Test]
    public void PauseModalController_ClickArtifactSlot_OpensDetailDialog()
    {
        GameObject root = new GameObject("PauseModalRoot");
        PauseModalController pauseCtrl = root.AddComponent<PauseModalController>();

        GameObject detailPanel = new GameObject("ArtifactDetailDialog", typeof(RectTransform));
        detailPanel.transform.SetParent(root.transform);
        detailPanel.SetActive(false);

        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        Image iconImg = iconObj.GetComponent<Image>();
        TMPro.TMP_Text nameText = new GameObject("NameText").AddComponent<TMPro.TextMeshProUGUI>();
        TMPro.TMP_Text loreText = new GameObject("LoreText").AddComponent<TMPro.TextMeshProUGUI>();
        TMPro.TMP_Text statText = new GameObject("StatText").AddComponent<TMPro.TextMeshProUGUI>();
        Button okBtn = new GameObject("OkBtn").AddComponent<Button>();

        pauseCtrl.SetArtifactDetailDialogForTesting(detailPanel, iconImg, nameText, loreText, statText, okBtn);

        GameObject slotObj = new GameObject("Slot_1", typeof(RectTransform), typeof(Image), typeof(Button));
        Button slotBtn = slotObj.GetComponent<Button>();

        ArtifactData data = ScriptableObject.CreateInstance<ArtifactData>();
        data.artifactName = "Emergency Repair Kit";
        data.statType = ArtifactStatType.MaxHealthPercent;
        data.statValue = 25f;
        data.loreDescription = "Military field emergency unit.";

        pauseCtrl.WireSlotButtonClick(slotObj, 0, data);

        // Click slot button
        slotBtn.onClick.Invoke();

        Assert.That(detailPanel.activeSelf, Is.True);
        Assert.That(nameText.text, Is.EqualTo("Emergency Repair Kit"));
        Assert.That(statText.text, Is.EqualTo("HP +25%"));
        Assert.That(loreText.text, Is.EqualTo("Military field emergency unit."));

        // Click OK button to close
        okBtn.onClick.Invoke();
        Assert.That(detailPanel.activeSelf, Is.False);

        Object.DestroyImmediate(data);
        Object.DestroyImmediate(root);
    }
}

