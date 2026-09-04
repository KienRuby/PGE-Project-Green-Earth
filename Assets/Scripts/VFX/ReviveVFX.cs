using UnityEngine;

/// <summary>
/// Hiệu ứng hào quang hồi sinh (Revive Visual Effect):
/// Phát hiệu ứng ánh sáng / particle vàng-lam khi người chơi hồi sinh,
/// hỗ trợ Unscaled Time để chạy mượt mà kể cả khi game đang pause hoặc đóng băng.
/// </summary>
public class ReviveVFX : MonoBehaviour, IPoolable
{
    [Header("Revive Settings")]
    [Tooltip("Thời gian tồn tại của hiệu ứng hồi sinh (giây).")]
    [SerializeField] private float duration = 1.2f;

    [Tooltip("Dùng Unscaled Time để chạy trong lúc game pause.")]
    [SerializeField] private bool useUnscaledTime = true;

    [SerializeField] private ParticleSystem particleSys;
    [SerializeField] private Animator animator;

    private float timer;

    public float Duration => duration;

    private void Awake()
    {
        if (particleSys == null) particleSys = GetComponent<ParticleSystem>() ?? GetComponentInChildren<ParticleSystem>();
        if (animator == null) animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();

        if (animator != null && useUnscaledTime)
        {
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }
    }

    private void OnEnable()
    {
        timer = duration;
        PlayEffect();
    }

    private void Update()
    {
        timer -= useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
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
            var main = particleSys.main;
            main.useUnscaledTime = useUnscaledTime;
            particleSys.Clear();
            particleSys.Play();
        }

        if (animator != null)
        {
            animator.Play(0, 0, 0f);
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
