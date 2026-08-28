using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Nhóm âm thanh dùng bởi hai nút BGM và SFX trong Settings.
/// Nếu AudioSource không có component này, source loop được xem là BGM và source one-shot là SFX.
/// </summary>
public sealed class GameAudioCategory : MonoBehaviour
{
    public enum Category
    {
        Music,
        SoundEffect
    }

    [SerializeField] private Category category = Category.SoundEffect;
    public Category AudioCategory => category;
}

/// <summary>
/// Tự áp dụng cài đặt BGM/SFX cho mọi AudioSource hiện có và được tạo trong lúc chơi.
/// Không cần thêm object thủ công vào scene.
/// </summary>
[DefaultExecutionOrder(-10000)]
public sealed class GameAudioSettingsRuntime : MonoBehaviour
{
    private const float RefreshInterval = 0.25f;
    private static GameAudioSettingsRuntime instance;

    private readonly Dictionary<AudioSource, bool> originalMuteStates = new Dictionary<AudioSource, bool>();
    private float nextRefreshTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;

        GameObject host = new GameObject(nameof(GameAudioSettingsRuntime));
        instance = host.AddComponent<GameAudioSettingsRuntime>();
        DontDestroyOnLoad(host);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void OnEnable()
    {
        GameSettings.Changed += ApplySettingsNow;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        ApplySettingsNow();
    }

    private void OnDisable()
    {
        GameSettings.Changed -= ApplySettingsNow;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefreshTime) return;
        nextRefreshTime = Time.unscaledTime + RefreshInterval;
        ApplySettingsNow();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplySettingsNow();
    }

    public void ApplySettingsNow()
    {
        AudioSource[] sources = FindObjectsOfType<AudioSource>(true);
        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource source = sources[i];
            if (source == null) continue;

            if (!originalMuteStates.ContainsKey(source))
            {
                originalMuteStates.Add(source, source.mute);
            }

            bool categoryEnabled = IsMusicSource(source)
                ? GameSettings.BgmEnabled
                : GameSettings.SfxEnabled;
            source.mute = originalMuteStates[source] || !categoryEnabled;
        }

        RemoveDestroyedSources();
    }

    public static bool IsMusicSource(AudioSource source)
    {
        if (source == null) return false;

        GameAudioCategory marker = source.GetComponent<GameAudioCategory>();
        if (marker != null)
        {
            return marker.AudioCategory == GameAudioCategory.Category.Music;
        }

        string objectName = source.gameObject.name.ToUpperInvariant();
        return source.loop || objectName.Contains("BGM") || objectName.Contains("MUSIC");
    }

    private void RemoveDestroyedSources()
    {
        List<AudioSource> destroyed = null;
        foreach (KeyValuePair<AudioSource, bool> pair in originalMuteStates)
        {
            if (pair.Key != null) continue;
            if (destroyed == null) destroyed = new List<AudioSource>();
            destroyed.Add(pair.Key);
        }

        if (destroyed == null) return;
        for (int i = 0; i < destroyed.Count; i++)
        {
            originalMuteStates.Remove(destroyed[i]);
        }
    }
}
