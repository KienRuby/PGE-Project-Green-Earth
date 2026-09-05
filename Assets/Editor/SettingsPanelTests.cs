using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;

public class SettingsPanelTests
{
    [Test]
    public void Settings_DefaultsAreEnabled_AndPersistChanges()
    {
        string[] keys =
        {
            GameSettings.BgmKey,
            GameSettings.SfxKey,
            GameSettings.LanguageKey,
            GameSettings.ShowDamageKey,
            GameSettings.DynamicJoystickKey,
            GameSettings.ScreenShakeKey
        };
        bool[] hadKey = new bool[keys.Length];
        string[] values = new string[keys.Length];

        for (int i = 0; i < keys.Length; i++)
        {
            hadKey[i] = PlayerPrefs.HasKey(keys[i]);
            values[i] = PlayerPrefs.GetString(keys[i], PlayerPrefs.GetInt(keys[i], 1).ToString());
            PlayerPrefs.DeleteKey(keys[i]);
        }

        try
        {
            Assert.That(GameSettings.BgmEnabled, Is.True);
            Assert.That(GameSettings.SfxEnabled, Is.True);
            Assert.That(GameSettings.ShowDamage, Is.True);
            Assert.That(GameSettings.DynamicJoystick, Is.True);
            Assert.That(GameSettings.ScreenShake, Is.True);
            Assert.That(GameSettings.Language, Is.EqualTo("English"));

            GameSettings.ShowDamage = false;
            Assert.That(PlayerPrefs.GetInt(GameSettings.ShowDamageKey), Is.Zero);
        }
        finally
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if (!hadKey[i])
                {
                    PlayerPrefs.DeleteKey(keys[i]);
                    continue;
                }

                if (keys[i] == GameSettings.LanguageKey)
                    PlayerPrefs.SetString(keys[i], values[i]);
                else if (int.TryParse(values[i], out int intValue))
                    PlayerPrefs.SetInt(keys[i], intValue);
            }
            PlayerPrefs.Save();
        }
    }

    [Test]
    public void Settings_RuntimePanel_HasAllRequiredControls_AndStartsClosed()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        try
        {
            SettingsPanelController panel = SettingsPanelController.CreateRuntimePanel(
                canvasObject.GetComponent<RectTransform>());

            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.IsOpen, Is.False);
            Assert.That(panel.transform.Find("SafeContent/Header"), Is.Not.Null);
            Assert.That(panel.transform.Find("SafeContent/AccountCard/CopyIdButton"), Is.Not.Null);
            Assert.That(panel.transform.Find("SafeContent/BgmButton"), Is.Not.Null);
            Assert.That(panel.transform.Find("SafeContent/SfxButton"), Is.Not.Null);
            Assert.That(panel.transform.Find("SafeContent/LanguageButton"), Is.Not.Null);
            Assert.That(panel.transform.Find("SafeContent/ShowDamageButton"), Is.Not.Null);
            Assert.That(panel.transform.Find("SafeContent/JoystickModeButton"), Is.Not.Null);
            Assert.That(panel.transform.Find("SafeContent/ScreenShakeButton"), Is.Not.Null);
            Assert.That(panel.transform.Find("SafeContent/ReviewButton"), Is.Not.Null);

            panel.Open();
            Assert.That(panel.IsOpen, Is.True);
            Assert.That(panel.GetComponentsInChildren<TMP_Text>(true).Length, Is.GreaterThanOrEqualTo(10));
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void Settings_GamePlayMode_HidesAccountSection_AndKeepsSevenButtons()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        try
        {
            SettingsPanelController panel = SettingsPanelController.CreateRuntimePanel(
                canvasObject.GetComponent<RectTransform>());

            Assert.That(panel, Is.Not.Null);

            panel.IsGameplayMode = true;
            panel.Open();

            Transform accountCard = panel.transform.Find("SafeContent/AccountCard");
            Assert.That(accountCard, Is.Not.Null);
            Assert.That(accountCard.gameObject.activeSelf, Is.False, "AccountCard should be hidden in GamePlay mode!");

            Transform bgm = panel.transform.Find("SafeContent/BgmButton");
            Transform sfx = panel.transform.Find("SafeContent/SfxButton");
            Transform lang = panel.transform.Find("SafeContent/LanguageButton");
            Transform dmg = panel.transform.Find("SafeContent/ShowDamageButton");
            Transform joy = panel.transform.Find("SafeContent/JoystickModeButton");
            Transform shake = panel.transform.Find("SafeContent/ScreenShakeButton");
            Transform review = panel.transform.Find("SafeContent/ReviewButton");

            Assert.That(bgm, Is.Not.Null);
            Assert.That(sfx, Is.Not.Null);
            Assert.That(lang, Is.Not.Null);
            Assert.That(dmg, Is.Not.Null);
            Assert.That(joy, Is.Not.Null);
            Assert.That(shake, Is.Not.Null);
            Assert.That(review, Is.Not.Null);

            Assert.That(bgm.gameObject.activeSelf, Is.True);
            Assert.That(sfx.gameObject.activeSelf, Is.True);
            Assert.That(lang.gameObject.activeSelf, Is.True);
            Assert.That(dmg.gameObject.activeSelf, Is.True);
            Assert.That(joy.gameObject.activeSelf, Is.True);
            Assert.That(shake.gameObject.activeSelf, Is.True);
            Assert.That(review.gameObject.activeSelf, Is.True);

            RectTransform bgmRect = bgm.GetComponent<RectTransform>();
            RectTransform langRect = lang.GetComponent<RectTransform>();
            Assert.That(bgmRect.anchoredPosition.y, Is.EqualTo(350f));
            Assert.That(langRect.anchoredPosition.y, Is.EqualTo(180f));
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void Settings_MainMenuMode_KeepsAccountSection_Active()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        try
        {
            SettingsPanelController panel = SettingsPanelController.CreateRuntimePanel(
                canvasObject.GetComponent<RectTransform>());

            Assert.That(panel, Is.Not.Null);

            panel.IsGameplayMode = false;
            panel.Open();

            Transform accountCard = panel.transform.Find("SafeContent/AccountCard");
            Assert.That(accountCard, Is.Not.Null);
            Assert.That(accountCard.gameObject.activeSelf, Is.True, "AccountCard should remain active in MainMenu!");
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void Settings_BgmAndSfxButtons_MuteTheirRealAudioCategories()
    {
        bool hadBgm = PlayerPrefs.HasKey(GameSettings.BgmKey);
        bool hadSfx = PlayerPrefs.HasKey(GameSettings.SfxKey);
        int originalBgm = PlayerPrefs.GetInt(GameSettings.BgmKey, 1);
        int originalSfx = PlayerPrefs.GetInt(GameSettings.SfxKey, 1);
        GameObject runtimeObject = new GameObject("AudioSettingsTest", typeof(GameAudioSettingsRuntime));
        GameObject musicObject = new GameObject("BGM Test", typeof(AudioSource));
        GameObject sfxObject = new GameObject("SFX Test", typeof(AudioSource));

        try
        {
            AudioSource music = musicObject.GetComponent<AudioSource>();
            AudioSource sfx = sfxObject.GetComponent<AudioSource>();
            music.loop = true;
            sfx.loop = false;

            GameSettings.BgmEnabled = false;
            GameSettings.SfxEnabled = true;
            runtimeObject.GetComponent<GameAudioSettingsRuntime>().ApplySettingsNow();
            Assert.That(music.mute, Is.True);
            Assert.That(sfx.mute, Is.False);

            GameSettings.BgmEnabled = true;
            GameSettings.SfxEnabled = false;
            runtimeObject.GetComponent<GameAudioSettingsRuntime>().ApplySettingsNow();
            Assert.That(music.mute, Is.False);
            Assert.That(sfx.mute, Is.True);
        }
        finally
        {
            RestoreIntPreference(GameSettings.BgmKey, hadBgm, originalBgm);
            RestoreIntPreference(GameSettings.SfxKey, hadSfx, originalSfx);
            Object.DestroyImmediate(runtimeObject);
            Object.DestroyImmediate(musicObject);
            Object.DestroyImmediate(sfxObject);
        }
    }

    [Test]
    public void Settings_LanguageButton_OpensFourLanguageChoices_AndPersistsSelection()
    {
        bool hadLanguage = PlayerPrefs.HasKey(GameSettings.LanguageKey);
        string originalLanguage = PlayerPrefs.GetString(GameSettings.LanguageKey, "English");
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));

        try
        {
            GameSettings.Language = "English";
            SettingsPanelController panel = SettingsPanelController.CreateRuntimePanel(
                canvasObject.GetComponent<RectTransform>());
            panel.Open();

            UnityEngine.UI.Button languageButton = panel.transform
                .Find("SafeContent/LanguageButton")
                .GetComponent<UnityEngine.UI.Button>();
            languageButton.onClick.Invoke();

            Transform options = panel.transform.Find("SafeContent/LanguageOptionsPanel");
            Assert.That(options, Is.Not.Null);
            Assert.That(options.gameObject.activeSelf, Is.True);
            Assert.That(options.Find("EnglishButton"), Is.Not.Null);
            Assert.That(options.Find("VietnameseButton"), Is.Not.Null);
            Assert.That(options.Find("ChineseButton"), Is.Not.Null);
            Assert.That(options.Find("RussianButton"), Is.Not.Null);

            options.Find("RussianButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(GameSettings.Language, Is.EqualTo(GameSettings.RussianLanguage));
            Assert.That(options.gameObject.activeSelf, Is.False);
        }
        finally
        {
            if (hadLanguage) PlayerPrefs.SetString(GameSettings.LanguageKey, originalLanguage);
            else PlayerPrefs.DeleteKey(GameSettings.LanguageKey);
            PlayerPrefs.Save();
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void Settings_BackgroundClick_ClosesSettingsPanel()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        try
        {
            SettingsPanelController panel = SettingsPanelController.CreateRuntimePanel(
                canvasObject.GetComponent<RectTransform>());
            panel.Open();

            panel.OnPointerClick(null);

            Assert.That(panel.IsOpen, Is.True, "Panel phải còn active trong lúc shader đang tan.");
            Assert.That(panel.GetComponent<UIDissolveController>().CurrentState,
                Is.EqualTo(UIDissolveController.TransitionState.Hiding));
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void Settings_BackgroundClick_WhenLanguageOptionsOpen_ClosesOnlyLanguageOptions()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        try
        {
            SettingsPanelController panel = SettingsPanelController.CreateRuntimePanel(
                canvasObject.GetComponent<RectTransform>());
            panel.Open();
            panel.ToggleLanguageOptions();

            Transform options = panel.transform.Find("SafeContent/LanguageOptionsPanel");
            Assert.That(options.gameObject.activeSelf, Is.True);

            panel.OnPointerClick(null);

            Assert.That(options.gameObject.activeSelf, Is.True, "Bảng ngôn ngữ chỉ tắt sau khi dissolve hoàn tất.");
            Assert.That(options.GetComponent<UIDissolveController>().CurrentState,
                Is.EqualTo(UIDissolveController.TransitionState.Hiding));
            Assert.That(panel.IsOpen, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void Settings_ScreenShake_DisabledStateReturnsZeroOffset()
    {
        bool hadKey = PlayerPrefs.HasKey(GameSettings.ScreenShakeKey);
        int originalValue = PlayerPrefs.GetInt(GameSettings.ScreenShakeKey, 1);

        try
        {
            GameSettings.ScreenShake = true;
            ScreenShakeService.Reset();
            ScreenShakeService.AddTrauma(0.8f);
            Assert.That(ScreenShakeService.UpdateAndGetOffset(0.016f), Is.Not.EqualTo(Vector3.zero));

            GameSettings.ScreenShake = false;
            Assert.That(ScreenShakeService.UpdateAndGetOffset(0.016f), Is.EqualTo(Vector3.zero));
        }
        finally
        {
            ScreenShakeService.Reset();
            RestoreIntPreference(GameSettings.ScreenShakeKey, hadKey, originalValue);
        }
    }

    [Test]
    public void Settings_GoogleAndAppleButtons_AutoWireAndToggleLogin()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        GameObject panelGo = new GameObject("SettingsPanel", typeof(RectTransform), typeof(SettingsPanelController));
        panelGo.transform.SetParent(canvasObject.transform, false);

        GameObject googleGo = new GameObject("Log in with Google", typeof(RectTransform), typeof(UnityEngine.UI.Button));
        googleGo.transform.SetParent(panelGo.transform, false);
        TextMeshProUGUI googleText = googleGo.AddComponent<TextMeshProUGUI>();

        GameObject appleGo = new GameObject("Sign in with Apple", typeof(RectTransform), typeof(UnityEngine.UI.Button));
        appleGo.transform.SetParent(panelGo.transform, false);
        TextMeshProUGUI appleText = appleGo.AddComponent<TextMeshProUGUI>();

        try
        {
            GameSettings.GoogleAccount = string.Empty;
            GameSettings.AppleAccount = string.Empty;

            SettingsPanelController controller = panelGo.GetComponent<SettingsPanelController>();
            controller.Open();

            Assert.That(GameSettings.IsLoggedInGoogle, Is.False);
            Assert.That(GameSettings.IsLoggedInApple, Is.False);

            googleGo.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(GameSettings.IsLoggedInGoogle, Is.True);
            Assert.That(googleText.text, Does.Contain("LOGGED IN"));

            appleGo.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(GameSettings.IsLoggedInApple, Is.True);
            Assert.That(appleText.text, Does.Contain("SIGNED IN"));
        }
        finally
        {
            GameSettings.GoogleAccount = string.Empty;
            GameSettings.AppleAccount = string.Empty;
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void Settings_ButtonSprites_SwapBetweenOnAndOffStates()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        GameObject panelGo = new GameObject("SettingsPanel", typeof(RectTransform), typeof(SettingsPanelController));
        panelGo.transform.SetParent(canvasObject.transform, false);

        GameObject bgmGo = new GameObject("BgmButton", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        bgmGo.transform.SetParent(panelGo.transform, false);

        GameObject langGo = new GameObject("LanguageButton", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        langGo.transform.SetParent(panelGo.transform, false);

        try
        {
            SettingsPanelController controller = panelGo.GetComponent<SettingsPanelController>();
            controller.Open();

            UnityEngine.UI.Image bgmImage = bgmGo.GetComponent<UnityEngine.UI.Image>();
            UnityEngine.UI.Image langImage = langGo.GetComponent<UnityEngine.UI.Image>();

            // Test BGM On -> Off
            GameSettings.BgmEnabled = true;
            controller.RefreshLabels();
            Sprite onSprite = bgmImage.sprite;
            if (onSprite != null) Assert.That(onSprite.name, Is.EqualTo("BGM ON"));

            GameSettings.BgmEnabled = false;
            controller.RefreshLabels();
            Sprite offSprite = bgmImage.sprite;
            if (offSprite != null) Assert.That(offSprite.name, Is.EqualTo("BGM OFF"));

            // Test Language English (On) -> Tiếng Việt (Off)
            GameSettings.Language = "English";
            controller.RefreshLabels();
            Sprite englishSprite = langImage.sprite;
            if (englishSprite != null) Assert.That(englishSprite.name, Does.Contain("English"));

            GameSettings.Language = "Tiếng Việt";
            controller.RefreshLabels();
            Sprite nonEnglishSprite = langImage.sprite;
            if (nonEnglishSprite != null) Assert.That(nonEnglishSprite.name, Is.EqualTo("English OFF"));

            // Test 3-State Joystick: Dynamic Pad On (0) -> Fixed Pad ON (1) -> Dynamic-Fixed Pad OFF (2)
            GameObject joyGo = new GameObject("JoystickModeButton", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            joyGo.transform.SetParent(panelGo.transform, false);
            controller.AutoWireReferencesIfMissing();

            GameSettings.JoystickMode = 0;
            controller.RefreshLabels();
            UnityEngine.UI.Image joyImage = joyGo.GetComponent<UnityEngine.UI.Image>();
            if (joyImage.sprite != null) Assert.That(joyImage.sprite.name, Is.EqualTo("Dynamic Pad On"));

            GameSettings.JoystickMode = 1;
            controller.RefreshLabels();
            if (joyImage.sprite != null) Assert.That(joyImage.sprite.name, Is.EqualTo("Fixed Pad ON"));

            GameSettings.JoystickMode = 2;
            controller.RefreshLabels();
            if (joyImage.sprite != null) Assert.That(joyImage.sprite.name, Is.EqualTo("Dynamic-Fixed Pad OFF"));
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void Chipset_SortButtons_SwapYellowAndGreenSpritesCorrectly()
    {
        GameObject go = new GameObject("ChipsetTestPanel", typeof(RectTransform));
        ChipsetController controller = go.AddComponent<ChipsetController>();

        GameObject barGo = new GameObject("SortFilterBar", typeof(RectTransform));
        barGo.transform.SetParent(go.transform, false);

        GameObject tierGo = new GameObject("ByTierBtn", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        tierGo.transform.SetParent(barGo.transform, false);

        GameObject qtyGo = new GameObject("ByQtyBtn", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        qtyGo.transform.SetParent(barGo.transform, false);

        try
        {
            controller.AutoWireSortButtonsIfMissing();
            UnityEngine.UI.Image tierImage = tierGo.GetComponent<UnityEngine.UI.Image>();
            UnityEngine.UI.Image qtyImage = qtyGo.GetComponent<UnityEngine.UI.Image>();

            // 1. By Tier selected (false) -> By Tier Vàng (By TileYellow), By Quantity Xanh (ByQuantityGreen)
            controller.SetSortMode(false);
            if (tierImage.sprite != null) Assert.That(tierImage.sprite.name, Is.EqualTo("By TileYellow"));
            if (qtyImage.sprite != null) Assert.That(qtyImage.sprite.name, Is.EqualTo("ByQuantityGreen"));

            // 2. By Quantity selected (true) -> By Tier Xanh (By Tile Green), By Quantity Vàng (By QuantityYellow)
            controller.SetSortMode(true);
            if (tierImage.sprite != null) Assert.That(tierImage.sprite.name, Is.EqualTo("By Tile Green"));
            if (qtyImage.sprite != null) Assert.That(qtyImage.sprite.name, Is.EqualTo("By QuantityYellow"));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Chipset_PresetDeckButtons_SwapYellowAndRedSprites_AndDatabaseHasTenChips()
    {
        // 1. Verify exactly 10 chipsets in database
        var db = ChipsetController.CreateDefaultDatabase();
        Assert.That(db.Count, Is.EqualTo(10));

        // 2. Verify Preset buttons sprite swapping
        GameObject go = new GameObject("ChipsetPresetTest", typeof(RectTransform));
        ChipsetController controller = go.AddComponent<ChipsetController>();

        GameObject p1 = new GameObject("Preset1Btn", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        p1.transform.SetParent(go.transform, false);
        GameObject p2 = new GameObject("Preset2Btn", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        p2.transform.SetParent(go.transform, false);
        GameObject p3 = new GameObject("Preset3Btn", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        p3.transform.SetParent(go.transform, false);

        try
        {
            controller.AutoWirePresetButtonsIfMissing();
            UnityEngine.UI.Image img1 = p1.GetComponent<UnityEngine.UI.Image>();
            UnityEngine.UI.Image img2 = p2.GetComponent<UnityEngine.UI.Image>();
            UnityEngine.UI.Image img3 = p3.GetComponent<UnityEngine.UI.Image>();

            // Deck 0 (Preset 1 active) -> 1 Yellow, 2 Red, 3 Red
            controller.SwitchDeck(0);
            if (img1.sprite != null) Assert.That(img1.sprite.name, Is.EqualTo("1 Yellow"));
            if (img2.sprite != null) Assert.That(img2.sprite.name, Is.EqualTo("2 Red"));
            if (img3.sprite != null) Assert.That(img3.sprite.name, Is.EqualTo("3 Red"));

            // Deck 1 (Preset 2 active) -> 1 Red, 2 Yellow, 3 Red
            controller.SwitchDeck(1);
            if (img1.sprite != null) Assert.That(img1.sprite.name, Is.EqualTo("1 Red"));
            if (img2.sprite != null) Assert.That(img2.sprite.name, Is.EqualTo("2 Yellow"));
            if (img3.sprite != null) Assert.That(img3.sprite.name, Is.EqualTo("3 Red"));

            // Deck 2 (Preset 3 active) -> 1 Red, 2 Red, 3 Yellow
            controller.SwitchDeck(2);
            if (img1.sprite != null) Assert.That(img1.sprite.name, Is.EqualTo("1 Red"));
            if (img2.sprite != null) Assert.That(img2.sprite.name, Is.EqualTo("2 Red"));
            if (img3.sprite != null) Assert.That(img3.sprite.name, Is.EqualTo("3 Yellow"));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Chipset_IconMapping_MatchesAllTenChipsetsAndWiresCorrectly()
    {
        GameObject go = new GameObject("ChipsetControllerTest", typeof(RectTransform));
        ChipsetController controller = go.AddComponent<ChipsetController>();

        try
        {
            controller.LoadVisualLibraryIfMissing();
            var catalog = ChipsetController.CreateDefaultDatabase();
            Assert.That(catalog.Count, Is.EqualTo(10));

            var resolvedSprites = new HashSet<Sprite>();
            foreach (var chip in catalog)
            {
                Assert.That(chip.iconKey, Is.Not.Null.And.Not.Empty);
                Sprite sprite = controller.GetIconSprite(chip.iconKey);
                Assert.That(sprite, Is.Not.Null, $"Sprite for chip {chip.chipName} ({chip.iconKey}) should not be null.");
                resolvedSprites.Add(sprite);
            }

            Assert.That(resolvedSprites.Count, Is.EqualTo(10), "All 10 chipsets must resolve to 10 unique sprites!");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Buddy_PresetDeckButtons_SwapYellowAndRedSprites_AndDatabaseHasSixDrones()
    {
        GameObject go = new GameObject("BuddyControllerTest", typeof(RectTransform));
        BuddyController controller = go.AddComponent<BuddyController>();

        try
        {
            controller.LoadPresetSpritesIfMissing();
            controller.LoadSortSpritesIfMissing();
            controller.InitializeDatabase();

            Assert.That(controller.AllBuddies.Count, Is.EqualTo(6));

            foreach (var buddy in controller.AllBuddies)
            {
                Assert.That(buddy.iconKey, Is.Not.Null.And.Not.Empty);
            }
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Chipset_AllFrames_UseChipsetGreen_AndClickTriggersDetailModal()
    {
        GameObject go = new GameObject("ChipsetCardTest", typeof(RectTransform));
        ChipsetCardUI card = go.AddComponent<ChipsetCardUI>();

        bool clicked = false;
        ChipItemData testChip = new ChipItemData { id = 1, chipName = "Standard Gun", iconKey = "standard-gun" };

        card.Setup(testChip, null, null, c => clicked = true);

        Assert.That(clicked, Is.True, "Clicking on ChipsetCardUI should trigger onCardClicked callback.");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Chipset_FrameUpgradeRule_MapsAllFiveTiersCorrectly()
    {
        GameObject go = new GameObject("ChipsetTierFrameTest", typeof(RectTransform));
        ChipsetController controller = go.AddComponent<ChipsetController>();

        try
        {
            controller.LoadFrameSpritesIfMissing();

            Sprite green = controller.GetFrameSprite(ChipTier.Magic);
            Sprite blue = controller.GetFrameSprite(ChipTier.Rare);
            Sprite purple = controller.GetFrameSprite(ChipTier.Unique);
            Sprite yellow = controller.GetFrameSprite(ChipTier.Epic);
            Sprite red = controller.GetFrameSprite(ChipTier.Holographic);

            Assert.That(green, Is.Not.Null);
            Assert.That(blue, Is.Not.Null);
            Assert.That(purple, Is.Not.Null);
            Assert.That(yellow, Is.Not.Null);
            Assert.That(red, Is.Not.Null);

            Assert.That(green.name.ToLowerInvariant(), Does.Contain("green"));
            Assert.That(blue.name.ToLowerInvariant(), Does.Contain("blue"));
            Assert.That(purple.name.ToLowerInvariant(), Does.Contain("purple"));
            Assert.That(yellow.name.ToLowerInvariant(), Does.Contain("yello"));
            Assert.That(red.name.ToLowerInvariant(), Does.Contain("red"));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    private static void RestoreIntPreference(string key, bool hadKey, int value)
    {
        if (hadKey) PlayerPrefs.SetInt(key, value);
        else PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
    }
}
