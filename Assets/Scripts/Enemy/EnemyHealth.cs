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

    [Tooltip("Số Chip Xanh (Data Chips) thưởng cho người chơi khi tiêu diệt quái này.")]
    [Min(0)] [SerializeField] private int dataChipReward = 1;

    [Tooltip("Số Ngọc Đỏ (Red Gems) thưởng cho người chơi khi tiêu diệt quái này (thích hợp cho Boss hoặc quái hiếm).")]
    [Min(0)] [SerializeField] private int redGemReward = 0;

    [Tooltip("Tỷ lệ rơi ngọc đỏ ngẫu nhiên khi tiêu diệt quái (Mặc định: 5% = 0.05).")]
    [Range(0f, 1f)] [SerializeField] private float randomRedGemDropChance = 0.05f;

    [Tooltip("Số ngọc đỏ rơi khi kích hoạt tỷ lệ ngẫu nhiên.")]
    [Min(1)] [SerializeField] private int randomRedGemAmount = 1;

    [Tooltip("Tỷ lệ rơi tiền khi quái chết (1 = 100% luôn rơi, 0.5 = 50% cơ hội).")]
    [Range(0f, 1f)] [SerializeField] private float currencyDropChance = 1f;

    [Header("Health Box Drops")]
    [Tooltip("Cho phép quái rơi hộp máu khi bị tiêu diệt.")]
    [SerializeField] private bool canDropHealthBox = true;

    [Tooltip("Tỷ lệ xuất hiện Hộp Máu Nhỏ (hồi 10% Max HP) khi quái chết (1.5% = 0.015).")]
    [Range(0f, 1f)] [SerializeField] private float smallHealthBoxDropChance = DropTable.SmallHealthBoxDropChance;

    [Tooltip("Tỷ lệ xuất hiện Hộp Máu Lớn (hồi 20% Max HP) khi quái chết (0.5% = 0.005).")]
    [Range(0f, 1f)] [SerializeField] private float largeHealthBoxDropChance = DropTable.LargeHealthBoxDropChance;

    [Header("Death & Animation")]
    [Tooltip("Tên Trigger kích hoạt animation Die trong Animator.")]
    [SerializeField] private string deathAnimationTrigger = "Die";

    [Tooltip("Tên State animation Die trong Animator.")]
    [SerializeField] private string deathAnimationState = "Die";

    [Tooltip("Thời gian phát animation Die tối thiểu dự phòng nếu không tìm thấy clip (giây).")]
    [SerializeField] private float fallbackDeathDuration = 0.25f;

    [Tooltip("Thời gian trễ cộng thêm trước khi quái vật bị thu hồi về Pool sau khi animation kết thúc (giây).")]
    [SerializeField] private float destroyDelay = 0f;

    [Tooltip("Thời gian hiệu ứng mờ dần (Fade-out) trước khi biến mất và thu hồi về Pool (giây).")]
    [SerializeField] private float fadeOutDuration = 0.15f;

    [Tooltip("Hiệu ứng xuất hiện tại tâm Boss khi HP về 0.")]
    [SerializeField] private GameObject bossDeathVfxPrefab;

    [Header("Damage Flash Effect")]
    [Tooltip("Bật hiệu ứng nhấp nháy đỏ khi nhận sát thương.")]
    [SerializeField] private bool enableDamageFlash = true;

    [Tooltip("Màu chuyển đổi khi nhận sát thương (Mặc định: Đỏ).")]
    [SerializeField] private Color damageFlashColor = Color.red;

    [Tooltip("Độ phủ màu đỏ khi nhận sát thương. 0.5 giữ lại chi tiết của enemy bên dưới.")]
    [SerializeField, Range(0f, 1f)] private float damageFlashIntensity = 0.5f;

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

    public float DamageFlashIntensity
    {
        get => damageFlashIntensity;
        set => damageFlashIntensity = Mathf.Clamp01(value);
    }

    public float DamageFlashDuration
    {
        get => damageFlashDuration;
        set
        {
            damageFlashDuration = Mathf.Max(0f, value);
            cachedFlashWait = new WaitForSeconds(damageFlashDuration);
        }
    }

    private int baseMaxHealth;
    private int baseExpReward;
    private int baseDataChipReward;
    private int baseRedGemReward;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public int BaseMaxHealth => baseMaxHealth > 0 ? baseMaxHealth : maxHealth;
    public int ExpReward => expReward;
    public int BaseExpReward => baseExpReward > 0 ? baseExpReward : expReward;
    public int DataChipReward => dataChipReward;
    public int BaseDataChipReward => baseDataChipReward > 0 ? baseDataChipReward : dataChipReward;
    public int RedGemReward => redGemReward;
    public int BaseRedGemReward => baseRedGemReward > 0 ? baseRedGemReward : redGemReward;
    public float CurrencyDropChance => currencyDropChance;
    public float RandomRedGemDropChance => randomRedGemDropChance;
    public int RandomRedGemAmount => randomRedGemAmount;
    public bool IsDead { get; private set; }
    public GameObject ActiveBossDeathVfx { get; private set; }

    public void SetDataChipReward(int amount) => dataChipReward = Mathf.Max(0, amount);
    public void SetRedGemReward(int amount) => redGemReward = Mathf.Max(0, amount);
    public void SetCurrencyDropChance(float chance) => currencyDropChance = Mathf.Clamp01(chance);
    public void SetRandomRedGemDropChance(float chance) => randomRedGemDropChance = Mathf.Clamp01(chance);
    public void SetRandomRedGemAmount(int amount) => randomRedGemAmount = Mathf.Max(1, amount);
    public bool CanDropHealthBox => canDropHealthBox;
    public float SmallHealthBoxDropChance => smallHealthBoxDropChance;
    public float LargeHealthBoxDropChance => largeHealthBoxDropChance;
    public void SetCanDropHealthBox(bool canDrop) => canDropHealthBox = canDrop;
    public void SetHealthBoxDropChances(float smallChance, float largeChance)
    {
        smallHealthBoxDropChance = Mathf.Clamp01(smallChance);
        largeHealthBoxDropChance = Mathf.Clamp01(largeChance);
    }

    [Header("Boss Classification")]
    [Tooltip("Đánh dấu đây là Boss (ưu tiên target cao nhất cho Player).")]
    [SerializeField] private bool isBoss = false;

    private BossEnemy bossEnemy;
    private Enemy enemyComponent;
    private bool initialIsBoss;

    public bool IsBoss
    {
        get
        {
            if (isBoss) return true;
            if (bossMovement != null || bossEnemy != null) return true;
            if (enemyComponent != null && enemyComponent.Type == EnemyType.Boss) return true;
            return false;
        }
    }

    public void SetIsBoss(bool value)
    {
        isBoss = value;
    }

    public Vector2 AimPoint => rb != null ? rb.worldCenterOfMass : (Vector2)transform.position;

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
    private int lastFlashFrame = -1;
    private float lastFlashTime = -1f;
    private WaitForSeconds cachedFlashWait;
    private float cachedDeathDuration;
    private bool hasDeathTriggerParam;
    private string defaultAnimationState = "run";
    private int defaultStateHash;
    private int deathStateHash;
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
        baseDataChipReward = dataChipReward;
        baseRedGemReward = redGemReward;
        cachedFlashWait = new WaitForSeconds(damageFlashDuration);
        initialRootScale = transform.localScale;
        colliders = GetComponentsInChildren<Collider2D>(true);
        animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        enemyMovement = GetComponent<EnemyMovement>();
        bossMovement = GetComponent<BossMovement>();
        bossEnemy = GetComponent<BossEnemy>();
        enemyComponent = GetComponent<Enemy>();
        initialIsBoss = isBoss;
        if (bossMovement != null || bossEnemy != null || (enemyComponent != null && enemyComponent.Type == EnemyType.Boss))
        {
            isBoss = true;
        }
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
        deathStateHash = 0;
        defaultStateHash = 0;

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;

            // 1. Tìm State Die/Death hợp lệ có trên Animator layer 0
            string[] candidateDeathStates = new string[]
            {
                deathAnimationState, "Die", "Death", "DeathBigcreep", "Deathcreep2", "DieBig", "death", "die"
            };
            for (int i = 0; i < candidateDeathStates.Length; i++)
            {
                if (string.IsNullOrEmpty(candidateDeathStates[i])) continue;
                int hash = Animator.StringToHash(candidateDeathStates[i]);
                if (animator.HasState(0, hash))
                {
                    deathAnimationState = candidateDeathStates[i];
                    deathStateHash = hash;
                    break;
                }
            }

            // Fallback qua clips nếu candidate chưa khớp state
            if (clips != null)
            {
                foreach (AnimationClip clip in clips)
                {
                    if (clip == null) continue;

                    if (string.Equals(clip.name, deathAnimationState, StringComparison.OrdinalIgnoreCase))
                    {
                        cachedDeathDuration = clip.length;
                    }
                    else if (cachedDeathDuration <= 0f && (
                        string.Equals(clip.name, "DieBig", StringComparison.OrdinalIgnoreCase) ||
                        clip.name.IndexOf("death", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        clip.name.IndexOf("die", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        cachedDeathDuration = clip.length;
                        if (deathStateHash == 0)
                        {
                            int clipHash = Animator.StringToHash(clip.name);
                            if (animator.HasState(0, clipHash))
                            {
                                deathAnimationState = clip.name;
                                deathStateHash = clipHash;
                            }
                        }
                    }
                }
            }

            // 2. Tìm State di chuyển / mặc định hợp lệ có trên Animator layer 0
            string[] candidateDefaultStates = new string[]
            {
                "Run", "run", "Walk", "walk", "WalkBigcreep", "Walkcreep1", "runbig", "Idle", "idle", defaultAnimationState
            };
            for (int i = 0; i < candidateDefaultStates.Length; i++)
            {
                if (string.IsNullOrEmpty(candidateDefaultStates[i])) continue;
                int hash = Animator.StringToHash(candidateDefaultStates[i]);
                if (animator.HasState(0, hash))
                {
                    defaultAnimationState = candidateDefaultStates[i];
                    defaultStateHash = hash;
                    break;
                }
            }

            if (defaultStateHash == 0 && clips != null)
            {
                foreach (AnimationClip clip in clips)
                {
                    if (clip == null) continue;
                    int hash = Animator.StringToHash(clip.name);
                    if (animator.HasState(0, hash))
                    {
                        defaultAnimationState = clip.name;
                        defaultStateHash = hash;
                        break;
                    }
                }
            }

            hasDeathTriggerParam = HasParameter(animator, deathAnimationTrigger, AnimatorControllerParameterType.Trigger);
        }
    }

    private void OnEnable()
    {
        // Khi GameObject được kích hoạt lại (kể cả không qua PoolManager.Get()),
        // đảm bảo quái luôn ở trạng thái sống và khôi phục animator/visual
        if (IsDead)
        {
            ResetForSpawn();
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

    private Coroutine bleedRoutine;

    public void ApplyBleed(int damagePerSec, float duration)
    {
        if (IsDead || damagePerSec <= 0 || duration <= 0f) return;
        if (bleedRoutine != null)
        {
            StopCoroutine(bleedRoutine);
        }
        bleedRoutine = StartCoroutine(BleedRoutine(damagePerSec, duration));
    }

    private IEnumerator BleedRoutine(int dps, float duration)
    {
        float timer = duration;
        while (timer > 0f && !IsDead)
        {
            yield return new WaitForSeconds(1.0f);
            timer -= 1.0f;
            if (!IsDead)
            {
                TakeDamage(dps);
            }
        }
        bleedRoutine = null;
    }

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, false);
    }

    public void TakeDamage(int damage, bool isCritical)
    {
        if (IsDead)
            return;

        if (damage <= 0)
            return;

        if (RadarEyeBuddy.CurrentWeakpointTarget != null && RadarEyeBuddy.CurrentWeakpointTarget == this)
        {
            damage = Mathf.RoundToInt(damage * 1.30f);
        }

        CurrentHealth -= damage;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);

        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        // Hiển thị số sát thương nhảy lên tương tự game gốc
        DamageType type = isCritical ? DamageType.Critical : DamageType.Normal;
        Vector3 spawnPos = transform.position + Vector3.up * 0.4f;
        DamageNumberManager.ShowDamage(spawnPos, damage, type);

        // Luôn kích hoạt hiệu ứng đỏ cho MỌI phát bắn trúng (kể cả phát bắn kết liễu khiến máu về 0)
        TriggerDamageFlash();

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Tiêu diệt quái tức thời (dùng khi Win game hoặc Boss bị tiêu diệt) với hiệu ứng chớp đỏ và animation chết.
    /// </summary>
    public void InstantKill(bool grantRewards = true)
    {
        if (IsDead) return;
        TriggerDamageFlash();
        Die(grantRewards);
    }

    public void InstantKillWithoutExp()
    {
        if (IsDead) return;
        TriggerDamageFlash();
        Die(true, false);
    }

    public void Die()
    {
        Die(true);
    }

    public void Die(bool grantRewards)
    {
        Die(grantRewards, true);
    }

    private void Die(bool grantRewards, bool grantExp)
    {
        if (IsDead)
            return;

        IsDead = true;

        if (CurrentHealth <= 0 && IsBoss && bossDeathVfxPrefab != null)
        {
            GameObject vfx = Instantiate(bossDeathVfxPrefab, transform.position, Quaternion.identity);
            ActiveBossDeathVfx = vfx;
            SpriteRenderer vfxRenderer = vfx.GetComponent<SpriteRenderer>();
            if (vfxRenderer != null && spriteRenderers != null)
            {
                foreach (SpriteRenderer bossRenderer in spriteRenderers)
                {
                    if (bossRenderer == null) continue;
                    int bossLayer = SortingLayer.GetLayerValueFromID(bossRenderer.sortingLayerID);
                    int vfxLayer = SortingLayer.GetLayerValueFromID(vfxRenderer.sortingLayerID);
                    if (bossLayer > vfxLayer || (bossLayer == vfxLayer && bossRenderer.sortingOrder >= vfxRenderer.sortingOrder))
                    {
                        vfxRenderer.sortingLayerID = bossRenderer.sortingLayerID;
                        vfxRenderer.sortingOrder = bossRenderer.sortingOrder + 1;
                    }
                }
            }
            float vfxDuration = 1f;
            Animator vfxAnimator = vfx.GetComponent<Animator>();
            if (vfxAnimator != null && vfxAnimator.runtimeAnimatorController != null)
            {
                AnimationClip[] clips = vfxAnimator.runtimeAnimatorController.animationClips;
                if (clips.Length > 0)
                {
                    vfxDuration = clips[0].length / Mathf.Max(0.01f, vfxAnimator.speed);
                }
            }
            Destroy(vfx, vfxDuration);
        }

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

        if (grantRewards)
        {
            try
            {
                // Boss kết thúc trận nên không thưởng EXP; quái thường rơi ngọc EXP.
                int awardedExp = !IsBoss && grantExp ? expReward : 0;
                if (awardedExp > 0)
                {
                    if (!Application.isPlaying)
                    {
                        // Fallback cho EditMode unit tests (không có physics/coroutine loop)
                        if (PlayerLevelController.Instance != null)
                        {
                            PlayerLevelController.Instance.AddEXP(awardedExp);
                        }
                    }
                    else
                    {
                        // Khi quái bị tiêu diệt: Viên ngọc kinh nghiệm nhảy văng ra từ quái, Player lại nhặt mới nhận EXP!
                        int currentWave = EnemySpawner.Instance != null ? EnemySpawner.Instance.CurrentWaveNumber : 1;
                        Enemy enemyComp = GetComponent<Enemy>() ?? GetComponentInParent<Enemy>();
                        bool isElite = enemyComp != null ? enemyComp.IsElite : (name.IndexOf("Big", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Elite", StringComparison.OrdinalIgnoreCase) >= 0);
                        DropTable.SpawnExpGemForEnemy(transform.position, awardedExp, currentWave, isElite, IsBoss, true);
                    }
                }

                // 4. Cấp tiền tệ (Data Chips / Red Gems) cho Player
                if (currencyDropChance >= 1f || UnityEngine.Random.value <= currencyDropChance)
                {
                    if (dataChipReward > 0)
                    {
                        ChipManager.AddDataChips(dataChipReward);
                    }
                    if (redGemReward > 0)
                    {
                        ChipManager.AddRedGems(redGemReward);
                    }

                    // Tỷ lệ ngẫu nhiên 5% rơi Gem đỏ khi tiêu diệt enemy
                    if (randomRedGemDropChance > 0f && UnityEngine.Random.value <= randomRedGemDropChance)
                    {
                        ChipManager.AddRedGems(randomRedGemAmount);
                    }
                }

                // Cơ hội rơi Hộp Máu (mặc định 1.5% hộp nhỏ, 0.5% hộp lớn)
                if (canDropHealthBox)
                {
                    DropTable.TryDropHealthBox(transform.position, smallHealthBoxDropChance, largeHealthBoxDropChance);
                }

                GameEvents.RaiseEnemyKilled(awardedExp);
            }
            catch (Exception ex)
            {
                Debug.LogError("[EnemyHealth] Error dropping loot on death: " + ex);
            }
        }

        // 5. Phát sự kiện để Spawner và hệ thống Achievements ghi nhận tiêu diệt
        OnEnemyDeath?.Invoke();
        OnDeath?.Invoke(this);

        // 6. Khóa chặt vị trí và chạy animation Die trọn vẹn rồi mới thu hồi / destroy
        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
        }
        deathRoutine = StartCoroutine(PlayDeathAnimationAndDespawn(transform.position, transform.rotation, transform.localScale));
    }

    private IEnumerator PlayDeathAnimationAndDespawn(Vector3 lockedPos, Quaternion lockedRot, Vector3 lockedScale)
    {
        float animDuration = cachedDeathDuration > 0f ? cachedDeathDuration : fallbackDeathDuration;
        bool isSquashDeath = transform.childCount == 0 && GetComponent<BossEnemy>() == null && (cachedDeathDuration <= 0f || cachedDeathDuration <= 0.45f);

        if (animator == null)
        {
            animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        }

        if (animator != null && animator.gameObject.activeInHierarchy)
        {
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.speed = 1f;

            if (hasDeathTriggerParam)
            {
                animator.SetTrigger(deathAnimationTrigger);
            }
            else if (deathStateHash != 0 && animator.HasState(0, deathStateHash))
            {
                animator.Play(deathStateHash, 0, 0f);
            }
            else if (!string.IsNullOrEmpty(deathAnimationState) && animator.HasState(0, Animator.StringToHash(deathAnimationState)))
            {
                animator.Play(deathAnimationState, 0, 0f);
            }
        }

        float animWait = Mathf.Max(0.05f, animDuration + destroyDelay);
        float elapsed = 0f;

        // Giai đoạn 1: Giữ cố định tọa độ tại chỗ và phát trọn vẹn animation Die/Death (không lặp)
        while (elapsed < animWait)
        {
            transform.position = lockedPos;
            transform.rotation = lockedRot;

            if (isSquashDeath)
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

            float dt = Time.timeScale > 0f ? Time.deltaTime : Time.unscaledDeltaTime;
            elapsed += dt;
            yield return null;
        }

        transform.position = lockedPos;
        Vector3 finalDeathScale = isSquashDeath
            ? new Vector3(lockedScale.x * 1.15f, lockedScale.y * 0.35f, lockedScale.z)
            : lockedScale;
        transform.localScale = finalDeathScale;

        // Dừng animation ở frame cuối cùng (normalizedTime = 1.0f) và khóa tốc độ speed = 0 (không lặp lại)
        if (animator != null && animator.gameObject.activeInHierarchy)
        {
            if (deathStateHash != 0 && animator.HasState(0, deathStateHash))
            {
                animator.Play(deathStateHash, 0, 1.0f);
            }
            else if (!string.IsNullOrEmpty(deathAnimationState) && animator.HasState(0, Animator.StringToHash(deathAnimationState)))
            {
                animator.Play(deathAnimationState, 0, 1.0f);
            }
            animator.Update(0f);
            animator.speed = 0f;
        }

        // Giai đoạn 2: Hiệu ứng Mờ dần (Fade Out) từ màu hiện tại về Alpha = 0 trong khi giữ nguyên frame cuối
        if (fadeOutDuration > 0f && spriteRenderers != null && spriteRenderers.Length > 0)
        {
            float fadeElapsed = 0f;
            while (fadeElapsed < fadeOutDuration)
            {
                transform.position = lockedPos;
                transform.rotation = lockedRot;
                transform.localScale = finalDeathScale;

                float dt = Time.timeScale > 0f ? Time.deltaTime : Time.unscaledDeltaTime;
                fadeElapsed += dt;
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
        ActiveBossDeathVfx = null;
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }
        lastFlashFrame = -1;
        lastFlashTime = -1f;

        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
            deathRoutine = null;
        }

        IsDead = false;
        CurrentHealth = maxHealth;
        isBoss = initialIsBoss || bossMovement != null || bossEnemy != null || (enemyComponent != null && enemyComponent.Type == EnemyType.Boss);

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

        // Rebind đưa toàn bộ xương và transform hierarchy về Bind Pose gốc của Prefab
        animator.Rebind();
        animator.speed = 1f;

        if (hasDeathTriggerParam)
        {
            animator.ResetTrigger(deathAnimationTrigger);
        }

        if (defaultStateHash != 0 && animator.HasState(0, defaultStateHash))
        {
            animator.Play(defaultStateHash, 0, 0f);
        }
        else if (!string.IsNullOrEmpty(defaultAnimationState) && animator.HasState(0, Animator.StringToHash(defaultAnimationState)))
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
    /// Có throttle bảo vệ chống nghẽn CPU/GPU khi nhiều viên đạn (như chùm đạn Shotgun) trúng quái cùng lúc trong 1 frame hoặc khoảng thời gian cực ngắn.
    /// </summary>
    public void TriggerDamageFlash()
    {
        if (!enableDamageFlash || !gameObject.activeInHierarchy)
            return;

        // Nếu quái đã đang trong hiệu ứng chớp đỏ của cùng 1 frame hoặc vừa chớp cách đây < 0.04s, bỏ qua để chống nghẽn CPU/GPU
        if (Time.frameCount == lastFlashFrame || (Time.time - lastFlashTime < 0.04f && flashRoutine != null))
        {
            return;
        }

        lastFlashFrame = Time.frameCount;
        lastFlashTime = Time.time;

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

                    Color orig = (initialSpriteColors != null && i < initialSpriteColors.Length)
                        ? initialSpriteColors[i]
                        : Color.white;

                    bool usesHitFlashShader = spriteRenderers[i].sharedMaterial != null &&
                        spriteRenderers[i].sharedMaterial.HasProperty(FlashAmountPropId);

                    spriteRenderers[i].GetPropertyBlock(flashPropBlock);
                    flashPropBlock.SetFloat(FlashAmountPropId, usesHitFlashShader ? damageFlashIntensity : 0f);
                    flashPropBlock.SetColor(FlashColorPropId, damageFlashColor);
                    spriteRenderers[i].SetPropertyBlock(flashPropBlock);

                    if (usesHitFlashShader)
                    {
                        // Shader tự pha màu đỏ với texture gốc, nên giữ nguyên màu renderer.
                        spriteRenderers[i].color = orig;
                    }
                    else
                    {
                        // Fallback vẫn giữ chi tiết sprite bằng cách chỉ pha màu 50%.
                        Color flashTint = new Color(damageFlashColor.r, damageFlashColor.g, damageFlashColor.b, orig.a);
                        spriteRenderers[i].color = Color.Lerp(orig, flashTint, damageFlashIntensity);
                    }
                }
            }
        }

        yield return cachedFlashWait ?? (cachedFlashWait = new WaitForSeconds(damageFlashDuration));

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
        if (animator != null)
        {
            animator.speed = 1f;
            if (hasDeathTriggerParam)
            {
                animator.ResetTrigger(deathAnimationTrigger);
            }
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
