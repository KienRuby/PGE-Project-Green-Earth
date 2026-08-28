using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Đạn của kỹ năng Standard Gun (Súng Tiêu Chuẩn - Chipset ID 1).
/// Hỗ trợ: Sát thương cấp độ, Chí mạng (10% x2 dmg), Hút máu (5%-10% Life Steal),
/// Đạn nảy (Ricochet Cấp 5) và Xuyên thấu (Penetration - Khung Đỏ Tier 5).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class StandardGunProjectile : MonoBehaviour, IPoolable
{
    [Header("Projectile Base Settings")]
    [SerializeField] private float moveSpeed = 16f;
    [SerializeField] private int damage = 53;
    [SerializeField] private float lifeTime = 3f;

    [Header("Special Attributes")]
    [SerializeField] private float critChance = 0f;
    [SerializeField] private float critMultiplier = 2.0f;
    [SerializeField] private float lifeStealPercent = 0f;
    [SerializeField] private bool canRicochet = false;
    [SerializeField] private int maxRicochetCount = 1;
    [SerializeField] private float ricochetRadius = 6.0f;
    [SerializeField] private bool isPiercing = false;

    private Rigidbody2D rb;
    private Vector2 moveDirection;
    private float lifeTimer;
    private int currentRicochetRemaining;
    private PlayerHealth cachedPlayerHealth;
    private readonly HashSet<int> hitEnemyInstanceIds = new HashSet<int>();

    public int Damage => damage;
    public float MoveSpeed => moveSpeed;

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
        if (rb != null)
        {
            Vector2 nextPos = rb.position + moveDirection * (moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(nextPos);
        }
        else
        {
            transform.position += (Vector3)(moveDirection * (moveSpeed * Time.fixedDeltaTime));
        }
    }

    public void Setup(
        int damageAmount,
        float speed,
        float maxRange,
        float critRate,
        float lifeStealRate,
        bool ricochet,
        bool piercing,
        PlayerHealth playerHealthRef)
    {
        damage = damageAmount;
        moveSpeed = speed;
        critChance = critRate;
        lifeStealPercent = lifeStealRate;
        canRicochet = ricochet;
        currentRicochetRemaining = ricochet ? maxRicochetCount : 0;
        isPiercing = piercing;
        cachedPlayerHealth = playerHealthRef;

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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;

        if (other.CompareTag("Player") || other.CompareTag("BulletPlayer"))
        {
            return;
        }

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            EnemyHealth enemyHealth = damageable as EnemyHealth;
            if (enemyHealth != null)
            {
                if (enemyHealth.IsDead) return;

                int enemyId = enemyHealth.gameObject.GetInstanceID();
                if (hitEnemyInstanceIds.Contains(enemyId)) return;
                hitEnemyInstanceIds.Add(enemyId);
            }

            // 1. Tính toán Sát thương & Chí mạng (Crit x2)
            bool isCrit = critChance > 0f && Random.value < critChance;
            int finalDamage = isCrit ? Mathf.RoundToInt(damage * critMultiplier) : damage;

            damageable.TakeDamage(finalDamage);

            // 2. Kích hoạt Hút máu (Life Steal)
            if (lifeStealPercent > 0f && cachedPlayerHealth != null && !cachedPlayerHealth.IsDead)
            {
                int healAmount = Mathf.Max(1, Mathf.RoundToInt(finalDamage * lifeStealPercent));
                cachedPlayerHealth.Heal(healAmount);
            }

            // 3. Xuyên thấu (Penetration)
            if (isPiercing)
            {
                // Đạn tiếp tục bay xuyên qua quái
                return;
            }

            // 4. Đạn nảy (Ricochet Cấp 5)
            if (canRicochet && currentRicochetRemaining > 0)
            {
                if (TryRicochetToNextEnemy(other.transform.position))
                {
                    currentRicochetRemaining--;
                    return;
                }
            }

            Despawn();
            return;
        }

        if (!other.isTrigger)
        {
            Despawn();
        }
    }

    private bool TryRicochetToNextEnemy(Vector2 currentHitPos)
    {
        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(currentHitPos, ricochetRadius);
        Transform closestEnemy = null;
        float closestDistanceSqr = Mathf.Infinity;

        for (int i = 0; i < nearbyEnemies.Length; i++)
        {
            Collider2D col = nearbyEnemies[i];
            if (col == null) continue;

            EnemyHealth enemy = col.GetComponentInParent<EnemyHealth>();
            if (enemy == null || enemy.IsDead || !enemy.gameObject.activeInHierarchy) continue;

            int id = enemy.gameObject.GetInstanceID();
            if (hitEnemyInstanceIds.Contains(id)) continue;

            float distSqr = ((Vector2)enemy.transform.position - currentHitPos).sqrMagnitude;
            if (distSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distSqr;
                closestEnemy = enemy.transform;
            }
        }

        if (closestEnemy != null)
        {
            Vector2 newDir = ((Vector2)closestEnemy.position - (Vector2)transform.position).normalized;
            SetDirection(newDir);
            lifeTimer = lifeTime * 0.75f; // Làm mới thời gian tồn tại cho pha nảy
            return true;
        }

        return false;
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
        isPiercing = false;
        canRicochet = false;
        if (rb != null) rb.velocity = Vector2.zero;
    }
}
