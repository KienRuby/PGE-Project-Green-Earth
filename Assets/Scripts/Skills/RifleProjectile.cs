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
                return; // Đạn tiếp tục bay xuyên qua quái
            }

            Despawn();
            return;
        }

        if (!other.isTrigger)
        {
            Despawn();
        }
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
