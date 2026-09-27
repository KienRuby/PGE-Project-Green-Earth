using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trạng thái hoạt động của Nắm Đấm Phản Lực:
/// 1. Orbiting: Nắm đấm bay lượn vòng tròn xung quanh Player với tốc độ vừa phải.
/// 2. Launched: Khi phát hiện quái vật, nắm đấm phóng vút ra và BẺ LÁI ÔM CUA NHƯ XE ĐUA (Car-like drift steering)
///    uốn lượn hình vòng cung đuổi theo quái vật, và tự động chuyển hướng nếu quái vật cũ bị tiêu diệt!
/// </summary>
public enum RocketPunchState
{
    Orbiting,
    Launched
}

[RequireComponent(typeof(Rigidbody2D))]
public class RocketPunchProjectile : MonoBehaviour, IPoolable
{
    [Header("Combat Stats")]
    [Tooltip("Sát thương trực tiếp lên mục tiêu va chạm.")]
    [SerializeField] private int directDamage = 70;

    [Tooltip("Sát thương nổ diện rộng (AoE).")]
    [SerializeField] private int aoeDamage = 37;

    [Tooltip("Bán kính vụ nổ (mét).")]
    [SerializeField] private float aoeRadius = 1.25f;

    [Tooltip("Vận tốc bay khi lao tới quái vật (mét/giây).")]
    [SerializeField] private float launchSpeed = 12.0f;

    [Tooltip("Thời gian tồn tại tối đa sau khi phóng trước khi tự nổ nếu không còn quái (giây).")]
    [SerializeField] private float maxFlightTime = 5.0f;

    [Header("Orbit Settings (Xoay quanh Player)")]
    [Tooltip("Bán kính vòng quay xung quanh Player (mét).")]
    [SerializeField] private float orbitRadius = 0.8f;

    [Tooltip("Tốc độ bay xoay vòng quanh Player (độ/giây).")]
    [SerializeField] private float orbitSpeed = 220f;

    [Header("Car-like Steering Dynamics (Bẻ lái ôm cua như xe)")]
    [Tooltip("Tốc độ bẻ lái ôm cua (độ/giây). Càng nhỏ ôm cua hình vòng cung càng rộng mượt mà, càng lớn bẻ lái càng gắt.")]
    [Range(120f, 1080f)]
    [SerializeField] private float steeringTurnRate = 420f;

    [Header("Special Perks (Cấp 4 - Cấp 5)")]
    [Tooltip("Làm choáng quái vật sống sót trong bán kính nổ (Cấp 4+).")]
    [SerializeField] private bool hasStun = false;

    [Tooltip("Thời gian làm choáng (giây).")]
    [SerializeField] private float stunDuration = 1.0f;

    [Tooltip("Để lại vùng dung nham thiêu đốt tại tâm vụ nổ (Cấp 5 Tối thượng).")]
    [SerializeField] private bool hasLavaPool = false;

    [Header("Prefabs & VFX")]
    [Tooltip("Prefab hiệu ứng nổ (VFX Boom.prefab).")]
    [SerializeField] private GameObject explosionVfxPrefab;

    [Tooltip("Prefab vùng dung nham thiêu đốt (LavaHazardZone).")]
    [SerializeField] private GameObject lavaHazardPrefab;

    [Header("Renderers & Trail Scaling")]
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("Tỷ lệ độ dày của vệt đuôi lửa so với chiều cao nắm đấm.")]
    [Range(0.1f, 2.0f)]
    [SerializeField] private float trailWidthRatio = 0.6f;

    private Rigidbody2D rb;
    private Transform playerTransform;
    private RocketPunchState state = RocketPunchState.Orbiting;
    private float currentOrbitAngle = 0f;
    private float currentFlightAngle = 0f;
    private float flightTimer;
    private bool hasExploded = false;
    private System.Action onPunchLaunchedOrDespawned;

    private Transform currentTargetEnemy;
    private PlayerAutoShooter sharedTargetProvider;
    private readonly HashSet<int> hitEnemiesInExplosion = new HashSet<int>();
    private static Material sharedFallbackTrailMaterial;
    private static readonly Collider2D[] sharedExplosionBuffer = new Collider2D[64];
    private static readonly RaycastHit2D[] SharedCastBuffer = new RaycastHit2D[16];
    private static int hitLayerMask = 0;
    private static int HitLayerMask
    {
        get
        {
            if (hitLayerMask == 0)
            {
                hitLayerMask = LayerMask.GetMask("Enemy", "Obstacle");
                if (hitLayerMask == 0) hitLayerMask = LayerMask.GetMask("Default");
            }
            return hitLayerMask;
        }
    }

    private static int obstacleLayerMask = -1;
    private static int ObstacleLayerMask
    {
        get
        {
            if (obstacleLayerMask == -1)
            {
                obstacleLayerMask = LayerMask.GetMask("Obstacle");
                if (obstacleLayerMask == 0) obstacleLayerMask = LayerMask.GetMask("Default");
            }
            return obstacleLayerMask;
        }
    }

    private static int obstacleLayerIndex = -1;
    private static int ObstacleLayerIndex
    {
        get
        {
            if (obstacleLayerIndex == -1)
            {
                obstacleLayerIndex = LayerMask.NameToLayer("Obstacle");
            }
            return obstacleLayerIndex;
        }
    }

    public RocketPunchState State => state;

    private CircleCollider2D circleCol;
    private float cachedCastRadius = 0.28f;
    public float CachedCastRadius => cachedCastRadius;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.isKinematic = true;

        circleCol = GetComponent<CircleCollider2D>();
        if (circleCol != null)
        {
            cachedCastRadius = Mathf.Max(0.28f, circleCol.radius * Mathf.Abs(transform.lossyScale.x));
        }
        else
        {
            cachedCastRadius = 0.28f;
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        if (trailRenderer == null)
        {
            trailRenderer = GetComponentInChildren<TrailRenderer>();
        }

        FixTrailMaterial();
        UpdateTrailScale();
    }

    private void OnEnable()
    {
        UpdateTrailScale();
    }

    /// <summary>
    /// Tự động co giãn (scale) kích thước vệt đuôi lửa theo đúng tỷ lệ kích thước của nắm đấm.
    /// </summary>
    public void UpdateTrailScale()
    {
        if (trailRenderer == null) return;

        float visualHeight = 0.5f;
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Bounds b = spriteRenderer.sprite.bounds;
            visualHeight = b.size.y * Mathf.Abs(transform.lossyScale.y);
        }
        else
        {
            float maxScale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
            visualHeight = 0.5f * maxScale;
        }

        trailRenderer.startWidth = visualHeight * trailWidthRatio;
        trailRenderer.endWidth = visualHeight * trailWidthRatio * 0.15f;
        trailRenderer.widthMultiplier = 1.0f;

        if (trailRenderer.transform != transform)
        {
            float localOffset = (spriteRenderer != null && spriteRenderer.sprite != null)
                ? -spriteRenderer.sprite.bounds.extents.x * 0.85f
                : -0.2f;
            trailRenderer.transform.localPosition = new Vector3(localOffset, 0f, 0f);
        }
    }

    private void FixTrailMaterial()
    {
        if (trailRenderer != null)
        {
            if (trailRenderer.sharedMaterial == null || trailRenderer.sharedMaterial.shader == null || trailRenderer.sharedMaterial.shader.name == "Hidden/InternalErrorShader")
            {
                if (sharedFallbackTrailMaterial == null)
                {
                    Shader spriteShader = Shader.Find("Sprites/Default") 
                        ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                        ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended");

                    if (spriteShader != null)
                    {
                        sharedFallbackTrailMaterial = new Material(spriteShader);
                        sharedFallbackTrailMaterial.name = "RocketPunch_SharedTrailMaterial";
                    }
                }

                if (sharedFallbackTrailMaterial != null)
                {
                    trailRenderer.sharedMaterial = sharedFallbackTrailMaterial;
                }
            }
        }
    }

    /// <summary>
    /// Khởi tạo nắm đấm ở trạng thái bay xoay quanh Player.
    /// </summary>
    public void SetupOrbit(
        Transform player,
        int damage,
        int aoeDmg,
        float radius,
        float speed,
        float orbitDist,
        float orbitSpd,
        bool stunEnabled,
        float stunTime,
        bool lavaEnabled,
        GameObject explosionVfx,
        GameObject lavaPrefab,
        float startAngle,
        System.Action onLaunchedCallback)
    {
        playerTransform = player;
        directDamage = damage;
        aoeDamage = aoeDmg > 0 ? aoeDmg : Mathf.RoundToInt(damage * 0.55f);
        aoeRadius = Mathf.Max(0.5f, radius);
        launchSpeed = Mathf.Max(2.0f, speed);
        orbitRadius = Mathf.Max(0.2f, orbitDist);
        orbitSpeed = Mathf.Max(30f, orbitSpd);
        hasStun = stunEnabled;
        stunDuration = Mathf.Max(0.1f, stunTime);
        hasLavaPool = false; // Đã loại bỏ bãi dung nham theo GDD (Cấp 5 chỉ Stun 1s)

        if (explosionVfx != null) explosionVfxPrefab = explosionVfx;
        if (lavaPrefab != null) lavaHazardPrefab = lavaPrefab;

        currentOrbitAngle = startAngle;
        currentFlightAngle = currentOrbitAngle + 90f;
        state = RocketPunchState.Orbiting;
        hasExploded = false;
        currentTargetEnemy = null;
        onPunchLaunchedOrDespawned = onLaunchedCallback;

        FixTrailMaterial();
        UpdateTrailScale();

        if (trailRenderer != null)
        {
            trailRenderer.Clear();
        }

        UpdateOrbitPosition();
    }

    /// <summary>
    /// Phóng nắm đấm tới mục tiêu quái vật cụ thể với cơ chế bẻ lái ôm cua mượt mà.
    /// </summary>
    public void LaunchTowards(Transform target)
    {
        if (state == RocketPunchState.Launched || hasExploded) return;

        currentTargetEnemy = target;
        state = RocketPunchState.Launched;
        flightTimer = maxFlightTime;

        // Giữ nguyên góc tiếp tuyến hiện tại để bắt đầu bẻ lái vòng cung như xe ôm cua
        currentFlightAngle = currentOrbitAngle + 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, currentFlightAngle);

        onPunchLaunchedOrDespawned?.Invoke();
        onPunchLaunchedOrDespawned = null;
    }

    public void SetSharedTargetProvider(PlayerAutoShooter provider)
    {
        sharedTargetProvider = provider;
    }

    /// <summary>
    /// Khóa hướng và phóng nắm đấm tới vị trí quái vật với cơ chế bẻ lái ôm cua mượt mà.
    /// </summary>
    public void LaunchTowards(Vector2 targetPosition)
    {
        if (state == RocketPunchState.Launched || hasExploded) return;

        state = RocketPunchState.Launched;
        flightTimer = maxFlightTime;

        currentFlightAngle = currentOrbitAngle + 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, currentFlightAngle);

        onPunchLaunchedOrDespawned?.Invoke();
        onPunchLaunchedOrDespawned = null;
    }

    private void Update()
    {
        if (hasExploded) return;

        if (state == RocketPunchState.Orbiting)
        {
            if (playerTransform == null || !playerTransform.gameObject.activeInHierarchy)
            {
                Explode();
                return;
            }

            currentOrbitAngle += orbitSpeed * Time.deltaTime;
            if (currentOrbitAngle >= 360f) currentOrbitAngle -= 360f;

            UpdateOrbitPosition();
        }
        else if (state == RocketPunchState.Launched)
        {
            flightTimer -= Time.deltaTime;
            if (flightTimer <= 0f)
            {
                Explode();
            }
        }
    }

    private void UpdateOrbitPosition()
    {
        if (playerTransform == null) return;

        float rad = currentOrbitAngle * Mathf.Deg2Rad;
        Vector2 offset = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * orbitRadius;
        Vector3 newPos = playerTransform.position + (Vector3)offset;

        transform.position = newPos;

        float faceAngle = currentOrbitAngle + 90f;
        currentFlightAngle = faceAngle;
        transform.rotation = Quaternion.Euler(0f, 0f, faceAngle);
    }

    private void FixedUpdate()
    {
        if (hasExploded || state != RocketPunchState.Launched) return;

        // 1. KIỂM TRA MỤC TIÊU: Nếu mục tiêu hiện tại bị tiêu diệt, tự động quét tìm mục tiêu mới
        if (IsEnemyDead(currentTargetEnemy))
        {
            currentTargetEnemy = FindNewTarget();
        }

        // 2. CƠ CHẾ BẺ LÁI ÔM CUA NHƯ XE ĐUA (Car-like steering turn)
        // Khi ở xa (> 2.0m), giữ góc lượn vòng cung xe đua 420°/s đẹp mắt
        // Khi áp sát (< 2.0m), tốc độ bẻ lái tự động tăng vọt lên tới 1440°/s để khóa chặt mục tiêu khi quái chạy
        if (currentTargetEnemy != null && currentTargetEnemy.gameObject.activeInHierarchy)
        {
            EnemyHealth targetEh = currentTargetEnemy.GetComponentInParent<EnemyHealth>();
            Vector2 targetPos = targetEh != null ? targetEh.AimPoint : (Vector2)currentTargetEnemy.position;
            Vector2 toTarget = targetPos - (Vector2)rb.position;
            float distToTarget = toTarget.magnitude;

            if (distToTarget > 0.0001f)
            {
                Vector2 toTargetNorm = toTarget / distToTarget;
                float targetAngle = Mathf.Atan2(toTargetNorm.y, toTargetNorm.x) * Mathf.Rad2Deg;

                float effectiveTurnRate = steeringTurnRate;
                if (distToTarget < 2.0f)
                {
                    float t = 1f - (distToTarget / 2.0f);
                    effectiveTurnRate = Mathf.Lerp(steeringTurnRate, 1440f, t);
                }

                if (distToTarget < 0.35f)
                {
                    currentFlightAngle = targetAngle;
                }
                else
                {
                    currentFlightAngle = Mathf.MoveTowardsAngle(currentFlightAngle, targetAngle, effectiveTurnRate * Time.fixedDeltaTime);
                }
            }
        }

        // Cập nhật hướng xoay của sprite nắm đấm
        transform.rotation = Quaternion.Euler(0f, 0f, currentFlightAngle);

        // 3. DI CHUYỂN TIẾN VỀ PHÍA TRƯỚC THEO GÓC ĐÃ BẺ LÁI
        float moveRad = currentFlightAngle * Mathf.Deg2Rad;
        Vector2 moveDirection = new Vector2(Mathf.Cos(moveRad), Mathf.Sin(moveRad));
        float stepDist = launchSpeed * Time.fixedDeltaTime;
        Vector2 startPos = rb.position;
        Vector2 nextPos = startPos + moveDirection * stepDist;

        // 4. KIỂM TRA ÁP SÁT TRỰC TIẾP MỤC TIÊU HIỆN TẠI (Swept-Segment Proximity Check)
        // Đảm bảo không bao giờ trượt khi quái chạy ngang qua hoặc nắm đấm vừa lướt qua quái
        if (currentTargetEnemy != null && currentTargetEnemy.gameObject.activeInHierarchy)
        {
            EnemyHealth targetEh = currentTargetEnemy.GetComponentInParent<EnemyHealth>();
            if (targetEh != null && !targetEh.IsDead && targetEh.gameObject.activeInHierarchy)
            {
                Vector2 targetPos = targetEh.AimPoint;
                float closestDistToSegment = DistancePointToSegment(targetPos, startPos, nextPos);
                float hitThreshold = Mathf.Max(0.5f, cachedCastRadius + 0.25f);

                if (closestDistToSegment <= hitThreshold)
                {
                    if (!Physics2D.Linecast(startPos, targetPos, ObstacleLayerMask))
                    {
                        Collider2D targetCol = targetEh.GetComponentInChildren<Collider2D>() ?? targetEh.GetComponent<Collider2D>();
                        if (ResolveHit(targetCol, targetPos))
                        {
                            return;
                        }
                    }
                }
            }
        }

        // 5. QUÉT TIA CONTINUOUS COLLISION DETECTION (Enemy + Obstacle)
        if (stepDist > 0f)
        {
            int hitCount = Physics2D.CircleCastNonAlloc(startPos, cachedCastRadius, moveDirection, SharedCastBuffer, stepDist, HitLayerMask);
            if (hitCount > 0)
            {
                // Sắp xếp tăng dần theo khoảng cách (First Contact First Hit)
                for (int i = 0; i < hitCount - 1; i++)
                {
                    int minIdx = i;
                    for (int j = i + 1; j < hitCount; j++)
                    {
                        if (SharedCastBuffer[j].distance < SharedCastBuffer[minIdx].distance)
                        {
                            minIdx = j;
                        }
                    }
                    if (minIdx != i)
                    {
                        var temp = SharedCastBuffer[i];
                        SharedCastBuffer[i] = SharedCastBuffer[minIdx];
                        SharedCastBuffer[minIdx] = temp;
                    }
                }

                for (int i = 0; i < hitCount; i++)
                {
                    RaycastHit2D hit = SharedCastBuffer[i];
                    if (hit.collider == null) continue;

                    Vector2 hitPoint = hit.point != Vector2.zero ? hit.point : (startPos + moveDirection * hit.distance);
                    if (ResolveHit(hit.collider, hitPoint))
                    {
                        return;
                    }
                }
            }
        }

        rb.MovePosition(nextPos);
    }

    public static float DistancePointToSegment(Vector2 point, Vector2 segA, Vector2 segB)
    {
        Vector2 ab = segB - segA;
        float abSqr = ab.sqrMagnitude;
        if (abSqr < 0.000001f)
        {
            return Vector2.Distance(point, segA);
        }

        float t = Mathf.Clamp01(Vector2.Dot(point - segA, ab) / abSqr);
        Vector2 proj = segA + t * ab;
        return Vector2.Distance(point, proj);
    }

    private bool IsEnemyDead(Transform enemy)
    {
        if (enemy == null || !enemy.gameObject.activeInHierarchy) return true;
        EnemyHealth health = enemy.GetComponentInParent<EnemyHealth>();
        return health == null || health.IsDead || !health.gameObject.activeInHierarchy;
    }

    /// <summary>
    /// Tự động quét tìm quái vật sống gần nhất để chuyển hướng tấn công.
    /// </summary>
    private Transform FindNewTarget()
    {
        if (sharedTargetProvider == null) return null;
        Transform sharedTarget = sharedTargetProvider.CurrentTarget;
        return IsEnemyDead(sharedTarget) ? null : sharedTarget;
    }

    private bool ResolveHit(Collider2D hitCollider, Vector2 hitPoint)
    {
        if (hasExploded) return false;
        if (hitCollider != null && (hitCollider.CompareTag("Player") || hitCollider.CompareTag("BulletPlayer"))) return false;

        // 1. Chướng ngại vật (Obstacle)
        if (hitCollider != null && ((ObstacleLayerIndex != -1 && hitCollider.gameObject.layer == ObstacleLayerIndex) || hitCollider.CompareTag("Obstacle")))
        {
            if (!hitCollider.isTrigger)
            {
                transform.position = hitPoint;
                if (rb != null) rb.position = hitPoint;
                Explode();
                return true;
            }
            return false;
        }

        // 2. Kẻ địch (Enemy)
        IDamageable damageable = hitCollider != null ? hitCollider.GetComponentInParent<IDamageable>() : null;
        if (damageable != null)
        {
            if (damageable is EnemyHealth enemyHealth && (enemyHealth.IsDead || !enemyHealth.gameObject.activeInHierarchy)) return false;

            transform.position = hitPoint;
            if (rb != null) rb.position = hitPoint;

            if (damageable is EnemyHealth)
                AudioManager.Instance?.PlaySFX(SoundIdConst.SFX_PUNCH_HIT);
            damageable.TakeDamage(directDamage);
            ChipsetBattleStats.RecordDamage(3, directDamage);
            Explode();
            return true;
        }

        // 3. Trúng mục tiêu đang bám đuổi (khi hitCollider là null từ kiểm tra áp sát Proximity)
        if (currentTargetEnemy != null && currentTargetEnemy.gameObject.activeInHierarchy)
        {
            EnemyHealth targetEh = currentTargetEnemy.GetComponentInParent<EnemyHealth>();
            if (targetEh != null && !targetEh.IsDead && targetEh.gameObject.activeInHierarchy)
            {
                transform.position = hitPoint;
                if (rb != null) rb.position = hitPoint;
                AudioManager.Instance?.PlaySFX(SoundIdConst.SFX_PUNCH_HIT);
                targetEh.TakeDamage(directDamage);
                ChipsetBattleStats.RecordDamage(3, directDamage);
                Explode();
                return true;
            }
        }

        if (state == RocketPunchState.Launched && hitCollider != null && !hitCollider.isTrigger)
        {
            transform.position = hitPoint;
            if (rb != null) rb.position = hitPoint;
            Explode();
            return true;
        }

        return false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasExploded || other == null) return;
        ResolveHit(other, transform.position);
    }

    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        Vector3 explosionPos = transform.position;

        if (explosionVfxPrefab != null)
        {
            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Spawn(explosionVfxPrefab, explosionPos, Quaternion.identity);
            }
            else
            {
                Instantiate(explosionVfxPrefab, explosionPos, Quaternion.identity);
            }
        }

        hitEnemiesInExplosion.Clear();
        int hitCount = Physics2D.OverlapCircleNonAlloc(explosionPos, aoeRadius, sharedExplosionBuffer);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = sharedExplosionBuffer[i];
            if (col == null) continue;

            EnemyHealth enemy = col.GetComponentInParent<EnemyHealth>();
            if (enemy != null && !enemy.IsDead && enemy.gameObject.activeInHierarchy)
            {
                // Check Line of Sight through obstacles
                if (Physics2D.Linecast(explosionPos, enemy.transform.position, ObstacleLayerMask))
                {
                    continue; // Sát thương nổ bị vật cản che chắn
                }

                int enemyId = enemy.GetInstanceID();
                if (!hitEnemiesInExplosion.Add(enemyId))
                {
                    continue;
                }

                enemy.TakeDamage(aoeDamage);
                ChipsetBattleStats.RecordDamage(3, aoeDamage);

                if (hasStun)
                {
                    EnemyMovement movement = enemy.GetComponent<EnemyMovement>();
                    if (movement != null)
                    {
                        movement.ApplyStun(stunDuration);
                    }
                }
            }
        }

        if (hasLavaPool)
        {
            SpawnLavaPool(explosionPos);
        }

        Despawn();
    }

    private void SpawnLavaPool(Vector3 position)
    {
        GameObject lavaObj = null;
        if (lavaHazardPrefab != null)
        {
            if (PoolManager.Instance != null)
            {
                lavaObj = PoolManager.Instance.Spawn(lavaHazardPrefab, position, Quaternion.identity);
            }
            else
            {
                lavaObj = Instantiate(lavaHazardPrefab, position, Quaternion.identity);
            }
        }
        else
        {
            lavaObj = new GameObject("LavaHazardZone_Instance");
            lavaObj.transform.position = position;
            lavaObj.AddComponent<LavaHazardZone>();
        }

        if (lavaObj != null)
        {
            LavaHazardZone zone = lavaObj.GetComponent<LavaHazardZone>();
            if (zone != null)
            {
                int tickDmg = Mathf.Max(10, Mathf.RoundToInt(directDamage * 0.15f));
                zone.Initialize(tickDmg, aoeRadius * 0.8f, 3.0f);
            }
        }
    }

    private void Despawn()
    {
        onPunchLaunchedOrDespawned?.Invoke();
        onPunchLaunchedOrDespawned = null;
        currentTargetEnemy = null;
        sharedTargetProvider = null;

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
        hasExploded = false;
        state = RocketPunchState.Orbiting;
        currentTargetEnemy = null;
        sharedTargetProvider = null;
        FixTrailMaterial();
        UpdateTrailScale();
        if (trailRenderer != null)
        {
            trailRenderer.Clear();
        }
    }

    public void OnReturnToPool()
    {
        hasExploded = true;
        currentTargetEnemy = null;
        sharedTargetProvider = null;
        onPunchLaunchedOrDespawned = null;
        hitEnemiesInExplosion.Clear();
        if (trailRenderer != null)
        {
            trailRenderer.Clear();
        }
    }
}
