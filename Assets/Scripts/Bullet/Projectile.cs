using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour, IPoolable
{
    [Header("Projectile Settings")]
    [Tooltip("Tốc độ bay của viên đạn.")]
    [SerializeField] private float moveSpeed = 10f;

    [Tooltip("Lượng sát thương gây ra khi trúng mục tiêu.")]
    [SerializeField] private int damage = 20;

    [Tooltip("Thời gian tồn tại tối đa của đạn trước khi tự thu hồi về Pool (giây).")]
    [SerializeField] private float lifeTime = 5f;

    [Header("Homing Settings (Đạn đuổi mục tiêu)")]
    [Tooltip("Bật tính năng đạn tự động bẻ lái đuổi theo quái vật (Homing). Tắt (Mặc định) = Đạn bay thẳng theo hướng bắn, không đuổi theo quái.")]
    [SerializeField] private bool isHoming = false;

    [Tooltip("Tốc độ bẻ lái đuổi theo mục tiêu khi bật isHoming (độ/giây).")]
    [SerializeField] private float homingRotateSpeed = 360f;

    [Header("Explosion Settings (Đạn Nổ Diện Rộng)")]
    [Tooltip("Bật tính năng đạn nổ gây sát thương diện rộng khi trúng mục tiêu.")]
    [SerializeField] private bool isExplosive = false;

    [Tooltip("Bán kính nổ gây sát thương (mét).")]
    [SerializeField] private float explosionRadius = 2.0f;

    [Tooltip("Prefab hiệu ứng nổ (VFX Boom).")]
    [SerializeField] private GameObject explosionVfxPrefab;

    [Header("Hit VFX Settings")]
    [Tooltip("Prefab hiệu ứng trúng đạn nổ tại điểm va chạm (Hit VFX).")]
    [SerializeField] private GameObject hitVfxPrefab;

    public GameObject HitVfxPrefab
    {
        get => hitVfxPrefab;
        set => hitVfxPrefab = value;
    }

    [Header("Perk Attributes")]
    [SerializeField] private float extraLifeStealPercent = 0f;
    [SerializeField] private bool canRicochet = false;
    [SerializeField] private float ricochetChance = 0f;
    [SerializeField] private int maxRicochetCount = 1;
    [SerializeField] private float ricochetRadius = 6.0f;

    private Rigidbody2D rb;
    private Vector2 moveDirection;
    private float lifeTimer;
    private Transform targetEnemy;
    private int sourceChipsetId;
    private int currentRicochetRemaining;
    private readonly HashSet<int> hitEnemyIds = new HashSet<int>();
    private bool isDespawning = false;
    private SpriteRenderer spriteRenderer;
    private Color defaultColor = Color.white;
    private bool hasCachedColor = false;
    private static readonly Collider2D[] SharedOverlapBuffer = new Collider2D[64];
    private static readonly RaycastHit2D[] SharedCastBuffer = new RaycastHit2D[16];
    private static int obstacleLayerMask = -1;
    private static int hitLayerMask = 0;

    private static int HitLayerMask
    {
        get
        {
            if (hitLayerMask == 0)
            {
                hitLayerMask = LayerMask.GetMask("Enemy", "Obstacle");
                if (hitLayerMask == 0)
                {
                    hitLayerMask = LayerMask.GetMask("Default");
                }
            }
            return hitLayerMask;
        }
    }

    private static int ObstacleLayerMask
    {
        get
        {
            if (obstacleLayerMask == -1)
            {
                obstacleLayerMask = LayerMask.GetMask("Obstacle");
                if (obstacleLayerMask == 0)
                {
                    obstacleLayerMask = LayerMask.GetMask("Default");
                }
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

    public bool IsHoming
    {
        get => isHoming;
        set => isHoming = value;
    }

    public int Damage => damage;
    public float MoveSpeed => moveSpeed;
    public bool IsCritical { get; set; }

    public void SetupPerks(float extraLifeSteal, bool ricochet, float ricochetProb)
    {
        extraLifeStealPercent = extraLifeSteal;
        canRicochet = ricochet;
        ricochetChance = ricochetProb;
        currentRicochetRemaining = ricochet ? maxRicochetCount : 0;
        hitEnemyIds.Clear();
    }

    private CircleCollider2D circleCol;
    private float cachedCastRadius = 0.12f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.interpolation = RigidbodyInterpolation2D.None;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.useFullKinematicContacts = true;
        }

        circleCol = GetComponent<CircleCollider2D>();
        if (circleCol != null)
        {
            cachedCastRadius = Mathf.Max(0.05f, circleCol.radius * Mathf.Abs(transform.lossyScale.x));
        }

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            defaultColor = spriteRenderer.color;
            hasCachedColor = true;
        }

        if (hitVfxPrefab == null)
        {
            hitVfxPrefab = Resources.Load<GameObject>("Prefabs/PlayerHitVFX");
#if UNITY_EDITOR
            if (hitVfxPrefab == null)
            {
                hitVfxPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PlayerHitVFX.prefab");
            }
#endif
        }
    }

    private void OnEnable()
    {
        lifeTimer = lifeTime;
        currentRicochetRemaining = canRicochet ? maxRicochetCount : 0;
        hitEnemyIds.Clear();
        isDespawning = false;
    }

    private void Update()
    {
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            Despawn();
        }
    }

    /// <summary>
    /// Nhận và cài đặt các chỉ số động từ khẩu súng (Sát thương, Tốc độ đạn, Tầm bắn).
    /// </summary>
    public void Setup(int damageAmount, float speed, float maxRange)
    {
        damage = damageAmount;
        moveSpeed = speed;

        if (speed > 0f && maxRange > 0f)
        {
            lifeTime = maxRange / speed;
        }
        lifeTimer = lifeTime;
    }

    public void SetDirection(Vector2 direction)
    {
        moveDirection = direction.normalized;
        RotateProjectile();
    }

    public void SetTarget(Transform target)
    {
        targetEnemy = target;
    }

    public void SetDamageSource(int chipsetId)
    {
        sourceChipsetId = Mathf.Max(0, chipsetId);
    }

    private void FixedUpdate()
    {
        // Nếu bật chế độ đạn đuổi (isHoming) và mục tiêu còn sống
        if (isHoming && targetEnemy != null && targetEnemy.gameObject.activeInHierarchy)
        {
            Vector2 targetDirection = ((Vector2)targetEnemy.position - (Vector2)transform.position).normalized;
            float rotateAmount = Vector3.Cross(moveDirection, targetDirection).z;

            moveDirection = Quaternion.Euler(0f, 0f, rotateAmount * homingRotateSpeed * Time.fixedDeltaTime) * moveDirection;
            moveDirection.Normalize();
            RotateProjectile();
        }

        float stepDist = moveSpeed * Time.fixedDeltaTime;
        Vector2 currentPos = rb != null ? rb.position : (Vector2)transform.position;

        // Quét liên tục kiểm tra va chạm vật thể đầu tiên (Enemy + Obstacle)
        if (moveDirection.sqrMagnitude > 0.001f && stepDist > 0f)
        {
            int hitCount = Physics2D.CircleCastNonAlloc(currentPos, cachedCastRadius, moveDirection, SharedCastBuffer, stepDist, HitLayerMask);
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

                    Vector2 hitPoint = hit.point != Vector2.zero ? hit.point : (currentPos + moveDirection * hit.distance);
                    if (ResolveHit(hit.collider, hitPoint))
                    {
                        return;
                    }
                }
            }
        }

        if (rb != null)
        {
            Vector2 nextPos = rb.position + moveDirection * stepDist;
            rb.MovePosition(nextPos);
        }
        else
        {
            transform.position += (Vector3)(moveDirection * stepDist);
        }
    }

    private void RotateProjectile()
    {
        if (moveDirection == Vector2.zero)
            return;

        float angle = Mathf.Atan2(
            moveDirection.y,
            moveDirection.x
        ) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(
            0f,
            0f,
            angle
        );
    }

    public void SetExplosive(bool explosive, float radius = 2.0f, GameObject vfx = null)
    {
        isExplosive = explosive;
        explosionRadius = Mathf.Max(0.5f, radius);
        if (vfx != null) explosionVfxPrefab = vfx;
    }

    private bool ResolveHit(Collider2D hitCollider, Vector2 hitPoint)
    {
        if (isDespawning || hitCollider == null) return false;

        // Tránh bắn trúng Player hoặc các viên đạn khác (Fast Path Tag Check)
        if (hitCollider.CompareTag("Player") || hitCollider.CompareTag("BulletPlayer"))
        {
            return false;
        }

        // 1. Chướng ngại vật (Obstacle)
        bool isObstacle = (ObstacleLayerIndex != -1 && hitCollider.gameObject.layer == ObstacleLayerIndex);
        if (!isObstacle)
        {
            try { isObstacle = hitCollider.CompareTag("Obstacle"); }
            catch { }
        }

        if (isObstacle)
        {
            if (!hitCollider.isTrigger)
            {
                transform.position = hitPoint;
                if (rb != null) rb.position = hitPoint;
                if (isExplosive) Explode();
                else SpawnHitVfx(hitPoint);
                isDespawning = true;
                Despawn();
                return true;
            }
            return false;
        }

        // 2. Kẻ địch (Enemy)
        IDamageable damageable = hitCollider.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            EnemyHealth eh = damageable as EnemyHealth;
            if (eh != null)
            {
                if (eh.IsDead || !eh.gameObject.activeInHierarchy) return false;
                int enemyId = eh.gameObject.GetInstanceID();
                if (hitEnemyIds.Contains(enemyId)) return false;
                hitEnemyIds.Add(enemyId);
            }

            transform.position = hitPoint;
            if (rb != null) rb.position = hitPoint;

            if (isExplosive)
            {
                Explode();
            }
            else
            {
                SpawnHitVfx(hitPoint);
                if (eh != null)
                {
                    eh.TakeDamage(damage, IsCritical);
                }
                else
                {
                    damageable.TakeDamage(damage);
                }

                ChipsetBattleStats.RecordDamage(sourceChipsetId, damage);
                EnergyJumperCablesSkill.TriggerLifeSteal(damage, isMainWeapon: true);

                if (extraLifeStealPercent > 0f)
                {
                    PlayerHealth playerHealth = PlayerHealth.Instance != null ? PlayerHealth.Instance : FindObjectOfType<PlayerHealth>();
                    if (playerHealth != null && !playerHealth.IsDead)
                    {
                        int heal = Mathf.Max(1, Mathf.RoundToInt(damage * extraLifeStealPercent));
                        playerHealth.Heal(heal);
                    }
                }

                if (canRicochet && currentRicochetRemaining > 0 && Random.value < ricochetChance)
                {
                    currentRicochetRemaining--;
                    Transform nextTarget = FindNextRicochetTarget(transform.position);
                    if (nextTarget != null)
                    {
                        EnemyHealth nextEh = nextTarget.GetComponentInParent<EnemyHealth>();
                        Vector2 targetPos = nextEh != null ? nextEh.AimPoint : (Vector2)nextTarget.position;
                        Vector2 nextDir = (targetPos - (Vector2)transform.position).normalized;
                        SetDirection(nextDir);
                        SetTarget(nextTarget);
                        lifeTimer = lifeTime;
                        return true;
                    }
                }
            }

            isDespawning = true;
            Despawn();
            return true;
        }

        // Tự hủy nếu đạn đâm vào tường hoặc vật cản vật lý (không phải trigger)
        if (!hitCollider.isTrigger)
        {
            if (isExplosive) Explode();
            else SpawnHitVfx(hitPoint);
            isDespawning = true;
            Despawn();
            return true;
        }

        return false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDespawning || other == null) return;
        Vector2 hitPoint = other.ClosestPoint(transform.position);
        if (hitPoint == Vector2.zero && ((Vector2)transform.position) != Vector2.zero)
        {
            hitPoint = (Vector2)transform.position;
        }
        ResolveHit(other, hitPoint);
    }

    private void SpawnHitVfx(Vector2 position)
    {
        GameObject vfxToSpawn = hitVfxPrefab != null ? hitVfxPrefab : explosionVfxPrefab;
        if (vfxToSpawn == null) return;

        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.Spawn(vfxToSpawn, position, Quaternion.identity);
        }
        else
        {
            Instantiate(vfxToSpawn, position, Quaternion.identity);
        }
    }

    private void Explode()
    {
        if (explosionVfxPrefab != null)
        {
            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Spawn(explosionVfxPrefab, transform.position, Quaternion.identity);
            }
            else
            {
                Instantiate(explosionVfxPrefab, transform.position, Quaternion.identity);
            }
        }

        Vector2 explosionCenter = transform.position;
        int hitCount = Physics2D.OverlapCircleNonAlloc(explosionCenter, explosionRadius, SharedOverlapBuffer);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = SharedOverlapBuffer[i];
            if (col == null) continue;

            EnemyHealth enemy = col.GetComponentInParent<EnemyHealth>();
            if (enemy != null && !enemy.IsDead && enemy.gameObject.activeInHierarchy)
            {
                // Check Line of Sight through obstacles
                if (Physics2D.Linecast(explosionCenter, enemy.transform.position, ObstacleLayerMask))
                {
                    continue; // Sát thương nổ bị vật cản che chắn
                }

                enemy.TakeDamage(damage, IsCritical);
                ChipsetBattleStats.RecordDamage(sourceChipsetId, damage);
                EnergyJumperCablesSkill.TriggerLifeSteal(damage, isMainWeapon: true);
            }
        }
    }

    private static int enemyLayerMask = 0;
    private static int EnemyLayerMask
    {
        get
        {
            if (enemyLayerMask == 0)
            {
                enemyLayerMask = LayerMask.GetMask("Enemy");
                if (enemyLayerMask == 0) enemyLayerMask = 1 << 7;
            }
            return enemyLayerMask;
        }
    }

    private Transform FindNextRicochetTarget(Vector3 origin)
    {
        int count = Physics2D.OverlapCircleNonAlloc(origin, ricochetRadius, SharedOverlapBuffer, EnemyLayerMask);
        Transform bestBoss = null;
        float minBossDist = float.MaxValue;
        Transform bestNormal = null;
        float minNormalDist = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider2D col = SharedOverlapBuffer[i];
            if (col == null) continue;
            EnemyHealth eh = col.GetComponentInParent<EnemyHealth>();
            if (eh != null && !eh.IsDead && !hitEnemyIds.Contains(eh.gameObject.GetInstanceID()))
            {
                float dist = Vector2.Distance(origin, eh.AimPoint);
                if (eh.IsBoss)
                {
                    if (dist < minBossDist)
                    {
                        minBossDist = dist;
                        bestBoss = eh.transform;
                    }
                }
                else
                {
                    if (dist < minNormalDist)
                    {
                        minNormalDist = dist;
                        bestNormal = eh.transform;
                    }
                }
            }
        }
        return bestBoss != null ? bestBoss : bestNormal;
    }

    private void Despawn()
    {
        PoolMember member = GetComponent<PoolMember>();
        if (member != null && member.Pool != null)
        {
            member.ReturnToPool();
        }
        else if (PoolManager.Instance != null)
        {
            PoolManager.Instance.ReturnToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ResetCustomModifiers()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer != null && hasCachedColor)
        {
            spriteRenderer.color = defaultColor;
        }

        var frostEffect = GetComponent<SloyFrostBuddy.FrostHitEffect>();
        if (frostEffect != null)
        {
            Destroy(frostEffect);
        }

        isHoming = false;
        isExplosive = false;
        IsCritical = false;
        targetEnemy = null;
        sourceChipsetId = 0;
        extraLifeStealPercent = 0f;
        canRicochet = false;
        ricochetChance = 0f;
        currentRicochetRemaining = 0;
        hitEnemyIds.Clear();
        isDespawning = false;
    }

    public void OnSpawnFromPool()
    {
        lifeTimer = lifeTime;
        moveDirection = Vector2.zero;
        targetEnemy = null;
        sourceChipsetId = 0;
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
        ResetCustomModifiers();
    }

    public void OnReturnToPool()
    {
        moveDirection = Vector2.zero;
        targetEnemy = null;
        sourceChipsetId = 0;
        isExplosive = false;
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
        ResetCustomModifiers();
    }
}
