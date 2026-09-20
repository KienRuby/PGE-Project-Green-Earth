using UnityEngine;

/// <summary>
/// Buddy 3: Purifying Drone - Sprite: drone-stealth-wing (ID 10).
/// Phi thuyền tàng hình bay hộ tống, bắn xung lượng tử và định kỳ phát
/// sóng thanh tẩy diện rộng (Purification Pulse) gây sát thương và đẩy lùi (Knockback) quái vật.
/// </summary>
public class PurifyingBuddy : BuddyCombatDrone
{
    [Header("Purifying Specifics")]
    [Tooltip("Bán kính sóng thanh tẩy diện rộng (mét).")]
    [SerializeField] private float pulseRadius = 3.8f;

    [Tooltip("Khoảng thời gian kích hoạt sóng thanh tẩy (giây).")]
    [SerializeField] private float pulseInterval = 3.5f;

    [Tooltip("Lực đẩy lùi kẻ thù trong phạm vi sóng.")]
    [SerializeField] private float knockbackForce = 4.0f;

    [Tooltip("Prefab viên đạn bắn ra.")]
    [SerializeField] private GameObject projectilePrefab;

    [Tooltip("Prefab hiệu ứng sóng thanh tẩy.")]
    [SerializeField] private GameObject pulseVfxPrefab;

    [SerializeField] private Color stealthWingColor = new Color(0.1f, 0.7f, 1f, 1f);

    private float pulseTimer;
    private float emergencyShieldCooldownTimer = 0f;
    private static PurifyingBuddy activeInstance;

    public static int GetAilmentResistance()
    {
        BuddyTier tier = BuddyTier.Common;
        if (activeInstance != null)
        {
            tier = activeInstance.currentTier;
        }
        else
        {
            int activeDeck = PlayerDataService.ActiveBuddyDeckIndex;
            int[] equipped = PlayerDataService.LoadBuddyDeck(activeDeck, fallback: null);
            if (equipped != null && System.Array.IndexOf(equipped, 10) >= 0)
            {
                BuddyItemData data = new BuddyItemData { id = 10, level = 1, tier = BuddyTier.Common };
                PlayerDataService.LoadBuddyProgress(data);
                tier = data.tier;
            }
            else
            {
                return 0;
            }
        }

        int tierLevel = (int)tier;
        if (tierLevel >= 4) return 26;
        if (tierLevel >= 3) return 17;
        if (tierLevel >= 2) return 10;
        return 5;
    }

    public GameObject ProjectilePrefab
    {
        get => projectilePrefab;
        set => projectilePrefab = value;
    }

    public GameObject PulseVfxPrefab
    {
        get => pulseVfxPrefab;
        set => pulseVfxPrefab = value;
    }

    public override void Initialize(Transform targetPlayer, int slotIdx, int totalEquipped, int level = 1, BuddyTier tier = BuddyTier.Common)
    {
        base.Initialize(targetPlayer, slotIdx, totalEquipped, level, tier);
        activeInstance = this;
    }

    private void OnDestroy()
    {
        if (activeInstance == this) activeInstance = null;
    }

    protected override void Awake()
    {
        base.Awake();
        baseDamage = 26;
        attackCooldown = 1.1f;
        pulseTimer = pulseInterval;
    }

    protected override void Update()
    {
        base.Update();
        UpdatePurificationPulse(Time.deltaTime);
        UpdateEmergencyShield(Time.deltaTime);
    }

    private void UpdateEmergencyShield(float deltaTime)
    {
        if (emergencyShieldCooldownTimer > 0f)
        {
            emergencyShieldCooldownTimer -= deltaTime;
        }

        int tierLevel = (int)currentTier;
        if (tierLevel >= 5 && emergencyShieldCooldownTimer <= 0f && playerTransform != null)
        {
            PlayerHealth ph = playerTransform.GetComponent<PlayerHealth>();
            if (ph != null && !ph.IsDead && ph.CurrentHealth <= ph.MaxHealth * 0.4f)
            {
                ph.SetMaxShield(Mathf.Max(ph.MaxShield, 50));
                ph.AddShield(50);
                ph.Heal(25);
                emergencyShieldCooldownTimer = 30f;
                if (pulseVfxPrefab != null)
                {
                    Instantiate(pulseVfxPrefab, playerTransform.position, Quaternion.identity);
                }
            }
        }
    }

    private void UpdatePurificationPulse(float deltaTime)
    {
        pulseTimer -= deltaTime;
        if (pulseTimer <= 0f)
        {
            pulseTimer = pulseInterval;
            TriggerPurificationPulse();
        }
    }

    private void TriggerPurificationPulse()
    {
        Vector3 pulseCenter = playerTransform != null ? playerTransform.position : transform.position;

        // Magic+ (Tier 1+): Purifying Pulse Heals +10 HP
        int tierLevel = (int)currentTier;
        if (tierLevel >= 1 && playerTransform != null)
        {
            PlayerHealth ph = playerTransform.GetComponent<PlayerHealth>();
            if (ph != null && !ph.IsDead)
            {
                ph.Heal(10);
            }
        }

        // Quét quái vật xung quanh
        Collider2D[] colliders = Physics2D.OverlapCircleAll(pulseCenter, pulseRadius, enemyLayer);
        int pulseDamage = Mathf.RoundToInt(baseDamage * 1.25f);

        if (colliders != null && colliders.Length > 0)
        {
            foreach (var col in colliders)
            {
                if (col == null) continue;
                EnemyHealth eh = col.GetComponent<EnemyHealth>() ?? col.GetComponentInParent<EnemyHealth>();
                if (eh != null && !eh.IsDead)
                {
                    eh.TakeDamage(pulseDamage);

                    // Hiệu ứng đẩy lùi
                    Rigidbody2D enemyRb = col.GetComponent<Rigidbody2D>() ?? col.GetComponentInParent<Rigidbody2D>();
                    if (enemyRb != null && enemyRb.bodyType == RigidbodyType2D.Dynamic)
                    {
                        Vector2 pushDir = ((Vector2)col.transform.position - (Vector2)pulseCenter).normalized;
                        enemyRb.AddForce(pushDir * knockbackForce, ForceMode2D.Impulse);
                    }
                }
            }
        }

        if (pulseVfxPrefab != null)
        {
            Instantiate(pulseVfxPrefab, pulseCenter, Quaternion.identity);
        }
    }

    protected override void ExecuteAttack(EnemyHealth target)
    {
        if (target == null || target.IsDead) return;

        Vector3 spawnPos = FirePoint.position;
        Vector2 direction = ((Vector2)target.transform.position - (Vector2)spawnPos).normalized;

        if (projectilePrefab != null)
        {
            GameObject projObj = PoolManager.Instance != null
                ? PoolManager.Instance.Spawn(projectilePrefab, spawnPos, Quaternion.identity)
                : Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            Projectile proj = projObj != null ? projObj.GetComponent<Projectile>() : null;
            if (proj != null)
            {
                proj.Setup(baseDamage, 14f, EffectiveAttackRange);
                proj.SetDirection(direction);
                proj.SetTarget(target.transform);
                proj.IsHoming = true;
            }

            SpriteRenderer sr = projObj.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = stealthWingColor;
            }
        }
        else
        {
            target.TakeDamage(baseDamage);
        }
    }
}
