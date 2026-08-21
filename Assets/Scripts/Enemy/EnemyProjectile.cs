using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class EnemyProjectile : MonoBehaviour, IPoolable
{
    [Header("Thông số đạn Boss")]
    [Tooltip("Sát thương gây cho Player khi trúng đạn.")]
    [SerializeField] private int damage = 15;

    [Tooltip("Tốc độ bay của đạn theo đơn vị world mỗi giây.")]
    [SerializeField] private float moveSpeed = 7f;

    [Tooltip("Thời gian tồn tại dự phòng nếu chưa được Setup.")]
    [SerializeField] private float lifeTime = 4f;

    private Rigidbody2D rb;
    private Vector2 direction;
    private float lifeTimer;

    public int Damage => damage;
    public float MoveSpeed => moveSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.useFullKinematicContacts = true;
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
        rb.MovePosition(rb.position + direction * (moveSpeed * Time.fixedDeltaTime));
    }

    public void Setup(Vector2 moveDirection, int damageAmount, float speed, float maxRange)
    {
        direction = moveDirection.sqrMagnitude > 0f ? moveDirection.normalized : Vector2.right;
        damage = Mathf.Max(1, damageAmount);
        moveSpeed = Mathf.Max(0.1f, speed);
        lifeTime = Mathf.Max(0.1f, maxRange / moveSpeed);
        lifeTimer = lifeTime;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;

        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null)
        {
            if (!playerHealth.IsDead)
            {
                playerHealth.TakeDamage(damage);
            }
            Despawn();
            return;
        }

        if (other.GetComponentInParent<EnemyHealth>() != null || other.GetComponentInParent<EnemyProjectile>() != null)
        {
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
        direction = Vector2.zero;
        lifeTimer = lifeTime;
        if (rb != null) rb.velocity = Vector2.zero;
    }

    public void OnReturnToPool()
    {
        direction = Vector2.zero;
        if (rb != null) rb.velocity = Vector2.zero;
    }
}
