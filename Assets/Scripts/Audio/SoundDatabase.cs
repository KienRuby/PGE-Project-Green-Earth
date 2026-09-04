using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Thư viện dữ liệu âm thanh trung tâm (ScriptableObject).
/// Cho phép cấu hình danh sách âm thanh trên Inspector và tra cứu nhanh O(1) trong Runtime.
/// </summary>
[CreateAssetMenu(fileName = "SoundDatabase", menuName = "PGE/Audio/Sound Database", order = 100)]
public class SoundDatabase : ScriptableObject
{
    [Tooltip("Danh sách toàn bộ các cấu hình âm thanh trong trò chơi")]
    [SerializeField] private List<SoundData> sounds = new List<SoundData>();

    private readonly Dictionary<string, SoundData> soundLookup = new Dictionary<string, SoundData>(StringComparer.OrdinalIgnoreCase);
    private bool isInitialized = false;

    public IReadOnlyList<SoundData> Sounds => sounds;

    private void OnEnable()
    {
        InitializeLookup();
    }

    /// <summary>
    /// Xây dựng bảng tra cứu Hash Table O(1) cho toàn bộ sounds theo soundId.
    /// </summary>
    public void InitializeLookup()
    {
        soundLookup.Clear();
        if (sounds == null) return;

        for (int i = 0; i < sounds.Count; i++)
        {
            SoundData sound = sounds[i];
            if (sound == null || string.IsNullOrEmpty(sound.SoundId)) continue;

            if (!soundLookup.ContainsKey(sound.SoundId))
            {
                soundLookup.Add(sound.SoundId, sound);
            }
            else
            {
                Debug.LogWarning($"[SoundDatabase] ⚠️ Trùng lặp soundId: '{sound.SoundId}' trong SoundDatabase. Bỏ qua mục trùng.");
            }
        }

        isInitialized = true;
    }

    /// <summary>
    /// Tra cứu SoundData theo soundId.
    /// </summary>
    public bool TryGetSound(string soundId, out SoundData soundData)
    {
        if (!isInitialized || soundLookup.Count == 0)
        {
            InitializeLookup();
        }

        if (string.IsNullOrEmpty(soundId))
        {
            soundData = null;
            return false;
        }

        return soundLookup.TryGetValue(soundId, out soundData);
    }

    /// <summary>
    /// Lấy SoundData theo soundId hoặc trả về null nếu không tìm thấy.
    /// </summary>
    public SoundData GetSound(string soundId)
    {
        TryGetSound(soundId, out SoundData data);
        return data;
    }

    /// <summary>
    /// Đăng ký thêm sound động trong runtime nếu cần.
    /// </summary>
    public void RegisterRuntimeSound(SoundData data)
    {
        if (data == null || string.IsNullOrEmpty(data.SoundId)) return;

        if (!isInitialized)
        {
            InitializeLookup();
        }

        if (!soundLookup.ContainsKey(data.SoundId))
        {
            soundLookup.Add(data.SoundId, data);
            sounds.Add(data);
        }
        else
        {
            soundLookup[data.SoundId] = data;
        }
    }
}
