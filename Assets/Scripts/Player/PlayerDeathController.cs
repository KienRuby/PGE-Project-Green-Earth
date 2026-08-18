using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Quản lý toàn bộ chuỗi sự kiện khi Player tử trận (Death Sequence):
/// 1. Khóa toàn bộ điều khiển, va chạm và hành vi của Player.
/// 2. Kích hoạt animation "Die" trong Animator và chờ animation chạy xong (tự động theo clip hoặc thời gian tùy chỉnh).
/// 3. Chờ thêm một khoảng thời gian trễ tùy chỉnh (Pause/Delay sau khi nằm xuống).
/// 4. Tự động áp dụng Shader Dissolve và kích hoạt viền sáng rực rỡ HDR Edge Color trên toàn bộ Sprite con (thân, chân, súng,...).
/// 5. Animate thông số _DissolveAmount từ 0 -> 1 mượt mà qua MaterialPropertyBlock (giữ nguyên Sprite texture của từng bộ phận).
/// 6. Kích hoạt sự kiện OnDeathCompleted / onDeathComplete và Destroy/Disable Player GameObject.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(50)]
public class PlayerDeathController : MonoBehaviour
{
    [Header("1. Animation Die Settings")]
    [Tooltip("Tham chiếu Animator điều khiển nhân vật. Tự động lấy GetComponent nếu để trống.")]
    [SerializeField] private Animator animator;

    [Tooltip("Tên Trigger trong Animator để kích hoạt trạng thái chết.")]
    [SerializeField] private string deathTriggerName = "Die";

    [Tooltip("Tên State trong Animator chứa clip animation chết.")]
    [SerializeField] private string deathStateName = "Die";

    [Tooltip("Bật tùy chọn này nếu bạn muốn tự đặt thời gian chờ animation Die chạy xong thay vì tự động lấy độ dài clip.")]
    [SerializeField] private bool useCustomAnimationDuration = false;

    [Tooltip("Thời gian chờ animation Die chạy xong (giây) khi bật tùy chọn Use Custom Animation Duration.")]
    [SerializeField, Min(0.05f)] private float customAnimationDuration = 1.0f;

    [Tooltip("Thời gian chờ dự phòng cho animation chết (giây) nếu không tìm thấy clip hoặc chuyển cảnh chậm.")]
    [SerializeField, Min(0.05f)] private float fallbackAnimationDuration = 0.8f;

    [Header("2. Pause / Delay After Die Animation")]
    [Tooltip("Thời gian chờ/nghỉ sau khi animation Die chạy xong rồi MỚI BẮT ĐẦU xuất hiện viền sáng rực rỡ và tan biến (giây).")]
    [SerializeField, Min(0f)] private float delayBeforeDissolve = 0.3f;

    [Header("3. HDR Edge Glow & Dissolve Shader")]
    [Tooltip("Material mẫu sử dụng Shader Custom/2D/SpriteDissolve.")]
    [SerializeField] private Material dissolveMaterialPreset;

    [Tooltip("Bật hiệu ứng viền phát sáng rực rỡ bừng lên (Ignite) trước khi bắt đầu tan biến.")]
    [SerializeField] private bool enableEdgeIgnite = true;

    [Tooltip("Thời gian viền phát sáng HDR bừng lên từ mờ đến cực đại (giây).")]
    [SerializeField, Min(0.01f)] private float edgeIgniteDuration = 0.25f;

    [Tooltip("Thời lượng chạy hiệu ứng phân rã tan biến (giây).")]
    [SerializeField, Min(0.05f)] private float dissolveDuration = 1.2f;

    [Tooltip("Đường cong lerp độ tan biến từ 0 đến 1.")]
    [SerializeField] private AnimationCurve dissolveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("4. Components To Disable On Death")]
    [Tooltip("Tự động tắt Collider2D trên Player và các object con.")]
    [SerializeField] private bool disableColliders = true;

    [Tooltip("Tự động ngắt vật lý Rigidbody2D.")]
    [SerializeField] private bool disablePhysics = true;

    [Tooltip("Danh sách các Behaviour bổ sung cần disable khi chết.")]
    [SerializeField] private Behaviour[] additionalBehavioursToDisable;

    [Header("5. Completion Settings")]
    [Tooltip("Tự động hủy (Destroy) GameObject sau khi tan biến xong.")]
    [SerializeField] private bool destroyOnComplete = true;

    [Tooltip("Tự động tắt (Disable) GameObject sau khi tan biến xong (nếu không dùng Destroy).")]
    [SerializeField] private bool disableOnComplete = false;

    [Tooltip("Thời gian trễ trước khi hủy/tắt GameObject.")]
    [SerializeField, Min(0f)] private float completionDelay = 0.05f;

    [Header("Unity Events")]
    [SerializeField] private UnityEvent onDeathStarted;
    [SerializeField] private UnityEvent onEdgeGlowStarted;
    [SerializeField] private UnityEvent onDissolveStarted;
    [SerializeField] private UnityEvent onDeathComplete;

    // C# Events
    public event Action OnDeathStarted;
    public event Action OnEdgeGlowStarted;
    public event Action OnDissolveStarted;
    public event Action OnDeathCompleted;

    private static readonly int DissolveAmountPropId = Shader.PropertyToID("_DissolveAmount");
    private static readonly int EdgeIntensityPropId = Shader.PropertyToID("_EdgeIntensity");

    private bool isDeathSequenceActive;
    private PlayerHealth playerHealth;
    private SpriteRenderer[] cachedRenderers;
    private MaterialPropertyBlock propertyBlock;
    private Material runtimeDissolveMaterial;

    public bool IsDeathSequenceActive => isDeathSequenceActive;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            playerHealth = GetComponentInParent<PlayerHealth>();
        }

        propertyBlock = new MaterialPropertyBlock();
        CacheAllRenderers();
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnPlayerDeath -= TriggerDeath;
            playerHealth.OnPlayerDeath += TriggerDeath;
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnPlayerDeath -= TriggerDeath;
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnPlayerDeath -= TriggerDeath;
        }

        if (runtimeDissolveMaterial != null)
        {
            Destroy(runtimeDissolveMaterial);
            runtimeDissolveMaterial = null;
        }
    }

    /// <summary>
    /// Thu thập và lưu cache toàn bộ SpriteRenderer trong cây phân cấp của Player.
    /// </summary>
    public void CacheAllRenderers()
    {
        cachedRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    /// <summary>
    /// Kích hoạt chuỗi sự kiện Tử trận của Player.
    /// </summary>
    [ContextMenu("Trigger Death Sequence")]
    public void TriggerDeath()
    {
        if (isDeathSequenceActive)
            return;

        isDeathSequenceActive = true;

        Debug.Log("[PlayerDeathController] Bắt đầu chuỗi Death Sequence...");

        onDeathStarted?.Invoke();
        OnDeathStarted?.Invoke();

        DisablePlayerControlAndPhysics();

        StartCoroutine(DeathSequenceCoroutine());
    }

    private void DisablePlayerControlAndPhysics()
    {
        // 1. Tắt di chuyển & input
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = false;

        // 2. Tắt hệ thống tự động bắn súng
        PlayerAutoShooter shooter = GetComponent<PlayerAutoShooter>();
        if (shooter != null) shooter.enabled = false;

        // 3. Tắt PlayerAnimatorController để tránh ghi đè parameter animator
        PlayerAnimatorController animCtrl = GetComponent<PlayerAnimatorController>();
        if (animCtrl != null) animCtrl.enabled = false;

        // 4. Tắt Colliders
        if (disableColliders)
        {
            Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
            foreach (Collider2D col in colliders)
            {
                if (col != null) col.enabled = false;
            }
        }

        // 5. Ngắt Rigidbody2D
        if (disablePhysics)
        {
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.simulated = false;
            }
        }

        // 6. Tắt các component bổ sung
        if (additionalBehavioursToDisable != null)
        {
            foreach (Behaviour b in additionalBehavioursToDisable)
            {
                if (b != null) b.enabled = false;
            }
        }
    }

    private IEnumerator DeathSequenceCoroutine()
    {
        // =========================================================================
        // BƯỚC 1: KÍCH HOẠT VÀ ĐỢI ANIMATION "DIE" CHẠY XONG
        // =========================================================================
        float animationDuration = useCustomAnimationDuration ? customAnimationDuration : fallbackAnimationDuration;

        if (animator != null && animator.gameObject.activeInHierarchy)
        {
            bool hasTriggerParam = HasParameter(animator, deathTriggerName, AnimatorControllerParameterType.Trigger);
            if (hasTriggerParam)
            {
                animator.ResetTrigger(deathTriggerName);
                animator.SetTrigger(deathTriggerName);
            }
            else
            {
                int stateHash = Animator.StringToHash(deathStateName);
                if (animator.HasState(0, stateHash))
                {
                    animator.Play(stateHash, 0, 0f);
                }
            }

            // Chờ 1 frame để Animator bắt đầu chuyển trạng thái
            yield return null;

            if (!useCustomAnimationDuration)
            {
                AnimatorClipInfo[] clipInfos = animator.GetCurrentAnimatorClipInfo(0);
                if (clipInfos != null && clipInfos.Length > 0 && clipInfos[0].clip != null)
                {
                    animationDuration = clipInfos[0].clip.length;
                }
                else
                {
                    AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                    if (stateInfo.length > 0f)
                    {
                        animationDuration = stateInfo.length;
                    }
                }
            }

            // Đợi toàn bộ animation Die chạy xong
            float animTimer = 0f;
            while (animTimer < animationDuration)
            {
                animTimer += Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(animationDuration);
        }

        // =========================================================================
        // BƯỚC 2: THỜI GIAN CHỜ/NGHỈ SAU KHI NẰM XUỐNG (PAUSE TRƯỚC KHI BẬT VIỀN SÁNG)
        // =========================================================================
        if (delayBeforeDissolve > 0f)
        {
            yield return new WaitForSeconds(delayBeforeDissolve);
        }

        // =========================================================================
        // BƯỚC 3: GÁN DISSOLVE MATERIAL & BẬT HIỆU ỨNG VIỀN SÁNG HDR RỰC RỠ
        // =========================================================================
        CacheAllRenderers();
        Material dissolveMat = GetOrCreateDissolveMaterial();

        if (dissolveMat != null && cachedRenderers != null)
        {
            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                SpriteRenderer sr = cachedRenderers[i];
                if (sr != null)
                {
                    sr.sharedMaterial = dissolveMat;
                }
            }
        }

        onEdgeGlowStarted?.Invoke();
        OnEdgeGlowStarted?.Invoke();

        // Hiệu ứng viền phát sáng bừng lên rực rỡ (Edge Ignite / Glow In)
        if (enableEdgeIgnite && edgeIgniteDuration > 0f)
        {
            // Đặt mức tan biến ban đầu rất nhỏ để viền phát sáng xuất hiện bao quanh sprite
            float initialDissolveForGlow = 0.01f;
            float igniteElapsed = 0f;

            while (igniteElapsed < edgeIgniteDuration)
            {
                igniteElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(igniteElapsed / edgeIgniteDuration);
                float intensity = Mathf.Lerp(0f, 1f, t);

                SetDissolveAndEdgeIntensity(initialDissolveForGlow, intensity);
                yield return null;
            }
            SetDissolveAndEdgeIntensity(initialDissolveForGlow, 1f);
        }
        else
        {
            SetDissolveAndEdgeIntensity(0f, 1f);
        }

        onDissolveStarted?.Invoke();
        OnDissolveStarted?.Invoke();

        // =========================================================================
        // BƯỚC 4: ANIMATE TAN BIẾN (_DissolveAmount TỪ 0 -> 1)
        // =========================================================================
        float dissolveElapsed = 0f;
        float safeDuration = Mathf.Max(0.05f, dissolveDuration);

        while (dissolveElapsed < safeDuration)
        {
            dissolveElapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(dissolveElapsed / safeDuration);
            float evaluatedValue = dissolveCurve != null ? dissolveCurve.Evaluate(normalized) : normalized;

            SetDissolveAndEdgeIntensity(evaluatedValue, 1f);
            yield return null;
        }

        // Đảm bảo kết thúc ở 1.0 (tan biến hoàn toàn 100%)
        SetDissolveAndEdgeIntensity(1.0f, 1f);

        if (completionDelay > 0f)
        {
            yield return new WaitForSeconds(completionDelay);
        }

        // =========================================================================
        // BƯỚC 5: HOÀN TẤT VÀ HỦY / TẮT GAMEOBJECT
        // =========================================================================
        Debug.Log("[PlayerDeathController] Death Sequence hoàn tất.");

        onDeathComplete?.Invoke();
        OnDeathCompleted?.Invoke();

        if (destroyOnComplete)
        {
            Destroy(gameObject);
        }
        else if (disableOnComplete)
        {
            gameObject.SetActive(false);
        }
    }

    private void SetDissolveAndEdgeIntensity(float dissolveAmount, float edgeIntensity)
    {
        if (cachedRenderers == null || cachedRenderers.Length == 0)
            return;

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            SpriteRenderer sr = cachedRenderers[i];
            if (sr != null)
            {
                sr.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat(DissolveAmountPropId, dissolveAmount);
                propertyBlock.SetFloat(EdgeIntensityPropId, edgeIntensity);
                sr.SetPropertyBlock(propertyBlock);
            }
        }
    }

    private Material GetOrCreateDissolveMaterial()
    {
        if (dissolveMaterialPreset != null) return dissolveMaterialPreset;
        if (runtimeDissolveMaterial != null) return runtimeDissolveMaterial;

        Shader shader = Shader.Find("Custom/2D/SpriteDissolve") ?? Shader.Find("Sprites/Default");
        if (shader != null)
        {
            runtimeDissolveMaterial = new Material(shader) { name = "Runtime_PlayerDissolve_Mat" };
            return runtimeDissolveMaterial;
        }

        return null;
    }

    private static bool HasParameter(Animator anim, string paramName, AnimatorControllerParameterType type)
    {
        if (anim == null || string.IsNullOrEmpty(paramName)) return false;
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.type == type && param.name == paramName) return true;
        }
        return false;
    }

    /// <summary>
    /// Khôi phục trạng thái Player về ban đầu (phục vụ trường hợp Respawn / Object Pooling).
    /// </summary>
    public void ResetDissolve()
    {
        StopAllCoroutines();
        isDeathSequenceActive = false;
        SetDissolveAndEdgeIntensity(0f, 1f);
    }
}
