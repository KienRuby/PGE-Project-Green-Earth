using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BossMovement : MonoBehaviour, IPoolable
{
    public enum BossState
    {
        Chase,
        Windup,
        Dash,
        Recover
    }

    [Header("Movement & Chase")]
    [Tooltip("Tốc độ di chuyển cơ bản của Boss khi đuổi theo Player.")]
    [SerializeField] private float moveSpeed = 1.8f;

    [Tooltip("Khoảng cách dừng dự phòng khi Boss không có component BossRangedAttack.")]
    [SerializeField] private float stoppingDistance = 1.0f;

    [Tooltip("Khoảng đệm giữ thân Boss nằm trọn bên trong mép bản đồ.")]
    [Min(0f)]
    [SerializeField] private float mapBoundaryPadding = 1f;

    [Header("Facing / Flipping")]
    [Tooltip("Tự động lật hướng mặt (quay trái/phải) về phía Player.")]
    [SerializeField] private bool autoFlipFacing = true;

    [Tooltip("Hướng mặt mặc định của sprite gốc trong prefab (chọn true nếu sprite gốc vẽ hướng sang trái).")]
    [SerializeField] private bool initialFacingLeft = false;

    [Tooltip("Vùng đệm khoảng cách X để chống rung lắc khi đứng thẳng hàng dọc với Player.")]
    [SerializeField] private float flipDeadzone = 0.1f;

    [Header("Special Ability: Dash / Charge Attack")]
    [Tooltip("Bật kỹ năng lướt/húc đặc biệt của Boss.")]
    [SerializeField] private bool enableDashAttack = true;

    [Tooltip("Thời gian hồi chiêu giữa các lần lướt/húc (giây).")]
    [SerializeField] private float dashCooldown = 6.0f;

    [Tooltip("Thời gian Boss tích lực/chuẩn bị trước khi húc (giây) - Telegraph cho người chơi né.")]
    [SerializeField] private float dashWindupDuration = 0.6f;

    [Tooltip("Thời gian Boss lao nhanh trong cú húc (giây).")]
    [SerializeField] private float dashDuration = 0.5f;

    [Tooltip("Hệ số nhân tốc độ trong cú húc (so với tốc độ cơ bản).")]
    [SerializeField] private float dashSpeedMultiplier = 3.5f;

    [Tooltip("Thời gian Boss hồi sức sau khi húc xong (giây).")]
    [SerializeField] private float dashRecoverDuration = 0.4f;

    [Tooltip("Số lần húc liên tục trong 1 chuỗi khi bình thường.")]
    [Min(1)]
    [SerializeField] private int baseDashComboCount = 1;

    [Tooltip("Số lần húc liên tục trong 1 chuỗi khi Cuồng nộ (Enrage).")]
    [Min(1)]
    [SerializeField] private int enrageDashComboCount = 1;

    [Tooltip("Thời gian hồi chiêu Dash cố định khi Cuồng nộ (nếu > 0, ưu tiên hơn enrageCooldownMultiplier).")]
    [Min(0f)]
    [SerializeField] private float enrageDashCooldown = 0f;

    [Tooltip("Bật chế độ lướt xuyên qua Player (không bị chặn vật lý bởi collider của Player).")]
    [SerializeField] private bool dashPassThroughPlayer = true;

    [Tooltip("Sát thương gây ra khi lướt trúng Player (Lượng lớn sát thương). Mặc định 100.")]
    [SerializeField] private int dashDamage = 100;

    [Tooltip("Bán kính vùng quét trúng Player trong lúc lướt (nếu = 0 sẽ lấy theo collider của Boss).")]
    [SerializeField] private float dashDamageRadius = 1.2f;

    [Header("Enrage Phase (Cuồng nộ khi thấp máu)")]
    [Tooltip("Bật trạng thái cuồng nộ khi máu Boss xuống thấp.")]
    [SerializeField] private bool enableEnrage = true;

    [Tooltip("Ngưỡng phần trăm máu để kích hoạt cuồng nộ (0.4 = 40% máu).")]
    [Range(0.1f, 0.9f)]
    [SerializeField] private float enrageHealthPercent = 0.4f;

    [Tooltip("Hệ số tăng tốc độ di chuyển khi cuồng nộ.")]
    [SerializeField] private float enrageSpeedMultiplier = 1.35f;

    [Tooltip("Hệ số giảm thời gian hồi chiêu Dash khi cuồng nộ.")]
    [SerializeField] private float enrageCooldownMultiplier = 0.6f;

    [Header("Visual Feedback")]
    [Tooltip("Màu cảnh báo khi Boss tích lực chuẩn bị húc (Telegraph).")]
    [SerializeField] private Color windupColor = new Color(1f, 0.3f, 0.3f, 1f);

    [Tooltip("Màu khi Boss cuồng nộ.")]
    [SerializeField] private Color enrageColor = new Color(1f, 0.6f, 0.6f, 1f);

    private Rigidbody2D rb;
    private Transform player;
    private EnemyHealth health;
    private BossRangedAttack rangedAttack;
    private Animator animator;
    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;

    private BossState currentState = BossState.Chase;
    private float dashTimer;
    private float stateTimer;
    private int remainingDashesInCombo = 0;
    private Vector2 dashDirection;
    private float nextPlayerSearchTime;
    private Vector3 basePrefabScale = Vector3.one;
    private Vector3 initialScale;
    private bool isFacingRight = true;
    private bool isEnraged = false;
    private float baseMoveSpeed;
    private int currentAnimationHash;

    private static readonly int RunAnimationHash = Animator.StringToHash("Run");
    private static readonly int IdleAnimationHash = Animator.StringToHash("Idle");
    private int activeRunAnimationHash = RunAnimationHash;
    private int activeIdleAnimationHash = IdleAnimationHash;
    private ContactFilter2D obstacleFilter;
    private static readonly RaycastHit2D[] obstacleHitBuffer = new RaycastHit2D[8];
    [SerializeField] private float bodyCollisionRadius = 0.5f;
    private float currentBodyRadius = 0.5f;
    private bool hasDealtDashDamage = false;
    private float currentDashDamageMultiplier = 1.0f;

    public void SetScaleMultiplier(float multiplier)
    {
        if (multiplier <= 0f) return;
        if (basePrefabScale == Vector3.zero)
        {
            basePrefabScale = transform.localScale != Vector3.zero ? transform.localScale : Vector3.one;
        }
        initialScale = basePrefabScale * multiplier;
        currentBodyRadius = bodyCollisionRadius * multiplier;
        float sign = (isFacingRight ^ initialFacingLeft) ? 1f : -1f;
        transform.localScale = new Vector3(Mathf.Abs(initialScale.x) * sign, initialScale.y, initialScale.z);
    }

    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = value;
    }
    public float BaseMoveSpeed => baseMoveSpeed > 0 ? baseMoveSpeed : moveSpeed;

    public BossState CurrentState => currentState;
    public bool IsEnraged => isEnraged;
    public bool EnableDashAttack => enableDashAttack;
    public float DashSpeedMultiplier => dashSpeedMultiplier;
    public float DashCooldown => dashCooldown;
    public int BaseDashComboCount => baseDashComboCount;
    public int EnrageDashComboCount => enrageDashComboCount;
    public float EnrageDashCooldown => enrageDashCooldown;
    public float EnrageHealthPercent => enrageHealthPercent;

    public int DashDamage
    {
        get => dashDamage;
        set => dashDamage = value;
    }

    public bool DashPassThroughPlayer
    {
        get => dashPassThroughPlayer;
        set => dashPassThroughPlayer = value;
    }

    public void SetDashDamageMultiplier(float multiplier)
    {
        currentDashDamageMultiplier = Mathf.Max(0.1f, multiplier);
    }

    private void Awake()
    {
        baseMoveSpeed = moveSpeed;
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        basePrefabScale = transform.localScale != Vector3.zero ? transform.localScale : Vector3.one;
        initialScale = basePrefabScale;

        int obstacleLayer = LayerMask.GetMask("Obstacle");
        if (obstacleLayer == 0) obstacleLayer = LayerMask.GetMask("Default");
        obstacleFilter = new ContactFilter2D
        {
            useTriggers = false,
            layerMask = obstacleLayer,
            useLayerMask = true
        };

        health = GetComponent<EnemyHealth>();
        rangedAttack = GetComponent<BossRangedAttack>();
        animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();

        if (animator != null)
        {
            int walkHash = Animator.StringToHash("Walk");
            if (animator.HasState(0, walkHash) && !animator.HasState(0, RunAnimationHash))
            {
                activeRunAnimationHash = walkHash;
            }
            else
            {
                activeRunAnimationHash = RunAnimationHash;
            }

            if (!animator.HasState(0, IdleAnimationHash))
            {
                activeIdleAnimationHash = activeRunAnimationHash;
            }
            else
            {
                activeIdleAnimationHash = IdleAnimationHash;
            }
        }

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        if (spriteRenderers != null && spriteRenderers.Length > 0)
        {
            originalColors = new Color[spriteRenderers.Length];
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                originalColors[i] = spriteRenderers[i].color;
            }
        }

        initialScale = basePrefabScale;
        currentBodyRadius = bodyCollisionRadius;
        isFacingRight = !initialFacingLeft;
        dashTimer = Random.Range(dashCooldown * 0.5f, dashCooldown);
    }

    private void Start()
    {
        if (player == null)
        {
            FindPlayer();
        }
    }

    public void SetTarget(Transform target)
    {
        player = target;
    }

    private void FindPlayer()
    {
        nextPlayerSearchTime = Time.time + 1.0f;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    private void Update()
    {
        CheckEnrageStatus();

        if (currentState == BossState.Chase && enableDashAttack && CanStartDashFromCurrentRange())
        {
            float currentCooldown = isEnraged ? (dashCooldown * enrageCooldownMultiplier) : dashCooldown;
            dashTimer -= Time.deltaTime;

            if (dashTimer <= 0f && player != null)
            {
                float minDashDist = stoppingDistance * 1.5f;
                if ((rb.position - (Vector2)player.position).sqrMagnitude > minDashDist * minDashDist)
                {
                    StartWindup();
                }
            }
        }
    }

    private void FixedUpdate()
    {
        KeepBossInsideMap();

        if (player == null || !player.gameObject.activeInHierarchy)
        {
            if (Time.time >= nextPlayerSearchTime)
            {
                FindPlayer();
            }
            if (player == null || !player.gameObject.activeInHierarchy)
            {
                if (rb != null) rb.velocity = Vector2.zero;
                PlayAnimation(activeIdleAnimationHash);
                return;
            }
        }

        switch (currentState)
        {
            case BossState.Chase:
                UpdateChaseMovement();
                UpdateFacingDirection();
                break;

            case BossState.Windup:
                rb.velocity = Vector2.zero;
                PlayAnimation(activeIdleAnimationHash);
                stateTimer -= Time.fixedDeltaTime;
                // Khóa hướng ngắm vào Player trong lúc tích lực
                if (player != null)
                {
                    dashDirection = ((Vector2)player.position - rb.position).normalized;
                }
                UpdateFacingDirection();
                UpdateWindupFlashing();
                if (stateTimer <= 0f)
                {
                    StartDash();
                }
                break;

            case BossState.Dash:
                PlayAnimation(activeRunAnimationHash);
                float currentDashSpeed = (moveSpeed * (isEnraged ? enrageSpeedMultiplier : 1f)) * dashSpeedMultiplier;
                MoveInsideMap(rb.position + dashDirection * currentDashSpeed * Time.fixedDeltaTime);
                CheckDashHitPlayer();
                stateTimer -= Time.fixedDeltaTime;
                if (stateTimer <= 0f)
                {
                    StartRecover();
                }
                break;

            case BossState.Recover:
                rb.velocity = Vector2.zero;
                PlayAnimation(activeIdleAnimationHash);
                stateTimer -= Time.fixedDeltaTime;
                if (stateTimer <= 0f)
                {
                    remainingDashesInCombo--;
                    if (remainingDashesInCombo > 0 && player != null && player.gameObject.activeInHierarchy)
                    {
                        StartWindup(true);
                    }
                    else
                    {
                        EndDash();
                    }
                }
                break;
        }
    }

    private void UpdateChaseMovement()
    {
        if (player == null)
        {
            PlayAnimation(activeIdleAnimationHash);
            return;
        }

        Vector2 toPlayer = (Vector2)player.position - rb.position;
        float distance = toPlayer.magnitude;

        // Chỉ dừng lại khi Boss đang thực sự thi triển loạt đạn bắn (IsAttacking),
        // tránh việc Boss đứng trơ như tượng trong suốt thời gian hồi chiêu 2.5 - 4.5s
        if (rangedAttack != null && rangedAttack.IsAttacking)
        {
            rb.velocity = Vector2.zero;
            PlayAnimation(activeIdleAnimationHash);
            return;
        }

        // Dừng khi đã áp sát đến cự ly cận chiến
        if (distance <= stoppingDistance)
        {
            rb.velocity = Vector2.zero;
            PlayAnimation(activeIdleAnimationHash);
            return;
        }

        Vector2 moveDir = toPlayer.normalized;
        float effectiveSpeed = moveSpeed * (isEnraged ? enrageSpeedMultiplier : 1f);
        Vector2 targetPos = rb.position + moveDir * effectiveSpeed * Time.fixedDeltaTime;
        MoveInsideMap(targetPos);
        PlayAnimation(activeRunAnimationHash);
    }

    private void MoveInsideMap(Vector2 targetPosition)
    {
        Vector2 currentPos = rb.position;
        Vector2 delta = targetPosition - currentPos;
        float distance = delta.magnitude;

        if (distance > 0.0001f)
        {
            Vector2 dir = delta / distance;
            int count = Physics2D.CircleCastNonAlloc(
                currentPos,
                currentBodyRadius,
                dir,
                obstacleHitBuffer,
                distance + 0.05f,
                obstacleFilter.layerMask
            );
            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit = obstacleHitBuffer[i];
                if (hit.collider != null && !hit.collider.isTrigger && hit.collider.gameObject != gameObject)
                {
                    float allowed = Mathf.Max(0f, hit.distance - 0.05f);
                    Vector2 normal = hit.normal;
                    Vector2 leftover = delta * (1f - Mathf.Clamp01(allowed / (distance + 0.05f)));
                    Vector2 slide = leftover - Vector2.Dot(leftover, normal) * normal;
                    if (slide.sqrMagnitude < 0.0001f && normal.sqrMagnitude > 0.001f)
                    {
                        Vector2 tangent = Vector2.Perpendicular(normal);
                        if (player != null && Vector2.Dot(tangent, (Vector2)player.position - currentPos) < 0f)
                        {
                            tangent = -tangent;
                        }
                        slide = tangent * leftover.magnitude;
                    }
                    targetPosition = currentPos + dir * allowed + slide;
                    break;
                }
            }
        }

        Vector2 clampedPosition = MapBoundary.Instance != null
            ? MapBoundary.Instance.ClampSpawnPosition(targetPosition, mapBoundaryPadding)
            : targetPosition;
        rb.MovePosition(clampedPosition);
    }

    private void KeepBossInsideMap()
    {
        if (rb == null || MapBoundary.Instance == null)
        {
            return;
        }

        Vector2 clampedPosition = MapBoundary.Instance.ClampSpawnPosition(rb.position, mapBoundaryPadding);
        if ((clampedPosition - rb.position).sqrMagnitude > 0.000001f)
        {
            rb.position = clampedPosition;
            rb.velocity = Vector2.zero;
        }
    }

    private bool CanStartDashFromCurrentRange()
    {
        if (rangedAttack == null) return true;
        return !rangedAttack.IsAttacking;
    }

    private void PlayAnimation(int stateHash)
    {
        if (animator == null || currentAnimationHash == stateHash) return;

        currentAnimationHash = stateHash;
        if (animator.HasState(0, stateHash))
        {
            animator.Play(stateHash, 0, 0f);
        }
    }

    /// <summary>
    private void LateUpdate()
    {
        if (currentState == BossState.Chase || currentState == BossState.Recover)
        {
            UpdateFacingDirection();
        }
    }

    /// <summary>
    /// Tự động lật mặt theo hướng Player (Trái/Phải), đảm bảo Boss luôn luôn quay mặt về phía Player.
    /// </summary>
    private void UpdateFacingDirection()
    {
        if (!autoFlipFacing || player == null || !player.gameObject.activeInHierarchy) return;

        float diffX = player.position.x - transform.position.x;
        if (Mathf.Abs(diffX) < flipDeadzone) return;

        isFacingRight = diffX > 0;
        float absScaleX = Mathf.Abs(initialScale.x > 0.0001f ? initialScale.x : transform.localScale.x);
        float sign = (isFacingRight ^ initialFacingLeft) ? 1f : -1f;
        float targetScaleX = absScaleX * sign;

        if (Mathf.Abs(transform.localScale.x - targetScaleX) > 0.0001f)
        {
            float targetScaleY = initialScale.y != 0f ? initialScale.y : transform.localScale.y;
            float targetScaleZ = initialScale.z != 0f ? initialScale.z : transform.localScale.z;
            transform.localScale = new Vector3(targetScaleX, targetScaleY, targetScaleZ);
        }
    }

    private void UpdateWindupFlashing()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0) return;

        // Tần số nhấp nháy báo hiệu chuẩn bị lao tới (khoảng 12Hz để người chơi thấy rõ nhấp nháy liên tục)
        const float flashHz = 12f;
        bool isFlashOn = Mathf.Repeat(stateTimer * flashHz, 1f) < 0.5f;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                Color baseCol = (originalColors != null && i < originalColors.Length)
                    ? (isEnraged ? enrageColor : originalColors[i])
                    : Color.white;

                spriteRenderers[i].color = isFlashOn ? windupColor : baseCol;
            }
        }
    }

    private void StartWindup(bool isComboFollowup = false)
    {
        currentState = BossState.Windup;
        stateTimer = dashWindupDuration;
        if (!isComboFollowup)
        {
            remainingDashesInCombo = (isEnraged && enrageDashComboCount > 1)
                ? enrageDashComboCount
                : Mathf.Max(1, baseDashComboCount);
        }
        if (player != null)
        {
            dashDirection = ((Vector2)player.position - rb.position).normalized;
        }
        UpdateWindupFlashing();
    }

    private void StartDash()
    {
        currentState = BossState.Dash;
        stateTimer = dashDuration;
        hasDealtDashDamage = false;
        RestoreSpritesColor();
        SetPassThroughPlayer(true);
    }

    private void StartRecover()
    {
        SetPassThroughPlayer(false);
        currentState = BossState.Recover;
        stateTimer = dashRecoverDuration;
    }

    private void EndDash()
    {
        SetPassThroughPlayer(false);
        currentState = BossState.Chase;
        float currentCooldown = isEnraged
            ? (enrageDashCooldown > 0f ? enrageDashCooldown : dashCooldown * enrageCooldownMultiplier)
            : dashCooldown;
        dashTimer = currentCooldown;
        remainingDashesInCombo = 0;
        RestoreSpritesColor();
    }

    private void CheckEnrageStatus()
    {
        if (!enableEnrage || health == null || isEnraged) return;

        if (health.CurrentHealth <= health.MaxHealth * enrageHealthPercent)
        {
            isEnraged = true;
            SetSpritesColor(enrageColor);

            if (enableDashAttack && player != null && player.gameObject.activeInHierarchy)
            {
                dashTimer = 0f;
                if (currentState == BossState.Chase)
                {
                    StartWindup(false);
                }
            }
        }
    }

    private void SetSpritesColor(Color color)
    {
        if (spriteRenderers == null) return;
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                spriteRenderers[i].color = color;
            }
        }
    }

    private void RestoreSpritesColor()
    {
        if (spriteRenderers == null || originalColors == null) return;
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null && i < originalColors.Length)
            {
                spriteRenderers[i].color = isEnraged ? enrageColor : originalColors[i];
            }
        }
    }

    public void OnSpawnFromPool()
    {
        moveSpeed = BaseMoveSpeed;
        currentState = BossState.Chase;
        isEnraged = false;
        remainingDashesInCombo = 0;
        if (initialScale != Vector3.zero)
        {
            transform.localScale = initialScale;
        }
        isFacingRight = !initialFacingLeft;
        dashTimer = Random.Range(dashCooldown * 0.5f, dashCooldown);
        RestoreSpritesColor();
        currentAnimationHash = 0;
        PlayAnimation(activeRunAnimationHash);
    }

    private void OnDisable()
    {
        SetPassThroughPlayer(false);
    }

    private void SetPassThroughPlayer(bool passThrough)
    {
        if (!dashPassThroughPlayer) return;
        if (player == null)
        {
            FindPlayer();
        }
        if (player == null) return;

        Collider2D[] bossCols = GetComponentsInChildren<Collider2D>();
        Collider2D[] playerCols = player.GetComponentsInChildren<Collider2D>();

        for (int i = 0; i < bossCols.Length; i++)
        {
            if (bossCols[i] == null) continue;
            for (int j = 0; j < playerCols.Length; j++)
            {
                if (playerCols[j] == null) continue;
                Physics2D.IgnoreCollision(bossCols[i], playerCols[j], passThrough);
            }
        }
    }

    private void CheckDashHitPlayer()
    {
        if (hasDealtDashDamage || player == null || !player.gameObject.activeInHierarchy) return;

        bool isHit = false;
        float hitRadius = dashDamageRadius > 0f ? dashDamageRadius : (currentBodyRadius * 1.5f);

        // 1. Kiểm tra cự ly tâm trực tiếp giữa Boss và Player
        if (((Vector2)player.position - rb.position).sqrMagnitude <= (hitRadius + 0.5f) * (hitRadius + 0.5f))
        {
            isHit = true;
        }
        else
        {
            // 2. Quét vùng overlap với hitbox của Player
            int playerMask = LayerMask.GetMask("Player");
            if (playerMask == 0) playerMask = ~0;

            Collider2D hit = Physics2D.OverlapCircle(rb.position, hitRadius, playerMask);
            if (hit != null && (hit.CompareTag("Player") || hit.transform.IsChildOf(player)))
            {
                isHit = true;
            }
        }

        if (isHit)
        {
            PlayerHealth playerHealth = player.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null && !playerHealth.IsDead)
            {
                int effectiveDamage = Mathf.RoundToInt(dashDamage * (isEnraged ? 1.25f : 1.0f) * currentDashDamageMultiplier);
                playerHealth.TakeDamage(effectiveDamage);
                hasDealtDashDamage = true;
                Debug.Log($"[BossMovement] Boss lướt xuyên trúng Player gây {effectiveDamage} sát thương lớn!");
            }
        }
    }

    public void OnReturnToPool()
    {
        SetPassThroughPlayer(false);
        hasDealtDashDamage = false;
        moveSpeed = BaseMoveSpeed;
        currentState = BossState.Chase;
        isEnraged = false;
        remainingDashesInCombo = 0;
        if (basePrefabScale != Vector3.zero)
        {
            initialScale = basePrefabScale;
            transform.localScale = basePrefabScale;
        }
        if (rb != null) rb.velocity = Vector2.zero;
        RestoreSpritesColor();
        currentAnimationHash = 0;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);
    }
}
