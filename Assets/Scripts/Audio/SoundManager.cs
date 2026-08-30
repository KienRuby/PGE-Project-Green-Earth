using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SCRIPT DUY NHẤT QUẢN LÝ TOÀN BỘ ÂM THANH TRONG GAME.
/// Gắn script này vào 1 GameObject duy nhất trong Scene (ví dụ đặt tên là [SoundManager]).
/// Kéo thả trực tiếp các file âm thanh (AudioClip) vào Inspector.
/// </summary>
public class SoundManager : MonoBehaviour
{
    // =========================================================================
    // 1. SINGLETON (Dùng SoundManager.Instance ở bất kỳ đâu trong code)
    // =========================================================================
    public static SoundManager Instance { get; private set; }

    [System.Serializable]
    public class NamedSound
    {
        [Tooltip("Tên định danh âm thanh để gọi trong code (ví dụ: 'gun_shot', 'laser_1')")]
        public string soundName;

        [Tooltip("File âm thanh tương ứng")]
        public AudioClip clip;

        [Range(0f, 1f)]
        [Tooltip("Âm lượng riêng cho âm thanh này (0.0 đến 1.0)")]
        public float volume = 1f;

        [Range(0.5f, 1.5f)]
        [Tooltip("Cao độ âm thanh (Pitch). 1 là bình thường")]
        public float pitch = 1f;
    }

    // =========================================================================
    // 2. INSPECTOR FIELDS (Tên tiếng Anh + Tooltip tiếng Việt)
    // =========================================================================
    [Header("=== 1. Background Music (BGM) ===")]
    [Tooltip("Nhạc nền màn hình Menu chính")]
    public AudioClip bgmMenu;

    [Tooltip("Nhạc nền trong trận chiến")]
    public AudioClip bgmGameplay;

    [Tooltip("Nhạc nền khi đánh Boss")]
    public AudioClip bgmBoss;

    [Tooltip("Danh sách nhạc nền tùy chọn thêm")]
    public List<NamedSound> customBgmList = new List<NamedSound>();

    [Header("=== 2. Sound Effects (SFX) ===")]
    [Tooltip("Âm thanh bắn súng cơ bản")]
    public AudioClip sfxGunShot;

    [Tooltip("Âm thanh tiếng nổ")]
    public AudioClip sfxExplosion;

    [Tooltip("Âm thanh người chơi bị dính sát thương")]
    public AudioClip sfxPlayerHurt;

    [Tooltip("Âm thanh quái vật bị tiêu diệt")]
    public AudioClip sfxEnemyDeath;

    [Tooltip("Âm thanh khi nhặt vật phẩm / điểm kinh nghiệm")]
    public AudioClip sfxItemPickup;

    [Tooltip("Âm thanh khi nhân vật lên cấp")]
    public AudioClip sfxLevelUp;

    [Tooltip("Danh sách âm thanh SFX tùy chọn thêm")]
    public List<NamedSound> customSfxList = new List<NamedSound>();

    [Header("=== 3. Visual Effects Audio (VFX) ===")]
    [Tooltip("Âm thanh chùm tia Laser")]
    public AudioClip vfxLaserBeam;

    [Tooltip("Âm thanh bùng lửa / phun lửa")]
    public AudioClip vfxFireBurst;

    [Tooltip("Âm thanh băng vỡ / đóng băng")]
    public AudioClip vfxIceShatter;

    [Tooltip("Âm thanh sấm sét / giật điện")]
    public AudioClip vfxLightningStrike;

    [Tooltip("Âm thanh kích hoạt lá chắn / khiên năng lượng")]
    public AudioClip vfxShieldActivate;

    [Tooltip("Danh sách âm thanh kỹ xảo VFX tùy chọn thêm")]
    public List<NamedSound> customVfxList = new List<NamedSound>();

    [Header("=== 4. User Interface (UI) ===")]
    [Tooltip("Âm thanh khi bấm vào nút bấm UI")]
    public AudioClip uiButtonClick;

    [Tooltip("Âm thanh khi mở bảng Popup / Cửa sổ")]
    public AudioClip uiPopupOpen;

    [Tooltip("Âm thanh khi đóng bảng Popup")]
    public AudioClip uiPopupClose;

    [Tooltip("Âm thanh khi nhận thưởng / mở quà")]
    public AudioClip uiRewardClaim;

    [Tooltip("Âm thanh thông báo lỗi / từ chối thao tác")]
    public AudioClip uiError;

    [Tooltip("Danh sách âm thanh giao diện UI tùy chọn thêm")]
    public List<NamedSound> customUiList = new List<NamedSound>();

    [Header("=== 5. Volume Settings ===")]
    [Range(0f, 1f)]
    [Tooltip("Âm lượng Nhạc nền BGM (0.0 đến 1.0)")]
    public float bgmVolume = 1f;

    [Range(0f, 1f)]
    [Tooltip("Âm lượng Hiệu ứng Gameplay SFX (0.0 đến 1.0)")]
    public float sfxVolume = 1f;

    [Range(0f, 1f)]
    [Tooltip("Âm lượng Hiệu ứng Kỹ xảo VFX (0.0 đến 1.0)")]
    public float vfxVolume = 1f;

    [Range(0f, 1f)]
    [Tooltip("Âm lượng Giao diện UI (0.0 đến 1.0)")]
    public float uiVolume = 1f;

    [Tooltip("Trạng thái bật/tắt nhạc nền BGM")]
    public bool isBgmEnabled = true;

    [Tooltip("Trạng thái bật/tắt âm thanh SFX & VFX")]
    public bool isSfxEnabled = true;

    // =========================================================================
    // 3. INTERNAL CHANNELS & POOL
    // =========================================================================
    private AudioSource bgmSource;
    private AudioSource uiSource;
    private AudioSource[] sfxSources;
    private const int SFX_POOL_SIZE = 16;
    private int currentSfxIndex = 0;

    private readonly Dictionary<string, NamedSound> soundLookup = new Dictionary<string, NamedSound>(StringComparer.OrdinalIgnoreCase);

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeChannels();
            BuildSoundLookup();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        GameSettings.Changed += SyncSettings;
        SyncSettings();
    }

    private void OnDisable()
    {
        GameSettings.Changed -= SyncSettings;
    }

    private void InitializeChannels()
    {
        // 1. Kênh phát BGM (Loop)
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.spatialBlend = 0f; // 2D

        // 2. Kênh phát UI
        uiSource = gameObject.AddComponent<AudioSource>();
        uiSource.loop = false;
        uiSource.playOnAwake = false;
        uiSource.spatialBlend = 0f; // 2D

        // 3. 16 Kênh phát SFX và VFX đồng thời
        sfxSources = new AudioSource[SFX_POOL_SIZE];
        for (int i = 0; i < SFX_POOL_SIZE; i++)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.loop = false;
            src.playOnAwake = false;
            sfxSources[i] = src;
        }
    }

    private void BuildSoundLookup()
    {
        soundLookup.Clear();

        void AddList(List<NamedSound> list)
        {
            if (list == null) return;
            foreach (var item in list)
            {
                if (item != null && !string.IsNullOrEmpty(item.soundName) && item.clip != null)
                {
                    soundLookup[item.soundName] = item;
                }
            }
        }

        AddList(customBgmList);
        AddList(customSfxList);
        AddList(customVfxList);
        AddList(customUiList);
    }

    private void SyncSettings()
    {
        isBgmEnabled = GameSettings.BgmEnabled;
        isSfxEnabled = GameSettings.SfxEnabled;
        UpdateVolumes();
    }

    // =========================================================================
    // 4. PUBLIC PLAY METHODS
    // =========================================================================

    // ---------- [ A. BGM (Background Music) ] ----------
    /// <summary>
    /// Phát nhạc nền bằng AudioClip.
    /// Ví dụ: SoundManager.Instance.PlayBGM(SoundManager.Instance.bgmGameplay);
    /// </summary>
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || bgmSource == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.volume = isBgmEnabled ? bgmVolume : 0f;
        bgmSource.Play();
    }

    /// <summary>
    /// Phát nhạc nền theo tên đã đặt trong danh sách BGM.
    /// Ví dụ: SoundManager.Instance.PlayBGM("menu_theme");
    /// </summary>
    public void PlayBGM(string soundName)
    {
        if (soundLookup.TryGetValue(soundName, out NamedSound item))
        {
            PlayBGM(item.clip);
        }
    }

    /// <summary> Dừng phát nhạc nền </summary>
    public void StopBGM()
    {
        if (bgmSource != null) bgmSource.Stop();
    }

    // ---------- [ B. SFX & VFX (Gameplay Sound) ] ----------
    /// <summary>
    /// Phát âm thanh hiệu ứng SFX 2D bằng AudioClip.
    /// Ví dụ: SoundManager.Instance.PlaySFX(SoundManager.Instance.sfxGunShot);
    /// </summary>
    public void PlaySFX(AudioClip clip, float volumeFactor = 1f, float pitch = 1f)
    {
        if (!isSfxEnabled || clip == null) return;

        AudioSource src = GetAvailableSfxSource();
        if (src == null) return;

        src.transform.position = transform.position;
        src.spatialBlend = 0f; // 2D
        src.pitch = pitch;
        src.volume = Mathf.Clamp01(sfxVolume * volumeFactor);
        src.clip = clip;
        src.Play();
    }

    /// <summary>
    /// Phát âm thanh SFX theo tên đã cấu hình trong danh sách.
    /// Ví dụ: SoundManager.Instance.PlaySFX("gun_shot");
    /// </summary>
    public void PlaySFX(string soundName)
    {
        if (soundLookup.TryGetValue(soundName, out NamedSound item))
        {
            PlaySFX(item.clip, item.volume, item.pitch);
        }
    }

    /// <summary>
    /// Phát âm thanh hiệu ứng kỹ xảo VFX (Laser, lửa, sét...).
    /// Ví dụ: SoundManager.Instance.PlayVFX(SoundManager.Instance.vfxLaserBeam);
    /// </summary>
    public void PlayVFX(AudioClip clip, float volumeFactor = 1f)
    {
        if (!isSfxEnabled || clip == null) return;
        PlaySFX(clip, volumeFactor * vfxVolume, 1f);
    }

    /// <summary>
    /// Phát âm thanh 3D tại một tọa độ trong không gian (ở gần to, ở xa nhỏ).
    /// Ví dụ: SoundManager.Instance.PlaySFX3D(SoundManager.Instance.sfxExplosion, enemyPos);
    /// </summary>
    public void PlaySFX3D(AudioClip clip, Vector3 worldPosition, float volumeFactor = 1f, float minDistance = 1f, float maxDistance = 25f)
    {
        if (!isSfxEnabled || clip == null) return;

        AudioSource src = GetAvailableSfxSource();
        if (src == null) return;

        src.transform.position = worldPosition;
        src.spatialBlend = 1f; // 3D
        src.minDistance = minDistance;
        src.maxDistance = maxDistance;
        src.rolloffMode = AudioRolloffMode.Logarithmic;
        src.pitch = UnityEngine.Random.Range(0.95f, 1.05f); // Dao động nhẹ cao độ chống nhàm chán
        src.volume = Mathf.Clamp01(sfxVolume * volumeFactor);
        src.clip = clip;
        src.Play();
    }

    // ---------- [ C. UI (User Interface) ] ----------
    /// <summary>
    /// Phát âm thanh giao diện UI bằng AudioClip.
    /// Ví dụ: SoundManager.Instance.PlayUI(SoundManager.Instance.uiButtonClick);
    /// </summary>
    public void PlayUI(AudioClip clip)
    {
        if (!isSfxEnabled || clip == null || uiSource == null) return;

        uiSource.pitch = 1f;
        uiSource.volume = uiVolume;
        uiSource.PlayOneShot(clip);
    }

    /// <summary> Phát âm thanh click nút mặc định </summary>
    public void PlayButtonClick()
    {
        if (uiButtonClick != null) PlayUI(uiButtonClick);
    }

    // =========================================================================
    // 5. VOLUME & TOGGLE CONTROLS
    // =========================================================================

    public void SetBGMVolume(float volume01)
    {
        bgmVolume = Mathf.Clamp01(volume01);
        UpdateVolumes();
    }

    public void SetSFXVolume(float volume01)
    {
        sfxVolume = Mathf.Clamp01(volume01);
        UpdateVolumes();
    }

    public void ToggleBGM(bool enable)
    {
        isBgmEnabled = enable;
        GameSettings.BgmEnabled = enable;
        UpdateVolumes();
    }

    public void ToggleSFX(bool enable)
    {
        isSfxEnabled = enable;
        GameSettings.SfxEnabled = enable;
        UpdateVolumes();
    }

    private void UpdateVolumes()
    {
        if (bgmSource != null)
        {
            bgmSource.volume = isBgmEnabled ? bgmVolume : 0f;
        }

        if (uiSource != null)
        {
            uiSource.volume = isSfxEnabled ? uiVolume : 0f;
        }
    }

    private AudioSource GetAvailableSfxSource()
    {
        if (sfxSources == null || sfxSources.Length == 0) return null;

        for (int i = 0; i < sfxSources.Length; i++)
        {
            int index = (currentSfxIndex + i) % sfxSources.Length;
            if (!sfxSources[index].isPlaying)
            {
                currentSfxIndex = (index + 1) % sfxSources.Length;
                return sfxSources[index];
            }
        }

        // Nếu tất cả các kênh đều đang phát, lấy xoay vòng
        currentSfxIndex = (currentSfxIndex + 1) % sfxSources.Length;
        return sfxSources[currentSfxIndex];
    }
}
