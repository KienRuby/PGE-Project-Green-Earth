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
    [SerializeField] private float aoeRadius = 2.5f;

    [Tooltip("Vận tốc bay khi lao tới quái vật (mét/giây).")]
    [SerializeField] private float launchSpeed = 12.0f;

    [Tooltip("Thời gian tồn tại tối đa sau khi phóng trước khi tự nổ nếu không còn quái (giây).")]
    [SerializeField] private float maxFlightTime = 5.0f;

    [Header("Orbit Settings (Xoay quanh Player)")]
    [Tooltip("Bán kính vòng quay xung quanh Player (mét).")]
    [SerializeField] private float orbitRadius = 1.6f;

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

    public RocketPunchState State => state;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.isKinematic = true;

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
                Shader spriteShader = Shader.Find("Sprites/Default") 
                    ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                    ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended");

                if (spriteShader != null)
                {
                    trailRenderer.material = new Material(spriteShader);
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
        aoeRadius = Mathf.Max(1.0f, radius);
        launchSpeed = Mathf.Max(2.0f, speed);
        orbitRadius = Mathf.Max(0.5f, orbitDist);
        orbitSpeed = Mathf.Max(30f, orbitSpd);
        hasStun = stunEnabled;
        stunDuration = Mathf.Max(0.1f, stunTime);
        hasLavaPool = lavaEnabled;

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
        if (currentTargetEnemy != null && currentTargetEnemy.gameObject.activeInHierarchy)
        {
            Vector2 toTarget = ((Vector2)currentTargetEnemy.position - (Vector2)rb.position).normalized;
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                float targetAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
                // Bẻ lái từ từ ôm cua vòng cung mượt mà theo tốc độ góc steeringTurnRate
                currentFlightAngle = Mathf.MoveTowardsAngle(currentFlightAngle, targetAngle, steeringTurnRate * Time.fixedDeltaTime);
            }
        }

        // Cập nhật hướng xoay của sprite nắm đấm
        transform.rotation = Quaternion.Euler(0f, 0f, currentFlightAngle);

        // 3. DI CHUYỂN TIẾN VỀ PHÍA TRƯỚC THEO GÓC ĐÃ BẺ LÁI
        float moveRad = currentFlightAngle * Mathf.Deg2Rad;
        Vector2 moveDirection = new Vector2(Mathf.Cos(moveRad), Mathf.Sin(moveRad));

        Vector2 nextPos = rb.position + moveDirection * (launchSpeed * Time.fixedDeltaTime);
        rb.MovePosition(nextPos);
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasExploded || other == null) return;

        if (other.CompareTag("Player") || other.CompareTag("BulletPlayer")) return;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            if (damageable is EnemyHealth enemyHealth && enemyHealth.IsDead) return;

            damageable.TakeDamage(directDamage);
            ChipsetBattleStats.RecordDamage(3, directDamage);
            Explode();
            return;
        }

        if (state == RocketPunchState.Launched && !other.isTrigger)
        {
            Explode();
        }
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
        Collider2D[] colliders = Physics2D.OverlapCircleAll(explosionPos, aoeRadius);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D col = colliders[i];
            if (col == null) continue;

            EnemyHealth enemy = col.GetComponentInParent<EnemyHealth>();
            if (enemy != null && !enemy.IsDead && enemy.gameObject.activeInHierarchy)
            {
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
