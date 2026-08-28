using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kỹ năng Shotgun (Súng Săn - Chipset ID 8).
/// Bắn ra chùm 5 mảnh đạn theo góc quạt (45° ở Cấp 1-2, 30° ở Cấp 3-5).
/// Quản lý 5 cấp độ in-game (Xuyên thấu, Đẩy lùi Knockback, Bắn đúp 2 lần) và nhận buff Khung Meta từ PlayerDataService (ID 8).
/// </summary>
public class ShotgunSkill : MonoBehaviour
{
    [System.Serializable]
    public struct ShotgunLevelConfig
    {
        public int totalDamage;
        public float fireInterval;
        public float spreadAngle;
        public bool hasPiercing;
        public bool hasKnockback;
        public bool hasDoubleTap;
    }

    [Header("Skill Status")]
    [Tooltip("Trạng thái mở khóa của kỹ năng.")]
    [SerializeField] private bool isUnlocked = false;

    [Tooltip("Cấp độ kỹ năng hiện tại trong trận đấu (1 -> 5).")]
    [SerializeField, Range(1, 5)] private int currentLevel = 1;

    [Header("5 Level Progression Configuration (Tùy chỉnh trong Inspector)")]
    [SerializeField]
    private ShotgunLevelConfig[] levelConfigs = new ShotgunLevelConfig[]
    {
        new ShotgunLevelConfig { totalDamage = 86, fireInterval = 1.5f, spreadAngle = 45f, hasPiercing = false, hasKnockback = false, hasDoubleTap = false },
        new ShotgunLevelConfig { totalDamage = 105, fireInterval = 1.3f, spreadAngle = 45f, hasPiercing = false, hasKnockback = false, hasDoubleTap = false },
        new ShotgunLevelConfig { totalDamage = 130, fireInterval = 1.1f, spreadAngle = 30f, hasPiercing = true, hasKnockback = false, hasDoubleTap = false },
        new ShotgunLevelConfig { totalDamage = 160, fireInterval = 0.9f, spreadAngle = 30f, hasPiercing = true, hasKnockback = true, hasDoubleTap = false },
        new ShotgunLevelConfig { totalDamage = 210, fireInterval = 0.7f, spreadAngle = 30f, hasPiercing = true, hasKnockback = true, hasDoubleTap = true }
    };

    [Header("Shooting & Range")]
    [SerializeField] private float attackRange = 9.0f;
    [SerializeField] private float bulletSpeed = 16f;
    [SerializeField] private int pelletsPerShot = 5;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Prefab References")]
    [SerializeField] private GameObject shotgunProjectilePrefab;
    [SerializeField] private GameObject muzzleFlashPrefab;

    private Transform playerTransform;
    private PlayerHealth playerHealth;
    private Transform attackPoint;
    private float nextFireTime;
    private readonly Collider2D[] enemyBuffer = new Collider2D[32];
    private ContactFilter2D contactFilter;

    // Meta Tier Bonuses
    private float metaDamageMultiplier = 1.0f;
    private bool metaPenetration = false;
    private bool metaDoubleTap = false;

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
        if (shotgunProjectilePrefab == null)
        {
            shotgunProjectilePrefab = Resources.Load<GameObject>("Prefabs/Projectile");
#if UNITY_EDITOR
            if (shotgunProjectilePrefab == null)
            {
                shotgunProjectilePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectile.prefab");
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
    /// Đọc cấp bậc Khung Thẻ Chipset Meta (Chip ID 8) từ PlayerDataService:
    /// - Tier 1 (Magic): Gây sát thương lớn cự ly gần.
    /// - Tier 2 (Rare): ATK +15%
    /// - Tier 3 (Unique): ATK +15% (Tổng +30%)
    /// - Tier 4 (Epic): Adds Penetration Skill
    /// - Tier 5 (Holographic): Fires two times in a row (Bắn 2 lần liên tiếp)
    /// </summary>
    public void LoadMetaTierBonuses()
    {
        ChipTier tier = PlayerDataService.GetChipTier(8);

        metaDamageMultiplier = 1.0f;
        metaPenetration = false;
        metaDoubleTap = false;

        if (tier >= ChipTier.Rare)
        {
            metaDamageMultiplier += 0.15f; // ATK +15%
        }
        if (tier >= ChipTier.Unique)
        {
            metaDamageMultiplier += 0.15f; // ATK +15% (Tổng +30%)
        }
        if (tier >= ChipTier.Epic)
        {
            metaPenetration = true; // Adds Penetration Skill
        }
        if (tier == ChipTier.Holographic)
        {
            metaDoubleTap = true; // Fires two times in a row
        }
    }

    /// <summary>
    /// Mở khóa hoặc nâng cấp kỹ năng Shotgun trong trận đấu (Cấp 1 -> 5).
    /// </summary>
    public void UnlockOrUpgrade(int targetLevel)
    {
        isUnlocked = true;
        currentLevel = Mathf.Clamp(targetLevel, 1, 5);
        LoadMetaTierBonuses();
        Debug.Log($"[ShotgunSkill] Shotgun đã lên Cấp {currentLevel}! (Total Dmg: {GetCalculatedTotalDamage()}, Cooldown: {GetCalculatedFireInterval():F2}s, Spread: {GetSpreadAngle()}°)");
    }

    private void Update()
    {
        if (!isUnlocked) return;
        if (playerHealth != null && playerHealth.IsDead) return;

        if (Time.time < nextFireTime) return;

        Transform target = FindNearestEnemy();
        if (target == null) return;

        FireShotgun(target);
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

    private void FireShotgun(Transform target)
    {
        Vector3 baseSpawnPos = attackPoint != null ? attackPoint.position : playerTransform.position;
        Vector2 baseDir = ((Vector2)target.position - (Vector2)baseSpawnPos).normalized;

        // Phát bắn đầu tiên
        FirePelletFan(baseSpawnPos, baseDir);

        // Bắn đúp (Double Tap ở Cấp 5 hoặc Meta Tier 5): xả phát thứ 2 sau 0.08s
        if (currentLevel >= 5 || metaDoubleTap)
        {
            StartCoroutine(DoubleTapRoutine(target));
        }
    }

    private IEnumerator DoubleTapRoutine(Transform target)
    {
        yield return new WaitForSeconds(0.08f);

        if (target != null && target.gameObject.activeInHierarchy)
        {
            Vector3 spawnPos = attackPoint != null ? attackPoint.position : playerTransform.position;
            Vector2 dir = ((Vector2)target.position - (Vector2)spawnPos).normalized;
            FirePelletFan(spawnPos, dir);
        }
        else
        {
            Transform newTarget = FindNearestEnemy();
            if (newTarget != null)
            {
                Vector3 spawnPos = attackPoint != null ? attackPoint.position : playerTransform.position;
                Vector2 dir = ((Vector2)newTarget.position - (Vector2)spawnPos).normalized;
                FirePelletFan(spawnPos, dir);
            }
        }
    }

    private void FirePelletFan(Vector3 spawnPos, Vector2 baseDirection)
    {
        float baseAngle = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg;

        // Muzzle Flash VFX
        SpawnMuzzleFlash(spawnPos, Quaternion.Euler(0f, 0f, baseAngle));

        float spreadAngle = GetSpreadAngle();
        float angleStep = pelletsPerShot > 1 ? spreadAngle / (pelletsPerShot - 1) : 0f;
        float startAngle = baseAngle - (spreadAngle * 0.5f);

        int damagePerPellet = GetCalculatedDamagePerPellet();
        bool piercing = currentLevel >= 3 || metaPenetration;
        float knockback = currentLevel >= 4 ? 12.0f : 0f;

        for (int i = 0; i < pelletsPerShot; i++)
        {
            float currentAngle = startAngle + (i * angleStep);
            float rad = currentAngle * Mathf.Deg2Rad;
            Vector2 pelletDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
            Quaternion rot = Quaternion.Euler(0f, 0f, currentAngle);

            SpawnSinglePellet(spawnPos, pelletDir, rot, damagePerPellet, piercing, knockback);
        }
    }

    private void SpawnSinglePellet(
        Vector3 position,
        Vector2 direction,
        Quaternion rotation,
        int damageAmount,
        bool piercing,
        float knockback)
    {
        GameObject pelletObj = null;
        if (PoolManager.Instance != null && shotgunProjectilePrefab != null)
        {
            pelletObj = PoolManager.Instance.Spawn(shotgunProjectilePrefab, position, rotation);
        }
        else if (shotgunProjectilePrefab != null)
        {
            pelletObj = Instantiate(shotgunProjectilePrefab, position, rotation);
        }

        if (pelletObj != null)
        {
            ShotgunProjectile shotgunProj = pelletObj.GetComponent<ShotgunProjectile>();
            if (shotgunProj == null)
            {
                shotgunProj = pelletObj.AddComponent<ShotgunProjectile>();
            }

            shotgunProj.Setup(
                damageAmount,
                bulletSpeed,
                attackRange * 1.1f,
                piercing,
                knockback
            );
            shotgunProj.SetDirection(direction);
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
                PoolManager.Instance.ReturnToPool(flash, 0.10f);
            }
        }
        else
        {
            flash = Instantiate(muzzleFlashPrefab, position, rotation);
            if (flash != null && flash.GetComponent<AutoDestroyVFX>() == null)
            {
                Destroy(flash, 0.10f);
            }
        }
    }

    // --- TÍNH TOÁN CHỈ SỐ THEO CẤP ĐỘ VÀ META ---

    public int GetCalculatedTotalDamage()
    {
        int index = Mathf.Clamp(currentLevel - 1, 0, levelConfigs.Length - 1);
        int baseTotalDmg = levelConfigs[index].totalDamage;
        return Mathf.RoundToInt(baseTotalDmg * metaDamageMultiplier);
    }

    public int GetCalculatedDamagePerPellet()
    {
        return Mathf.Max(1, Mathf.RoundToInt((float)GetCalculatedTotalDamage() / pelletsPerShot));
    }

    public float GetCalculatedFireInterval()
    {
        int index = Mathf.Clamp(currentLevel - 1, 0, levelConfigs.Length - 1);
        return levelConfigs[index].fireInterval;
    }

    public float GetSpreadAngle()
    {
        int index = Mathf.Clamp(currentLevel - 1, 0, levelConfigs.Length - 1);
        return levelConfigs[index].spreadAngle;
    }
}
