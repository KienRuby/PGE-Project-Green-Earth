#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class FixAndroidMinSdk
{
    static FixAndroidMinSdk()
    {
        if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel24)
        {
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            AssetDatabase.SaveAssets();
            Debug.Log($"[PlayerSettings] Đã cập nhật Android Minimum API Level lên {PlayerSettings.Android.minSdkVersion} (API 24)");
        }
    }
}
#endif
