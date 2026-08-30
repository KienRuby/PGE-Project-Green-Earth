using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Đại diện cho một AudioSource nằm trong Object Pool của AudioManager.
/// Hỗ trợ phát âm thanh 2D/3D, bám theo mục tiêu di động (Follow Target - đạn, quái, nhân vật),
/// Fade-in/Fade-out và tự động hoàn trả về Pool khi phát xong để tối ưu bộ nhớ (Zero GC Alloc).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class PooledAudioSource : MonoBehaviour
{
    private AudioSource audioSource;
    private Transform followTarget;
    private Vector3 followOffset;
    private SoundData currentData;
    private Coroutine lifetimeCoroutine;
    private Coroutine fadeCoroutine;
    private float targetVolume = 1f;
    private bool isRecycled = false;

    public AudioSource Source => audioSource;
    public SoundData CurrentData => currentData;
    public bool IsPlaying => audioSource != null && audioSource.isPlaying;
    public bool IsLooping => audioSource != null && audioSource.loop;

    private void Awake()
    {
        EnsureAudioSource();
    }

    private void EnsureAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
        }
    }

    private void LateUpdate()
    {
        if (followTarget != null)
        {
            if (followTarget.gameObject.activeInHierarchy)
            {
                transform.position = followTarget.position + followOffset;
            }
            else
            {
                // Mục tiêu bị tắt hoặc huỷ, ngắt follow
                followTarget = null;
            }
        }
    }

    /// <summary>
    /// Phát âm thanh dựa trên cấu hình SoundData.
    /// </summary>
    public void Play(SoundData data, Vector3 position, Transform target = null, Vector3 offset = default, bool loopOverride = false, float volumeMultiplier = 1f)
    {
        EnsureAudioSource();
        StopAllCoroutines();

        isRecycled = false;
        currentData = data;
        followTarget = target;
        followOffset = offset;
        transform.position = target != null ? target.position + offset : position;

        AudioClip clip = data.GetRandomClip();
        if (clip == null)
        {
            StopAndRecycle();
            return;
        }

        audioSource.clip = clip;
        audioSource.pitch = data.GetRandomPitch();
        audioSource.spatialBlend = data.SpatialBlend;
        audioSource.minDistance = data.MinDistance;
        audioSource.maxDistance = data.MaxDistance;
        audioSource.rolloffMode = data.RolloffMode;
        audioSource.loop = loopOverride;

        // Tính volume kết hợp
        float categoryVol = AudioManager.Instance != null ? AudioManager.Instance.GetEffectiveVolume(data.Category) : 1f;
        targetVolume = Mathf.Clamp01(data.BaseVolume * volumeMultiplier * categoryVol);
        audioSource.volume = targetVolume;

        audioSource.Play();

        if (!audioSource.loop)
        {
            // Dự trù thời gian thực phát theo pitch
            float realDuration = clip.length / Mathf.Max(0.01f, Mathf.Abs(audioSource.pitch));
            lifetimeCoroutine = StartCoroutine(AutoRecycleCoroutine(realDuration));
        }
    }

    /// <summary>
    /// Phát một AudioClip đơn lẻ tùy biến (dành cho các trường hợp gọi nhanh).
    /// </summary>
    public void PlayCustom(AudioClip clip, AudioCategory category, float volume = 1f, float pitch = 1f, Vector3 position = default, float spatialBlend = 0f, bool loop = false)
    {
        EnsureAudioSource();
        StopAllCoroutines();

        isRecycled = false;
        currentData = null;
        followTarget = null;
        transform.position = position;

        if (clip == null)
        {
            StopAndRecycle();
            return;
        }

        audioSource.clip = clip;
        audioSource.pitch = pitch;
        audioSource.spatialBlend = spatialBlend;
        audioSource.loop = loop;

        float categoryVol = AudioManager.Instance != null ? AudioManager.Instance.GetEffectiveVolume(category) : 1f;
        targetVolume = Mathf.Clamp01(volume * categoryVol);
        audioSource.volume = targetVolume;

        audioSource.Play();

        if (!loop)
        {
            float realDuration = clip.length / Mathf.Max(0.01f, Mathf.Abs(pitch));
            lifetimeCoroutine = StartCoroutine(AutoRecycleCoroutine(realDuration));
        }
    }

    /// <summary>
    /// Cập nhật lại âm lượng hiện tại khi cài đặt volume chung thay đổi.
    /// </summary>
    public void UpdateVolume(float categoryVolumeFactor)
    {
        if (audioSource == null || !audioSource.isPlaying) return;
        float baseVol = currentData != null ? currentData.BaseVolume : 1f;
        audioSource.volume = Mathf.Clamp01(baseVol * categoryVolumeFactor);
    }

    /// <summary>
    /// Làm nhỏ dần âm lượng (Fade-out) rồi hoàn trả về pool.
    /// </summary>
    public void FadeOutAndStop(float duration)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        if (lifetimeCoroutine != null) StopCoroutine(lifetimeCoroutine);

        if (duration <= 0.01f || audioSource == null || !audioSource.isPlaying)
        {
            StopAndRecycle();
            return;
        }

        fadeCoroutine = StartCoroutine(FadeOutCoroutine(duration));
    }

    /// <summary>
    /// Dừng phát ngay lập tức và thu hồi về Pool.
    /// </summary>
    public void StopAndRecycle()
    {
        if (isRecycled) return;
        isRecycled = true;

        if (lifetimeCoroutine != null)
        {
            StopCoroutine(lifetimeCoroutine);
            lifetimeCoroutine = null;
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = null;
            audioSource.loop = false;
        }

        followTarget = null;
        currentData = null;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ReturnToPool(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private IEnumerator AutoRecycleCoroutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        StopAndRecycle();
    }

    private IEnumerator FadeOutCoroutine(float duration)
    {
        float startVol = audioSource.volume;
        float timer = 0f;

        while (timer < duration && audioSource != null)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / duration;
            audioSource.volume = Mathf.Lerp(startVol, 0f, t);
            yield return null;
        }

        StopAndRecycle();
    }
}
