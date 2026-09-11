// Isolated logic probes only. These doubles never access Unity or real PlayerPrefs.
using System;
using System.Collections.Generic;
using System.Text.Json;
namespace TMPro { }
namespace UnityEngine.UI { }
namespace UnityEngine {
    public class SerializeField : Attribute { }
    public class HeaderAttribute : Attribute { public HeaderAttribute(string s) {} }
    public class TooltipAttribute : Attribute { public TooltipAttribute(string s) {} }
    public class TextAreaAttribute : Attribute { public TextAreaAttribute(int a,int b) {} }
    public class DefaultExecutionOrder : Attribute { public DefaultExecutionOrder(int n) {} }
    public class CreateAssetMenuAttribute : Attribute { public string fileName,menuName; public int order; }
    public enum RuntimeInitializeLoadType { BeforeSceneLoad }
    public class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t) {} }
    public enum ScreenOrientation { Portrait }
    public static class Application { public static int targetFrameRate; }
    public static class QualitySettings { public static int vSyncCount; }
    public static class Screen { public static ScreenOrientation orientation; }
    public static class Time { public static float deltaTime; }
    public class Sprite { }
    public struct Color { }
    public struct Color32 {
        public Color32(byte r,byte g,byte b,byte a) {}
        public static implicit operator Color(Color32 c) => new Color();
    }
    public class ScriptableObject { }
    public class GameObject { public T AddComponent<T>() where T : new() => new T(); }
    public class MonoBehaviour {
        public GameObject gameObject = new GameObject();
        public T GetComponent<T>() where T : class => null;
        public T GetComponentInParent<T>() where T : class => null;
        public static T FindObjectOfType<T>() where T : class => null;
    }
    public static class Debug {
        public static void Log(object o) {}
        public static void LogError(object o) {}
    }
    public static class Mathf {
        public static int Max(int a,int b) => Math.Max(a,b);
        public static float Max(float a,float b) => Math.Max(a,b);
        public static int Min(int a,int b) => Math.Min(a,b);
        public static int Clamp(int n,int a,int b) => Math.Clamp(n,a,b);
        public static float Clamp01(float n) => Math.Clamp(n,0,1);
        public static int RoundToInt(float n) => (int)Math.Round(n,MidpointRounding.ToEven);
        public static int FloorToInt(float n) => (int)Math.Floor(n);
    }
    public static class PlayerPrefs {
        private static readonly Dictionary<string,object> values = new();
        public static int GetInt(string k,int d=0) => values.TryGetValue(k,out var v) ? (int)v : d;
        public static string GetString(string k,string d="") => values.TryGetValue(k,out var v) ? (string)v : d;
        public static void SetInt(string k,int v) => values[k]=v;
        public static void SetString(string k,string v) => values[k]=v;
        public static bool HasKey(string k) => values.ContainsKey(k);
        public static void Save() {}
        public static void ResetProbeStore() => values.Clear();
    }
    public static class JsonUtility {
        private static readonly JsonSerializerOptions opts = new() { IncludeFields=true };
        public static string ToJson(object o) => JsonSerializer.Serialize(o,o.GetType(),opts);
        public static T FromJson<T>(string s) => JsonSerializer.Deserialize<T>(s,opts);
    }
}
public static class ChipManager {
    public static int DataChips => PlayerDataService.DataChips;
    public static bool HasEnoughDataChips(int n) => PlayerDataService.HasEnoughDataChips(n);
    public static bool HasEnoughRedGems(int n) => PlayerDataService.HasEnoughRedGems(n);
    public static bool TrySpendDataChips(int n) => PlayerDataService.TrySpendDataChips(n);
    public static bool TrySpendRedGems(int n) => PlayerDataService.TrySpendRedGems(n);
    public static bool TrySpendAdvanceStones(int n) => PlayerDataService.TrySpendAdvanceStones(n);
}
public class PlayerHealth : UnityEngine.MonoBehaviour {
    public int BaseMaxHealth=100, MaxHealth=100, CurrentHealth=100, Reduction;
    public bool IsDead;
    public float RangedDefenseBonusPercent;
    public void SetMaxHealth(int n,bool reset=false) { MaxHealth=n; CurrentHealth=reset?n:Math.Min(CurrentHealth,n); }
    public void Heal(int n) { CurrentHealth=Math.Min(MaxHealth,CurrentHealth+n); }
    public void SetDamageReduction(int n) { Reduction=n; }
}
public class PlayerMovement { public float Bonus; public void SetMoveSpeedBonus(float n) { Bonus=n; } }
public class PlayerAutoShooter {
    public float ArtifactDamageMultiplier,ArtifactCritBonus;
    public void ApplyStatBonuses(int a,float b,float c,float d,float e) {}
}
public static class GunTurret { public static float GlobalTurretFireRateMultiplier; }
public static class GameSettings { public static string LocalPlayerId="probe-local"; }
namespace PGE.Auth {
    public class UserProfile { public string userId="probe-account",authProvider="probe"; }
    public class GoogleAuthManager {
        public static GoogleAuthManager Instance=new();
        public bool IsLoggedIn=true;
        public UserProfile CurrentUser=new();
    }
    public class AppleAuthManager {
        public static AppleAuthManager Instance;
        public bool IsLoggedIn;
        public UserProfile CurrentUser;
    }
}
