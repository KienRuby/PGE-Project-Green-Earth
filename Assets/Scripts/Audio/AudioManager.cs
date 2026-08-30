using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý âm thanh trung tâm cho toàn bộ Game (PGE - Project Green Earth).
/// Hỗ trợ BGM A/B Crossfade, Object Pool AudioSource cho SFX/VFX/UI,
/// điều chỉnh âm lượng đa kênh (Master, BGM, SFX, VFX, UI, Ambient),
/// chống spam âm thanh (Cooldown/Throttling) và đồng bộ với GameSettings.
/// </summary>
[DefaultExecutionOrder(-9500)]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Databases & Configuration")]
    [Tooltip("Thư viện SoundDatabase chứa danh sách cấu hình âm thanh")]
    [SerializeField] private SoundDatabase database;

    [Header("Pool Settings")]
    [Tooltip("Số lượng AudioSource khởi tạo sẵn trong Pool")]
    [SerializeField] private int initialPoolSize = 32;
    [Tooltip("Có cho phép Pool tự mở rộng khi hết AudioSource khả dụng không")]
    [SerializeField] private bool canGrowPool = true;

    [Header("Default Volumes")]
    [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float bgmVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float vfxVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float uiVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float ambientVolume = 1f;

    // BGM Subsystem (2 Channels for smooth A/B Crossfade)
    private AudioSource bgmSourceA;
    private AudioSource bgmSourceB;
    private AudioSource activeBgmSource;
    private Coroutine bgmCrossfadeCoroutine;
    private string currentBgmId;

    // Ambient Subsystem
    private AudioSource ambientSource;
    private Coroutine ambientFadeCoroutine;
    private string currentAmbientId;

    // AudioSource Pool
    private Transform poolContainer;
    private readonly Queue<PooledAudioSource> availablePool = new Queue<PooledAudioSource>();
    private readonly List<PooledAudioSource> activeSources = new List<PooledAudioSource>();

    // Cooldown & Concurrency Tracking
    private readonly Dictionary<string, float> lastPlayTimeMap = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> activeCountMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    // Mute flags
    private bool isMasterMuted = false;
    private bool isBgmMuted = false;
    private bool isSfxMuted = false;
    private bool isVfxMuted = false;
    private bool isUiMuted = false;
    private bool isAmbientMuted = false;

    public SoundDatabase Database => database;
    public string CurrentBgmId => currentBgmId;
    public string CurrentAmbientId => currentAmbientId;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance == null)
        {
            GameObject host = new GameObject("[AudioManager]");
            Instance = host.AddComponent<AudioManager>();
            DontDestroyOnLoad(host);
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSystem();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        GameSettings.Changed += SyncWithGameSettings;
        SyncWithGameSettings();
    }

    private void OnDisable()
    {
        GameSettings.Changed -= SyncWithGameSettings;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void InitializeSystem()
    {
        // 1. Tự động nạp Database nếu chưa gán
        if (database == null)
        {
            database = Resources.Load<SoundDatabase>("SoundDatabase");
        }
        if (database != null)
        {
            database.InitializeLookup();
        }

        // 2. Tạo kênh BGM A/B
        GameObject bgmObjectA = new GameObject("BGM_Channel_A");
        bgmObjectA.transform.SetParent(transform);
        bgmSourceA = bgmObjectA.AddComponent<AudioSource>();
        bgmSourceA.loop = true;
        bgmSourceA.playOnAwake = false;
        bgmSourceA.spatialBlend = 0f;

        GameObject bgmObjectB = new GameObject("BGM_Channel_B");
        bgmObjectB.transform.SetParent(transform);
        bgmSourceB = bgmObjectB.AddComponent<AudioSource>();
        bgmSourceB.loop = true;
        bgmSourceB.playOnAwake = false;
        bgmSourceB.spatialBlend = 0f;

        activeBgmSource = bgmSourceA;

        // 3. Tạo kênh Ambient
        GameObject ambientObject = new GameObject("Ambient_Channel");
        ambientObject.transform.SetParent(transform);
        ambientSource = ambientObject.AddComponent<AudioSource>();
        ambientSource.loop = true;
        ambientSource.playOnAwake = false;
        ambientSource.spatialBlend = 0f;

        // 4. Tạo Container và Pool AudioSource
        GameObject poolObj = new GameObject("Audio_Pool_Container");
        poolObj.transform.SetParent(transform);
        poolContainer = poolObj.transform;

        for (int i = 0; i < initialPoolSize; i++)
        {
            CreatePooledAudioSource();
        }

        SyncWithGameSettings();
    }

    private PooledAudioSource CreatePooledAudioSource()
    {
        GameObject sourceObj = new GameObject($"AudioSource_Pooled_{availablePool.Count + activeSources.Count}");
        sourceObj.transform.SetParent(poolContainer);
        sourceObj.SetActive(false);

        PooledAudioSource pooled = sourceObj.AddComponent<PooledAudioSource>();
        availablePool.Enqueue(pooled);
        return pooled;
    }

    /// <summary>
    /// Đồng bộ trạng thái BGM/SFX với cấu hình từ GameSettings.
    /// </summary>
    public void SyncWithGameSettings()
    {
        isBgmMuted = !GameSettings.BgmEnabled;
        isSfxMuted = !GameSettings.SfxEnabled;
        isVfxMuted = !GameSettings.SfxEnabled; // VFX gộp chung kênh SFX theo thiết lập người dùng

        UpdateAllAudioVolumes();
    }

    // =========================================================================
    // BGM & AMBIENT SUBSYSTEM
    // =========================================================================

    /// <summary>
    /// Phát nhạc nền với ID tương ứng và thực hiện Crossfade mượt mà.
    /// </summary>
    public void PlayBGM(string soundId, float fadeDuration = 1.0f, bool loop = true)
    {
        if (string.IsNullOrEmpty(soundId)) return;
        if (string.Equals(currentBgmId, soundId, StringComparison.OrdinalIgnoreCase) && activeBgmSource != null && activeBgmSource.isPlaying)
        {
            return; // Đang phát đúng bài này
        }

        AudioClip clip = null;
        float baseVol = 1f;

        if (database != null && database.TryGetSound(soundId, out SoundData data))
        {
            clip = data.GetRandomClip();
            baseVol = data.BaseVolume;
        }
        else
        {
            Debug.LogWarning($"[AudioManager] ⚠️ Không tìm thấy BGM ID '{soundId}' trong Database!");
            return;
        }

        if (clip == null) return;

        currentBgmId = soundId;
        CrossfadeBGM(clip, baseVol, fadeDuration, loop);
    }

    /// <summary>
    /// Phát một AudioClip nhạc nền tùy ý có Crossfade.
    /// </summary>
    public void PlayBGM(AudioClip clip, float fadeDuration = 1.0f, bool loop = true, float baseVol = 1f)
    {
        if (clip == null) return;
        currentBgmId = clip.name;
        CrossfadeBGM(clip, baseVol, fadeDuration, loop);
    }

    private void CrossfadeBGM(AudioClip newClip, float baseVolumeMultiplier, float duration, bool loop)
    {
        if (bgmCrossfadeCoroutine != null)
        {
            StopCoroutine(bgmCrossfadeCoroutine);
        }

        AudioSource incomingSource = (activeBgmSource == bgmSourceA) ? bgmSourceB : bgmSourceA;
        AudioSource outgoingSource = activeBgmSource;

        activeBgmSource = incomingSource;
        bgmCrossfadeCoroutine = StartCoroutine(CrossfadeBgmCoroutine(outgoingSource, incomingSource, newClip, baseVolumeMultiplier, duration, loop));
    }

    private IEnumerator CrossfadeBgmCoroutine(AudioSource outgoing, AudioSource incoming, AudioClip newClip, float baseVol, float duration, bool loop)
    {
        incoming.clip = newClip;
        incoming.loop = loop;
        incoming.volume = 0f;
        incoming.Play();

        float effectiveTargetVol = GetEffectiveVolume(AudioCategory.BGM) * baseVol;
        float outgoingStartVol = outgoing != null ? outgoing.volume : 0f;
        float timer = 0f;

        if (duration <= 0.05f)
        {
            if (outgoing != null) outgoing.Stop();
            incoming.volume = effectiveTargetVol;
            yield break;
        }

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / duration);

            if (outgoing != null && outgoing.isPlaying)
            {
                outgoing.volume = Mathf.Lerp(outgoingStartVol, 0f, t);
            }

            incoming.volume = Mathf.Lerp(0f, effectiveTargetVol, t);
            yield return null;
        }

        if (outgoing != null)
        {
            outgoing.Stop();
            outgoing.clip = null;
        }

        incoming.volume = effectiveTargetVol;
    }

    /// <summary>
    /// Dừng BGM hiện tại kèm Fade out.
    /// </summary>
    public void StopBGM(float fadeDuration = 1.0f)
    {
        if (bgmCrossfadeCoroutine != null) StopCoroutine(bgmCrossfadeCoroutine);
        currentBgmId = null;

        if (activeBgmSource != null && activeBgmSource.isPlaying)
        {
            StartCoroutine(FadeOutAndStopCoroutine(activeBgmSource, fadeDuration));
        }
    }

    /// <summary>
    /// Tạm dừng BGM.
    /// </summary>
    public void PauseBGM()
    {
        if (activeBgmSource != null && activeBgmSource.isPlaying)
        {
            activeBgmSource.Pause();
        }
    }

    /// <summary>
    /// Tiếp tục BGM đã tạm dừng.
    /// </summary>
    public void ResumeBGM()
    {
        if (activeBgmSource != null && !activeBgmSource.isPlaying)
        {
            activeBgmSource.UnPause();
        }
    }

    /// <summary>
    /// Phát âm thanh môi trường (Ambient).
    /// </summary>
    public void PlayAmbient(string soundId, float fadeDuration = 1.0f)
    {
        if (string.IsNullOrEmpty(soundId)) return;
        if (string.Equals(currentAmbientId, soundId, StringComparison.OrdinalIgnoreCase) && ambientSource.isPlaying) return;

        AudioClip clip = null;
        float baseVol = 1f;

        if (database != null && database.TryGetSound(soundId, out SoundData data))
        {
            clip = data.GetRandomClip();
            baseVol = data.BaseVolume;
        }

        if (clip == null) return;

        currentAmbientId = soundId;
        if (ambientFadeCoroutine != null) StopCoroutine(ambientFadeCoroutine);
        ambientFadeCoroutine = StartCoroutine(FadeInAmbientCoroutine(clip, baseVol, fadeDuration));
    }

    /// <summary>
    /// Dừng âm thanh môi trường (Ambient).
    /// </summary>
    public void StopAmbient(float fadeDuration = 1.0f)
    {
        currentAmbientId = null;
        if (ambientFadeCoroutine != null) StopCoroutine(ambientFadeCoroutine);
        if (ambientSource != null && ambientSource.isPlaying)
        {
            ambientFadeCoroutine = StartCoroutine(FadeOutAndStopCoroutine(ambientSource, fadeDuration));
        }
    }

    private IEnumerator FadeInAmbientCoroutine(AudioClip clip, float baseVol, float duration)
    {
        ambientSource.clip = clip;
        ambientSource.loop = true;
        ambientSource.volume = 0f;
        ambientSource.Play();

        float targetVol = GetEffectiveVolume(AudioCategory.Ambient) * baseVol;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            ambientSource.volume = Mathf.Lerp(0f, targetVol, timer / duration);
            yield return null;
        }

        ambientSource.volume = targetVol;
    }

    private IEnumerator FadeOutAndStopCoroutine(AudioSource source, float duration)
    {
        if (source == null) yield break;

        float startVol = source.volume;
        float timer = 0f;

        while (timer < duration && source != null)
        {
            timer += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(startVol, 0f, timer / duration);
            yield return null;
        }

        if (source != null)
        {
            source.Stop();
            source.clip = null;
        }
    }

    // =========================================================================
    // SFX, VFX, UI PLAYBACK SUBSYSTEM (POOL-BASED)
    // =========================================================================

    /// <summary>
    /// Phát âm thanh SFX chuẩn từ SoundDatabase theo soundId tại vị trí (hoặc 2D nếu không truyền position).
    /// </summary>
    public PooledAudioSource PlaySFX(string soundId, Vector3? position = null, float volumeMultiplier = 1f)
    {
        return PlaySoundInternal(soundId, position ?? Vector3.zero, null, Vector3.zero, false, volumeMultiplier, AudioCategory.SFX);
    }

    /// <summary>
    /// Phát một AudioClip SFX trực tiếp (không qua Database).
    /// </summary>
    public PooledAudioSource PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f, Vector3? position = null)
    {
        if (clip == null) return null;
        PooledAudioSource pooled = GetPooledSource();
        if (pooled == null) return null;

        pooled.gameObject.SetActive(true);
        pooled.PlayCustom(clip, AudioCategory.SFX, volume, pitch, position ?? Vector3.zero, position.HasValue ? 1f : 0f, false);
        return pooled;
    }

    /// <summary>
    /// Phát âm thanh VFX bám theo một Transform di động (ví dụ viên đạn, quái vật, kỹ năng).
    /// </summary>
    public PooledAudioSource PlayVFXSound(string soundId, Transform followTarget, bool loop = false, float volumeMultiplier = 1f, Vector3 offset = default)
    {
        Vector3 pos = followTarget != null ? followTarget.position + offset : Vector3.zero;
        return PlaySoundInternal(soundId, pos, followTarget, offset, loop, volumeMultiplier, AudioCategory.VFX);
    }

    /// <summary>
    /// Phát âm thanh VFX tại một vị trí cố định.
    /// </summary>
    public PooledAudioSource PlayVFXSound(string soundId, Vector3 position, float volumeMultiplier = 1f)
    {
        return PlaySoundInternal(soundId, position, null, Vector3.zero, false, volumeMultiplier, AudioCategory.VFX);
    }

    /// <summary>
    /// Phát âm thanh UI giao diện (luôn là 2D).
    /// </summary>
    public PooledAudioSource PlayUI(string soundId, float volumeMultiplier = 1f)
    {
        return PlaySoundInternal(soundId, Vector3.zero, null, Vector3.zero, false, volumeMultiplier, AudioCategory.UI);
    }

    /// <summary>
    /// Phát âm thanh UI từ AudioClip đơn lẻ.
    /// </summary>
    public PooledAudioSource PlayUI(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return null;
        PooledAudioSource pooled = GetPooledSource();
        if (pooled == null) return null;

        pooled.gameObject.SetActive(true);
        pooled.PlayCustom(clip, AudioCategory.UI, volume, 1f, Vector3.zero, 0f, false);
        return pooled;
    }

    private PooledAudioSource PlaySoundInternal(string soundId, Vector3 position, Transform followTarget, Vector3 offset, bool loop, float volumeMultiplier, AudioCategory forcedCategory)
    {
        if (string.IsNullOrEmpty(soundId) || database == null) return null;

        if (!database.TryGetSound(soundId, out SoundData data))
        {
            return null;
        }

        // Kiểm tra Cooldown chống spam
        float currentTime = Time.unscaledTime;
        if (lastPlayTimeMap.TryGetValue(soundId, out float lastTime))
        {
            if (currentTime - lastTime < data.Cooldown)
            {
                return null; // Bị throttled bởi cooldown
            }
        }
        lastPlayTimeMap[soundId] = currentTime;

        // Kiểm tra giới hạn số lượng phát đồng thời
        if (data.MaxSimultaneous > 0)
        {
            activeCountMap.TryGetValue(soundId, out int currentCount);
            if (currentCount >= data.MaxSimultaneous)
            {
                return null; // Vượt quá số lượng phát đồng thời cho phép
            }
            activeCountMap[soundId] = currentCount + 1;
        }

        PooledAudioSource pooled = GetPooledSource();
        if (pooled == null) return null;

        pooled.gameObject.SetActive(true);
        pooled.Play(data, position, followTarget, offset, loop, volumeMultiplier);
        return pooled;
    }

    // =========================================================================
    // POOL MANAGEMENT
    // =========================================================================

    private PooledAudioSource GetPooledSource()
    {
        PooledAudioSource source = null;

        while (availablePool.Count > 0)
        {
            source = availablePool.Dequeue();
            if (source != null) break;
        }

        if (source == null && canGrowPool)
        {
            source = CreatePooledAudioSource();
            availablePool.Dequeue(); // Lấy ra khỏi queue vừa enqueue trong Create
        }

        if (source != null)
        {
            activeSources.Add(source);
        }

        return source;
    }

    /// <summary>
    /// Hoàn trả một PooledAudioSource về Pool để tái sử dụng.
    /// </summary>
    public void ReturnToPool(PooledAudioSource pooled)
    {
        if (pooled == null) return;

        // Giảm đếm số lượng phát đồng thời nếu có soundId
        if (pooled.CurrentData != null && !string.IsNullOrEmpty(pooled.CurrentData.SoundId))
        {
            string id = pooled.CurrentData.SoundId;
            if (activeCountMap.TryGetValue(id, out int count) && count > 0)
            {
                activeCountMap[id] = count - 1;
            }
        }

        activeSources.Remove(pooled);
        pooled.transform.SetParent(poolContainer);
        pooled.gameObject.SetActive(false);
        availablePool.Enqueue(pooled);
    }

    /// <summary>
    /// Dừng một AudioSource đang phát và thu hồi về Pool.
    /// </summary>
    public void StopSound(PooledAudioSource pooled, float fadeOutDuration = 0.1f)
    {
        if (pooled == null) return;
        if (fadeOutDuration > 0.01f)
        {
            pooled.FadeOutAndStop(fadeOutDuration);
        }
        else
        {
            pooled.StopAndRecycle();
        }
    }

    /// <summary>
    /// Dừng toàn bộ âm thanh SFX đang phát.
    /// </summary>
    public void StopAllSFX()
    {
        StopCategoryInternal(AudioCategory.SFX);
    }

    /// <summary>
    /// Dừng toàn bộ âm thanh VFX đang phát.
    /// </summary>
    public void StopAllVFX()
    {
        StopCategoryInternal(AudioCategory.VFX);
    }

    /// <summary>
    /// Dừng tất cả âm thanh hiệu ứng (SFX + VFX + UI).
    /// </summary>
    public void StopAllSounds()
    {
        for (int i = activeSources.Count - 1; i >= 0; i--)
        {
            if (activeSources[i] != null)
            {
                activeSources[i].StopAndRecycle();
            }
        }
    }

    private void StopCategoryInternal(AudioCategory cat)
    {
        for (int i = activeSources.Count - 1; i >= 0; i--)
        {
            PooledAudioSource src = activeSources[i];
            if (src != null && src.CurrentData != null && src.CurrentData.Category == cat)
            {
                src.StopAndRecycle();
            }
        }
    }

    // =========================================================================
    // VOLUME & MUTE CONTROLS
    // =========================================================================

    public float GetEffectiveVolume(AudioCategory category)
    {
        if (isMasterMuted) return 0f;

        float catVol = 1f;
        bool isCatMuted = false;

        switch (category)
        {
            case AudioCategory.Master:
                catVol = masterVolume;
                isCatMuted = isMasterMuted;
                break;
            case AudioCategory.BGM:
                catVol = bgmVolume;
                isCatMuted = isBgmMuted;
                break;
            case AudioCategory.SFX:
                catVol = sfxVolume;
                isCatMuted = isSfxMuted;
                break;
            case AudioCategory.VFX:
                catVol = vfxVolume;
                isCatMuted = isVfxMuted;
                break;
            case AudioCategory.UI:
                catVol = uiVolume;
                isCatMuted = isUiMuted;
                break;
            case AudioCategory.Ambient:
                catVol = ambientVolume;
                isCatMuted = isAmbientMuted;
                break;
        }

        return isCatMuted ? 0f : Mathf.Clamp01(masterVolume * catVol);
    }

    public void SetVolume(AudioCategory category, float volume01)
    {
        volume01 = Mathf.Clamp01(volume01);
        switch (category)
        {
            case AudioCategory.Master:
                masterVolume = volume01;
                break;
            case AudioCategory.BGM:
                bgmVolume = volume01;
                break;
            case AudioCategory.SFX:
                sfxVolume = volume01;
                break;
            case AudioCategory.VFX:
                vfxVolume = volume01;
                break;
            case AudioCategory.UI:
                uiVolume = volume01;
                break;
            case AudioCategory.Ambient:
                ambientVolume = volume01;
                break;
        }

        UpdateAllAudioVolumes();
    }

    public void SetMute(AudioCategory category, bool isMuted)
    {
        switch (category)
        {
            case AudioCategory.Master:
                isMasterMuted = isMuted;
                break;
            case AudioCategory.BGM:
                isBgmMuted = isMuted;
                break;
            case AudioCategory.SFX:
                isSfxMuted = isMuted;
                break;
            case AudioCategory.VFX:
                isVfxMuted = isMuted;
                break;
            case AudioCategory.UI:
                isUiMuted = isMuted;
                break;
            case AudioCategory.Ambient:
                isAmbientMuted = isMuted;
                break;
        }

        UpdateAllAudioVolumes();
    }

    public bool IsMuted(AudioCategory category)
    {
        switch (category)
        {
            case AudioCategory.Master: return isMasterMuted;
            case AudioCategory.BGM: return isBgmMuted;
            case AudioCategory.SFX: return isSfxMuted;
            case AudioCategory.VFX: return isVfxMuted;
            case AudioCategory.UI: return isUiMuted;
            case AudioCategory.Ambient: return isAmbientMuted;
            default: return false;
        }
    }

    private void UpdateAllAudioVolumes()
    {
        // Cập nhật BGM
        if (activeBgmSource != null)
        {
            activeBgmSource.volume = GetEffectiveVolume(AudioCategory.BGM);
        }

        // Cập nhật Ambient
        if (ambientSource != null)
        {
            ambientSource.volume = GetEffectiveVolume(AudioCategory.Ambient);
        }

        // Cập nhật các active sources trong pool
        for (int i = 0; i < activeSources.Count; i++)
        {
            PooledAudioSource src = activeSources[i];
            if (src != null && src.CurrentData != null)
            {
                src.UpdateVolume(GetEffectiveVolume(src.CurrentData.Category));
            }
        }
    }
}
