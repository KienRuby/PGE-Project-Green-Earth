using System.Collections.Generic;
using UnityEngine;

public class PlayerAutoShooter : MonoBehaviour
{
    public enum DetectionShape
    {
        CameraViewport,
        CustomBox,
        Circle
    }

    [Header("Weapon Data System (Chọn súng từ Menu)")]
    [Tooltip("Vũ khí mặc định nếu chưa chọn từ Menu.")]
    [SerializeField] private WeaponData defaultWeapon;

    [Tooltip("Danh sách toàn bộ các loại súng trong game để tra cứu theo ID khi chọn ở Main Menu.")]
    [SerializeField] private List<WeaponData> availableWeapons = new List<WeaponData>();

    [Header("Target Detection Shape")]
    [Tooltip("Hình dạng phạm vi quét quái: CameraViewport (tự động đúng 100% tỷ lệ khung hình màn hình), CustomBox (chữ nhật tùy chỉnh), hoặc Circle (hình tròn).")]
    [SerializeField] private DetectionShape detectionShape = DetectionShape.CameraViewport;

    [Tooltip("Tham chiếu Camera chính (tự động lấy Camera.main nếu để trống).")]
    [SerializeField] private Camera targetCamera;

    [Tooltip("Tỷ lệ co giãn phạm vi quét so với khung hình Camera (1.0 = khớp chính xác 100% mép màn hình, 1.05 = quét chớm ra ngoài viền).")]
    [SerializeField, Range(0.5f, 2.0f)] private float viewportScale = 1.0f;

    [Tooltip("Kích thước khung chữ nhật tùy chỉnh (Rộng X, Cao Y) khi chọn chế độ CustomBox.")]
    [SerializeField] private Vector2 customBoxSize = new Vector2(10f, 16f);

    [Tooltip("Bán kính phát hiện khi chọn chế độ Circle.")]
    [SerializeField] private float detectionRadius = 12f;

    [Header("Target Layer & Cooldown")]
    [Tooltip("LayerMask chứa các Collider của quái vật để hệ thống phát hiện.")]
    [SerializeField] private LayerMask enemyLayer;

    [Tooltip("Khoảng thời gian giữa các lần quét tìm lại mục tiêu gần nhất (giây).")]
    [SerializeField] private float targetRefreshRate = 0.1f;

    [Header("Gun & Barrel (Nòng súng)")]
    [Tooltip("Transform trục xoay 360 độ của tay và súng (GunPivot). Tay cầm súng và GunSprite phải là con của object này.")]
    [SerializeField] private Transform gunTransform;

    [Tooltip("Transform phần thân của Player. GunPivot phải là object con để đổi bên cùng thân.")]
    [SerializeField] private Transform bodyTransform;

    [Tooltip("Transform nòng súng (AttackPoint / FirePoint) - vị trí chính xác viên đạn xuất hiện khi bắn.")]
    [SerializeField] private Transform attackPoint;

    [Tooltip("SpriteRenderer của khẩu súng (GunSprite).")]
    [SerializeField] private SpriteRenderer gunSpriteRenderer;

    [Tooltip("SpriteRenderer của thân và chân. Thân đổi hướng bằng Transform; chân được flip riêng vì không phải object con của thân.")]
    [SerializeField] private SpriteRenderer[] bodyRenderers;

    [Tooltip("Khoảng cách nòng súng tính từ tâm Player nếu nòng súng gắn trực tiếp trên Player.")]
    [SerializeField] private float attackPointDistance = 0.6f;

    [Tooltip("Tự động xoay súng theo hướng di chuyển khi không có quái vật xung quanh.")]
    [SerializeField] private bool aimMoveDirectionWhenIdle = true;

    [Header("Shooting Settings")]
    [Tooltip("Prefab viên đạn được bắn ra từ đầu nòng súng.")]
    [SerializeField] private GameObject projectilePrefab;

    [Tooltip("1. TỐC ĐỘ BẮN: Số phát bắn mỗi giây.")]
    [SerializeField] private float fireRate = 2f;

    [Tooltip("2. SÁT THƯƠNG: Lượng sát thương gây ra cho mỗi viên đạn.")]
    [SerializeField] private int currentDamage = 20;

    [Tooltip("3. TỐC ĐỘ RA ĐẠN: Vận tốc bay của viên đạn.")]
    [SerializeField] private float currentBulletSpeed = 12f;

    [Tooltip("4. KHOẢNG CÁCH BẮN: Tầm bắn tối đa của viên đạn trước khi tự hủy.")]
    [SerializeField] private float currentAttackRange = 12f;

    private int currentBulletsPerShot = 1;
    private float currentSpreadAngle = 15f;

    private Transform currentTarget;
    private PlayerMovement playerMovement;
    private PlayerHealth playerHealth;
    private float nextFireTime;
    private float targetSearchTimer;
    private WeaponData currentEquippedWeapon;
    private GatlingSpinner gatlingSpinner;
    private Vector3 gunTransformBaseScale = Vector3.one;
    private Vector3 bodyTransformBaseScale = Vector3.one;

    private int bonusDamage;
    private float bonusFireRate;
    private float bonusAttackRange;
    private float bonusBulletSpeed;
    private float critChance;

    // Buffer cố định để quét quái không sinh rác GC
    private readonly Collider2D[] enemyColliderBuffer = new Collider2D[64];
    private ContactFilter2D contactFilter;

    public WeaponData CurrentEquippedWeapon => currentEquippedWeapon;
    public bool IsAttacking { get; private set; }

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerHealth = GetComponent<PlayerHealth>();

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
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

        if (attackPoint == null && gunTransform != null)
        {
            attackPoint = gunTransform;
        }

        if (gunTransform != null)
        {
            gunTransformBaseScale = gunTransform.localScale;
        }

        if (bodyTransform == null && gunTransform != null)
        {
            bodyTransform = gunTransform.parent;
        }

        if (bodyTransform != null)
        {
            bodyTransformBaseScale = bodyTransform.localScale;
        }
    }

    private void Start()
    {
        LoadSelectedWeapon();
    }

    public void ApplyStatBonuses(int damageBonus, float fireRateBonus, float rangeBonus, float bulletSpeedBonus, float crit)
    {
        bonusDamage = damageBonus;
        bonusFireRate = fireRateBonus;
        bonusAttackRange = rangeBonus;
        bonusBulletSpeed = bulletSpeedBonus;
        critChance = crit;
    }

    /// <summary>
    /// Tự động đọc dữ liệu súng đã chọn từ Main Menu qua PlayerPrefs.
    /// </summary>
    public void LoadSelectedWeapon()
    {
        string selectedId = PlayerDataService.SelectedWeaponId;
        WeaponData toEquip = null;

        if (!string.IsNullOrEmpty(selectedId) && availableWeapons != null)
        {
            toEquip = availableWeapons.Find(w => w != null && w.weaponId == selectedId);
        }

        if (toEquip == null)
        {
            toEquip = defaultWeapon;
        }

        if (toEquip != null)
        {
            EquipWeapon(toEquip);
        }
    }

    /// <summary>
    /// Trang bị khẩu súng mới và áp dụng toàn bộ 4 chỉ số cốt lõi từ WeaponData.
    /// </summary>
    public void EquipWeapon(WeaponData weapon)
    {
        if (weapon == null) return;

        currentEquippedWeapon = weapon;

        // 1. Chỉ cập nhật Sprite. Kích thước súng được giữ theo Transform đã đặt trong Scene.
        if (gunSpriteRenderer != null)
        {
            if (weapon.gunSprite != null)
            {
                gunSpriteRenderer.sprite = weapon.gunSprite;
            }
        }

        // 2. Cập nhật vị trí nòng súng (FirePoint)
        if (attackPoint != null && weapon.firePointOffset != Vector2.zero)
        {
            attackPoint.localPosition = new Vector3(weapon.firePointOffset.x, weapon.firePointOffset.y, 0f);
        }

        // 3. Cập nhật Prefab đạn
        if (weapon.projectilePrefab != null)
        {
            projectilePrefab = weapon.projectilePrefab;
        }

        // 4. Cập nhật 4 Chỉ số cốt lõi: Tốc độ bắn, Khoảng cách bắn, Sát thương, Tốc độ ra đạn
        if (weapon.fireRate > 0f) fireRate = weapon.fireRate;
        if (weapon.damage > 0) currentDamage = weapon.damage;
        if (weapon.bulletSpeed > 0f) currentBulletSpeed = weapon.bulletSpeed;
        if (weapon.attackRange > 0f)
        {
            currentAttackRange = weapon.attackRange;
            detectionRadius = weapon.attackRange;
        }
        currentBulletsPerShot = Mathf.Max(1, weapon.bulletsPerShot);
        currentSpreadAngle = weapon.spreadAngle;
    }

    /// <summary>
    /// Trang bị súng theo ID (tiện lợi cho việc gọi từ Menu UI).
    /// </summary>
    public void EquipWeaponById(string weaponId)
    {
        if (availableWeapons == null) return;

        WeaponData found = availableWeapons.Find(w => w != null && w.weaponId == weaponId);
        if (found != null)
        {
            EquipWeapon(found);
        }
    }

    private void Update()
    {
        if (playerHealth != null && playerHealth.IsDead)
        {
            currentTarget = null;
            IsAttacking = false;
            if (gatlingSpinner != null)
            {
                gatlingSpinner.SetFiring(false);
            }
            return;
        }

        UpdateTarget();

        UpdateGunAndAttackPointRotation();

        AutoShoot();

        if (gatlingSpinner == null && gunSpriteRenderer != null)
        {
            gatlingSpinner = gunSpriteRenderer.GetComponent<GatlingSpinner>();
        }

        if (gatlingSpinner != null)
        {
            gatlingSpinner.SetFiring(currentTarget != null);
        }
    }

    // =====================================================
    // TÍNH TOÁN KÍCH THƯỚC PHẠM VI THEO KHUNG HÌNH CAMERA
    // =====================================================

    public Vector2 GetDetectionBoxSize()
    {
        if (detectionShape == DetectionShape.CameraViewport)
        {
            Camera cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam != null && cam.orthographic)
            {
                float height = cam.orthographicSize * 2f;
                float width = height * cam.aspect;
                return new Vector2(width, height) * viewportScale;
            }
            return customBoxSize;
        }

        if (detectionShape == DetectionShape.CustomBox)
        {
            return customBoxSize;
        }

        return new Vector2(detectionRadius * 2f, detectionRadius * 2f);
    }

    public Vector2 GetDetectionCenter()
    {
        if (detectionShape == DetectionShape.CameraViewport)
        {
            Camera cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam != null)
            {
                return (Vector2)cam.transform.position;
            }
        }

        return (Vector2)transform.position;
    }

    // =====================================================
    // TÌM ENEMY GẦN NHẤT THEO TỶ LỆ KHUNG HÌNH (NonAlloc)
    // =====================================================

    private void UpdateTarget()
    {
        targetSearchTimer -= Time.deltaTime;

        // Target hiện tại chết / bị Destroy / bị Disable
        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
        {
            currentTarget = null;
            targetSearchTimer = 0f;
        }

        if (targetSearchTimer > 0f)
            return;

        targetSearchTimer = targetRefreshRate;

        FindNearestEnemy();
    }

    private void FindNearestEnemy()
    {
        int hitCount = 0;
        Vector2 center = GetDetectionCenter();

        if (detectionShape == DetectionShape.Circle)
        {
            hitCount = Physics2D.OverlapCircle(
                center,
                detectionRadius + bonusAttackRange,
                contactFilter,
                enemyColliderBuffer
            );

            // Fallback nếu LayerMask chưa trúng
            if (hitCount == 0 && enemyLayer.value != 0)
            {
                ContactFilter2D fallbackFilter = new ContactFilter2D { useTriggers = true };
                hitCount = Physics2D.OverlapCircle(
                    center,
                    detectionRadius + bonusAttackRange,
                    fallbackFilter,
                    enemyColliderBuffer
                );
            }
        }
        else
        {
            Vector2 boxSize = GetDetectionBoxSize();
            hitCount = Physics2D.OverlapBox(
                center,
                boxSize,
                0f,
                contactFilter,
                enemyColliderBuffer
            );

            // Fallback nếu LayerMask chưa trúng
            if (hitCount == 0 && enemyLayer.value != 0)
            {
                ContactFilter2D fallbackFilter = new ContactFilter2D { useTriggers = true };
                hitCount = Physics2D.OverlapBox(
                    center,
                    boxSize,
                    0f,
                    fallbackFilter,
                    enemyColliderBuffer
                );
            }
        }

        Transform nearestEnemy = null;
        float nearestDistanceSqr = Mathf.Infinity;
        float effectiveRange = currentAttackRange + bonusAttackRange;
        float attackRangeSqr = effectiveRange * effectiveRange;
        Vector2 playerPosition = transform.position;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D enemyCollider = enemyColliderBuffer[i];
            if (enemyCollider == null) continue;

            EnemyHealth health = enemyCollider.GetComponentInParent<EnemyHealth>();
            if (health == null || health.IsDead || !health.gameObject.activeInHierarchy)
                continue;

            Vector2 difference = (Vector2)health.transform.position - playerPosition;
            float distanceSqr = difference.sqrMagnitude;

            if (distanceSqr > attackRangeSqr)
                continue;

            if (distanceSqr < nearestDistanceSqr)
            {
                nearestDistanceSqr = distanceSqr;
                nearestEnemy = health.transform;
            }
        }

        currentTarget = nearestEnemy;
    }

    // =====================================================
    // XOAY SÚNG & NÒNG SÚNG 360 ĐỘ KHÔNG GÓC CHẾT
    // =====================================================

    private void UpdateGunAndAttackPointRotation()
    {
        Vector2 aimDirection = Vector2.zero;

        if (currentTarget != null)
        {
            aimDirection = ((Vector2)currentTarget.position - (Vector2)transform.position).normalized;
        }
        else if (aimMoveDirectionWhenIdle && playerMovement != null && playerMovement.MoveDirection.sqrMagnitude > 0.01f)
        {
            aimDirection = playerMovement.MoveDirection.normalized;
        }
        else
        {
            return;
        }

        if (aimDirection == Vector2.zero)
            return;

        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;

        // 1. Xoay khẩu súng (GunPivot / GunTransform)
        if (gunTransform != null)
        {
            bool isAimingLeft = Mathf.Abs(angle) > 90f;
            SetBodyFacing(isAimingLeft);

            if (bodyTransform != null && gunTransform.IsChildOf(bodyTransform))
            {
                float localAimAngle = CalculateLocalAimAngle(angle, isAimingLeft);
                gunTransform.localRotation = Quaternion.Euler(0f, 0f, localAimAngle);
                gunTransform.localScale = gunTransformBaseScale;
            }
            else
            {
                // Fallback cho scene cũ chưa gắn GunPivot dưới thân.
                gunTransform.rotation = Quaternion.Euler(0f, 0f, angle);
                gunTransform.localScale = CalculateAimScale(angle, gunTransformBaseScale);
            }

            // Tắt flipY của SpriteRenderer để tránh xung đột lật 2 lần
            if (gunSpriteRenderer != null)
            {
                gunSpriteRenderer.flipY = false;
            }
        }

        // 2. Định vị Nòng súng nếu không dùng GunTransform
        if (attackPoint != null)
        {
            if (gunTransform == null || attackPoint.parent == transform)
            {
                attackPoint.position = transform.position + (Vector3)(aimDirection * attackPointDistance);
                attackPoint.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }
    }

    private static Vector3 CalculateAimScale(float angle, Vector3 baseScale)
    {
        Vector3 aimScale = baseScale;
        aimScale.y = Mathf.Abs(baseScale.y) * (Mathf.Abs(angle) > 90f ? -1f : 1f);
        return aimScale;
    }

    private static float CalculateLocalAimAngle(float worldAngle, bool isAimingLeft)
    {
        return isAimingLeft ? 180f - worldAngle : worldAngle;
    }

    private static Vector3 CalculateBodyScale(bool isAimingLeft, Vector3 baseScale)
    {
        Vector3 bodyScale = baseScale;
        bodyScale.x = Mathf.Abs(baseScale.x) * (isAimingLeft ? -1f : 1f);
        return bodyScale;
    }

    private void SetBodyFacing(bool isAimingLeft)
    {
        if (bodyTransform != null)
        {
            bodyTransform.localScale = CalculateBodyScale(
                isAimingLeft,
                bodyTransformBaseScale
            );
        }

        if (bodyRenderers == null)
            return;

        foreach (SpriteRenderer bodyRenderer in bodyRenderers)
        {
            if (bodyRenderer != null)
            {
                bool belongsToBodyTransform =
                    bodyTransform != null && bodyRenderer.transform == bodyTransform;

                bodyRenderer.flipX = !belongsToBodyTransform && isAimingLeft;
            }
        }
    }

    // =====================================================
    // BẮN ĐẠN TỪ NÒNG SÚNG (ĐẠN BAY THẲNG, ÁP DỤNG 4 CHỈ SỐ)
    // =====================================================

    private void AutoShoot()
    {
        // Giữ trạng thái Attack trong suốt thời gian súng tự động bắn mục tiêu.
        // Khoảng nghỉ giữa hai viên đạn vẫn thuộc cùng một chu kỳ Attack.
        IsAttacking = currentTarget != null && projectilePrefab != null;

        if (!IsAttacking)
            return;

        if (Time.time < nextFireTime)
            return;

        Shoot();

        float effectiveFireRate = Mathf.Max(0.1f, fireRate + bonusFireRate);
        nextFireTime = Time.time + (1f / effectiveFireRate);
    }

    private void OnDisable()
    {
        IsAttacking = false;
    }

    private void Shoot()
    {
        if (projectilePrefab == null)
            return;

        if (currentTarget == null)
            return;

        // Vị trí nòng súng (đạn luôn luôn xuất phát tại nòng súng)
        Vector3 spawnPosition = attackPoint != null ? attackPoint.position : transform.position;
        Vector2 baseDirection = ((Vector2)currentTarget.position - (Vector2)spawnPosition).normalized;
        float baseAngle = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg;

        if (currentBulletsPerShot <= 1)
        {
            SpawnSingleBullet(spawnPosition, baseDirection, baseAngle);
        }
        else
        {
            // Bắn tỏa nhiều viên (Shotgun)
            float startAngle = baseAngle - (currentSpreadAngle * 0.5f);
            float angleStep = currentSpreadAngle / (currentBulletsPerShot - 1);

            for (int i = 0; i < currentBulletsPerShot; i++)
            {
                float angle = startAngle + (i * angleStep);
                float rad = angle * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;

                SpawnSingleBullet(spawnPosition, dir, angle);
            }
        }
    }

    private void SpawnSingleBullet(Vector3 position, Vector2 direction, float angle)
    {
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
        GameObject projectileObj;

        if (PoolManager.Instance != null)
        {
            projectileObj = PoolManager.Instance.Spawn(projectilePrefab, position, rotation);
        }
        else
        {
            projectileObj = Instantiate(projectilePrefab, position, rotation);
        }

        if (projectileObj != null)
        {
            Projectile projectileScript = projectileObj.GetComponent<Projectile>();
            if (projectileScript != null)
            {
                int finalDamage = currentDamage + bonusDamage;
                if (critChance > 0f && Random.value < critChance)
                {
                    finalDamage = Mathf.RoundToInt(finalDamage * 1.5f);
                }
                float finalBulletSpeed = currentBulletSpeed + bonusBulletSpeed;
                float finalAttackRange = currentAttackRange + bonusAttackRange;

                // Cài đặt 4 chỉ số động từ khẩu súng đang trang bị kèm bonus nâng cấp Lab
                projectileScript.Setup(finalDamage, finalBulletSpeed, finalAttackRange);
                projectileScript.SetDirection(direction);
                projectileScript.SetTarget(currentTarget);
            }
        }
    }

    // =====================================================
    // DEBUG GIZMOS (HIỂN THỊ CHUẨN KHUNG HÌNH)
    // =====================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector2 center = GetDetectionCenter();

        if (detectionShape == DetectionShape.Circle)
        {
            Gizmos.DrawWireSphere(center, detectionRadius + bonusAttackRange);
        }
        else
        {
            Vector2 boxSize = GetDetectionBoxSize();
            Gizmos.DrawWireCube(center, new Vector3(boxSize.x, boxSize.y, 0f));
        }

        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, 0.1f);
            Gizmos.DrawRay(attackPoint.position, attackPoint.right * 1f);
        }
    }
}
