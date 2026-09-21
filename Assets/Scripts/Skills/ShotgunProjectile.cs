using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mảnh đạn của kỹ năng Shotgun (Súng Săn - Chipset ID 8).
/// Hỗ trợ: Sát thương từng mảnh, Xuyên thấu toàn bộ mục tiêu (Cấp 3+),
/// và Đẩy lùi (Knockback) cực mạnh (Cấp 4+).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ShotgunProjectile : MonoBehaviour, IPoolable
{
    [Header("Projectile Base Settings")]
    [SerializeField] private float moveSpeed = 15f;
    [SerializeField] private int damage = 18;
    [SerializeField] private float lifeTime = 0.65f;

    [Header("Special Attributes")]
    [SerializeField] private bool isPiercing = false;
    [SerializeField] private float knockbackForce = 0f;

    private Rigidbody2D rb;
    private Vector2 moveDirection;
    private float lifeTimer;
    private readonly HashSet<int> hitEnemyInstanceIds = new HashSet<int>();
    private bool isDespawning = false;
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
    public float MoveSpeed => moveSpeed;

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
    }

    private void OnEnable()
    {
        lifeTimer = lifeTime;
        hitEnemyInstanceIds.Clear();
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
        bool piercing,
        float knockback)
    {
        damage = damageAmount;
        moveSpeed = speed;
        isPiercing = piercing;
        knockbackForce = knockback;

        if (speed > 0f && maxRange > 0f)
        {
            lifeTime = maxRange / speed;
        }
        lifeTimer = lifeTime;
        hitEnemyInstanceIds.Clear();
    }

    public void SetDirection(Vector2 direction)
    {
        moveDirection = direction.normalized;
        RotateProjectile();
    }

    private void RotateProjectile()
    {
        if (moveDirection == Vector2.zero) return;

        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private bool ResolveHit(Collider2D hitCollider, Vector2 hitPoint)
    {
        if (isDespawning || hitCollider == null) return false;
        if (hitCollider.CompareTag("Player") || hitCollider.CompareTag("BulletPlayer")) return false;

        // 1. Chướng ngại vật (Obstacle) - Không thể xuyên qua
        if ((ObstacleLayerIndex != -1 && hitCollider.gameObject.layer == ObstacleLayerIndex) || hitCollider.CompareTag("Obstacle"))
        {
            if (!hitCollider.isTrigger)
            {
                transform.position = hitPoint;
                if (rb != null) rb.position = hitPoint;
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
            EnemyHealth enemyHealth = damageable as EnemyHealth;
            if (enemyHealth != null)
            {
                if (enemyHealth.IsDead || !enemyHealth.gameObject.activeInHierarchy) return false;

                int enemyId = enemyHealth.gameObject.GetInstanceID();
                if (hitEnemyInstanceIds.Contains(enemyId)) return false;
                hitEnemyInstanceIds.Add(enemyId);

                // 1. Áp dụng hiệu ứng Đẩy lùi (Knockback)
                if (knockbackForce > 0.01f)
                {
                    EnemyMovement movement = enemyHealth.GetComponent<EnemyMovement>();
                    if (movement != null)
                    {
                        movement.ApplyKnockback(moveDirection, knockbackForce, 0.22f);
                    }
                }
            }

            transform.position = hitPoint;
            if (rb != null) rb.position = hitPoint;

            damageable.TakeDamage(damage);

            // 2. Xuyên thấu toàn bộ mục tiêu (Pierce All)
            if (isPiercing)
            {
                return false; // Đạn tiếp tục bay xuyên qua
            }

            isDespawning = true;
            Despawn();
            return true;
        }

        if (!hitCollider.isTrigger)
        {
            isDespawning = true;
            Despawn();
            return true;
        }

        return false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDespawning || other == null) return;
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
        isDespawning = false;
        if (rb != null) rb.velocity = Vector2.zero;
    }

    public void OnReturnToPool()
    {
        moveDirection = Vector2.zero;
        hitEnemyInstanceIds.Clear();
        isPiercing = false;
        knockbackForce = 0f;
        isDespawning = false;
        if (rb != null) rb.velocity = Vector2.zero;

        Projectile baseProj = GetComponent<Projectile>();
        if (baseProj != null)
        {
            baseProj.enabled = true;
        }
        enabled = false;
    }
}
