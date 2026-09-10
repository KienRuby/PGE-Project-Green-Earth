using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lớp cơ sở cho các Companion Drone tham gia chiến đấu trong trận đấu (GamePlay).
/// Quản lý việc bay lượn bồng bềnh xung quanh người chơi (Player),
/// phân chia vị trí góc theo số lượng slot trang bị, và tìm kiếm mục tiêu quái vật.
/// </summary>
public abstract class BuddyCombatDrone : MonoBehaviour
{
    [Header("Drone Identity")]
    [SerializeField] private int buddyId;
    [SerializeField] private string droneName = "Buddy Drone";

    [Header("Formation & Follow Movement")]
    [Tooltip("Khoảng cách giữ cự ly với người chơi (mét).")]
    [SerializeField] protected float followDistance = 0.85f;

    [Tooltip("Bán kính vùng an toàn tối thiểu, tuyệt đối không cho phép drone đè vào Player (mét).")]
    [SerializeField] protected float minPlayerDistance = 0.72f;

    [Tooltip("Tốc độ bay theo người chơi (m/s).")]
    [SerializeField] protected float followSpeed = 16.0f;

    [Tooltip("Biên độ bồng bềnh lên xuống (Sinusoidal Bobbing).")]
    [SerializeField] protected float bobbingAmplitude = 0.05f;

    [Tooltip("Tần số bồng bềnh (chu kỳ/giây).")]
    [SerializeField] protected float bobbingFrequency = 2.5f;

    [Tooltip("Góc nghiêng tối đa khi di chuyển (độ).")]
    [SerializeField] protected float tiltAmount = 15f;

    [Header("Combat Targeting")]
    [Tooltip("Bán kính phát hiện kẻ thù (mét).")]
    [SerializeField] protected float targetDetectionRadius = 9.0f;

    [Tooltip("Tần suất quét mục tiêu (giây).")]
    [SerializeField] protected float targetRefreshInterval = 0.2f;

    [Tooltip("Layer chứa quái vật.")]
    [SerializeField] protected LayerMask enemyLayer;

    [Header("Combat Stats")]
    [SerializeField] protected int baseDamage = 25;
    [SerializeField] protected float attackCooldown = 1.2f;

    [Header("Drone Appearance & Scale")]
    [Tooltip("Kích cỡ drone khi vào trận đấu (tương đương kích cỡ nắm đấm RocketPunch).")]
    [Range(0.05f, 0.5f)]
    [SerializeField] protected float combatScale = 0.12f;

    [Header("Visual Components")]
    [SerializeField] protected SpriteRenderer spriteRenderer;
    [SerializeField] protected Transform firePoint;

    // Runtime state
    protected Transform playerTransform;
    protected int slotIndex;
    protected int totalSlots = 1;
    protected float slotAngleOffset;
    protected EnemyHealth currentTarget;
    protected float attackTimer;
    protected float targetScanTimer;
    protected float bobbingTime;
    protected Vector3 currentVelocity;
    protected int currentLevel = 1;
    protected BuddyTier currentTier = BuddyTier.Common;

    public int BuddyId => buddyId;
    public string DroneName => droneName;
    public float CombatScale
    {
        get => combatScale;
        set
        {
            combatScale = Mathf.Max(0.01f, value);
            transform.localScale = new Vector3(combatScale, combatScale, 1f);
        }
    }
    public Transform FirePoint => firePoint != null ? firePoint : transform;

    public virtual void Initialize(Transform targetPlayer, int slotIdx, int totalEquipped, int level = 1, BuddyTier tier = BuddyTier.Common)
    {
        playerTransform = targetPlayer;
        slotIndex = slotIdx;
        totalSlots = Mathf.Max(1, totalEquipped);
        currentLevel = Mathf.Max(1, level);
        currentTier = tier;

        // Tự động điều chỉnh kích cỡ nhỏ gọn tương đương nắm đấm RocketPunch
        transform.localScale = new Vector3(combatScale, combatScale, 1f);

        // Đảm bảo cự ly hợp lý, không quá xa nhưng tuyệt đối không đè vào người chơi
        if (followDistance < 0.75f || followDistance > 1.2f)
        {
            followDistance = 0.85f;
        }

        // Phân bổ góc hình quạt quanh Player
        CalculateSlotOffset();

        // Đặt vị trí ban đầu ngay cạnh Player
        if (playerTransform != null)
        {
            transform.position = CalculateTargetPosition();
        }

        // Tự động gán enemyLayer nếu chưa thiết lập (Layer 7: Enemy / 128)
        if (enemyLayer.value == 0)
        {
            enemyLayer = 1 << 7; // Thường là layer Enemy (128)
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        ApplyLevelAndTierScaling();
    }

    protected virtual void Awake()
    {
        transform.localScale = new Vector3(combatScale, combatScale, 1f);
        if (followDistance < 0.75f || followDistance > 1.2f)
        {
            followDistance = 0.85f;
        }
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    protected virtual void Update()
    {
        if (playerTransform == null)
        {
            FindPlayerIfMissing();
            if (playerTransform == null) return;
        }

        UpdateMovement(Time.deltaTime);
        UpdateTargeting(Time.deltaTime);
        UpdateCombat(Time.deltaTime);
    }

    private void FindPlayerIfMissing()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            CalculateSlotOffset();
        }
    }

    protected virtual void CalculateSlotOffset()
    {
        // Phân bố góc hình vòng cung bao quanh vai và đầu Player
        if (totalSlots <= 1)
        {
            slotAngleOffset = 45f * Mathf.Deg2Rad; // Phía trên vai phải
        }
        else if (totalSlots == 2)
        {
            // 2 bên vai: 135° (vai trái) và 45° (vai phải)
            float angle = (slotIndex == 0) ? 135f : 45f;
            slotAngleOffset = angle * Mathf.Deg2Rad;
        }
        else if (totalSlots == 3)
        {
            // 3 vị trí: 150° (trái), 90° (đỉnh đầu), 30° (phải)
            float[] angles = { 150f, 90f, 30f };
            slotAngleOffset = angles[Mathf.Clamp(slotIndex, 0, 2)] * Mathf.Deg2Rad;
        }
        else if (totalSlots == 4)
        {
            // 4 vị trí phân bố đều 2 bên
            float[] angles = { 160f, 115f, 65f, 20f };
            slotAngleOffset = angles[Mathf.Clamp(slotIndex, 0, 3)] * Mathf.Deg2Rad;
        }
        else
        {
            // 5 vị trí: xòe hình quạt rộng từ 165° (sát sườn trái) sang 15° (sát sườn phải)
            float startAngle = 165f;
            float endAngle = 15f;
            float step = (endAngle - startAngle) / Mathf.Max(1, totalSlots - 1);
            float angleDeg = startAngle + step * slotIndex;
            slotAngleOffset = angleDeg * Mathf.Deg2Rad;
        }
    }

    protected virtual Vector3 CalculateTargetPosition()
    {
        if (playerTransform == null) return transform.position;

        // Tâm người chơi (nhích lên 0.15m để căn đúng phần ngực/vai)
        Vector3 playerCenter = playerTransform.position + new Vector3(0f, 0.15f, 0f);

        Vector3 baseOffset = new Vector3(
            Mathf.Cos(slotAngleOffset) * followDistance,
            Mathf.Sin(slotAngleOffset) * followDistance,
            0f
        );

        // Thêm độ bồng bềnh sine wave nhẹ nhàng
        bobbingTime += Time.deltaTime * bobbingFrequency;
        float bobOffset = Mathf.Sin(bobbingTime + slotIndex * 1.5f) * bobbingAmplitude;

        return playerCenter + baseOffset + new Vector3(0f, bobOffset, 0f);
    }

    protected virtual void UpdateMovement(float deltaTime)
    {
        Vector3 targetPos = CalculateTargetPosition();
        Vector3 prevPos = transform.position;

        // 1. Bay mượt tới vị trí mục tiêu (smoothTime 0.08s nhanh nhạy, không trễ vào người chơi)
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref currentVelocity, 0.08f, followSpeed, deltaTime);

        // 2. CƠ CHẾ VÙNG AN TOÀN (ANTI-OVERLAP): TUYỆT ĐỐI KHÔNG ĐÈ LÊN BODY PLAYER
        if (playerTransform != null)
        {
            Vector3 playerCenter = playerTransform.position + new Vector3(0f, 0.15f, 0f);
            Vector3 toDrone = transform.position - playerCenter;
            toDrone.z = 0f;
            float dist = toDrone.magnitude;
            if (dist < minPlayerDistance)
            {
                Vector3 pushDir = dist > 0.001f ? toDrone.normalized : Vector3.up;
                transform.position = playerCenter + pushDir * minPlayerDistance;
                // Triệt tiêu vận tốc hướng vào player để tránh rung giật
                if (Vector3.Dot(currentVelocity, pushDir) < 0f)
                {
                    currentVelocity = Vector3.ProjectOnPlane(currentVelocity, pushDir);
                }
            }
        }

        // 3. TÁCH GIÃN KHOẢNG CÁCH GIỮA CÁC DRONE (ANTI-CLUMPING)
        if (BuddyCombatManager.Instance != null)
        {
            var drones = BuddyCombatManager.Instance.ActiveDrones;
            if (drones != null)
            {
                for (int i = 0; i < drones.Count; i++)
                {
                    BuddyCombatDrone other = drones[i];
                    if (other != null && other != this)
                    {
                        Vector3 diff = transform.position - other.transform.position;
                        diff.z = 0f;
                        float d = diff.magnitude;
                        if (d > 0.0001f && d < 0.35f)
                        {
                            transform.position += diff.normalized * ((0.35f - d) * 0.4f);
                        }
                    }
                }
            }
        }

        // 4. Hiệu ứng nghiêng (Tilt) theo hướng di chuyển ngang
        float horizontalVel = (transform.position.x - prevPos.x) / Mathf.Max(0.001f, deltaTime);
        float targetTilt = Mathf.Clamp(-horizontalVel * 3f, -tiltAmount, tiltAmount);
        Quaternion desiredRot = Quaternion.Euler(0f, 0f, targetTilt);
        transform.rotation = Quaternion.Lerp(transform.rotation, desiredRot, deltaTime * 8f);

        // 5. Lật mặt Sprite (FlipX) theo hướng nhìn hoặc vị trí mục tiêu
        if (currentTarget != null && currentTarget.gameObject.activeInHierarchy)
        {
            bool lookRight = currentTarget.transform.position.x >= transform.position.x;
            if (spriteRenderer != null) spriteRenderer.flipX = !lookRight;
        }
        else if (Mathf.Abs(horizontalVel) > 0.2f)
        {
            bool moveRight = horizontalVel > 0f;
            if (spriteRenderer != null) spriteRenderer.flipX = !moveRight;
        }
    }

    protected virtual void UpdateTargeting(float deltaTime)
    {
        targetScanTimer -= deltaTime;
        if (targetScanTimer > 0f) return;
        targetScanTimer = targetRefreshInterval;

        // Kiểm tra target hiện tại còn hợp lệ không
        if (currentTarget != null)
        {
            if (!currentTarget.gameObject.activeInHierarchy || currentTarget.IsDead ||
                Vector2.Distance(transform.position, currentTarget.transform.position) > targetDetectionRadius * 1.3f)
            {
                currentTarget = null;
            }
        }

        if (currentTarget == null)
        {
            currentTarget = FindBestTarget();
        }
    }

    protected virtual EnemyHealth FindBestTarget()
    {
        Vector2 scanCenter = transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(scanCenter, targetDetectionRadius, enemyLayer);
        if (hits == null || hits.Length == 0)
        {
            GameObject[] creeps = GameObject.FindGameObjectsWithTag("Enemy");
            EnemyHealth bestFallback = null;
            float minFallbackDist = float.MaxValue;
            foreach (var go in creeps)
            {
                if (go == null || !go.activeInHierarchy) continue;
                EnemyHealth eh = go.GetComponent<EnemyHealth>() ?? go.GetComponentInParent<EnemyHealth>();
                if (eh == null || eh.IsDead) continue;
                float d = Vector2.Distance(scanCenter, go.transform.position);
                if (d <= targetDetectionRadius && d < minFallbackDist)
                {
                    minFallbackDist = d;
                    bestFallback = eh;
                }
            }
            return bestFallback;
        }

        EnemyHealth nearestEnemy = null;
        float minDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null) continue;
            EnemyHealth health = hits[i].GetComponent<EnemyHealth>() ?? hits[i].GetComponentInParent<EnemyHealth>();
            if (health == null || health.IsDead || !health.gameObject.activeInHierarchy) continue;

            float dist = Vector2.Distance(scanCenter, health.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearestEnemy = health;
            }
        }

        return nearestEnemy;
    }

    protected virtual void UpdateCombat(float deltaTime)
    {
        attackTimer -= deltaTime;
        if (attackTimer <= 0f && currentTarget != null)
        {
            ExecuteAttack(currentTarget);
            attackTimer = attackCooldown;
        }
    }

    /// <summary>
    /// Hành động tấn công riêng biệt của từng loại Drone.
    /// </summary>
    protected abstract void ExecuteAttack(EnemyHealth target);

    protected virtual void ApplyLevelAndTierScaling()
    {
        float levelMultiplier = 1f + (currentLevel - 1) * 0.15f;
        float tierMultiplier = 1f + (int)currentTier * 0.2f;

        baseDamage = Mathf.RoundToInt(baseDamage * levelMultiplier * tierMultiplier);
        attackCooldown = Mathf.Max(0.25f, attackCooldown * (1f - (int)currentTier * 0.05f));
    }
}
