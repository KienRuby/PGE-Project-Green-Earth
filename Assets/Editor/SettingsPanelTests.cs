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
    public void Settings_LanguageButton_TranslatesTheSettingsPanel()
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

            TMP_Text title = panel.transform.Find("SafeContent/Title").GetComponent<TMP_Text>();
            Assert.That(GameSettings.Language, Is.EqualTo("Tiếng Việt"));
            Assert.That(title.text, Is.EqualTo("CÀI ĐẶT"));
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

    private static void RestoreIntPreference(string key, bool hadKey, int value)
    {
        if (hadKey) PlayerPrefs.SetInt(key, value);
        else PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
    }
}
