using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Đạn của kỹ năng Multigun (Súng Đa Tia - Chipset ID 5).
/// Bay tốc độ cao, hỗ trợ cơ chế Bám đuổi nhẹ (Homing) ở Cấp 3+.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class MultigunProjectile : MonoBehaviour, IPoolable
{
    [Header("Base Settings")]
    [SerializeField] private float moveSpeed = 16f;
    [SerializeField] private int damage = 19;
    [SerializeField] private float lifeTime = 2.0f;

    [Header("Homing Settings")]
    [SerializeField] private bool isHoming = false;
    [SerializeField] private float homingRange = 8.0f;
    [SerializeField] private float homingSteerStrength = 6.0f;
    [SerializeField] private LayerMask enemyLayer;

    private Rigidbody2D rb;
    private Vector2 moveDirection;
    private float lifeTimer;
    private Transform homingTarget;
    private readonly HashSet<int> hitEnemyInstanceIds = new HashSet<int>();
    private readonly Collider2D[] enemyBuffer = new Collider2D[16];
    private ContactFilter2D contactFilter;
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

    public int Damage => damage;

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

    private void OnEnable()
    {
        lifeTimer = lifeTime;
        hitEnemyInstanceIds.Clear();
        homingTarget = null;
    }

    private void Update()
    {
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            Despawn();
            return;
        }

        if (isHoming)
        {
            UpdateHomingSteering();
        }
    }

    private void FixedUpdate()
    {
        float stepDist = moveSpeed * Time.fixedDeltaTime;
        Vector2 currentPos = rb != null ? rb.position : (Vector2)transform.position;

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

    public void Setup(
        int damageAmount,
        float speed,
        float maxRange,
        bool homingEnabled)
    {
        damage = damageAmount;
        moveSpeed = speed;
        isHoming = homingEnabled;

        if (speed > 0f && maxRange > 0f)
        {
            lifeTime = maxRange / speed;
        }
        lifeTimer = lifeTime;
        hitEnemyInstanceIds.Clear();
        homingTarget = null;
    }

    public void SetDirection(Vector2 direction)
    {
        moveDirection = direction.normalized;
        RotateProjectile();
    }

    private void UpdateHomingSteering()
    {
        if (homingTarget == null || !homingTarget.gameObject.activeInHierarchy)
        {
            homingTarget = FindHomingTarget();
        }

        if (homingTarget != null)
        {
            EnemyHealth targetEh = homingTarget.GetComponent<EnemyHealth>();
            Vector2 targetPos = targetEh != null ? targetEh.AimPoint : (Vector2)homingTarget.position;
            Vector2 currentPos = transform.position;
            Vector2 desiredDir = (targetPos - currentPos).normalized;

            moveDirection = Vector2.Lerp(moveDirection, desiredDir, Time.deltaTime * homingSteerStrength).normalized;
            RotateProjectile();
        }
    }

    private Transform FindHomingTarget()
    {
        int hitCount = Physics2D.OverlapCircle(transform.position, homingRange, contactFilter, enemyBuffer);
        Transform bestBoss = null;
        float minBossDistSqr = Mathf.Infinity;
        Transform bestNormal = null;
        float minNormalDistSqr = Mathf.Infinity;
        Vector2 currentPos = transform.position;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = enemyBuffer[i];
            if (col == null) continue;

            EnemyHealth enemy = col.GetComponentInParent<EnemyHealth>();
            if (enemy == null || enemy.IsDead || !enemy.gameObject.activeInHierarchy) continue;

            float distSqr = ((Vector2)enemy.AimPoint - currentPos).sqrMagnitude;
            if (enemy.IsBoss)
            {
                if (distSqr < minBossDistSqr)
                {
                    minBossDistSqr = distSqr;
                    bestBoss = enemy.transform;
                }
            }
            else
            {
                if (distSqr < minNormalDistSqr)
                {
                    minNormalDistSqr = distSqr;
                    bestNormal = enemy.transform;
                }
            }
        }

        return bestBoss != null ? bestBoss : bestNormal;
    }

    private void RotateProjectile()
    {
        if (moveDirection == Vector2.zero) return;

        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private bool ResolveHit(Collider2D hitCollider, Vector2 hitPoint)
    {
        if (hitCollider == null) return false;
        if (hitCollider.CompareTag("Player") || hitCollider.CompareTag("BulletPlayer")) return false;

        // 1. Chướng ngại vật (Obstacle) - Không thể xuyên qua
        if ((ObstacleLayerIndex != -1 && hitCollider.gameObject.layer == ObstacleLayerIndex) || hitCollider.CompareTag("Obstacle"))
        {
            if (!hitCollider.isTrigger)
            {
                transform.position = hitPoint;
                if (rb != null) rb.position = hitPoint;
                Despawn();
                return true;
            }
            return false;
        }

        // 2. Kẻ địch (Enemy)
        IDamageable damageable = hitCollider.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            EnemyHealth enemyHealth = damageable as EnemyHealth;
            if (enemyHealth != null)
            {
                if (enemyHealth.IsDead || !enemyHealth.gameObject.activeInHierarchy) return false;

                int enemyId = enemyHealth.gameObject.GetInstanceID();
                if (hitEnemyInstanceIds.Contains(enemyId)) return false;
                hitEnemyInstanceIds.Add(enemyId);
            }

            transform.position = hitPoint;
            if (rb != null) rb.position = hitPoint;

            damageable.TakeDamage(damage);
            EnergyJumperCablesSkill.TriggerLifeSteal(damage, false);

            Despawn();
            return true;
        }

        if (!hitCollider.isTrigger)
        {
            Despawn();
            return true;
        }

        return false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        ResolveHit(other, transform.position);
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

    public void OnSpawnFromPool()
    {
        lifeTimer = lifeTime;
        moveDirection = Vector2.zero;
        hitEnemyInstanceIds.Clear();
        homingTarget = null;
        if (rb != null) rb.velocity = Vector2.zero;
    }

    public void OnReturnToPool()
    {
        moveDirection = Vector2.zero;
        hitEnemyInstanceIds.Clear();
        homingTarget = null;
        isHoming = false;
        if (rb != null) rb.velocity = Vector2.zero;
    }
}
