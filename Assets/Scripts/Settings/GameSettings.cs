using System;
using UnityEngine;

/// <summary>
/// Nguồn dữ liệu cài đặt dùng chung cho MainMenu và Gameplay.
/// Mọi tùy chọn đều có mặc định bật và được lưu bằng PlayerPrefs.
/// </summary>
public static class GameSettings
{
    public const string EnglishLanguage = "English";
    public const string VietnameseLanguage = "Vietnamese";
    public const string ChineseLanguage = "Chinese";
    public const string RussianLanguage = "Russian";

    public static readonly string[] SupportedLanguages =
    {
        EnglishLanguage,
        VietnameseLanguage,
        ChineseLanguage,
        RussianLanguage
    };

    public const string BgmKey = "PGE.Settings.BGM";
    public const string SfxKey = "PGE.Settings.SFX";
    public const string LanguageKey = "PGE.Settings.Language";
    public const string ShowDamageKey = "PGE.Settings.ShowDamage";
    public const string DynamicJoystickKey = "PGE.Settings.DynamicJoystick";
    public const string JoystickModeKey = "PGE.Settings.JoystickMode";
    public const string ScreenShakeKey = "PGE.Settings.ScreenShake";
    public const string LocalPlayerIdKey = "PGE.Settings.LocalPlayerId";
    public const string GoogleAccountKey = "PGE.Settings.GoogleAccount";
    public const string AppleAccountKey = "PGE.Settings.AppleAccount";

    public static event Action Changed;

    public static string GoogleAccount
    {
        get => PlayerPrefs.GetString(GoogleAccountKey, string.Empty);
        set
        {
            PlayerPrefs.SetString(GoogleAccountKey, value ?? string.Empty);
            SaveAndNotify();
        }
    }

    public static string AppleAccount
    {
        get => PlayerPrefs.GetString(AppleAccountKey, string.Empty);
        set
        {
            PlayerPrefs.SetString(AppleAccountKey, value ?? string.Empty);
            SaveAndNotify();
        }
    }

    public static bool IsLoggedInGoogle => !string.IsNullOrEmpty(GoogleAccount);
    public static bool IsLoggedInApple => !string.IsNullOrEmpty(AppleAccount);

    public static bool BgmEnabled
    {
        get => GetBool(BgmKey, true);
        set => SetBool(BgmKey, value);
    }

    public static bool SfxEnabled
    {
        get => GetBool(SfxKey, true);
        set => SetBool(SfxKey, value);
    }

    public static string Language
    {
        get => NormalizeLanguage(PlayerPrefs.GetString(LanguageKey, EnglishLanguage));
        set
        {
            PlayerPrefs.SetString(LanguageKey, NormalizeLanguage(value));
            SaveAndNotify();
        }
    }

    public static bool IsVietnamese => Language == VietnameseLanguage;

    public static string NormalizeLanguage(string value)
    {
        if (string.Equals(value, "Tiếng Việt", StringComparison.OrdinalIgnoreCase))
            return VietnameseLanguage;

        foreach (string supportedLanguage in SupportedLanguages)
        {
            if (string.Equals(value, supportedLanguage, StringComparison.OrdinalIgnoreCase))
                return supportedLanguage;
        }

        return EnglishLanguage;
    }

    public static bool ShowDamage
    {
        get => GetBool(ShowDamageKey, true);
        set => SetBool(ShowDamageKey, value);
    }

    /// <summary>
    /// 0 = Dynamic Pad ON, 1 = Fixed Pad ON, 2 = Dynamic/Fixed Pad OFF
    /// </summary>
    public static int JoystickMode
    {
        get => PlayerPrefs.GetInt(JoystickModeKey, GetBool(DynamicJoystickKey, true) ? 0 : 1);
        set
        {
            int clamped = (value % 3 + 3) % 3;
            PlayerPrefs.SetInt(JoystickModeKey, clamped);
            PlayerPrefs.SetInt(DynamicJoystickKey, clamped == 0 ? 1 : 0);
            SaveAndNotify();
        }
    }

    public static bool DynamicJoystick
    {
        get => JoystickMode == 0;
        set => JoystickMode = value ? 0 : 1;
    }

    public static bool JoystickEnabled => JoystickMode != 2;

    public static bool ScreenShake
    {
        get => GetBool(ScreenShakeKey, true);
        set => SetBool(ScreenShakeKey, value);
    }

    public static string LocalPlayerId
    {
        get
        {
            string id = PlayerPrefs.GetString(LocalPlayerIdKey, string.Empty);
            if (!string.IsNullOrEmpty(id)) return id;

            id = Guid.NewGuid().ToString("N").Substring(0, 20).ToUpperInvariant();
            PlayerPrefs.SetString(LocalPlayerIdKey, id);
            PlayerPrefs.Save();
            return id;
        }
    }

    private static bool GetBool(string key, bool defaultValue)
    {
        return PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) != 0;
    }

    private static void SetBool(string key, bool value)
    {
        PlayerPrefs.SetInt(key, value ? 1 : 0);
        SaveAndNotify();
    }

    private static void SaveAndNotify()
    {
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
