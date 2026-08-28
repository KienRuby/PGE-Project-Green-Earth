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

    public int Damage => damage;

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
            Vector2 targetPos = homingTarget.position;
            Vector2 currentPos = transform.position;
            Vector2 desiredDir = (targetPos - currentPos).normalized;

            moveDirection = Vector2.Lerp(moveDirection, desiredDir, Time.deltaTime * homingSteerStrength).normalized;
            RotateProjectile();
        }
    }

    private Transform FindHomingTarget()
    {
        int hitCount = Physics2D.OverlapCircle(transform.position, homingRange, contactFilter, enemyBuffer);
        Transform closest = null;
        float minSqr = Mathf.Infinity;
        Vector2 currentPos = transform.position;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = enemyBuffer[i];
            if (col == null) continue;

            EnemyHealth enemy = col.GetComponentInParent<EnemyHealth>();
            if (enemy == null || enemy.IsDead || !enemy.gameObject.activeInHierarchy) continue;

            float distSqr = ((Vector2)enemy.transform.position - currentPos).sqrMagnitude;
            if (distSqr < minSqr)
            {
                minSqr = distSqr;
                closest = enemy.transform;
            }
        }

        return closest;
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
        if (other.CompareTag("Player") || other.CompareTag("BulletPlayer")) return;

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
            EnergyJumperCablesSkill.TriggerLifeSteal(damage, false);

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
