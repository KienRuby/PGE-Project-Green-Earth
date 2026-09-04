using UnityEngine;

/// <summary>
/// Hiệu ứng vụ nổ (Explosion VFX):
/// - Kích hoạt Particle System / Sprite Animation khi sinh ra.
/// - Gửi xung rung màn hình (Screen Shake) qua ScreenShakeService.
/// - Tự động thu hồi về ObjectPool sau khi hoàn tất thời lượng.
/// </summary>
public class VFXBoom : MonoBehaviour, IPoolable
{
    [Header("Explosion Settings")]
    [Tooltip("Thời lượng tồn tại trước khi thu hồi (giây).")]
    [SerializeField] private float duration = 0.6f;

    [Tooltip("Cường độ rung màn hình khi nổ.")]
    [SerializeField] private float screenShakeIntensity = 0.25f;

    [Tooltip("Thời gian rung màn hình (giây).")]
    [SerializeField] private float screenShakeDuration = 0.2f;

    [Tooltip("Bật rung màn hình khi nổ.")]
    [SerializeField] private bool enableScreenShake = true;

    [Header("Particle & Animator")]
    [SerializeField] private ParticleSystem particleSys;
    [SerializeField] private Animator animator;

    private float timer;

    public float Duration => duration;

    private void Awake()
    {
        if (particleSys == null) particleSys = GetComponent<ParticleSystem>() ?? GetComponentInChildren<ParticleSystem>();
        if (animator == null) animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        timer = duration;
        PlayEffect();
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            Despawn();
        }
    }

    public void PlayEffect()
    {
        timer = duration;

        if (particleSys != null)
        {
            particleSys.Clear();
            particleSys.Play();
        }

        if (animator != null)
        {
            animator.Play(0, 0, 0f);
        }

        if (enableScreenShake && ScreenShakeService.Instance != null)
        {
            ScreenShakeService.Shake(screenShakeIntensity, screenShakeDuration);
        }
    }

    public void Despawn()
    {
        PoolMember member = GetComponent<PoolMember>();
        if (member != null && member.Pool != null)
        {
            member.ReturnToPool();
        }
        else if (PoolManager.Instance != null)
        {
            PoolManager.Instance.ReturnToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void OnSpawnFromPool()
    {
        PlayEffect();
    }

    public void OnReturnToPool()
    {
        if (particleSys != null) particleSys.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
