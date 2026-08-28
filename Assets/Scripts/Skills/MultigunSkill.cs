using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kỹ năng Multigun (Súng Đa Tia - Chipset ID 5).
/// Bắn ra cơn mưa đạn nhiều hướng cùng lúc nhắm vào quái vật.
/// Quản lý 5 cấp độ in-game (4 -> 13 tia, hồi chiêu 1.0s -> 0.2s, Homing ở Cấp 3+, Cơn bão đạn 360° ở Cấp 5)
/// và nhận buff Khung Meta từ PlayerDataService (ID 5) (+1 -> +9 tia đạn).
/// </summary>
public class MultigunSkill : MonoBehaviour
{
    [System.Serializable]
    public struct MultigunLevelConfig
    {
        public int damage;
        public float fireInterval;
        public int shellCount;
        public bool isHoming;
        public bool is360Storm;
    }

    [Header("Skill Status")]
    [Tooltip("Trạng thái mở khóa của kỹ năng.")]
    [SerializeField] private bool isUnlocked = false;

    [Tooltip("Cấp độ kỹ năng hiện tại trong trận đấu (1 -> 5).")]
    [SerializeField, Range(1, 5)] private int currentLevel = 1;

    [Header("5 Level Progression Configuration (Tùy chỉnh trong Inspector)")]
    [SerializeField]
    private MultigunLevelConfig[] levelConfigs = new MultigunLevelConfig[]
    {
        new MultigunLevelConfig { damage = 19, fireInterval = 1.0f, shellCount = 4, isHoming = false, is360Storm = false },
        new MultigunLevelConfig { damage = 25, fireInterval = 0.8f, shellCount = 5, isHoming = false, is360Storm = false },
        new MultigunLevelConfig { damage = 35, fireInterval = 0.6f, shellCount = 6, isHoming = true, is360Storm = false },
        new MultigunLevelConfig { damage = 50, fireInterval = 0.4f, shellCount = 9, isHoming = true, is360Storm = false },
        new MultigunLevelConfig { damage = 70, fireInterval = 0.2f, shellCount = 13, isHoming = true, is360Storm = true }
    };

    [Header("Shooting Settings")]
    [SerializeField] private float attackRange = 12f;
    [SerializeField] private float bulletSpeed = 16f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Prefab References")]
    [SerializeField] private GameObject multigunProjectilePrefab;
    [SerializeField] private GameObject muzzleFlashPrefab;

    private Transform playerTransform;
    private PlayerHealth playerHealth;
    private Transform attackPoint;
    private float nextFireTime;
    private readonly Collider2D[] enemyBuffer = new Collider2D[32];
    private ContactFilter2D contactFilter;

    // Meta Tier Bonuses
    private int metaShellsBonus = 0;

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
        if (multigunProjectilePrefab == null)
        {
            multigunProjectilePrefab = Resources.Load<GameObject>("Prefabs/Projectile");
#if UNITY_EDITOR
            if (multigunProjectilePrefab == null)
            {
                multigunProjectilePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectile.prefab");
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
    /// Đọc cấp bậc Khung Thẻ Chipset Meta (Chip ID 5) từ PlayerDataService:
    /// - Tier 1 (Magic): ATK 19, Shells: 4, ATK Speed: Slow
    /// - Tier 2 (Rare): Adds +1 shells
    /// - Tier 3 (Unique): Adds +1 shells (Tổng +2 shells)
    /// - Tier 4 (Epic): Adds +3 shells (Tổng +5 shells)
    /// - Tier 5 (Holographic): Adds +4 shells (Tổng +9 shells)
    /// </summary>
    public void LoadMetaTierBonuses()
    {
        ChipTier tier = PlayerDataService.GetChipTier(5);

        metaShellsBonus = 0;

        if (tier >= ChipTier.Rare)
        {
            metaShellsBonus += 1; // +1 shell
        }
        if (tier >= ChipTier.Unique)
        {
            metaShellsBonus += 1; // +1 shell (Tổng +2)
        }
        if (tier >= ChipTier.Epic)
        {
            metaShellsBonus += 3; // +3 shells (Tổng +5)
        }
        if (tier == ChipTier.Holographic)
        {
            metaShellsBonus += 4; // +4 shells (Tổng +9)
        }
    }

    /// <summary>
    /// Mở khóa hoặc nâng cấp kỹ năng Multigun trong trận đấu (Cấp 1 -> 5).
    /// </summary>
    public void UnlockOrUpgrade(int targetLevel)
    {
        isUnlocked = true;
        currentLevel = Mathf.Clamp(targetLevel, 1, 5);
        LoadMetaTierBonuses();
        Debug.Log($"[MultigunSkill] Súng Đa Tia đã lên Cấp {currentLevel}! (Damage/Tia: {GetCalculatedDamage()}, Shells: {GetTotalShellCount()}, Fire Interval: {GetCalculatedFireInterval():F2}s, 360 Storm: {currentLevel >= 5})");
    }

    private void Update()
    {
        if (!isUnlocked) return;
        if (playerHealth != null && playerHealth.IsDead) return;

        if (Time.time < nextFireTime) return;

        Transform target = FindNearestEnemy();
        // Ở cấp 5 vẫn xả bão đạn 360 độ kể cả khi quái ở xa
        if (target == null && currentLevel < 5) return;

        FireMultigunBarrage(target);
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

    private void FireMultigunBarrage(Transform target)
    {
        Vector3 baseSpawnPos = attackPoint != null ? attackPoint.position : playerTransform.position;
        int totalShells = GetTotalShellCount();
        int damagePerBullet = GetCalculatedDamage();
        bool homingEnabled = currentLevel >= 3;

        // Muzzle Flash VFX
        SpawnMuzzleFlash(baseSpawnPos, Quaternion.identity);

        if (currentLevel >= 5)
        {
            // Cấp 5 (Tối thượng): Cơn bão đạn xả 360 độ quanh người
            float angleStep = 360f / totalShells;
            float randomOffset = Random.Range(0f, angleStep);

            for (int i = 0; i < totalShells; i++)
            {
                float angle = (i * angleStep) + randomOffset;
                float rad = angle * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
                Quaternion rot = Quaternion.Euler(0f, 0f, angle);

                SpawnSingleBullet(baseSpawnPos, dir, rot, damagePerBullet, homingEnabled);
            }
        }
        else
        {
            // Cấp 1-4: Xả mưa đạn theo hướng mục tiêu trong hình nón ngẫu nhiên
            Vector2 baseDir = Vector2.right;
            if (target != null)
            {
                baseDir = ((Vector2)target.position - (Vector2)baseSpawnPos).normalized;
            }
            float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
            float spreadCone = 50f;

            for (int i = 0; i < totalShells; i++)
            {
                float angle = baseAngle + Random.Range(-spreadCone * 0.5f, spreadCone * 0.5f);
                float rad = angle * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
                Quaternion rot = Quaternion.Euler(0f, 0f, angle);

                SpawnSingleBullet(baseSpawnPos, dir, rot, damagePerBullet, homingEnabled);
            }
        }
    }

    private void SpawnSingleBullet(
        Vector3 position,
        Vector2 direction,
        Quaternion rotation,
        int damageAmount,
        bool homing)
    {
        GameObject bulletObj = null;
        if (PoolManager.Instance != null && multigunProjectilePrefab != null)
        {
            bulletObj = PoolManager.Instance.Spawn(multigunProjectilePrefab, position, rotation);
        }
        else if (multigunProjectilePrefab != null)
        {
            bulletObj = Instantiate(multigunProjectilePrefab, position, rotation);
        }

        if (bulletObj != null)
        {
            MultigunProjectile proj = bulletObj.GetComponent<MultigunProjectile>();
            if (proj == null)
            {
                proj = bulletObj.AddComponent<MultigunProjectile>();
            }

            proj.Setup(damageAmount, bulletSpeed, attackRange * 1.2f, homing);
            proj.SetDirection(direction);
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
        return levelConfigs[index].damage;
    }

    public float GetCalculatedFireInterval()
    {
        int index = Mathf.Clamp(currentLevel - 1, 0, levelConfigs.Length - 1);
        return levelConfigs[index].fireInterval;
    }

    public int GetTotalShellCount()
    {
        int index = Mathf.Clamp(currentLevel - 1, 0, levelConfigs.Length - 1);
        int baseShells = levelConfigs[index].shellCount;
        return baseShells + metaShellsBonus;
    }
}
