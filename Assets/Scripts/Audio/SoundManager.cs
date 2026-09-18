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
    [Tooltip("Tự động phát BGM khi vào Scene")]
    public bool autoPlayBgmOnStart = true;

    // --- 1. Background Music (BGM) ---
    [Tooltip("Nhạc nền màn hình Menu chính")]
    public AudioClip bgmMenu;
    [Range(0f, 1f)] [Tooltip("Âm lượng riêng BGM Menu chính (0.0 đến 1.0)")]
    public float bgmMenuVolume = 0.6f;

    [Tooltip("Nhạc nền trong trận chiến")]
    public AudioClip bgmGameplay;
    [Range(0f, 1f)] [Tooltip("Âm lượng riêng BGM Gameplay (0.0 đến 1.0)")]
    public float bgmGameplayVolume = 0.6f;

    [Tooltip("Nhạc nền khi đánh Boss")]
    public AudioClip bgmBoss;
    [Range(0f, 1f)] [Tooltip("Âm lượng riêng BGM Boss (0.0 đến 1.0)")]
    public float bgmBossVolume = 0.6f;

    [Tooltip("Danh sách nhạc nền tùy chọn thêm")]
    public List<NamedSound> customBgmList = new List<NamedSound>();

    // --- 2. Sound Effects (SFX) ---
    [Tooltip("Âm thanh bắn súng cơ bản")]
    public AudioClip sfxGunShot;
    [Range(0f, 1f)] [Tooltip("Âm lượng riêng Bắn súng cơ bản (0.0 đến 1.0) - Cân bằng mặc định 0.32")]
    public float sfxGunShotVolume = 0.32f;

    [Tooltip("Âm thanh súng săn Shotgun")]
    public AudioClip sfxShotgun;
    [Range(0f, 1f)] [Tooltip("Âm lượng riêng Shotgun (0.0 đến 1.0) - Cân bằng mặc định 0.80")]
    public float sfxShotgunVolume = 0.80f;

    [Tooltip("Âm thanh tiếng nổ")]
    public AudioClip sfxExplosion;
    [Range(0f, 1f)] [Tooltip("Âm lượng riêng Tiếng nổ (0.0 đến 1.0)")]
    public float sfxExplosionVolume = 0.6f;

    [Tooltip("Âm thanh nắm đấm / kỹ năng Rocket Punch")]
    public AudioClip sfxPunch;
    [Range(0f, 1f)] [Tooltip("Âm lượng riêng Nắm đấm Rocket Punch (0.0 đến 1.0)")]
    public float sfxPunchVolume = 0.6f;

    [Tooltip("Âm thanh quái nổ Boomer / phi tiêu đĩa cưa")]
    public AudioClip sfxBoomer;
    [Range(0f, 1f)] [Tooltip("Âm lượng riêng Quái nổ Boomer (0.0 đến 1.0)")]
    public float sfxBoomerVolume = 0.6f;

    [Tooltip("Âm thanh người chơi bị dính sát thương")]
    public AudioClip sfxPlayerHurt;
    [Range(0f, 1f)] [Tooltip("Âm lượng riêng Người chơi bị đau (0.0 đến 1.0)")]
    public float sfxPlayerHurtVolume = 0.8f;

    [Tooltip("Âm thanh khi người chơi tử trận")]
    public AudioClip sfxPlayerDeath;
    [Range(0f, 1f)] [Tooltip("Âm lượng riêng Khi người chơi tử trận (0.0 đến 1.0)")]
    public float sfxPlayerDeathVolume = 0.85f;

    [Tooltip("Âm thanh quái vật bị tiêu diệt")]
    public AudioClip sfxEnemyDeath;
    [Range(0f, 1f)] [Tooltip("Âm lượng riêng Quái vật bị tiêu diệt (0.0 đến 1.0)")]
    public float sfxEnemyDeathVolume = 0.85f;

    [Tooltip("Âm thanh khi nhặt vật phẩm / điểm kinh nghiệm")]
    public AudioClip sfxItemPickup;
    [Range(0f, 1f)] [Tooltip("Âm lượng riêng Nhặt vật phẩm/EXP (0.0 đến 1.0)")]
    public float sfxItemPickupVolume = 0.8f;

    [Tooltip("Âm thanh khi nhân vật lên cấp")]
    public AudioClip sfxLevelUp;
    [Range(0f, 1f)] [Tooltip("Âm lượng riêng Nhân vật lên cấp (0.0 đến 1.0)")]
    public float sfxLevelUpVolume = 0.8f;

    [Tooltip("Danh sách âm thanh SFX tùy chọn thêm")]
    public List<NamedSound> customSfxList = new List<NamedSound>();

    // --- 3. Visual Effects Audio (VFX) ---
    [Tooltip("Âm thanh chùm tia Laser")]
    public AudioClip vfxLaserBeam;
    [Range(0f, 1f)] [Tooltip("Âm lượng riêng Laser Beam (0.0 đến 1.0)")]
    public float vfxLaserBeamVolume = 0.7f;

    [Tooltip("Âm thanh bùng lửa / phun lửa")]
    public AudioClip vfxFireBurst;
    [Range(0f, 1f)] [Tooltip("Âm lượng riêng Phun lửa (0.0 đến 1.0)")]
    public float vfxFireBurstVolume = 0.7f;

    [Tooltip("Âm thanh băng vỡ / đóng băng")]
    public AudioClip vfxIceShatter;
    [Range(0f, 1f)] [Tooltip("Âm lượng riêng Băng vỡ (0.0 đến 1.0)")]
    public float vfxIceShatterVolume = 0.7f;

    [Tooltip("Âm thanh sấm sét / giật điện")]
    public AudioClip vfxLightningStrike;
    [Range(0f, 1f)] [Tooltip("Âm lượng riêng Sấm sét (0.0 đến 1.0)")]
    public float vfxLightningStrikeVolume = 0.7f;

    [Tooltip("Âm thanh kích hoạt lá chắn / khiên năng lượng")]
    public AudioClip vfxShieldActivate;
    [Range(0f, 1f)] [Tooltip("Âm lượng riêng Lá chắn năng lượng (0.0 đến 1.0)")]
    public float vfxShieldActivateVolume = 0.7f;

    [Tooltip("Danh sách âm thanh kỹ xảo VFX tùy chọn thêm")]
    public List<NamedSound> customVfxList = new List<NamedSound>();

    // --- 4. User Interface (UI) ---
    [Tooltip("Âm thanh khi bấm vào nút bấm UI")]
    public AudioClip uiButtonClick;
    [Range(0f, 1f)] [Tooltip("Âm lượng riêng Bấm nút UI (0.0 đến 1.0)")]
    public float uiButtonClickVolume = 0.8f;

    [Tooltip("Âm thanh khi mở bảng Popup / Cửa sổ")]
    public AudioClip uiPopupOpen;
    [Range(0f, 1f)] [Tooltip("Âm lượng riêng Mở Popup (0.0 đến 1.0)")]
    public float uiPopupOpenVolume = 0.8f;

    [Tooltip("Âm thanh khi đóng bảng Popup")]
    public AudioClip uiPopupClose;
    [Range(0f, 1f)] [Tooltip("Âm lượng riêng Đóng Popup (0.0 đến 1.0)")]
    public float uiPopupCloseVolume = 0.8f;

    [Tooltip("Âm thanh khi nhận thưởng / mở quà")]
    public AudioClip uiRewardClaim;
    [Range(0f, 1f)] [Tooltip("Âm lượng riêng Nhận thưởng / Quà (0.0 đến 1.0)")]
    public float uiRewardClaimVolume = 0.85f;

    [Tooltip("Âm thanh thông báo lỗi / từ chối thao tác")]
    public AudioClip uiError;
    [Range(0f, 1f)] [Tooltip("Âm lượng riêng Báo lỗi (0.0 đến 1.0)")]
    public float uiErrorVolume = 0.8f;

    [Tooltip("Danh sách âm thanh giao diện UI tùy chọn thêm")]
    public List<NamedSound> customUiList = new List<NamedSound>();

    // --- 5. Database Synchronization ---
    [Tooltip("Thư viện SoundDatabase để tự động đồng bộ âm lượng riêng sang AudioManager")]
    [SerializeField] private SoundDatabase database;

    // --- 6. Volume Settings ---
    [Range(0f, 1f)]
    [Tooltip("Âm lượng Tổng Nhạc nền BGM (0.0 đến 1.0)")]
    public float bgmVolume = 1f;

    [Range(0f, 1f)]
    [Tooltip("Âm lượng Tổng Hiệu ứng Gameplay SFX (0.0 đến 1.0)")]
    public float sfxVolume = 1f;

    [Range(0f, 1f)]
    [Tooltip("Âm lượng Tổng Hiệu ứng Kỹ xảo VFX (0.0 đến 1.0)")]
    public float vfxVolume = 1f;

    [Range(0f, 1f)]
    [Tooltip("Âm lượng Tổng Giao diện UI (0.0 đến 1.0)")]
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
            SyncToDatabase();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (autoPlayBgmOnStart)
        {
            PlayDefaultSceneBgm();
        }
    }

    public void PlayDefaultSceneBgm()
    {
        // AudioManager owns automatic scene music when available.
        if (AudioManager.Instance != null) return;
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName.IndexOf("game", StringComparison.OrdinalIgnoreCase) >= 0 && bgmGameplay != null)
        {
            PlayBGM(bgmGameplay, bgmGameplayVolume);
        }
        else if (bgmMenu != null)
        {
            PlayBGM(bgmMenu, bgmMenuVolume);
        }
        else if (bgmGameplay != null)
        {
            PlayBGM(bgmGameplay, bgmGameplayVolume);
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
    /// Phát nhạc nền bằng AudioClip kèm hệ số âm lượng riêng.
    /// </summary>
    public void PlayBGM(AudioClip clip, float volumeFactor = 1f)
    {
        if (clip == null || bgmSource == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.volume = isBgmEnabled ? Mathf.Clamp01(bgmVolume * volumeFactor) : 0f;
        bgmSource.Play();
    }

    /// <summary>
    /// Phát nhạc nền theo tên đã đặt trong danh sách BGM.
    /// </summary>
    public void PlayBGM(string soundName)
    {
        if (soundLookup.TryGetValue(soundName, out NamedSound item))
        {
            PlayBGM(item.clip, item.volume);
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
    /// </summary>
    public void PlaySFX(string soundName)
    {
        if (soundLookup.TryGetValue(soundName, out NamedSound item))
        {
            PlaySFX(item.clip, item.volume, item.pitch);
        }
    }

    /// <summary>
    /// Phát âm thanh hiệu ứng kỹ xảo VFX.
    /// </summary>
    public void PlayVFX(AudioClip clip, float volumeFactor = 1f)
    {
        if (!isSfxEnabled || clip == null) return;
        PlaySFX(clip, volumeFactor * vfxVolume, 1f);
    }

    /// <summary>
    /// Phát âm thanh 3D tại một tọa độ trong không gian.
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
        src.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
        src.volume = Mathf.Clamp01(sfxVolume * volumeFactor);
        src.clip = clip;
        src.Play();
    }

    // ---------- [ C. UI (User Interface) ] ----------
    /// <summary>
    /// Phát âm thanh giao diện UI bằng AudioClip kèm hệ số âm lượng riêng.
    /// </summary>
    public void PlayUI(AudioClip clip, float volumeFactor = 1f)
    {
        if (!isSfxEnabled || clip == null || uiSource == null) return;

        uiSource.pitch = 1f;
        uiSource.volume = Mathf.Clamp01(uiVolume * volumeFactor);
        uiSource.PlayOneShot(clip);
    }

    public void PlayButtonClick()
    {
        if (uiButtonClick != null) PlayUI(uiButtonClick, uiButtonClickVolume);
    }

    public void PlayPopupOpen()
    {
        if (uiPopupOpen != null) PlayUI(uiPopupOpen, uiPopupOpenVolume);
    }

    public void PlayPopupClose()
    {
        if (uiPopupClose != null) PlayUI(uiPopupClose, uiPopupCloseVolume);
    }

    public void PlayRewardClaim()
    {
        if (uiRewardClaim != null) PlayUI(uiRewardClaim, uiRewardClaimVolume);
    }

    public void PlayError()
    {
        if (uiError != null) PlayUI(uiError, uiErrorVolume);
    }

    public void PlayGunShot()
    {
        if (sfxGunShot != null) PlaySFX(sfxGunShot, sfxGunShotVolume);
    }

    public void PlayShotgun()
    {
        if (sfxShotgun != null) PlaySFX(sfxShotgun, sfxShotgunVolume);
    }

    public void PlayExplosion(Vector3? position = null)
    {
        if (sfxExplosion == null) return;
        if (position.HasValue) PlaySFX3D(sfxExplosion, position.Value, sfxExplosionVolume);
        else PlaySFX(sfxExplosion, sfxExplosionVolume);
    }

    public void PlayPunch()
    {
        if (sfxPunch != null) PlaySFX(sfxPunch, sfxPunchVolume);
    }

    public void PlayBoomer()
    {
        if (sfxBoomer != null) PlaySFX(sfxBoomer, sfxBoomerVolume);
    }

    public void PlayEnemyDeath()
    {
        if (sfxEnemyDeath != null) PlaySFX(sfxEnemyDeath, sfxEnemyDeathVolume);
    }

    public void PlayItemPickup()
    {
        if (sfxItemPickup != null) PlaySFX(sfxItemPickup, sfxItemPickupVolume);
    }

    public void PlayLevelUp()
    {
        if (sfxLevelUp != null) PlaySFX(sfxLevelUp, sfxLevelUpVolume);
    }

    public void PlayPlayerDeath()
    {
        if (sfxPlayerDeath != null) PlaySFX(sfxPlayerDeath, sfxPlayerDeathVolume);
        else if (sfxPlayerHurt != null) PlaySFX(sfxPlayerHurt, sfxPlayerHurtVolume);
    }

    public void PlayPlayerHurt()
    {
        if (sfxPlayerHurt != null) PlaySFX(sfxPlayerHurt, sfxPlayerHurtVolume);
    }

    public void PlayLaserBeam(float volumeMultiplier = 1f)
    {
        if (vfxLaserBeam != null) PlayVFX(vfxLaserBeam, vfxLaserBeamVolume * volumeMultiplier);
    }

    public void PlayFireBurst(float volumeMultiplier = 1f)
    {
        if (vfxFireBurst != null) PlayVFX(vfxFireBurst, vfxFireBurstVolume * volumeMultiplier);
    }

    public void PlayIceShatter(float volumeMultiplier = 1f)
    {
        if (vfxIceShatter != null) PlayVFX(vfxIceShatter, vfxIceShatterVolume * volumeMultiplier);
    }

    public void PlayLightningStrike(float volumeMultiplier = 1f)
    {
        if (vfxLightningStrike != null) PlayVFX(vfxLightningStrike, vfxLightningStrikeVolume * volumeMultiplier);
    }

    public void PlayShieldActivate(float volumeMultiplier = 1f)
    {
        if (vfxShieldActivate != null) PlayVFX(vfxShieldActivate, vfxShieldActivateVolume * volumeMultiplier);
    }

    // =========================================================================
    // 5. DATABASE SYNC & PRESETS
    // =========================================================================

    /// <summary>
    /// Đồng bộ tất cả thanh âm lượng riêng sang SoundDatabase (dùng cho AudioManager).
    /// </summary>
    public void SyncToDatabase()
    {
        if (database == null)
        {
            database = Resources.Load<SoundDatabase>("SoundDatabase");
        }
        if (database == null) return;

        database.SetSoundVolume(SoundIdConst.BGM_MAIN_MENU, bgmMenuVolume);
        database.SetSoundVolume(SoundIdConst.BGM_COMBAT, bgmGameplayVolume);
        database.SetSoundVolume(SoundIdConst.BGM_BOSS, bgmBossVolume);

        database.SetSoundVolume(SoundIdConst.SFX_GUN_SHOT_STANDARD, sfxGunShotVolume);
        database.SetSoundVolume(SoundIdConst.SFX_GUN_SHOT_SHOTGUN, sfxShotgunVolume);
        database.SetSoundVolume(SoundIdConst.SFX_EXPLOSION_SMALL, sfxExplosionVolume);
        database.SetSoundVolume(SoundIdConst.SFX_EXPLOSION_LARGE, sfxExplosionVolume);
        database.SetSoundVolume(SoundIdConst.SFX_PUNCH_SPAWN, sfxPunchVolume);
        database.SetSoundVolume(SoundIdConst.SFX_PUNCH_HIT, sfxExplosionVolume);
        database.SetSoundVolume(SoundIdConst.SFX_BOOMER_HIT, sfxBoomerVolume);
        database.SetSoundVolume(SoundIdConst.SFX_PLAYER_HURT, sfxPlayerHurtVolume);
        database.SetSoundVolume(SoundIdConst.SFX_PLAYER_DEATH, sfxPlayerDeathVolume);
        database.SetSoundVolume(SoundIdConst.SFX_ENEMY_DEATH, sfxEnemyDeathVolume);
        database.SetSoundVolume(SoundIdConst.SFX_PICKUP_ITEM, sfxItemPickupVolume);
        database.SetSoundVolume(SoundIdConst.SFX_PICKUP_EXP, sfxItemPickupVolume);
        database.SetSoundVolume(SoundIdConst.SFX_LEVEL_UP, sfxLevelUpVolume);

        database.SetSoundVolume(SoundIdConst.VFX_LASER_BEAM, vfxLaserBeamVolume);
        database.SetSoundVolume(SoundIdConst.VFX_FIRE_BURST, vfxFireBurstVolume);
        database.SetSoundVolume(SoundIdConst.VFX_ICE_SHATTER, vfxIceShatterVolume);
        database.SetSoundVolume(SoundIdConst.VFX_LIGHTNING_STRIKE, vfxLightningStrikeVolume);
        database.SetSoundVolume(SoundIdConst.VFX_SHIELD_ACTIVATE, vfxShieldActivateVolume);

        database.SetSoundVolume(SoundIdConst.UI_BUTTON_CLICK, uiButtonClickVolume);
        database.SetSoundVolume(SoundIdConst.UI_TAB_SWITCH, uiButtonClickVolume);
        database.SetSoundVolume(SoundIdConst.UI_MODAL_OPEN, uiPopupOpenVolume);
        database.SetSoundVolume(SoundIdConst.UI_MODAL_CLOSE, uiPopupCloseVolume);
        database.SetSoundVolume(SoundIdConst.UI_REWARD_CLAIM, uiRewardClaimVolume);
        database.SetSoundVolume(SoundIdConst.UI_ERROR_BUZZ, uiErrorVolume);

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(database);
#endif
    }

    /// <summary>
    /// Đặt lại các giá trị âm lượng đã được cân bằng chuẩn phòng thu (Studio Balanced Defaults).
    /// </summary>
    public void ApplyBalancedDefaults()
    {
        bgmMenuVolume = 0.6f;
        bgmGameplayVolume = 0.6f;
        bgmBossVolume = 0.6f;

        sfxGunShotVolume = 0.32f;
        sfxShotgunVolume = 0.80f;
        sfxExplosionVolume = 0.6f;
        sfxPunchVolume = 0.6f;
        sfxBoomerVolume = 0.6f;
        sfxPlayerHurtVolume = 0.8f;
        sfxPlayerDeathVolume = 0.85f;
        sfxEnemyDeathVolume = 0.85f;
        sfxItemPickupVolume = 0.8f;
        sfxLevelUpVolume = 0.8f;

        vfxLaserBeamVolume = 0.7f;
        vfxFireBurstVolume = 0.7f;
        vfxIceShatterVolume = 0.7f;
        vfxLightningStrikeVolume = 0.7f;
        vfxShieldActivateVolume = 0.7f;

        uiButtonClickVolume = 0.8f;
        uiPopupOpenVolume = 0.8f;
        uiPopupCloseVolume = 0.8f;
        uiRewardClaimVolume = 0.85f;
        uiErrorVolume = 0.8f;

        SyncToDatabase();
    }

    private void OnValidate()
    {
        SyncToDatabase();
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
