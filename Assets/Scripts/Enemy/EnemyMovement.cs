using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMovement : MonoBehaviour, IPoolable
{
    [Header("Movement")]
    [Tooltip("Tốc độ di chuyển của quái vật khi đuổi theo Player.")]
    [SerializeField] private float moveSpeed = 2f;

    [Tooltip("Khoảng cách tối thiểu với Player (giúp quái dàn hàng bao quanh Player thay vì chui vào trong người chơi).")]
    [SerializeField] private float stoppingDistance = 0.7f;

    [Header("Anti-Stacking / Separation")]
    [Tooltip("Bán kính phát hiện các quái vật khác để tạo lực đẩy tách nhau ra, chống xếp chồng.")]
    [SerializeField] private float separationRadius = 0.9f;

    [Tooltip("Lực đẩy chống xếp chồng (trọng số tách đàn). Càng lớn thì quái càng giữ khoảng cách tốt.")]
    [SerializeField] private float separationWeight = 2.5f;

    [Tooltip("LayerMask của quái vật để quét va chạm tách đàn (tự động gán Layer 'Enemy' nếu để trống).")]
    [SerializeField] private LayerMask enemyLayer;

    [Header("Facing / Flipping")]
    [Tooltip("Tự động lật hướng mặt (quay trái/phải) về phía Player.")]
    [SerializeField] private bool autoFlipFacing = true;

    [Tooltip("Hướng mặt mặc định của sprite gốc trong prefab (chọn true nếu sprite gốc vẽ hướng sang trái).")]
    [SerializeField] private bool initialFacingLeft = false;

    [Tooltip("Vùng đệm khoảng cách X để chống rung lắc khi đứng thẳng hàng dọc với Player.")]
    [SerializeField] private float flipDeadzone = 0.05f;

    private Rigidbody2D rb;
    private Transform player;
    private PlayerMovement playerMovement;
    private float nextPlayerSearchTime;
    private Vector3 initialScale;
    private bool isFacingRight = true;
    private float stunTimer = 0f;

    // Shared static buffer để quét quái xung quanh không tạo rác GC
    private static readonly Collider2D[] sharedCollidersBuffer = new Collider2D[16];
    private ContactFilter2D contactFilter;
    private float baseMoveSpeed;
    private Vector2 cachedSeparationForce;
    private int instanceId;
    private Vector2 knockbackVelocity;
    private float knockbackTimer;
    private float slowTimer;
    private float currentSlowPercent;

    public bool IsStunned => stunTimer > 0f;

    public void ApplyStun(float duration)
    {
        if (duration <= 0f) return;
        stunTimer = Mathf.Max(stunTimer, duration);
        if (rb != null) rb.velocity = Vector2.zero;
    }

    public void ApplyKnockback(Vector2 direction, float force, float duration = 0.2f)
    {
        if (duration <= 0f || force <= 0f) return;
        knockbackVelocity = direction.normalized * force;
        knockbackTimer = duration;
    }

    public void ApplySlow(float slowPercent, float duration)
    {
        if (duration <= 0f || slowPercent <= 0f) return;
        slowTimer = Mathf.Max(slowTimer, duration);
        currentSlowPercent = Mathf.Max(currentSlowPercent, Mathf.Clamp01(slowPercent));
    }

    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = value;
    }
    public float BaseMoveSpeed => baseMoveSpeed > 0 ? baseMoveSpeed : moveSpeed;

    private void Awake()
    {
        baseMoveSpeed = moveSpeed;
        instanceId = GetInstanceID();
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        initialScale = transform.localScale;
        isFacingRight = !initialFacingLeft;

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
        playerMovement = target != null ? target.GetComponent<PlayerMovement>() : null;
    }

    private void FindPlayer()
    {
        nextPlayerSearchTime = Time.time + 1.0f;
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
            playerMovement = playerObject.GetComponent<PlayerMovement>();
        }
    }

    private void FixedUpdate()
    {
        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.fixedDeltaTime;
            rb.MovePosition(rb.position + knockbackVelocity * Time.fixedDeltaTime);
            knockbackVelocity = Vector2.Lerp(knockbackVelocity, Vector2.zero, Time.fixedDeltaTime * 8f);
            return;
        }

        if (stunTimer > 0f)
        {
            stunTimer -= Time.fixedDeltaTime;
            if (rb != null) rb.velocity = Vector2.zero;
            return;
        }

        if (player == null || !player.gameObject.activeInHierarchy)
        {
            if (Time.time >= nextPlayerSearchTime)
            {
                FindPlayer();
            }
            if (player == null || !player.gameObject.activeInHierarchy)
            {
                if (rb != null) rb.velocity = Vector2.zero;
                return;
            }
        }

        Vector2 playerDirection = CalculatePlayerDirection();
        Vector2 separationForce = CalculateSeparationForce();

        // Kết hợp hướng đuổi Player và lực đẩy chống xếp chồng
        Vector2 finalDirection = playerDirection + separationForce * separationWeight;

        if (finalDirection.sqrMagnitude > 1f)
        {
            finalDirection.Normalize();
        }

        float effectiveSpeed = moveSpeed;
        if (slowTimer > 0f)
        {
            slowTimer -= Time.fixedDeltaTime;
            effectiveSpeed *= Mathf.Max(0.1f, 1f - currentSlowPercent);
            if (slowTimer <= 0f)
            {
                currentSlowPercent = 0f;
            }
        }

        Vector2 newPosition = rb.position + finalDirection * effectiveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);

        UpdateFacingDirection();
    }

    private void LateUpdate()
    {
        UpdateFacingDirection();
    }

    /// <summary>
    /// Tự động lật mặt theo hướng Player (Trái/Phải), đảm bảo quái luôn luôn quay mặt về phía Player.
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

    /// <summary>
    /// Tính toán hướng di chuyển tới Player và giữ khoảng cách không đè lên Player.
    /// </summary>
    private Vector2 CalculatePlayerDirection()
    {
        if (player == null) return Vector2.zero;

        Vector2 toPlayer = (Vector2)player.position - rb.position;
        float distanceToPlayer = toPlayer.magnitude;

        if (distanceToPlayer <= 0.001f)
        {
            // Trùng vị trí 100% với player -> đẩy ra theo hướng ổn định dựa trên InstanceID
            int id = instanceId != 0 ? instanceId : GetInstanceID();
            float angle = (id & 0xFFFF) * (Mathf.PI * 2f / 65536f);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        // 1. Khi Player đang chủ động di chuyển về phía quái này, quái sẽ né dạt sang 2 bên để nhường đường
        if (playerMovement == null && player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
        }

        if (playerMovement != null && playerMovement.MoveDirection.sqrMagnitude > 0.05f)
        {
            Vector2 playerMoveDir = playerMovement.MoveDirection.normalized;
            Vector2 toThisEnemy = -toPlayer; // Vector từ tâm Player tới Quái
            float dot = Vector2.Dot(playerMoveDir, toThisEnemy.normalized);

            // Quái nằm ở phía trước hướng Player đang đi và ở cự ly gần
            if (dot > 0.1f && distanceToPlayer < stoppingDistance * 2.2f)
            {
                Vector2 sideDir = Vector2.Perpendicular(playerMoveDir);
                if (Vector2.Dot(sideDir, toThisEnemy) < 0f)
                {
                    sideDir = -sideDir;
                }
                return (sideDir * 1.6f + toThisEnemy.normalized * 0.4f).normalized;
            }
        }

        // 2. Nếu quái đã áp sát quá gần Player (< stoppingDistance), đẩy nhẹ ra để không đè vào lòng Player
        if (distanceToPlayer < stoppingDistance)
        {
            float pushBack = 1f - (distanceToPlayer / stoppingDistance);
            return -toPlayer.normalized * pushBack;
        }

        return toPlayer.normalized;
    }

    /// <summary>
    /// Quét các quái vật xung quanh và tính toán lực đẩy ra xa nhau (Flocking Separation).
    /// Áp dụng Staggered Interleaving & Viewport Culling để giảm 66% số lần quét vật lý.
    /// </summary>
    private Vector2 CalculateSeparationForce()
    {
        // 1. Viewport Culling: Nếu quái ở quá xa ngoài rìa màn hình (> 13m), không cần tính tách đàn
        if (player != null)
        {
            Vector2 toPlayer = (Vector2)player.position - rb.position;
            if (toPlayer.sqrMagnitude > 169f) // 13 * 13
            {
                cachedSeparationForce = Vector2.zero;
                return Vector2.zero;
            }
        }

        // 2. Staggered update: Chỉ tính toán lại 1 lần mỗi 3 frames theo instanceId
        if (((Time.frameCount + instanceId) % 3) != 0)
        {
            return cachedSeparationForce;
        }

        int hitCount = Physics2D.OverlapCircle(
            rb.position,
            separationRadius,
            contactFilter,
            sharedCollidersBuffer
        );

        // Fallback nếu LayerMask chưa trúng
        if (hitCount == 0 && enemyLayer.value != 0)
        {
            ContactFilter2D fallbackFilter = new ContactFilter2D { useTriggers = true };
            hitCount = Physics2D.OverlapCircle(
                rb.position,
                separationRadius,
                fallbackFilter,
                sharedCollidersBuffer
            );
        }

        Vector2 separation = Vector2.zero;
        Vector2 myPos = rb.position;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D otherCollider = sharedCollidersBuffer[i];
            if (otherCollider == null || otherCollider.gameObject == gameObject)
                continue;

            // Bỏ qua nếu collider đó là Player
            if (otherCollider.CompareTag("Player"))
                continue;

            Vector2 otherPos = otherCollider.transform.position;
            Vector2 diff = myPos - otherPos;
            float distance = diff.magnitude;

            if (distance < 0.001f)
            {
                // Nếu 2 quái trùng khít tọa độ, tạo lực đẩy đối xứng ổn định giữa 2 quái
                int myId = instanceId != 0 ? instanceId : GetInstanceID();
                int otherId = otherCollider.gameObject.GetInstanceID();
                float sign = myId > otherId ? 1f : -1f;
                int combinedId = myId ^ otherId;
                float angle = (combinedId & 0xFFFF) * (Mathf.PI * 2f / 65536f);
                Vector2 baseDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                if (baseDir.sqrMagnitude < 0.001f)
                    baseDir = Vector2.right;
                diff = baseDir.normalized * (sign * 0.1f);
                distance = diff.magnitude;
            }

            if (distance < separationRadius)
            {
                // Lực đẩy tỷ lệ nghịch với khoảng cách (càng gần đẩy càng mạnh)
                float pushStrength = 1f - (distance / separationRadius);
                separation += diff.normalized * pushStrength;
            }
        }

        cachedSeparationForce = separation;
        return separation;
    }

    public void OnSpawnFromPool()
    {
        moveSpeed = BaseMoveSpeed;
        stunTimer = 0f;
        cachedSeparationForce = Vector2.zero;
        if (initialScale != Vector3.zero)
        {
            transform.localScale = initialScale;
        }
        isFacingRight = !initialFacingLeft;

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
    }

    public void OnReturnToPool()
    {
        moveSpeed = BaseMoveSpeed;
        stunTimer = 0f;
        cachedSeparationForce = Vector2.zero;
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, separationRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);
    }
}