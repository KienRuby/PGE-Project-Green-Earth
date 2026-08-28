using System;
using UnityEngine;

/// <summary>
/// Nguồn dữ liệu cài đặt dùng chung cho MainMenu và Gameplay.
/// Mọi tùy chọn đều có mặc định bật và được lưu bằng PlayerPrefs.
/// </summary>
public static class GameSettings
{
    public const string BgmKey = "PGE.Settings.BGM";
    public const string SfxKey = "PGE.Settings.SFX";
    public const string LanguageKey = "PGE.Settings.Language";
    public const string ShowDamageKey = "PGE.Settings.ShowDamage";
    public const string DynamicJoystickKey = "PGE.Settings.DynamicJoystick";
    public const string ScreenShakeKey = "PGE.Settings.ScreenShake";
    public const string LocalPlayerIdKey = "PGE.Settings.LocalPlayerId";

    public static event Action Changed;

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
        get => PlayerPrefs.GetString(LanguageKey, "English");
        set
        {
            PlayerPrefs.SetString(LanguageKey, string.IsNullOrWhiteSpace(value) ? "English" : value);
            SaveAndNotify();
        }
    }

    public static bool IsVietnamese => Language == "Tiếng Việt";

    public static bool ShowDamage
    {
        get => GetBool(ShowDamageKey, true);
        set => SetBool(ShowDamageKey, value);
    }

    public static bool DynamicJoystick
    {
        get => GetBool(DynamicJoystickKey, true);
        set => SetBool(DynamicJoystickKey, value);
    }

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
