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

            damageable.TakeDamage(damage);

            // 2. Xuyên thấu toàn bộ mục tiêu (Pierce All)
            if (isPiercing)
            {
                return; // Đạn tiếp tục bay xuyên qua
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
        isPiercing = false;
        knockbackForce = 0f;
        if (rb != null) rb.velocity = Vector2.zero;
    }
}
