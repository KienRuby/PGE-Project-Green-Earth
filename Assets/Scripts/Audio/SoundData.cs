using System;
using UnityEngine;

/// <summary>
/// Cấu hình chi tiết cho một âm thanh (SFX, VFX, BGM, UI, Ambient).
/// Hỗ trợ danh sách clips để ngẫu nhiên hóa (Anti-repetition), dao động cao độ (Pitch Variance),
/// không gian hóa 3D và giới hạn tần suất phát (Cooldown Throttling).
/// </summary>
[Serializable]
public class SoundData
{
    [Header("Identity")]
    [Tooltip("Mã định danh duy nhất của âm thanh (ví dụ: SFX_GunShot_Standard, VFX_Laser_Beam)")]
    [SerializeField] private string soundId = "Sound_New";

    [Tooltip("Phân loại âm thanh")]
    [SerializeField] private AudioCategory category = AudioCategory.SFX;

    [Header("Audio Clips")]
    [Tooltip("Danh sách các AudioClip sẽ được chọn ngẫu nhiên khi phát để tránh nhàm chán")]
    [SerializeField] private AudioClip[] clips = new AudioClip[0];

    [Header("Volume & Pitch")]
    [Range(0f, 1f)]
    [Tooltip("Âm lượng cơ sở của clip (0.0 đến 1.0)")]
    [SerializeField] private float baseVolume = 1f;

    [Tooltip("Dải Pitch ngẫu nhiên (Min X, Max Y). Mặc định [0.95, 1.05] để tạo sự tự nhiên cho SFX lặp lại")]
    [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    [Header("3D Spatial Settings")]
    [Range(0f, 1f)]
    [Tooltip("0 = 2D hoàn toàn (UI, BGM), 1 = 3D hoàn toàn (SFX trong không gian, VFX)")]
    [SerializeField] private float spatialBlend = 0f;

    [Tooltip("Khoảng cách tối thiểu bắt đầu suy giảm âm lượng 3D")]
    [SerializeField] private float minDistance = 1f;

    [Tooltip("Khoảng cách tối đa không còn nghe thấy âm thanh 3D")]
    [SerializeField] private float maxDistance = 30f;

    [Tooltip("Đường cong suy giảm âm thanh 3D")]
    [SerializeField] private AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;

    [Header("Limits & Control")]
    [Tooltip("Khoảng thời gian tối thiểu giữa 2 lần phát cùng 1 âm thanh (giây), tránh spam khi bắn liên thanh hoặc nổ chùm")]
    [SerializeField] private float cooldown = 0.04f;

    [Tooltip("Số lượng phát đồng thời tối đa của âm thanh này (0 = không giới hạn)")]
    [SerializeField] private int maxSimultaneous = 6;

    // Public Getters
    public string SoundId => soundId;
    public AudioCategory Category => category;
    public AudioClip[] Clips => clips;
    public float BaseVolume => baseVolume;
    public Vector2 PitchRange => pitchRange;
    public float SpatialBlend => spatialBlend;
    public float MinDistance => minDistance;
    public float MaxDistance => maxDistance;
    public AudioRolloffMode RolloffMode => rolloffMode;
    public float Cooldown => cooldown;
    public int MaxSimultaneous => maxSimultaneous;

    public SoundData() { }

    public SoundData(string soundId, AudioCategory category, AudioClip clip, float volume = 1f, float spatialBlend = 0f)
    {
        this.soundId = soundId;
        this.category = category;
        this.clips = clip != null ? new[] { clip } : new AudioClip[0];
        this.baseVolume = volume;
        this.spatialBlend = spatialBlend;
        this.pitchRange = new Vector2(0.95f, 1.05f);
        this.cooldown = 0.04f;
        this.maxSimultaneous = 6;
    }

    /// <summary>
    /// Lấy ngẫu nhiên một AudioClip trong danh sách clips.
    /// </summary>
    public AudioClip GetRandomClip()
    {
        if (clips == null || clips.Length == 0) return null;
        if (clips.Length == 1) return clips[0];
        return clips[UnityEngine.Random.Range(0, clips.Length)];
    }

    /// <summary>
    /// Lấy giá trị pitch ngẫu nhiên trong khoảng [pitchRange.x, pitchRange.y].
    /// </summary>
    public float GetRandomPitch()
    {
        if (Mathf.Approximately(pitchRange.x, pitchRange.y))
        {
            return pitchRange.x;
        }
        return UnityEngine.Random.Range(pitchRange.x, pitchRange.y);
    }
}
