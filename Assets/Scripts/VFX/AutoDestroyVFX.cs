using UnityEngine;

/// <summary>
/// Tự động hủy hoặc trả về ObjectPool sau khoảng thời gian duration (hoặc theo độ dài AnimationClip của Animator).
/// </summary>
[DisallowMultipleComponent]
public class AutoDestroyVFX : MonoBehaviour
{
    [Tooltip("Thời gian tồn tại của hiệu ứng trước khi hủy/trả về pool (giây).")]
    [SerializeField] private float duration = 1.0f;

    [Tooltip("Tự động lấy thời lượng clip từ Animator nếu có.")]
    [SerializeField] private bool useAnimatorDuration = true;

    [Tooltip("Dùng thời gian thực để VFX vẫn chạy và tự hủy khi gameplay đang tạm dừng.")]
    [SerializeField] private bool useUnscaledTime = true;

    public bool UseUnscaledTime => useUnscaledTime;

    private float timer;

    private void Awake()
    {
        InitializeTimer();
    }

    private void OnEnable()
    {
        InitializeTimer();
    }

    private void InitializeTimer()
    {
        timer = duration;
        Animator animator = GetComponent<Animator>();
        if (animator != null && useUnscaledTime)
        {
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }

        if (useAnimatorDuration)
        {
            Animator anim = animator;
            if (anim != null && anim.runtimeAnimatorController != null)
            {
                AnimationClip[] clips = anim.runtimeAnimatorController.animationClips;
                if (clips != null && clips.Length > 0 && clips[0] != null)
                {
                    timer = clips[0].length;
                }
            }
        }
    }

    private void Update()
    {
        timer -= useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (timer <= 0f)
        {
            Cleanup();
        }
    }

    private void Cleanup()
    {
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.ReturnToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
