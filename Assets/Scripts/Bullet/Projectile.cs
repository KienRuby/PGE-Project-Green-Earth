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

    private Rigidbody2D rb;
    private Vector2 moveDirection;
    private float lifeTimer;
    private Transform targetEnemy;

    public bool IsHoming
    {
        get => isHoming;
        set => isHoming = value;
    }

    public int Damage => damage;
    public float MoveSpeed => moveSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.useFullKinematicContacts = true;
        }
    }

    private void OnEnable()
    {
        lifeTimer = lifeTime;
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

        if (rb != null)
        {
            if (rb.bodyType == RigidbodyType2D.Kinematic)
            {
                Vector2 nextPos = rb.position + moveDirection * (moveSpeed * Time.fixedDeltaTime);
                rb.MovePosition(nextPos);
            }
            else
            {
                rb.velocity = moveDirection * moveSpeed;
            }
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;

        // Tránh bắn trúng Player
        if (other.CompareTag("Player") || other.GetComponentInParent<PlayerHealth>() != null)
        {
            return;
        }

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            EnemyHealth enemyHealth = other.GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null && enemyHealth.IsDead)
            {
                return;
            }

            damageable.TakeDamage(damage);
            Despawn();
            return;
        }

        // Tự hủy nếu đạn đâm vào tường hoặc vật cản vật lý (không phải trigger)
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
        targetEnemy = null;
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
    }

    public void OnReturnToPool()
    {
        moveDirection = Vector2.zero;
        targetEnemy = null;
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
    }
}