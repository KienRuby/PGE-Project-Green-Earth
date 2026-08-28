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

    private static void RestoreIntPreference(string key, bool hadKey, int value)
    {
        if (hadKey) PlayerPrefs.SetInt(key, value);
        else PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
    }
}
