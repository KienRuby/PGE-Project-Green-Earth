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
    private Vector2 dashDirection;
    private float nextPlayerSearchTime;
    private Vector3 initialScale;
    private bool isFacingRight = true;
    private bool isEnraged = false;
    private float baseMoveSpeed;
    private int currentAnimationHash;

    private static readonly int RunAnimationHash = Animator.StringToHash("Run");
    private static readonly int IdleAnimationHash = Animator.StringToHash("Idle");

    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = value;
    }
    public float BaseMoveSpeed => baseMoveSpeed > 0 ? baseMoveSpeed : moveSpeed;

    public BossState CurrentState => currentState;
    public bool IsEnraged => isEnraged;

    private void Awake()
    {
        baseMoveSpeed = moveSpeed;
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        health = GetComponent<EnemyHealth>();
        rangedAttack = GetComponent<BossRangedAttack>();
        animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        if (spriteRenderers != null && spriteRenderers.Length > 0)
        {
            originalColors = new Color[spriteRenderers.Length];
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                originalColors[i] = spriteRenderers[i].color;
            }
        }

        initialScale = transform.localScale;
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
                PlayAnimation(IdleAnimationHash);
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
                PlayAnimation(IdleAnimationHash);
                stateTimer -= Time.fixedDeltaTime;
                // Khóa hướng ngắm vào Player trong lúc tích lực
                if (player != null)
                {
                    dashDirection = ((Vector2)player.position - rb.position).normalized;
                }
                UpdateFacingDirection();
                if (stateTimer <= 0f)
                {
                    StartDash();
                }
                break;

            case BossState.Dash:
                PlayAnimation(RunAnimationHash);
                float currentDashSpeed = (moveSpeed * (isEnraged ? enrageSpeedMultiplier : 1f)) * dashSpeedMultiplier;
                MoveInsideMap(rb.position + dashDirection * currentDashSpeed * Time.fixedDeltaTime);
                stateTimer -= Time.fixedDeltaTime;
                if (stateTimer <= 0f)
                {
                    StartRecover();
                }
                break;

            case BossState.Recover:
                rb.velocity = Vector2.zero;
                PlayAnimation(IdleAnimationHash);
                stateTimer -= Time.fixedDeltaTime;
                if (stateTimer <= 0f)
                {
                    EndDash();
                }
                break;
        }
    }

    private void UpdateChaseMovement()
    {
        if (player == null)
        {
            PlayAnimation(IdleAnimationHash);
            return;
        }

        Vector2 toPlayer = (Vector2)player.position - rb.position;
        float distance = toPlayer.magnitude;

        BossRangedAttack.TargetRangeState rangeState = rangedAttack != null
            ? rangedAttack.GetTargetRangeState()
            : BossRangedAttack.TargetRangeState.NoTarget;

        if (rangeState == BossRangedAttack.TargetRangeState.InRange)
        {
            rb.velocity = Vector2.zero;
            PlayAnimation(IdleAnimationHash);
            return;
        }

        if (rangedAttack == null && distance <= stoppingDistance)
        {
            rb.velocity = Vector2.zero;
            PlayAnimation(IdleAnimationHash);
            return;
        }

        Vector2 moveDir = toPlayer.normalized;
        float effectiveSpeed = moveSpeed * (isEnraged ? enrageSpeedMultiplier : 1f);
        Vector2 targetPos = rb.position + moveDir * effectiveSpeed * Time.fixedDeltaTime;
        MoveInsideMap(targetPos);
        PlayAnimation(RunAnimationHash);
    }

    private void MoveInsideMap(Vector2 targetPosition)
    {
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
        return rangedAttack.GetTargetRangeState() == BossRangedAttack.TargetRangeState.TooFar;
    }

    private void PlayAnimation(int stateHash)
    {
        if (animator == null || currentAnimationHash == stateHash) return;

        currentAnimationHash = stateHash;
        animator.Play(stateHash, 0, 0f);
    }

    /// <summary>
    /// Tự động lật mặt theo hướng Player (Trái/Phải).
    /// </summary>
    private void UpdateFacingDirection()
    {
        if (!autoFlipFacing || player == null) return;

        float diffX = player.position.x - transform.position.x;
        if (Mathf.Abs(diffX) < flipDeadzone) return;

        bool shouldFaceRight = diffX > 0;
        if (shouldFaceRight != isFacingRight)
        {
            isFacingRight = shouldFaceRight;
            float absScaleX = Mathf.Abs(initialScale.x);
            float sign = (isFacingRight ^ initialFacingLeft) ? 1f : -1f;
            transform.localScale = new Vector3(absScaleX * sign, initialScale.y, initialScale.z);
        }
    }

    private void StartWindup()
    {
        currentState = BossState.Windup;
        stateTimer = dashWindupDuration;
        if (player != null)
        {
            dashDirection = ((Vector2)player.position - rb.position).normalized;
        }
        SetSpritesColor(windupColor);
    }

    private void StartDash()
    {
        currentState = BossState.Dash;
        stateTimer = dashDuration;
        RestoreSpritesColor();
    }

    private void StartRecover()
    {
        currentState = BossState.Recover;
        stateTimer = dashRecoverDuration;
    }

    private void EndDash()
    {
        currentState = BossState.Chase;
        float currentCooldown = isEnraged ? (dashCooldown * enrageCooldownMultiplier) : dashCooldown;
        dashTimer = currentCooldown;
        RestoreSpritesColor();
    }

    private void CheckEnrageStatus()
    {
        if (!enableEnrage || health == null || isEnraged) return;

        if (health.CurrentHealth <= health.MaxHealth * enrageHealthPercent)
        {
            isEnraged = true;
            Debug.Log($"[BossMovement] ⚡ BOSS CUỒNG NỘ! (Máu < {enrageHealthPercent * 100}%) Tốc độ và tần suất húc tăng mạnh!");
            SetSpritesColor(enrageColor);
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
        if (initialScale != Vector3.zero)
        {
            transform.localScale = initialScale;
        }
        isFacingRight = !initialFacingLeft;
        dashTimer = Random.Range(dashCooldown * 0.5f, dashCooldown);
        RestoreSpritesColor();
        currentAnimationHash = 0;
        PlayAnimation(RunAnimationHash);
    }

    public void OnReturnToPool()
    {
        moveSpeed = BaseMoveSpeed;
        currentState = BossState.Chase;
        isEnraged = false;
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
