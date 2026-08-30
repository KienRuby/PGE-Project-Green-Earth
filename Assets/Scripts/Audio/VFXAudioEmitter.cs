using UnityEngine;

/// <summary>
/// Component gắn trực tiếp vào các Prefab VFX, Hiệu ứng hạt (Particle System),
/// đạn (Projectile), hoặc kỹ năng để tự động phát và điều khiển âm thanh tương ứng.
/// </summary>
[DisallowMultipleComponent]
public class VFXAudioEmitter : MonoBehaviour
{
    [Header("Sound Configuration")]
    [Tooltip("Mã định danh SoundId trong SoundDatabase (ví dụ: VFX_Laser_Beam, VFX_Fire_Burst, SFX_Explosion_Small)")]
    [SerializeField] private string soundId = SoundIdConst.VFX_LASER_BEAM;

    [Tooltip("Âm lượng nhân thêm cho riêng đối tượng VFX này (0.0 đến 1.0)")]
    [Range(0f, 1f)]
    [SerializeField] private float volumeMultiplier = 1f;

    [Header("Playback Behaviour")]
    [Tooltip("Tự động phát âm thanh ngay khi GameObject này được kích hoạt (OnEnable)")]
    [SerializeField] private bool playOnEnable = true;

    [Tooltip("Phát lặp lại liên tục (dành cho luồng lửa, chùm laser kéo dài)")]
    [SerializeField] private bool isLooping = false;

    [Tooltip("Âm thanh có di chuyển bám theo Transform của VFX này không")]
    [SerializeField] private bool followTransform = true;

    [Tooltip("Độ lệch vị trí so với tâm Transform")]
    [SerializeField] private Vector3 positionOffset = Vector3.zero;

    [Header("Lifecycle Cleanup")]
    [Tooltip("Tự động dừng âm thanh khi GameObject này bị tắt (OnDisable/ReturnToPool)")]
    [SerializeField] private bool stopOnDisable = true;

    [Tooltip("Thời gian làm mờ dần (Fade out) khi dừng âm thanh")]
    [SerializeField] private float fadeOutDuration = 0.1f;

    private PooledAudioSource currentPlayingSource;

    public string SoundId
    {
        get => soundId;
        set => soundId = value;
    }

    public bool IsLooping
    {
        get => isLooping;
        set => isLooping = value;
    }

    private void OnEnable()
    {
        if (playOnEnable)
        {
            Play();
        }
    }

    private void OnDisable()
    {
        if (stopOnDisable)
        {
            Stop();
        }
    }

    /// <summary>
    /// Kích hoạt phát âm thanh thủ công từ script hoặc Animation Event.
    /// </summary>
    public void Play()
    {
        if (string.IsNullOrEmpty(soundId) || AudioManager.Instance == null) return;

        // Nếu đang phát âm thanh cũ (đặc biệt là dạng loop), hãy dừng trước khi phát mới
        if (currentPlayingSource != null && currentPlayingSource.IsPlaying)
        {
            currentPlayingSource.StopAndRecycle();
            currentPlayingSource = null;
        }

        Transform target = followTransform ? transform : null;
        Vector3 spawnPos = transform.position + positionOffset;

        currentPlayingSource = AudioManager.Instance.PlayVFXSound(soundId, target, isLooping, volumeMultiplier, positionOffset);
    }

    /// <summary>
    /// Dừng phát âm thanh hiện tại.
    /// </summary>
    public void Stop()
    {
        if (currentPlayingSource != null)
        {
            if (fadeOutDuration > 0.01f)
            {
                currentPlayingSource.FadeOutAndStop(fadeOutDuration);
            }
            else
            {
                currentPlayingSource.StopAndRecycle();
            }
            currentPlayingSource = null;
        }
    }
}
