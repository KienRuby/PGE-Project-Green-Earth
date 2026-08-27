using System;
using UnityEngine;

/// <summary>
/// Component điều khiển thực thể Trụ Súng (GunTurret) xuất hiện trong bản đồ.
/// Tự động quét quái vật gần nhất trong phạm vi hình hộp (Width x Height tùy chỉnh),
/// xoay nòng 360 độ, bắn đạn và tự hủy sau thời gian tồn tại.
/// Hỗ trợ các đặc tính theo 5 cấp độ:
/// - Cấp 1..2: Bắn đạn đơn mục tiêu, tăng sát thương và thời gian đứng vững.
/// - Cấp 3: 30% tỉ lệ đạn nổ gây sát thương diện rộng.
/// - Cấp 4: Tự động hồi phục máu khi bị quái đánh.
/// - Cấp 5: Hỗ trợ phối hợp nhiều tháp cùng lúc.
/// </summary>
public class GunTurret : MonoBehaviour, IPoolable, IDamageable
{
    [Header("Hierarchy References")]
    [Tooltip("Trục xoay của nòng súng 360 độ (AimPivot).")]
    [SerializeField] private Transform aimPivot;

    [Tooltip("Điểm xuất hiện viên đạn (FirePoint).")]
    [SerializeField] private Transform firePoint;

    [Tooltip("SpriteRenderer nòng súng.")]
    [SerializeField] private SpriteRenderer gunSpriteRenderer;

    [Header("Projectile & VFX")]
    [Tooltip("Prefab viên đạn bắn ra từ nòng súng.")]
    [SerializeField] private GameObject projectilePrefab;

    [Tooltip("Prefab hiệu ứng tóe lửa nòng súng (Muzzle Flash).")]
    [SerializeField] private GameObject muzzleFlashPrefab;

    [Tooltip("Prefab hiệu ứng nổ đạn diện rộng (VFX Boom).")]
    [SerializeField] private GameObject explosionVfxPrefab;

    [Tooltip("Thời gian tồn tại của Muzzle Flash (giây).")]
    [SerializeField] private float muzzleFlashDuration = 0.1f;

    [Header("Combat Stats")]
    [Tooltip("Lượng sát thương của mỗi viên đạn.")]
    [SerializeField] private int damage = 27;

    [Tooltip("Tốc độ bắn: số phát mỗi giây.")]
    [SerializeField] private float fireRate = 3f;

    [Tooltip("Vận tốc bay của viên đạn (mét/giây).")]
    [SerializeField] private float bulletSpeed = 12f;

    [Tooltip("Thời gian tồn tại của trụ súng trước khi biến mất (giây).")]
    [SerializeField] private float duration = 10f;

    [Header("Detection Box (Phát hiện quái hình hộp tùy chỉnh)")]
    [Tooltip("Chiều rộng vùng phát hiện quái vật (Width - Trục X, mét).")]
    [SerializeField] private float detectionWidth = 6f;

    [Tooltip("Chiều cao vùng phát hiện quái vật (Height - Trục Y, mét).")]
    [SerializeField] private float detectionHeight = 10f;

    [Tooltip("Độ lệch tâm của vùng phát hiện so với trụ súng (Offset).")]
    [SerializeField] private Vector2 detectionOffset = Vector2.zero;

    [Header("Special Perks (Cấp 3 - Cấp 4)")]
    [Tooltip("Tỉ lệ đạn nổ diện rộng (0.3 = 30%).")]
    [SerializeField] private float explosiveBulletChance = 0f;

    [Tooltip("Bán kính nổ diện rộng (mét).")]
    [SerializeField] private float explosionRadius = 2.0f;

    [Tooltip("Máu tối đa của tháp súng.")]
    [SerializeField] private int maxHealth = 250;

    [Tooltip("Máu hiện tại của tháp súng.")]
    [SerializeField] private int currentHealth = 250;

    [Tooltip("Bật tính năng tháp tự động hồi máu khi bị quái đánh (Cấp 4+).")]
    [SerializeField] private bool hasHealthRegen = false;

    [Tooltip("Lượng máu tự hồi phục mỗi giây.")]
    [SerializeField] private float healthRegenPerSecond = 25f;

    [Header("Targeting & Layers")]
    [Tooltip("Layer chứa quái vật để phát hiện.")]
    [SerializeField] private LayerMask enemyLayer;

    [Tooltip("Khoảng thời gian giữa các lần quét tìm lại mục tiêu gần nhất (giây).")]
    [SerializeField] private float targetRefreshRate = 0.1f;

    private float durationTimer;
    private float nextFireTime;
    private float targetSearchTimer;
    private Transform currentTarget;
    private Action onDespawnCallback;
    private bool isInitialized;
    private float regenAccumulator;

    // Buffer cố định để quét quái không sinh rác GC
    private readonly Collider2D[] enemyColliderBuffer = new Collider2D[48];
    private ContactFilter2D contactFilter;

    public bool IsActive => durationTimer > 0f && currentHealth > 0;
    public float RemainingDuration => Mathf.Max(0f, durationTimer);
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    public float DetectionWidth
    {
        get => detectionWidth;
        set => detectionWidth = Mathf.Max(0.5f, value);
    }

    public float DetectionHeight
    {
        get => detectionHeight;
        set => detectionHeight = Mathf.Max(0.5f, value);
    }

    public Vector2 DetectionSize
    {
        get => new Vector2(detectionWidth, detectionHeight);
        set
        {
            detectionWidth = Mathf.Max(0.5f, value.x);
            detectionHeight = Mathf.Max(0.5f, value.y);
        }
    }

    public Vector2 DetectionOffset
    {
        get => detectionOffset;
        set => detectionOffset = value;
    }

    private void Awake()
    {
        if (aimPivot == null)
        {
            aimPivot = transform.Find("AimPivot");
        }

        if (firePoint == null && aimPivot != null)
        {
            firePoint = aimPivot.Find("FirePoint");
        }

        if (gunSpriteRenderer == null && aimPivot != null)
        {
            Transform gunTr = aimPivot.Find("GunSprite");
            if (gunTr != null) gunSpriteRenderer = gunTr.GetComponent<SpriteRenderer>();
        }

        if (enemyLayer.value == 0)
        {
            enemyLayer = LayerMask.GetMask("Enemy");
            if (enemyLayer.value == 0)
            {
                enemyLayer = 1 << 7; // Default Enemy layer fallback
            }
        }

        contactFilter = new ContactFilter2D
        {
            layerMask = enemyLayer,
            useLayerMask = enemyLayer.value != 0,
            useTriggers = true
        };
    }

    /// <summary>
    /// Khởi tạo toàn bộ chỉ số chi tiết theo bảng thiết kế 5 cấp độ và kích thước hình hộp.
    /// </summary>
    public void Initialize(
        int damageAmount,
        float fireRateValue,
        float bulletSpeedValue,
        float durationValue,
        float explosiveChanceValue,
        bool healthRegenEnabled,
        int healthAmount,
        GameObject bulletPrefab,
        GameObject boomVfx,
        Action onDespawn,
        float width = -1f,
        float height = -1f)
    {
        damage = damageAmount;
        fireRate = Mathf.Max(0.1f, fireRateValue);
        bulletSpeed = Mathf.Max(1f, bulletSpeedValue);
        duration = Mathf.Max(0.5f, durationValue);
        explosiveBulletChance = Mathf.Clamp01(explosiveChanceValue);
        hasHealthRegen = healthRegenEnabled;
        maxHealth = Mathf.Max(50, healthAmount);
        currentHealth = maxHealth;

        if (width > 0f) detectionWidth = width;
        if (height > 0f) detectionHeight = height;

        if (bulletPrefab != null) projectilePrefab = bulletPrefab;
        if (boomVfx != null) explosionVfxPrefab = boomVfx;
        onDespawnCallback = onDespawn;

        durationTimer = duration;
        nextFireTime = Time.time + (1f / fireRate);
        targetSearchTimer = 0f;
        currentTarget = null;
        regenAccumulator = 0f;
        isInitialized = true;
    }

    private void OnEnable()
    {
        if (!isInitialized)
        {
            durationTimer = duration;
            currentHealth = maxHealth;
            nextFireTime = Time.time;
        }
    }

    private void Update()
    {
        // 1. Đếm ngược thời gian tồn tại của trụ súng
        durationTimer -= Time.deltaTime;
        if (durationTimer <= 0f || currentHealth <= 0)
        {
            Despawn();
            return;
        }

        // 2. Hồi phục máu tự động nếu có kỹ năng hồi phục (Cấp 4+)
        if (hasHealthRegen && currentHealth < maxHealth)
        {
            regenAccumulator += healthRegenPerSecond * Time.deltaTime;
            if (regenAccumulator >= 1f)
            {
                int healAmount = Mathf.FloorToInt(regenAccumulator);
                regenAccumulator -= healAmount;
                currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
            }
        }

        // 3. Tìm kiếm mục tiêu gần nhất trong hình hộp
        UpdateTarget();

        // 4. Xoay nòng súng 360 độ về phía quái vật
        UpdateAimRotation();

        // 5. Tự động bắn khi có mục tiêu trong tầm
        AutoShoot();
    }

    private void UpdateTarget()
    {
        targetSearchTimer -= Time.deltaTime;

        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
        {
            currentTarget = null;
            targetSearchTimer = 0f;
        }
        else
        {
            // Kiểm tra xem mục tiêu hiện tại còn nằm trong phạm vi hình hộp không
            Vector2 boxCenter = (Vector2)transform.position + detectionOffset;
            Vector2 diff = (Vector2)currentTarget.position - boxCenter;
            if (Mathf.Abs(diff.x) > detectionWidth * 0.5f || Mathf.Abs(diff.y) > detectionHeight * 0.5f)
            {
                currentTarget = null;
                targetSearchTimer = 0f;
            }
        }

        if (targetSearchTimer > 0f) return;

        targetSearchTimer = targetRefreshRate;
        FindNearestEnemy();
    }

    private void FindNearestEnemy()
    {
        Vector2 center = (Vector2)transform.position + detectionOffset;
        Vector2 size = new Vector2(detectionWidth, detectionHeight);

        int hitCount = Physics2D.OverlapBox(center, size, 0f, contactFilter, enemyColliderBuffer);

        if (hitCount == 0 && enemyLayer.value != 0)
        {
            ContactFilter2D fallbackFilter = new ContactFilter2D { useTriggers = true };
            hitCount = Physics2D.OverlapBox(center, size, 0f, fallbackFilter, enemyColliderBuffer);
        }

        Transform nearestEnemy = null;
        float nearestDistanceSqr = Mathf.Infinity;
        Vector2 turretPos = transform.position;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = enemyColliderBuffer[i];
            if (col == null) continue;

            EnemyHealth health = col.GetComponentInParent<EnemyHealth>();
            if (health == null || health.IsDead || !health.gameObject.activeInHierarchy)
                continue;

            Vector2 diff = (Vector2)health.transform.position - turretPos;
            float distSqr = diff.sqrMagnitude;

            if (distSqr < nearestDistanceSqr)
            {
                nearestDistanceSqr = distSqr;
                nearestEnemy = health.transform;
            }
        }

        currentTarget = nearestEnemy;
    }

    private void UpdateAimRotation()
    {
        if (aimPivot == null || currentTarget == null) return;

        Vector2 aimDirection = ((Vector2)currentTarget.position - (Vector2)aimPivot.position).normalized;
        if (aimDirection.sqrMagnitude < 0.001f) return;

        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        aimPivot.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void AutoShoot()
    {
        if (currentTarget == null || projectilePrefab == null) return;

        if (Time.time < nextFireTime) return;

        Shoot();

        nextFireTime = Time.time + (1f / fireRate);
    }

    private void Shoot()
    {
        if (currentTarget == null || projectilePrefab == null) return;

        Vector3 spawnPos = firePoint != null ? firePoint.position : (aimPivot != null ? aimPivot.position : transform.position);
        Vector2 direction = ((Vector2)currentTarget.position - (Vector2)spawnPos).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);

        // Hiệu ứng nòng súng
        SpawnMuzzleFlash(spawnPos, rotation);

        // Sinh đạn
        GameObject projectileObj;
        if (PoolManager.Instance != null)
        {
            projectileObj = PoolManager.Instance.Spawn(projectilePrefab, spawnPos, rotation);
        }
        else
        {
            projectileObj = Instantiate(projectilePrefab, spawnPos, rotation);
        }

        if (projectileObj != null)
        {
            Projectile projectileScript = projectileObj.GetComponent<Projectile>();
            if (projectileScript != null)
            {
                float maxBulletRange = Mathf.Sqrt(detectionWidth * detectionWidth + detectionHeight * detectionHeight) + 2f;
                projectileScript.Setup(damage, bulletSpeed, maxBulletRange);
                projectileScript.SetDirection(direction);
                projectileScript.SetTarget(currentTarget);

                // Cấp 3+: 30% tỉ lệ đạn nổ gây sát thương diện rộng
                if (explosiveBulletChance > 0f && UnityEngine.Random.value < explosiveBulletChance)
                {
                    projectileScript.SetExplosive(true, explosionRadius, explosionVfxPrefab);
                }
            }
        }
    }

    private void SpawnMuzzleFlash(Vector3 position, Quaternion rotation)
    {
        if (muzzleFlashPrefab == null) return;

        Transform parentTransform = firePoint != null ? firePoint : transform;
        GameObject flashObj;

        if (PoolManager.Instance != null)
        {
            flashObj = PoolManager.Instance.Spawn(muzzleFlashPrefab, position, rotation, parentTransform);
            if (flashObj != null && flashObj.GetComponent<AutoDestroyVFX>() == null)
            {
                PoolManager.Instance.ReturnToPool(flashObj, muzzleFlashDuration);
            }
        }
        else
        {
            flashObj = Instantiate(muzzleFlashPrefab, position, rotation, parentTransform);
            if (flashObj != null && flashObj.GetComponent<AutoDestroyVFX>() == null)
            {
                Destroy(flashObj, muzzleFlashDuration);
            }
        }
    }

    /// <summary>
    /// Nhận sát thương khi quái đánh trúng tháp súng.
    /// Cấp 4+: Tự động hồi máu / kích hoạt hồi phục tức thì.
    /// </summary>
    public void TakeDamage(int damageAmount)
    {
        if (damageAmount <= 0 || currentHealth <= 0) return;

        int finalDamage = damageAmount;
        if (hasHealthRegen)
        {
            // Cấp 4: Giảm nhẹ sát thương nhận và hồi phục lại máu ngay khi bị đánh
            finalDamage = Mathf.Max(1, Mathf.RoundToInt(damageAmount * 0.7f));
            currentHealth = Mathf.Min(maxHealth, currentHealth + Mathf.RoundToInt(damageAmount * 0.4f));
        }

        currentHealth -= finalDamage;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Despawn();
        }
    }

    private void Despawn()
    {
        isInitialized = false;
        currentTarget = null;
        Action callback = onDespawnCallback;
        onDespawnCallback = null;

        callback?.Invoke();

        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.ReturnToPool(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void OnSpawnFromPool()
    {
        durationTimer = duration;
        currentHealth = maxHealth;
        currentTarget = null;
    }

    public void OnReturnToPool()
    {
        currentTarget = null;
        onDespawnCallback = null;
        isInitialized = false;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 boxCenter = transform.position + (Vector3)detectionOffset;
        Vector3 boxSize = new Vector3(detectionWidth, detectionHeight, 0f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(boxCenter, boxSize);

        Gizmos.color = new Color(0f, 1f, 1f, 0.12f);
        Gizmos.DrawCube(boxCenter, boxSize);

        if (firePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(firePoint.position, 0.1f);
        }
    }
}
