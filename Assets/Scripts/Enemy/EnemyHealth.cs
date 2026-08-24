using System;
using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable, IPoolable
{
    [Header("Health")]
    [Tooltip("Lượng máu tối đa của quái vật.")]
    [SerializeField] private int maxHealth = 50;

    [Header("Reward")]
    [Tooltip("Lượng điểm kinh nghiệm (EXP) thưởng cho người chơi khi tiêu diệt quái này.")]
    [SerializeField] private int expReward = 10;

    [Header("Death & Animation")]
    [Tooltip("Tên Trigger kích hoạt animation Die trong Animator.")]
    [SerializeField] private string deathAnimationTrigger = "Die";

    [Tooltip("Tên State animation Die trong Animator.")]
    [SerializeField] private string deathAnimationState = "Die";

    [Tooltip("Thời gian phát animation Die tối thiểu dự phòng nếu không tìm thấy clip (giây).")]
    [SerializeField] private float fallbackDeathDuration = 0.5f;

    [Tooltip("Thời gian trễ cộng thêm trước khi quái vật bị thu hồi về Pool sau khi animation kết thúc (giây).")]
    [SerializeField] private float destroyDelay = 0f;

    [Tooltip("Thời gian hiệu ứng mờ dần (Fade-out) trước khi biến mất và thu hồi về Pool (giây).")]
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("Damage Flash Effect")]
    [Tooltip("Bật hiệu ứng nhấp nháy đỏ khi nhận sát thương.")]
    [SerializeField] private bool enableDamageFlash = true;

    [Tooltip("Màu chuyển đổi khi nhận sát thương (Mặc định: Đỏ).")]
    [SerializeField] private Color damageFlashColor = Color.red;

    [Tooltip("Thời gian nhấp nháy màu đỏ khi nhận sát thương (giây). 0.15s cho mỗi lần nhận dame.")]
    [SerializeField] private float damageFlashDuration = 0.15f;

    [Tooltip("Material dùng shader Custom/2D/SpriteHitFlash. Nếu để trống sẽ tự động tìm hoặc nạp từ Shader/Assets.")]
    [SerializeField] private Material hitFlashMaterial;

    public float FadeOutDuration
    {
        get => fadeOutDuration;
        set => fadeOutDuration = Mathf.Max(0f, value);
    }

    public bool EnableDamageFlash
    {
        get => enableDamageFlash;
        set => enableDamageFlash = value;
    }

    public Color DamageFlashColor
    {
        get => damageFlashColor;
        set => damageFlashColor = value;
    }

    public float DamageFlashDuration
    {
        get => damageFlashDuration;
        set => damageFlashDuration = Mathf.Max(0f, value);
    }

    private int baseMaxHealth;
    private int baseExpReward;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public int BaseMaxHealth => baseMaxHealth > 0 ? baseMaxHealth : maxHealth;
    public int ExpReward => expReward;
    public int BaseExpReward => baseExpReward > 0 ? baseExpReward : expReward;
    public bool IsDead { get; private set; }

    public event Action<int, int> OnHealthChanged;
    public event Action OnEnemyDeath;
    public event Action<EnemyHealth> OnDeath;

    private static Material sharedHitFlashMaterial;
    private static readonly int FlashAmountPropId = Shader.PropertyToID("_FlashAmount");
    private static readonly int FlashColorPropId = Shader.PropertyToID("_FlashColor");
    private MaterialPropertyBlock flashPropBlock;

    private Collider2D[] colliders;
    private Animator animator;
    private EnemyMovement enemyMovement;
    private BossMovement bossMovement;
    private Rigidbody2D rb;
    private SpriteRenderer[] spriteRenderers;
    private Color[] initialSpriteColors;
    private Coroutine flashRoutine;
    private float cachedDeathDuration;
    private bool hasDeathTriggerParam;
    private string defaultAnimationState = "run";
    private int defaultStateHash;
    private Coroutine deathRoutine;
    private Vector3 initialRootScale;

    private struct ChildTransformSnapshot
    {
        public Transform transform;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;
    }

    private ChildTransformSnapshot[] cachedChildSnapshots;

    private void Awake()
    {
        baseMaxHealth = maxHealth;
        baseExpReward = expReward;
        initialRootScale = transform.localScale;
        colliders = GetComponentsInChildren<Collider2D>(true);
        animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        enemyMovement = GetComponent<EnemyMovement>();
        bossMovement = GetComponent<BossMovement>();
        rb = GetComponent<Rigidbody2D>();
        CurrentHealth = maxHealth;

        CacheSpriteRenderers();

        // Lưu lại vị trí/góc xoay/tỉ lệ ban đầu của tất cả child transforms (chân, thân, v.v. - trừ root)
        // để khôi phục chính xác 100% khi tái sử dụng từ Pool mà không ghi đè vị trí spawn của quái
        Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
        var childList = new System.Collections.Generic.List<ChildTransformSnapshot>(allTransforms.Length);
        for (int i = 0; i < allTransforms.Length; i++)
        {
            if (allTransforms[i] == transform) continue; // Bỏ qua Root transform để giữ nguyên spawn position
            childList.Add(new ChildTransformSnapshot
            {
                transform = allTransforms[i],
                localPosition = allTransforms[i].localPosition,
                localRotation = allTransforms[i].localRotation,
                localScale = allTransforms[i].localScale
            });
        }
        cachedChildSnapshots = childList.ToArray();

        CacheDeathAnimationSettings();
    }

    public Material HitFlashMaterial
    {
        get => hitFlashMaterial;
        set
        {
            hitFlashMaterial = value;
            if (hitFlashMaterial != null)
            {
                sharedHitFlashMaterial = hitFlashMaterial;
            }
        }
    }

    public void CacheSpriteRenderers(bool forceRecache = false)
    {
        if (!forceRecache && spriteRenderers != null && initialSpriteColors != null && initialSpriteColors.Length == spriteRenderers.Length)
        {
            return;
        }

        if (flashPropBlock == null)
        {
            flashPropBlock = new MaterialPropertyBlock();
        }

        if (hitFlashMaterial != null)
        {
            sharedHitFlashMaterial = hitFlashMaterial;
        }
        else if (sharedHitFlashMaterial == null)
        {
            Shader hitShader = Shader.Find("Custom/2D/SpriteHitFlash");
            if (hitShader != null)
            {
                sharedHitFlashMaterial = new Material(hitShader);
                sharedHitFlashMaterial.name = "Runtime_SpriteHitFlash_Shared";
            }
        }

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (spriteRenderers != null && spriteRenderers.Length > 0)
        {
            initialSpriteColors = new Color[spriteRenderers.Length];
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    initialSpriteColors[i] = spriteRenderers[i].color;

                    if (sharedHitFlashMaterial != null && (spriteRenderers[i].sharedMaterial == null ||
                        spriteRenderers[i].sharedMaterial.shader == null ||
                        !spriteRenderers[i].sharedMaterial.HasProperty(FlashAmountPropId)))
                    {
                        spriteRenderers[i].sharedMaterial = sharedHitFlashMaterial;
                    }
                }
            }
        }
    }

    private void CacheDeathAnimationSettings()
    {
        cachedDeathDuration = fallbackDeathDuration;
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
            if (clips != null)
            {
                foreach (AnimationClip clip in clips)
                {
                    if (clip != null)
                    {
                        if (string.Equals(clip.name, deathAnimationState, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(clip.name, "DieBig", StringComparison.OrdinalIgnoreCase))
                        {
                            cachedDeathDuration = clip.length;
                        }
                        else if (string.Equals(clip.name, "run", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(clip.name, "Run", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(clip.name, "runbig", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(clip.name, "walk", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(clip.name, "idle", StringComparison.OrdinalIgnoreCase))
                        {
                            defaultAnimationState = clip.name;
                            defaultStateHash = Animator.StringToHash(clip.name);
                        }
                    }
                }
            }
            if (defaultStateHash == 0 && !string.IsNullOrEmpty(defaultAnimationState))
            {
                defaultStateHash = Animator.StringToHash(defaultAnimationState);
            }
            hasDeathTriggerParam = HasParameter(animator, deathAnimationTrigger, AnimatorControllerParameterType.Trigger);
        }
    }

    private void Start()
    {
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void SetMaxHealth(int newMaxHealth, bool resetCurrentHealth = true)
    {
        maxHealth = Mathf.Max(1, newMaxHealth);
        if (resetCurrentHealth)
        {
            CurrentHealth = maxHealth;
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }
    }

    public void SetExpReward(int newExpReward)
    {
        expReward = Mathf.Max(0, newExpReward);
    }

    public void TakeDamage(int damage)
    {
        if (IsDead)
            return;

        if (damage <= 0)
            return;

        CurrentHealth -= damage;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);

        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        // Luôn kích hoạt hiệu ứng đỏ cho MỌI phát bắn trúng (kể cả phát bắn kết liễu khiến máu về 0)
        TriggerDamageFlash();

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (IsDead)
            return;

        IsDead = true;

        // 1. Vô hiệu hóa toàn bộ collider để không nhận thêm sát thương hay va chạm người chơi
        if (colliders != null)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null) colliders[i].enabled = false;
            }
        }

        // 2. Dừng chuyển động, khóa vật lý để không bị quán tính hay lực đẩy xê dịch
        if (enemyMovement != null) enemyMovement.enabled = false;
        if (bossMovement != null) bossMovement.enabled = false;
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false; // Ngắt hoàn toàn khỏi hệ thống vật lý để đứng yên 100%
        }

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        // 3. Cấp kinh nghiệm cho Player
        if (PlayerLevelController.Instance != null && expReward > 0)
        {
            PlayerLevelController.Instance.AddEXP(expReward);
        }

        // 4. Phát sự kiện để Spawner và hệ thống Achievements ghi nhận tiêu diệt
        OnEnemyDeath?.Invoke();
        OnDeath?.Invoke(this);
        GameEvents.RaiseEnemyKilled();

        // 5. Khóa chặt vị trí và chạy animation Die trọn vẹn rồi mới thu hồi / destroy
        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
        }
        deathRoutine = StartCoroutine(PlayDeathAnimationAndDespawn(transform.position, transform.rotation, transform.localScale));
    }

    private IEnumerator PlayDeathAnimationAndDespawn(Vector3 lockedPos, Quaternion lockedRot, Vector3 lockedScale)
    {
        float animDuration = cachedDeathDuration > 0f ? cachedDeathDuration : fallbackDeathDuration;
        bool isSingleSprite = transform.childCount == 0;

        // Nếu là quái single-sprite (như BigCreep), giới hạn thời gian diễn hoạt chết gọn gàng (0.4s)
        if (isSingleSprite)
        {
            animDuration = Mathf.Min(0.4f, animDuration);
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        }

        if (animator != null && animator.gameObject.activeInHierarchy)
        {
            animator.applyRootMotion = false;
            animator.speed = 1f;

            if (hasDeathTriggerParam)
            {
                animator.SetTrigger(deathAnimationTrigger);
            }
            else
            {
                animator.Play(deathAnimationState, 0, 0f);
            }
        }

        float animWait = Mathf.Max(0.05f, animDuration + destroyDelay);
        float elapsed = 0f;

        // Giai đoạn 1: Giữ cố định tọa độ tại chỗ và phát trọn vẹn animation Die
        while (elapsed < animWait)
        {
            transform.position = lockedPos;
            transform.rotation = lockedRot;

            if (isSingleSprite)
            {
                // Hiệu ứng Squash mượt mà: co xẹp Y xuống 35% và giãn nhẹ X 15%, giữ nguyên 100% hướng mặt
                float t = Mathf.Clamp01(elapsed / animWait);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                float squashY = Mathf.Lerp(1f, 0.35f, smoothT);
                float expandX = Mathf.Lerp(1f, 1.15f, smoothT);
                transform.localScale = new Vector3(lockedScale.x * expandX, lockedScale.y * squashY, lockedScale.z);
            }
            else
            {
                transform.localScale = lockedScale;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = lockedPos;
        Vector3 finalDeathScale = isSingleSprite
            ? new Vector3(lockedScale.x * 1.15f, lockedScale.y * 0.35f, lockedScale.z)
            : lockedScale;
        transform.localScale = finalDeathScale;

        // Khóa đúng pose cuối của Die trong suốt thời gian fade.
        if (animator != null && animator.gameObject.activeInHierarchy)
        {
            animator.Play(deathAnimationState, 0, 0.999f);
            animator.Update(0f);
            animator.speed = 0f;
            animator.enabled = false;
        }

        // Giai đoạn 2: Hiệu ứng Mờ dần (Fade Out) từ màu hiện tại về Alpha = 0
        if (fadeOutDuration > 0f && spriteRenderers != null && spriteRenderers.Length > 0)
        {
            float fadeElapsed = 0f;
            while (fadeElapsed < fadeOutDuration)
            {
                transform.position = lockedPos;
                transform.rotation = lockedRot;
                transform.localScale = finalDeathScale;

                fadeElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(fadeElapsed / fadeOutDuration);

                for (int i = 0; i < spriteRenderers.Length; i++)
                {
                    if (spriteRenderers[i] != null)
                    {
                        Color orig = (initialSpriteColors != null && i < initialSpriteColors.Length) ? initialSpriteColors[i] : Color.white;
                        float newAlpha = Mathf.Lerp(orig.a, 0f, t);
                        spriteRenderers[i].color = new Color(orig.r, orig.g, orig.b, newAlpha);
                    }
                }

                yield return null;
            }

            // Đảm bảo alpha về 0 hoàn toàn trước khi thu hồi
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    Color orig = (initialSpriteColors != null && i < initialSpriteColors.Length) ? initialSpriteColors[i] : Color.white;
                    spriteRenderers[i].color = new Color(orig.r, orig.g, orig.b, 0f);
                }
            }
        }

        deathRoutine = null;
        Despawn();
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
        ResetForSpawn();
    }

    /// <summary>
    /// Hợp đồng Reset duy nhất: Đảm bảo mọi Runtime State, Visual Pose, Physics và Animator
    /// được khôi phục 100% về trạng thái sống ngay tại Frame 0 khi lấy từ Pool.
    /// </summary>
    public void ResetForSpawn()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
            deathRoutine = null;
        }

        IsDead = false;
        CurrentHealth = maxHealth;

        ResetVisualState();
        ResetPhysicsState();
        ResetMovementState();
        ResetAnimatorState();

        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    private void ResetVisualState()
    {
        transform.rotation = Quaternion.identity;
        if (initialRootScale != Vector3.zero)
        {
            transform.localScale = initialRootScale;
        }

        if (cachedChildSnapshots != null)
        {
            for (int i = 0; i < cachedChildSnapshots.Length; i++)
            {
                Transform t = cachedChildSnapshots[i].transform;
                if (t != null && t != transform)
                {
                    t.localPosition = cachedChildSnapshots[i].localPosition;
                    t.localRotation = cachedChildSnapshots[i].localRotation;
                    t.localScale = cachedChildSnapshots[i].localScale;
                }
            }
        }

        // Khôi phục lại toàn bộ màu sắc và FlashAmount = 0 ban đầu cho các SpriteRenderer
        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            CacheSpriteRenderers();
        }

        RestoreSpriteColors();
    }

    private void ResetPhysicsState()
    {
        if (colliders != null)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null) colliders[i].enabled = true;
            }
        }

        if (rb != null)
        {
            rb.simulated = true;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    private void ResetMovementState()
    {
        if (enemyMovement != null) enemyMovement.enabled = true;
        if (bossMovement != null) bossMovement.enabled = true;
    }

    private void ResetAnimatorState()
    {
        if (animator == null) return;

        animator.enabled = true;
        animator.applyRootMotion = false;
        animator.speed = 1f;
        if (hasDeathTriggerParam)
        {
            animator.ResetTrigger(deathAnimationTrigger);
        }

        if (defaultStateHash != 0)
        {
            animator.Play(defaultStateHash, 0, 0f);
        }
        else if (!string.IsNullOrEmpty(defaultAnimationState))
        {
            animator.Play(defaultAnimationState, 0, 0f);
        }
        else
        {
            animator.Play(0, 0, 0f);
        }

        // Cập nhật ngay tại frame 0 để render chuẩn pose di chuyển ngay frame đầu tiên
        animator.Update(0f);
    }

    /// <summary>
    /// Kích hoạt hiệu ứng chớp đỏ đúng 1 lần duy nhất cho mỗi lần nhận sát thương.
    /// Nhận bao nhiêu lần sát thương sẽ chớp bấy nhiêu lần riêng biệt.
    /// </summary>
    public void TriggerDamageFlash()
    {
        if (!enableDamageFlash || !gameObject.activeInHierarchy)
            return;

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            RestoreSpriteColors();
        }
        flashRoutine = StartCoroutine(DamageFlashRoutine());
    }

    private IEnumerator DamageFlashRoutine()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            CacheSpriteRenderers();
        }

        if (flashPropBlock == null)
        {
            flashPropBlock = new MaterialPropertyBlock();
        }

        if (spriteRenderers != null)
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    if (sharedHitFlashMaterial != null && (spriteRenderers[i].sharedMaterial == null || !spriteRenderers[i].sharedMaterial.HasProperty(FlashAmountPropId)))
                    {
                        spriteRenderers[i].sharedMaterial = sharedHitFlashMaterial;
                    }

                    spriteRenderers[i].GetPropertyBlock(flashPropBlock);
                    flashPropBlock.SetFloat(FlashAmountPropId, 1f);
                    flashPropBlock.SetColor(FlashColorPropId, damageFlashColor);
                    spriteRenderers[i].SetPropertyBlock(flashPropBlock);

                    Color orig = (initialSpriteColors != null && i < initialSpriteColors.Length)
                        ? initialSpriteColors[i]
                        : Color.white;
                    spriteRenderers[i].color = new Color(damageFlashColor.r, damageFlashColor.g, damageFlashColor.b, orig.a);
                }
            }
        }

        yield return new WaitForSeconds(damageFlashDuration);

        RestoreSpriteColors();
        flashRoutine = null;
    }

    /// <summary>
    /// Khôi phục lại màu sắc ban đầu của các SpriteRenderer.
    /// </summary>
    public void RestoreSpriteColors()
    {
        if (bossMovement != null && bossMovement.IsEnraged)
        {
            // Boss đang trong trạng thái Enrage, không ghi đè màu cuồng nộ
            return;
        }

        if (flashPropBlock == null)
        {
            flashPropBlock = new MaterialPropertyBlock();
        }

        if (spriteRenderers != null && initialSpriteColors != null)
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    spriteRenderers[i].GetPropertyBlock(flashPropBlock);
                    flashPropBlock.SetFloat(FlashAmountPropId, 0f);
                    spriteRenderers[i].SetPropertyBlock(flashPropBlock);

                    if (i < initialSpriteColors.Length)
                    {
                        spriteRenderers[i].color = initialSpriteColors[i];
                    }
                }
            }
        }
    }

    public void OnReturnToPool()
    {
        IsDead = true;
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }
        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
            deathRoutine = null;
        }
        if (animator != null && hasDeathTriggerParam)
        {
            animator.ResetTrigger(deathAnimationTrigger);
        }
        OnDeath = null;
        OnEnemyDeath = null;
        OnHealthChanged = null;
    }

    private void OnDestroy()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }
        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
            deathRoutine = null;
        }
        OnDeath = null;
        OnEnemyDeath = null;
        OnHealthChanged = null;
    }
}
