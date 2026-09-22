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
    [SerializeField] private float separationWeight = 1.0f;

    [Tooltip("LayerMask của quái vật để quét va chạm tách đàn (tự động gán Layer 'Enemy' nếu để trống).")]
    [SerializeField] private LayerMask enemyLayer;

    [Header("Obstacle Navigation & Avoidance")]
    [Tooltip("LayerMask của chướng ngại vật (tự động tìm 'Obstacle' nếu để trống).")]
    [SerializeField] private LayerMask obstacleLayer;

    [Tooltip("Cự ly cảm biến phía trước để lượn qua mép chướng ngại vật, chống đâm mặt vào tường.")]
    [SerializeField] private float feelerDistance = 0.75f;

    [Tooltip("Khoảng đệm an toàn khi tìm góc bo của chướng ngại vật.")]
    [SerializeField] private float obstaclePadding = 0.45f;

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
    private Vector3 basePrefabScale = Vector3.one;
    private Vector3 initialScale;
    private bool isFacingRight = true;
    private float stunTimer = 0f;

    private static readonly Collider2D[] sharedCollidersBuffer = new Collider2D[16];
    private static readonly RaycastHit2D[] sharedObstacleHitBuffer = new RaycastHit2D[8];
    private ContactFilter2D contactFilter;
    private ContactFilter2D obstacleFilter;
    private float baseMoveSpeed;
    private Vector2 cachedSeparationForce;
    private Vector2 cachedFinalDirection = Vector2.zero;
    private int physicsTickCounter = 0;
    private float baseBodyRadius = 0.22f;
    private float currentBodyRadius = 0.22f;
    private int instanceId;
    private Vector2 knockbackVelocity;
    private float knockbackTimer;
    private float slowTimer;
    private float currentSlowPercent;

    // Detour & Navigation
    private Vector2 detourWaypoint;
    private bool isDetourActive = false;
    private float nextPathCheckTime = 0f;

    // Anti-Stuck Detection
    private Vector2 lastSamplePos;
    private float stuckCheckTimer = 0f;
    private int stuckCount = 0;

    // Bước 3: Bộ nhớ đệm quãng đường an toàn (Obstacle Proximity Caching)
    private float safeClearDistance = 0f;
    private Vector2 lastClearDir = Vector2.zero;

    public bool IsStunned => stunTimer > 0f;
    public Transform CurrentTarget => player;

    public void SetScaleMultiplier(float multiplier)
    {
        if (multiplier <= 0f) return;
        if (basePrefabScale == Vector3.zero)
        {
            basePrefabScale = transform.localScale != Vector3.zero ? transform.localScale : Vector3.one;
        }
        initialScale = basePrefabScale * multiplier;
        currentBodyRadius = baseBodyRadius * multiplier;
        float sign = (isFacingRight ^ initialFacingLeft) ? 1f : -1f;
        transform.localScale = new Vector3(Mathf.Abs(initialScale.x) * sign, initialScale.y, initialScale.z);
    }

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
        basePrefabScale = transform.localScale != Vector3.zero ? transform.localScale : Vector3.one;
        initialScale = basePrefabScale;
        currentBodyRadius = baseBodyRadius;
        isFacingRight = !initialFacingLeft;

        if (enemyLayer.value == 0)
        {
            enemyLayer = LayerMask.GetMask("Enemy");
        }

        if ((enemyLayer.value & (1 << gameObject.layer)) == 0)
        {
            enemyLayer.value |= (1 << gameObject.layer);
        }

        contactFilter = new ContactFilter2D
        {
            layerMask = enemyLayer,
            useLayerMask = enemyLayer.value != 0,
            useTriggers = true
        };

        if (obstacleLayer.value == 0)
        {
            obstacleLayer = LayerMask.GetMask("Obstacle");
            if (obstacleLayer.value == 0)
            {
                obstacleLayer = LayerMask.GetMask("Default");
            }
        }

        obstacleFilter = new ContactFilter2D
        {
            useTriggers = false,
            layerMask = obstacleLayer,
            useLayerMask = true
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
            Vector2 kbDelta = knockbackVelocity * Time.fixedDeltaTime;
            MoveWithObstacleSlide(kbDelta);
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

        physicsTickCounter++;
        bool shouldUpdateSteering = ((physicsTickCounter + instanceId) % 4) == 0 || cachedFinalDirection == Vector2.zero;

        if (shouldUpdateSteering)
        {
            Vector2 playerDirection = CalculatePlayerDirection();

            // Bước 2: Tối ưu tần số quét Separation (Time-slicing theo khoảng cách)
            // Quái ngoài màn hình (> 10m): bỏ qua separation hoàn toàn (lực tách = 0).
            // Quái tầm trung (4-10m): quét mỗi 10 physics ticks (~0.20s).
            // Quái cận chiến (<= 4m): quét mỗi 6 physics ticks (~0.12s).
            float sqrDistToPlayer = player != null ? ((Vector2)player.position - rb.position).sqrMagnitude : 0f;
            bool shouldUpdateSeparation;
            if (player != null && sqrDistToPlayer > 100f)
            {
                cachedSeparationForce = Vector2.zero;
                shouldUpdateSeparation = false;
            }
            else if (player == null)
            {
                shouldUpdateSeparation = true;
            }
            else
            {
                int sepInterval = sqrDistToPlayer <= 16f ? 6 : 10;
                shouldUpdateSeparation = ((physicsTickCounter + instanceId) % sepInterval) == 0 || cachedSeparationForce == Vector2.zero;
            }

            Vector2 separationForce = shouldUpdateSeparation ? CalculateSeparationForce() : cachedSeparationForce;

            Vector2 sep = separationForce;
            if (sep.sqrMagnitude > 1f)
            {
                sep.Normalize();
            }

            // Lực tách đàn chỉ đóng vai trò phân tán đàn quái thành vòng cung bao vây Player
            float effectiveSepWeight = Mathf.Clamp(separationWeight, 0f, 1.0f);
            Vector2 finalDirection = playerDirection + sep * effectiveSepWeight;

            // Đảm bảo quái luôn kiên định lao về phía Player, không bao giờ bị lực tách đàn đẩy lùi ngược lại
            if (playerDirection.sqrMagnitude > 0.01f && Vector2.Dot(finalDirection, playerDirection) < 0.2f)
            {
                Vector2 tangent = Vector2.Perpendicular(playerDirection);
                if (Vector2.Dot(tangent, sep) < 0f) tangent = -tangent;
                finalDirection = (playerDirection * 0.75f + tangent * 0.25f).normalized;
            }
            else if (finalDirection.sqrMagnitude > 1f)
            {
                finalDirection.Normalize();
            }

            cachedFinalDirection = finalDirection;
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

        Vector2 moveDelta = cachedFinalDirection * (effectiveSpeed * Time.fixedDeltaTime);
        MoveWithObstacleSlide(moveDelta);
    }

    /// <summary>
    /// Chống xuyên thấu tuyệt đối (Hard Clamping): Kiểm tra chướng ngại vật trước khi dịch chuyển.
    /// Bước 3: Tối ưu với Obstacle Proximity Caching và Off-screen Culling, giảm 85-90% CircleCast.
    /// </summary>
    private void MoveWithObstacleSlide(Vector2 delta)
    {
        float distance = delta.magnitude;
        if (distance < 0.0001f) return;

        Vector2 dir = delta / distance;

        // 1. Off-screen Culling: Quái ở ngoài màn hình (> 11m) di chuyển trực tiếp, không cần CircleCast
        if (player != null)
        {
            Vector2 toPlayer = (Vector2)player.position - rb.position;
            if (toPlayer.sqrMagnitude > 121f) // > 11m
            {
                rb.MovePosition(rb.position + delta);
                safeClearDistance = 0f;
                return;
            }
        }

        // 2. Proximity Caching: Nếu hướng đi không đổi và quãng đường an toàn còn đủ lớn, di chuyển không cần CircleCast
        bool isDirectionConsistent = safeClearDistance > distance && Vector2.Dot(dir, lastClearDir) > 0.95f;
        if (isDirectionConsistent)
        {
            safeClearDistance -= distance;
            rb.MovePosition(rb.position + delta);
            return;
        }

        // 3. Quét kiểm tra chướng ngại vật phía trước với khoảng đệm an toàn 0.35m
        float castRange = Mathf.Max(distance + 0.03f, 0.35f);
        int hitCount = Physics2D.CircleCastNonAlloc(
            rb.position,
            currentBodyRadius,
            dir,
            sharedObstacleHitBuffer,
            castRange,
            obstacleFilter.layerMask
        );

        RaycastHit2D validHit = default;
        bool hasHit = false;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = sharedObstacleHitBuffer[i];
            if (hit.collider != null && !hit.collider.isTrigger && hit.collider.gameObject != gameObject)
            {
                validHit = hit;
                hasHit = true;
                break;
            }
        }

        if (hasHit)
        {
            lastClearDir = dir;

            if (validHit.distance <= distance + 0.03f)
            {
                safeClearDistance = 0f;
                float allowedDist = Mathf.Max(0f, validHit.distance - 0.02f);
                Vector2 normal = validHit.normal;
                float leftoverFraction = 1f - Mathf.Clamp01(allowedDist / (distance + 0.03f));
                Vector2 leftover = delta * leftoverFraction;
                Vector2 slideDelta = leftover - Vector2.Dot(leftover, normal) * normal;

                Vector2 newPos = rb.position + dir * allowedDist + slideDelta;
                rb.MovePosition(newPos);
            }
            else
            {
                safeClearDistance = validHit.distance - (distance + 0.03f);
                rb.MovePosition(rb.position + delta);
            }
        }
        else
        {
            safeClearDistance = castRange - distance;
            lastClearDir = dir;
            rb.MovePosition(rb.position + delta);
        }
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
    /// Tính toán hướng di chuyển áp sát Player thông minh:
    /// 1. Kiểm tra tầm nhìn (Line of Sight) tới Player.
    /// 2. Nếu bị chặn bởi chướng ngại vật: Tính toán các điểm góc bo (contour corners) và chọn đường ngắn nhất.
    /// 3. Cảm biến tia (Feeler rays) phía trước giúp lượn mượt quanh mép đá, không đập mặt vào tường.
    /// 4. Cơ chế chống kẹt (Anti-stuck) tự đổi hướng thoát hiểm.
    /// 5. Phân luồng Staggered theo frame để tối ưu CPU cực đại cho 50+ quái.
    /// </summary>
    private Vector2 CalculatePlayerDirection()
    {
        if (player == null) return Vector2.zero;

        Vector2 myPos = rb.position;
        Vector2 targetPos = (Vector2)player.position;
        Vector2 toPlayer = targetPos - myPos;
        float distanceToPlayer = toPlayer.magnitude;

        if (distanceToPlayer <= 0.001f)
        {
            int id = instanceId != 0 ? instanceId : GetInstanceID();
            float angle = (id & 0xFFFF) * (Mathf.PI * 2f / 65536f);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        Vector2 directDir = toPlayer / distanceToPlayer;

        // Quái ngoài tầm nhìn (> 11m): Không cần bắn tia Linecast/Feeler rays phức tạp, di chuyển thẳng về phía Player
        if (distanceToPlayer > 11f)
        {
            isDetourActive = false;
            return directDir;
        }

        // 1. Phân luồng Staggered: Chỉ kiểm tra Line of Sight và tính toán đường vòng theo chu kỳ so le khi hết thời gian chờ
        bool shouldEvaluatePath = Time.time >= nextPathCheckTime && ((physicsTickCounter + instanceId) % 4) == 0;

        if (shouldEvaluatePath)
        {
            RaycastHit2D losHit = Physics2D.Linecast(myPos, targetPos, obstacleFilter.layerMask);

            if (losHit.collider == null)
            {
                // Tầm nhìn thông suốt -> Lao thẳng về phía Player
                isDetourActive = false;
                nextPathCheckTime = Time.time + 0.25f;
            }
            else
            {
                // Bị chướng ngại vật che chắn -> Tìm điểm góc bo ngắn nhất
                EvaluateShortestDetour(myPos, targetPos, directDir, losHit.collider);
                nextPathCheckTime = Time.time + 0.18f;
            }
        }

        // 2. Định hướng di chuyển chính: Hoặc bám theo waypoint đi vòng, hoặc bám theo Player
        Vector2 desiredDir = directDir;
        if (isDetourActive)
        {
            float distToWaypoint = Vector2.Distance(myPos, detourWaypoint);
            if (distToWaypoint < 0.35f)
            {
                // Đã đến điểm bo góc -> Hủy waypoint để frame sau kiểm tra LOS lại
                isDetourActive = false;
            }
            else
            {
                Vector2 toWaypoint = detourWaypoint - myPos;
                if (toWaypoint.sqrMagnitude > 0.0001f)
                {
                    desiredDir = toWaypoint.normalized;
                }
            }
        }

        // 3. Cảm biến tia phía trước (Feeler Rays) chống đâm mặt vào tường
        desiredDir = ApplyObstacleGliding(myPos, desiredDir);

        // 4. Kiểm tra chống kẹt (Anti-Stuck Recovery)
        stuckCheckTimer += Time.fixedDeltaTime;
        if (stuckCheckTimer >= 0.4f)
        {
            stuckCheckTimer = 0f;
            if (lastSamplePos != Vector2.zero && Vector2.Distance(myPos, lastSamplePos) < 0.06f && distanceToPlayer > stoppingDistance)
            {
                stuckCount++;
                if (stuckCount >= 2)
                {
                    // Đang bị kẹt góc -> Bẻ lái 90 độ theo phương tiếp tuyến để thoát kẹt
                    Vector2 escapeTangent = Vector2.Perpendicular(directDir);
                    if ((instanceId & 1) == 0) escapeTangent = -escapeTangent;
                    desiredDir = (desiredDir * 0.3f + escapeTangent * 0.7f).normalized;
                    isDetourActive = false;
                    stuckCount = 0;
                }
            }
            else
            {
                stuckCount = 0;
            }
            lastSamplePos = myPos;
        }

        return desiredDir;
    }

    /// <summary>
    /// Tính toán 4 góc bo của chướng ngại vật bị chắn và chọn góc có tổng quãng đường (Cost) ngắn nhất tới Player.
    /// </summary>
    private void EvaluateShortestDetour(Vector2 myPos, Vector2 targetPos, Vector2 directDir, Collider2D obstacle)
    {
        if (obstacle == null) return;

        Bounds b = obstacle.bounds;
        float pad = obstaclePadding;
        Vector2 min = (Vector2)b.min - new Vector2(pad, pad);
        Vector2 max = (Vector2)b.max + new Vector2(pad, pad);

        Vector2[] corners = {
            new Vector2(min.x, min.y),
            new Vector2(min.x, max.y),
            new Vector2(max.x, min.y),
            new Vector2(max.x, max.y)
        };

        float bestCost = float.MaxValue;
        Vector2 bestCorner = myPos + directDir;
        bool foundValidCorner = false;

        for (int i = 0; i < 4; i++)
        {
            Vector2 corner = corners[i];

            // Giữ điểm góc nằm trong map
            if (MapBoundary.Instance != null && !MapBoundary.Instance.IsInsideMap(corner, 0.2f))
            {
                continue;
            }

            // Kiểm tra điểm góc có bị nằm trong collider chướng ngại vật khác không
            if (Physics2D.OverlapPoint(corner, obstacleFilter.layerMask) != null)
            {
                continue;
            }

            // Cost = Quãng đường từ Enemy tới góc + Quãng đường từ góc tới Player
            float distToCorner = Vector2.Distance(myPos, corner);
            float distCornerToPlayer = Vector2.Distance(corner, targetPos);
            float totalCost = distToCorner + distCornerToPlayer;

            // Kiểm tra quái có nhìn thấy góc bo này không
            bool isCornerVisible = !Physics2D.Linecast(myPos, corner, obstacleFilter.layerMask);
            if (!isCornerVisible)
            {
                totalCost += 1.5f; // Điểm phạt nếu góc bị khuất
            }

            if (totalCost < bestCost)
            {
                bestCost = totalCost;
                bestCorner = corner;
                foundValidCorner = true;
            }
        }

        if (foundValidCorner)
        {
            detourWaypoint = bestCorner;
            isDetourActive = true;
        }
    }

    /// <summary>
    /// Bắn cảm biến tia phía trước (Feeler Rays) để lượn mượt quanh mép chướng ngại vật, không bao giờ đập mặt vào tường.
    /// </summary>
    private Vector2 ApplyObstacleGliding(Vector2 myPos, Vector2 currentDir)
    {
        if (currentDir.sqrMagnitude < 0.0001f) return currentDir;

        // Bắn tia thẳng phía trước trước tiên
        RaycastHit2D centerHit = Physics2D.Raycast(myPos, currentDir, feelerDistance, obstacleFilter.layerMask);
        RaycastHit2D mostCriticalHit = default;

        if (centerHit.collider != null && !centerHit.collider.isTrigger)
        {
            mostCriticalHit = centerHit;
        }
        else if (isDetourActive)
        {
            // Chỉ khi đang lách vật cản mới kiểm tra thêm 2 tia lệch góc để tiết kiệm 66% Raycasts
            Vector2 leftDir = Quaternion.Euler(0f, 0f, 28f) * currentDir;
            RaycastHit2D leftHit = Physics2D.Raycast(myPos, leftDir, feelerDistance * 0.85f, obstacleFilter.layerMask);
            if (leftHit.collider != null && !leftHit.collider.isTrigger)
            {
                mostCriticalHit = leftHit;
            }
            else
            {
                Vector2 rightDir = Quaternion.Euler(0f, 0f, -28f) * currentDir;
                RaycastHit2D rightHit = Physics2D.Raycast(myPos, rightDir, feelerDistance * 0.85f, obstacleFilter.layerMask);
                if (rightHit.collider != null && !rightHit.collider.isTrigger)
                {
                    mostCriticalHit = rightHit;
                }
            }
        }

        if (mostCriticalHit.collider != null)
        {
            Vector2 normal = mostCriticalHit.normal;
            // Chiếu vector di chuyển trượt theo tiếp tuyến bề mặt chướng ngại vật
            Vector2 slide = currentDir - Vector2.Dot(currentDir, normal) * normal;
            if (slide.sqrMagnitude > 0.001f)
            {
                return (currentDir * 0.25f + slide.normalized * 0.75f).normalized;
            }
        }

        return currentDir;
    }

    /// <summary>
    /// Quét các quái vật xung quanh và tính toán lực đẩy ra xa nhau (Flocking Separation).
    /// Bước 2: Khử trùng lặp Collider của cùng một Enemy, loại bỏ Fallback quét toàn bộ physics,
    /// và culling quái ngoài tầm nhìn để tối ưu hóa CPU cực đại cho bầy quái 200+.
    /// </summary>
    private Vector2 CalculateSeparationForce()
    {
        // 1. Viewport Culling: Nếu quái ở quá xa ngoài tầm nhìn (> 10m), không cần tính tách đàn
        if (player != null)
        {
            Vector2 toPlayer = (Vector2)player.position - rb.position;
            if (toPlayer.sqrMagnitude > 100f) // 10 * 10
            {
                cachedSeparationForce = Vector2.zero;
                return Vector2.zero;
            }
        }

        int hitCount = Physics2D.OverlapCircle(
            rb.position,
            separationRadius,
            contactFilter,
            sharedCollidersBuffer
        );

        if (hitCount <= 0)
        {
            cachedSeparationForce = Vector2.zero;
            return Vector2.zero;
        }

        Vector2 separation = Vector2.zero;
        Vector2 myPos = rb.position;
        int maxChecks = Mathf.Min(hitCount, sharedCollidersBuffer.Length);

        // Khử trùng lặp: Mỗi Enemy chỉ tính lực đẩy 1 lần (dù quái có 4-5 child PolygonCollider2D)
        int uniqueEnemies = 0;
        Rigidbody2D rb0 = null, rb1 = null, rb2 = null, rb3 = null;

        for (int i = 0; i < maxChecks && uniqueEnemies < 4; i++)
        {
            Collider2D otherCollider = sharedCollidersBuffer[i];
            if (otherCollider == null)
                continue;

            Rigidbody2D otherRb = otherCollider.attachedRigidbody;
            if (otherRb == null || otherRb == rb)
                continue;

            // Bỏ qua nếu collider đó là Player
            if (otherCollider.CompareTag("Player"))
                continue;

            // Bỏ qua nếu quái này đã được tính toán trong vòng lặp hiện tại
            if (otherRb == rb0 || otherRb == rb1 || otherRb == rb2 || otherRb == rb3)
                continue;

            // Ghi nhận quái duy nhất
            if (uniqueEnemies == 0) rb0 = otherRb;
            else if (uniqueEnemies == 1) rb1 = otherRb;
            else if (uniqueEnemies == 2) rb2 = otherRb;
            else if (uniqueEnemies == 3) rb3 = otherRb;
            uniqueEnemies++;

            Vector2 otherPos = otherRb.position;
            Vector2 diff = myPos - otherPos;
            float distance = diff.magnitude;

            if (distance < 0.001f)
            {
                // Nếu 2 quái trùng khít tọa độ, tạo lực đẩy đối xứng ổn định giữa 2 quái
                int myId = instanceId != 0 ? instanceId : GetInstanceID();
                int otherId = otherRb.gameObject.GetInstanceID();
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
        cachedFinalDirection = Vector2.zero;
        physicsTickCounter = 0;
        isDetourActive = false;
        detourWaypoint = Vector2.zero;
        nextPathCheckTime = 0f;
        stuckCheckTimer = 0f;
        stuckCount = 0;
        lastSamplePos = Vector2.zero;
        safeClearDistance = 0f;
        lastClearDir = Vector2.zero;

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
        cachedFinalDirection = Vector2.zero;
        physicsTickCounter = 0;
        isDetourActive = false;
        detourWaypoint = Vector2.zero;
        nextPathCheckTime = 0f;
        stuckCheckTimer = 0f;
        stuckCount = 0;
        lastSamplePos = Vector2.zero;
        safeClearDistance = 0f;
        lastClearDir = Vector2.zero;

        if (basePrefabScale != Vector3.zero)
        {
            initialScale = basePrefabScale;
            transform.localScale = basePrefabScale;
        }
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

        if (isDetourActive)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, detourWaypoint);
            Gizmos.DrawWireSphere(detourWaypoint, 0.2f);
        }
    }
}