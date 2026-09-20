using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Đạn của kỹ năng Rifle (Súng Trường - Chipset ID 2).
/// Hỗ trợ: Tốc độ cao, Xuyên thấu (20% ở Cấp 3, Chắc chắn 1 kẻ địch ở Cấp 4 & 5).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class RifleProjectile : MonoBehaviour, IPoolable
{
    [Header("Projectile Base Settings")]
    [SerializeField] private float moveSpeed = 18f;
    [SerializeField] private int damage = 15;
    [SerializeField] private float lifeTime = 2.5f;

    [Header("Pierce Settings")]
    [SerializeField] private float pierceChance = 0f;
    [SerializeField] private int maxPierceCount = 0;

    private Rigidbody2D rb;
    private Vector2 moveDirection;
    private float lifeTimer;
    private int currentPierceRemaining;
    private readonly HashSet<int> hitEnemyInstanceIds = new HashSet<int>();
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
        float chanceToPierce,
        int pierceCount)
    {
        damage = damageAmount;
        moveSpeed = speed;
        pierceChance = chanceToPierce;
        maxPierceCount = pierceCount;
        currentPierceRemaining = pierceCount;

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
        if (hitCollider == null) return false;
        if (hitCollider.CompareTag("Player") || hitCollider.CompareTag("BulletPlayer")) return false;

        // 1. Chướng ngại vật (Obstacle) - Không thể xuyên qua
        if (hitCollider.gameObject.layer == LayerMask.NameToLayer("Obstacle") || hitCollider.CompareTag("Obstacle"))
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

            // Kiểm tra khả năng xuyên thấu (Pierce)
            bool shouldPierce = false;
            if (currentPierceRemaining > 0)
            {
                shouldPierce = true;
                currentPierceRemaining--;
            }
            else if (pierceChance > 0f && Random.value < pierceChance)
            {
                shouldPierce = true;
                pierceChance = 0f; // Chỉ xuyên 1 lần theo cơ hội
            }

            if (shouldPierce)
            {
                return false; // Đạn tiếp tục bay xuyên qua quái
            }

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
        if (rb != null) rb.velocity = Vector2.zero;
    }

    public void OnReturnToPool()
    {
        moveDirection = Vector2.zero;
        hitEnemyInstanceIds.Clear();
        pierceChance = 0f;
        maxPierceCount = 0;
        currentPierceRemaining = 0;
        if (rb != null) rb.velocity = Vector2.zero;
    }
}
