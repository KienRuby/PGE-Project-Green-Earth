// Isolated Shop & Transaction security probe stubs
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace TMPro
{
    public class TMP_Text : UnityEngine.MonoBehaviour
    {
        public string text;
    }
}

namespace UnityEngine.Events
{
    public class UnityEvent
    {
        private readonly List<Action> listeners = new();
        public void AddListener(Action call) => listeners.Add(call);
        public void RemoveAllListeners() => listeners.Clear();
        public void Invoke()
        {
            var copy = new List<Action>(listeners);
            foreach (var l in copy) l?.Invoke();
        }
    }
}

namespace UnityEngine.UI
{
    public class Button : UnityEngine.MonoBehaviour
    {
        public bool interactable = true;
        public UnityEngine.Events.UnityEvent onClick = new();
    }
}

namespace UnityEngine
{
    public class SerializeField : Attribute { }
    public class HeaderAttribute : Attribute { public HeaderAttribute(string s) {} }
    public class TooltipAttribute : Attribute { public TooltipAttribute(string s) {} }
    public class TextAreaAttribute : Attribute { public TextAreaAttribute(int a,int b) {} }
    public class DefaultExecutionOrder : Attribute { public DefaultExecutionOrder(int n) {} }
    public class MinAttribute : Attribute { public MinAttribute(float min) {} }
    public class ContextMenuAttribute : Attribute { public ContextMenuAttribute(string itemName) {} }
    public class DisallowMultipleComponentAttribute : Attribute { }

    public enum RuntimeInitializeLoadType { BeforeSceneLoad, AfterSceneLoad, BeforeSplashScreen }
    public class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t = RuntimeInitializeLoadType.AfterSceneLoad) {}
    }

    public static class Time
    {
        public static float unscaledTime = 100.0f;
    }

    public static class Application
    {
        public static bool isPlaying = true;
        public static int targetFrameRate;
    }

    public static class QualitySettings
    {
        public static int vSyncCount;
    }

    public enum ScreenOrientation { Portrait }
    public static class Screen
    {
        public static ScreenOrientation orientation;
    }

    public class GameObject
    {
        public GameObject() {}
        public GameObject(string name) {}
        public bool activeSelf = true;
        public void SetActive(bool active) => activeSelf = active;
        public T AddComponent<T>() where T : new() => new T();
    }

    public class MonoBehaviour
    {
        public GameObject gameObject = new GameObject();
        public void CancelInvoke() {}
        public void CancelInvoke(string methodName) {}
        public void Invoke(string methodName, float time) {}
        public T GetComponent<T>() where T : class => null;
        public T GetComponentInParent<T>() where T : class => null;
        public static T FindObjectOfType<T>() where T : class => null;
        public static void DontDestroyOnLoad(object o) {}
        public static void Destroy(object o) {}
    }

    public static class Debug
    {
        public static void Log(object o) {}
        public static void LogWarning(object o) {}
        public static void LogError(object o) {}
    }

    public static class Mathf
    {
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static int Min(int a, int b) => Math.Min(a, b);
        public static int Clamp(int n, int a, int b) => Math.Clamp(n, a, b);
        public static float Clamp01(float n) => Math.Clamp(n, 0, 1);
        public static int RoundToInt(float n) => (int)Math.Round(n, MidpointRounding.ToEven);
        public static int FloorToInt(float n) => (int)Math.Floor(n);
    }

    public static class PlayerPrefs
    {
        private static readonly Dictionary<string, object> values = new();
        public static int GetInt(string k, int d = 0) => values.TryGetValue(k, out var v) ? (int)v : d;
        public static string GetString(string k, string d = "") => values.TryGetValue(k, out var v) ? (string)v : d;
        public static void SetInt(string k, int v) => values[k] = v;
        public static void SetString(string k, string v) => values[k] = v;
        public static bool HasKey(string k) => values.ContainsKey(k);
        public static void DeleteKey(string k) => values.Remove(k);
        public static void Save() {}
        public static void ResetStore() => values.Clear();
    }

    public static class JsonUtility
    {
        private static readonly JsonSerializerOptions opts = new() { IncludeFields = true };
        public static string ToJson(object o) => JsonSerializer.Serialize(o, o.GetType(), opts);
        public static T FromJson<T>(string s) => JsonSerializer.Deserialize<T>(s, opts);
    }
}

public enum ChipTier { None, Normal, Magic, Rare, Epic, Legendary }
public enum BuddyTier { Holographic, Normal, Rare, Epic }
public class BuddyItemData
{
    public int id;
    public string buddyName;
    public BuddyTier tier;
    public int count;
    public int level;
}

public static class GameSettings
{
    public static string LocalPlayerId = "probe-shop-local";
}

namespace PGE.Auth
{
    public class UserProfile { public string userId = "probe-shop-user", authProvider = "probe"; }
    public class GoogleAuthManager
    {
        public static GoogleAuthManager Instance = new();
        public bool IsLoggedIn = true;
        public UserProfile CurrentUser = new();
    }
    public class AppleAuthManager
    {
        public static AppleAuthManager Instance;
        public bool IsLoggedIn;
        public UserProfile CurrentUser;
    }
}
