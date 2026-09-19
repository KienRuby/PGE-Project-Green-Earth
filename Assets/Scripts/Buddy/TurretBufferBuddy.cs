using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Buddy 2: Turret Buffer - Sprite: drone-spider (ID 2).
/// Bắn đạn plasma hỗ trợ và định kỳ phát xung năng lượng cường hóa, tăng tốc độ bắn
/// và kéo dài thời gian tồn tại của Tháp súng (Gun Turret) cũng như vũ khí Player.
/// </summary>
public class TurretBufferBuddy : BuddyCombatDrone
{
    [Header("Turret Buffer Specifics")]
    [Tooltip("Khoảng thời gian phát xung cường hóa tháp súng (giây).")]
    [SerializeField] private float buffInterval = 4.0f;

    [Tooltip("Tỷ lệ tăng sát thương/tốc độ cho tháp súng hoặc người chơi.")]
    [Range(0.1f, 0.5f)]
    [SerializeField] private float buffMultiplier = 0.25f;

    public float BuffMultiplier
    {
        get => buffMultiplier;
        set => buffMultiplier = value;
    }

    [Tooltip("Prefab viên đạn bắn ra.")]
    [SerializeField] private GameObject projectilePrefab;

    [Tooltip("Tốc độ đạn plasma.")]
    [SerializeField] private float projectileSpeed = 13f;

    [SerializeField] private Color plasmaColor = new Color(0.2f, 1f, 0.8f, 1f);

    private float buffTimer;
    private static TurretBufferBuddy activeInstance;

    public static float GetActiveTurretDurationBonus()
    {
        BuddyTier tier = BuddyTier.Common;
        if (activeInstance != null)
        {
            tier = activeInstance.currentTier;
        }
        else
        {
            int activeDeck = PlayerDataService.ActiveBuddyDeckIndex;
            int[] equipped = PlayerDataService.LoadBuddyDeck(activeDeck, new int[] { 1, 2, 10 });
            if (equipped != null && System.Array.IndexOf(equipped, 2) >= 0)
            {
                BuddyItemData data = new BuddyItemData { id = 2, level = 1, tier = BuddyTier.Common };
                PlayerDataService.LoadBuddyProgress(data);
                tier = data.tier;
            }
            else
            {
                return 0f;
            }
        }

        int tierLevel = (int)tier;
        if (tierLevel >= 5) return 1.20f;
        if (tierLevel >= 4) return 0.90f;
        if (tierLevel >= 3) return 0.60f;
        if (tierLevel >= 2) return 0.30f;
        return 0.10f;
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
        if (currentTier >= BuddyTier.Epic)
        {
            GunTurret.GlobalTurretFireRateMultiplier = 1.25f;
        }
    }

    private void OnDestroy()
    {
        if (activeInstance == this)
        {
            activeInstance = null;
            GunTurret.GlobalTurretFireRateMultiplier = 1.0f;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        baseDamage = 22;
        attackCooldown = 1.0f;
    }

    protected override void Update()
    {
        base.Update();
        UpdateBuffPulse(Time.deltaTime);
    }

    private void UpdateBuffPulse(float deltaTime)
    {
        buffTimer -= deltaTime;
        if (buffTimer <= 0f)
        {
            buffTimer = buffInterval;
            TriggerBuffPulse();
        }
    }

    private void TriggerBuffPulse()
    {
        int tierLevel = (int)currentTier;
        // 1. Tìm các tháp súng đang hoạt động trên sàn đấu
        GunTurret[] turrets = FindObjectsOfType<GunTurret>();
        if (turrets != null && turrets.Length > 0)
        {
            foreach (var t in turrets)
            {
                if (t != null && t.gameObject.activeInHierarchy)
                {
                    // Rare+ (tierLevel >= 2): Pulse Extends Active Turrets (+3s)
                    if (tierLevel >= 2)
                    {
                        t.AddDuration(3.0f);
                    }

                    // Tạo hiệu ứng chớp màu xanh neon buff cho tháp dựa theo buffMultiplier
                    SpriteRenderer sr = t.GetComponentInChildren<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.color = Color.Lerp(sr.color, plasmaColor, Mathf.Clamp01(buffMultiplier * 1.5f));
                    }
                }
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
                proj.Setup(baseDamage, projectileSpeed, EffectiveAttackRange);
                proj.SetDirection(direction);
                proj.SetTarget(target.transform);
                proj.IsHoming = true;
            }

            SpriteRenderer sr = projObj.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = plasmaColor;
            }
        }
        else
        {
            target.TakeDamage(baseDamage);
        }
    }
}
