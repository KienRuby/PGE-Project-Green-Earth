using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Keeps the game's persisted language choice in sync with Unity Localization.
/// Uses reflection so it compiles cleanly regardless of whether Unity Localization package assemblies are pre-referenced.
/// </summary>
public static class PGEGameLocalization
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        GameSettings.Changed -= ApplySavedLanguage;
        GameSettings.Changed += ApplySavedLanguage;

        EnsureStartupSync();
    }

    private static void EnsureStartupSync()
    {
        try
        {
            Type locSettingsType = Type.GetType("UnityEngine.Localization.Settings.LocalizationSettings, Unity.Localization");
            if (locSettingsType == null) return;

            PropertyInfo initOpProp = locSettingsType.GetProperty("InitializationOperation", BindingFlags.Public | BindingFlags.Static);
            if (initOpProp != null)
            {
                object initOp = initOpProp.GetValue(null);
                if (initOp != null)
                {
                    PropertyInfo isDoneProp = initOp.GetType().GetProperty("IsDone");
                    if (isDoneProp != null && (bool)isDoneProp.GetValue(initOp))
                    {
                        ApplySavedLanguage();
                        return;
                    }

                    GameObject runnerObj = new GameObject("[PGEGameLocalizationHelper]");
                    UnityEngine.Object.DontDestroyOnLoad(runnerObj);
                    LocalizationStartupWatcher watcher = runnerObj.AddComponent<LocalizationStartupWatcher>();
                    watcher.StartWatching(initOp);
                    return;
                }
            }

            ApplySavedLanguage();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[PGEGameLocalization] Startup sync error: {ex.Message}");
        }
    }

    private class LocalizationStartupWatcher : MonoBehaviour
    {
        public void StartWatching(object initOp)
        {
            StartCoroutine(WaitRoutine(initOp));
        }

        private System.Collections.IEnumerator WaitRoutine(object initOp)
        {
            if (initOp is System.Collections.IEnumerator enumerator)
            {
                yield return enumerator;
            }
            else
            {
                PropertyInfo isDoneProp = initOp?.GetType().GetProperty("IsDone");
                float timeout = 5f;
                while (timeout > 0f)
                {
                    if (isDoneProp != null && (bool)isDoneProp.GetValue(initOp))
                    {
                        break;
                    }
                    timeout -= Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            ApplySavedLanguage();
            Destroy(gameObject);
        }
    }

    public static void ApplySavedLanguage()
    {
        try
        {
            Type locSettingsType = Type.GetType("UnityEngine.Localization.Settings.LocalizationSettings, Unity.Localization");
            if (locSettingsType == null) return;

            PropertyInfo initOpProp = locSettingsType.GetProperty("InitializationOperation", BindingFlags.Public | BindingFlags.Static);
            if (initOpProp != null)
            {
                object initOp = initOpProp.GetValue(null);
                if (initOp != null)
                {
                    PropertyInfo isDoneProp = initOp.GetType().GetProperty("IsDone");
                    if (isDoneProp != null && !(bool)isDoneProp.GetValue(initOp))
                    {
                        return; // Not initialized yet
                    }
                }
            }

            string localeCode = ToLocaleCode(GameSettings.Language);

            PropertyInfo availLocalesProp = locSettingsType.GetProperty("AvailableLocales", BindingFlags.Public | BindingFlags.Static);
            if (availLocalesProp == null) return;
            object availLocales = availLocalesProp.GetValue(null);
            if (availLocales == null) return;

            MethodInfo getLocaleMethod = availLocales.GetType().GetMethod("GetLocale", new Type[] { typeof(string) });
            if (getLocaleMethod == null) return;
            object targetLocale = getLocaleMethod.Invoke(availLocales, new object[] { localeCode });

            if (targetLocale != null)
            {
                PropertyInfo selectedLocaleProp = locSettingsType.GetProperty("SelectedLocale", BindingFlags.Public | BindingFlags.Static);
                if (selectedLocaleProp != null)
                {
                    object currentLocale = selectedLocaleProp.GetValue(null);
                    if (!object.Equals(currentLocale, targetLocale))
                    {
                        selectedLocaleProp.SetValue(null, targetLocale);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[PGEGameLocalization] Could not apply locale: {ex.Message}");
        }
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
