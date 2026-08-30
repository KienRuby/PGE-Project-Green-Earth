using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Keeps the game's persisted language choice in sync with Unity Localization.
/// </summary>
public static class PGEGameLocalization
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        GameSettings.Changed -= ApplySavedLanguage;
        GameSettings.Changed += ApplySavedLanguage;

        AsyncOperationHandle<LocalizationSettings> operation = LocalizationSettings.InitializationOperation;
        if (operation.IsDone) ApplySavedLanguage();
        else operation.Completed += _ => ApplySavedLanguage();
    }

    public static void ApplySavedLanguage()
    {
        if (!LocalizationSettings.InitializationOperation.IsDone) return;

        string localeCode = ToLocaleCode(GameSettings.Language);
        Locale locale = LocalizationSettings.AvailableLocales.GetLocale(localeCode);
        if (locale != null && LocalizationSettings.SelectedLocale != locale)
            LocalizationSettings.SelectedLocale = locale;
    }

    private static string ToLocaleCode(string language)
    {
        switch (GameSettings.NormalizeLanguage(language))
        {
            case GameSettings.VietnameseLanguage: return "vi";
            case GameSettings.ChineseLanguage: return "zh-Hans";
            case GameSettings.RussianLanguage: return "ru";
            default: return "en";
        }
    }
}
