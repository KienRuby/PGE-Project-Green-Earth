using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kỹ năng Standard Gun (Súng Tiêu Chuẩn - Chipset ID 1).
/// Tự động bắn hỗ trợ mục tiêu gần nhất.
/// Quản lý 5 cấp độ trong trận đấu và đọc buff Khung Meta từ PlayerDataService (ID 1).
/// </summary>
public class StandardGunSkill : MonoBehaviour
{
    [System.Serializable]
    public struct StandardGunLevelConfig
    {
        public int damage;
        public float fireInterval;
        public float critChance;
        public float lifeSteal;
        public bool isDualParallel;
    }

    [Header("Skill Status")]
    [Tooltip("Trạng thái mở khóa của kỹ năng. Mặc định luôn bật (Luôn luôn được trang bị).")]
    [SerializeField] private bool isUnlocked = true;

    [Tooltip("Cấp độ kỹ năng hiện tại trong trận đấu (1 -> 5).")]
    [SerializeField, Range(1, 5)] private int currentLevel = 1;

    [Header("5 Level Progression Configuration (Tùy chỉnh trong Inspector)")]
    [SerializeField]
    private StandardGunLevelConfig[] levelConfigs = new StandardGunLevelConfig[]
    {
        new StandardGunLevelConfig { damage = 53, fireInterval = 0.35f, critChance = 0f, lifeSteal = 0f, isDualParallel = false },
        new StandardGunLevelConfig { damage = 65, fireInterval = 0.30f, critChance = 0f, lifeSteal = 0f, isDualParallel = false },
        new StandardGunLevelConfig { damage = 80, fireInterval = 0.25f, critChance = 0.10f, lifeSteal = 0f, isDualParallel = false },
        new StandardGunLevelConfig { damage = 100, fireInterval = 0.20f, critChance = 0.10f, lifeSteal = 0.05f, isDualParallel = false },
        new StandardGunLevelConfig { damage = 130, fireInterval = 0.15f, critChance = 0.10f, lifeSteal = 0.05f, isDualParallel = true }
    };

    [Header("Shooting & Range")]
    [SerializeField] private float attackRange = 12f;
    [SerializeField] private float bulletSpeed = 16f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Prefab References")]
    [SerializeField] private GameObject standardProjectilePrefab;
    [SerializeField] private GameObject muzzleFlashPrefab;

    private Transform playerTransform;
    private PlayerHealth playerHealth;
    private Transform attackPoint;
    private float nextFireTime;
    private readonly Collider2D[] enemyBuffer = new Collider2D[32];
    private ContactFilter2D contactFilter;

    // Meta Tier Bonuses
    private float metaDamageMultiplier = 1.0f;
    private float metaAttackSpeedMultiplier = 1.0f;
    private float metaExtraLifeSteal = 0f;
    private bool metaPenetration = false;

    public bool IsUnlocked => isUnlocked;
    public int CurrentLevel => currentLevel;

    private void Awake()
    {
        playerTransform = transform;
        playerHealth = GetComponent<PlayerHealth>();

        Transform gunTrans = transform.Find("GunPivot") ?? transform.Find("GunTransform") ?? transform.Find("AttackPoint");
        if (gunTrans != null)
        {
            attackPoint = gunTrans.Find("AttackPoint") ?? gunTrans.Find("FirePoint") ?? gunTrans;
        }
        if (attackPoint == null)
        {
            attackPoint = transform;
        }

        if (enemyLayer.value == 0)
        {
            enemyLayer = LayerMask.GetMask("Enemy");
        }

        contactFilter = new ContactFilter2D
        {
            layerMask = enemyLayer,
            useLayerMask = enemyLayer.value != 0,
            useTriggers = true
        };

        LoadPrefabsIfMissing();
        LoadMetaTierBonuses();
    }

    private void LoadPrefabsIfMissing()
    {
        if (standardProjectilePrefab == null)
        {
            standardProjectilePrefab = Resources.Load<GameObject>("Prefabs/Projectile");
#if UNITY_EDITOR
            if (standardProjectilePrefab == null)
            {
                standardProjectilePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectile.prefab");
            }
#endif
        }

        if (muzzleFlashPrefab == null)
        {
            muzzleFlashPrefab = Resources.Load<GameObject>("Prefabs/VFX shoote");
#if UNITY_EDITOR
            if (muzzleFlashPrefab == null)
            {
                muzzleFlashPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VFX shoote.prefab");
            }
#endif
        }
    }

    private void Start()
    {
        LoadMetaTierBonuses();
    }

    /// <summary>
    /// Đọc cấp bậc Khung Thẻ Chipset Meta (Chip ID 1) từ PlayerDataService:
    /// - Tier 1 (Common): Mặc định luôn trang bị.
    /// - Tier 2 (Rare): ATK +15%
    /// - Tier 3 (Epic): ATK Speed +15%
    /// - Tier 4 (Legendary): +5% Life Steal
    /// - Tier 5 (Secret): Adds Penetration Skill
    /// </summary>
    public void LoadMetaTierBonuses()
    {
        ChipTier tier = PlayerDataService.GetChipTier(1);

        metaDamageMultiplier = 1.0f;
        metaAttackSpeedMultiplier = 1.0f;
        metaExtraLifeSteal = 0f;
        metaPenetration = false;

        if (tier >= ChipTier.Rare)
        {
            metaDamageMultiplier += 0.15f; // ATK +15%
        }
        if (tier >= ChipTier.Unique) // Epic / Unique
        {
            metaAttackSpeedMultiplier += 0.15f; // ATK Speed +15%
        }
        if (tier >= ChipTier.Epic) // Legendary / Epic
        {
            metaExtraLifeSteal += 0.05f; // +5% Life Steal
        }
        if (tier == ChipTier.Holographic)
        {
            metaPenetration = true; // Adds Penetration Skill
        }
    }

    /// <summary>
    /// Mở khóa hoặc nâng cấp kỹ năng Standard Gun trong trận đấu (Cấp 1 -> 5).
    /// </summary>
    public void UnlockOrUpgrade(int targetLevel)
    {
        isUnlocked = true;
        currentLevel = Mathf.Clamp(targetLevel, 1, 5);
        LoadMetaTierBonuses();
        Debug.Log($"[StandardGunSkill] Standard Gun đã lên Cấp {currentLevel}! (Dmg: {GetCalculatedDamage()}, Cooldown: {GetCalculatedFireInterval():F2}s)");
    }

    private void Update()
    {
        if (!isUnlocked) return;
        if (playerHealth != null && playerHealth.IsDead) return;

        if (Time.time < nextFireTime) return;

        Transform target = FindNearestEnemy();
        if (target == null) return;

        FireAtTarget(target);
        nextFireTime = Time.time + GetCalculatedFireInterval();
    }

    private Transform FindNearestEnemy()
    {
        Vector2 center = playerTransform.position;
        int hitCount = Physics2D.OverlapCircle(center, attackRange, contactFilter, enemyBuffer);

        if (hitCount == 0 && enemyLayer.value != 0)
        {
            ContactFilter2D fallback = new ContactFilter2D { useTriggers = true };
            hitCount = Physics2D.OverlapCircle(center, attackRange, fallback, enemyBuffer);
        }

        Transform nearest = null;
        float minDistanceSqr = Mathf.Infinity;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = enemyBuffer[i];
            if (col == null) continue;

            EnemyHealth enemy = col.GetComponentInParent<EnemyHealth>();
            if (enemy == null || enemy.IsDead || !enemy.gameObject.activeInHierarchy) continue;

            float distSqr = ((Vector2)enemy.transform.position - center).sqrMagnitude;
            if (distSqr < minDistanceSqr)
            {
                minDistanceSqr = distSqr;
                nearest = enemy.transform;
            }
        }

        return nearest;
    }

    private void FireAtTarget(Transform target)
    {
        Vector3 spawnPos = attackPoint != null ? attackPoint.position : playerTransform.position;
        Vector2 dir = ((Vector2)target.position - (Vector2)spawnPos).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Quaternion rot = Quaternion.Euler(0f, 0f, angle);

        // 1. Muzzle Flash VFX
        SpawnMuzzleFlash(spawnPos, rot);

        // 2. Spawn & Setup Bullet
        GameObject bulletObj = null;
        if (PoolManager.Instance != null && standardProjectilePrefab != null)
        {
            bulletObj = PoolManager.Instance.Spawn(standardProjectilePrefab, spawnPos, rot);
        }
        else if (standardProjectilePrefab != null)
        {
            bulletObj = Instantiate(standardProjectilePrefab, spawnPos, rot);
        }

        if (bulletObj != null)
        {
            StandardGunProjectile standardProj = bulletObj.GetComponent<StandardGunProjectile>();
            if (standardProj == null)
            {
                standardProj = bulletObj.AddComponent<StandardGunProjectile>();
            }

            int finalDamage = GetCalculatedDamage();
            float critRate = GetCalculatedCritChance();
            float lifeStealRate = GetCalculatedLifeSteal();
            bool ricochet = currentLevel >= 5;
            bool piercing = metaPenetration;

            standardProj.Setup(
                finalDamage,
                bulletSpeed,
                attackRange * 1.25f,
                critRate,
                lifeStealRate,
                ricochet,
                piercing,
                playerHealth
            );
            standardProj.SetDirection(dir);
        }
    }

    private void SpawnMuzzleFlash(Vector3 position, Quaternion rotation)
    {
        if (muzzleFlashPrefab == null) return;

        GameObject flash;
        if (PoolManager.Instance != null)
        {
            flash = PoolManager.Instance.Spawn(muzzleFlashPrefab, position, rotation);
            if (flash != null && flash.GetComponent<AutoDestroyVFX>() == null)
            {
                PoolManager.Instance.ReturnToPool(flash, 0.12f);
            }
        }
        else
        {
            flash = Instantiate(muzzleFlashPrefab, position, rotation);
            if (flash != null && flash.GetComponent<AutoDestroyVFX>() == null)
            {
                Destroy(flash, 0.12f);
            }
        }
    }

    // --- TÍNH TOÁN CHỈ SỐ THEO CẤP ĐỘ VÀ META ---

    public int GetCalculatedDamage()
    {
        int index = Mathf.Clamp(currentLevel - 1, 0, levelConfigs.Length - 1);
        int baseDmg = levelConfigs[index].damage;
        return Mathf.RoundToInt(baseDmg * metaDamageMultiplier);
    }

    public float GetCalculatedFireInterval()
    {
        int index = Mathf.Clamp(currentLevel - 1, 0, levelConfigs.Length - 1);
        float baseInterval = levelConfigs[index].fireInterval;
        return Mathf.Max(0.05f, baseInterval / metaAttackSpeedMultiplier);
    }

    public float GetCalculatedCritChance()
    {
        int index = Mathf.Clamp(currentLevel - 1, 0, levelConfigs.Length - 1);
        return levelConfigs[index].critChance;
    }

    public float GetCalculatedLifeSteal()
    {
        int index = Mathf.Clamp(currentLevel - 1, 0, levelConfigs.Length - 1);
        float total = metaExtraLifeSteal + levelConfigs[index].lifeSteal;
        return total;
    }
}
