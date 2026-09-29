using UnityEngine;

/// <summary>
/// Buddy 3: Purifying Drone - Sprite: drone-stealth-wing (ID 10).
/// Phi thuyền tàng hình bay hộ tống và bắn xung lượng tử vào một mục tiêu.
/// </summary>
public class PurifyingBuddy : BuddyCombatDrone
{
    [Header("Purifying Specifics")]
    [Tooltip("Prefab viên đạn bắn ra.")]
    [SerializeField] private GameObject projectilePrefab;

    [SerializeField] private Color stealthWingColor = new Color(0.1f, 0.7f, 1f, 1f);

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
    }

    protected override void Update()
    {
        base.Update();
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
            }
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
