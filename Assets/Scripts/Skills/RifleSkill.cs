using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kỹ năng Rifle (Súng Trường - Chipset ID 2).
/// Xả loạt đạn liên thanh tốc độ cao nhắm mục tiêu gần nhất.
/// Quản lý 5 cấp độ trong trận đấu (Cấp 5 bắn 2 tia song song) và nhận buff Khung Meta từ PlayerDataService (ID 2).
/// </summary>
public class RifleSkill : MonoBehaviour
{
    [System.Serializable]
    public struct RifleLevelConfig
    {
        public int damage;
        public float fireInterval;
        public float pierceChance;
        public int guaranteedPierceCount;
        public bool isDualParallel;
    }

    [Header("Skill Status")]
    [Tooltip("Trạng thái mở khóa của kỹ năng.")]
    [SerializeField] private bool isUnlocked = false;

    [Tooltip("Cấp độ kỹ năng hiện tại trong trận đấu (1 -> 5).")]
    [SerializeField, Range(1, 5)] private int currentLevel = 1;

    [Header("5 Level Progression Configuration (Tùy chỉnh trong Inspector)")]
    [SerializeField]
    private RifleLevelConfig[] levelConfigs = new RifleLevelConfig[]
    {
        new RifleLevelConfig { damage = 15, fireInterval = 0.20f, pierceChance = 0f, guaranteedPierceCount = 0, isDualParallel = false },
        new RifleLevelConfig { damage = 20, fireInterval = 0.18f, pierceChance = 0f, guaranteedPierceCount = 0, isDualParallel = false },
        new RifleLevelConfig { damage = 25, fireInterval = 0.15f, pierceChance = 0.20f, guaranteedPierceCount = 0, isDualParallel = false },
        new RifleLevelConfig { damage = 30, fireInterval = 0.12f, pierceChance = 1.0f, guaranteedPierceCount = 1, isDualParallel = false },
        new RifleLevelConfig { damage = 40, fireInterval = 0.10f, pierceChance = 1.0f, guaranteedPierceCount = 1, isDualParallel = true }
    };

    [Header("Shooting & Range")]
    [SerializeField] private float attackRange = 12f;
    [SerializeField] private float bulletSpeed = 18f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Prefab References")]
    [SerializeField] private GameObject rifleProjectilePrefab;
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
        if (rifleProjectilePrefab == null)
        {
            rifleProjectilePrefab = Resources.Load<GameObject>("Prefabs/Projectile");
#if UNITY_EDITOR
            if (rifleProjectilePrefab == null)
            {
                rifleProjectilePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectile.prefab");
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
    /// Đọc cấp bậc Khung Thẻ Chipset Meta (Chip ID 2) từ PlayerDataService:
    /// - Tier 1 (Common/Magic): ATK: 10.5, Fast ATK Speed.
    /// - Tier 2 (Rare): ATK +25%
    /// - Tier 3 (Unique): ATK Speed +20%
    /// - Tier 4 (Epic): ATK +80%
    /// - Tier 5 (Holographic/Secret): ATK Speed +35%
    /// </summary>
    public void LoadMetaTierBonuses()
    {
        ChipTier tier = PlayerDataService.GetChipTier(2);

        metaDamageMultiplier = 1.0f;
        metaAttackSpeedMultiplier = 1.0f;

        if (tier >= ChipTier.Rare)
        {
            metaDamageMultiplier += 0.25f; // ATK +25%
        }
        if (tier >= ChipTier.Unique)
        {
            metaAttackSpeedMultiplier += 0.20f; // ATK Speed +20%
        }
        if (tier >= ChipTier.Epic)
        {
            metaDamageMultiplier += 0.80f; // ATK +80%
        }
        if (tier == ChipTier.Holographic)
        {
            metaAttackSpeedMultiplier += 0.35f; // ATK Speed +35%
        }
    }

    /// <summary>
    /// Mở khóa hoặc nâng cấp kỹ năng Rifle trong trận đấu (Cấp 1 -> 5).
    /// </summary>
    public void UnlockOrUpgrade(int targetLevel)
    {
        isUnlocked = true;
        currentLevel = Mathf.Clamp(targetLevel, 1, 5);
        LoadMetaTierBonuses();
        Debug.Log($"[RifleSkill] Rifle đã lên Cấp {currentLevel}! (Dmg: {GetCalculatedDamage()}, Cooldown: {GetCalculatedFireInterval():F3}s)");
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
        Vector3 baseSpawnPos = attackPoint != null ? attackPoint.position : playerTransform.position;
        Vector2 dir = ((Vector2)target.position - (Vector2)baseSpawnPos).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Quaternion rot = Quaternion.Euler(0f, 0f, angle);

        // Muzzle flash
        SpawnMuzzleFlash(baseSpawnPos, rot);

        int finalDamage = GetCalculatedDamage();
        float chanceToPierce = GetCalculatedPierceChance();
        int guaranteedPierce = GetCalculatedGuaranteedPierce();

        if (currentLevel >= 5)
        {
            // Cấp 5 (Tối thượng): Bắn ra 2 tia đạn song song
            Vector2 perpDir = new Vector2(-dir.y, dir.x).normalized;
            float parallelOffset = 0.16f;

            Vector3 pos1 = baseSpawnPos + (Vector3)(perpDir * parallelOffset);
            Vector3 pos2 = baseSpawnPos - (Vector3)(perpDir * parallelOffset);

            SpawnSingleRifleBullet(pos1, dir, rot, finalDamage, chanceToPierce, guaranteedPierce);
            SpawnSingleRifleBullet(pos2, dir, rot, finalDamage, chanceToPierce, guaranteedPierce);
        }
        else
        {
            // Cấp 1-4: Bắn 1 tia đạn
            SpawnSingleRifleBullet(baseSpawnPos, dir, rot, finalDamage, chanceToPierce, guaranteedPierce);
        }
    }

    private void SpawnSingleRifleBullet(
        Vector3 position,
        Vector2 direction,
        Quaternion rotation,
        int damageAmount,
        float chanceToPierce,
        int pierceCount)
    {
        GameObject bulletObj = null;
        if (PoolManager.Instance != null && rifleProjectilePrefab != null)
        {
            bulletObj = PoolManager.Instance.Spawn(rifleProjectilePrefab, position, rotation);
        }
        else if (rifleProjectilePrefab != null)
        {
            bulletObj = Instantiate(rifleProjectilePrefab, position, rotation);
        }

        if (bulletObj != null)
        {
            RifleProjectile rifleProj = bulletObj.GetComponent<RifleProjectile>();
            if (rifleProj == null)
            {
                rifleProj = bulletObj.AddComponent<RifleProjectile>();
            }

            rifleProj.Setup(
                damageAmount,
                bulletSpeed,
                attackRange * 1.25f,
                chanceToPierce,
                pierceCount
            );
            rifleProj.SetDirection(direction);
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
                PoolManager.Instance.ReturnToPool(flash, 0.08f);
            }
        }
        else
        {
            flash = Instantiate(muzzleFlashPrefab, position, rotation);
            if (flash != null && flash.GetComponent<AutoDestroyVFX>() == null)
            {
                Destroy(flash, 0.08f);
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
        return Mathf.Max(0.04f, baseInterval / metaAttackSpeedMultiplier);
    }

    public float GetCalculatedPierceChance()
    {
        int index = Mathf.Clamp(currentLevel - 1, 0, levelConfigs.Length - 1);
        return levelConfigs[index].pierceChance;
    }

    public int GetCalculatedGuaranteedPierce()
    {
        int index = Mathf.Clamp(currentLevel - 1, 0, levelConfigs.Length - 1);
        return levelConfigs[index].guaranteedPierceCount;
    }
}
